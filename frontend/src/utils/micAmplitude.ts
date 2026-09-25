/**
 * Real microphone amplitude for the radar waveform.
 * Nothing is recorded or uploaded — only the live RMS level is read from the analyser.
 */
import { useEffect, useRef, useState } from 'react';

export type MicState = 'off' | 'requesting' | 'live' | 'denied' | 'unsupported';

export const useMicAmplitude = (active: boolean) => {
  const [state, setState] = useState<MicState>('off');
  const levelRef = useRef(0);

  useEffect(() => {
    if (!active) {
      setState('off');
      levelRef.current = 0;
      return;
    }

    if (!navigator.mediaDevices?.getUserMedia || typeof AudioContext === 'undefined') {
      setState('unsupported');
      return;
    }

    let stream: MediaStream | null = null;
    let ctx: AudioContext | null = null;
    let raf = 0;
    let cancelled = false;

    setState('requesting');

    navigator.mediaDevices
      .getUserMedia({ audio: true })
      .then((s) => {
        if (cancelled) {
          s.getTracks().forEach((t) => t.stop());
          return;
        }
        stream = s;
        ctx = new AudioContext();
        const source = ctx.createMediaStreamSource(s);
        const analyser = ctx.createAnalyser();
        analyser.fftSize = 1024;
        source.connect(analyser);
        const buf = new Float32Array(analyser.fftSize);
        setState('live');

        const tick = () => {
          analyser.getFloatTimeDomainData(buf);
          let sum = 0;
          for (let i = 0; i < buf.length; i += 1) sum += buf[i] * buf[i];
          const rms = Math.sqrt(sum / buf.length);
          // Smooth and normalise into a usable 0..1 range.
          levelRef.current = levelRef.current * 0.7 + Math.min(1, rms * 6) * 0.3;
          raf = requestAnimationFrame(tick);
        };
        tick();
      })
      .catch(() => {
        if (!cancelled) setState('denied');
      });

    return () => {
      cancelled = true;
      cancelAnimationFrame(raf);
      stream?.getTracks().forEach((t) => t.stop());
      ctx?.close();
      levelRef.current = 0;
    };
  }, [active]);

  return { state, levelRef };
};
