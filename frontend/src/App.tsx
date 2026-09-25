import { useEffect, useState } from 'react';
import homeBg from './assets/images/jarvis_home_bg.png';
import builderBg from './assets/images/jarvis_builder_bg.png';
import workstationBg from './assets/images/jarvis_workstation_bg.png';

/**
 * TEMPORARY — Step 1 asset-foundation review only.
 * Renders each clean, background-only artwork full screen with NOTHING over it
 * except a small temporary label. No command panel, menu, inputs, panels,
 * preview controls or navigation. The previous overlay app is preserved in
 * App.overlays.bak.tsx and will be restored once the raw backgrounds are confirmed.
 */

const SCREENS = [
  { key: 'home', label: 'Home background', src: homeBg },
  { key: 'builder', label: 'Builder background', src: builderBg },
  { key: 'workstation', label: 'Workstation background', src: workstationBg },
] as const;

export default function App() {
  const [index, setIndex] = useState(0);

  const go = (next: number) => setIndex((next + SCREENS.length) % SCREENS.length);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'ArrowRight') go(index + 1);
      if (e.key === 'ArrowLeft') go(index - 1);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [index]);

  const screen = SCREENS[index];

  return (
    <div
      className="w-screen h-screen overflow-hidden bg-black"
      data-testid={`raw-bg-${screen.key}`}
    >
      {/* Clean background artwork ONLY — full screen, nothing baked or overlaid. */}
      <img
        src={screen.src}
        alt={screen.label}
        className="w-full h-full object-cover select-none"
        draggable={false}
        data-testid="raw-bg-image"
      />

      {/* Small temporary label (the only React UI on screen). */}
      <div
        className="fixed top-3 left-3 px-3 py-1.5 rounded-md text-xs tracking-wide"
        style={{
          background: 'rgba(0,0,0,0.55)',
          color: '#DCE5F2',
          border: '1px solid rgba(255,255,255,0.18)',
          backdropFilter: 'blur(4px)',
        }}
        data-testid="raw-bg-label"
      >
        {screen.label}
      </div>

      {/* Tiny temporary review switcher (← / → or click). Not part of the product UI. */}
      <div
        className="fixed bottom-3 left-1/2 -translate-x-1/2 flex items-center gap-3 px-3 py-1.5 rounded-md text-[11px]"
        style={{
          background: 'rgba(0,0,0,0.5)',
          color: '#8EA1BA',
          border: '1px solid rgba(255,255,255,0.14)',
          backdropFilter: 'blur(4px)',
        }}
      >
        <button type="button" onClick={() => go(index - 1)} className="hover:text-white" data-testid="raw-prev">
          ← Prev
        </button>
        <span>
          {index + 1} / {SCREENS.length} · temporary review only
        </span>
        <button type="button" onClick={() => go(index + 1)} className="hover:text-white" data-testid="raw-next">
          Next →
        </button>
      </div>
    </div>
  );
}
