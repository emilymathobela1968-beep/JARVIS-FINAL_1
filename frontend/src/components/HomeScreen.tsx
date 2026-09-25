import React, { useRef, useState } from 'react';
import { Paperclip, Mic, SendHorizontal } from 'lucide-react';
import { JarvisLogo } from './JarvisLogo';
import { JarvisMenu } from './JarvisMenu';
import homeBg from '../assets/images/bg_a.png';

interface HomeScreenProps {
  /** Home collects the objective and always hands it to the Builder screen. */
  onSubmit: (directive: string) => void;
  onOpenBuilder: () => void;
}

export const HomeScreen: React.FC<HomeScreenProps> = ({ onSubmit, onOpenBuilder }) => {
  const [value, setValue] = useState('');
  const [notice, setNotice] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const submit = () => {
    const text = value.trim();
    if (!text) return;
    onSubmit(text);
  };

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-black" data-testid="home-screen">
      <img
        src={homeBg}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full object-cover select-none pointer-events-none"
      />

      {/* Top-left live logo — small breathing room only. */}
      <div className="absolute top-[10px] left-[10px] z-20" data-testid="home-logo">
        <JarvisLogo size="md" />
      </div>

      {/* Top-right live menu */}
      <div className="absolute top-[10px] right-[14px] z-30">
        <JarvisMenu onGoBuilder={onOpenBuilder} />
      </div>

      {/* Command panel — lower third, clear of the radar circle. */}
      <div className="absolute left-0 right-0 bottom-[7vh] z-20 flex justify-center px-6">
        <div
          className="w-full max-w-[720px] rounded-2xl px-5 py-4"
          style={{
            background: 'linear-gradient(180deg, rgba(8,20,42,0.62) 0%, rgba(5,12,26,0.68) 100%)',
            border: '1px solid rgba(95,160,255,0.38)',
            boxShadow: '0 0 34px rgba(47,124,255,0.18), inset 0 1px 0 rgba(255,255,255,0.06), 0 26px 60px rgba(0,0,0,0.45)',
            backdropFilter: 'blur(18px) saturate(120%)',
            WebkitBackdropFilter: 'blur(18px) saturate(120%)',
          }}
          data-testid="home-command-panel"
        >
          <div className="flex items-center gap-3">
            <span
              className="w-7 h-7 shrink-0 rounded-md flex items-center justify-center text-[12px] text-[#9FC6FF]"
              style={{ background: 'rgba(47,124,255,0.16)', border: '1px solid rgba(95,160,255,0.45)' }}
              data-testid="home-jarvis-badge"
            >
              J
            </span>
            <span className="text-[13px] tracking-[0.2em] text-[#EAF2FF]">JARVIS</span>
            <span className="flex items-center gap-1.5 text-[11px] text-[#8FE9C8]">
              <span className="w-1.5 h-1.5 rounded-full bg-[#28D7A1] shadow-[0_0_8px_rgba(40,215,161,0.9)]" />
              Online
            </span>
            <span className="ml-auto text-[12px] text-[#8EA1BA] hidden sm:block">
              Tell me what to build.
            </span>
          </div>

          <div className="mt-3 flex items-center gap-2.5">
            <button
              type="button"
              onClick={() => setNotice('Attachments are not connected yet')}
              className="w-9 h-9 shrink-0 rounded-lg flex items-center justify-center text-[#8EA1BA] hover:text-[#DCE5F2] transition-colors"
              style={{ background: 'rgba(8,18,36,0.6)', border: '1px solid rgba(95,160,255,0.28)' }}
              aria-label="Attach"
              data-testid="home-attach-button"
            >
              <Paperclip className="w-4 h-4" />
            </button>

            <input
              ref={inputRef}
              value={value}
              onChange={(e) => setValue(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && submit()}
              placeholder="Give JARVIS a directive…"
              className="flex-1 min-w-0 h-10 bg-transparent outline-none text-[15px] text-[#F5F8FF] placeholder-[#7C8DA6]"
              data-testid="home-command-input"
            />

            <button
              type="button"
              onClick={() => setNotice('Voice is not connected yet')}
              className="w-9 h-9 shrink-0 rounded-lg flex items-center justify-center text-[#8EA1BA] hover:text-[#7FB4FF] transition-colors"
              style={{ background: 'rgba(8,18,36,0.6)', border: '1px solid rgba(95,160,255,0.28)' }}
              aria-label="Voice"
              data-testid="home-mic-button"
            >
              <Mic className="w-4 h-4" />
            </button>

            <button
              type="button"
              onClick={submit}
              disabled={!value.trim()}
              className="w-10 h-9 shrink-0 rounded-lg flex items-center justify-center text-white transition-all disabled:opacity-35 disabled:cursor-not-allowed"
              style={{
                background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                border: '1px solid rgba(120,178,255,0.85)',
                boxShadow: '0 0 18px rgba(47,124,255,0.34), inset 0 1px 0 rgba(255,255,255,0.18)',
              }}
              aria-label="Send"
              data-testid="home-send-button"
            >
              <SendHorizontal className="w-4 h-4" />
            </button>
          </div>

          {notice && (
            <div className="mt-2 text-[11px] text-[#8EA1BA]" data-testid="home-notice">
              {notice}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
