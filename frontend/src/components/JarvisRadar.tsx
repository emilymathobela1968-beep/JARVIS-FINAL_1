import React, { useEffect, useRef } from 'react';
import { soundFX } from '../utils/audio';

interface JarvisRadarProps {
  size?: number;
  isActive?: boolean;
  onClick?: () => void;
  className?: string;
}

export const JarvisRadar: React.FC<JarvisRadarProps> = ({
  size = 420,
  isActive = true,
  onClick,
  className = '',
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animRef = useRef<number | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let phase = 0;

    const render = () => {
      phase += 0.04;
      const w = canvas.width;
      const h = canvas.height;
      const cy = h / 2;
      const cx = w / 2;

      ctx.clearRect(0, 0, w, h);

      // Horizontal cyan laser baseline streak
      const streakGrad = ctx.createLinearGradient(10, cy, w - 10, cy);
      streakGrad.addColorStop(0, 'rgba(0, 210, 255, 0)');
      streakGrad.addColorStop(0.2, 'rgba(0, 210, 255, 0.25)');
      streakGrad.addColorStop(0.5, 'rgba(255, 255, 255, 0.95)');
      streakGrad.addColorStop(0.8, 'rgba(0, 210, 255, 0.25)');
      streakGrad.addColorStop(1, 'rgba(0, 210, 255, 0)');

      ctx.fillStyle = streakGrad;
      ctx.fillRect(16, cy - 1, w - 32, 2);

      // 3 Layers of undulating glowing cyan audio/sine wave filaments
      const waveLayers = [
        { freq: 0.022, speed: 2.4, amp: 14, color: 'rgba(56, 189, 248, 0.95)', width: 2.2 },
        { freq: 0.038, speed: -1.8, amp: 9, color: 'rgba(0, 229, 255, 0.65)', width: 1.5 },
        { freq: 0.016, speed: 1.2, amp: 18, color: 'rgba(125, 211, 252, 0.4)', width: 1.2 },
      ];

      for (const layer of waveLayers) {
        ctx.beginPath();
        ctx.strokeStyle = layer.color;
        ctx.lineWidth = layer.width;
        ctx.shadowColor = 'rgba(56, 189, 248, 0.8)';
        ctx.shadowBlur = 10;

        const leftBound = 24;
        const rightBound = w - 24;

        for (let x = leftBound; x <= rightBound; x += 3) {
          const normX = (x - leftBound) / (rightBound - leftBound); // 0 to 1
          const envelope = Math.sin(normX * Math.PI); // Envelope peak at center

          const yOffset = Math.sin((x * layer.freq) + (phase * layer.speed)) * layer.amp * envelope;
          const y = cy + yOffset;

          if (x === leftBound) {
            ctx.moveTo(x, y);
          } else {
            ctx.lineTo(x, y);
          }
        }
        ctx.stroke();
      }

      ctx.shadowBlur = 0;

      // Left & right cyan flare end-nodes
      const leftNodeX = 40;
      const rightNodeX = w - 40;

      // Draw end flares
      [leftNodeX, rightNodeX].forEach((nx) => {
        const glow = ctx.createRadialGradient(nx, cy, 0, nx, cy, 14);
        glow.addColorStop(0, 'rgba(255, 255, 255, 1)');
        glow.addColorStop(0.3, 'rgba(56, 189, 248, 0.9)');
        glow.addColorStop(1, 'rgba(0, 229, 255, 0)');
        ctx.fillStyle = glow;
        ctx.beginPath();
        ctx.arc(nx, cy, 14, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(nx, cy, 2.5, 0, Math.PI * 2);
        ctx.fill();
      });

      // Center sphere luminous glow
      const centerGlow = ctx.createRadialGradient(cx, cy, 0, cx, cy, 32);
      centerGlow.addColorStop(0, 'rgba(255, 255, 255, 1)');
      centerGlow.addColorStop(0.35, 'rgba(0, 229, 255, 0.95)');
      centerGlow.addColorStop(0.7, 'rgba(2, 132, 199, 0.4)');
      centerGlow.addColorStop(1, 'rgba(2, 132, 199, 0)');

      ctx.fillStyle = centerGlow;
      ctx.beginPath();
      ctx.arc(cx, cy, 32, 0, Math.PI * 2);
      ctx.fill();

      // Core electric dot
      ctx.fillStyle = '#f0f9ff';
      ctx.beginPath();
      ctx.arc(cx, cy, 8, 0, Math.PI * 2);
      ctx.fill();

      animRef.current = requestAnimationFrame(render);
    };

    render();

    return () => {
      if (animRef.current) {
        cancelAnimationFrame(animRef.current);
      }
    };
  }, []);

  const handleClick = () => {
    soundFX.playAcknowledge();
    onClick?.();
  };

  return (
    <div 
      className={`relative flex items-center justify-center select-none cursor-pointer group ${className}`}
      style={{ width: size, height: size }}
      onClick={handleClick}
    >
      {/* Outer ambient red glow */}
      <div 
        className="absolute inset-4 rounded-full opacity-45 pointer-events-none blur-3xl transition-opacity duration-700"
        style={{
          background: 'radial-gradient(circle, rgba(225, 29, 72, 0.3) 0%, rgba(159, 18, 57, 0.1) 65%, transparent 100%)',
        }}
      />

      {/* SVG Concentric Mechanical Rings & Calibrated Dial */}
      <svg 
        className="absolute inset-0 w-full h-full pointer-events-none" 
        viewBox="0 0 420 420"
      >
        <defs>
          <linearGradient id="neonRedGrad" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#ff2a4b" />
            <stop offset="50%" stopColor="#e11d48" />
            <stop offset="100%" stopColor="#991b1b" />
          </linearGradient>

          <filter id="neonRedGlow" x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="3.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* Cardinal crosshair hairlines (0, 90, 180, 270 deg) */}
        <line x1="210" y1="20" x2="210" y2="400" stroke="rgba(225, 29, 72, 0.3)" strokeWidth="0.75" strokeDasharray="4 6" />
        <line x1="20" y1="210" x2="400" y2="210" stroke="rgba(56, 189, 248, 0.3)" strokeWidth="0.75" strokeDasharray="4 6" />

        {/* Cardinal orientation markers (dots) */}
        <circle cx="210" cy="52" r="2.5" fill="#ef4444" />
        <circle cx="210" cy="368" r="2.5" fill="#ef4444" />
        <circle cx="52" cy="210" r="2.5" fill="#38bdf8" />
        <circle cx="368" cy="210" r="2.5" fill="#38bdf8" />

        {/* Track 1: Outer calibrated perimeter dial with tick marks */}
        <g style={{ transformOrigin: '210px 210px', animation: 'spin 120s linear infinite' }}>
          <circle
            cx="210"
            cy="210"
            r="194"
            fill="none"
            stroke="rgba(255, 255, 255, 0.12)"
            strokeWidth="1"
          />
          <circle
            cx="210"
            cy="210"
            r="190"
            fill="none"
            stroke="rgba(239, 68, 68, 0.45)"
            strokeWidth="1.5"
            strokeDasharray="2 12"
          />
        </g>

        {/* Track 2: Outer glowing red segmented arc chassis */}
        <g style={{ transformOrigin: '210px 210px', animation: 'reverseSpin 80s linear infinite' }}>
          <circle
            cx="210"
            cy="210"
            r="170"
            fill="none"
            stroke="url(#neonRedGrad)"
            strokeWidth="2.5"
            strokeDasharray="50 35 15 20 80 40"
            filter="url(#neonRedGlow)"
            opacity="0.85"
          />
          <circle
            cx="210"
            cy="210"
            r="156"
            fill="none"
            stroke="rgba(225, 29, 72, 0.4)"
            strokeWidth="1"
            strokeDasharray="4 8"
          />
        </g>

        {/* Track 3: Middle heavy concentric red arcs (Prominent in screenshot) */}
        <g style={{ transformOrigin: '210px 210px', animation: 'spin 45s linear infinite' }}>
          <circle
            cx="210"
            cy="210"
            r="128"
            fill="none"
            stroke="#ff2a4b"
            strokeWidth="4"
            strokeDasharray="65 25 35 25"
            filter="url(#neonRedGlow)"
          />
          <circle
            cx="210"
            cy="210"
            r="114"
            fill="none"
            stroke="rgba(255, 42, 75, 0.6)"
            strokeWidth="1.5"
            strokeDasharray="12 12"
          />
        </g>

        {/* Track 4: Inner glowing red containment ring */}
        <circle
          cx="210"
          cy="210"
          r="86"
          fill="none"
          stroke="#ef4444"
          strokeWidth="2"
          strokeDasharray="8 6"
          filter="url(#neonRedGlow)"
          opacity="0.9"
        />

        {/* Track 5: Center dark chamber base */}
        <circle
          cx="210"
          cy="210"
          r="64"
          fill="#030712"
          stroke="rgba(56, 189, 248, 0.4)"
          strokeWidth="1"
        />
      </svg>

      {/* HTML5 Canvas overlay rendering the undulating cyan voice wave filaments */}
      <canvas
        ref={canvasRef}
        width={420}
        height={420}
        className="absolute inset-0 w-full h-full pointer-events-none z-10"
      />

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
