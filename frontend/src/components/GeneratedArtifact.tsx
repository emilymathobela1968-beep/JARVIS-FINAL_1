import React from 'react';
import { Artifact } from '../types';
import { Loader2, ShieldAlert, ShieldCheck, LayoutTemplate } from 'lucide-react';

interface GeneratedArtifactProps {
  /** Null until a real artifact exists. Nothing is rendered from model text alone. */
  artifact: Artifact | null;
}

const Shell: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="w-full h-full flex items-center justify-center bg-[#030508] px-8">
    <div className="max-w-md w-full text-center space-y-3">{children}</div>
  </div>
);

export const GeneratedArtifact: React.FC<GeneratedArtifactProps> = ({ artifact }) => {
  if (!artifact) {
    return (
      <Shell>
        <div className="flex justify-center">
          <div className="w-11 h-11 rounded-2xl bg-[#060D18] border border-white/10 flex items-center justify-center text-[#4E5C70]">
            <LayoutTemplate className="w-5 h-5 stroke-[1.6]" />
          </div>
        </div>
        <div className="text-sm font-semibold tracking-[0.18em] text-[#8B98AA]">PREVIEW</div>
        <p className="text-sm text-[#66738A] leading-relaxed">
          Waiting for first verified render
        </p>
        <p className="text-xs font-mono-jarvis text-[#4E5C70]">Planning • No artifact created</p>
      </Shell>
    );
  }

  if (artifact.status === 'generating') {
    return (
      <Shell>
        <div className="flex justify-center">
          <Loader2 className="w-6 h-6 text-[#58D7FF] animate-spin" />
        </div>
        <div className="text-base font-semibold text-[#F5F8FF]">Building artifact</div>
        <p className="text-sm text-[#8B98AA] leading-relaxed">
          {artifact.name} is being generated.
        </p>
        {typeof artifact.progress === 'number' && (
          <p className="text-xs font-mono-jarvis text-[#58D7FF]">{artifact.progress}% reported by runtime</p>
        )}
      </Shell>
    );
  }

  if (artifact.status === 'unverified') {
    return (
      <Shell>
        <div className="flex justify-center">
          <div className="w-11 h-11 rounded-2xl bg-[#1A1405] border border-[#F5B942]/30 flex items-center justify-center text-[#F5B942]">
            <ShieldCheck className="w-5 h-5 stroke-[1.6]" />
          </div>
        </div>
        <div className="text-base font-semibold text-[#F5F8FF]">Verification pending</div>
        <p className="text-sm text-[#8B98AA] leading-relaxed">
          {artifact.name} was produced but has not passed verification. Preview stays closed until it does.
        </p>
        {artifact.evidence && (
          <p className="text-xs font-mono-jarvis text-[#66738A] break-words">{artifact.evidence}</p>
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

  // status === 'verified' — render only what the run actually produced.
  return (
    <div className="w-full h-full bg-[#030508] overflow-auto">
      {artifact.source ? (
        <iframe
          title={artifact.name}
          srcDoc={artifact.source}
          className="w-full h-full border-0 bg-white"
          sandbox="allow-scripts"
        />
      ) : (
        <Shell>
          <div className="text-base font-semibold text-[#F5F8FF]">{artifact.name}</div>
          <p className="text-sm text-[#8B98AA] leading-relaxed">
            Verified, but the run returned no renderable output.
          </p>
          {artifact.evidence && (
            <p className="text-xs font-mono-jarvis text-[#66738A] break-words">{artifact.evidence}</p>
          )}
        </Shell>
      )}
    </div>
  );
};
