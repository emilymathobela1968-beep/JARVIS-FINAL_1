import React, { useEffect, useRef, useState } from 'react';
import heroArtwork from '../assets/images/jarvis_home_hero.png';

/**
 * The artwork is the visual source of truth. Every live control is an overlay
 * positioned in the artwork's own coordinate space, so it stays aligned at any
 * viewport size.
 */
const IMG_W = 1672;
const IMG_H = 941;

/** Rects measured directly from the reference artwork (image pixels). */
const RECT = {
  menu: { x: 1470, y: 30, w: 168, h: 64 },
  attach: { x: 318, y: 788, w: 50, h: 50 },
  input: { x: 402, y: 788, w: 690, h: 50 },
  keyboard: { x: 1095, y: 789, w: 50, h: 48 },
  mic: { x: 1189, y: 775, w: 72, h: 72 },
  send: { x: 1300, y: 786, w: 55, h: 53 },
  statusLine: { x: 402, y: 872, w: 960, h: 44 },
} as const;

type Rect = { x: number; y: number; w: number; h: number };

const place = (r: Rect): React.CSSProperties => ({
  position: 'absolute',
  left: `${(r.x / IMG_W) * 100}%`,
  top: `${(r.y / IMG_H) * 100}%`,
  width: `${(r.w / IMG_W) * 100}%`,
  height: `${(r.h / IMG_H) * 100}%`,
});

const DESTINATIONS = ['Home', 'Computer', 'Developer', 'Media', 'Builder', 'Barehands', 'System'] as const;

interface JarvisHeroScreenProps {
  onStartBuild: (directive: string) => void;
}

