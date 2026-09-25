import React, { useEffect, useRef, useState } from 'react';
import {
  Home,
  Monitor,
  Link2,
  Loader2,
  Check,
  AlertTriangle,
  Ban,
  SendHorizontal,
  Calculator,
  FileText,
  Table,
  Mail,
  Settings,
  Cpu,
  Network,
  Activity,
  Clock,
  HardDrive,
  ShieldQuestion,
} from 'lucide-react';
import { JarvisMenu } from './JarvisMenu';
import {
  windowsCommand,
  windowsIntent,
  windowsPair,
  windowsStatus,
  WindowsCommandResult,
} from '../utils/api';
import workstationBg from '../assets/images/bg_c.png';

interface ComputerWorkspaceProps {
  onReturnHome: () => void;
  onOpenBuilder: () => void;
  onOpenImageGeneration: () => void;
}

type LogKind = 'user' | 'plan' | 'ok' | 'fail' | 'blocked';

interface LogEntry {
  id: string;
  kind: LogKind;
  text: string;
  detail?: string;
  evidence?: string;
  at: string;
}

const QUICK = [
  { label: 'Calculator', icon: Calculator, action: 'app.launch', params: { app: 'calculator' } },
  { label: 'Notepad', icon: FileText, action: 'app.launch', params: { app: 'notepad' } },
  { label: 'Word', icon: FileText, action: 'app.launch', params: { app: 'winword' } },
  { label: 'Excel', icon: Table, action: 'app.launch', params: { app: 'excel' } },
  { label: 'Outlook', icon: Mail, action: 'app.launch', params: { app: 'outlook' } },
  { label: 'Control Panel', icon: Settings, action: 'windows.open', params: { target: 'control_panel' } },
  { label: 'Device Manager', icon: Cpu, action: 'windows.open', params: { target: 'device_manager' } },
  { label: 'Network Settings', icon: Network, action: 'windows.open', params: { target: 'network' } },
  { label: 'Task Manager', icon: Activity, action: 'windows.open', params: { target: 'task_manager' } },
  { label: 'Date & Time', icon: Clock, action: 'system.info', params: { what: 'time' } },
  { label: 'Disk Space', icon: HardDrive, action: 'system.info', params: { what: 'disk' } },
  { label: 'Processes', icon: Activity, action: 'system.info', params: { what: 'processes' } },
];

const LINK_KEY = 'jarvis.windows.link_id';
const now = () => new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

