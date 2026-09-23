import React from 'react';

interface JarvisBackgroundProps {
  showHorizons?: boolean;
  className?: string;
}

export const JarvisBackground: React.FC<JarvisBackgroundProps> = ({
  showHorizons = true,
  className = '',
}) => {
  return (
    <div className={`absolute inset-0 pointer-events-none overflow-hidden select-none z-0 ${className}`}>
      {/* Deep pitch-black base */}
      <div className="absolute inset-0 bg-[#020306]" />

      {showHorizons && (
        <>
          {/* Left curved red planetary horizon arc */}
          <div 
            className="absolute -left-[30vw] top-[5vh] w-[70vw] h-[85vh] rounded-[50%] border-[2px] border-red-600/40 opacity-90 blur-[1px]"
            style={{
              boxShadow: '0 0 45px 6px rgba(225, 29, 72, 0.45), inset 0 0 35px 4px rgba(239, 68, 68, 0.25)',
              transform: 'rotate(-12deg)',
            }}
          />

          {/* Right curved cyan/ice-blue planetary horizon arc */}
          <div 
            className="absolute -right-[30vw] top-[8vh] w-[70vw] h-[85vh] rounded-[50%] border-[2px] border-sky-500/40 opacity-80 blur-[1px]"
            style={{
              boxShadow: '0 0 45px 6px rgba(14, 165, 233, 0.4), inset 0 0 35px 4px rgba(56, 189, 248, 0.25)',
              transform: 'rotate(14deg)',
            }}
          />

          {/* Center dark celestial body shadow */}
          <div className="absolute top-[2%] left-1/2 -translate-x-1/2 w-[85vw] max-w-[1200px] h-[55vh] rounded-full bg-gradient-to-b from-[#010204]/90 via-[#030509]/95 to-transparent blur-md" />

          {/* Bottom reflective wet obsidian terrain / floor */}
          <div className="absolute bottom-0 left-0 right-0 h-[38vh] bg-gradient-to-t from-[#010306] via-[#040810]/90 to-transparent">
            {/* Rocky terrain contour textures */}
            <div className="absolute inset-0 opacity-40 mix-blend-screen bg-[radial-gradient(ellipse_at_bottom_center,rgba(15,23,42,0.8)_0%,transparent_70%)]" />

            {/* Left red light pool reflection on wet terrain */}
            <div 
              className="absolute -bottom-6 left-[8%] w-[38vw] h-[22vh] rounded-[50%] opacity-45 blur-3xl pointer-events-none"
              style={{
                background: 'radial-gradient(ellipse at center, rgba(225, 29, 72, 0.35) 0%, rgba(159, 18, 57, 0.15) 50%, transparent 80%)',
              }}
            />

            {/* Right cyan light pool reflection on wet terrain */}
            <div 
              className="absolute -bottom-6 right-[8%] w-[38vw] h-[22vh] rounded-[50%] opacity-40 blur-3xl pointer-events-none"
              style={{
                background: 'radial-gradient(ellipse at center, rgba(14, 165, 233, 0.3) 0%, rgba(2, 132, 199, 0.12) 50%, transparent 80%)',
              }}
            />

            {/* Horizontal terrain mist line */}
            <div className="absolute bottom-[20%] left-0 right-0 h-[1px] bg-gradient-to-r from-transparent via-neutral-800/40 to-transparent" />
          </div>

          {/* Vignette edge shadows */}
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,transparent_45%,rgba(0,0,0,0.85)_100%)] pointer-events-none" />
        </>
      )}
    </div>
  );
};
