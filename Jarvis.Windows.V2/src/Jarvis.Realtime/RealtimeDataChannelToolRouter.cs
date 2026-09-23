using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Jarvis.Realtime;

public sealed class RealtimeDataChannelToolRouter(IRealtimeToolBridge bridge, IRealtimeAuditSink audit)
{
    private readonly Dictionary<string, IReadOnlyList<string>> completedCalls = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public async Task<IReadOnlyList<string>> HandleServerEventAsync(
        Guid sessionId,
        string eventJson,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseToolCall(sessionId, eventJson, out var toolCall, out var callId))
            return [];

        var eventType = ReadEventType(eventJson);
        if (eventType is "response.output_item.done" or "response.function_call_arguments.done")
            audit.Write(sessionId, toolCall.CorrelationId, "GA_FUNCTION_CALL_RECEIVED", new { eventType, toolCall.Name, callId });

        var idempotencyKey = sessionId.ToString("N") + ":" + callId;
        lock (sync)
        {
            if (completedCalls.TryGetValue(idempotencyKey, out var cached))
            {
                audit.Write(sessionId, toolCall.CorrelationId, "TOOL_DUPLICATE_REPLAY_SUPPRESSED", new { toolCall.Name, callId });
                return cached;
            }
        }

        audit.Write(sessionId, toolCall.CorrelationId, "TOOL_REQUEST_RECEIVED", new { toolCall.Name, callId, state = "REQUESTED" });
        audit.Write(sessionId, toolCall.CorrelationId, "TOOL_AUTHORIZATION_RESULT", new { toolCall.Name, callId, state = "AUTHORIZATION_CHECK" });
        audit.Write(sessionId, toolCall.CorrelationId, "TOOL_EXECUTION_STARTED", new { toolCall.Name, callId, state = "EXECUTING" });
        var result = await bridge.InvokeAsync(toolCall, cancellationToken).ConfigureAwait(false);
        audit.Write(sessionId, toolCall.CorrelationId, "TOOL_VERIFICATION_RESULT", new { result.Name, result.Status, result.FailureReason });
        var responses = new[]
        {
            JsonSerializer.Serialize(new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = JsonSerializer.Serialize(new
                    {
                        operationId = toolCall.CorrelationId,
                        status = result.Status.ToString(),
                        message = result.Message,
                        failureReason = result.FailureReason,
                        evidence = result.Evidence
                    })
                }
            }),
            JsonSerializer.Serialize(new
            {
                type = "response.create",
                response = new
                {
                    instructions = "Report the tool result truthfully and briefly. Claim success only when status is SUCCEEDED. If status is UNAVAILABLE, FAILED, VERIFICATION_FAILED, DENIED, REJECTED, TIMED_OUT, CANCELLED, or CONFIRMATION_REQUIRED, state that exact outcome plainly. Do not say an operation is being processed unless the tool result explicitly reports a tracked pending state."
                }
            })
        };
        lock (sync)
        {
            completedCalls[idempotencyKey] = responses;
        }

        audit.Write(sessionId, toolCall.CorrelationId, "TOOL_RESULT_RETURNED", new { result.Name, result.Status, result.FailureReason });
        return responses;
    }

    public static bool TryParseToolCall(
        Guid sessionId,
        string eventJson,
        out RealtimeToolCall call,
        out string callId)
    {
        call = default!;
        callId = string.Empty;
        using var document = JsonDocument.Parse(eventJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        var type = ReadString(root, "type");
        JsonElement toolElement;
        if (string.Equals(type, "response.output_item.done", StringComparison.Ordinal) &&
            root.TryGetProperty("item", out var item) &&
            string.Equals(ReadString(item, "type"), "function_call", StringComparison.Ordinal))
        {
            toolElement = item;
        }
        else if (string.Equals(type, "response.function_call_arguments.done", StringComparison.Ordinal))
        {
            toolElement = root;
        }
        else if (string.Equals(type, "tool.call", StringComparison.Ordinal))
        {
            toolElement = root;
        }
        else
        {
            return false;
        }

        var name = ReadString(toolElement, "name");
        callId = ReadString(toolElement, "call_id") ?? ReadString(toolElement, "id") ?? Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var arguments = ReadArguments(toolElement);
        call = new RealtimeToolCall(sessionId, StableCorrelationId(sessionId, callId), name, arguments);
        return true;
    }

    private static JsonElement ReadArguments(JsonElement toolElement)
    {
        if (!toolElement.TryGetProperty("arguments", out var args))
            return JsonSerializer.SerializeToElement(new { });
        if (args.ValueKind == JsonValueKind.String)
        {
            var text = args.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return JsonSerializer.SerializeToElement(new { });
            using var parsed = JsonDocument.Parse(text);
            return parsed.RootElement.Clone();
        }

        return args.Clone();
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(name, out var element) &&
        element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? ReadEventType(string eventJson)
    {
        using var document = JsonDocument.Parse(eventJson);
        return ReadString(document.RootElement, "type");
    }

    private static Guid StableCorrelationId(Guid sessionId, string callId)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(sessionId.ToString("N") + ":" + callId));
        return new Guid(bytes);
    }
}