export const ComputerWorkspace: React.FC<ComputerWorkspaceProps> = ({
  onReturnHome,
  onOpenBuilder,
  onOpenImageGeneration,
}) => {
  const [linkId, setLinkId] = useState<string | null>(() => localStorage.getItem(LINK_KEY));
  const [status, setStatus] = useState<'checking' | 'connected' | 'not_running'>('checking');
  const [host, setHost] = useState<string>('');
  const [pairCode, setPairCode] = useState<string | null>(null);
  const [log, setLog] = useState<LogEntry[]>([]);
  const [input, setInput] = useState('');
  const [busy, setBusy] = useState(false);
  const [pendingConfirm, setPendingConfirm] = useState<{
    action: string;
    params: Record<string, unknown>;
    summary: string;
  } | null>(null);
  const seq = useRef(0);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  const push = (entry: Omit<LogEntry, 'id' | 'at'>) => {
    seq.current += 1;
    setLog((prev) => [...prev, { ...entry, id: `l-${seq.current}`, at: now() }]);
    requestAnimationFrame(() =>
      scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
    );
  };

  const refreshStatus = async () => {
    const s = await windowsStatus(linkId);
    setStatus(s.state === 'connected' ? 'connected' : 'not_running');
    setHost(s.hostname || '');
    if (s.state === 'connected') setPairCode(null);
  };

  useEffect(() => {
    refreshStatus();
    const t = setInterval(refreshStatus, 4000);
    return () => clearInterval(t);
  }, [linkId]);

  const pair = async () => {
    const res = await windowsPair();
    localStorage.setItem(LINK_KEY, res.link_id);
    setLinkId(res.link_id);
    setPairCode(res.code);
    push({
      kind: 'plan',
      text: 'Pairing code generated',
      detail: 'Run the companion agent on your Windows 11 machine and enter this code.',
      evidence: `code=${res.code} expires=${new Date(res.expires_at).toLocaleTimeString()}`,
    });
  };

  const describe = (result: WindowsCommandResult) => {
    if (result.status === 'completed') {
      return typeof result.data === 'object' ? JSON.stringify(result.data, null, 1) : String(result.data ?? '');
    }
    return result.error || result.message || 'No detail returned.';
  };

  const execute = async (
    action: string,
    params: Record<string, unknown>,
    summary: string,
    confirmed = false
  ) => {
    if (!linkId) {
      push({ kind: 'blocked', text: 'No agent paired yet', detail: 'Press "Pair agent" to begin.' });
      return;
    }
    setBusy(true);
    push({ kind: 'plan', text: summary, evidence: `action=${action}` });
    const result = await windowsCommand(linkId, action, params, confirmed);
    setBusy(false);

    if (result.status === 'confirmation_required') {
      setPendingConfirm({ action, params, summary });
      push({
        kind: 'blocked',
        text: 'Confirmation required',
        detail: result.message || `${action} needs your confirmation.`,
      });
      return;
    }
    if (result.status === 'agent_offline') {
      setStatus('not_running');
      push({
        kind: 'blocked',
        text: 'Local agent not running',
        detail: result.message || 'Start the JARVIS Windows agent and pair it.',
      });
      return;
    }
    push({
      kind: result.status === 'completed' ? 'ok' : 'fail',
      text: result.status === 'completed' ? `${action} completed` : `${action} failed`,
      detail: describe(result),
    });
  };

  const send = async () => {
    const text = input.trim();
    if (!text || busy) return;
    setInput('');
    push({ kind: 'user', text });
    const intent = await windowsIntent(text);
    if (!intent.understood || !intent.action) {
      push({
        kind: 'blocked',
        text: 'Not mapped to a Windows action',
        detail: intent.message || 'Try "open Word", "open Control Panel", "check disk space".',
      });
      return;
    }
    const params = { ...(intent.params || {}) } as Record<string, unknown>;
    if (intent.requires_confirmation) {
      setPendingConfirm({ action: intent.action, params, summary: intent.summary || intent.action });
      push({
        kind: 'blocked',
        text: 'Confirmation required',
        detail: `${intent.summary || intent.action} — confirm to execute.`,
      });
      return;
    }
    await execute(intent.action, params, intent.summary || intent.action);
  };

  const statusPill = () => {
    if (status === 'checking') return { text: 'Checking agent…', color: '#9FB0C6', Icon: Loader2, spin: true };
    if (status === 'connected')
      return { text: `Local Agent Connected${host ? ` · ${host}` : ''}`, color: '#28D7A1', Icon: Check, spin: false };
    return { text: 'Local Agent Not Running', color: '#FF7A90', Icon: Ban, spin: false };
  };
  const pill = statusPill();

  return (
    <div
      className="relative w-screen h-screen overflow-hidden text-[#F5F8FF] flex flex-col font-sans"
      data-testid="computer-workspace"
    >
      <img
        src={workstationBg}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full object-cover select-none pointer-events-none"
      />

      <div className="relative z-20 shrink-0 flex items-center justify-between px-3 pt-[10px] pb-2">
        <button
          type="button"
          onClick={onReturnHome}
          className="flex items-center gap-2 px-3.5 h-10 rounded-lg text-[12px] tracking-[0.12em] text-[#DCE5F2] hover:text-white transition-all"
          style={{
            background: 'linear-gradient(180deg, rgba(10,22,44,0.66) 0%, rgba(6,13,26,0.66) 100%)',
            border: '1px solid rgba(95,160,255,0.42)',
            boxShadow: '0 0 18px rgba(47,124,255,0.22), inset 0 1px 0 rgba(255,255,255,0.06)',
            backdropFilter: 'blur(14px)',
            WebkitBackdropFilter: 'blur(14px)',
          }}
          data-testid="computer-home-button"
        >
          <Home className="w-4 h-4 text-[#7FB4FF]" />
          HOME
        </button>

        <JarvisMenu
          onGoHome={onReturnHome}
          onGoBuilder={onOpenBuilder}
          onGoImageGeneration={onOpenImageGeneration}
        />
      </div>

      <div className="relative z-10 flex-1 min-h-0 flex gap-4 px-4 pb-4">
        {/* LEFT — status, pairing, activity log, command composer */}
        <section
          className="w-[42%] min-w-[380px] flex flex-col rounded-2xl overflow-hidden border border-[rgba(95,160,255,0.2)] bg-[rgba(5,12,24,0.72)] backdrop-blur-2xl"
          data-testid="computer-left-pane"
        >
          <div className="px-5 pt-5 pb-3">
            <div className="flex items-center gap-2 text-xs tracking-[0.2em] text-[#8EA1BA]">
              <Monitor className="w-3.5 h-3.5 text-[#7FB4FF]" />
              WINDOWS CONTROL
            </div>

            <div className="mt-2 flex items-center gap-2" data-testid="agent-status">
              <pill.Icon
                className={`w-4 h-4 ${pill.spin ? 'animate-spin' : ''}`}
                style={{ color: pill.color }}
              />
              <span className="text-[14px]" style={{ color: pill.color }}>
                {pill.text}
              </span>
            </div>

            <div className="mt-3 flex items-center gap-2">
              <button
                type="button"
                onClick={pair}
                className="flex items-center gap-1.5 px-3 h-8 rounded-md text-[12px] text-[#EAF2FF] transition-colors"
                style={{
                  background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                  border: '1px solid rgba(120,178,255,0.85)',
                }}
                data-testid="pair-agent-button"
              >
                <Link2 className="w-3.5 h-3.5" />
                Pair agent
              </button>
              {busy && (
                <span className="flex items-center gap-1.5 text-[12px] text-[#9FB0C6]" data-testid="command-running">
                  <Loader2 className="w-3.5 h-3.5 animate-spin" /> Command running…
                </span>
              )}
            </div>

            {pairCode && (
              <div
                className="mt-3 rounded-lg px-3 py-2.5"
                style={{ background: 'rgba(47,124,255,0.1)', border: '1px solid rgba(95,160,255,0.32)' }}
                data-testid="pair-code-panel"
              >
                <div className="text-[11px] text-[#9FB0C6]">Pairing code — enter it in the agent</div>
                <div className="mt-0.5 text-2xl tracking-[0.3em] font-mono-jarvis text-[#EAF2FF]" data-testid="pair-code">
                  {pairCode}
                </div>
                <div className="mt-1 text-[11px] text-[#7C8DA6] leading-relaxed">
                  On your Windows 11 PC: run <span className="font-mono-jarvis">install.bat</span> once, then{' '}
                  <span className="font-mono-jarvis">run.bat</span> and paste this code.
                </div>
              </div>
            )}
          </div>

          <div
            ref={scrollRef}
            className="flex-1 min-h-0 overflow-y-auto px-5 pb-3 space-y-4"
            data-testid="computer-activity-log"
          >
            {log.length === 0 ? (
              <p className="text-[13px] text-[#7C8DA6] leading-relaxed" data-testid="log-empty">
                No commands yet. Pair the agent, then use a quick action or type a command.
              </p>
            ) : (
              log.map((e) => (
                <div key={e.id} className="flex gap-2.5" data-testid={`log-${e.kind}`}>
                  <span className="mt-0.5 shrink-0">
                    {e.kind === 'user' && <SendHorizontal className="w-3.5 h-3.5 text-[#7FB4FF]" />}
                    {e.kind === 'plan' && <ShieldQuestion className="w-3.5 h-3.5 text-[#9FB0C6]" />}
                    {e.kind === 'ok' && <Check className="w-3.5 h-3.5 text-[#28D7A1]" />}
                    {e.kind === 'fail' && <AlertTriangle className="w-3.5 h-3.5 text-[#FF7A90]" />}
                    {e.kind === 'blocked' && <Ban className="w-3.5 h-3.5 text-[#F5B942]" />}
                  </span>
                  <div className="min-w-0">
                    <div className="text-[13px] text-[#EAF2FF]">
                      {e.text}
                      <span className="ml-2 text-[11px] font-mono-jarvis text-[#5C6B81]">{e.at}</span>
                    </div>
                    {e.detail && (
                      <pre className="mt-1 text-[12px] text-[#9FB0C6] whitespace-pre-wrap break-words font-sans">
                        {e.detail}
                      </pre>
                    )}
                    {e.evidence && (
                      <p className="mt-1 text-[11px] font-mono-jarvis text-[#5C7C9E] break-words">{e.evidence}</p>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>

          {pendingConfirm && (
            <div
              className="mx-4 mb-3 rounded-lg px-3 py-2.5"
              style={{ background: 'rgba(245,185,66,0.1)', border: '1px solid rgba(245,185,66,0.4)' }}
              data-testid="confirm-bar"
            >
              <div className="text-[12px] text-[#F5D98A]">Confirm: {pendingConfirm.summary}</div>
              <div className="mt-2 flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => {
                    const p = pendingConfirm;
                    setPendingConfirm(null);
                    execute(p.action, p.params, p.summary, true);
                  }}
                  className="px-3 h-8 rounded-md text-[12px] text-white"
                  style={{ background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)' }}
                  data-testid="confirm-execute-button"
                >
                  Execute
                </button>
                <button
                  type="button"
                  onClick={() => setPendingConfirm(null)}
                  className="px-3 h-8 rounded-md text-[12px] text-[#9FB0C6] border border-[rgba(95,160,255,0.24)]"
                  data-testid="confirm-cancel-button"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          <div className="px-4 pb-4">
            <div
              className="rounded-xl px-3 pt-3 pb-2.5"
              style={{
                background: 'linear-gradient(180deg, rgba(12,24,46,0.9) 0%, rgba(8,16,32,0.9) 100%)',
                border: '1px solid rgba(95,160,255,0.3)',
                backdropFilter: 'blur(16px)',
                WebkitBackdropFilter: 'blur(16px)',
              }}
            >
              <textarea
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    send();
                  }
                }}
                rows={2}
                placeholder='Tell JARVIS what to do on Windows — e.g. "open Word", "write a resignation letter to Conrad Adams at Paramount Group, effective 30 September"'
                className="w-full bg-transparent resize-none outline-none text-[13px] text-[#F5F8FF] placeholder-[#66738A] leading-relaxed"
                data-testid="windows-command-input"
              />
              <div className="mt-1.5 flex justify-end">
                <button
                  type="button"
                  onClick={send}
                  disabled={!input.trim() || busy}
                  className="w-10 h-8 rounded-md flex items-center justify-center text-white disabled:opacity-35 disabled:cursor-not-allowed"
                  style={{
                    background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                    border: '1px solid rgba(120,178,255,0.85)',
                  }}
                  aria-label="Run command"
                  data-testid="windows-command-send"
                >
                  <SendHorizontal className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        </section>

        {/* RIGHT — quick actions over the artwork */}
        <section className="flex-1 min-w-0 flex flex-col" data-testid="computer-actions-pane">
          <div
            className="flex items-center px-3 h-10 rounded-lg mb-2 text-[12px] text-[#9FB0C6]"
            style={{
              background: 'rgba(5,12,24,0.42)',
              border: '1px solid rgba(95,160,255,0.16)',
              backdropFilter: 'blur(10px)',
              WebkitBackdropFilter: 'blur(10px)',
            }}
          >
            Quick actions
            <span className="ml-auto font-mono-jarvis text-[11px] text-[#7C8DA6]" data-testid="computer-status-chip">
              agent: {status === 'connected' ? 'connected' : status === 'checking' ? 'checking' : 'offline'}
            </span>
          </div>

          <div className="flex-1 min-h-0 overflow-y-auto">
            <div className="grid grid-cols-2 lg:grid-cols-3 gap-2.5">
              {QUICK.map(({ label, icon: Icon, action, params }) => (
                <button
                  key={label}
                  type="button"
                  onClick={() => execute(action, params, `${label}`)}
                  disabled={busy}
                  className="flex items-center gap-2.5 px-3.5 h-12 rounded-lg text-[13px] text-[#DCE5F2] hover:text-white transition-all disabled:opacity-40"
                  style={{
                    background: 'linear-gradient(180deg, rgba(10,22,44,0.55) 0%, rgba(6,13,26,0.55) 100%)',
                    border: '1px solid rgba(95,160,255,0.22)',
                    backdropFilter: 'blur(10px)',
                    WebkitBackdropFilter: 'blur(10px)',
                  }}
                  data-testid={`quick-${label.toLowerCase().replace(/[^a-z]+/g, '-')}`}
                >
                  <Icon className="w-4 h-4 text-[#7FB4FF]" />
                  {label}
                </button>
              ))}
            </div>

            <div className="mt-4 text-[12px] text-[#7C8DA6] leading-relaxed max-w-2xl">
              JARVIS executes these on your own Windows 11 machine through the local companion agent.
              Sending email, shell commands, deleting files, installing software and changing system
              settings always ask for confirmation first. No passwords are ever requested or stored.
            </div>
          </div>
        </section>
      </div>
    </div>
  );
};
