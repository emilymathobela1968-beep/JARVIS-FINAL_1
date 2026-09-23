import React, { useEffect, useRef, useState, useCallback } from 'react';
import { VoiceState } from '../types';
import { soundFX } from '../utils/audio';

interface ArcReactorProps {
  state: VoiceState;
  onClick?: () => void;
}

export const ArcReactorVisualizer: React.FC<ArcReactorProps> = ({ state, onClick }) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [isHovered, setIsHovered] = useState(false);
  const animFrameRef = useRef<number | null>(null);
  const phaseRef = useRef<number>(0);
  const waveEnergyRef = useRef<number>(0.2);

  const isBlue = state === 'LISTENING' || state === 'SPEAKING' || state === 'THINKING' || state === 'EXECUTING';

  // Smooth audio wave animation on HTML5 Canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let localPhase = 0;

    const render = () => {
      localPhase += isBlue ? 0.06 : 0.015;
      phaseRef.current = localPhase;

      // Target wave energy based on state
      let targetEnergy = 0.12;
      if (state === 'LISTENING') targetEnergy = 0.55;
      if (state === 'SPEAKING') targetEnergy = 0.85;
      if (state === 'THINKING') targetEnergy = 0.45;
      if (state === 'OFFLINE') targetEnergy = isHovered ? 0.2 : 0.08;

      waveEnergyRef.current += (targetEnergy - waveEnergyRef.current) * 0.1;
      const energy = waveEnergyRef.current;

      const width = canvas.width;
      const height = canvas.height;
      const centerY = height / 2;

      ctx.clearRect(0, 0, width, height);

      // Colors based on current mode
      // Standby: Deep Maroon / Crimson Red
      // Active: Electric Blue / Neon Cyan
      const mainHue = isBlue ? '195, 100%, 55%' : '350, 78%, 45%';
      const glowHue = isBlue ? '190, 100%, 70%' : '345, 82%, 58%';

      // 1. Draw central anamorphic horizon laser streak
      const streakGrad = ctx.createLinearGradient(0, centerY, width, centerY);
      if (isBlue) {
        streakGrad.addColorStop(0, 'rgba(0, 210, 255, 0)');
        streakGrad.addColorStop(0.2, 'rgba(0, 210, 255, 0.15)');
        streakGrad.addColorStop(0.5, 'rgba(255, 255, 255, 0.9)');
        streakGrad.addColorStop(0.8, 'rgba(0, 210, 255, 0.15)');
        streakGrad.addColorStop(1, 'rgba(0, 210, 255, 0)');
      } else {
        streakGrad.addColorStop(0, 'rgba(159, 18, 57, 0)');
        streakGrad.addColorStop(0.2, 'rgba(159, 18, 57, 0.2)');
        streakGrad.addColorStop(0.5, 'rgba(251, 113, 133, 0.85)');
        streakGrad.addColorStop(0.8, 'rgba(159, 18, 57, 0.2)');
        streakGrad.addColorStop(1, 'rgba(159, 18, 57, 0)');
      }
      ctx.fillStyle = streakGrad;
      ctx.fillRect(10, centerY - 1, width - 20, 2);

      // 2. Draw multi-layered voice waves
      const waveCount = isBlue ? 3 : 1;

      for (let w = 0; w < waveCount; w++) {
        ctx.beginPath();
        const freqOffset = w * 0.45;
        const phaseOffset = w * 1.2;
        const waveAlpha = w === 0 ? 0.95 : 0.45;
        const maxAmp = (isBlue ? 36 : 14) * energy * (w === 0 ? 1 : 0.7);

        ctx.strokeStyle = w === 0 
          ? (isBlue ? 'rgba(255, 255, 255, 0.95)' : 'rgba(253, 164, 175, 0.9)') 
          : `hsla(${isBlue ? '195, 100%, 65%' : '345, 80%, 50%'}, ${waveAlpha})`;
        ctx.lineWidth = w === 0 ? 2.5 : 1.5;
        ctx.shadowColor = `hsla(${glowHue}, 0.8)`;
        ctx.shadowBlur = isBlue ? 12 : 6;

        for (let x = 30; x <= width - 30; x += 3) {
          // Envelope: taper at ends, maximum at center
          const normX = (x - 30) / (width - 60); // 0 to 1
          const envelope = Math.sin(normX * Math.PI);

          const harmonic1 = Math.sin((normX * 12) + (localPhase * 2.2) + phaseOffset);
          const harmonic2 = Math.sin((normX * 24) - (localPhase * 1.5) + freqOffset);
          const harmonic3 = Math.cos((normX * 6) + localPhase);

          const yOffset = (harmonic1 * 0.6 + harmonic2 * 0.3 + harmonic3 * 0.1) * maxAmp * envelope;
          const y = centerY + yOffset;

          if (x === 30) {
            ctx.moveTo(x, y);
          } else {
            ctx.lineTo(x, y);
          }
        }
        ctx.stroke();
      }

      // Reset shadow
      ctx.shadowBlur = 0;

      // 3. Circular frequency equalizer dots when blue/active
      if (isBlue) {
        const radius = 125;
        const barCount = 36;
        const cx = width / 2;
        const cy = height / 2;

        ctx.fillStyle = 'rgba(56, 189, 248, 0.7)';
        for (let i = 0; i < barCount; i++) {
          const angle = (i / barCount) * Math.PI * 2 + localPhase * 0.2;
          const barEnergy = Math.abs(Math.sin((i * 0.8) + (localPhase * 3))) * energy;
          const dotRadius = 1.2 + barEnergy * 2.4;
          const dist = radius + barEnergy * 14;

          const bx = cx + Math.cos(angle) * dist;
          const by = cy + Math.sin(angle) * dist;

          ctx.beginPath();
          ctx.arc(bx, by, dotRadius, 0, Math.PI * 2);
          ctx.fill();
        }
      }

      animFrameRef.current = requestAnimationFrame(render);
    };

    render();

    return () => {
      if (animFrameRef.current) {
        cancelAnimationFrame(animFrameRef.current);
      }
    };
  }, [isBlue, state, isHovered]);

  const handleClick = useCallback(() => {
    if (!isBlue) {
      soundFX.playIgnite();
    } else {
      soundFX.playPowerDown();
    }
    onClick?.();
  }, [isBlue, onClick]);

  return (
    <div className="relative flex flex-col items-center justify-center select-none py-2">
      {/* Outer Glow Disc Atmosphere */}
      <div 
        className={`absolute w-[440px] sm:w-[500px] h-[440px] sm:h-[500px] rounded-full blur-3xl pointer-events-none transition-all duration-1000 ${
          isBlue
            ? 'bg-cyan-500/25 shadow-[0_0_120px_rgba(56,189,248,0.3)]'
            : isHovered
            ? 'bg-rose-900/30 shadow-[0_0_80px_rgba(225,29,72,0.25)]'
            : 'bg-rose-950/20 shadow-[0_0_60px_rgba(159,18,57,0.15)]'
        }`}
      />

      {/* Main Interactive Round Chassis */}
      <div
        id="cinematic-arc-reactor"
        onClick={handleClick}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className="relative w-[340px] sm:w-[410px] h-[340px] sm:h-[410px] cursor-pointer group flex items-center justify-center transition-transform duration-500 active:scale-95"
        title={isBlue ? "Click to deactivate / enter standby" : "Click to activate JARVIS voice"}
      >
        {/* SVG Concentric Rings & Mechanical Markings */}
        <svg 
          className="absolute inset-0 w-full h-full pointer-events-none" 
          viewBox="0 0 420 420"
        >
          <defs>
            {/* Gradients */}
            <radialGradient id="maroonChamber" cx="50%" cy="50%" r="50%">
              <stop offset="0%" stopColor="#2e050b" stopOpacity="0.95" />
              <stop offset="60%" stopColor="#1a0306" stopOpacity="0.98" />
              <stop offset="100%" stopColor="#0a0103" stopOpacity="1" />
            </radialGradient>

            <radialGradient id="blueChamber" cx="50%" cy="50%" r="50%">
              <stop offset="0%" stopColor="#042f4e" stopOpacity="0.95" />
              <stop offset="60%" stopColor="#021a2d" stopOpacity="0.98" />
              <stop offset="100%" stopColor="#010a13" stopOpacity="1" />
            </radialGradient>

            <linearGradient id="maroonRingGrad" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#be123c" />
              <stop offset="50%" stopColor="#881337" />
              <stop offset="100%" stopColor="#4c0519" />
            </linearGradient>

            <linearGradient id="blueRingGrad" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#38bdf8" />
              <stop offset="50%" stopColor="#0284c7" />
              <stop offset="100%" stopColor="#0369a1" />
            </linearGradient>
          </defs>

          {/* Ring 1 - Deep Chamber Backdrop Fill */}
          <circle
            cx="210"
            cy="210"
            r="190"
            fill={isBlue ? "url(#blueChamber)" : "url(#maroonChamber)"}
            className="transition-all duration-700"
          />

          {/* Ring 2 - Outer Chassis Boundary */}
          <circle
            cx="210"
            cy="210"
            r="195"
            fill="none"
            stroke={isBlue ? "rgba(56, 189, 248, 0.35)" : "rgba(190, 18, 60, 0.35)"}
            strokeWidth="1.5"
            className="transition-colors duration-700"
          />

          {/* Ring 3 - Outer Calibrated Dial with Tick Marks (Slow Rotation) */}
          <g 
            className="transition-all duration-700"
            style={{ 
              transformOrigin: '210px 210px',
              animation: isBlue ? 'spin 40s linear infinite' : 'spin 90s linear infinite'
            }}
          >
            <circle
              cx="210"
              cy="210"
              r="182"
              fill="none"
              stroke={isBlue ? "rgba(56, 189, 248, 0.45)" : "rgba(225, 29, 72, 0.4)"}
              strokeWidth="2"
              strokeDasharray="4 16"
            />
            {/* Cardinal Quadrant Notches */}
            <circle cx="210" cy="28" r="3.5" fill={isBlue ? "#38bdf8" : "#f43f5e"} />
            <circle cx="392" cy="210" r="3.5" fill={isBlue ? "#38bdf8" : "#f43f5e"} />
            <circle cx="210" cy="392" r="3.5" fill={isBlue ? "#38bdf8" : "#f43f5e"} />
            <circle cx="28" cy="210" r="3.5" fill={isBlue ? "#38bdf8" : "#f43f5e"} />
          </g>

          {/* Ring 4 - Counter-Rotating Segmented Dial */}
          <g 
            className="transition-all duration-700"
            style={{ 
              transformOrigin: '210px 210px',
              animation: isBlue ? 'reverseSpin 30s linear infinite' : 'reverseSpin 75s linear infinite'
            }}
          >
            <circle
              cx="210"
              cy="210"
              r="165"
              fill="none"
              stroke={isBlue ? "url(#blueRingGrad)" : "url(#maroonRingGrad)"}
              strokeWidth="3"
              strokeDasharray="45 15 15 15 90 20"
              opacity={isBlue ? "0.9" : "0.75"}
            />
          </g>

          {/* Ring 5 - Inner Heavy Metallic Bezel */}
          <circle
            cx="210"
            cy="210"
            r="140"
            fill="none"
            stroke={isBlue ? "rgba(56, 189, 248, 0.6)" : "rgba(159, 18, 57, 0.6)"}
            strokeWidth="2"
            className="transition-colors duration-700"
          />

          {/* Ring 6 - Arc Reactor Core Shield */}
          <circle
            cx="210"
            cy="210"
            r="110"
            fill="none"
            stroke={isBlue ? "#00e5ff" : "#e11d48"}
            strokeWidth="3.5"
            strokeDasharray="70 8 30 8"
            className="transition-all duration-700"
            style={{
              filter: isBlue ? "drop-shadow(0 0 10px rgba(0, 229, 255, 0.8))" : "drop-shadow(0 0 8px rgba(225, 29, 72, 0.6))"
            }}
          />

          {/* Ring 7 - Innermost Glowing Plasma Ring */}
          <circle
            cx="210"
            cy="210"
            r="75"
            fill={isBlue ? "rgba(3, 105, 161, 0.25)" : "rgba(136, 19, 55, 0.25)"}
            stroke={isBlue ? "rgba(186, 230, 253, 0.85)" : "rgba(254, 205, 211, 0.75)"}
            strokeWidth="1.5"
            className="transition-all duration-700"
          />

          {/* Central Reactor Core Bulb */}
          <circle
            cx="210"
            cy="210"
            r="28"
            fill={isBlue ? "#0284c7" : "#881337"}
            className="transition-all duration-700"
            style={{
              filter: isBlue 
                ? "drop-shadow(0 0 18px rgba(56, 189, 248, 0.95))" 
                : "drop-shadow(0 0 14px rgba(225, 29, 72, 0.8))"
            }}
          />
          <circle
            cx="210"
            cy="210"
            r="12"
            fill={isBlue ? "#f0f9ff" : "#ffe4e6"}
            className="transition-all duration-700"
          />
        </svg>

        {/* Overlay Canvas for Real-Time Fluid Voice Wave */}
        <canvas
          ref={canvasRef}
          width={400}
          height={400}
          className="absolute inset-0 w-full h-full pointer-events-none z-10"
        />

        {/* Floating Tap Directive Pill Hint */}
        <div 
          className={`absolute bottom-6 px-3 py-1 rounded-full text-[10px] sm:text-[11px] font-mono-jarvis tracking-widest uppercase transition-all duration-500 z-20 ${
            isBlue
              ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-400/50 shadow-[0_0_12px_rgba(56,189,248,0.4)]'
              : 'bg-rose-950/70 text-rose-300/80 border border-rose-900/60 group-hover:border-rose-600 group-hover:text-rose-200'
          }`}
        >
          {isBlue ? '● ACTIVE VOICE STREAM' : 'ENGAGE JARVIS'}
        </div>
      </div>

      {/* Cinematic Status Caption */}
      <div className="mt-4 flex items-center gap-3 select-none">
        <span 
          className={`w-2 h-2 rounded-full transition-all duration-500 ${
            isBlue 
              ? 'bg-cyan-400 shadow-[0_0_10px_#38bdf8] animate-pulse' 
              : 'bg-rose-600 shadow-[0_0_8px_#e11d48]'
          }`} 
        />
        <span className="font-mono-jarvis text-xs sm:text-sm tracking-[0.25em] font-medium text-neutral-300 uppercase">
          {isBlue ? 'VOICE SYSTEM ONLINE' : 'STANDBY MODE'}
        </span>
        <span className="text-neutral-600">·</span>
        <span className="font-mono-jarvis text-xs text-neutral-400">
          {isBlue ? 'Awaiting utterance' : 'Click core to activate'}
        </span>
      </div>

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        @keyframes reverseSpin {
          from { transform: rotate(360deg); }
          to { transform: rotate(0deg); }
        }
      `}</style>
    </div>
  );
};
