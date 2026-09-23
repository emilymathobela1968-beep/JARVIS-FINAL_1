import React, { useEffect, useRef } from 'react';

interface JarvisArcOrbProps {
  size?: number;
  isListening?: boolean;
  intensity?: number; // 0 to 1
  className?: string;
}

export const JarvisArcOrb: React.FC<JarvisArcOrbProps> = ({
  size = 460,
  isListening = false,
  intensity = 0.8,
  className = '',
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animFrameRef = useRef<number | null>(null);
  const phaseRef = useRef<number>(0);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let running = true;

    const render = () => {
      if (!running) return;

      const width = canvas.width;
      const height = canvas.height;
      const cx = width / 2;
      const cy = height / 2;
      const baseRadius = (width * 0.42);

      ctx.clearRect(0, 0, width, height);
      phaseRef.current += isListening ? 0.08 : 0.025;
      const phase = phaseRef.current;

      // 1. HORIZONTAL LENS FLARE LINE (Spanning across behind the orb)
      const flareGrad = ctx.createLinearGradient(0, cy, width, cy);
      flareGrad.addColorStop(0, 'rgba(57, 184, 255, 0)');
      flareGrad.addColorStop(0.25, 'rgba(57, 184, 255, 0.15)');
      flareGrad.addColorStop(0.48, 'rgba(88, 215, 255, 0.7)');
      flareGrad.addColorStop(0.5, 'rgba(255, 255, 255, 0.95)');
      flareGrad.addColorStop(0.52, 'rgba(88, 215, 255, 0.7)');
      flareGrad.addColorStop(0.75, 'rgba(57, 184, 255, 0.15)');
      flareGrad.addColorStop(1, 'rgba(57, 184, 255, 0)');

      ctx.save();
      ctx.strokeStyle = flareGrad;
      ctx.lineWidth = 1.8;
      ctx.beginPath();
      ctx.moveTo(width * 0.05, cy);
      ctx.lineTo(width * 0.95, cy);
      ctx.stroke();

      // Soft flare halo around horizontal line
      ctx.strokeStyle = 'rgba(57, 184, 255, 0.08)';
      ctx.lineWidth = 10;
      ctx.beginPath();
      ctx.moveTo(width * 0.15, cy);
      ctx.lineTo(width * 0.85, cy);
      ctx.stroke();
      ctx.restore();

      // 2. OUTER GLOWING ELECTRIC BLUE RING
      ctx.save();
      ctx.beginPath();
      ctx.arc(cx, cy, baseRadius, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(57, 184, 255, 0.85)';
      ctx.lineWidth = 2.2;
      ctx.shadowColor = '#39B8FF';
      ctx.shadowBlur = 18;
      ctx.stroke();
      ctx.restore();

      // 3. CALIBRATED TICK RING (72 radial ticks with 4 cardinal points)
      ctx.save();
      const tickCount = 72;
      for (let i = 0; i < tickCount; i++) {
        const angle = (i * Math.PI * 2) / tickCount;
        const isCardinal = i % 18 === 0;
        const tickLength = isCardinal ? 14 : (i % 6 === 0 ? 8 : 4.5);
        const rInner = baseRadius - tickLength - 3;
        const rOuter = baseRadius - 3;

        const x1 = cx + Math.cos(angle) * rInner;
        const y1 = cy + Math.sin(angle) * rInner;
        const x2 = cx + Math.cos(angle) * rOuter;
        const y2 = cy + Math.sin(angle) * rOuter;

        ctx.strokeStyle = isCardinal 
          ? 'rgba(88, 215, 255, 0.95)' 
          : 'rgba(57, 184, 255, 0.45)';
        ctx.lineWidth = isCardinal ? 2 : 1;
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
      }
      ctx.restore();

      // 4. INNER BLUE CIRCULAR HOUSING / GLASS SPHERE RIM
      const sphereRadius = baseRadius * 0.78;
      ctx.save();
      // Outer rim
      ctx.beginPath();
      ctx.arc(cx, cy, sphereRadius, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(88, 215, 255, 0.9)';
      ctx.lineWidth = 2;
      ctx.shadowColor = '#39B8FF';
      ctx.shadowBlur = 12;
      ctx.stroke();

      // Sphere gradient glass interior
      const sphereGrad = ctx.createRadialGradient(
        cx, cy - sphereRadius * 0.3, sphereRadius * 0.1,
        cx, cy, sphereRadius
      );
      sphereGrad.addColorStop(0, 'rgba(15, 35, 65, 0.25)');
      sphereGrad.addColorStop(0.7, 'rgba(4, 10, 20, 0.65)');
      sphereGrad.addColorStop(1, 'rgba(2, 6, 12, 0.92)');
      ctx.fillStyle = sphereGrad;
      ctx.fill();

      // Specular crescent curve on top edge of sphere
      ctx.beginPath();
      ctx.arc(cx, cy, sphereRadius - 3, -Math.PI * 0.85, -Math.PI * 0.15);
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
      ctx.lineWidth = 1.5;
      ctx.stroke();
      ctx.restore();

      // 5. CONCENTRIC SEGMENTED RED TARGETING RING
      const redRadius = sphereRadius * 0.58;
      ctx.save();
      ctx.shadowColor = '#FF294D';
      ctx.shadowBlur = 14;

      // 4 Dash segments with gaps at cardinal points
      const quadrants = [
        { start: 0.12 * Math.PI, end: 0.38 * Math.PI },
        { start: 0.62 * Math.PI, end: 0.88 * Math.PI },
        { start: 1.12 * Math.PI, end: 1.38 * Math.PI },
        { start: 1.62 * Math.PI, end: 1.88 * Math.PI },
      ];

      quadrants.forEach((q) => {
        ctx.beginPath();
        ctx.arc(cx, cy, redRadius, q.start, q.end);
        ctx.strokeStyle = 'rgba(255, 41, 77, 0.92)';
        ctx.lineWidth = 3.2;
        ctx.stroke();
      });

      // Cardinal Red Glow Nodes
      const cardinalAngles = [0, Math.PI * 0.5, Math.PI, Math.PI * 1.5];
      cardinalAngles.forEach((a) => {
        const nx = cx + Math.cos(a) * redRadius;
        const ny = cy + Math.sin(a) * redRadius;
        ctx.beginPath();
        ctx.arc(nx, ny, 2.5, 0, Math.PI * 2);
        ctx.fillStyle = '#FFFFFF';
        ctx.fill();

        ctx.beginPath();
        ctx.arc(nx, ny, 4.5, 0, Math.PI * 2);
        ctx.fillStyle = 'rgba(255, 41, 77, 0.7)';
        ctx.fill();
      });
      ctx.restore();

      // 6. INNER CYAN SPHERICAL CORE
      const coreRadius = sphereRadius * 0.28;
      ctx.save();
      const coreGrad = ctx.createRadialGradient(cx, cy, 0, cx, cy, coreRadius * 1.6);
      coreGrad.addColorStop(0, '#FFFFFF');
      coreGrad.addColorStop(0.2, '#58D7FF');
      coreGrad.addColorStop(0.6, 'rgba(57, 184, 255, 0.5)');
      coreGrad.addColorStop(1, 'rgba(57, 184, 255, 0)');

      ctx.fillStyle = coreGrad;
      ctx.beginPath();
      ctx.arc(cx, cy, coreRadius * 1.6, 0, Math.PI * 2);
      ctx.fill();

      // Sharp central white node
      ctx.beginPath();
      ctx.arc(cx, cy, 4, 0, Math.PI * 2);
      ctx.fillStyle = '#FFFFFF';
      ctx.shadowColor = '#58D7FF';
      ctx.shadowBlur = 15;
      ctx.fill();
      ctx.restore();

      // 7. HORIZONTAL AUDIO / ENERGY WAVEFORM FILAMENTS
      ctx.save();
      const waveWidth = sphereRadius * 1.8;
      const startX = cx - waveWidth / 2;
      const endX = cx + waveWidth / 2;
      const numPoints = 80;

      // Draw primary waveform
      ctx.beginPath();
      ctx.strokeStyle = '#58D7FF';
      ctx.lineWidth = 1.8;
      ctx.shadowColor = '#39B8FF';
      ctx.shadowBlur = 10;

      for (let i = 0; i <= numPoints; i++) {
        const x = startX + (i / numPoints) * waveWidth;
        const normDistFromCenter = 1 - Math.abs((x - cx) / (waveWidth / 2));
        
        // Multi-frequency harmonic synthesis
        const waveAmp = (isListening ? 22 : 9) * normDistFromCenter;
        const freq1 = Math.sin(i * 0.45 + phase * 2.5);
        const freq2 = Math.cos(i * 0.85 - phase * 3.2);
        const freq3 = Math.sin(i * 1.25 + phase * 4.0);
        const yOffset = (freq1 * 0.5 + freq2 * 0.35 + freq3 * 0.15) * waveAmp;

        const y = cy + yOffset;
        if (i === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      }
      ctx.stroke();

      // Secondary subtle waveform harmonic
      ctx.beginPath();
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.65)';
      ctx.lineWidth = 1;
      for (let i = 0; i <= numPoints; i++) {
        const x = startX + (i / numPoints) * waveWidth;
        const normDistFromCenter = 1 - Math.abs((x - cx) / (waveWidth / 2));
        const waveAmp = (isListening ? 14 : 5) * normDistFromCenter;
        const freq = Math.sin(i * 0.7 - phase * 3.5);
        const y = cy + freq * waveAmp;
        if (i === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      }
      ctx.stroke();

      // Left and right wave anchor nodes
      [startX, endX].forEach((nodeX) => {
        ctx.beginPath();
        ctx.arc(nodeX, cy, 3, 0, Math.PI * 2);
        ctx.fillStyle = '#FFFFFF';
        ctx.shadowColor = '#58D7FF';
        ctx.shadowBlur = 8;
        ctx.fill();
      });

      ctx.restore();

      animFrameRef.current = requestAnimationFrame(render);
    };

    render();

    return () => {
      running = false;
      if (animFrameRef.current) {
        cancelAnimationFrame(animFrameRef.current);
      }
    };
  }, [isListening, intensity]);

  return (
    <div className={`relative flex items-center justify-center pointer-events-none select-none ${className}`}>
      <canvas
        ref={canvasRef}
        width={size}
        height={size}
        style={{ width: `${size}px`, height: `${size}px` }}
        className="max-w-full max-h-full"
      />
    </div>
  );
};
