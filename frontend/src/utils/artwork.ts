import { useEffect, useState } from 'react';
import type { CSSProperties } from 'react';

export type ArtworkBox = { left: number; top: number; w: number; h: number; scale: number };
export type ArtworkRect = { x: number; y: number; w: number; h: number };

/**
 * Presentation scale of supplied artwork relative to a full cover fit.
 * Slightly pulled back so the composition reads cinematic rather than zoomed-in.
 */
export const FOREGROUND_SCALE = 0.907;

/** Measured rendered rectangle of the sharp foreground artwork. */
export const useArtworkBox = (imgW: number, imgH: number, scaleFactor = FOREGROUND_SCALE): ArtworkBox => {
  const [box, setBox] = useState<ArtworkBox>({ left: 0, top: 0, w: imgW, h: imgH, scale: 1 });

  useEffect(() => {
    const measure = () => {
      const vw = window.innerWidth;
      const vh = window.innerHeight;
      const scale = Math.max(vw / imgW, vh / imgH) * scaleFactor;
      const w = imgW * scale;
      const h = imgH * scale;
      setBox({ left: (vw - w) / 2, top: (vh - h) / 2, w, h, scale });
    };
    measure();
    window.addEventListener('resize', measure);
    return () => window.removeEventListener('resize', measure);
  }, [imgW, imgH, scaleFactor]);

  return box;
};

/** Converts artwork-space pixels into a position inside the rendered artwork rectangle. */
export const placeIn = (r: ArtworkRect, imgW: number, imgH: number): CSSProperties => ({
  position: 'absolute',
  left: `${(r.x / imgW) * 100}%`,
  top: `${(r.y / imgH) * 100}%`,
  width: `${(r.w / imgW) * 100}%`,
  height: `${(r.h / imgH) * 100}%`,
});

/** Softens the foreground edge so it blends into the background fill layer. */
export const EDGE_FEATHER: CSSProperties = {
  maskImage:
    'linear-gradient(to right, transparent 0, #000 2.2%, #000 97.8%, transparent 100%), linear-gradient(to bottom, transparent 0, #000 2.6%, #000 97.4%, transparent 100%)',
  WebkitMaskImage:
    'linear-gradient(to right, transparent 0, #000 2.2%, #000 97.8%, transparent 100%), linear-gradient(to bottom, transparent 0, #000 2.6%, #000 97.4%, transparent 100%)',
  maskComposite: 'intersect',
  WebkitMaskComposite: 'source-in',
};

/** Background fill styling for the perimeter beyond the sharp artwork. */
export const FILL_LAYER: CSSProperties = {
  objectFit: 'cover',
  transform: 'scale(1.05)',
  filter: 'blur(26px) brightness(0.62) saturate(0.9)',
};
