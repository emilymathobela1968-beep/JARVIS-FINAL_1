import React from 'react';
import { VoiceState, ActivityLogItem } from '../../types';
import { soundFX } from '../../utils/audio';

interface SystemViewProps {
  voiceState: VoiceState;
  onStartVoice: () => void;
  onStopVoice: () => void;
  latestTranscript: string;
  latestResponse: string;
  activityLogs: ActivityLogItem[];
  onClearLogs: () => void;
}

export const SystemView: React.FC<SystemViewProps> = ({
  voiceState,
  onStartVoice,
  onStopVoice,
  latestTranscript,
  latestResponse,
  activityLogs,
  onClearLogs,
}) => {
  const isVoiceActive = voiceState !== 'OFFLINE';

  return (
    <div id="system-diagnostics-view" className="w-full max-w-5xl mx-auto space-y-6">
      {/* Title & Description */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl sm:text-3xl font-semibold text-[#F5FAFF]">System / Diagnostics</h2>
          <span className="font-mono-jarvis text-xs px-3 py-1 bg-[#1A1828] border border-cyan-500/40 text-[#55C7FF] rounded-full">
            REALTIME TELEMETRY & GOVERNANCE
          </span>
        </div>
        <p className="text-sm sm:text-base text-[#8CBFEA] max-w-3xl">
          Detailed lifecycle, Realtime WebRTC evidence, capability, and governed tool dispatch remain available without dominating the operating surface.
        </p>
      </div>

      {/* 3 Status Cards (Transcript, Jarvis Response, Runtime Controls) */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Latest Transcript Card */}
        <div className="bg-[#081320]/90 border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-col justify-between space-y-2">
          <div>
            <div className="text-xs font-mono-jarvis text-[#8CBFEA] mb-1">LATEST TRANSCRIPT</div>
            <div className="text-sm text-white font-medium min-h-[48px]">
              {latestTranscript || 'No transcript yet.'}
            </div>
          </div>
          <div className="text-[10px] font-mono-jarvis text-[#55C7FF] opacity-70">
            Source: Web Speech / Realtime Audio Input
          </div>
        </div>

        {/* Jarvis Response Card */}
        <div className="bg-[#081320]/90 border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-col justify-between space-y-2">
          <div>
            <div className="text-xs font-mono-jarvis text-[#8CBFEA] mb-1">JARVIS RESPONSE</div>
            <div className="text-sm text-[#F5FAFF] font-medium min-h-[48px] line-clamp-3">
              {latestResponse || 'Ready.'}
            </div>
          </div>
          <div className="text-[10px] font-mono-jarvis text-[#55C7FF] opacity-70">
            Agent Output Buffer
          </div>
        </div>

        {/* Runtime Controls Card */}
        <div className="bg-[#081320]/90 border border-[#45BCFF]/30 rounded-2xl p-4 flex flex-col justify-between space-y-3">
          <div>
            <div className="text-xs font-mono-jarvis text-[#49C6FF] font-bold mb-2">RUNTIME CONTROLS</div>
            <div className="space-y-2">
              <div className="flex gap-2">
                <button
                  id="btn-start-voice"
                  onClick={() => {
                    soundFX.playPulse();
                    onStartVoice();
                  }}
                  disabled={isVoiceActive}
                  className={`flex-1 py-1.5 rounded-lg text-xs font-mono-jarvis font-semibold transition-all ${
                    !isVoiceActive
                      ? 'bg-[#183E63] hover:bg-[#225585] text-[#55C7FF] border border-[#55C7FF]'
                      : 'bg-[#0E2030] text-gray-500 cursor-not-allowed'
                  }`}
                >
                  Start Voice
                </button>
                <button
                  id="btn-stop-voice"
                  onClick={() => {
                    soundFX.playBeep(400, 0.08);
                    onStopVoice();
                  }}
                  disabled={!isVoiceActive}
                  className={`flex-1 py-1.5 rounded-lg text-xs font-mono-jarvis font-semibold transition-all ${
                    isVoiceActive
                      ? 'bg-red-950/70 hover:bg-red-900 border border-red-500/50 text-red-200'
                      : 'bg-[#0E2030] text-gray-500 cursor-not-allowed'
                  }`}
                >
                  Stop Voice
                </button>
              </div>

              <div className="flex gap-2">
                <button
                  onClick={() => {
                    soundFX.playExecute();
                    onStartVoice();
                  }}
                  className="flex-1 py-1.5 rounded-lg text-xs font-mono-jarvis bg-[#102A42] hover:bg-[#183D61] text-[#9CD5FF] border border-[#45BCFF]/30"
                >
                  Realtime Sync
                </button>
                <button
                  onClick={() => {
                    soundFX.playBeep(500, 0.05);
                    onClearLogs();
                  }}
                  className="flex-1 py-1.5 rounded-lg text-xs font-mono-jarvis bg-[#0B1B2B] hover:bg-[#122A42] text-[#8CBFEA] border border-[#45BCFF]/20"
                >
                  Clear Logs
                </button>
              </div>
            </div>
          </div>
          <div className="text-[10px] font-mono-jarvis text-emerald-400">
            Current Status: {voiceState}
          </div>
        </div>
      </div>

      {/* Realtime WebRTC Readiness Evidence Checklist */}
      <div className="bg-[#091524]/80 border border-[#45BCFF]/25 rounded-2xl p-4 space-y-3">
        <div className="text-xs font-mono-jarvis font-bold text-[#45BCFF] tracking-wider">
          WEBRTC REALTIME READINESS GATE EVIDENCE
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs font-mono-jarvis">
          <div className="p-2.5 rounded-xl bg-[#050E17] border border-emerald-500/40 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-emerald-300">mic.stream.started</span>
          </div>
          <div className="p-2.5 rounded-xl bg-[#050E17] border border-emerald-500/40 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-emerald-300">data.channel.open</span>
          </div>
          <div className="p-2.5 rounded-xl bg-[#050E17] border border-emerald-500/40 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-emerald-300">remote.audio.track</span>
          </div>
          <div className="p-2.5 rounded-xl bg-[#050E17] border border-emerald-500/40 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
            <span className="text-emerald-300">remote.audio.playing</span>
          </div>
        </div>
      </div>

      {/* Governed Capability Activity Log Table */}
      <div className="bg-[#081320]/90 border border-[#45BCFF]/30 rounded-2xl p-5 space-y-3">
        <div className="flex items-center justify-between">
          <div className="text-xs font-mono-jarvis font-bold text-[#45BCFF] tracking-wider">
            CAPABILITY ACTIVITY LOG ({activityLogs.length})
          </div>
          <span className="text-xs font-mono-jarvis text-[#8CBFEA]">Governed Dispatch Audited</span>
        </div>

        <div className="overflow-x-auto max-h-[320px] overflow-y-auto">
          <table className="w-full text-left text-xs font-mono-jarvis">
            <thead className="text-[#8CBFEA] border-b border-[#45BCFF]/20 sticky top-0 bg-[#081320]">
              <tr>
                <th className="py-2 px-3">TIMESTAMP</th>
                <th className="py-2 px-3">CAPABILITY TOOL</th>
                <th className="py-2 px-3">ACTION</th>
                <th className="py-2 px-3">STATUS</th>
                <th className="py-2 px-3">DETAILS</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#45BCFF]/10 text-white">
              {activityLogs.map((log) => (
                <tr key={log.id} className="hover:bg-[#0E243A]/40 transition-colors">
                  <td className="py-2 px-3 text-[#8CBFEA]">{log.timestamp}</td>
                  <td className="py-2 px-3 text-[#55C7FF] font-semibold">{log.tool}</td>
                  <td className="py-2 px-3">{log.action}</td>
                  <td className="py-2 px-3">
                    <span className={`px-2 py-0.5 rounded text-[10px] ${
                      log.status === 'SUCCESS' 
                        ? 'bg-emerald-950 text-emerald-300 border border-emerald-500/30'
                        : log.status === 'EXECUTING'
                        ? 'bg-blue-950 text-blue-300 border border-blue-500/30'
                        : 'bg-purple-950 text-purple-300 border border-purple-500/30'
                    }`}>
                      {log.status}
                    </span>
                  </td>
                  <td className="py-2 px-3 text-[#9CD5FF] text-[11px] truncate max-w-xs">
                    {log.details}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
