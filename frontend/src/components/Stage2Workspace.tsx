import React, { useState, useRef, useEffect } from 'react';
import { GeneratedArtifact } from './GeneratedArtifact';
import { soundFX } from '../utils/audio';
import { jarvisVoice } from '../utils/voice';
import {
  Artifact,
  AgentState,
  ConversationMessage,
  ExecutionEvent,
} from '../types';
import {
  Home,
  Plus,
  LayoutGrid,
  Monitor,
  Settings,
  Mic,
  Send,
  Paperclip,
  Search,
  Eye,
  Code,
  Maximize2,
  Minimize2,
  Edit3,
  RefreshCw,
  Play,
  Share2,
  MoreHorizontal,
  Gauge,
  Check,
  Loader2,
  CircleDashed,
  AlertTriangle,
  Ban,
} from 'lucide-react';

interface Stage2WorkspaceProps {
  initialPrompt: string;
  onReturnToStage1: () => void;
  onNewBuild: () => void;
}

const AGENT_STATE_UI: Record<AgentState, { label: string; hint: string; tone: string; dot: string }> = {
  WAITING: {
    label: 'Agent is waiting...',
    hint: 'Waiting for your next message',
    tone: 'text-[#8B98AA]',
    dot: 'bg-[#4E5C70]',
  },
  WORKING: {
    label: 'JARVIS working',
    hint: 'Analyzing directive...',
    tone: 'text-[#58D7FF]',
    dot: 'bg-[#39B8FF]',
  },
  ACTION_REQUIRED: {
    label: 'Action required',
    hint: 'JARVIS needs your input to continue',
    tone: 'text-[#F5B942]',
    dot: 'bg-[#F5B942]',
  },
  BLOCKED: {
    label: 'Blocked',
    hint: 'No execution runtime is connected',
    tone: 'text-[#FF5C7A]',
    dot: 'bg-[#FF294D]',
  },
  ARTIFACT_READY: {
    label: 'Artifact ready',
    hint: 'Preview available',
    tone: 'text-[#18CC8A]',
    dot: 'bg-[#18CC8A]',
  },
};

const now = () => new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });

export const Stage2Workspace: React.FC<Stage2WorkspaceProps> = ({
  initialPrompt,
  onReturnToStage1,
  onNewBuild,
}) => {
  const [viewMode, setViewMode] = useState<'preview' | 'inspect' | 'edit'>('preview');
  const [isFullScreen, setIsFullScreen] = useState(false);

  // Truthful runtime state. Nothing is generated until a real operation runs.
  const [agentState, setAgentState] = useState<AgentState>(initialPrompt ? 'BLOCKED' : 'WAITING');
  const [artifact, setArtifact] = useState<Artifact | null>(null);
  const [executionEvents, setExecutionEvents] = useState<ExecutionEvent[]>(
    initialPrompt
      ? [
          {
            id: 'evt-directive-0',
            title: 'Directive received',
            detail: 'Recorded locally. No execution runtime is connected, so nothing was dispatched.',
            timestamp: now(),
            status: 'blocked',
            evidence: 'runtime=none dispatch=skipped',
          },
        ]
      : []
  );

  const [inputVal, setInputVal] = useState('');
  const [isListening, setIsListening] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  // The only seeded message is the operator's real directive.
  const [conversation, setConversation] = useState<ConversationMessage[]>(
    initialPrompt
      ? [{ id: 'directive-0', sender: 'user', text: initialPrompt, timestamp: now() }]
      : []
  );

  const chatScrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(null), 2600);
    return () => clearTimeout(t);
  }, [notice]);

  const reportNotConfigured = (what: string) => {
    setNotice(`${what}: Not configured`);
  };

  const handleSubmitDirective = (textToSend?: string) => {
    const query = (textToSend ?? inputVal).trim();
    if (!query) return;

    const timestamp = now();
    setConversation((prev) => [
      ...prev,
      { id: `u-${Date.now()}`, sender: 'user', text: query, timestamp },
    ]);
    setInputVal('');

    // No execution runtime is wired yet. Record it truthfully instead of faking a build.
    setAgentState('BLOCKED');
    setExecutionEvents((prev) => [
      ...prev,
      {
        id: `evt-${Date.now()}`,
        title: 'Directive received',
        detail: 'Recorded locally. No execution runtime is connected, so nothing was dispatched.',
        timestamp,
        status: 'blocked',
        evidence: 'runtime=none dispatch=skipped',
      },
    ]);

    requestAnimationFrame(() => {
      chatScrollRef.current?.scrollTo({ top: chatScrollRef.current.scrollHeight, behavior: 'smooth' });
    });
  };

  const handleToggleVoice = () => {
    if (isListening) {
      soundFX.playPowerDown();
      jarvisVoice.stopListening();
      setIsListening(false);
      return;
    }
    soundFX.playIgnite();
    setIsListening(true);
    jarvisVoice.startListening(
      (transcript) => {
        setIsListening(false);
        handleSubmitDirective(transcript);
      },
      (listening) => setIsListening(listening)
    );
  };

  const stateUI = AGENT_STATE_UI[agentState];
  const hasArtifact = artifact !== null;

  const railButton = (
    label: string,
    Icon: React.ComponentType<{ className?: string }>,
    onClick?: () => void
  ) => (
    <button
      key={label}
      onClick={onClick}
      disabled={!onClick}
      className="w-10 h-10 rounded-2xl text-[#8B98AA] enabled:hover:text-[#F5F8FF] enabled:hover:bg-white/5 disabled:opacity-35 disabled:cursor-not-allowed flex items-center justify-center transition-all"
      title={onClick ? label : `${label} — Not configured`}
      data-testid={`rail-${label.toLowerCase().replace(/\s+/g, '-')}`}
    >
      <Icon className="w-5 h-5 stroke-[1.8]" />
    </button>
  );

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-[#030508] text-[#F5F8FF] flex select-none font-sans">
      {/* LEFT NAVIGATION RAIL */}
      <aside className="w-16 h-full bg-[#05070B] border-r border-white/5 flex flex-col items-center justify-between py-5 shrink-0 z-30">
        <div className="flex flex-col items-center gap-4">
          <button
            onClick={() => {
              soundFX.playBeep(640, 0.04);
              onReturnToStage1();
            }}
            className="w-10 h-10 rounded-2xl bg-[#0B1420] border border-[rgba(70,170,255,0.4)] text-[#58D7FF] flex items-center justify-center hover:bg-[#39B8FF]/15 hover:text-white transition-all active:scale-95 shadow-[0_0_15px_rgba(57,184,255,0.2)]"
            title="Return to JARVIS home"
            data-testid="rail-home-button"
          >
            <Home className="w-5 h-5 stroke-[1.8]" />
          </button>

          <button
            onClick={() => {
              soundFX.playAcknowledge();
              onNewBuild();
            }}
            className="w-10 h-10 rounded-2xl text-[#8B98AA] hover:text-[#F5F8FF] hover:bg-white/5 flex items-center justify-center transition-all active:scale-95"
            title="New directive"
            data-testid="rail-new-build-button"
          >
            <Plus className="w-5 h-5 stroke-[1.8]" />
          </button>

          {railButton('Telemetry', Gauge)}
          {railButton('Components', LayoutGrid)}
          {railButton('Displays', Monitor)}
          {railButton('Settings', Settings)}
        </div>

        <div className="w-9 h-9 rounded-full bg-[#08101A] border border-white/10 flex items-center justify-center text-xs font-semibold text-[#8B98AA]">
          JV
        </div>
      </aside>

      <div className="flex-1 h-full flex overflow-hidden">
        {/* LEFT PANE: objective, execution activity, conversation, composer */}
        {!isFullScreen && (
          <section
            className="w-full lg:w-[38%] h-full flex flex-col p-4 sm:p-5 border-r border-white/5 overflow-hidden z-20 shrink-0 bg-[#030508]/60"
            data-testid="builder-left-pane"
          >
            {/* Objective — the operator's real directive only */}
            <div className="rounded-3xl p-4 jarvis-glass shrink-0 mb-3 border border-[rgba(70,170,255,0.25)]">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold tracking-wide text-[#58D7FF]">Objective</span>
                {artifact && (
                  <span className="text-[11px] font-mono-jarvis text-[#8B98AA] bg-[#050B14] px-2 py-0.5 rounded border border-white/10">
                    {artifact.revision}
                  </span>
                )}
              </div>

              <div className="text-sm text-[#F5F8FF] leading-relaxed" data-testid="builder-objective-text">
                {initialPrompt || 'No objective set yet.'}
              </div>
            </div>

            {/* Agent state */}
            <div
              className="rounded-3xl p-4 jarvis-glass-soft shrink-0 mb-3 flex items-center gap-3"
              data-testid="agent-state-card"
            >
              <span className={`w-2.5 h-2.5 rounded-full ${stateUI.dot}`} />
              <div>
                <div className={`text-sm font-semibold ${stateUI.tone}`} data-testid="agent-state-label">
                  {stateUI.label}
                </div>
                <div className="text-xs text-[#8B98AA] mt-0.5" data-testid="agent-state-hint">
                  {stateUI.hint}
                </div>
              </div>
            </div>

            {/* Execution activity + conversation */}
            <div ref={chatScrollRef} className="flex-1 overflow-y-auto pr-1 space-y-4">
              <div className="space-y-2" data-testid="execution-activity">
                <div className="text-xs font-semibold text-[#8B98AA]">Execution activity</div>

                {executionEvents.length === 0 ? (
                  <div className="p-3 rounded-2xl bg-[#060D18]/70 border border-white/5 text-xs text-[#66738A] flex items-center gap-2">
                    <CircleDashed className="w-3.5 h-3.5" />
                    <span data-testid="execution-activity-empty">No execution activity yet</span>
                  </div>
                ) : (
                  executionEvents.map((evt) => (
                    <div key={evt.id} className="p-3 rounded-2xl bg-[#060D18]/80 border border-white/5">
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-sm font-medium text-[#F5F8FF] flex items-center gap-2">
                          {evt.status === 'completed' && <Check className="w-3.5 h-3.5 text-[#18CC8A]" />}
                          {evt.status === 'running' && <Loader2 className="w-3.5 h-3.5 text-[#58D7FF] animate-spin" />}
                          {evt.status === 'blocked' && <Ban className="w-3.5 h-3.5 text-[#FF5C7A]" />}
                          {evt.status === 'failed' && <AlertTriangle className="w-3.5 h-3.5 text-[#FF5C7A]" />}
                          {evt.title}
                        </span>
                        <span className="text-[11px] font-mono-jarvis text-[#66738A]">{evt.timestamp}</span>
                      </div>
                      <p className="text-xs text-[#8B98AA] leading-relaxed">{evt.detail}</p>
                      {evt.evidence && (
                        <p className="text-[11px] font-mono-jarvis text-[#58D7FF]/80 mt-1.5 break-words">
                          {evt.evidence}
                        </p>
                      )}
                    </div>
                  ))
                )}
              </div>

              <div className="pt-3 space-y-2.5 border-t border-white/10" data-testid="conversation-stream">
                <div className="text-xs font-semibold text-[#8B98AA]">Conversation</div>

                {conversation.length === 0 ? (
                  <div className="text-xs text-[#66738A]" data-testid="conversation-empty">
                    No messages yet
                  </div>
                ) : (
                  conversation.map((msg) => (
                    <div
                      key={msg.id}
                      className={`p-3 rounded-2xl text-sm leading-relaxed ${
                        msg.sender === 'user'
                          ? 'bg-[#39B8FF]/12 border border-[#39B8FF]/35 text-[#F5F8FF] ml-4'
                          : 'bg-[#060D18]/90 border border-white/10 text-[#DCE5F2] mr-3'
                      }`}
                      data-testid={`message-${msg.sender}`}
                    >
                      <div className="flex items-center justify-between text-xs text-[#8B98AA] mb-1">
                        <span>{msg.sender === 'user' ? 'You' : 'JARVIS'}</span>
                        <span className="font-mono-jarvis text-[11px]">{msg.timestamp}</span>
                      </div>
                      <div className="text-sm">{msg.text}</div>
                    </div>
                  ))
                )}
              </div>
            </div>

            {notice && (
              <div
                className="mt-3 px-3 py-2 rounded-2xl bg-[#1A1405]/80 border border-[#F5B942]/30 text-xs text-[#F5B942]"
                data-testid="composer-notice"
              >
                {notice}
              </div>
            )}

            {/* COMMAND COMPOSER */}
            <div className="mt-3 rounded-3xl p-3.5 jarvis-glass-strong shrink-0 border border-[rgba(70,170,255,0.35)] focus-within:border-[#39B8FF]">
              <textarea
                value={inputVal}
                onChange={(e) => setInputVal(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSubmitDirective();
                  }
                }}
                rows={1}
                placeholder="Message JARVIS..."
                className="w-full bg-transparent text-[#F5F8FF] placeholder-[#607085] text-sm focus:outline-none resize-none leading-relaxed"
                data-testid="composer-input"
              />

              <div className="flex items-center justify-between mt-2 pt-2 border-t border-white/5">
                <div className="flex items-center gap-1.5">
                  <button
                    onClick={() => reportNotConfigured('Attachments')}
                    className="p-1.5 rounded-xl btn-jarvis-neutral text-[#8B98AA] hover:text-[#F5F8FF] transition-all"
                    title="Attachments — Not configured"
                    data-testid="composer-attach-button"
                  >
                    <Paperclip className="w-3.5 h-3.5" />
                  </button>

                  <button
                    onClick={() => reportNotConfigured('Plan')}
                    className="px-2.5 py-1 rounded-xl btn-jarvis-neutral text-[#8B98AA] hover:text-white transition-all text-xs"
                    title="Plan — Not configured"
                    data-testid="composer-plan-button"
                  >
                    Plan
                  </button>

                  <button
                    onClick={() => reportNotConfigured('Context')}
                    className="px-2.5 py-1 rounded-xl btn-jarvis-neutral text-[#8B98AA] hover:text-white transition-all text-xs flex items-center gap-1"
                    title="Context — Not configured"
                    data-testid="composer-context-button"
                  >
                    <Search className="w-3 h-3 opacity-70" /> Context
                  </button>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={handleToggleVoice}
                    className={`p-2 rounded-xl transition-all ${
                      isListening
                        ? 'text-[#58D7FF] bg-[#39B8FF]/20 shadow-[0_0_14px_#39B8FF] animate-pulse'
                        : 'text-[#39B8FF] hover:bg-white/5'
                    }`}
                    title={isListening ? 'Listening — click to stop' : 'Dictate a message'}
                    data-testid="composer-mic-button"
                  >
                    <Mic className="w-4 h-4 stroke-[2]" />
                  </button>

                  <button
                    onClick={() => handleSubmitDirective()}
                    disabled={!inputVal.trim()}
                    className="w-8 h-8 rounded-xl bg-[#0B203D] enabled:hover:bg-[#102B52] border border-[#39B8FF] flex items-center justify-center text-white active:scale-95 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
                    title="Send"
                    data-testid="composer-send-button"
                  >
                    <Send className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            </div>
          </section>
        )}

        {/* RIGHT PANE: preview surface */}
        <section className="flex-1 h-full flex flex-col p-4 sm:p-5 overflow-hidden z-20 transition-all duration-300">
          {/* Preview controls */}
          <div className="flex items-center justify-between pb-3 shrink-0 gap-3">
            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  soundFX.playBeep(640, 0.04);
                  setViewMode('preview');
                }}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium transition-all ${
                  viewMode === 'preview'
                    ? 'bg-[#39B8FF]/20 border border-[#39B8FF] text-[#F5F8FF]'
                    : 'btn-jarvis-neutral text-[#8B98AA]'
                }`}
                data-testid="view-preview-button"
              >
                <Eye className="w-3.5 h-3.5" />
                <span>Preview</span>
              </button>

              <button
                onClick={() => {
                  soundFX.playBeep(640, 0.04);
                  setViewMode(viewMode === 'inspect' ? 'preview' : 'inspect');
                }}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium transition-all ${
                  viewMode === 'inspect'
                    ? 'bg-[#39B8FF]/20 border border-[#39B8FF] text-[#58D7FF]'
                    : 'btn-jarvis-neutral text-[#8B98AA]'
                }`}
                data-testid="view-inspect-button"
              >
                <Code className="w-3.5 h-3.5" />
                <span>Inspect</span>
              </button>

              <button
                onClick={() => {
                  if (!hasArtifact) {
                    reportNotConfigured('Edit');
                    return;
                  }
                  soundFX.playBeep(600, 0.04);
                  setViewMode(viewMode === 'edit' ? 'preview' : 'edit');
                }}
                disabled={!hasArtifact}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium transition-all disabled:opacity-40 disabled:cursor-not-allowed ${
                  viewMode === 'edit'
                    ? 'bg-[#FF294D]/20 border border-[#FF294D] text-[#FF5C7A]'
                    : 'btn-jarvis-neutral text-[#8B98AA]'
                }`}
                title={hasArtifact ? 'Edit artifact' : 'Edit — no artifact yet'}
                data-testid="view-edit-button"
              >
                <Edit3 className="w-3.5 h-3.5" />
                <span>Edit</span>
              </button>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={() => reportNotConfigured('Refresh')}
                disabled={!hasArtifact}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl btn-jarvis-neutral text-xs text-[#8B98AA] enabled:hover:text-white transition-all disabled:opacity-40 disabled:cursor-not-allowed"
                title={hasArtifact ? 'Refresh preview' : 'Refresh — no artifact yet'}
                data-testid="preview-refresh-button"
              >
                <RefreshCw className="w-3.5 h-3.5" />
                <span>Refresh</span>
              </button>

              <button
                onClick={() => {
                  soundFX.playAcknowledge();
                  setIsFullScreen(!isFullScreen);
                }}
                className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-medium transition-all active:scale-95 ${
                  isFullScreen
                    ? 'bg-[#0B1E38] border border-[#39B8FF] text-[#58D7FF]'
                    : 'btn-jarvis-neutral text-[#8B98AA] hover:text-white'
                }`}
                data-testid="preview-fullscreen-button"
              >
                {isFullScreen ? (
                  <>
                    <Minimize2 className="w-3.5 h-3.5" />
                    <span>Exit full screen</span>
                  </>
                ) : (
                  <>
                    <Maximize2 className="w-3.5 h-3.5" />
                    <span>Full screen</span>
                  </>
                )}
              </button>

              <button
                onClick={() => reportNotConfigured('Launch')}
                disabled={!hasArtifact}
                className="flex items-center gap-1.5 px-4 py-1.5 rounded-xl btn-jarvis-red text-xs font-semibold tracking-wide active:scale-95 transition-all disabled:opacity-40 disabled:cursor-not-allowed"
                title={hasArtifact ? 'Launch artifact' : 'Launch — no artifact yet'}
                data-testid="preview-launch-button"
              >
                <Play className="w-3 h-3 fill-white" />
                <span>Launch</span>
              </button>
            </div>
          </div>

          {/* Preview frame */}
          <div className="flex-1 rounded-3xl jarvis-glass-strong overflow-hidden flex flex-col relative border border-[rgba(70,170,255,0.22)]">
            <div className="h-10 bg-[#060D18]/95 border-b border-[rgba(70,170,255,0.18)] px-4 flex items-center justify-between shrink-0">
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1.5">
                  <div className="w-2.5 h-2.5 rounded-full bg-[#FF294D]/80" />
                  <div className="w-2.5 h-2.5 rounded-full bg-[#F5B942]/80" />
                  <div className="w-2.5 h-2.5 rounded-full bg-[#18CC8A]/80" />
                </div>
                <div className="text-xs font-mono-jarvis text-[#8B98AA] pl-2" data-testid="preview-path">
                  {artifact ? `/ app-preview • ${artifact.revision}` : '/ app-preview'}
                </div>
              </div>

              <div className="flex items-center gap-4 text-xs text-[#8B98AA]">
                <div className="flex items-center gap-1.5" data-testid="preview-status">
                  <span className={`w-1.5 h-1.5 rounded-full ${stateUI.dot}`} />
                  <span className="text-[#DCE5F2]">{artifact ? artifact.status : 'No artifact'}</span>
                </div>

                <button
                  onClick={() => reportNotConfigured('Share')}
                  disabled={!hasArtifact}
                  className="flex items-center gap-1 text-[#8B98AA] enabled:hover:text-[#F5F8FF] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                  title={hasArtifact ? 'Share' : 'Share — no artifact yet'}
                  data-testid="preview-share-button"
                >
                  <Share2 className="w-3.5 h-3.5" />
                  <span>Share</span>
                </button>

                <button
                  onClick={() => reportNotConfigured('More actions')}
                  className="text-[#8B98AA] hover:text-[#F5F8FF] transition-colors"
                  title="More actions — Not configured"
                  data-testid="preview-more-button"
                >
                  <MoreHorizontal className="w-4 h-4" />
                </button>
              </div>
            </div>

            <div className="flex-1 relative overflow-hidden bg-[#030508]">
              {viewMode === 'preview' && <GeneratedArtifact artifact={artifact} />}

              {viewMode === 'inspect' && (
                <div className="w-full h-full p-6 overflow-y-auto" data-testid="inspect-panel">
                  {artifact?.source ? (
                    <pre className="p-4 rounded-2xl bg-[#060D18] border border-[rgba(70,170,255,0.2)] overflow-x-auto text-[11px] font-mono-jarvis leading-relaxed text-[#58D7FF]">
                      {artifact.source}
                    </pre>
                  ) : (
                    <div className="h-full flex items-center justify-center text-center">
                      <div className="space-y-2 max-w-sm">
                        <div className="text-sm font-semibold text-[#8B98AA]">No source to inspect</div>
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
                    <div className="text-sm font-semibold text-[#8B98AA]">Nothing to edit</div>
                    <p className="text-sm text-[#66738A] leading-relaxed">
                      Artifact parameters become editable after a verified render.
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </section>
      </div>
    </div>
  );
};
