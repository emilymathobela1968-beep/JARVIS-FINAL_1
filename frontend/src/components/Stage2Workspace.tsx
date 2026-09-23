import React, { useEffect, useRef, useState } from 'react';
import { GeneratedArtifact } from './GeneratedArtifact';
import { AgentState, AppCategory, Artifact, TimelineEntry } from '../types';
import {
  Home,
  Monitor,
  Code2,
  PlaySquare,
  Box,
  Activity,
  Settings,
  Search,
  Eye,
  Maximize2,
  Minimize2,
  PenLine,
  Paperclip,
  Mic,
  SendHorizontal,
  Ban,
  Loader2,
  Check,
  AlertTriangle,
} from 'lucide-react';

interface Stage2WorkspaceProps {
  initialPrompt: string;
  appType: AppCategory | null;
  onReturnHome: () => void;
  onOpenIntake: () => void;
}

const NAV = [
  { id: 'home', label: 'Home', icon: Home },
  { id: 'computer', label: 'Computer', icon: Monitor },
  { id: 'developer', label: 'Developer', icon: Code2 },
  { id: 'media', label: 'Media', icon: PlaySquare },
  { id: 'builder', label: 'Builder', icon: Box },
  { id: 'barehands', label: 'Barehands', icon: Activity },
  { id: 'system', label: 'System', icon: Settings },
] as const;

const AGENT_LINE: Record<AgentState, string> = {
  WAITING: 'JARVIS is waiting',
  WORKING: 'JARVIS is analysing…',
  ACTION_REQUIRED: 'JARVIS needs your input',
  BLOCKED: 'JARVIS is blocked — no execution runtime connected',
  ARTIFACT_READY: 'Artifact ready — preview available',
};

const APP_TYPE_LABEL: Record<AppCategory, string> = {
  web: 'Web App',
  mobile: 'Mobile App',
  ai: 'AI Model',
};

const now = () => new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

