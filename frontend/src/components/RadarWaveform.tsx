import React, { useEffect, useRef } from 'react';
import { useArtworkBox } from '../utils/artwork';

export type WaveState = 'idle' | 'listening' | 'speaking' | 'thinking' | 'blocked';

const IMG_W = 1672;
const IMG_H = 941;

interface RadarWaveformProps {
  /** Radar centre + horizontal reach of the artwork's blue centre line, in artwork pixels. */
  cx: number;
  cy: number;
  halfLen: number;
  state: WaveState;
  /** Live mic level (0..1) when the user granted microphone access. */
  levelRef?: React.MutableRefObject<number>;
}

const HEIGHT = 150; // artwork px of vertical room for the waveform

/**
 * Live waveform drawn exactly over the artwork's existing blue centre line.
 * The background artwork is never modified — this is a transparent canvas overlay.
 */
export const RadarWaveform: React.FC<RadarWaveformProps> = ({
  cx,
  cy,
  halfLen,
  state,
  levelRef,
}) => {
  const box = useArtworkBox(IMG_W, IMG_H, 1);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const stateRef = useRef<WaveState>(state);
  const blockedAtRef = useRef(0);

  useEffect(() => {
    if (state === 'blocked') blockedAtRef.current = performance.now();
    stateRef.current = state;
  }, [state]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let raf = 0;
    let energy = 0;

    const render = (time: number) => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const cssW = canvas.clientWidth;
      const cssH = canvas.clientHeight;
      if (canvas.width !== cssW * dpr || canvas.height !== cssH * dpr) {
        canvas.width = Math.max(1, Math.round(cssW * dpr));
        canvas.height = Math.max(1, Math.round(cssH * dpr));
      }
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, cssW, cssH);

      const s = stateRef.current;
      const mic = levelRef?.current ?? 0;
      const t = time / 1000;

      let target = 0.05;
      if (s === 'listening') target = 0.16 + mic * 0.75;
      else if (s === 'speaking') target = 0.5 + Math.abs(Math.sin(t * 2.2)) * 0.28;
      else if (s === 'thinking') target = 0.2;
      else if (s === 'blocked') target = 0.34;
      energy += (target - energy) * 0.12;

      const mid = cssH / 2;
      const maxAmp = (cssH / 2) * 0.82;
      const blockedFade = s === 'blocked' ? Math.max(0, 1 - (time - blockedAtRef.current) / 900) : 0;

      const core = s === 'blocked' && blockedFade > 0 ? '255, 70, 96' : '110, 190, 255';
      const halo = s === 'blocked' && blockedFade > 0 ? '255, 40, 70' : '47, 124, 255';

      const points: [number, number][] = [];
      const steps = Math.max(80, Math.round(cssW / 2));
      for (let i = 0; i <= steps; i += 1) {
        const p = i / steps; // 0..1 across the line
        const x = p * cssW;
        // Taper to zero at both ends so the overlay fuses into the artwork's line.
        const envelope = Math.pow(Math.sin(Math.PI * p), 1.6);

        let wave =
          Math.sin(p * 26 - t * 5.2) * 0.55 +
          Math.sin(p * 47 + t * 3.1) * 0.28 +
          Math.sin(p * 11 - t * 1.7) * 0.22;

        if (s === 'thinking') {
          // Slow scanning pulse travelling along the line.
          const head = (t * 0.32) % 1;
          const d = Math.abs(p - head);
          const packet = Math.exp(-(d * d) / 0.004);
          wave = wave * 0.18 + packet * Math.sin(t * 22) * 1.1;
        } else if (s === 'idle') {
          wave *= 0.35;
        }

        const y = mid - wave * envelope * maxAmp * energy;
        points.push([x, y]);
      }

      const drawPath = () => {
        ctx.beginPath();
        ctx.moveTo(points[0][0], points[0][1]);
        for (let i = 1; i < points.length; i += 1) ctx.lineTo(points[i][0], points[i][1]);
      };

      ctx.lineCap = 'round';

      // Wide soft halo
      ctx.globalCompositeOperation = 'lighter';
      drawPath();
      ctx.strokeStyle = `rgba(${halo}, ${0.16 + energy * 0.2})`;
      ctx.lineWidth = 7;
      ctx.shadowColor = `rgba(${halo}, 0.75)`;
      ctx.shadowBlur = 22;
      ctx.stroke();

      // Bright core
      drawPath();
      ctx.strokeStyle = `rgba(${core}, ${0.7 + energy * 0.3})`;
      ctx.lineWidth = 1.7;
      ctx.shadowColor = `rgba(${core}, 0.9)`;
      ctx.shadowBlur = 10;
      ctx.stroke();

      // Centre hotspot, matching the artwork's core glow
      const g = ctx.createRadialGradient(cssW / 2, mid, 0, cssW / 2, mid, 26);
      g.addColorStop(0, `rgba(255,255,255,${0.22 + energy * 0.4})`);
      g.addColorStop(1, 'rgba(255,255,255,0)');
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(cssW / 2, mid, 26, 0, Math.PI * 2);
      ctx.fill();

      ctx.shadowBlur = 0;
      ctx.globalCompositeOperation = 'source-over';

      raf = requestAnimationFrame(render);
    };

    raf = requestAnimationFrame(render);
    return () => cancelAnimationFrame(raf);
  }, [levelRef]);

  const left = box.left + ((cx - halfLen) / IMG_W) * box.w;
  const top = box.top + ((cy - HEIGHT / 2) / IMG_H) * box.h;
  const width = ((halfLen * 2) / IMG_W) * box.w;
  const height = (HEIGHT / IMG_H) * box.h;

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      className="absolute pointer-events-none select-none"
      style={{ left, top, width, height, zIndex: 10 }}
      data-testid="radar-waveform"
    />
  );
};
