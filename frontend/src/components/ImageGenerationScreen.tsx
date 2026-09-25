import React, { useEffect, useRef, useState } from 'react';
import {
  Home,
  Sparkles,
  Download,
  RotateCw,
  Copy,
  Loader2,
  AlertTriangle,
  Ban,
  ImageIcon,
  Save,
} from 'lucide-react';
import { JarvisMenu } from './JarvisMenu';
import { absoluteUrl, generateImage, imageProviderStatus, ImageResult } from '../utils/api';
import workstationBg from '../assets/images/bg_c.png';

interface ImageGenerationScreenProps {
  onReturnHome: () => void;
  onOpenBuilder: () => void;
  onOpenComputer: () => void;
}

const STYLES = [
  { id: 'realistic', label: 'Realistic' },
  { id: 'cinematic', label: 'Cinematic' },
  { id: 'product', label: 'Product' },
  { id: 'social_post', label: 'Social Post' },
  { id: 'ui_mockup', label: 'UI Mockup' },
  { id: 'logo', label: 'Logo' },
  { id: 'freeform', label: 'Freeform' },
];

const RATIOS = ['1:1', '16:9', '9:16', '4:5'];

type Phase = 'idle' | 'generating' | 'completed' | 'failed' | 'blocked';

export const ImageGenerationScreen: React.FC<ImageGenerationScreenProps> = ({
  onReturnHome,
  onOpenBuilder,
  onOpenComputer,
}) => {
  const [prompt, setPrompt] = useState('');
  const [style, setStyle] = useState('realistic');
  const [ratio, setRatio] = useState('1:1');
  const [quality, setQuality] = useState<'standard' | 'high'>('standard');
  const [phase, setPhase] = useState<Phase>('idle');
  const [result, setResult] = useState<ImageResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [providerConnected, setProviderConnected] = useState<boolean | null>(null);
  const lastPromptRef = useRef('');

  useEffect(() => {
    imageProviderStatus()
      .then((s) => setProviderConnected(s.connected))
      .catch(() => setProviderConnected(null));
  }, []);

  useEffect(() => {
    if (!notice) return;
    const t = setTimeout(() => setNotice(null), 3000);
    return () => clearTimeout(t);
  }, [notice]);

  const run = async (usePrompt?: string) => {
    const text = (usePrompt ?? prompt).trim();
    if (!text || phase === 'generating') return;
    lastPromptRef.current = text;
    setPhase('generating');
    setError(null);
    setResult(null);

    const res = await generateImage({
      prompt: text,
      style,
      aspect_ratio: ratio,
      quality,
    });

    if (res.status === 'completed' && res.image_url) {
      setResult(res);
      setPhase('completed');
      return;
    }
    if (res.status === 'blocked') {
      setError(res.error || 'Image generation provider is not connected.');
      setPhase('blocked');
      return;
    }
    setError(res.error || 'Generation failed.');
    setPhase('failed');
  };

  const download = async () => {
    if (!result?.image_url) return;
    const res = await fetch(absoluteUrl(result.image_url));
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `jarvis-image-${result.id.slice(0, 8)}.png`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const smallBtn =
    'flex items-center gap-1.5 px-3 h-8 rounded-md text-[12px] text-[#9FB0C6] hover:text-[#EAF2FF] hover:bg-[rgba(47,124,255,0.16)] border border-[rgba(95,160,255,0.22)] transition-colors disabled:opacity-35 disabled:cursor-not-allowed';

  return (
    <div
      className="relative w-screen h-screen overflow-hidden text-[#F5F8FF] flex flex-col font-sans"
      data-testid="image-generation-screen"
    >
      <img
        src={workstationBg}
        alt=""
        aria-hidden
        draggable={false}
        className="absolute inset-0 w-full h-full object-cover select-none pointer-events-none"
      />

      <div className="relative z-20 shrink-0 flex items-center justify-between px-3 pt-[10px] pb-2">
        <button
          type="button"
          onClick={onReturnHome}
          className="flex items-center gap-2 px-3.5 h-10 rounded-lg text-[12px] tracking-[0.12em] text-[#DCE5F2] hover:text-white transition-all"
          style={{
            background: 'linear-gradient(180deg, rgba(10,22,44,0.66) 0%, rgba(6,13,26,0.66) 100%)',
            border: '1px solid rgba(95,160,255,0.42)',
            boxShadow: '0 0 18px rgba(47,124,255,0.22), inset 0 1px 0 rgba(255,255,255,0.06)',
            backdropFilter: 'blur(14px)',
            WebkitBackdropFilter: 'blur(14px)',
          }}
          data-testid="image-home-button"
        >
          <Home className="w-4 h-4 text-[#7FB4FF]" />
          HOME
        </button>

        <JarvisMenu onGoHome={onReturnHome} onGoBuilder={onOpenBuilder} onGoComputer={onOpenComputer} />
      </div>

      <div className="relative z-10 flex-1 min-h-0 flex gap-4 px-4 pb-4">
        {/* LEFT — prompt + controls */}
        <section
          className="w-[35%] min-w-[340px] flex flex-col rounded-2xl overflow-hidden border border-[rgba(95,160,255,0.2)] bg-[rgba(5,12,24,0.72)] backdrop-blur-2xl"
          data-testid="image-controls-panel"
        >
          <div className="px-5 pt-5 pb-3">
            <div className="flex items-center gap-2 text-xs tracking-[0.2em] text-[#8EA1BA]">
              <Sparkles className="w-3.5 h-3.5 text-[#7FB4FF]" />
              IMAGE GENERATION
            </div>
            <p className="mt-1 text-[13px] text-[#7C8DA6]">
              {providerConnected === false
                ? 'Provider not connected'
                : providerConnected === true
                ? 'Provider connected'
                : 'Checking provider…'}
            </p>
          </div>

          <div className="flex-1 min-h-0 overflow-y-auto px-5 pb-4 space-y-5">
            <div>
              <label className="text-xs text-[#8EA1BA]">Prompt</label>
              <textarea
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                rows={5}
                placeholder="Describe the image you want to create..."
                className="mt-1.5 w-full rounded-lg bg-[rgba(3,8,16,0.7)] border border-[rgba(95,160,255,0.24)] px-3 py-2.5 text-[14px] text-[#F5F8FF] placeholder-[#7C8DA6] outline-none focus:border-[rgba(95,160,255,0.55)] resize-none"
                data-testid="image-prompt-input"
              />
            </div>

            <div>
              <label className="text-xs text-[#8EA1BA]">Style</label>
              <div className="mt-1.5 flex flex-wrap gap-2" data-testid="image-style-selector">
                {STYLES.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => setStyle(s.id)}
                    className={`px-3 h-8 rounded-md text-[12px] border transition-colors ${
                      style === s.id
                        ? 'text-white bg-[rgba(47,124,255,0.85)] border-[rgba(120,178,255,0.85)]'
                        : 'text-[#9FB0C6] border-[rgba(95,160,255,0.22)] hover:text-[#EAF2FF]'
                    }`}
                    data-testid={`image-style-${s.id}`}
                  >
                    {s.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-xs text-[#8EA1BA]">Aspect ratio</label>
              <div className="mt-1.5 flex gap-2" data-testid="image-ratio-selector">
                {RATIOS.map((r) => (
                  <button
                    key={r}
                    type="button"
                    onClick={() => setRatio(r)}
                    className={`px-3 h-8 rounded-md text-[12px] border transition-colors ${
                      ratio === r
                        ? 'text-white bg-[rgba(47,124,255,0.85)] border-[rgba(120,178,255,0.85)]'
                        : 'text-[#9FB0C6] border-[rgba(95,160,255,0.22)] hover:text-[#EAF2FF]'
                    }`}
                    data-testid={`image-ratio-${r.replace(':', '-')}`}
                  >
                    {r}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="text-xs text-[#8EA1BA]">Quality</label>
              <div className="mt-1.5 flex gap-2">
                {(['standard', 'high'] as const).map((q) => (
                  <button
                    key={q}
                    type="button"
                    onClick={() => setQuality(q)}
                    className={`px-3 h-8 rounded-md text-[12px] border capitalize transition-colors ${
                      quality === q
                        ? 'text-white bg-[rgba(47,124,255,0.85)] border-[rgba(120,178,255,0.85)]'
                        : 'text-[#9FB0C6] border-[rgba(95,160,255,0.22)] hover:text-[#EAF2FF]'
                    }`}
                    data-testid={`image-quality-${q}`}
                  >
                    {q}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="px-5 pb-5">
            <button
              type="button"
              onClick={() => run()}
              disabled={!prompt.trim() || phase === 'generating'}
              className="w-full h-11 rounded-lg flex items-center justify-center gap-2 text-[14px] text-white transition-all disabled:opacity-35 disabled:cursor-not-allowed"
              style={{
                background: 'linear-gradient(180deg, #2F7CFF 0%, #1B55CC 100%)',
                border: '1px solid rgba(120,178,255,0.85)',
                boxShadow: '0 0 18px rgba(47,124,255,0.34), inset 0 1px 0 rgba(255,255,255,0.18)',
              }}
              data-testid="image-generate-button"
            >
              {phase === 'generating' ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Generating…
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4" />
                  Generate
                </>
              )}
            </button>
            {notice && (
              <div className="mt-2 text-[11px] text-[#8EA1BA]" data-testid="image-notice">
                {notice}
              </div>
            )}
          </div>
        </section>

        {/* RIGHT — result over the artwork */}
        <section className="flex-1 min-w-0 flex flex-col" data-testid="image-result-pane">
          <div
            className="flex items-center gap-1 px-2 h-10 rounded-lg mb-2"
            style={{
              background: 'rgba(5,12,24,0.42)',
              border: '1px solid rgba(95,160,255,0.16)',
              backdropFilter: 'blur(10px)',
              WebkitBackdropFilter: 'blur(10px)',
            }}
            data-testid="image-toolbar"
          >
            <button
              type="button"
              onClick={download}
              disabled={phase !== 'completed'}
              className={smallBtn}
              data-testid="image-download-button"
            >
              <Download className="w-3.5 h-3.5" />
              Download Image
            </button>
            <button
              type="button"
              onClick={() => run(lastPromptRef.current || prompt)}
              disabled={!(lastPromptRef.current || prompt.trim()) || phase === 'generating'}
              className={smallBtn}
              data-testid="image-regenerate-button"
            >
              <RotateCw className="w-3.5 h-3.5" />
              Regenerate
            </button>
            <button
              type="button"
              onClick={() => {
                navigator.clipboard?.writeText(lastPromptRef.current || prompt);
                setNotice('Prompt copied');
              }}
              disabled={!(lastPromptRef.current || prompt.trim())}
              className={smallBtn}
              data-testid="image-copy-prompt-button"
            >
              <Copy className="w-3.5 h-3.5" />
              Copy Prompt
            </button>
            <button
              type="button"
              onClick={() => setNotice('Save to Project — coming soon (project storage not built yet)')}
              className={smallBtn}
              data-testid="image-save-project-button"
            >
              <Save className="w-3.5 h-3.5" />
              Save to Project
            </button>

            <span className="ml-auto pr-1 text-[11px] font-mono-jarvis text-[#7C8DA6]" data-testid="image-status-chip">
              status: {phase}
            </span>
          </div>

          <div className="flex-1 min-h-0 rounded-xl flex items-center justify-center overflow-hidden">
            {phase === 'idle' && (
              <div className="text-center space-y-1.5" data-testid="image-idle-state">
                <div className="flex justify-center text-[#5C9DFF]">
                  <ImageIcon className="w-6 h-6" />
                </div>
                <div className="text-sm text-[#9FB0C6]">No image yet</div>
                <p className="text-[13px] text-[#7C8DA6]">Describe an image and press Generate.</p>
              </div>
            )}

            {phase === 'generating' && (
              <div className="text-center space-y-2" data-testid="image-generating-state">
                <div className="flex justify-center">
                  <Loader2 className="w-6 h-6 text-[#7FB4FF] animate-spin" />
                </div>
                <div className="text-sm text-[#EAF2FF]">Generating image</div>
                <p className="text-[13px] text-[#7C8DA6]">
                  JARVIS is rendering your prompt with the connected provider.
                </p>
              </div>
            )}

            {phase === 'completed' && result?.image_url && (
              <img
                src={absoluteUrl(result.image_url)}
                alt={lastPromptRef.current}
                className="max-w-full max-h-full object-contain rounded-xl"
                style={{ boxShadow: '0 20px 60px rgba(0,0,0,0.55), 0 0 28px rgba(47,124,255,0.18)' }}
                data-testid="generated-image"
              />
            )}

            {(phase === 'failed' || phase === 'blocked') && (
              <div className="text-center space-y-2 max-w-md px-6" data-testid="image-error-state">
                <div className="flex justify-center text-[#FF5C7A]">
                  {phase === 'blocked' ? <Ban className="w-6 h-6" /> : <AlertTriangle className="w-6 h-6" />}
                </div>
                <div className="text-sm text-[#EAF2FF]">
                  {phase === 'blocked' ? 'Image generation blocked' : 'Generation failed'}
                </div>
                <p className="text-[13px] text-[#9FB0C6] leading-relaxed">{error}</p>
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
};
