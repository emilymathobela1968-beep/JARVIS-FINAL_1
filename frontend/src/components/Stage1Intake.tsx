import React, { useState } from 'react';
import { JarvisBackground } from './JarvisBackground';
import { JarvisRadar } from './JarvisRadar';
import { Globe, Smartphone, Cpu, Mic } from 'lucide-react';
import { soundFX } from '../utils/audio';
import { jarvisVoice } from '../utils/voice';

export type AppCategory = 'web' | 'mobile' | 'ai';

interface Stage1IntakeProps {
  onStartBuild: (prompt: string, category: AppCategory) => void;
}

export const Stage1Intake: React.FC<Stage1IntakeProps> = ({ onStartBuild }) => {
  const [prompt, setPrompt] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<AppCategory>('web');
  const [isListening, setIsListening] = useState(false);

  const handleBuild = (e?: React.FormEvent) => {
    e?.preventDefault();
    const finalPrompt = prompt.trim() || getDefaultPrompt(selectedCategory);
    soundFX.playIgnite();
    onStartBuild(finalPrompt, selectedCategory);
  };

  const getDefaultPrompt = (cat: AppCategory): string => {
    switch (cat) {
      case 'web':
        return 'Build an Executive Cloud Operations Dashboard with live service nodes, latency metrics, and action dispatch.';
      case 'mobile':
        return 'Build a Next-Gen Mobile FinTech Experience with biometric card security, asset balances, and instant transactions.';
      case 'ai':
        return 'Build an Autonomous Neural Intelligence Console with model inference telemetry, prompt playground, and latency benchmarks.';
    }
  };

  const handleCategorySelect = (cat: AppCategory) => {
    soundFX.playAcknowledge();
    setSelectedCategory(cat);
    if (!prompt.trim()) {
      setPrompt(getDefaultPrompt(cat));
    }
  };

  const handleToggleVoice = () => {
    if (isListening) {
      soundFX.playPowerDown();
      jarvisVoice.stopListening();
      setIsListening(false);
    } else {
      soundFX.playIgnite();
      setIsListening(true);
      jarvisVoice.startListening(
        (transcript) => {
          setPrompt(transcript);
          setIsListening(false);
          soundFX.playAcknowledge();
        },
        (listening) => {
          setIsListening(listening);
        }
      );
    }
  };

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-[#030508] text-[#F4F7FB] flex flex-col justify-between select-none">
      {/* Cinematic Horizon Atmosphere Background */}
      <JarvisBackground showHorizons={true} />

      {/* Top Header with Brand */}
      <header className="relative z-20 w-full px-8 pt-8 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="text-2xl sm:text-3xl font-bold tracking-[0.38em] text-[#F4F7FB] font-mono-jarvis drop-shadow-[0_2px_12px_rgba(255,255,255,0.15)]">
            JARVIS
          </span>
        </div>
      </header>

      {/* Center Zone: Circular Radar + Large Glass Prompt Surface */}
      <main className="relative z-20 flex-1 flex flex-col items-center justify-center -mt-4 px-4">
        {/* Radar Core Centerpiece */}
        <div className="mb-4">
          <JarvisRadar size={380} />
        </div>

        {/* Large Dark Glass Command / Prompt Area */}
        <form 
          onSubmit={handleBuild}
          className="w-full max-w-3xl relative"
        >
          <div className="relative rounded-3xl p-4 sm:p-5 jarvis-glass transition-all duration-500 flex flex-col sm:flex-row items-center justify-between gap-4">
            {/* Text Input Area */}
            <div className="flex-1 w-full flex items-center px-2">
              <input
                type="text"
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder="Describe the application you want to build..."
                className="w-full bg-transparent text-[#F4F7FB] placeholder-[#728097] text-base sm:text-lg focus:outline-none font-sans antialiased selection:bg-[#22C7FF]/30"
                autoFocus
              />
            </div>

            {/* Right Action Group: Microphone + Build Button */}
            <div className="flex items-center gap-4 shrink-0">
              {/* Cyan Microphone Button */}
              <button
                type="button"
                onClick={handleToggleVoice}
                className={`p-2.5 rounded-full transition-all duration-300 active:scale-95 ${
                  isListening 
                    ? 'text-[#22C7FF] bg-[#22C7FF]/15 shadow-[0_0_18px_rgba(34,199,255,0.6)] animate-pulse' 
                    : 'text-[#22C7FF] hover:text-[#4DDAFF] hover:bg-white/5'
                }`}
                title={isListening ? 'Listening... click to stop' : 'Click to speak to JARVIS'}
              >
                <Mic className="w-6 h-6 stroke-[1.8]" />
              </button>

              {/* Crimson Build Button */}
              <button
                type="submit"
                className="btn-jarvis-red px-7 py-2.5 rounded-2xl font-medium text-base tracking-wide transition-all duration-300 active:scale-95"
              >
                Build
              </button>
            </div>
          </div>
        </form>

        {/* 3 Creation Modality Options (Centered underneath prompt box) */}
        <div className="mt-8 flex items-center justify-center gap-10 sm:gap-14">
          {/* Web App */}
          <button
            type="button"
            onClick={() => handleCategorySelect('web')}
            className={`group flex flex-col items-center gap-2 cursor-pointer transition-all duration-300 ${
              selectedCategory === 'web' ? 'opacity-100' : 'opacity-65 hover:opacity-90'
            }`}
          >
            <Globe className={`w-6 h-6 stroke-[1.5] transition-colors ${
              selectedCategory === 'web' ? 'text-[#F4F7FB]' : 'text-[#A7B3C5] group-hover:text-white'
            }`} />
            <span className="text-sm font-medium tracking-wide text-[#A7B3C5] group-hover:text-white">
              Web App
            </span>
            <div className={`h-[2px] transition-all duration-300 ${
              selectedCategory === 'web' ? 'w-8 bg-[#D81F45] shadow-[0_0_8px_rgba(216,31,69,0.7)]' : 'w-4 bg-transparent'
            }`} />
          </button>

          {/* Mobile App */}
          <button
            type="button"
            onClick={() => handleCategorySelect('mobile')}
            className={`group flex flex-col items-center gap-2 cursor-pointer transition-all duration-300 ${
              selectedCategory === 'mobile' ? 'opacity-100' : 'opacity-65 hover:opacity-90'
            }`}
          >
            <Smartphone className={`w-6 h-6 stroke-[1.5] transition-colors ${
              selectedCategory === 'mobile' ? 'text-[#F4F7FB]' : 'text-[#A7B3C5] group-hover:text-white'
            }`} />
            <span className="text-sm font-medium tracking-wide text-[#A7B3C5] group-hover:text-white">
              Mobile App
            </span>
            <div className={`h-[2px] transition-all duration-300 ${
              selectedCategory === 'mobile' ? 'w-8 bg-[#D81F45] shadow-[0_0_8px_rgba(216,31,69,0.7)]' : 'w-4 bg-transparent'
            }`} />
          </button>

          {/* AI Model */}
          <button
            type="button"
            onClick={() => handleCategorySelect('ai')}
            className={`group flex flex-col items-center gap-2 cursor-pointer transition-all duration-300 ${
              selectedCategory === 'ai' ? 'opacity-100' : 'opacity-65 hover:opacity-90'
            }`}
          >
            <Cpu className={`w-6 h-6 stroke-[1.5] transition-colors ${
              selectedCategory === 'ai' ? 'text-[#F4F7FB]' : 'text-[#A7B3C5] group-hover:text-white'
            }`} />
            <span className="text-sm font-medium tracking-wide text-[#A7B3C5] group-hover:text-white">
              AI Model
            </span>
            <div className={`h-[2px] transition-all duration-300 ${
              selectedCategory === 'ai' ? 'w-8 bg-[#D81F45] shadow-[0_0_8px_rgba(216,31,69,0.7)]' : 'w-4 bg-transparent'
            }`} />
          </button>
        </div>
      </main>

      {/* Bottom Subtle Status Line */}
      <footer className="relative z-20 w-full pb-5 text-center text-xs font-mono-jarvis text-[#728097] tracking-widest uppercase">
        Ready for natural language instruction
      </footer>
    </div>
  );
};
