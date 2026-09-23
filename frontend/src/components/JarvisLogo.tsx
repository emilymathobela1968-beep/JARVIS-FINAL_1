import React from 'react';

interface JarvisLogoProps {
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

export const JarvisLogo: React.FC<JarvisLogoProps> = ({ 
  className = '', 
  size = 'md' 
}) => {
  const heightClass = size === 'sm' ? 'h-7' : size === 'lg' ? 'h-12' : 'h-9 sm:h-10';

  return (
    <div className={`relative inline-flex items-center select-none ${className}`}>
      <svg
        viewBox="0 0 260 56"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className={`${heightClass} w-auto filter drop-shadow-[0_2px_14px_rgba(73,168,255,0.4)]`}
      >
        <defs>
          {/* Chrome / Silver Bevel Gradients */}
          <linearGradient id="chromeGradient" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stopColor="#FFFFFF" />
            <stop offset="35%" stopColor="#E2EAF4" />
            <stop offset="50%" stopColor="#8DA3BE" />
            <stop offset="52%" stopColor="#4A627E" />
            <stop offset="85%" stopColor="#C4D7ED" />
            <stop offset="100%" stopColor="#FFFFFF" />
          </linearGradient>

          <linearGradient id="chromeStroke" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#FFFFFF" stopOpacity="0.9" />
            <stop offset="50%" stopColor="#6C8BB3" stopOpacity="0.6" />
            <stop offset="100%" stopColor="#FFFFFF" stopOpacity="0.8" />
          </linearGradient>

          {/* Red Underline Streak Gradient */}
          <linearGradient id="redSwooshGrad" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#FF1E46" stopOpacity="1" />
            <stop offset="65%" stopColor="#FF2A4D" stopOpacity="0.85" />
            <stop offset="100%" stopColor="#FF1E46" stopOpacity="0" />
          </linearGradient>

          {/* Specular Flare Radial */}
          <radialGradient id="specularGlow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="#FFFFFF" stopOpacity="0.95" />
            <stop offset="40%" stopColor="#70B8FF" stopOpacity="0.6" />
            <stop offset="100%" stopColor="#70B8FF" stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Dynamic Red Underline Streak under the 'J' tail and 'ARVIS' */}
        <path
          d="M12 47 C 35 47, 55 49, 115 48 C 145 47.5, 175 49, 195 49.5"
          stroke="url(#redSwooshGrad)"
          strokeWidth="3.2"
          strokeLinecap="round"
          className="filter drop-shadow-[0_0_8px_rgba(255,30,70,0.85)]"
        />

        {/* Stylized Chrome Letter J */}
        <g id="letter-J">
          {/* Main sweeping curved J */}
          <path
            d="M 46 8 L 30 8 L 30 31 C 30 43, 16 43.5, 8 40 L 4 47 C 18 53, 44 52, 44 32 L 44 8 Z"
            fill="url(#chromeGradient)"
            stroke="url(#chromeStroke)"
            strokeWidth="0.8"
          />
          {/* Top-left sharp wing serif of J */}
          <path
            d="M 30 8 L 18 12 L 30 16 Z"
            fill="#FFFFFF"
            opacity="0.9"
          />
        </g>

        {/* Letters: ARVIS in futuristic, beveled chrome typography */}
        <text
          x="52"
          y="42"
          fontFamily="system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif"
          fontSize="36"
          fontWeight="900"
          letterSpacing="5"
          fontStyle="italic"
          fill="url(#chromeGradient)"
          stroke="url(#chromeStroke)"
          strokeWidth="0.75"
          style={{ fontStretch: 'expanded' }}
        >
          ARVIS
        </text>

        {/* Specular Sparkle / Lens Flare on 'J' and 'V' */}
        <ellipse cx="30" cy="9" rx="3.5" ry="3.5" fill="url(#specularGlow)" />
        <ellipse cx="148" cy="18" rx="2.5" ry="2.5" fill="url(#specularGlow)" />
      </svg>
    </div>
  );
};
