using Jarvis.Voice;

var diagnostics = new JsonLineVoiceDiagnosticSink(Path.Combine(AppContext.BaseDirectory, "logs", "voice-gate2.jsonl"));
var capture = new WindowsMicrophoneCapture(diagnostics);
var stt = new FasterWhisperSttProvider(diagnostics);
var completed = 0;
for (var attempt = 1; attempt <= 10; attempt++)
{
    var turnId = Guid.NewGuid();
    try
    {
        Console.WriteLine($"Gate 2 test {attempt}/10. Turn {turnId}.");
        var audio = await capture.CaptureUntilEnterAsync(turnId);
        var result = await stt.TranscribeAsync(audio);
        Console.WriteLine($"FINAL TRANSCRIPT [{turnId}]: {result.Transcript}");
        Console.WriteLine($"TRANSCRIPTION ELAPSED MS: {result.Elapsed.TotalMilliseconds:F0}");
        completed++;
    }
    catch (Exception ex) { diagnostics.Write("gate2_fault", new Dictionary<string, object?> { ["turnId"] = turnId, ["reason"] = ex.Message }); Console.Error.WriteLine($"GATE2_FAULT [{turnId}]: {ex.Message}"); break; }
}
Console.WriteLine($"gate2TranscriptCompletionCount={completed}");
return completed == 10 ? 0 : 1;
