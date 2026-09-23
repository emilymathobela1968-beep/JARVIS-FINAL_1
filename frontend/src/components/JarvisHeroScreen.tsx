import React, { useState } from 'react';
import { JarvisLogo } from './JarvisLogo';
import { JarvisArcOrb } from './JarvisArcOrb';
import { Menu, Paperclip, Keyboard, Mic, Send, X, Terminal, Cpu, Gauge, Radio, Shield, Settings } from 'lucide-react';
import { soundFX } from '../utils/audio';
import { jarvisVoice } from '../utils/voice';
import heroBgImage from '../assets/images/jarvis_hero_bg.jpg';

interface JarvisHeroScreenProps {
  onStartBuild: (directive: string) => void;
}

export const JarvisHeroScreen: React.FC<JarvisHeroScreenProps> = ({ onStartBuild }) => {
  const [inputText, setInputText] = useState('');
  const [isListening, setIsListening] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isKeyboardHintsOpen, setIsKeyboardHintsOpen] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const handleSubmit = (e?: React.FormEvent) => {
    e?.preventDefault();
    const finalDirective = inputText.trim();
    if (!finalDirective) return;
    soundFX.playIgnite();
    onStartBuild(finalDirective);
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
          setInputText(transcript);
          setIsListening(false);
          soundFX.playAcknowledge();
          if (transcript.trim().length > 3) {
            onStartBuild(transcript.trim());
          }
        },
        (listening) => {
          setIsListening(listening);
        }
      );
    }
  };

  const handleQuickSelect = (prompt: string) => {
    soundFX.playAcknowledge();
    setInputText(prompt);
  };

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-[#030508] text-[#F5F8FF] flex flex-col justify-between select-none">
      {/* 1. CINEMATIC BACKGROUND LAYER */}
      <div className="absolute inset-0 pointer-events-none z-0 overflow-hidden">
        {/* Photorealistic Orbital Penthouse BG Image */}
        <img
          src={heroBgImage}
          alt="JARVIS Orbital Penthouse Observation Deck"
          className="w-full h-full object-cover object-center opacity-85 scale-105 filter contrast-105"
        />

        {/* Ambient Dark Overlay to protect UI contrast */}
        <div className="absolute inset-0 bg-gradient-to-b from-[#030508]/85 via-[#030508]/40 to-[#030508]/90" />

        {/* Earth Curvature Atmospheric Glow on lower left */}
        <div 
          className="absolute -bottom-24 -left-20 w-[45vw] h-[45vh] rounded-full opacity-60 blur-3xl pointer-events-none"
          style={{
            background: 'radial-gradient(circle at center, rgba(34, 199, 255, 0.45) 0%, rgba(14, 80, 160, 0.25) 50%, transparent 80%)',
          }}
        />

        {/* Floor Reflections on Foreground Wet Obsidian Floor */}
        <div className="absolute bottom-0 left-0 right-0 h-44 bg-gradient-to-t from-[#030508] via-[#030508]/75 to-transparent pointer-events-none" />

        {/* Subtle Lens Flare Horizontal Accent across central horizon */}
        <div className="absolute top-[42%] left-0 right-0 h-[1px] bg-gradient-to-r from-transparent via-[#39B8FF]/20 to-transparent pointer-events-none" />
      </div>

      {/* 2. TOP BAR / HEADER */}
      <header className="relative z-30 w-full px-6 sm:px-10 pt-7 flex items-center justify-between">
        {/* Top-Left: High-End Sci-Fi Chrome JARVIS Logo */}
        <div className="flex items-center">
          <JarvisLogo size="md" />
        </div>

        {/* Top-Right: Premium Glass "MENU" Button */}
        <div className="relative">
          <button
            onClick={() => {
              soundFX.playBeep(620, 0.04);
              setIsMenuOpen(!isMenuOpen);
            }}
            className="flex items-center gap-2.5 px-5 py-2 rounded-2xl bg-[rgba(8,18,32,0.65)] hover:bg-[rgba(12,25,45,0.85)] border border-[rgba(70,170,255,0.35)] hover:border-[#39B8FF] backdrop-blur-md shadow-[0_4px_20px_rgba(0,0,0,0.5),0_0_15px_rgba(57,184,255,0.12)] transition-all duration-300 active:scale-95 group cursor-pointer"
          >
            <Menu className="w-4 h-4 text-[#58D7FF] group-hover:text-white transition-colors stroke-[2]" />
            <span className="text-xs font-semibold tracking-[0.2em] text-[#F5F8FF] uppercase font-mono-jarvis">
              MENU
            </span>
          </button>

          {/* Glass Menu Drawer */}
          {isMenuOpen && (
            <div className="absolute right-0 top-12 w-72 rounded-3xl p-3 bg-[#060D18]/95 border border-[rgba(70,170,255,0.35)] backdrop-blur-2xl shadow-[0_20px_60px_rgba(0,0,0,0.9),0_0_30px_rgba(57,184,255,0.2)] z-50 animate-in fade-in slide-in-from-top-3 duration-200">
              <div className="flex items-center justify-between px-3 py-2 border-b border-white/10 mb-2">
                <span className="text-xs font-mono-jarvis uppercase tracking-widest text-[#58D7FF]">
                  System Modules
                </span>
                <button
                  onClick={() => setIsMenuOpen(false)}
                  className="p-1 rounded-lg text-[#8B98AA] hover:text-white"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>

              <div className="space-y-1">
                {[
                  { title: 'Automotive Diagnostics', desc: 'CAN-bus & live telemetry', icon: Gauge, prompt: 'Build a premium automotive Live Data screen.' },
                  { title: 'Computer Control', desc: 'OS commands & desktop agents', icon: Terminal, prompt: 'Launch Windows automation orchestrator.' },
                  { title: 'Neural Model Lab', desc: 'OmniSynth multimodal runtime', icon: Cpu, prompt: 'Build an Autonomous Neural Intelligence Console.' },
                  { title: 'Cloud Operations', desc: 'Cluster nodes & network SLA', icon: Radio, prompt: 'Build an Executive Cloud Operations Dashboard.' },
                  { title: 'Security Perimeter', desc: 'mTLS & cryptographic mesh', icon: Shield, prompt: 'Audit perimeter zero-trust security mesh.' },
                  { title: 'Preferences', desc: 'Audio FX & hardware bridge', icon: Settings, prompt: '' },
                ].map((item, idx) => (
                  <button
                    key={idx}
                    onClick={() => {
                      if (item.prompt) {
                        setInputText(item.prompt);
                        setIsMenuOpen(false);
                        soundFX.playAcknowledge();
                      }
                    }}
                    className="w-full flex items-center gap-3 p-2.5 rounded-xl hover:bg-white/5 transition-colors text-left group"
                  >
                    <div className="w-7 h-7 rounded-lg bg-[#0A1626] border border-white/10 flex items-center justify-center text-[#58D7FF] group-hover:border-[#39B8FF]">
                      <item.icon className="w-3.5 h-3.5" />
                    </div>
                    <div>
                      <div className="text-xs font-medium text-[#F5F8FF] group-hover:text-[#58D7FF]">
                        {item.title}
                      </div>
                      <div className="text-[10px] text-[#8B98AA]">
                        {item.desc}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      </header>

      {/* 3. MAIN CENTERPIECE: Glowing Circular JARVIS Arc Reactor / Orb / Radar */}
      <main className="relative z-20 flex-1 flex flex-col items-center justify-center -mt-6 px-4">
        {/* Glowing Orb */}
        <div className="relative mb-2">
          <JarvisArcOrb size={430} isListening={isListening} />
        </div>

        {/* 4. MAIN GLASS COMMAND PANEL (Lower Center, exactly matching reference image) */}
        <div className="w-full max-w-4xl relative">
          <div 
            className="relative rounded-[32px] p-6 sm:p-7 transition-all duration-500"
            style={{
              background: 'linear-gradient(180deg, rgba(8, 18, 32, 0.68) 0%, rgba(5, 12, 22, 0.82) 100%)',
              backdropFilter: 'blur(20px) saturate(130%)',
              WebkitBackdropFilter: 'blur(20px) saturate(130%)',
              border: '1px solid rgba(70, 170, 255, 0.45)',
              boxShadow: '0 0 35px rgba(34, 199, 255, 0.16), 0 25px 60px rgba(0, 0, 0, 0.6), inset 0 1px 0 rgba(255, 255, 255, 0.08)',
            }}
          >
            {/* Top Border Glow Flare */}
            <div className="absolute top-0 left-1/4 right-1/4 h-[1.5px] bg-gradient-to-r from-transparent via-[#58D7FF] to-transparent shadow-[0_0_10px_#58D7FF]" />

            {/* Top Row: Circular Avatar ( J ) + JARVIS Label + Status Indicator */}
            <div className="flex items-center gap-3">
              {/* Circular Avatar */}
              <div className="w-9 h-9 rounded-full border border-[#39B8FF] bg-[#061120] flex items-center justify-center shadow-[0_0_14px_rgba(57,184,255,0.45)]">
                <span className="text-[#58D7FF] font-bold text-sm tracking-tighter">
                  J
                </span>
              </div>

              {/* JARVIS Brand & Ready Dot */}
              <div className="flex items-center gap-2.5">
                <span className="text-sm font-semibold tracking-[0.16em] text-[#F5F8FF] font-sans">
                  JARVIS
                </span>
                <span className="flex items-center gap-1.5 text-xs text-[#8B98AA] font-sans">
                  <span className="w-2 h-2 rounded-full bg-[#39B8FF] shadow-[0_0_8px_#39B8FF] animate-pulse" />
                  Ready
                </span>
              </div>
            </div>

            {/* Main Greeting Lines */}
            <div className="mt-3.5 space-y-1">
              <h1 className="text-xl sm:text-2xl font-normal text-[#F5F8FF] tracking-tight antialiased">
                I’m listening. What would you like to work on today?
              </h1>
              <p className="text-sm text-[#49a8ff]/80 antialiased font-normal">
                Speak naturally or type a command.
              </p>
            </div>

            {/* Bottom Embedded Command Input Bar */}
            <form
              onSubmit={handleSubmit}
              className="mt-5 rounded-full bg-[#050b14]/85 border border-[rgba(70,170,255,0.25)] px-3 py-2 flex items-center gap-3 shadow-[inset_0_2px_6px_rgba(0,0,0,0.6)] focus-within:border-[#39B8FF]/60 transition-all"
            >
              {/* Attachment Button */}
              <button
                type="button"
                onClick={() => {
                  soundFX.playBeep(560, 0.04);
                  setNotice('Attachments: Not configured');
                  setTimeout(() => setNotice(null), 2600);
                }}
                className="w-9 h-9 rounded-full flex items-center justify-center text-[#8B98AA] hover:text-[#F5F8FF] hover:bg-white/5 transition-all cursor-pointer shrink-0"
                title="Attachments — Not configured"
                data-testid="hero-attach-button"
              >
                <Paperclip className="w-4 h-4 stroke-[1.8]" />
              </button>

              {/* Text Input Field */}
              <input
                type="text"
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                placeholder="Talk to Jarvis or type a command..."
                className="flex-1 bg-transparent text-[#F5F8FF] placeholder-[#607085] text-sm focus:outline-none antialiased selection:bg-[#39B8FF]/30 font-sans"
                autoFocus
                data-testid="hero-command-input"
              />

              {/* Keyboard Button */}
              <button
                type="button"
                onClick={() => setIsKeyboardHintsOpen(!isKeyboardHintsOpen)}
                className="w-8 h-8 rounded-lg flex items-center justify-center text-[#8B98AA] hover:text-[#58D7FF] hover:bg-white/5 transition-all cursor-pointer shrink-0"
                title="Quick Prompt Suggestions"
              >
                <Keyboard className="w-4 h-4 stroke-[1.8]" />
              </button>

              {/* Glowing Circular Microphone Button */}
              <button
                type="button"
                onClick={handleToggleVoice}
                className={`w-11 h-11 rounded-full flex items-center justify-center transition-all duration-300 active:scale-95 cursor-pointer shrink-0 ${
                  isListening
                    ? 'bg-[#0B203D] border-2 border-[#58D7FF] text-white shadow-[0_0_25px_rgba(88,215,255,0.9)] animate-pulse'
                    : 'bg-[#0B1E38] border-2 border-[#39B8FF] text-white shadow-[0_0_18px_rgba(57,184,255,0.65)] hover:shadow-[0_0_24px_rgba(57,184,255,0.85)] hover:border-[#58D7FF]'
                }`}
                title={isListening ? 'Listening... click to stop' : 'Click to speak to JARVIS'}
              >
                <Mic className="w-5 h-5 stroke-[2]" />
              </button>

              {/* Send / Arrow Button */}
              <button
                type="submit"
                className="w-9 h-9 rounded-full bg-[#06101E] hover:bg-[#0B1C33] border border-[rgba(70,170,255,0.3)] hover:border-[#39B8FF] flex items-center justify-center text-[#8B98AA] hover:text-white transition-all active:scale-95 cursor-pointer shrink-0 shadow-sm"
                title="Send instruction to JARVIS"
                data-testid="hero-send-button"
              >
                <Send className="w-4 h-4 stroke-[1.8]" />
              </button>
            </form>

            {notice && (
              <div
                className="mt-3 px-3 py-2 rounded-2xl bg-[#1A1405]/80 border border-[#F5B942]/30 text-xs text-[#F5B942]"
                data-testid="hero-notice"
              >
                {notice}
              </div>
            )}

            {/* Quick Prompt Hints Modal / Popover */}
            {isKeyboardHintsOpen && (
              <div className="mt-3 p-3 rounded-2xl bg-[#050A14]/90 border border-white/10 flex flex-wrap gap-2 text-xs">
                <span className="text-[#8B98AA] self-center mr-1">Suggested:</span>
                {[
                  'Build a premium automotive Live Data screen.',
                  'Build an Executive Cloud Operations Dashboard.',
                  'Build an Autonomous Neural Intelligence Console.',
                ].map((sample, i) => (
                  <button
                    key={i}
                    onClick={() => {
                      handleQuickSelect(sample);
                      setIsKeyboardHintsOpen(false);
                    }}
                    className="px-3 py-1 rounded-xl bg-white/5 hover:bg-white/10 text-[#49a8ff] hover:text-white border border-white/5 transition-all text-left"
                  >
                    {sample}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>

      {/* 5. SUBTLE BOTTOM FOOTER */}
      <footer className="relative z-20 w-full pb-4 text-center text-xs font-mono-jarvis text-[#607085] tracking-widest uppercase">
        Autonomous Sovereign Intelligence // Ready for Directive
      </footer>
    </div>
  );
};
