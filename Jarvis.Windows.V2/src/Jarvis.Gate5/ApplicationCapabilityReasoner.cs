using Jarvis.Core;
using Jarvis.ApplicationCapabilities;
using Jarvis.Voice;
namespace Jarvis.Gate5;

/// <summary>Instrumentation and response composition above the frozen IJarvisReasoner boundary.</summary>
public sealed class ApplicationCapabilityReasoner(ApplicationLaunchExecutor executor, Action<string, object?>? trace = null) : IJarvisReasoner
{
    private readonly ApplicationLaunchIntentRouter router = new();
    public LaunchResult? LastResult { get; private set; }
    public async Task<string> RespondAsync(string transcript, Guid turnId, CancellationToken cancellationToken = default)
    {
        LastResult = null;
        var requestTraceId = Guid.NewGuid();
        void Trace(string stage, object? data) => trace?.Invoke(stage, new { requestTraceId, turnId, data });
        cancellationToken.ThrowIfCancellationRequested();
        Trace("parser_input", new { transcript });
        string response;
        if (!router.TryRoute(transcript, out var request, out var rejection))
        {
            Trace("intent_rejected", new { reason = rejection, typedRequestCount = 0 });
            response = rejection == "empty_transcript" ? "I did not receive any words. No application was requested." :
                "I could not recognize a single application command. No application was launched. Please say, for example, open Word.";
        }
        else
        {
            Trace("intent_recognized", new { intent = "LaunchApplication", application = request!.Application.ToString() });
            Trace("typed_request", request);
            LastResult = await executor.ExecuteAsync(request, TimeSpan.FromSeconds(15), trace: Trace);
            cancellationToken.ThrowIfCancellationRequested();
            response = LastResult.Message;
        }
        Trace("response_text", new { response });
        return response;
    }
}

