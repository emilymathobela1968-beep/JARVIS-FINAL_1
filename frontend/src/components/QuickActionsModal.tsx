import React from 'react';
import { soundFX } from '../utils/audio';

interface QuickActionsModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelectAction: (prompt: string, category?: string) => void;
}

export const QuickActionsModal: React.FC<QuickActionsModalProps> = ({
  isOpen,
  onClose,
  onSelectAction,
}) => {
  if (!isOpen) return null;

  const actions = [
    {
      category: 'Windows Control',
      icon: '💻',
      prompts: [
        'Open Microsoft Word and create a project proposal draft',
        'Open Microsoft Excel and load the financial forecast workbook',
        'Find and list all recent Markdown notes in the workspace',
        'Check system health and running background processes',
      ],
    },
    {
      category: 'Developer & Code',
      icon: '⚙',
      prompts: [
        'Run all verified test suites (Realtime, AppBuilder, Developer)',
        'Inspect RealtimeWebViewHost.cs microphone payload parsing logic',
        'Check git diff and working directory status in Jarvis.sln',
        'Build the solution with Configuration=Debug and Platform=x64',
      ],
    },
    {
      category: 'App Builder & Media',
      icon: '📐',
      prompts: [
        'Load the App Builder project and render current candidate screen',
        'Evaluate visual contrast and design tokens for WCAG AA compliance',
        'Generate Scene 1 candidate for the ALEXIS promotional teaser',
        'Switch to Barehands holographic stage and track hand gestures',
      ],
    },
  ];

  return (
    <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4">
      <div className="w-full max-w-xl bg-[#071320] border border-[#45BCFF]/50 rounded-3xl p-6 shadow-[0_0_50px_rgba(69,188,255,0.25)] space-y-5 animate-in fade-in zoom-in-95 max-h-[85vh] flex flex-col">
        <div className="flex items-center justify-between border-b border-[#45BCFF]/20 pb-3">
          <div className="flex items-center gap-2">
            <span className="text-xl text-[#45BCFF]">+</span>
            <h3 className="text-lg font-semibold text-white font-mono-jarvis">
              GOVERNED PROMPT CATALOG
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

        <div className="flex-1 overflow-y-auto space-y-4 pr-1">
          {actions.map((cat, i) => (
            <div key={i} className="space-y-2">
              <div className="flex items-center gap-2 text-xs font-mono-jarvis font-bold text-[#45BCFF] tracking-wider">
                <span>{cat.icon}</span>
                <span>{cat.category.toUpperCase()}</span>
              </div>
              <div className="grid grid-cols-1 gap-2">
                {cat.prompts.map((p, idx) => (
                  <button
                    key={idx}
                    onClick={() => {
                      soundFX.playAcknowledge();
                      onSelectAction(p, cat.category);
                      onClose();
                    }}
                    className="w-full text-left p-3 rounded-xl bg-[#050E17] hover:bg-[#102B45] border border-[#45BCFF]/20 hover:border-[#55C7FF] text-xs font-mono-jarvis text-[#E8F7FF] transition-all active:scale-[0.99] flex items-center justify-between group"
                  >
                    <span>{p}</span>
                    <span className="text-[#55C7FF] opacity-0 group-hover:opacity-100 transition-opacity ml-2">
                      ›
                    </span>
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
