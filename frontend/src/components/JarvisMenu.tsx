import React, { useEffect, useRef, useState } from 'react';
import { Home, Monitor, Code2, PlaySquare, Box, Activity, Settings, Menu as MenuIcon } from 'lucide-react';

const DESTINATIONS = [
  { id: 'home', label: 'Home', icon: Home, live: true },
  { id: 'builder', label: 'Builder', icon: Box, live: true },
  { id: 'media', label: 'Media', icon: PlaySquare, live: false },
  { id: 'barehands', label: 'Barehands', icon: Activity, live: false },
  { id: 'computer', label: 'Computer', icon: Monitor, live: false },
  { id: 'developer', label: 'Developer', icon: Code2, live: false },
  { id: 'system', label: 'System', icon: Settings, live: false },
] as const;

interface JarvisMenuProps {
  onGoHome?: () => void;
  onGoBuilder?: () => void;
}

export const JarvisMenu: React.FC<JarvisMenuProps> = ({ onGoHome, onGoBuilder }) => {
  const [open, setOpen] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const wrapRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    window.addEventListener('mousedown', onDown);
    window.addEventListener('keydown', onKey);
    return () => {
      window.removeEventListener('mousedown', onDown);
      window.removeEventListener('keydown', onKey);
    };
  }, [open]);

  return (
    <div ref={wrapRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 px-4 h-10 rounded-lg text-[12px] tracking-[0.22em] text-[#DCE5F2] hover:text-white transition-all"
        style={{
          background: 'linear-gradient(180deg, rgba(10,22,44,0.66) 0%, rgba(6,13,26,0.66) 100%)',
          border: '1px solid rgba(95,160,255,0.42)',
          boxShadow: '0 0 18px rgba(47,124,255,0.22), inset 0 1px 0 rgba(255,255,255,0.06)',
          backdropFilter: 'blur(14px)',
          WebkitBackdropFilter: 'blur(14px)',
        }}
        data-testid="home-menu-button"
      >
        <MenuIcon className="w-4 h-4 text-[#7FB4FF]" />
        MENU
      </button>

      {open && (
        <div
          className="absolute right-0 mt-2 w-[236px] rounded-xl overflow-hidden py-1.5"
          style={{
            background: 'linear-gradient(180deg, rgba(8,18,36,0.94) 0%, rgba(5,11,22,0.94) 100%)',
            border: '1px solid rgba(95,160,255,0.34)',
            boxShadow: '0 22px 60px rgba(0,0,0,0.6), 0 0 26px rgba(47,124,255,0.16)',
            backdropFilter: 'blur(18px)',
            WebkitBackdropFilter: 'blur(18px)',
          }}
          data-testid="home-menu-panel"
        >
          {DESTINATIONS.map(({ id, label, icon: Icon, live }) => (
            <button
              key={id}
              type="button"
              onClick={() => {
                if (id === 'home') {
                  setOpen(false);
                  onGoHome?.();
                  return;
                }
                if (id === 'builder') {
                  setOpen(false);
                  onGoBuilder?.();
                  return;
                }
                setNotice(`${label} is not connected yet`);
              }}
              className={`w-full flex items-center gap-3 px-4 py-2.5 text-sm transition-colors ${
                live ? 'text-[#DCE5F2] hover:bg-[rgba(47,124,255,0.14)]' : 'text-[#66738A] hover:text-[#8EA1BA]'
              }`}
              data-testid={`menu-item-${id}`}
            >
              <Icon className={`w-4 h-4 ${live ? 'text-[#7FB4FF]' : 'text-[#4E5C70]'}`} />
              <span className="flex-1 text-left">{label}</span>
              {!live && <span className="text-[10px] tracking-wide text-[#4E5C70]">Soon</span>}
            </button>
          ))}

          {notice && (
            <div className="px-4 pt-2 pb-1.5 text-[11px] text-[#8EA1BA] border-t border-[rgba(95,160,255,0.16)] mt-1" data-testid="menu-notice">
              {notice}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
