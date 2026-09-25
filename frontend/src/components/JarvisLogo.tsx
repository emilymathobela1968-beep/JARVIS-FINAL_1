import React from 'react';
import logoSrc from '../assets/images/jarvis_logo_trimmed.png';

interface JarvisLogoProps {
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

/** Official JARVIS logo asset (transparent PNG, transparent padding trimmed). */
export const JarvisLogo: React.FC<JarvisLogoProps> = ({ className = '', size = 'md' }) => {
  const heightClass = size === 'sm' ? 'h-6' : size === 'lg' ? 'h-12' : 'h-9';

  return (
    <img
      src={logoSrc}
      alt="JARVIS"
      draggable={false}
      className={`${heightClass} w-auto select-none drop-shadow-[0_2px_14px_rgba(73,168,255,0.35)] ${className}`}
    />
  );
};
