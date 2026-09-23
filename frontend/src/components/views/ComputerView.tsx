import React, { useState } from 'react';
import { SystemProcess } from '../../types';
import { soundFX } from '../../utils/audio';

interface ComputerViewProps {
  onExecuteTool: (toolName: string, params: string) => void;
}

const INITIAL_PROCESSES: SystemProcess[] = [
  { id: '1', name: 'Jarvis.App.exe', pid: 14208, cpu: '1.4%', memory: '242 MB', status: 'Running', category: 'System' },
  { id: '2', name: 'WINWORD.EXE', pid: 18924, cpu: '0.2%', memory: '185 MB', status: 'Running', category: 'App' },
  { id: '3', name: 'EXCEL.EXE', pid: 12040, cpu: '0.1%', memory: '148 MB', status: 'Idle', category: 'App' },
  { id: '4', name: 'Code.exe (VS Code)', pid: 21900, cpu: '2.8%', memory: '612 MB', status: 'Running', category: 'Developer' },
  { id: '5', name: 'WindowsTerminal.exe', pid: 9812, cpu: '0.4%', memory: '98 MB', status: 'Running', category: 'Developer' },
  { id: '6', name: 'explorer.exe', pid: 4820, cpu: '0.3%', memory: '124 MB', status: 'Running', category: 'System' },
  { id: '7', name: 'msedge.exe', pid: 15304, cpu: '3.1%', memory: '840 MB', status: 'Running', category: 'App' },
];

const INITIAL_DOCUMENTS = [
  { name: 'JARVIS_V2_EMERGENT_HANDOFF.md', path: 'C:\\JARVIS_CODEX\\Jarvis.Windows.V2', type: 'Markdown', size: '11.3 KB' },
  { name: 'ENGINEERING_CHECKPOINT.md', path: 'C:\\JARVIS_CODEX\\Jarvis.Windows.V2', type: 'Markdown', size: '7.5 KB' },
  { name: 'Quarterly_Mission_Briefing.docx', path: 'C:\\Users\\Emily\\Documents', type: 'Word Document', size: '48.2 KB' },
  { name: 'Financial_Projections_2026.xlsx', path: 'C:\\Users\\Emily\\Documents\\Finances', type: 'Excel Sheet', size: '120.4 KB' },
  { name: 'ALEXIS_Hologram_Stage.pptx', path: 'C:\\Users\\Emily\\Presentations', type: 'PowerPoint', size: '8.4 MB' },
];

