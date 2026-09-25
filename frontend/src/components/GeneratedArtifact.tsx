import React, { useEffect, useRef, useState } from 'react';
import { Artifact } from '../types';
import { withVerificationProbe } from '../utils/api';
import { Loader2, ShieldAlert, ShieldCheck, LayoutTemplate, MousePointerClick } from 'lucide-react';

interface GeneratedArtifactProps {
  /** Null until a real artifact exists. Nothing is rendered from model text alone. */
  artifact: Artifact | null;
  /** Streamed byte count while generating (honest progress, not a fake timer). */
  streamedChars?: number;
  /** Fired once the sandbox reports BOTH a real render and a real interaction. */
  onVerified?: () => void;
}

const Shell: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="w-full h-full flex items-center justify-center px-8">
    <div className="max-w-md w-full text-center space-y-3">{children}</div>
  </div>
);

export const GeneratedArtifact: React.FC<GeneratedArtifactProps> = ({
  artifact,
  streamedChars,
  onVerified,
}) => {
  const [rendered, setRendered] = useState(false);
  const [interacted, setInteracted] = useState(false);
  const verifiedSentRef = useRef(false);

  // Reset probe state whenever a new artifact (by id) shows up.
  useEffect(() => {
    setRendered(false);
    setInteracted(false);
    verifiedSentRef.current = false;
  }, [artifact?.id]);

  // Listen for the sandbox probe's render / interaction reports.
  useEffect(() => {
    const onMessage = (e: MessageEvent) => {
      const d = e.data;
      if (!d || d.__jarvis !== true) return;
      if (d.type === 'render') setRendered(true);
      if (d.type === 'interaction') setInteracted(true);
    };
    window.addEventListener('message', onMessage);
    return () => window.removeEventListener('message', onMessage);
  }, []);

  // Promote to verified once BOTH signals are in (and only once).
  useEffect(() => {
    if (
      artifact &&
      artifact.status === 'unverified' &&
      rendered &&
      interacted &&
      !verifiedSentRef.current
    ) {
      verifiedSentRef.current = true;
      onVerified?.();
    }
  }, [rendered, interacted, artifact, onVerified]);

  if (!artifact) {
    return (
      <Shell>
        <div className="flex justify-center">
          <div className="w-11 h-11 rounded-2xl bg-[#060D18] border border-white/10 flex items-center justify-center text-[#4E5C70]">
            <LayoutTemplate className="w-5 h-5 stroke-[1.6]" />
          </div>
        </div>
        <div className="text-sm font-semibold tracking-[0.18em] text-[#8B98AA]">PREVIEW</div>
        <p className="text-sm text-[#66738A] leading-relaxed">Waiting for first render</p>
        <p className="text-xs font-mono-jarvis text-[#4E5C70]">Idle • No artifact created</p>
      </Shell>
    );
  }

  if (artifact.status === 'generating') {
    return (
      <Shell>
        <div className="flex justify-center">
          <Loader2 className="w-6 h-6 text-[#7FB4FF] animate-spin" />
        </div>
        <div className="text-base font-semibold text-[#F5F8FF]">Generating web app</div>
        <p className="text-sm text-[#8B98AA] leading-relaxed">
          {artifact.name} is being written by JARVIS.
        </p>
        {typeof streamedChars === 'number' && streamedChars > 0 && (
          <p className="text-xs font-mono-jarvis text-[#7FB4FF]">
            {streamedChars.toLocaleString()} chars received
          </p>
        )}
      </Shell>
    );
  }

  if (artifact.status === 'failed') {
    return (
      <Shell>
        <div className="flex justify-center">
          <div className="w-11 h-11 rounded-2xl bg-[#1A060C] border border-[#FF294D]/35 flex items-center justify-center text-[#FF5C7A]">
            <ShieldAlert className="w-5 h-5 stroke-[1.6]" />
          </div>
        </div>
        <div className="text-base font-semibold text-[#F5F8FF]">Artifact blocked</div>
        <p className="text-sm text-[#8B98AA] leading-relaxed">
          {artifact.failureReason || 'The run failed before a renderable artifact existed.'}
        </p>
        {artifact.evidence && (
          <pre className="text-left text-[11px] font-mono-jarvis text-[#FF8FA3] bg-[#0B0509] border border-[#FF294D]/20 rounded-2xl p-3 overflow-x-auto">
            {artifact.evidence}
          </pre>
        )}
      </Shell>
    );
  }

  // unverified OR verified with real source -> render it live in the sandbox.
  if (!artifact.source) {
    return (
      <Shell>
        <div className="text-base font-semibold text-[#F5F8FF]">{artifact.name}</div>
        <p className="text-sm text-[#8B98AA] leading-relaxed">
          The run returned no renderable output.
        </p>
      </Shell>
    );
  }

  const isVerified = artifact.status === 'verified';

  return (
    <div className="w-full h-full relative overflow-hidden rounded-xl">
      <iframe
        key={artifact.id}
        title={artifact.name}
        srcDoc={withVerificationProbe(artifact.source)}
        className="w-full h-full border-0 bg-white"
        sandbox="allow-scripts allow-forms allow-modals allow-popups"
        data-testid="artifact-iframe"
      />

      {/* Verification banner — honest, driven only by real sandbox signals. */}
      {!isVerified ? (
        <div
          className="absolute top-3 left-1/2 -translate-x-1/2 z-10 flex items-center gap-3 px-4 py-2 rounded-full backdrop-blur-md"
          style={{
            background: 'linear-gradient(180deg, rgba(26,20,5,0.92) 0%, rgba(18,14,4,0.92) 100%)',
            border: '1px solid rgba(245,185,66,0.45)',
            boxShadow: '0 0 18px rgba(245,185,66,0.18)',
          }}
          data-testid="verify-banner"
        >
          <MousePointerClick className="w-4 h-4 text-[#F5B942]" />
          <span className="text-xs text-[#F5D98A]">
            {rendered ? 'Rendered ✓ — interact with the preview to verify' : 'Waiting for render…'}
          </span>
          <span className="text-[10px] font-mono-jarvis text-[#B99A4A]">
            render {rendered ? '✓' : '…'} · interact {interacted ? '✓' : '…'}
          </span>
        </div>
      ) : (
        <div
          className="absolute top-3 left-1/2 -translate-x-1/2 z-10 flex items-center gap-2 px-4 py-2 rounded-full backdrop-blur-md"
          style={{
            background: 'linear-gradient(180deg, rgba(5,26,20,0.92) 0%, rgba(4,18,14,0.92) 100%)',
            border: '1px solid rgba(40,215,161,0.45)',
            boxShadow: '0 0 18px rgba(40,215,161,0.18)',
          }}
          data-testid="verified-badge"
        >
          <ShieldCheck className="w-4 h-4 text-[#28D7A1]" />
          <span className="text-xs text-[#7FE9C9]">Verified — render + interaction confirmed</span>
        </div>
      )}
    </div>
  );
};
