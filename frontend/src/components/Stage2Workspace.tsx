import React, { useEffect, useRef, useState } from 'react';
import { GeneratedArtifact } from './GeneratedArtifact';
import { AgentState, AppCategory, Artifact, TimelineEntry } from '../types';
import { generateWebApp, markVerified } from '../utils/api';
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
  X,
  ExternalLink,
  RotateCw,
  Download,
  FileCode2,
} from 'lucide-react';
import workstationBg from '../assets/images/bg_c.png';

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
  WORKING: 'JARVIS is generating…',
  ACTION_REQUIRED: 'Interact with the preview to verify',
  BLOCKED: 'JARVIS is blocked',
  ARTIFACT_READY: 'Verified artifact ready',
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
  const [viewMode, setViewMode] = useState<'preview' | 'edit' | 'inspect'>('preview');
  const [sourceDraft, setSourceDraft] = useState('');
  const [isFullScreen, setIsFullScreen] = useState(false);
  const [agentState, setAgentState] = useState<AgentState>('WAITING');
  const [artifact, setArtifact] = useState<Artifact | null>(null);
  const [inputVal, setInputVal] = useState('');
  const [notice, setNotice] = useState<string | null>(null);
  const [streamedChars, setStreamedChars] = useState(0);

  const [objective, setObjective] = useState(initialPrompt);
  const [editingObjective, setEditingObjective] = useState(false);
  const [objectiveDraft, setObjectiveDraft] = useState(initialPrompt);

  const [timeline, setTimeline] = useState<TimelineEntry[]>([]);

  const scrollRef = useRef<HTMLDivElement | null>(null);
  const seq = useRef(0);
  const abortRef = useRef<AbortController | null>(null);
  const sessionRef = useRef<string | undefined>(undefined);
  const startedRef = useRef(false);

  const nextId = (prefix: string) => `${prefix}-${(seq.current += 1)}`;
  const isBusy = agentState === 'WORKING';
  const canEditArtifact = artifact?.status === 'verified';

  const scrollToBottom = () => {
    requestAnimationFrame(() => {
      scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    });
  };

  const pushTimeline = (entries: TimelineEntry[]) => {
    setTimeline((prev) => [...prev, ...entries]);
    scrollToBottom();
  };

  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(null), 3400);
    return () => clearTimeout(t);
  }, [notice]);

  // ---- generation --------------------------------------------------------
  const runGeneration = (effectiveObjective: string, displayText: string, isInitial = false) => {
    const ts = now();
    setStreamedChars(0);
    setViewMode('preview');
    setAgentState('WORKING');

    const genArtifact: Artifact = {
      id: nextId('art'),
      name: 'Web App',
      revision: 'REV',
      status: 'generating',
      createdAt: ts,
    };
    setArtifact(genArtifact);

    const runEventId = nextId('e');
    pushTimeline([
      ...(isInitial ? [] : [{ id: nextId('u'), kind: 'user' as const, text: displayText, timestamp: ts }]),
      {
        id: runEventId,
        kind: 'event',
        text: 'Generating web app',
        detail: 'JARVIS is writing a self-contained single-page web app.',
        status: 'running',
        timestamp: ts,
      },
    ]);

    let received = 0;
    abortRef.current = generateWebApp(
      { objective: effectiveObjective, app_type: 'web', session_id: sessionRef.current },
      {
        onStart: (d) => {
          sessionRef.current = d.session_id;
        },
        onDelta: (content) => {
          received += content.length;
          setStreamedChars(received);
        },
        onDone: (d) => {
          setArtifact({
            id: d.id,
            name: 'Web App',
            revision: 'REV',
            status: 'unverified',
            createdAt: now(),
            source: d.source,
            evidence: d.evidence,
          });
          setAgentState('ACTION_REQUIRED');
          setTimeline((prev) =>
            prev.map((en) =>
              en.id === runEventId
                ? {
                    ...en,
                    text: 'Web app generated',
                    detail: 'Rendered in the sandbox. Interact with the preview to verify it.',
                    evidence: d.evidence,
                    status: 'completed',
                  }
                : en
            )
          );
          scrollToBottom();
        },
        onError: (d) => {
          setArtifact({
            id: nextId('art'),
            name: 'Web App',
            revision: 'REV',
            status: 'failed',
            createdAt: now(),
            failureReason: d.message,
            evidence: d.evidence,
          });
          setAgentState('BLOCKED');
          setTimeline((prev) =>
            prev.map((en) =>
              en.id === runEventId
                ? {
                    ...en,
                    text: 'Generation failed',
                    detail: d.message,
                    evidence: d.evidence,
                    status: 'failed',
                  }
                : en
            )
          );
          scrollToBottom();
        },
      }
    );
  };

  const stopGeneration = () => {
    abortRef.current?.abort();
    abortRef.current = null;
    setArtifact(null);
    setAgentState('WAITING');
    setStreamedChars(0);
    pushTimeline([
      {
        id: nextId('e'),
        kind: 'event',
        text: 'Generation stopped',
        detail: 'You stopped the run before it finished.',
        status: 'blocked',
        timestamp: now(),
      },
    ]);
  };

  const onVerified = () => {
    setArtifact((prev) => (prev ? { ...prev, status: 'verified' } : prev));
    setAgentState('ARTIFACT_READY');
    if (artifact?.id) markVerified(artifact.id);
    pushTimeline([
      {
        id: nextId('e'),
        kind: 'event',
        text: 'Artifact verified',
        detail: 'Real render and interaction were observed inside the sandbox.',
        evidence: 'render=confirmed interaction=confirmed',
        status: 'completed',
        timestamp: now(),
      },
    ]);
  };

  // ---- initial run (once) ------------------------------------------------
  useEffect(() => {
    if (startedRef.current) return;
    startedRef.current = true;

    if (!initialPrompt.trim()) {
      setAgentState('WAITING');
      return;
    }

    const ts = now();
    setTimeline([{ id: nextId('u'), kind: 'user', text: initialPrompt, timestamp: ts }]);

    if (appType === 'mobile' || appType === 'ai') {
      setAgentState('BLOCKED');
      pushTimeline([
        {
          id: nextId('e'),
          kind: 'event',
          text: `${APP_TYPE_LABEL[appType]} is not available yet`,
          detail:
            'This milestone builds Web Apps only. Mobile App and AI Model generation arrive in a later milestone.',
          evidence: `requested=${appType} supported=web`,
          status: 'blocked',
          timestamp: ts,
        },
      ]);
      return;
    }

    runGeneration(initialPrompt, initialPrompt, true);
  }, []);

  // ---- composer / objective ---------------------------------------------
  const send = () => {
    const text = inputVal.trim();
    if (!text || isBusy) return;
    setInputVal('');
    const effective = objective
      ? `${objective}\n\nApply this change to the web app: ${text}`
      : text;
    runGeneration(effective, text);
  };

  const saveObjective = () => {
    const next = objectiveDraft.trim();
    setEditingObjective(false);
    if (!next || next === objective) return;
    setObjective(next);
    sessionRef.current = undefined; // fresh intent -> fresh session
    runGeneration(next, `Objective updated: ${next}`);
  };

  const fileSlug = () =>
    (objective || 'jarvis-app')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
      .slice(0, 40) || 'jarvis-app';

  const downloadSource = (extension: string) => {
    if (!artifact?.source) return;
    const blob = new Blob([artifact.source], { type: 'text/html;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${fileSlug()}${extension}`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const openPopout = () => {
    if (!artifact?.source) return;
    const w = window.open('', '_blank');
    if (!w) {
      setNotice('Pop-out was blocked by the browser');
      return;
    }
    w.document.write(artifact.source);
    w.document.close();
  };

  const rebuild = () => {
    if (isBusy || !objective.trim()) return;
    runGeneration(objective, `Rebuild: ${objective}`);
  };

  /** Compact segmented control (Preview / Edit / Inspect). */
  const segButton = (
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
      className={`flex items-center gap-1.5 px-3 h-7 rounded-md text-[12px] transition-colors disabled:opacity-35 disabled:cursor-not-allowed ${
        active ? 'text-white bg-[rgba(47,124,255,0.85)]' : 'text-[#9FB0C6] hover:text-[#EAF2FF] hover:bg-[rgba(47,124,255,0.16)]'
      }`}
      data-testid={testId}
    >
      <Icon className="w-3.5 h-3.5" />
      <span>{label}</span>
    </button>
  );

  /** Compact icon action in the preview toolbar. */
  const iconAction = (
    title: string,
    Icon: React.ComponentType<{ className?: string }>,
    onClick: () => void,
    testId: string,
    disabled = false
  ) => (
    <button
      type="button"
      title={title}
      aria-label={title}
      onClick={onClick}
      disabled={disabled}
      className="w-7 h-7 rounded-md flex items-center justify-center text-[#9FB0C6] hover:text-[#EAF2FF] hover:bg-[rgba(47,124,255,0.16)] transition-colors disabled:opacity-35 disabled:cursor-not-allowed"
      data-testid={testId}
    >
      <Icon className="w-3.5 h-3.5" />
    </button>
  );

  /** Compact text action (downloads). */
  const textAction = (
    label: string,
    Icon: React.ComponentType<{ className?: string }>,
    onClick: () => void,
    testId: string,
    disabled = false
  ) => (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="flex items-center gap-1.5 px-2.5 h-7 rounded-md text-[12px] text-[#9FB0C6] hover:text-[#EAF2FF] hover:bg-[rgba(47,124,255,0.16)] transition-colors disabled:opacity-35 disabled:cursor-not-allowed"
      data-testid={testId}
    >
      <Icon className="w-3.5 h-3.5" />
      <span>{label}</span>
    </button>
  );


  return (
    <div className="relative w-screen h-screen overflow-hidden text-[#F5F8FF] flex flex-col font-sans" data-testid="workstation-screen">
      {/* Clean background artwork foundation — never edited, never covered by chrome. */}
      <img
        src={workstationBg}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full object-cover select-none pointer-events-none"
      />

      {/* TOP NAVIGATION */}
      <nav className="relative z-10 h-14 shrink-0 flex items-center justify-center border-b border-[rgba(95,160,255,0.16)] bg-[rgba(4,10,20,0.62)] backdrop-blur-xl">
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
                  setNotice(`${label} is not available in this milestone`);
                }}
                className={`relative flex items-center gap-2 px-4 py-2 text-sm transition-colors ${
                  active ? 'text-[#F5F8FF]' : 'text-[#8EA1BA] hover:text-[#DCE5F2]'
                }`}
                data-testid={`nav-${id}`}
              >
                <Icon className={`w-4 h-4 ${active ? 'text-[#5C9DFF]' : ''}`} />
                <span>{label}</span>
                {active && (
                  <span className="absolute left-3 right-3 -bottom-[11px] h-[2px] bg-[#2F7CFF] shadow-[0_0_10px_rgba(47,124,255,0.75)]" />
                )}
              </button>
            );
          })}
        </div>

        <button
          type="button"
          onClick={() => setNotice('Search is not available in this milestone')}
          className="absolute right-6 text-[#8EA1BA] hover:text-[#F5F8FF] transition-colors"
          aria-label="Search"
          data-testid="nav-search"
        >
          <Search className="w-[18px] h-[18px]" />
        </button>
      </nav>

      <div className="relative z-10 flex-1 min-h-0 flex gap-4 p-4">
        {/* LEFT — one open console surface */}
        {!isFullScreen && (
          <section
            className="w-[35%] min-w-[360px] flex flex-col rounded-2xl overflow-hidden border border-[rgba(95,160,255,0.2)] bg-[rgba(5,12,24,0.72)] backdrop-blur-2xl"
            data-testid="builder-left-pane"
          >
            <div className="px-6 pt-6 pb-4">
              <div className="flex items-center justify-between">
                <div className="text-xs tracking-wide text-[#8EA1BA]">Objective</div>
                {!editingObjective && (
                  <button
                    type="button"
                    onClick={() => {
                      setObjectiveDraft(objective);
                      setEditingObjective(true);
                    }}
                    disabled={isBusy}
                    className="flex items-center gap-1.5 text-xs text-[#7FB4FF] hover:text-[#A9CCFF] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    data-testid="edit-objective-button"
                  >
                    <PenLine className="w-3.5 h-3.5" />
                    Edit objective
                  </button>
                )}
              </div>

              {editingObjective ? (
                <div className="mt-2" data-testid="objective-editor">
                  <textarea
                    value={objectiveDraft}
                    onChange={(e) => setObjectiveDraft(e.target.value)}
                    rows={3}
                    className="w-full bg-[#050A14] rounded-lg px-3 py-2 text-sm text-[#F5F8FF] outline-none border border-[rgba(47,124,255,0.35)] focus:border-[rgba(47,124,255,0.7)] resize-none"
                    autoFocus
                  />
                  <div className="mt-2 flex items-center gap-2">
                    <button
                      type="button"
                      onClick={saveObjective}
                      className="px-3 py-1.5 rounded-lg text-xs text-white"
                      style={{ background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)' }}
                      data-testid="objective-save-button"
                    >
                      Save & regenerate
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingObjective(false)}
                      className="px-3 py-1.5 rounded-lg text-xs text-[#8EA1BA] border border-[rgba(47,124,255,0.24)]"
                      data-testid="objective-cancel-button"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <h1
                  className="mt-1.5 text-base md:text-lg text-[#F5F8FF] leading-relaxed"
                  data-testid="builder-objective-text"
                >
                  {objective || 'No objective set yet.'}
                </h1>
              )}

              <div className="mt-2 text-sm text-[#8EA1BA] flex items-center gap-2" data-testid="builder-agent-line">
                <span>
                  {appType ? `${APP_TYPE_LABEL[appType]} · ` : 'Web App · '}
                  {AGENT_LINE[agentState]}
                </span>
                {isBusy && <Loader2 className="w-3.5 h-3.5 text-[#5C9DFF] animate-spin" />}
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
                        <span className="mt-1 w-5 h-5 shrink-0 rounded-full border border-[#2F7CFF]/70 text-[#7FB4FF] text-[10px] flex items-center justify-center">
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
                        {entry.status === 'running' && <Loader2 className="w-4 h-4 text-[#5C9DFF] animate-spin" />}
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
                  background: 'linear-gradient(180deg, rgba(12, 24, 46, 0.9) 0%, rgba(8, 16, 32, 0.9) 100%)',
                  border: '1px solid rgba(47, 124, 255, 0.3)',
                  boxShadow: '0 0 20px rgba(47, 124, 255, 0.08), inset 0 1px 0 rgba(255,255,255,0.05)',
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
                  disabled={isBusy}
                  placeholder={isBusy ? 'JARVIS is generating…' : 'Describe a change to the web app…'}
                  className="w-full bg-transparent resize-none outline-none text-sm text-[#F5F8FF] placeholder-[#66738A] leading-relaxed disabled:opacity-50"
                  data-testid="composer-input"
                />

                <div className="mt-2 flex items-center justify-between">
                  <button
                    type="button"
                    onClick={() => setNotice('Attachments are not available in this milestone')}
                    className="w-9 h-9 rounded-lg bg-[rgba(12,24,46,0.85)] border border-[rgba(47,124,255,0.24)] text-[#8EA1BA] hover:text-[#F5F8FF] hover:border-[rgba(47,124,255,0.55)] flex items-center justify-center transition-all"
                    aria-label="Attach a file"
                    data-testid="composer-attach-button"
                  >
                    <Paperclip className="w-4 h-4" />
                  </button>

                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setNotice('Voice is not available in this milestone')}
                      className="w-9 h-9 rounded-lg bg-[rgba(12,24,46,0.85)] border border-[rgba(47,124,255,0.24)] text-[#8EA1BA] hover:text-[#7FB4FF] hover:border-[rgba(47,124,255,0.55)] flex items-center justify-center transition-all"
                      aria-label="Voice input"
                      data-testid="composer-mic-button"
                    >
                      <Mic className="w-4 h-4" />
                    </button>

                    {isBusy ? (
                      <button
                        type="button"
                        onClick={stopGeneration}
                        className="w-10 h-9 rounded-lg text-white flex items-center justify-center transition-all"
                        style={{
                          background: 'linear-gradient(180deg, #FF3B5C 0%, #C81E3C 100%)',
                          border: '1px solid rgba(255, 120, 140, 0.85)',
                          boxShadow: '0 0 16px rgba(255, 59, 92, 0.3)',
                        }}
                        aria-label="Stop generation"
                        data-testid="composer-stop-button"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    ) : (
                      <button
                        type="button"
                        onClick={send}
                        disabled={!inputVal.trim()}
                        className="w-10 h-9 rounded-lg text-white flex items-center justify-center transition-all disabled:opacity-35 disabled:cursor-not-allowed"
                        style={{
                          background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                          border: '1px solid rgba(120, 178, 255, 0.85)',
                          boxShadow: '0 0 16px rgba(47, 124, 255, 0.3), inset 0 1px 0 rgba(255,255,255,0.18)',
                        }}
                        aria-label="Send"
                        data-testid="composer-send-button"
                      >
                        <SendHorizontal className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </section>
        )}

        {/* RIGHT — preview workspace: the artwork stays visible behind it. */}
        <section className="flex-1 min-w-0 flex flex-col" data-testid="workstation-preview-pane">
          {/* Compact preview toolbar */}
          <div
            className="flex items-center gap-1 px-2 h-10 rounded-lg mb-2 overflow-x-auto"
            style={{
              background: 'rgba(5,12,24,0.42)',
              border: '1px solid rgba(95,160,255,0.16)',
              backdropFilter: 'blur(10px)',
              WebkitBackdropFilter: 'blur(10px)',
            }}
            data-testid="preview-toolbar"
          >
            {segButton('Preview', Eye, viewMode === 'preview', () => setViewMode('preview'), 'view-preview-button')}
            {segButton(
              'Edit',
              PenLine,
              viewMode === 'edit',
              () => {
                setSourceDraft(artifact?.source ?? '');
                setViewMode('edit');
              },
              'view-edit-button',
              !artifact?.source
            )}
            {segButton('Inspect', Code2, viewMode === 'inspect', () => setViewMode('inspect'), 'view-inspect-button')}

            <span className="w-px h-4 mx-1 bg-[rgba(95,160,255,0.22)]" />

            {iconAction('Open in new tab', ExternalLink, openPopout, 'preview-popout-button', !artifact?.source)}
            {iconAction('Rebuild', RotateCw, rebuild, 'preview-rebuild-button', isBusy || !objective.trim())}

            <span className="w-px h-4 mx-1 bg-[rgba(95,160,255,0.22)]" />

            {textAction('Download App', Download, () => downloadSource('.html'), 'download-app-button', !artifact?.source)}
            {textAction('Download Code', FileCode2, () => downloadSource('.source.html'), 'download-code-button', !artifact?.source)}

            <span className="w-px h-4 mx-1 bg-[rgba(95,160,255,0.22)]" />

            {textAction(
              isFullScreen ? 'Exit Full Screen' : 'Full Screen',
              isFullScreen ? Minimize2 : Maximize2,
              () => setIsFullScreen((v) => !v),
              'preview-fullscreen-button'
            )}

            <span className="ml-auto pl-3 pr-1 text-[11px] text-[#7C8DA6] font-mono-jarvis shrink-0" data-testid="artifact-status-chip">
              {artifact ? `status: ${artifact.status}` : 'status: none'}
            </span>
          </div>

          <div className="flex-1 min-h-0 overflow-hidden rounded-xl">
            {viewMode === 'preview' && (
              <GeneratedArtifact artifact={artifact} streamedChars={streamedChars} onVerified={onVerified} />
            )}

            {viewMode === 'edit' && (
              <div
                className="w-full h-full flex flex-col p-3 rounded-xl"
                style={{ background: 'rgba(4,10,20,0.6)', border: '1px solid rgba(95,160,255,0.16)' }}
                data-testid="edit-panel"
              >
                <textarea
                  value={sourceDraft}
                  onChange={(e) => setSourceDraft(e.target.value)}
                  spellCheck={false}
                  className="flex-1 min-h-0 w-full rounded-lg bg-[rgba(3,8,16,0.72)] border border-[rgba(95,160,255,0.22)] px-3 py-3 text-[12px] font-mono-jarvis leading-relaxed text-[#9FC6FF] outline-none focus:border-[rgba(95,160,255,0.5)] resize-none"
                  data-testid="edit-source-textarea"
                />
                <div className="mt-2.5 flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setArtifact((prev) =>
                        prev ? { ...prev, source: sourceDraft, status: 'unverified' } : prev
                      );
                      setAgentState('ACTION_REQUIRED');
                      setViewMode('preview');
                      pushTimeline([
                        {
                          id: nextId('e'),
                          kind: 'event',
                          text: 'Source edited manually',
                          detail: 'Your edited source was re-rendered in the sandbox and needs re-verification.',
                          evidence: `chars=${sourceDraft.length}`,
                          status: 'completed',
                          timestamp: now(),
                        },
                      ]);
                    }}
                    className="px-3.5 h-8 rounded-md text-[12px] text-white"
                    style={{ background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)' }}
                    data-testid="edit-apply-button"
                  >
                    Apply & re-render
                  </button>
                  <button
                    type="button"
                    onClick={() => setViewMode('preview')}
                    className="px-3.5 h-8 rounded-md text-[12px] text-[#9FB0C6] border border-[rgba(95,160,255,0.22)]"
                    data-testid="edit-cancel-button"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            )}

            {viewMode === 'inspect' && (
              <div
                className="w-full h-full p-4 overflow-y-auto rounded-xl"
                style={{ background: 'rgba(4,10,20,0.6)', border: '1px solid rgba(95,160,255,0.16)' }}
                data-testid="inspect-panel"
              >
                {artifact?.source ? (
                  <pre className="text-[12px] font-mono-jarvis leading-relaxed text-[#7FB4FF] whitespace-pre-wrap">
                    {artifact.source}
                  </pre>
                ) : (
                  <div className="h-full flex items-center justify-center text-center">
                    <div className="space-y-1.5 max-w-sm">
                      <div className="text-sm text-[#9FB0C6]">No source to inspect</div>
                      <p className="text-[13px] text-[#7C8DA6] leading-relaxed">
                        Source appears here once a run produces an artifact.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
};
