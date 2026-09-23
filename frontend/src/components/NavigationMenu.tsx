import React, { useState } from 'react';
import { NavigationTab, VoiceState } from '../types';
import { soundFX } from '../utils/audio';

interface NavigationMenuProps {
  activeTab: NavigationTab;
  onSelectTab: (tab: NavigationTab) => void;
  voiceState: VoiceState;
  onToggleVoice: () => void;
}

export const NavigationMenu: React.FC<NavigationMenuProps> = ({
  activeTab,
  onSelectTab,
  voiceState,
  onToggleVoice,
}) => {
  const [menuOpen, setMenuOpen] = useState(false);

  const tabs: { id: NavigationTab; label: string }[] = [
    { id: 'home', label: 'Home' },
    { id: 'computer', label: 'Computer' },
    { id: 'developer', label: 'Developer' },
    { id: 'media', label: 'Media' },
    { id: 'builder', label: 'Builder' },
    { id: 'barehands', label: 'Barehands' },
    { id: 'system', label: 'System' },
  ];

  const isListening = voiceState === 'LISTENING';

  return (
    <header className="w-full max-w-7xl mx-auto px-6 pt-7 pb-4 flex items-center justify-between relative z-40 select-none">
      {/* Brand Zone - Clean single wordmark with subtle cinematic glow */}
      <div 
        onClick={() => {
          soundFX.playBeep(640, 0.04);
          onSelectTab('home');
        }}
        className="cursor-pointer group flex items-center gap-3"
      >
        <span className="text-2xl sm:text-3xl font-semibold tracking-[0.4em] text-white font-mono-jarvis transition-colors group-hover:text-cyan-400">
          JARVIS
        </span>
        <span 
          className={`w-2 h-2 rounded-full transition-all duration-500 ${
            isListening 
              ? 'bg-cyan-400 shadow-[0_0_12px_#38bdf8] animate-pulse' 
              : 'bg-rose-800 shadow-[0_0_6px_#9f1239]'
          }`} 
        />
      </div>

      {/* Center Nav Links - Clean typography with subtle active underline */}
      <nav className="hidden lg:flex items-center gap-8 text-sm font-medium">
        {tabs.map((tab) => {
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => {
                soundFX.playBeep(600, 0.04);
                onSelectTab(tab.id);
              }}
              className={`relative py-1 font-mono-jarvis text-xs tracking-wider transition-colors ${
                isActive 
                  ? 'text-white font-semibold' 
                  : 'text-neutral-400 hover:text-white'
              }`}
            >
              {tab.label}
              {isActive && (
                <span className="absolute bottom-0 left-0 right-0 h-[1.5px] bg-gradient-to-r from-transparent via-cyan-400 to-transparent shadow-[0_0_8px_#38bdf8]" />
              )}
            </button>
          );
        })}
      </nav>

      {/* Right Controls - Mobile Menu Trigger & Voice Toggle */}
      <div className="flex items-center gap-3">
        {/* Quick Voice State Indicator Button */}
        <button
          onClick={onToggleVoice}
          className={`hidden sm:flex items-center gap-2.5 px-3.5 py-1.5 rounded-full border text-xs font-mono-jarvis transition-all active:scale-95 ${
            isListening
              ? 'bg-cyan-950/40 border-cyan-400/80 text-cyan-300 shadow-[0_0_15px_rgba(56,189,248,0.3)]'
              : 'bg-rose-950/30 border-rose-900/50 text-rose-300/80 hover:border-rose-700 hover:text-rose-200'
          }`}
          title="Click to toggle Jarvis voice"
        >
          <span 
            className={`w-1.5 h-1.5 rounded-full ${
              isListening ? 'bg-cyan-400 animate-ping' : 'bg-rose-600'
            }`} 
          />
          <span className="tracking-widest">
            {isListening ? 'ACTIVE' : 'STANDBY'}
          </span>
        </button>

        {/* Mobile / Compact Menu Toggle */}
        <div className="relative lg:hidden">
          <button
            onClick={() => {
              soundFX.playBeep(640, 0.04);
              setMenuOpen(!menuOpen);
            }}
            className="p-2 rounded-xl bg-white/5 border border-white/10 text-neutral-300 hover:text-white transition-all active:scale-95"
            aria-label="Toggle Navigation Menu"
          >
            <span className="text-lg">☰</span>
          </button>

          {menuOpen && (
            <div className="absolute right-0 mt-2 w-52 bg-[#050911]/95 border border-white/15 rounded-2xl shadow-2xl backdrop-blur-2xl py-2 z-50 animate-in fade-in zoom-in-95">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => {
                    soundFX.playBeep(600, 0.04);
                    onSelectTab(tab.id);
                    setMenuOpen(false);
                  }}
                  className={`w-full px-4 py-2.5 text-left text-xs font-mono-jarvis flex items-center justify-between transition-colors ${
                    activeTab === tab.id
                      ? 'text-cyan-400 bg-cyan-950/30 font-semibold'
                      : 'text-neutral-300 hover:bg-white/5 hover:text-white'
                  }`}
                >
                  <span>{tab.label}</span>
                  {activeTab === tab.id && <span className="text-[10px]">●</span>}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
