import React, { useState } from 'react';
import { Mic, Hammer } from 'lucide-react';
import { JarvisLogo } from './JarvisLogo';
import { AppCategory } from '../types';
import builderBg from '../assets/images/bg_b.png';

interface BuilderScreenProps {
  initialDirective?: string;
  onBuild: (directive: string, type: AppCategory) => void;
}

const TYPES: { id: AppCategory; label: string; live: boolean }[] = [
  { id: 'web', label: 'Web App', live: true },
  { id: 'mobile', label: 'Mobile App', live: false },
  { id: 'ai', label: 'AI Model', live: false },
];

export const BuilderScreen: React.FC<BuilderScreenProps> = ({ initialDirective = '', onBuild }) => {
  const [value, setValue] = useState(initialDirective);
  const [type, setType] = useState<AppCategory>('web');
  const [notice, setNotice] = useState<string | null>(null);

  const build = () => {
    const text = value.trim();
    if (!text) return;
    onBuild(text, type);
  };

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-black" data-testid="builder-screen">
      <img
        src={builderBg}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full object-cover select-none pointer-events-none"
      />

      <div className="absolute top-[10px] left-[10px] z-20" data-testid="builder-logo">
        <JarvisLogo size="md" />
      </div>

      {/* Build panel — lower third, clear of the radar circle. */}
      <div className="absolute left-0 right-0 bottom-[5vh] z-20 flex flex-col items-center px-6">
        <div
          className="w-full max-w-[760px] rounded-2xl px-5 py-4"
          style={{
            background: 'linear-gradient(180deg, rgba(8,20,42,0.62) 0%, rgba(5,12,26,0.68) 100%)',
            border: '1px solid rgba(95,160,255,0.38)',
            boxShadow: '0 0 34px rgba(47,124,255,0.18), inset 0 1px 0 rgba(255,255,255,0.06), 0 26px 60px rgba(0,0,0,0.45)',
            backdropFilter: 'blur(18px) saturate(120%)',
            WebkitBackdropFilter: 'blur(18px) saturate(120%)',
          }}
          data-testid="builder-input-panel"
        >
          <div className="flex items-start gap-3">
            <textarea
              value={value}
              onChange={(e) => setValue(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  build();
                }
              }}
              rows={2}
              placeholder="Describe the application you want to build..."
              className="flex-1 min-w-0 bg-transparent resize-none outline-none text-[15px] leading-relaxed text-[#F5F8FF] placeholder-[#7C8DA6] py-1"
              data-testid="builder-description-input"
            />

            <button
              type="button"
              onClick={() => setNotice('Voice is not connected yet')}
              className="w-10 h-10 shrink-0 rounded-lg flex items-center justify-center text-[#8EA1BA] hover:text-[#7FB4FF] transition-colors"
              style={{ background: 'rgba(8,18,36,0.6)', border: '1px solid rgba(95,160,255,0.28)' }}
              aria-label="Voice"
              data-testid="builder-mic-button"
            >
              <Mic className="w-4 h-4" />
            </button>

            <button
              type="button"
              onClick={build}
              disabled={!value.trim()}
              className="h-10 px-5 shrink-0 rounded-lg flex items-center gap-2 text-[14px] text-white transition-all disabled:opacity-35 disabled:cursor-not-allowed"
              style={{
                background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                border: '1px solid rgba(120,178,255,0.85)',
                boxShadow: '0 0 18px rgba(47,124,255,0.34), inset 0 1px 0 rgba(255,255,255,0.18)',
              }}
              data-testid="builder-build-button"
            >
              <Hammer className="w-4 h-4" />
              Build
            </button>
          </div>

          {notice && (
            <div className="mt-2 text-[11px] text-[#8EA1BA]" data-testid="builder-notice">
              {notice}
            </div>
          )}
        </div>

        {/* App type selector — live controls below the panel. */}
        <div className="mt-3 flex items-center gap-2.5" data-testid="builder-type-selector">
          {TYPES.map(({ id, label, live }) => {
            const active = live && type === id;
            return (
              <button
                key={id}
                type="button"
                disabled={!live}
                onClick={() => setType(id)}
                className="h-10 px-4 rounded-lg flex items-center gap-2 text-[13px] transition-all disabled:cursor-not-allowed"
                style={
                  active
                    ? {
                        color: '#F5F8FF',
                        background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                        border: '1px solid rgba(120,178,255,0.85)',
                        boxShadow: '0 0 16px rgba(47,124,255,0.3)',
                      }
                    : {
                        color: live ? '#DCE5F2' : '#5C6B81',
                        background: 'linear-gradient(180deg, rgba(8,18,36,0.6) 0%, rgba(5,11,22,0.6) 100%)',
                        border: '1px solid rgba(95,160,255,0.24)',
                        backdropFilter: 'blur(12px)',
                        WebkitBackdropFilter: 'blur(12px)',
                      }
                }
                data-testid={`builder-type-${id}`}
              >
                {label}
                {!live && (
                  <span
                    className="text-[10px] px-1.5 py-0.5 rounded"
                    style={{ background: 'rgba(95,160,255,0.12)', color: '#7C8DA6' }}
                  >
                    Soon
                  </span>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