export const JarvisHeroScreen: React.FC<JarvisHeroScreenProps> = ({ onStartBuild }) => {
  const [inputText, setInputText] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [status, setStatus] = useState<{ title: string; detail?: string } | null>(null);

  // Rendered artwork box: cover the viewport without ever distorting the image.
  const [box, setBox] = useState({ left: 0, top: 0, w: IMG_W, h: IMG_H, scale: 1 });

  const inputRef = useRef<HTMLInputElement | null>(null);
  const fileRef = useRef<HTMLInputElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const measure = () => {
      const vw = window.innerWidth;
      const vh = window.innerHeight;
      const scale = Math.max(vw / IMG_W, vh / IMG_H);
      const w = IMG_W * scale;
      const h = IMG_H * scale;
      setBox({ left: (vw - w) / 2, top: (vh - h) / 2, w, h, scale });
    };
    measure();
    window.addEventListener('resize', measure);
    return () => window.removeEventListener('resize', measure);
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsMenuOpen(false);
    };
    const onPointer = (e: MouseEvent) => {
      if (!isMenuOpen) return;
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setIsMenuOpen(false);
    };
    window.addEventListener('keydown', onKey);
    window.addEventListener('mousedown', onPointer);
    return () => {
      window.removeEventListener('keydown', onKey);
      window.removeEventListener('mousedown', onPointer);
    };
  }, [isMenuOpen]);

  useEffect(() => {
    if (!status) return;
    const t = setTimeout(() => setStatus(null), 4200);
    return () => clearTimeout(t);
  }, [status]);

  const submit = () => {
    const directive = inputText.trim();
    if (!directive) return;
    onStartBuild(directive);
  };

  const s = (v: number) => v * box.scale;

  return (
    <div className="fixed inset-0 overflow-hidden bg-black" data-testid="hero-screen">
      <div
        className="absolute"
        style={{ left: box.left, top: box.top, width: box.w, height: box.h }}
        data-testid="hero-artwork-frame"
      >
        {/* 1. THE ARTWORK — never recolored, never regenerated */}
        <img
          src={heroArtwork}
          alt="JARVIS"
          draggable={false}
          className="absolute inset-0 w-full h-full select-none"
          style={{ objectFit: 'fill' }}
          data-testid="hero-artwork"
        />

        {/* 2. MENU — transparent hit target over the artwork control */}
        <div ref={menuRef} style={place(RECT.menu)}>
          <button
            type="button"
            onClick={() => setIsMenuOpen((v) => !v)}
            className="w-full h-full cursor-pointer rounded-2xl transition-colors hover:bg-white/[0.04]"
            aria-label="Open navigation menu"
            aria-expanded={isMenuOpen}
            data-testid="hero-menu-button"
          />

          {isMenuOpen && (
            <div
              className="absolute right-0 rounded-2xl overflow-hidden"
              style={{
                top: '110%',
                width: s(260),
                background: 'rgba(7, 18, 31, 0.86)',
                border: '1px solid rgba(80, 180, 255, 0.42)',
                backdropFilter: 'blur(18px)',
                WebkitBackdropFilter: 'blur(18px)',
                boxShadow: '0 24px 60px rgba(0,0,0,0.65)',
              }}
              data-testid="hero-menu-panel"
            >
              {DESTINATIONS.map((dest) => (
                <button
                  key={dest}
                  type="button"
                  onClick={() => {
                    setIsMenuOpen(false);
                    if (dest !== 'Home') {
                      setStatus({ title: `${dest}: not connected`, detail: 'Runtime pending integration' });
                    }
                  }}
                  className="w-full text-left transition-colors hover:bg-[#2F9DFF]/12"
                  style={{
                    color: dest === 'Home' ? '#F5F8FF' : '#8EA6C2',
                    padding: `${s(11)}px ${s(18)}px`,
                    fontSize: s(15),
                  }}
                  data-testid={`hero-menu-item-${dest.toLowerCase()}`}
                >
                  {dest}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* 3. COMMAND INPUT — a blurred slice of the artwork masks the baked-in
            placeholder so the live field reads cleanly with no visible seam. */}
        <div style={{ ...place(RECT.input), overflow: 'hidden' }}>
          <div
            className="absolute pointer-events-none"
            style={{
              inset: -s(24),
              backgroundImage: `url(${heroArtwork})`,
              backgroundSize: `${box.w}px ${box.h}px`,
              backgroundPosition: `${-(s(RECT.input.x) - s(24))}px ${-(s(RECT.input.y) - s(24))}px`,
              filter: `blur(${Math.max(6, s(9))}px)`,
            }}
          />
          <input
            ref={inputRef}
            type="text"
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                submit();
              }
            }}
            placeholder="Talk to Jarvis or type a command..."
            autoFocus
            className="absolute inset-0 w-full h-full bg-transparent border-0 outline-none font-sans placeholder-[#8EA6C2]"
            style={{
              paddingLeft: s(17),
              paddingRight: s(8),
              fontSize: s(21),
              lineHeight: 1.2,
              color: '#F5F8FF',
              caretColor: '#33D6FF',
            }}
            data-testid="hero-command-input"
          />
        </div>

        {/* 4. ATTACHMENT — real local file picker, nothing is uploaded */}
        <input
          ref={fileRef}
          type="file"
          className="hidden"
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) setStatus({ title: `Attached: ${f.name}`, detail: 'Held locally. Not sent anywhere yet.' });
          }}
          data-testid="hero-file-input"
        />
        <button
          type="button"
          onClick={() => fileRef.current?.click()}
          style={place(RECT.attach)}
          className="rounded-full cursor-pointer transition-colors hover:bg-white/[0.05]"
          aria-label="Attach a local file"
          data-testid="hero-attach-button"
        />

        {/* 5. KEYBOARD — focuses the command field */}
        <button
          type="button"
          onClick={() => inputRef.current?.focus()}
          style={place(RECT.keyboard)}
          className="rounded-lg cursor-pointer transition-colors hover:bg-white/[0.05]"
          aria-label="Focus the command field"
          data-testid="hero-keyboard-button"
        />

        {/* 6. MICROPHONE — truthful: no voice runtime is connected yet */}
        <button
          type="button"
          onClick={() =>
            setStatus({ title: 'Voice not connected', detail: 'Realtime voice runtime pending integration' })
          }
          style={place(RECT.mic)}
          className="rounded-full cursor-pointer transition-colors hover:bg-white/[0.06]"
          aria-label="Voice input"
          data-testid="hero-mic-button"
        />

        {/* 7. SEND — hands the directive to the Builder */}
        <button
          type="button"
          onClick={submit}
          style={place(RECT.send)}
          className="rounded-full cursor-pointer transition-colors hover:bg-white/[0.06]"
          aria-label="Send directive"
          data-testid="hero-send-button"
        />

        {/* 8. STATUS LINE — only appears when there is something true to say */}
        {status && (
          <div style={place(RECT.statusLine)} className="flex items-start" data-testid="hero-status">
            <div
              className="rounded-xl"
              style={{
                background: 'rgba(3, 8, 16, 0.55)',
                border: '1px solid rgba(80, 180, 255, 0.42)',
                backdropFilter: 'blur(12px)',
                WebkitBackdropFilter: 'blur(12px)',
                padding: `${s(7)}px ${s(14)}px`,
              }}
            >
              <span style={{ color: '#F5F8FF', fontSize: s(14) }} data-testid="hero-status-title">
                {status.title}
              </span>
              {status.detail && (
                <span style={{ color: '#8EA6C2', fontSize: s(13), marginLeft: s(10) }}>{status.detail}</span>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
