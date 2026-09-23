import React, { useState, useRef } from 'react';
import { soundFX } from '../utils/audio';

interface CommandBarProps {
  onSendMessage: (text: string) => void;
  isListening: boolean;
  onToggleMic: () => void;
  onOpenQuickMenu: () => void;
  onOpenHotkeys: () => void;
}

export const UniversalCommandBar: React.FC<CommandBarProps> = ({
  onSendMessage,
  isListening,
  onToggleMic,
  onOpenQuickMenu,
  onOpenHotkeys,
}) => {
  const [inputVal, setInputVal] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleSubmit = (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!inputVal.trim()) return;
    soundFX.playExecute();
    onSendMessage(inputVal.trim());
    setInputVal('');
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
    <div id="universal-command-bar-container" className="w-full max-w-4xl mx-auto px-4 pb-6">
      <div 
        id="command-bar-surface"
        className={`relative backdrop-blur-2xl rounded-3xl p-2.5 sm:p-3.5 flex items-center gap-2 sm:gap-3 transition-all duration-500 border ${
          isListening
            ? 'bg-[#040e1a]/90 border-cyan-500/50 shadow-[0_12px_40px_rgba(0,180,255,0.2)]'
            : 'bg-[#090306]/90 border-white/10 shadow-[0_12px_40px_rgba(0,0,0,0.8)] focus-within:border-white/20'
        }`}
      >
        {/* Quick Actions Plus Button */}
        <button
          id="btn-quick-actions"
          onClick={() => {
            soundFX.playBeep(520, 0.05);
            onOpenQuickMenu();
          }}
          className="w-10 h-10 sm:w-11 sm:h-11 rounded-2xl bg-white/5 border border-white/10 text-neutral-300 text-lg flex items-center justify-center hover:bg-white/10 hover:text-white transition-all active:scale-95 shrink-0"
          title="Directives catalog"
          type="button"
        >
          +
        </button>

        {/* Command Input Box */}
        <div className="flex-1 relative flex items-center">
          <textarea
            ref={textareaRef}
            id="command-input"
            value={inputVal}
            onChange={(e) => setInputVal(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={isListening ? "Listening... Speak now or type a directive" : "Speak to Jarvis or type a directive..."}
            rows={1}
            className="w-full bg-transparent text-white placeholder-neutral-500 border-none px-3 py-2 text-sm sm:text-base focus:outline-none resize-none font-mono-jarvis"
            style={{ minHeight: '40px', maxHeight: '100px' }}
          />
        </div>

        {/* Keyboard Cheatsheet Button */}
        <button
          id="btn-hotkeys"
          onClick={() => {
            soundFX.playBeep(660, 0.05);
            onOpenHotkeys();
          }}
          className="hidden sm:flex w-10 h-10 sm:w-11 sm:h-11 rounded-2xl bg-white/5 border border-white/10 text-neutral-400 text-base items-center justify-center hover:bg-white/10 hover:text-white transition-all active:scale-95 shrink-0"
          title="Keyboard shortcuts"
          type="button"
        >
          ⌨
        </button>

        {/* Send Action Button */}
        {inputVal.trim() && (
          <button
            onClick={() => handleSubmit()}
            className="px-4 py-2 rounded-2xl bg-cyan-500 hover:bg-cyan-400 text-black text-xs font-mono-jarvis font-bold transition-all active:scale-95 shrink-0 shadow-[0_0_15px_#38bdf8]"
            type="button"
          >
            DISPATCH
          </button>
        )}

        {/* Cinematic Glowing Mic Button: Maroon when Standby, Electric Blue when Active */}
        <button
          id="btn-voice-mic"
          onClick={() => {
            if (!isListening) {
              soundFX.playIgnite();
            } else {
              soundFX.playPowerDown();
            }
            onToggleMic();
          }}
          className={`w-12 h-12 sm:w-14 sm:h-14 rounded-2xl flex items-center justify-center text-xl sm:text-2xl transition-all duration-500 active:scale-95 shrink-0 border ${
            isListening
              ? 'bg-gradient-to-br from-cyan-400 to-blue-600 text-white border-cyan-300 shadow-[0_0_30px_rgba(56,189,248,0.9)] animate-pulse'
              : 'bg-gradient-to-br from-[#4c0519] to-[#1c0208] text-rose-300 border-rose-900/80 hover:border-rose-700 hover:text-rose-100 shadow-[0_0_15px_rgba(159,18,57,0.35)]'
          }`}
          title={isListening ? 'Click to stop voice session' : 'Click to activate voice session'}
          type="button"
        >
          🎙
        </button>
      </div>
    </div>
  );
};
