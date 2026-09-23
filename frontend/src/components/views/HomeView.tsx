import React, { useState } from 'react';
import { ArcReactorVisualizer } from '../ArcReactorVisualizer';
import { VoiceState, ChatMessage, NavigationTab } from '../../types';
import { soundFX } from '../../utils/audio';

interface HomeViewProps {
  voiceState: VoiceState;
  onToggleVoice: () => void;
  latestJarvis: string;
  latestUser: string;
  messages: ChatMessage[];
  onNavigate: (tab: NavigationTab) => void;
  onQuickPrompt: (prompt: string) => void;
}

export const HomeView: React.FC<HomeViewProps> = ({
  voiceState,
  onToggleVoice,
  latestJarvis,
  latestUser,
  messages,
  onNavigate,
  onQuickPrompt,
}) => {
  const [showHistory, setShowHistory] = useState(false);
  const isBlue = voiceState === 'LISTENING' || voiceState === 'SPEAKING' || voiceState === 'THINKING' || voiceState === 'EXECUTING';

  return (
    <div id="home-view-wrapper" className="w-full max-w-4xl mx-auto flex flex-col items-center justify-start space-y-8 py-2">
      {/* Cinematic Centerpiece Reactor */}
      <div className="w-full flex flex-col items-center">
        <ArcReactorVisualizer state={voiceState} onClick={onToggleVoice} />
      </div>

      {/* Cinematic Dialogue Stage */}
      <div 
        id="jarvis-dialogue-card"
        className={`w-full transition-all duration-700 rounded-3xl p-6 sm:p-8 backdrop-blur-2xl border ${
          isBlue
            ? 'bg-[#04101e]/85 border-cyan-500/40 shadow-[0_16px_50px_rgba(0,180,255,0.15)]'
            : 'bg-[#100609]/80 border-rose-950/70 shadow-[0_16px_50px_rgba(159,18,57,0.12)]'
        }`}
      >
        <div className="space-y-4">
          {/* Header Metadata */}
          <div className="flex items-center justify-between text-xs font-mono-jarvis">
            <div className="flex items-center gap-2">
              <span className={`w-2 h-2 rounded-full ${isBlue ? 'bg-cyan-400' : 'bg-rose-600'}`} />
              <span className={`tracking-widest font-semibold ${isBlue ? 'text-cyan-400' : 'text-rose-400'}`}>
                JARVIS INTELLIGENCE
              </span>
            </div>

            <button
              onClick={() => {
                soundFX.playBeep(640, 0.04);
                setShowHistory(!showHistory);
              }}
              className="text-neutral-400 hover:text-white transition-colors"
            >
              {showHistory ? 'Close History' : `History (${messages.length})`}
            </button>
          </div>

          {/* Primary Speech Text */}
          <div className="text-xl sm:text-2xl text-white font-medium leading-relaxed tracking-tight">
            {latestJarvis}
          </div>

          {/* User Prompt Echo */}
          {latestUser && (
            <div className="text-sm text-neutral-400 border-l-2 border-white/20 pl-4 py-1 italic">
              "{latestUser}"
            </div>
          )}

          {/* Minimalist Quick Directives */}
          <div className="pt-2 flex flex-wrap gap-2.5">
            <button
              onClick={() => {
                soundFX.playAcknowledge();
                onQuickPrompt("Analyze current system diagnostics and active processes");
                onNavigate('system');
              }}
              className="px-4 py-2 rounded-xl text-xs font-mono-jarvis bg-white/5 hover:bg-white/10 text-neutral-300 hover:text-white border border-white/10 hover:border-white/25 transition-all active:scale-95"
            >
              Run System Diagnostics
            </button>
            <button
              onClick={() => {
                soundFX.playAcknowledge();
                onQuickPrompt("Open developer workspace and inspect verified test suites");
                onNavigate('developer');
              }}
              className="px-4 py-2 rounded-xl text-xs font-mono-jarvis bg-white/5 hover:bg-white/10 text-neutral-300 hover:text-white border border-white/10 hover:border-white/25 transition-all active:scale-95"
            >
              Inspect Developer Codex
            </button>
            <button
              onClick={() => {
                soundFX.playAcknowledge();
                onQuickPrompt("Open Computer control and check running applications");
                onNavigate('computer');
              }}
              className="px-4 py-2 rounded-xl text-xs font-mono-jarvis bg-white/5 hover:bg-white/10 text-neutral-300 hover:text-white border border-white/10 hover:border-white/25 transition-all active:scale-95"
            >
              Launch Windows Applications
            </button>
            <button
              onClick={() => {
                soundFX.playAcknowledge();
                onQuickPrompt("Initialize Barehands holographic stage and gesture tracking");
                onNavigate('barehands');
              }}
              className="px-4 py-2 rounded-xl text-xs font-mono-jarvis bg-white/5 hover:bg-white/10 text-neutral-300 hover:text-white border border-white/10 hover:border-white/25 transition-all active:scale-95"
            >
              Barehands Hologram
            </button>
          </div>
        </div>

        {/* Collapsible History Drawer */}
        {showHistory && (
          <div className="mt-6 pt-5 border-t border-white/10 space-y-3 max-h-60 overflow-y-auto pr-2 font-mono-jarvis text-xs">
            {messages.map((m) => (
              <div
                key={m.id}
                className={`p-3 rounded-xl border ${
                  m.sender === 'jarvis'
                    ? 'bg-white/5 border-white/10 text-neutral-200'
                    : 'bg-cyan-950/20 border-cyan-500/20 text-cyan-300'
                }`}
              >
                <div className="flex items-center justify-between text-[10px] text-neutral-500 mb-1">
                  <span>{m.sender.toUpperCase()}</span>
                  <span>{m.timestamp}</span>
                </div>
                <div className="leading-relaxed">{m.text}</div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
