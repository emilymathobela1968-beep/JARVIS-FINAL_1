import React, { useState, useEffect, useRef } from 'react';
import { soundFX } from '../../utils/audio';

interface BarehandsViewProps {
  onExecuteTool: (tool: string, params: string) => void;
}

type GestureType = 'open_palm' | 'pinch' | 'point' | 'fist' | 'frame';
type HologramModel = 'arc_core' | 'quantum_cube' | 'dna_helix' | 'spatial_notes';

export const BarehandsView: React.FC<BarehandsViewProps> = ({ onExecuteTool }) => {
  const [activeGesture, setActiveGesture] = useState<GestureType>('open_palm');
  const [activeModel, setActiveModel] = useState<HologramModel>('arc_core');
  const [scale, setScale] = useState(1.0);
  const [rotation, setRotation] = useState({ x: 15, y: 30 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [particlesActive, setParticlesActive] = useState(true);

  // Auto rotation when not dragging
  useEffect(() => {
    if (isDragging) return;
    const interval = setInterval(() => {
      setRotation((prev) => ({
        ...prev,
        y: (prev.y + 0.8) % 360,
      }));
    }, 40);
    return () => clearInterval(interval);
  }, [isDragging]);

  const handleGestureSelect = (g: GestureType) => {
    soundFX.playBeep(640, 0.05);
    setActiveGesture(g);
    if (g === 'pinch') {
      setScale((prev) => (prev > 1.4 ? 0.8 : prev + 0.2));
    } else if (g === 'fist') {
      setScale(1.0);
      setRotation({ x: 0, y: 0 });
    }
    onExecuteTool('barehands.gesture_event', `Gesture=${g}`);
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    setDragStart({ x: e.clientX, y: e.clientY });
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging) return;
    const dx = e.clientX - dragStart.x;
    const dy = e.clientY - dragStart.y;
    setRotation((prev) => ({
      x: Math.max(-60, Math.min(60, prev.x - dy * 0.4)),
      y: (prev.y + dx * 0.4) % 360,
    }));
    setDragStart({ x: e.clientX, y: e.clientY });
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  return (
    <div id="barehands-view" className="w-full max-w-5xl mx-auto space-y-6 select-none">
      {/* Title & Description */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl sm:text-3xl font-semibold text-[#F5FAFF]">Barehands Stage</h2>
          <span className="font-mono-jarvis text-xs px-3 py-1 bg-[#102B3A] border border-[#45BCFF]/40 text-[#55C7FF] rounded-full">
            SPATIAL GESTURE COMPUTING
          </span>
        </div>
        <p className="text-sm sm:text-base text-[#8CBFEA] max-w-3xl">
          Holographic spatial computing stage from the Barehands asset library. Manipulate objects using gestures or cursor tracking.
        </p>
      </div>

      {/* Control Bar: Gestures & Hologram Selector */}
      <div className="bg-[#081320]/90 border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">GESTURE:</span>
          {(['open_palm', 'pinch', 'point', 'fist', 'frame'] as GestureType[]).map((g) => (
            <button
              key={g}
              onClick={() => handleGestureSelect(g)}
              className={`px-3 py-1 rounded-lg text-xs font-mono-jarvis transition-all ${
                activeGesture === g
                  ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF] shadow-sm'
                  : 'bg-[#050E17] text-[#8CBFEA] border border-[#45BCFF]/20 hover:text-white'
              }`}
            >
              {g === 'open_palm' ? '🖐 Palm' : g === 'pinch' ? '🤏 Pinch (Zoom)' : g === 'point' ? '☝ Point' : g === 'fist' ? '✊ Fist (Reset)' : '👐 Frame'}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">MODEL:</span>
          {(['arc_core', 'quantum_cube', 'dna_helix', 'spatial_notes'] as HologramModel[]).map((m) => (
            <button
              key={m}
              onClick={() => {
                soundFX.playBeep(700, 0.04);
                setActiveModel(m);
              }}
              className={`px-2.5 py-1 rounded-lg text-xs font-mono-jarvis transition-all ${
                activeModel === m
                  ? 'bg-[#183E63] text-[#55C7FF] border border-[#55C7FF]'
                  : 'bg-[#050E17] text-[#8CBFEA] border border-[#45BCFF]/20 hover:text-white'
              }`}
            >
              {m.replace('_', ' ').toUpperCase()}
            </button>
          ))}
        </div>
      </div>

      {/* Holographic 3D Interactive Stage Canvas */}
      <div 
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        className="w-full h-[460px] bg-gradient-to-b from-[#02050A] via-[#040C16] to-[#010307] border border-[#45BCFF]/30 rounded-3xl relative overflow-hidden flex items-center justify-center cursor-grab active:cursor-grabbing shadow-[inset_0_0_80px_rgba(69,188,255,0.08)]"
      >
        {/* Holographic Grid Floor */}
        <div 
          className="absolute inset-x-0 bottom-0 h-48 opacity-25 pointer-events-none"
          style={{
            backgroundImage: 'linear-gradient(rgba(69,188,255,0.4) 1px, transparent 1px), linear-gradient(90deg, rgba(69,188,255,0.4) 1px, transparent 1px)',
            backgroundSize: '40px 40px',
            transform: 'perspective(500px) rotateX(65deg)',
            transformOrigin: 'bottom center',
          }}
        />

        {/* Ambient Stage Glow */}
        <div className="absolute w-[360px] h-[360px] rounded-full bg-cyan-500/10 blur-3xl pointer-events-none" />

        {/* Dynamic 3D CSS Model */}
        <div 
          className="relative transition-transform duration-75 pointer-events-none"
          style={{
            transform: `scale(${scale}) rotateX(${rotation.x}deg) rotateY(${rotation.y}deg)`,
            transformStyle: 'preserve-3d',
          }}
        >
          {activeModel === 'arc_core' && (
            <div className="w-48 h-48 relative flex items-center justify-center">
              <div className="absolute inset-0 rounded-full border-4 border-cyan-400/60 shadow-[0_0_30px_rgba(69,188,255,0.6)] animate-spin" style={{ animationDuration: '8s' }} />
              <div className="absolute inset-4 rounded-full border-2 border-dashed border-sky-300/80 animate-spin" style={{ animationDuration: '4s', animationDirection: 'reverse' }} />
              <div className="absolute inset-10 rounded-full bg-cyan-500/30 blur-md" />
              <div className="w-16 h-16 rounded-full bg-white shadow-[0_0_40px_#45BCFF] flex items-center justify-center font-mono-jarvis text-xs text-blue-900 font-bold">
                CORE
              </div>
            </div>
          )}

          {activeModel === 'quantum_cube' && (
            <div className="w-36 h-36 relative" style={{ transformStyle: 'preserve-3d' }}>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'translateZ(72px)' }}>FRONT</div>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'rotateY(180deg) translateZ(72px)' }}>BACK</div>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'rotateY(-90deg) translateZ(72px)' }}>LEFT</div>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'rotateY(90deg) translateZ(72px)' }}>RIGHT</div>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'rotateX(90deg) translateZ(72px)' }}>TOP</div>
              <div className="absolute inset-0 border-2 border-cyan-400 bg-cyan-900/30 flex items-center justify-center font-mono-jarvis text-xs text-cyan-200" style={{ transform: 'rotateX(-90deg) translateZ(72px)' }}>BOTTOM</div>
            </div>
          )}

          {activeModel === 'dna_helix' && (
            <div className="w-48 h-56 relative flex flex-col items-center justify-between">
              {Array.from({ length: 8 }, (_, i) => (
                <div 
                  key={i} 
                  className="w-40 h-1.5 bg-gradient-to-r from-cyan-400 via-sky-200 to-cyan-400 rounded-full shadow-[0_0_12px_#45BCFF]"
                  style={{ transform: `rotateY(${i * 45}deg)` }}
                />
              ))}
            </div>
          )}

          {activeModel === 'spatial_notes' && (
            <div className="grid grid-cols-2 gap-4 w-72">
              <div className="p-3 bg-amber-950/70 border border-amber-500/50 rounded-lg text-amber-200 text-xs font-mono-jarvis shadow-lg">
                <div className="font-bold border-b border-amber-500/30 pb-1 mb-1">NOTE 01</div>
                <div>Wire in Azure Realtime WebRTC audio endpoints.</div>
              </div>
              <div className="p-3 bg-cyan-950/70 border border-cyan-500/50 rounded-lg text-cyan-200 text-xs font-mono-jarvis shadow-lg">
                <div className="font-bold border-b border-cyan-500/30 pb-1 mb-1">NOTE 02</div>
                <div>Preserve governed dispatch across all operations.</div>
              </div>
            </div>
          )}
        </div>

        {/* HUD Overlay Indicators */}
        <div className="absolute top-4 left-4 font-mono-jarvis text-xs text-[#8CBFEA] space-y-1">
          <div>TRACKING: <strong className="text-emerald-400">ACTIVE (30 FPS)</strong></div>
          <div>ROTATION: X:{rotation.x.toFixed(0)}° Y:{rotation.y.toFixed(0)}°</div>
          <div>SCALE: {(scale * 100).toFixed(0)}%</div>
        </div>

        <div className="absolute bottom-4 right-4 font-mono-jarvis text-[11px] text-[#55C7FF] opacity-80">
          Drag to rotate • Click gestures above to manipulate
        </div>
      </div>
    </div>
  );
};