export const ComputerView: React.FC<ComputerViewProps> = ({ onExecuteTool }) => {
  const [processes, setProcesses] = useState<SystemProcess[]>(INITIAL_PROCESSES);
  const [selectedTab, setSelectedTab] = useState<'windows' | 'files' | 'office'>('windows');
  const [statusMessage, setStatusMessage] = useState<string>('Governed Computer subsystem ready.');
  const [activeWindow, setActiveWindow] = useState<string>('Jarvis Core');

  const handleLaunchApp = (appName: string, exeName: string) => {
    soundFX.playExecute();
    const newPid = Math.floor(10000 + Math.random() * 20000);
    const existing = processes.find(p => p.name.toLowerCase().includes(exeName.toLowerCase()));
    
    if (existing) {
      setActiveWindow(existing.name);
      setStatusMessage(`Focused running instance of ${appName} (PID ${existing.pid}).`);
      onExecuteTool('computer.focus_window', `PID=${existing.pid}`);
    } else {
      const newProcess: SystemProcess = {
        id: String(Date.now()),
        name: exeName,
        pid: newPid,
        cpu: '0.5%',
        memory: '130 MB',
        status: 'Running',
        category: 'App',
      };
      setProcesses([newProcess, ...processes]);
      setActiveWindow(exeName);
      setStatusMessage(`Launched ${appName} via Governed Windows Process Subsystem (PID ${newPid}).`);
      onExecuteTool('computer.launch_application', `Target=${appName}`);
    }
  };

  const handleCloseProcess = (pid: number, name: string) => {
    soundFX.playBeep(400, 0.1, 'sawtooth');
    setProcesses(processes.filter(p => p.pid !== pid));
    setStatusMessage(`Gracefully terminated ${name} (PID ${pid}) with governance confirmation.`);
    onExecuteTool('computer.graceful_close', `PID=${pid}`);
  };

  const handleFocusProcess = (name: string, pid: number) => {
    soundFX.playBeep(800, 0.06);
    setActiveWindow(name);
    setStatusMessage(`Window focus switched to: ${name} (PID: ${pid}).`);
    onExecuteTool('computer.focus_window', `Target=${name}`);
  };

  return (
    <div id="computer-view" className="w-full max-w-5xl mx-auto space-y-6">
      {/* Title & Governance Description */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl sm:text-3xl font-semibold text-[#F5FAFF]">Computer</h2>
          <span className="font-mono-jarvis text-xs px-3 py-1 bg-[#051E33] border border-[#45BCFF]/40 text-[#55C7FF] rounded-full">
            WINDOWS 11 OS RUNTIME
          </span>
        </div>
        <p className="text-sm sm:text-base text-[#8CBFEA] max-w-3xl">
          Governed Windows operations resolve application, process, window, control, file, and document state separately.
        </p>
      </div>

      {/* Governed Operational Pillars */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <button
          onClick={() => {
            soundFX.playBeep(600, 0.05);
            setSelectedTab('windows');
          }}
          className={`p-4 rounded-xl border text-left transition-all ${
            selectedTab === 'windows'
              ? 'bg-[#0E243A] border-[#45BCFF] shadow-[0_0_15px_rgba(69,188,255,0.2)]'
              : 'bg-[#071320]/60 border-[#45BCFF]/20 hover:border-[#45BCFF]/40'
          }`}
        >
          <div className="font-mono-jarvis text-xs font-bold text-[#45BCFF] tracking-widest mb-1">
            01 / WINDOWS
          </div>
          <div className="text-sm font-medium text-white mb-1">Processes & Windows</div>
          <div className="text-xs text-[#8CBFEA]">Discover / focus / state / graceful close</div>
        </button>

        <button
          onClick={() => {
            soundFX.playBeep(600, 0.05);
            setSelectedTab('files');
          }}
          className={`p-4 rounded-xl border text-left transition-all ${
            selectedTab === 'files'
              ? 'bg-[#0E243A] border-[#45BCFF] shadow-[0_0_15px_rgba(69,188,255,0.2)]'
              : 'bg-[#071320]/60 border-[#45BCFF]/20 hover:border-[#45BCFF]/40'
          }`}
        >
          <div className="font-mono-jarvis text-xs font-bold text-[#45BCFF] tracking-widest mb-1">
            02 / FILES
          </div>
          <div className="text-sm font-medium text-white mb-1">Explorer & Storage</div>
          <div className="text-xs text-[#8CBFEA]">Folders / files / Explorer windows</div>
        </button>

        <button
          onClick={() => {
            soundFX.playBeep(600, 0.05);
            setSelectedTab('office');
          }}
          className={`p-4 rounded-xl border text-left transition-all ${
            selectedTab === 'office'
              ? 'bg-[#0E243A] border-[#45BCFF] shadow-[0_0_15px_rgba(69,188,255,0.2)]'
              : 'bg-[#071320]/60 border-[#45BCFF]/20 hover:border-[#45BCFF]/40'
          }`}
        >
          <div className="font-mono-jarvis text-xs font-bold text-[#45BCFF] tracking-widest mb-1">
            03 / OFFICE
          </div>
          <div className="text-sm font-medium text-white mb-1">Productivity Suite</div>
          <div className="text-xs text-[#8CBFEA]">Documents / workbooks / presentations</div>
        </button>
      </div>

      {/* Action Notification Strip */}
      <div className="flex items-center justify-between px-4 py-3 bg-[#06101B]/80 border border-[#45BCFF]/25 rounded-xl font-mono-jarvis text-xs text-[#9CD5FF]">
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <span>Active Surface: <strong className="text-white">{activeWindow}</strong></span>
        </div>
        <span className="opacity-80">{statusMessage}</span>
      </div>

      {/* Quick App Launcher Bar */}
      <div className="bg-[#0A1624]/70 border border-[#45BCFF]/20 rounded-2xl p-4 space-y-3">
        <div className="text-xs font-mono-jarvis tracking-wider text-[#45BCFF]">
          LAUNCH APPROVED WINDOWS APPLICATIONS
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-2">
          <button
            onClick={() => handleLaunchApp('Microsoft Word', 'WINWORD.EXE')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-blue-400 font-bold">W</span>
            <span>Word</span>
          </button>
          <button
            onClick={() => handleLaunchApp('Microsoft Excel', 'EXCEL.EXE')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-emerald-400 font-bold">X</span>
            <span>Excel</span>
          </button>
          <button
            onClick={() => handleLaunchApp('PowerPoint', 'POWERPNT.EXE')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-orange-400 font-bold">P</span>
            <span>PowerPoint</span>
          </button>
          <button
            onClick={() => handleLaunchApp('Visual Studio Code', 'Code.exe')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-cyan-400 font-bold">{'</>'}</span>
            <span>VS Code</span>
          </button>
          <button
            onClick={() => handleLaunchApp('Windows Terminal', 'WindowsTerminal.exe')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-neutral-300 font-mono">$_</span>
            <span>Terminal</span>
          </button>
          <button
            onClick={() => handleLaunchApp('File Explorer', 'explorer.exe')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-amber-300">📁</span>
            <span>Explorer</span>
          </button>
          <button
            onClick={() => handleLaunchApp('Microsoft Edge', 'msedge.exe')}
            className="p-2.5 rounded-lg bg-[#0C1E30] hover:bg-[#153450] border border-[#45BCFF]/30 text-xs text-white font-medium flex flex-col items-center gap-1.5 transition-all active:scale-95"
          >
            <span className="text-base text-sky-400">🌐</span>
            <span>Browser</span>
          </button>
        </div>
      </div>

      {/* Tab Specific Views */}
      {selectedTab === 'windows' && (
        <div className="bg-[#081422]/90 border border-[#45BCFF]/25 rounded-2xl p-5 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-semibold text-[#F5FAFF]">Governed Windows Process Subsystem</h3>
            <span className="text-xs text-[#8CBFEA]">{processes.length} Processes Tracked</span>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs font-mono-jarvis">
              <thead className="text-[#8CBFEA] border-b border-[#45BCFF]/20">
                <tr>
                  <th className="py-2.5 px-3">PROCESS NAME</th>
                  <th className="py-2.5 px-3">PID</th>
                  <th className="py-2.5 px-3">CATEGORY</th>
                  <th className="py-2.5 px-3">CPU</th>
                  <th className="py-2.5 px-3">MEMORY</th>
                  <th className="py-2.5 px-3">STATUS</th>
                  <th className="py-2.5 px-3 text-right">GOVERNED ACTIONS</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#45BCFF]/10 text-white">
                {processes.map((p) => (
                  <tr key={p.id} className="hover:bg-[#0E243A]/50 transition-colors">
                    <td className="py-2.5 px-3 font-medium text-[#55C7FF]">{p.name}</td>
                    <td className="py-2.5 px-3 text-[#8CBFEA]">{p.pid}</td>
                    <td className="py-2.5 px-3">{p.category}</td>
                    <td className="py-2.5 px-3">{p.cpu}</td>
                    <td className="py-2.5 px-3">{p.memory}</td>
                    <td className="py-2.5 px-3">
                      <span className="px-2 py-0.5 rounded text-[10px] bg-emerald-950/60 text-emerald-400 border border-emerald-500/30">
                        {p.status}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right space-x-2">
                      <button
                        onClick={() => handleFocusProcess(p.name, p.pid)}
                        className="px-2 py-1 rounded bg-[#102B45] hover:bg-[#184065] text-[#55C7FF] text-[11px]"
                      >
                        Focus
                      </button>
                      <button
                        onClick={() => handleCloseProcess(p.pid, p.name)}
                        className="px-2 py-1 rounded bg-red-950/40 hover:bg-red-900/60 text-red-300 text-[11px] border border-red-500/30"
                      >
                        Close
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {(selectedTab === 'files' || selectedTab === 'office') && (
        <div className="bg-[#081422]/90 border border-[#45BCFF]/25 rounded-2xl p-5 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-semibold text-[#F5FAFF]">
              {selectedTab === 'files' ? 'Governed File System Explorer' : 'Managed Office Documents'}
            </h3>
            <span className="text-xs text-[#8CBFEA]">Windows 11 Context</span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {INITIAL_DOCUMENTS.map((doc, idx) => (
              <div 
                key={idx}
                className="p-3.5 rounded-xl bg-[#0B1D2F]/70 border border-[#45BCFF]/20 hover:border-[#55C7FF]/50 transition-all flex items-start justify-between group"
              >
                <div className="space-y-1">
                  <div className="text-sm font-medium text-[#F5FAFF] group-hover:text-[#55C7FF] transition-colors">
                    {doc.name}
                  </div>
                  <div className="text-xs font-mono-jarvis text-[#8CBFEA] break-all">
                    {doc.path}
                  </div>
                  <div className="text-[11px] text-[#5E83A8]">
                    {doc.type} • {doc.size}
                  </div>
                </div>
                <button
                  onClick={() => {
                    soundFX.playAcknowledge();
                    setStatusMessage(`Opened ${doc.name} in associated Windows application.`);
                    onExecuteTool('computer.open_document', `Path=${doc.path}\\${doc.name}`);
                  }}
                  className="px-3 py-1.5 rounded-lg bg-[#143452] hover:bg-[#1E4D7A] text-xs font-mono-jarvis text-[#55C7FF] shrink-0 active:scale-95"
                >
                  Open
                </button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