export const Stage2Workspace: React.FC<Stage2WorkspaceProps> = ({
  initialPrompt,
  appType,
  onReturnHome,
  onOpenIntake,
}) => {
  const [viewMode, setViewMode] = useState<'preview' | 'inspect' | 'edit'>('preview');
  const [isFullScreen, setIsFullScreen] = useState(false);
  const [agentState, setAgentState] = useState<AgentState>(initialPrompt ? 'BLOCKED' : 'WAITING');
  const [artifact, setArtifact] = useState<Artifact | null>(null);
  const [inputVal, setInputVal] = useState('');
  const [notice, setNotice] = useState<string | null>(null);

  const [timeline, setTimeline] = useState<TimelineEntry[]>(
    initialPrompt
      ? [
          { id: 'u-0', kind: 'user', text: initialPrompt, timestamp: now() },
          {
            id: 'e-0',
            kind: 'event',
            text: 'Directive received',
            detail: 'Recorded locally. No execution runtime is connected, so nothing was dispatched.',
            evidence: 'runtime=none dispatch=skipped',
            status: 'blocked',
            timestamp: now(),
          },
        ]
      : []
  );

  const scrollRef = useRef<HTMLDivElement | null>(null);
  const seq = useRef(0);
  const nextId = (prefix: string) => `${prefix}-${(seq.current += 1)}`;
  const hasArtifact = artifact !== null;

  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(null), 3200);
    return () => clearTimeout(t);
  }, [notice]);

  const send = () => {
    const text = inputVal.trim();
    if (!text) return;
    const ts = now();
    setInputVal('');
    setAgentState('BLOCKED');
    setTimeline((prev) => [
      ...prev,
      { id: nextId('u'), kind: 'user', text, timestamp: ts },
      {
        id: nextId('e'),
        kind: 'event',
        text: 'Directive received',
        detail: 'Recorded locally. No execution runtime is connected, so nothing was dispatched.',
        evidence: 'runtime=none dispatch=skipped',
        status: 'blocked',
        timestamp: ts,
      },
    ]);
    requestAnimationFrame(() => {
      scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    });
  };

  const toolButton = (
    label: string,
    Icon: React.ComponentType<{ className?: string }>,
    active: boolean,
    onClick: () => void,
    testId: string,
    disabled = false
  ) => (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`flex items-center gap-2 px-3.5 py-2 rounded-lg text-sm transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
        active
          ? 'text-[#F5F8FF] bg-[#2F7CFF]/90 border border-[#149BFF]'
          : 'text-[#8EA1BA] bg-[rgba(7,17,31,0.72)] border border-[rgba(47,124,255,0.22)] enabled:hover:text-[#F5F8FF] enabled:hover:border-[rgba(47,124,255,0.5)]'
      }`}
      data-testid={testId}
    >
      <Icon className="w-4 h-4" />
      <span>{label}</span>
    </button>
  );

  return (
    <div className="w-screen h-screen overflow-hidden bg-[#030508] text-[#F5F8FF] flex flex-col font-sans">
      {/* TOP NAVIGATION */}
      <nav className="h-14 shrink-0 flex items-center justify-center relative border-b border-[rgba(47,124,255,0.14)]">
        <div className="flex items-center gap-1">
          {NAV.map(({ id, label, icon: Icon }) => {
            const active = id === 'builder';
            return (
              <button
                key={id}
                type="button"
                onClick={() => {
                  if (id === 'home') return onReturnHome();
                  if (id === 'builder') return onOpenIntake();
                  setNotice(`${label} is not connected yet`);
                }}
                className={`relative flex items-center gap-2 px-4 py-2 text-sm transition-colors ${
                  active ? 'text-[#F5F8FF]' : 'text-[#8EA1BA] hover:text-[#DCE5F2]'
                }`}
                data-testid={`nav-${id}`}
              >
                <Icon className={`w-4 h-4 ${active ? 'text-[#25C8FF]' : ''}`} />
                <span>{label}</span>
                {active && (
                  <span className="absolute left-3 right-3 -bottom-[11px] h-[2px] bg-[#149BFF] shadow-[0_0_10px_#149BFF]" />
                )}
              </button>
            );
          })}
        </div>

        <button
          type="button"
          onClick={() => setNotice('Search is not connected yet')}
          className="absolute right-6 text-[#8EA1BA] hover:text-[#F5F8FF] transition-colors"
          aria-label="Search"
          data-testid="nav-search"
        >
          <Search className="w-[18px] h-[18px]" />
        </button>
      </nav>

      <div className="flex-1 min-h-0 flex gap-4 p-4">
        {/* LEFT — one open console surface */}
        {!isFullScreen && (
          <section
            className="w-[36%] min-w-[380px] flex flex-col rounded-2xl border border-[rgba(47,124,255,0.2)] bg-[rgba(5,9,21,0.75)] overflow-hidden shadow-[0_0_30px_rgba(20,155,255,0.07)]"
            data-testid="builder-left-pane"
          >
            <div className="px-6 pt-6 pb-4">
              <div className="text-xs tracking-wide text-[#8EA1BA]">Objective</div>
              <h1 className="mt-1.5 text-base md:text-lg text-[#F5F8FF] leading-relaxed" data-testid="builder-objective-text">
                {initialPrompt || 'No objective set yet.'}
              </h1>
              <div className="mt-2 text-sm text-[#8EA1BA]" data-testid="builder-agent-line">
                {appType ? `${APP_TYPE_LABEL[appType]} · ` : ''}
                {AGENT_LINE[agentState]}
              </div>
            </div>

            <div ref={scrollRef} className="flex-1 min-h-0 overflow-y-auto px-6 pb-4 space-y-6" data-testid="builder-timeline">
              {timeline.length === 0 ? (
                <p className="text-sm text-[#66738A] leading-relaxed" data-testid="timeline-empty">
                  Nothing has run yet. Send a directive to begin.
                </p>
              ) : (
                timeline.map((entry) => {
                  if (entry.kind === 'user') {
                    return (
                      <div key={entry.id} className="pl-3 border-l-2 border-[#2F7CFF]/70" data-testid="timeline-user">
                        <div className="text-xs text-[#8EA1BA] mb-1">
                          You <span className="font-mono-jarvis ml-2 text-[#5C6B81]">{entry.timestamp}</span>
                        </div>
                        <p className="text-sm text-[#F5F8FF] leading-relaxed">{entry.text}</p>
                      </div>
                    );
                  }

                  if (entry.kind === 'agent') {
                    return (
                      <div key={entry.id} className="flex gap-3" data-testid="timeline-agent">
                        <span className="mt-1 w-5 h-5 shrink-0 rounded-full border border-[#149BFF]/70 text-[#25C8FF] text-[10px] flex items-center justify-center">
                          J
                        </span>
                        <div>
                          <p className="text-sm text-[#DCE5F2] leading-relaxed">{entry.text}</p>
                          {entry.detail && (
                            <p className="mt-1 text-sm text-[#8EA1BA] leading-relaxed">{entry.detail}</p>
                          )}
                        </div>
                      </div>
                    );
                  }

                  return (
                    <div key={entry.id} className="flex gap-3" data-testid="timeline-event">
                      <span className="mt-1 shrink-0">
                        {entry.status === 'completed' && <Check className="w-4 h-4 text-[#28D7A1]" />}
                        {entry.status === 'running' && <Loader2 className="w-4 h-4 text-[#25C8FF] animate-spin" />}
                        {entry.status === 'blocked' && <Ban className="w-4 h-4 text-[#FF2346]" />}
                        {entry.status === 'failed' && <AlertTriangle className="w-4 h-4 text-[#FF2346]" />}
                      </span>
                      <div className="min-w-0">
                        <div className="text-sm text-[#DCE5F2]">
                          {entry.text}
                          <span className="font-mono-jarvis ml-2 text-xs text-[#5C6B81]">{entry.timestamp}</span>
                        </div>
                        {entry.detail && (
                          <p className="mt-1 text-sm text-[#8EA1BA] leading-relaxed">{entry.detail}</p>
                        )}
                        {entry.evidence && (
                          <p className="mt-1 text-xs font-mono-jarvis text-[#5C7C9E] break-words">{entry.evidence}</p>
                        )}
                      </div>
                    </div>
                  );
                })
              )}
            </div>

            {/* COMPOSER */}
            <div className="px-4 pb-4">
              {notice && (
                <div className="mb-2 px-2 text-xs text-[#8EA1BA]" data-testid="workspace-notice">
                  {notice}
                </div>
              )}

              <div
                className="rounded-2xl px-4 pt-4 pb-3"
                style={{
                  background: 'rgba(8, 20, 36, 0.82)',
                  border: '1px solid rgba(47, 124, 255, 0.35)',
                  boxShadow: '0 0 24px rgba(20, 155, 255, 0.10)',
                  backdropFilter: 'blur(16px)',
                  WebkitBackdropFilter: 'blur(16px)',
                }}
              >
                <textarea
                  value={inputVal}
                  onChange={(e) => setInputVal(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      send();
                    }
                  }}
                  rows={2}
                  placeholder="Ask me anything…"
                  className="w-full bg-transparent resize-none outline-none text-sm text-[#F5F8FF] placeholder-[#66738A] leading-relaxed"
                  data-testid="composer-input"
                />

                <div className="mt-2 flex items-center justify-between">
                  <button
                    type="button"
                    onClick={() => setNotice('Attachments are not configured yet')}
                    className="w-9 h-9 rounded-lg border border-[rgba(47,124,255,0.22)] text-[#8EA1BA] hover:text-[#F5F8FF] hover:border-[rgba(47,124,255,0.5)] flex items-center justify-center transition-colors"
                    aria-label="Attach a file"
                    data-testid="composer-attach-button"
                  >
                    <Paperclip className="w-4 h-4" />
                  </button>

                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setNotice('Voice not connected — realtime voice runtime pending integration')}
                      className="w-9 h-9 rounded-lg border border-[rgba(47,124,255,0.22)] text-[#8EA1BA] hover:text-[#25C8FF] hover:border-[rgba(47,124,255,0.5)] flex items-center justify-center transition-colors"
                      aria-label="Voice input"
                      data-testid="composer-mic-button"
                    >
                      <Mic className="w-4 h-4" />
                    </button>

                    <button
                      type="button"
                      onClick={send}
                      disabled={!inputVal.trim()}
                      className="w-10 h-9 rounded-lg bg-[#2F7CFF] enabled:hover:bg-[#149BFF] text-white flex items-center justify-center transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                      aria-label="Send"
                      data-testid="composer-send-button"
                    >
                      <SendHorizontal className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </section>
        )}

        {/* RIGHT — preview workspace */}
        <section className="flex-1 min-w-0 flex flex-col rounded-2xl border border-[rgba(47,124,255,0.2)] bg-[rgba(5,9,21,0.6)] overflow-hidden shadow-[0_0_30px_rgba(20,155,255,0.07)]">
          <div className="flex items-center justify-between gap-3 px-4 py-3">
            <div className="flex items-center gap-2">
              {toolButton('Preview', Eye, viewMode === 'preview', () => setViewMode('preview'), 'view-preview-button')}
              {toolButton('Inspect', Code2, viewMode === 'inspect', () => setViewMode('inspect'), 'view-inspect-button')}
              {toolButton(
                isFullScreen ? 'Exit Full Screen' : 'Full Screen',
                isFullScreen ? Minimize2 : Maximize2,
                isFullScreen,
                () => setIsFullScreen((v) => !v),
                'preview-fullscreen-button'
              )}
            </div>

            {toolButton(
              'Edit',
              PenLine,
              viewMode === 'edit',
              () => {
                if (!hasArtifact) {
                  setNotice('Nothing to edit until a verified artifact exists');
                  return;
                }
                setViewMode(viewMode === 'edit' ? 'preview' : 'edit');
              },
              'view-edit-button',
              !hasArtifact
            )}
          </div>

          <div className="flex-1 min-h-0 m-4 mt-0 rounded-xl border border-[rgba(47,124,255,0.18)] bg-[#030508] overflow-hidden">
            {viewMode === 'preview' && <GeneratedArtifact artifact={artifact} />}

            {viewMode === 'inspect' && (
              <div className="w-full h-full p-6 overflow-y-auto" data-testid="inspect-panel">
                {artifact?.source ? (
                  <pre className="text-[12px] font-mono-jarvis leading-relaxed text-[#25C8FF] whitespace-pre-wrap">
                    {artifact.source}
                  </pre>
                ) : (
                  <div className="h-full flex items-center justify-center text-center">
                    <div className="space-y-2 max-w-sm">
                      <div className="text-sm text-[#8EA1BA]">No source to inspect</div>
                      <p className="text-sm text-[#66738A] leading-relaxed">
                        Source appears here once a real run produces an artifact.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            )}

            {viewMode === 'edit' && (
              <div className="w-full h-full flex items-center justify-center text-center p-6" data-testid="edit-panel">
                <div className="space-y-2 max-w-sm">
                  <div className="text-sm text-[#8EA1BA]">Nothing to edit</div>
                  <p className="text-sm text-[#66738A] leading-relaxed">
                    Artifact parameters become editable after a verified render.
                  </p>
                </div>
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
};
