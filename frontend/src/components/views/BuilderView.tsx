import React, { useState } from 'react';
import { BuilderRevision } from '../../types';
import { soundFX } from '../../utils/audio';

interface BuilderViewProps {
  onExecuteTool: (tool: string, params: string) => void;
}

const INITIAL_REVISIONS: BuilderRevision[] = [
  {
    id: 'REV-001',
    name: 'Universal HUD Interface',
    timestamp: '2026-09-21 11:20',
    evalScore: 94,
    status: 'Accepted',
    summary: 'High-contrast cyan & deep glass dark layout with concentric Arc Reactor and Cascadia Mono typography.',
    renderedType: 'dashboard',
  },
  {
    id: 'REV-002',
    name: 'Spatial Node Network',
    timestamp: '2026-09-21 18:05',
    evalScore: 88,
    status: 'Candidate',
    summary: '3D Node network with interactive capability hubs and real-time execution links.',
    renderedType: 'hologram',
  },
  {
    id: 'REV-003',
    name: 'Developer Terminal Canvas',
    timestamp: '2026-09-21 21:10',
    evalScore: 92,
    status: 'Candidate',
    summary: 'Streaming CLI pane with dual code inspector and test verification matrix.',
    renderedType: 'terminal',
  },
];

export const BuilderView: React.FC<BuilderViewProps> = ({ onExecuteTool }) => {
  const [projectLoaded, setProjectLoaded] = useState(true);
  const [builderMode, setBuilderMode] = useState<'DESIGN' | 'PREVIEW' | 'INSPECT'>('DESIGN');
  const [selectedRevision, setSelectedRevision] = useState<BuilderRevision>(INITIAL_REVISIONS[0]);
  const [leftOpen, setLeftOpen] = useState(true);
  const [rightOpen, setRightOpen] = useState(true);
  const [selectedElement, setSelectedElement] = useState<string>('ArcReactorVisualizer [Ring-Core-HUD]');
  const [revisions, setRevisions] = useState<BuilderRevision[]>(INITIAL_REVISIONS);

  const handleLoadProject = () => {
    soundFX.playExecute();
    setProjectLoaded(true);
    onExecuteTool('builder.load_project', 'Target=Jarvis.UniversalSurface');
  };

  const handleRenderMock = () => {
    soundFX.playExecute();
    const newRev: BuilderRevision = {
      id: `REV-00${revisions.length + 1}`,
      name: `Iterative Screen Revision ${revisions.length + 1}`,
      timestamp: new Date().toLocaleTimeString(),
      evalScore: Math.floor(90 + Math.random() * 8),
      status: 'Candidate',
      summary: 'Automated synthesis generated with governed capability tokens.',
      renderedType: 'dashboard',
    };
    setRevisions([newRev, ...revisions]);
    setSelectedRevision(newRev);
    onExecuteTool('builder.render_mock', `Rev=${newRev.id}`);
  };

  const handleReviseMock = () => {
    soundFX.playAcknowledge();
    onExecuteTool('builder.revise_mock', `Rev=${selectedRevision.id}`);
  };

  const handleAcceptMock = () => {
    soundFX.playExecute();
    setRevisions(revisions.map(r => r.id === selectedRevision.id ? { ...r, status: 'Accepted' } : r));
    setSelectedRevision({ ...selectedRevision, status: 'Accepted' });
    onExecuteTool('builder.accept_mock', `Rev=${selectedRevision.id}`);
  };

  const handleRejectMock = () => {
    soundFX.playBeep(350, 0.1, 'sawtooth');
    setRevisions(revisions.map(r => r.id === selectedRevision.id ? { ...r, status: 'Rejected' } : r));
    setSelectedRevision({ ...selectedRevision, status: 'Rejected' });
    onExecuteTool('builder.reject_mock', `Rev=${selectedRevision.id}`);
  };

  return (
    <div id="builder-view" className="w-full max-w-6xl mx-auto space-y-4">
      {/* Top Builder Control Header */}
      <div className="bg-[#070E17]/90 border border-[#8A6A2A]/80 rounded-2xl p-3 sm:p-4 backdrop-blur-md flex flex-wrap items-center justify-between gap-3 shadow-lg">
        <div className="flex items-center gap-3">
          <span className="text-xl sm:text-2xl font-bold text-[#F5FAFF] tracking-wider">
            BUILDER
          </span>
          <span className="font-mono-jarvis text-xs px-2.5 py-0.5 rounded bg-[#2A1E08] text-[#F5B843] border border-[#8A6A2A]">
            {builderMode}
          </span>
        </div>

        <div className="text-xs sm:text-sm text-[#8CBFEA] truncate max-w-md">
          {projectLoaded 
            ? 'Project: Jarvis Workspace (36 tests verified)' 
            : 'No App Builder project loaded.'}
        </div>

        <div className="flex items-center gap-1.5 sm:gap-2">
          <button
            onClick={handleLoadProject}
            className="px-3 py-1.5 rounded-lg bg-[#142B3E] hover:bg-[#1F3E58] border border-[#45BCFF]/40 text-xs font-mono-jarvis text-[#55C7FF]"
          >
            Load
          </button>
          <button
            onClick={handleRenderMock}
            className="px-3 py-1.5 rounded-lg bg-[#142B3E] hover:bg-[#1F3E58] border border-[#45BCFF]/40 text-xs font-mono-jarvis text-[#55C7FF]"
          >
            Render
          </button>
          <button
            onClick={handleReviseMock}
            className="px-3 py-1.5 rounded-lg bg-[#142B3E] hover:bg-[#1F3E58] border border-[#45BCFF]/40 text-xs font-mono-jarvis text-[#55C7FF]"
          >
            Revise
          </button>
          <button
            onClick={handleAcceptMock}
            className="px-3 py-1.5 rounded-lg bg-emerald-950 hover:bg-emerald-900 border border-emerald-500/50 text-xs font-mono-jarvis text-emerald-300"
          >
            Accept
          </button>
          <button
            onClick={handleRejectMock}
            className="px-3 py-1.5 rounded-lg bg-red-950 hover:bg-red-900 border border-red-500/50 text-xs font-mono-jarvis text-red-300"
          >
            Reject
          </button>
        </div>
      </div>

      {/* 3-Column Studio Workspace */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3 min-h-[480px]">
        {/* Left Column: Navigator */}
        {leftOpen ? (
          <div className="lg:col-span-3 bg-[#060B10]/95 border border-[#2A5C8A] rounded-2xl p-4 flex flex-col space-y-4">
            <div className="flex items-center justify-between border-b border-[#2A5C8A]/40 pb-2">
              <span className="font-mono-jarvis text-xs font-bold text-[#45BCFF] tracking-wider">
                NAVIGATOR
              </span>
              <button
                onClick={() => setLeftOpen(false)}
                className="text-xs px-2 py-0.5 rounded bg-[#102030] text-[#8CBFEA] hover:text-white"
              >
                -
              </button>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">DURABLE CONTEXT</div>
              <p className="text-xs text-[#8CBFEA]">
                {selectedRevision.summary}
              </p>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">CAPABILITY TRUTH</div>
              <div className="p-2 rounded bg-[#03060A] border border-[#2A5C8A]/30 text-[11px] font-mono-jarvis text-[#55C7FF] space-y-1">
                <div>✔ Voice WebRTC Subsystem</div>
                <div>✔ Governed Windows Dispatch</div>
                <div>✔ AppBuilder Tests: 36/36 Pass</div>
                <div>✔ Developer Bounded Sandbox</div>
              </div>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">SCREENS & SLICES</div>
              <div className="space-y-1">
                {revisions.map((rev) => (
                  <button
                    key={rev.id}
                    onClick={() => {
                      soundFX.playBeep(600, 0.04);
                      setSelectedRevision(rev);
                    }}
                    className={`w-full text-left p-2 rounded-lg text-xs font-mono-jarvis transition-all ${
                      selectedRevision.id === rev.id
                        ? 'bg-[#143048] text-[#55C7FF] border border-[#45BCFF]/50'
                        : 'bg-[#0A1420] text-[#789BB3] hover:text-white'
                    }`}
                  >
                    <div className="flex justify-between font-semibold">
                      <span>{rev.id}</span>
                      <span>{rev.evalScore}/100</span>
                    </div>
                    <div className="text-[11px] truncate opacity-80">{rev.name}</div>
                  </button>
                ))}
              </div>
            </div>
          </div>
        ) : (
          <div className="lg:col-span-1 flex items-start">
            <button
              onClick={() => setLeftOpen(true)}
              className="p-2 rounded-xl bg-[#060B10] border border-[#2A5C8A] text-xs font-mono-jarvis text-[#55C7FF]"
              title="Expand Navigator"
            >
              NAV +
            </button>
          </div>
        )}

        {/* Center Column: Render Preview Canvas */}
        <div className={`${leftOpen && rightOpen ? 'lg:col-span-6' : leftOpen || rightOpen ? 'lg:col-span-8' : 'lg:col-span-10'} bg-[#020509] border border-[#45BCFF]/40 rounded-2xl p-3 sm:p-4 flex flex-col space-y-3`}>
          {/* Mode Switcher Tabs */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  soundFX.playBeep(550, 0.04);
                  setBuilderMode('DESIGN');
                }}
                className={`px-3 py-1 rounded-lg text-xs font-mono-jarvis font-semibold transition-all ${
                  builderMode === 'DESIGN'
                    ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF]'
                    : 'text-[#789BB3] hover:text-white'
                }`}
              >
                DESIGN
              </button>
              <button
                onClick={() => {
                  soundFX.playBeep(550, 0.04);
                  setBuilderMode('PREVIEW');
                }}
                className={`px-3 py-1 rounded-lg text-xs font-mono-jarvis font-semibold transition-all ${
                  builderMode === 'PREVIEW'
                    ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF]'
                    : 'text-[#789BB3] hover:text-white'
                }`}
              >
                PREVIEW
              </button>
              <button
                onClick={() => {
                  soundFX.playBeep(550, 0.04);
                  setBuilderMode('INSPECT');
                }}
                className={`px-3 py-1 rounded-lg text-xs font-mono-jarvis font-semibold transition-all ${
                  builderMode === 'INSPECT'
                    ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF]'
                    : 'text-[#789BB3] hover:text-white'
                }`}
              >
                INSPECT
              </button>
            </div>
            <span className="text-xs font-mono-jarvis text-[#8CBFEA]">1280 × 720 Viewport</span>
          </div>

          {/* Interactive Scaled Canvas */}
          <div className="flex-1 w-full bg-[#03070D] border border-[#45BCFF]/20 rounded-xl relative overflow-hidden flex items-center justify-center p-4">
            <div className="w-full max-w-[560px] aspect-video bg-[#05111E] border border-[#55C7FF]/40 rounded-lg p-4 relative shadow-2xl flex flex-col justify-between">
              {/* Inner Mock Canvas Top Bar */}
              <div className="flex items-center justify-between border-b border-cyan-500/20 pb-2">
                <div className="flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full bg-cyan-400 animate-pulse" />
                  <span className="font-mono-jarvis text-[11px] text-cyan-300 font-bold">
                    JARVIS V2 • {selectedRevision.name}
                  </span>
                </div>
                <span className="text-[10px] font-mono-jarvis text-[#8CBFEA]">ACTIVE RENDER</span>
              </div>

              {/* Center Wireframe / Interactive Elements */}
              <div className="my-auto flex flex-col items-center justify-center py-4 space-y-3">
                <div 
                  onClick={() => setSelectedElement('ArcReactor [Center HUD Pulse]')}
                  className="w-20 h-20 rounded-full border-2 border-cyan-400 bg-cyan-950/40 flex items-center justify-center text-cyan-200 text-xs font-mono-jarvis cursor-pointer hover:scale-105 transition-transform"
                >
                  CORE
                </div>
                <div 
                  onClick={() => setSelectedElement('DialogueBar [Command Surface]')}
                  className="w-3/4 h-6 rounded-md border border-cyan-500/40 bg-[#0C1E30] flex items-center px-2 text-[10px] font-mono-jarvis text-[#8CBFEA] cursor-pointer hover:border-cyan-400"
                >
                  › Command line input component
                </div>
              </div>

              {/* Bottom Telemetry Strip */}
              <div className="flex items-center justify-between text-[9px] font-mono-jarvis text-cyan-400/70 border-t border-cyan-500/20 pt-1">
                <span>FPS: 60.0</span>
                <span>GOVERNED RENDER BOUNDARY</span>
                <span>LATENCY: 12ms</span>
              </div>
            </div>
          </div>
        </div>

        {/* Right Column: Inspector */}
        {rightOpen ? (
          <div className="lg:col-span-3 bg-[#060B10]/95 border border-[#2A5C8A] rounded-2xl p-4 flex flex-col space-y-4">
            <div className="flex items-center justify-between border-b border-[#2A5C8A]/40 pb-2">
              <span className="font-mono-jarvis text-xs font-bold text-[#45BCFF] tracking-wider">
                INSPECTOR
              </span>
              <button
                onClick={() => setRightOpen(false)}
                className="text-xs px-2 py-0.5 rounded bg-[#102030] text-[#8CBFEA] hover:text-white"
              >
                -
              </button>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">CURRENT SCREEN / REVISION</div>
              <div className="text-xs font-semibold text-white font-mono-jarvis">
                {selectedRevision.id}: {selectedRevision.name}
              </div>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">SELECTION</div>
              <div className="p-2 rounded bg-[#03060A] border border-[#2A5C8A]/30 text-xs font-mono-jarvis text-[#55C7FF]">
                {selectedElement}
              </div>
            </div>

            <div className="space-y-1">
              <div className="text-[11px] font-mono-jarvis text-[#5E83A8]">VISUAL EVALUATION</div>
              <div className="p-3 rounded-xl bg-[#05111E] border border-emerald-500/30 space-y-2">
                <div className="flex items-center justify-between text-xs font-semibold">
                  <span className="text-[#8CBFEA]">Score:</span>
                  <span className="text-emerald-400 font-mono-jarvis">{selectedRevision.evalScore} / 100</span>
                </div>
                <div className="w-full h-1.5 bg-neutral-800 rounded-full overflow-hidden">
                  <div 
                    className="h-full bg-emerald-400"
                    style={{ width: `${selectedRevision.evalScore}%` }}
                  />
                </div>
                <p className="text-[11px] text-[#9CD5FF] leading-relaxed">
                  High contrast passes WCAG AA. Responsive single-surface viewport without jarring layout shifts.
                </p>
              </div>
            </div>
          </div>
        ) : (
          <div className="lg:col-span-1 flex items-start justify-end">
            <button
              onClick={() => setRightOpen(true)}
              className="p-2 rounded-xl bg-[#060B10] border border-[#2A5C8A] text-xs font-mono-jarvis text-[#55C7FF]"
              title="Expand Inspector"
            >
              INSPECT +
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
