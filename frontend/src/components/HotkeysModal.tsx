import React from 'react';
import { soundFX } from '../utils/audio';

interface HotkeysModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const HotkeysModal: React.FC<HotkeysModalProps> = ({ isOpen, onClose }) => {
  if (!isOpen) return null;

  const hotkeys = [
    { key: 'Enter', desc: 'Send command in Universal Command Bar' },
    { key: 'Space', desc: 'Hold/Tap Mic button to toggle voice session' },
    { key: 'Esc', desc: 'Close dialogs or cancel execution' },
    { key: 'Alt + 1', desc: 'Switch to Home / Arc Reactor HUD' },
    { key: 'Alt + 2', desc: 'Switch to Computer / Process Control' },
    { key: 'Alt + 3', desc: 'Switch to Developer / Workspace & Tests' },
    { key: 'Alt + 4', desc: 'Switch to Media / ALEXIS Promo' },
    { key: 'Alt + 5', desc: 'Switch to Builder / App Studio' },
    { key: 'Alt + 6', desc: 'Switch to Barehands / Spatial Stage' },
    { key: 'Alt + 7', desc: 'Switch to System / WebRTC Diagnostics' },
  ];

  return (
    <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4">
      <div className="w-full max-w-lg bg-[#071320] border border-[#45BCFF]/50 rounded-3xl p-6 shadow-[0_0_50px_rgba(69,188,255,0.25)] space-y-5 animate-in fade-in zoom-in-95">
        <div className="flex items-center justify-between border-b border-[#45BCFF]/20 pb-3">
          <div className="flex items-center gap-2">
            <span className="text-xl text-[#45BCFF]">⌨</span>
            <h3 className="text-lg font-semibold text-white font-mono-jarvis">
              UNIVERSAL HOTKEYS & COMMANDS
            </h3>
          </div>
          <button
            onClick={() => {
              soundFX.playBeep(450, 0.05);
              onClose();
            }}
            className="w-8 h-8 rounded-full bg-[#0E243A] text-[#8CBFEA] hover:text-white flex items-center justify-center"
          >
            ✕
          </button>
        </div>

        <div className="space-y-2 max-h-80 overflow-y-auto pr-1 font-mono-jarvis text-xs">
          {hotkeys.map((hk, i) => (
            <div key={i} className="flex items-center justify-between p-2.5 rounded-xl bg-[#040C16] border border-[#45BCFF]/15">
              <span className="text-[#8CBFEA]">{hk.desc}</span>
              <kbd className="px-2.5 py-1 rounded bg-[#102B45] text-[#55C7FF] border border-[#45BCFF]/40 font-bold">
                {hk.key}
              </kbd>
            </div>
          ))}
        </div>

        <div className="text-[11px] text-[#8CBFEA] text-center pt-2">
          Designed for rapid hands-free and keyboard-driven Windows operations.
        </div>
      </div>
    </div>
  );
};
