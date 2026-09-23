using System.Text.Json;
using Jarvis.ApplicationCapabilities;
using AppId = Jarvis.ApplicationCapabilities.ApplicationId;
namespace Jarvis.Gate5;
public sealed record AcceptanceStep(string Command, AppId? Application);
public sealed class FormalAcceptance
{
    public static IReadOnlyList<AcceptanceStep> Steps { get; } = Array.AsReadOnly(new[] {
        new AcceptanceStep("Open Fight Explorer", null),
        new AcceptanceStep("Open Calculator", AppId.Calculator), new AcceptanceStep("Open Microsoft Word", AppId.Word),
        new AcceptanceStep("Open Notepad", AppId.Notepad), new AcceptanceStep("Open File Explorer", AppId.FileExplorer),
        new AcceptanceStep("Launch Calculator", AppId.Calculator), new AcceptanceStep("Launch Microsoft Word", AppId.Word),
        new AcceptanceStep("Start Notepad", AppId.Notepad), new AcceptanceStep("Start File Explorer", AppId.FileExplorer),
        new AcceptanceStep("Open Word", AppId.Word), new AcceptanceStep("Start Calculator", AppId.Calculator)
    });
    private readonly List<(string Stage, JsonElement Payload)> events = new();
    public void Reset() => events.Clear();
    public void Observe(string stage, object? data)
    {
        var payload = JsonSerializer.SerializeToElement(data).GetProperty("data");
        if (payload.TryGetProperty("operationId", out _)) payload = payload.GetProperty("data");
        events.Add((stage, payload.Clone()));
    }
    public bool Verify(AcceptanceStep step, LaunchResult? result, bool conversationCompleted, out string reason)
    {
        int Count(string stage) => events.Count(x => x.Stage == stage);
        reason = "Acceptance requirements not met; stop and inspect retained evidence.";
        if (!conversationCompleted || Count("parser_input") != 1 || Count("response_text") != 1) return false;
        if (step.Application is null)
        {
            var transcript = events.Single(x => x.Stage == "parser_input").Payload.GetProperty("transcript").GetString()?.Trim().TrimEnd('.', '!', '?');
            if (!string.Equals(transcript, step.Command, StringComparison.OrdinalIgnoreCase) || Count("intent_rejected") != 1 || Count("typed_request") != 0 || Count("executor_invoked") != 0 || Count("process_start_requested") != 0 || result is not null) return false;
            if (string.IsNullOrWhiteSpace(events.Single(x => x.Stage == "response_text").Payload.GetProperty("response").GetString())) return false;
            reason = "Intentional rejection verified: zero typed requests, executor calls and launch requests."; return true;
        }
        if (Count("typed_request") != 1 || Count("executor_invoked") != 1 || Count("authorization_decision") != 1 || Count("structured_result") != 1) return false;
        if (!events.Single(x => x.Stage == "authorization_decision").Payload.GetProperty("allowed").GetBoolean()) return false;
        if (result?.Application != step.Application || result.Status is not (LaunchStatus.Succeeded or LaunchStatus.AlreadyRunning) || result.Evidence is null || result.Evidence.ProcessId <= 0 || result.Evidence.WindowHandle == 0) return false;
        if (events.Single(x => x.Stage == "typed_request").Payload.GetProperty("Application").GetInt32() != (int)step.Application.Value) return false;
        if (result.Status == LaunchStatus.Succeeded && (Count("process_start_requested") != 1 || Count("process_start_returned") != 1 || !result.LaunchAttempted)) return false;
        if (result.Status == LaunchStatus.AlreadyRunning && (Count("process_start_requested") != 0 || result.LaunchAttempted)) return false;
        if (events.Single(x => x.Stage == "response_text").Payload.GetProperty("response").GetString() != result.Message) return false;
        reason = $"Verified {result.Status}: one typed request, authorized executor call and matching window evidence."; return true;
    }
}
