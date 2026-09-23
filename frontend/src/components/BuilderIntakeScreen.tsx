import React, { useEffect, useRef, useState } from 'react';
import intakeArtwork from '../assets/images/jarvis_builder_intake.png';
import chromeLogo from '../assets/images/jarvis_logo_chrome.png';
import { AppCategory } from '../types';
import { useArtworkBox, placeIn, EDGE_FEATHER, FILL_LAYER, ArtworkRect } from '../utils/artwork';

const IMG_W = 1672;
const IMG_H = 941;

/** Control rects measured from the supplied intake artwork (panel assembly shifted down 33px). */
const RECT: Record<string, ArtworkRect> = {
  input: { x: 360, y: 579, w: 686, h: 48 },
  mic: { x: 1044, y: 635, w: 54, h: 56 },
  build: { x: 1128, y: 634, w: 182, h: 61 },
  web: { x: 526, y: 763, w: 120, h: 104 },
  mobile: { x: 775, y: 763, w: 120, h: 104 },
  ai: { x: 1023, y: 763, w: 120, h: 104 },
  webRule: { x: 566, y: 858, w: 42, h: 3 },
  mobileRule: { x: 814, y: 858, w: 42, h: 3 },
  aiRule: { x: 1062, y: 858, w: 42, h: 3 },
  status: { x: 360, y: 700, w: 700, h: 40 },
  logoMask: { x: 26, y: 26, w: 256, h: 68 },
  logo: { x: 40, y: 21, w: 250, h: 78 },
};

const place = (r: ArtworkRect) => placeIn(r, IMG_W, IMG_H);

interface BuilderIntakeScreenProps {
  onBuild: (directive: string, appType: AppCategory) => void;
}

export const BuilderIntakeScreen: React.FC<BuilderIntakeScreenProps> = ({ onBuild }) => {
  const [directive, setDirective] = useState('');
  const [appType, setAppType] = useState<AppCategory>('web');
  const [status, setStatus] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const box = useArtworkBox(IMG_W, IMG_H);
  const s = (v: number) => v * box.scale;

  useEffect(() => {
    if (!status) return;
    const t = setTimeout(() => setStatus(null), 4000);
    return () => clearTimeout(t);
  }, [status]);

  const submit = () => {
    const value = directive.trim();
    if (!value) return;
    onBuild(value, appType);
  };

  const selector = (type: AppCategory, rect: ArtworkRect, rule: ArtworkRect, label: string) => (
    <React.Fragment key={type}>
      <button
        type="button"
        onClick={() => setAppType(type)}
        style={place(rect)}
        className="cursor-pointer"
        aria-label={label}
        aria-pressed={appType === type}
        data-testid={`intake-select-${type}`}
      />
      {appType === type && (
        <div
          style={{
            ...place(rule),
            background: '#2F7CFF',
            boxShadow: '0 0 10px rgba(47, 124, 255, 0.8)',
            borderRadius: 2,
          }}
          className="pointer-events-none"
          data-testid={`intake-selected-${type}`}
        />
      )}
    </React.Fragment>
  );

  return (
    <div className="fixed inset-0 overflow-hidden bg-black" data-testid="builder-intake-screen">
      <img
        src={intakeArtwork}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full select-none pointer-events-none"
        style={FILL_LAYER}
      />

      <div
        className="absolute"
        style={{ left: box.left, top: box.top, width: box.w, height: box.h }}
        data-testid="intake-artwork-frame"
      >
        <img
          src={intakeArtwork}
          alt="JARVIS App Builder"
          draggable={false}
          className="absolute inset-0 w-full h-full select-none"
          style={{ objectFit: 'fill', ...EDGE_FEATHER }}
          data-testid="intake-artwork"
        />

        {/* Brand consistency: mask the baked plain wordmark and reuse the accepted
            chrome JARVIS identity from the Hero artwork. Nothing else is altered. */}
        <div style={{ ...place(RECT.logoMask), overflow: 'hidden' }} className="pointer-events-none">
          <div
            className="absolute"
            style={{
              inset: -s(20),
              backgroundImage: `url(${intakeArtwork})`,
              backgroundSize: `${box.w}px ${box.h}px`,
              backgroundPosition: `${-(s(RECT.logoMask.x) - s(20))}px ${-(s(RECT.logoMask.y) - s(20))}px`,
              filter: `blur(${Math.max(8, s(12))}px)`,
            }}
          />
        </div>
        <img
          src={chromeLogo}
          alt="JARVIS"
          draggable={false}
          style={{ ...place(RECT.logo) }}
          className="pointer-events-none select-none object-contain"
          data-testid="intake-chrome-logo"
        />

        {/* Application description field — the artwork's baked placeholder is
            masked by an aligned, blurred slice of the artwork itself. */}
        <div style={{ ...place(RECT.input), overflow: 'hidden' }}>
          <div
            className="absolute pointer-events-none"
            style={{
              inset: -s(24),
              backgroundImage: `url(${intakeArtwork})`,
              backgroundSize: `${box.w}px ${box.h}px`,
              backgroundPosition: `${-(s(RECT.input.x) - s(24))}px ${-(s(RECT.input.y) - s(24))}px`,
              filter: `blur(${Math.max(6, s(9))}px)`,
            }}
          />
          <input
            ref={inputRef}
            type="text"
            value={directive}
            onChange={(e) => setDirective(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                submit();
              }
            }}
            placeholder="Describe the application you want to build..."
            autoFocus
            className="absolute inset-0 w-full h-full bg-transparent border-0 outline-none font-sans placeholder-[#8EA1BA]"
            style={{
              paddingLeft: s(14),
              paddingRight: s(8),
              fontSize: s(23),
              lineHeight: 1.2,
              color: '#F5F8FF',
              caretColor: '#2F7CFF',
            }}
            data-testid="intake-input"
          />
        </div>

        <button
          type="button"
          onClick={() => setStatus('Voice not connected — realtime voice runtime pending integration')}
          style={place(RECT.mic)}
          className="rounded-full cursor-pointer transition-colors hover:bg-white/[0.06]"
          aria-label="Voice input"
          data-testid="intake-mic-button"
        />

        <button
          type="button"
          onClick={submit}
          style={place(RECT.build)}
          className="rounded-lg cursor-pointer transition-colors hover:bg-white/[0.08]"
          aria-label="Build"
          data-testid="intake-build-button"
        />

        {selector('web', RECT.web, RECT.webRule, 'Web App')}
        {selector('mobile', RECT.mobile, RECT.mobileRule, 'Mobile App')}
        {selector('ai', RECT.ai, RECT.aiRule, 'AI Model')}

        {status && (
          <div style={place(RECT.status)} className="flex items-start" data-testid="intake-status">
            <span style={{ color: '#8EA1BA', fontSize: s(14) }}>{status}</span>
          </div>
        )}
      </div>
    </div>
  );
};
