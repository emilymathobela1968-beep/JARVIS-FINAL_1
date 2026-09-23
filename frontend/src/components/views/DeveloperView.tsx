import React, { useState } from 'react';
import { soundFX } from '../../utils/audio';

interface DeveloperViewProps {
  onExecuteTool: (tool: string, params: string) => void;
}

interface TestSuite {
  name: string;
  command: string;
  total: number;
  passed: number;
  failed: number;
  status: 'PASSED' | 'RUNNING' | 'IDLE';
}

export const DeveloperView: React.FC<DeveloperViewProps> = ({ onExecuteTool }) => {
  const [workspace, setWorkspace] = useState('C:\\JARVIS_CODEX\\Jarvis.Windows.V2');
  const [objective, setObjective] = useState('Inspect RealtimeWebViewHost payload parser and run test suites');
  const [activeFile, setActiveFile] = useState('RealtimeWebViewHost.cs');
  const [terminalOutput, setTerminalOutput] = useState<string[]>([
    'JARVIS Developer Subsystem Initialized.',
    'Workspace approved: C:\\JARVIS_CODEX\\Jarvis.Windows.V2',
    'Governance boundary: Bounded source inspection & controlled builds.',
  ]);
  const [isExecuting, setIsExecuting] = useState(false);

  const [testSuites, setTestSuites] = useState<TestSuite[]>([
    {
      name: 'Jarvis.Realtime.Tests',
      command: 'dotnet run --project src/Jarvis.Realtime.Tests/Jarvis.Realtime.Tests.csproj -p:Platform=x64',
      total: 70,
      passed: 70,
      failed: 0,
      status: 'PASSED',
    },
    {
      name: 'Jarvis.AppBuilder.Tests',
      command: 'dotnet run --project src/Jarvis.AppBuilder.Tests/Jarvis.AppBuilder.Tests.csproj -p:Platform=x64',
      total: 36,
      passed: 36,
      failed: 0,
      status: 'PASSED',
    },
    {
      name: 'Jarvis.Developer.Tests',
      command: 'dotnet run --project src/Jarvis.Developer.Tests/Jarvis.Developer.Tests.csproj -p:Platform=x64',
      total: 11,
      passed: 11,
      failed: 0,
      status: 'PASSED',
    },
  ]);

  const fileSnippets: Record<string, string> = {
    'RealtimeWebViewHost.cs': `// Jarvis.App / RealtimeWebViewHost.cs
// Fixed: Object payload parser ensures mic.stream.started reaches readiness gate
public void HandleWebMessage(string messageJson)
{
    using var doc = JsonDocument.Parse(messageJson);
    var root = doc.RootElement;
    string eventType = root.GetProperty("type").GetString() ?? "";

    switch (eventType)
    {
        case "mic.stream.started":
            // Accepts string payloads normally and raw JSON objects without rejection
            _logger.LogInformation("Physical mic stream active: {settings}", root.GetRawText());
            _readinessGate.SignalEvidence(ReadinessEvidence.MicStreamStarted);
            break;
        case "data.channel.open":
            _readinessGate.SignalEvidence(ReadinessEvidence.DataChannelOpen);
            break;
        case "remote.audio.track":
            _readinessGate.SignalEvidence(ReadinessEvidence.RemoteAudioTrack);
            break;
        case "remote.audio.playing":
            _readinessGate.SignalEvidence(ReadinessEvidence.RemoteAudioPlaying);
            break;
    }
}`,
    'DesktopVoiceRuntime.cs': `// Jarvis.App / DesktopVoiceRuntime.cs
public class DesktopVoiceRuntime : IAsyncDisposable
{
    private readonly RealtimeWebViewHost _webViewHost;
    private readonly ILogger<DesktopVoiceRuntime> _logger;

    public VoiceState State { get; private set; } = VoiceState.Offline;

    public async Task StartRealtimeSessionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating Azure/OpenAI Realtime WebRTC session...");
        State = VoiceState.Listening;
        await _webViewHost.InitializeWebRtcPeerConnectionAsync(ct);
    }
}`,
    'MainWindow.xaml.cs': `// Jarvis.App / MainWindow.xaml.cs
public sealed partial class MainWindow : Window
{
    private readonly DesktopVoiceRuntime _voiceRuntime;
    private readonly IGovernedDispatcher _dispatcher;

    public MainWindow()
    {
        this.InitializeComponent();
        _voiceRuntime = new DesktopVoiceRuntime();
        CurrentStateText.Text = "OFFLINE";
    }
}`
  };

  const handleRunDevTask = () => {
    if (!objective.trim() || isExecuting) return;
    setIsExecuting(true);
    soundFX.playExecute();

    const timestamp = new Date().toLocaleTimeString();
    setTerminalOutput((prev) => [
      ...prev,
      `[${timestamp}] DEV_EXECUTE: Objective="${objective}" on Workspace="${workspace}"`,
      `[${timestamp}] Analyzing code graph and testing prerequisites...`,
    ]);

    setTimeout(() => {
      setTerminalOutput((prev) => [
        ...prev,
        `[${new Date().toLocaleTimeString()}] Building Jarvis.sln (Configuration=Debug, Platform=x64)...`,
        'Build succeeded. 0 Warning(s). 0 Error(s).',
      ]);
      setIsExecuting(false);
      onExecuteTool('developer.run_task', `Objective=${objective}`);
    }, 1200);
  };

  const handleRunAllTests = () => {
    soundFX.playExecute();
    setIsExecuting(true);
    setTerminalOutput((prev) => [
      ...prev,
      `[${new Date().toLocaleTimeString()}] Running verified test suites across 3 projects...`,
    ]);

    setTimeout(() => {
      setTerminalOutput((prev) => [
        ...prev,
        'REALTIME_TESTS_TOTAL passed=70 failed=0',
        'APP_BUILDER_TESTS_TOTAL passed=36 failed=0',
        'DEVELOPER_TESTS_TOTAL passed=11 failed=0',
        'TOTAL: 117 passed, 0 failed. All verification boundaries green.',
      ]);
      setIsExecuting(false);
      onExecuteTool('developer.run_tests', 'Suites=Realtime,AppBuilder,Developer');
    }, 1000);
  };

  return (
    <div id="developer-view" className="w-full max-w-5xl mx-auto space-y-6">
      {/* Title & Description */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl sm:text-3xl font-semibold text-[#F5FAFF]">Developer</h2>
          <span className="font-mono-jarvis text-xs px-3 py-1 bg-[#1A1A2E] border border-purple-500/40 text-purple-300 rounded-full">
            GOVERNED AGENT WORKSPACE
          </span>
        </div>
        <p className="text-sm sm:text-base text-[#8CBFEA] max-w-3xl">
          Bounded source inspection, edits, tests, and builds remain behind Developer governance.
        </p>
      </div>

      {/* Inputs Form */}
      <div className="bg-[#091524]/80 border border-[#45BCFF]/30 rounded-2xl p-4 sm:p-5 space-y-3 backdrop-blur-md">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
          <div className="md:col-span-4">
            <label className="block text-[11px] font-mono-jarvis text-[#8CBFEA] mb-1">
              APPROVED WORKSPACE
            </label>
            <input
              type="text"
              value={workspace}
              onChange={(e) => setWorkspace(e.target.value)}
              placeholder="Approved workspace path"
              className="w-full bg-[#050D16] border border-[#45BCFF]/30 rounded-xl px-3 py-2 text-xs sm:text-sm font-mono-jarvis text-[#F5FAFF] focus:outline-none focus:border-[#55C7FF]"
            />
          </div>

          <div className="md:col-span-6">
            <label className="block text-[11px] font-mono-jarvis text-[#8CBFEA] mb-1">
              BOUNDED OBJECTIVE
            </label>
            <input
              type="text"
              value={objective}
              onChange={(e) => setObjective(e.target.value)}
              placeholder="Bounded development objective"
              className="w-full bg-[#050D16] border border-[#45BCFF]/30 rounded-xl px-3 py-2 text-xs sm:text-sm font-mono-jarvis text-[#F5FAFF] focus:outline-none focus:border-[#55C7FF]"
            />
          </div>

          <div className="md:col-span-2 flex items-end">
            <button
              onClick={handleRunDevTask}
              disabled={isExecuting}
              className={`w-full py-2.5 rounded-xl font-mono-jarvis text-xs sm:text-sm font-semibold transition-all active:scale-95 ${
                isExecuting
                  ? 'bg-purple-950 text-purple-300 border border-purple-500/50 cursor-wait'
                  : 'bg-[#183E63] hover:bg-[#20527C] border border-[#55C7FF] text-[#E8F7FF] shadow-[0_0_15px_rgba(69,188,255,0.25)]'
              }`}
            >
              {isExecuting ? 'Running...' : 'Run Dev Task'}
            </button>
          </div>
        </div>
      </div>

      {/* Test Suites Verification Banner */}
      <div className="bg-[#0A1626]/70 border border-[#45BCFF]/20 rounded-2xl p-4 space-y-3">
        <div className="flex items-center justify-between">
          <div className="font-mono-jarvis text-xs font-semibold text-[#45BCFF] tracking-wider">
            VERIFIED SUITES (117 / 117 PASSED)
          </div>
          <button
            onClick={handleRunAllTests}
            className="text-xs font-mono-jarvis px-3 py-1 bg-[#102B45] hover:bg-[#1A4166] text-[#55C7FF] border border-[#45BCFF]/40 rounded-lg transition-all"
          >
            Re-run All Tests
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          {testSuites.map((ts, idx) => (
            <div key={idx} className="p-3 bg-[#06101B] border border-[#45BCFF]/20 rounded-xl space-y-1">
              <div className="flex items-center justify-between text-xs font-semibold text-white">
                <span>{ts.name}</span>
                <span className="text-emerald-400 font-mono-jarvis">PASSED</span>
              </div>
              <div className="text-[11px] font-mono-jarvis text-[#8CBFEA]">
                {ts.passed} / {ts.total} tests passed (0 failures)
              </div>
              <div className="w-full h-1.5 bg-neutral-800 rounded-full overflow-hidden mt-1">
                <div className="w-full h-full bg-emerald-400" />
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Code Inspector & Live Terminal Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Source Code Viewer */}
        <div className="bg-[#081320] border border-[#45BCFF]/30 rounded-2xl p-4 space-y-3 flex flex-col">
          <div className="flex items-center justify-between border-b border-[#45BCFF]/20 pb-2">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">SOURCE INSPECTION:</span>
              <div className="flex gap-1">
                {Object.keys(fileSnippets).map((fn) => (
                  <button
                    key={fn}
                    onClick={() => {
                      soundFX.playBeep(600, 0.04);
                      setActiveFile(fn);
                    }}
                    className={`text-[11px] font-mono-jarvis px-2 py-0.5 rounded transition-all ${
                      activeFile === fn
                        ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF]/40'
                        : 'text-[#7B9FB8] hover:text-white'
                    }`}
                  >
                    {fn}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <pre className="flex-1 bg-[#03080F] p-3 rounded-xl border border-[#45BCFF]/10 text-xs font-mono-jarvis text-[#9CD5FF] overflow-x-auto leading-relaxed max-h-[320px]">
            <code>{fileSnippets[activeFile]}</code>
          </pre>
        </div>

        {/* Governed Terminal Output */}
        <div className="bg-[#081320] border border-[#45BCFF]/30 rounded-2xl p-4 space-y-3 flex flex-col">
          <div className="flex items-center justify-between border-b border-[#45BCFF]/20 pb-2">
            <span className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">GOVERNED EXECUTION OUTPUT</span>
            <button
              onClick={() => setTerminalOutput(['Console cleared. Subsystem ready.'])}
              className="text-[11px] font-mono-jarvis text-[#7B9FB8] hover:text-white"
            >
              Clear
            </button>
          </div>

          <div className="flex-1 bg-[#03080F] p-3 rounded-xl border border-[#45BCFF]/10 text-xs font-mono-jarvis text-emerald-400 overflow-y-auto space-y-1 max-h-[320px]">
            {terminalOutput.map((line, i) => (
              <div key={i} className="leading-tight">
                <span className="text-[#45BCFF] mr-2">›</span>
                <span>{line}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
