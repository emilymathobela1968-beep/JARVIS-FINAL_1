import React, { useState } from 'react';
import { AlexisMediaCandidate } from '../../types';
import { soundFX } from '../../utils/audio';

interface MediaViewProps {
  onExecuteTool: (tool: string, params: string) => void;
}

const INITIAL_CANDIDATES: AlexisMediaCandidate[] = [
  {
    id: 'ALEXIS_PROMO_001',
    scene: 1,
    title: 'Cinematic Arc Reactor Initial Horizon',
    aspectRatio: '16:9',
    status: 'Approved',
    generatedAt: '2026-09-20 23:14',
    thumbnailColor: 'from-blue-900 via-cyan-900 to-black',
    description: 'Cinematic wide frame of JARVIS glowing Arc Reactor HUD with high-contrast skyline.',
  },
  {
    id: 'ALEXIS_PROMO_002',
    scene: 2,
    title: 'Holographic Barehands Spatial Interaction',
    aspectRatio: '16:9',
    status: 'Pending',
    generatedAt: '2026-09-21 18:40',
    thumbnailColor: 'from-purple-900 via-indigo-950 to-black',
    description: 'Human hands manipulating floating holographic wireframes and quantum UI panels.',
  },
];

export const MediaView: React.FC<MediaViewProps> = ({ onExecuteTool }) => {
  const [scene, setScene] = useState<number>(1);
  const [candidateInput, setCandidateInput] = useState<string>('ALEXIS_PROMO_002');
  const [statusText, setStatusText] = useState<string>('ALEXIS_PROMO_001 ready.');
  const [candidates, setCandidates] = useState<AlexisMediaCandidate[]>(INITIAL_CANDIDATES);
  const [selectedCandidate, setSelectedCandidate] = useState<AlexisMediaCandidate>(INITIAL_CANDIDATES[0]);

  const handleGenerate = () => {
    soundFX.playExecute();
    const newId = `ALEXIS_PROMO_00${candidates.length + 1}`;
    const newCand: AlexisMediaCandidate = {
      id: newId,
      scene: scene,
      title: `Scene ${scene} Candidate Visual Render`,
      aspectRatio: '16:9',
      status: 'Pending',
      generatedAt: new Date().toISOString().slice(0, 16).replace('T', ' '),
      thumbnailColor: 'from-cyan-900 via-blue-950 to-black',
      description: `Governed synthetic generation for Scene ${scene} promotional materials.`,
    };
    setCandidates([newCand, ...candidates]);
    setSelectedCandidate(newCand);
    setCandidateInput(newId);
    setStatusText(`Generated new candidate ${newId} for Scene ${scene}.`);
    onExecuteTool('media.generate_candidate', `Scene=${scene},ID=${newId}`);
  };

  const handleRegister = () => {
    soundFX.playAcknowledge();
    setStatusText(`Registered candidate ${candidateInput} to governed media catalog.`);
    onExecuteTool('media.register_candidate', `ID=${candidateInput}`);
  };

  const handleApprove = () => {
    soundFX.playExecute();
    setCandidates(candidates.map(c => c.id === candidateInput ? { ...c, status: 'Approved' } : c));
    if (selectedCandidate.id === candidateInput) {
      setSelectedCandidate({ ...selectedCandidate, status: 'Approved' });
    }
    setStatusText(`Approved candidate ${candidateInput} for production release.`);
    onExecuteTool('media.approve_candidate', `ID=${candidateInput}`);
  };

  const handleReject = () => {
    soundFX.playBeep(350, 0.1, 'sawtooth');
    setCandidates(candidates.map(c => c.id === candidateInput ? { ...c, status: 'Rejected' } : c));
    if (selectedCandidate.id === candidateInput) {
      setSelectedCandidate({ ...selectedCandidate, status: 'Rejected' });
    }
    setStatusText(`Rejected candidate ${candidateInput}. Revision requested.`);
    onExecuteTool('media.reject_candidate', `ID=${candidateInput}`);
  };

  return (
    <div id="media-view" className="w-full max-w-5xl mx-auto space-y-6">
      {/* Title & Description */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl sm:text-3xl font-semibold text-[#F5FAFF]">Media</h2>
          <span className="font-mono-jarvis text-xs px-3 py-1 bg-[#152336] border border-[#45BCFF]/40 text-[#55C7FF] rounded-full">
            ALEXIS PROMO ENGINE
          </span>
        </div>
        <p className="text-sm sm:text-base text-[#8CBFEA] max-w-3xl">
          ALEXIS promo media remains governed and evidence-backed.
        </p>
      </div>

      {/* Control Card */}
      <div className="bg-[#091524]/90 border border-[#45BCFF]/30 rounded-2xl p-5 space-y-4 backdrop-blur-md">
        <div className="text-xs font-mono-jarvis font-bold text-[#45BCFF] tracking-wider">
          ALEXIS PROMO MEDIA PIPELINE
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-12 gap-3">
          <div className="sm:col-span-3">
            <label className="block text-[11px] font-mono-jarvis text-[#8CBFEA] mb-1">
              SCENE
            </label>
            <input
              type="number"
              min={1}
              max={10}
              value={scene}
              onChange={(e) => setScene(Number(e.target.value))}
              className="w-full bg-[#050D16] border border-[#45BCFF]/30 rounded-xl px-3 py-2 text-sm font-mono-jarvis text-[#F5FAFF] focus:outline-none focus:border-[#55C7FF]"
            />
          </div>

          <div className="sm:col-span-9">
            <label className="block text-[11px] font-mono-jarvis text-[#8CBFEA] mb-1">
              CANDIDATE PATH OR CANDIDATE ID
            </label>
            <input
              type="text"
              value={candidateInput}
              onChange={(e) => setCandidateInput(e.target.value)}
              placeholder="Candidate path or candidate ID"
              className="w-full bg-[#050D16] border border-[#45BCFF]/30 rounded-xl px-3 py-2 text-sm font-mono-jarvis text-[#F5FAFF] focus:outline-none focus:border-[#55C7FF]"
            />
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap gap-2.5 pt-1">
          <button
            onClick={handleGenerate}
            className="px-5 py-2 rounded-xl bg-[#143452] hover:bg-[#1E4D7A] border border-[#55C7FF] text-[#E8F7FF] text-xs sm:text-sm font-semibold transition-all active:scale-95 shadow-[0_0_12px_rgba(69,188,255,0.2)]"
          >
            Generate
          </button>
          <button
            onClick={handleRegister}
            className="px-5 py-2 rounded-xl bg-[#0F263D] hover:bg-[#163859] border border-[#45BCFF]/40 text-[#9CD5FF] text-xs sm:text-sm font-medium transition-all active:scale-95"
          >
            Register
          </button>
          <button
            onClick={handleApprove}
            className="px-5 py-2 rounded-xl bg-emerald-950/60 hover:bg-emerald-900/80 border border-emerald-500/50 text-emerald-300 text-xs sm:text-sm font-medium transition-all active:scale-95 shadow-sm"
          >
            Approve
          </button>
          <button
            onClick={handleReject}
            className="px-5 py-2 rounded-xl bg-red-950/60 hover:bg-red-900/80 border border-red-500/50 text-red-300 text-xs sm:text-sm font-medium transition-all active:scale-95 shadow-sm"
          >
            Reject
          </button>
        </div>

        {/* Status Callout */}
        <div className="p-3 rounded-xl bg-[#050D16] border border-[#45BCFF]/20 text-xs font-mono-jarvis text-[#8CBFEA]">
          {statusText}
        </div>
      </div>

      {/* Candidate Inspector & Canvas Preview */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Rendered Preview Visualizer */}
        <div className="bg-[#081320] border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-col space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">PREVIEW CANVAS</span>
            <span className={`text-[10px] font-mono-jarvis px-2 py-0.5 rounded ${
              selectedCandidate.status === 'Approved'
                ? 'bg-emerald-950 text-emerald-300 border border-emerald-500/30'
                : selectedCandidate.status === 'Rejected'
                ? 'bg-red-950 text-red-300 border border-red-500/30'
                : 'bg-amber-950 text-amber-300 border border-amber-500/30'
            }`}>
              {selectedCandidate.status}
            </span>
          </div>

          <div className={`w-full aspect-video rounded-xl bg-gradient-to-br ${selectedCandidate.thumbnailColor} border border-[#45BCFF]/40 relative overflow-hidden flex flex-col items-center justify-center p-6 text-center`}>
            {/* Holographic grid lines */}
            <div className="absolute inset-0 hologram-grid opacity-30 pointer-events-none" />
            <div className="relative z-10 space-y-2">
              <div className="w-12 h-12 mx-auto rounded-full border-2 border-cyan-400/80 bg-cyan-950/50 flex items-center justify-center text-cyan-300 text-xl animate-pulse">
                ✦
              </div>
              <div className="text-sm font-semibold text-white tracking-wide">
                {selectedCandidate.title}
              </div>
              <div className="text-xs text-[#8CBFEA] max-w-xs line-clamp-2">
                {selectedCandidate.description}
              </div>
            </div>
            <div className="absolute bottom-2 right-3 font-mono-jarvis text-[10px] text-cyan-400 opacity-70">
              {selectedCandidate.id} • {selectedCandidate.aspectRatio}
            </div>
          </div>
        </div>

        {/* Catalog Table */}
        <div className="bg-[#081320] border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-col space-y-3">
          <div className="text-xs font-mono-jarvis text-[#45BCFF] font-bold">
            REGISTERED CANDIDATES
          </div>
          <div className="space-y-2 overflow-y-auto max-h-[260px] pr-1">
            {candidates.map((c) => (
              <div
                key={c.id}
                onClick={() => {
                  soundFX.playBeep(640, 0.04);
                  setSelectedCandidate(c);
                  setCandidateInput(c.id);
                  setScene(c.scene);
                }}
                className={`p-3 rounded-xl border cursor-pointer transition-all ${
                  selectedCandidate.id === c.id
                    ? 'bg-[#102B45] border-[#55C7FF]'
                    : 'bg-[#06101B] border-[#45BCFF]/20 hover:border-[#45BCFF]/40'
                }`}
              >
                <div className="flex items-center justify-between text-xs font-medium text-white mb-1">
                  <span className="font-mono-jarvis text-[#55C7FF]">{c.id}</span>
                  <span className="text-[11px] text-[#8CBFEA]">{c.status}</span>
                </div>
                <div className="text-xs text-[#9CD5FF] line-clamp-1">{c.title}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
