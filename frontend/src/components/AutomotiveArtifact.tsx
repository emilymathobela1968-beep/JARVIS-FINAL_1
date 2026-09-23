import React, { useState, useEffect } from 'react';
import { 
  Zap, 
  Activity, 
  Flame, 
  Gauge as GaugeIcon, 
  Sliders, 
  ShieldCheck, 
  RefreshCw, 
  CheckCircle2, 
  TrendingUp,
  AlertCircle
} from 'lucide-react';
import { soundFX } from '../utils/audio';

export interface AutomotiveConfig {
  title: string;
  revision: string;
  gaugeLayout: 'standard' | 'single-row-compact';
  theme: 'cyber-cyan' | 'crimson-track';
  driveMode: 'COMFORT' | 'SPORT' | 'SPORT+' | 'TRACK';
  showTelemetryStream: boolean;
  highlightCardId?: string;
}

interface AutomotiveArtifactProps {
  config: AutomotiveConfig;
  onDispatchAction?: (actionName: string) => void;
}

export const AutomotiveArtifact: React.FC<AutomotiveArtifactProps> = ({
  config,
  onDispatchAction,
}) => {
  const [speed, setSpeed] = useState(142);
  const [rpm, setRpm] = useState(6800);
  const [gear, setGear] = useState('D7');
  const [powerKw, setPowerKw] = useState(485);
  const [driveMode, setDriveMode] = useState<'COMFORT' | 'SPORT' | 'SPORT+' | 'TRACK'>(config.driveMode || 'SPORT+');
  const [isAccelerating, setIsAccelerating] = useState(false);
  const [canBusLogs, setCanBusLogs] = useState<string[]>([
    '0x1F4 [POWERTRAIN] INVERTER_FREQ=18.4kHz VOLT=840V',
    '0x2A1 [CHASSIS_CAN] LAT_G=+1.28 LONG_G=+0.85 YAW_RATE=4.2deg/s',
    '0x3E8 [THERMAL_SYS] PUMP_1=100% COOLANT_TEMP=33.4C FLOW=24L/min',
  ]);

  // Dynamic telemetry tick
  useEffect(() => {
    const timer = setInterval(() => {
      if (!isAccelerating) {
        setSpeed((prev) => {
          const delta = (Math.random() - 0.48) * 3;
          return Math.min(198, Math.max(85, Math.round(prev + delta)));
        });
        setRpm((prev) => {
          const delta = (Math.random() - 0.48) * 120;
          return Math.min(8400, Math.max(3200, Math.round(prev + delta)));
        });
        setPowerKw((prev) => {
          const delta = (Math.random() - 0.48) * 15;
          return Math.min(760, Math.max(180, Math.round(prev + delta)));
        });
      }
    }, 450);

    return () => clearInterval(timer);
  }, [isAccelerating]);

  // Trigger acceleration burst
  const handleBoost = () => {
    soundFX.playIgnite();
    setIsAccelerating(true);
    setSpeed(178);
    setRpm(8100);
    setPowerKw(720);
    setGear('D7');

    setCanBusLogs((prev) => [
      `0x100 [LAUNCH_SEQ] TORQUE_VECTOR_LOCK=100% BOOST=ARMED`,
      ...prev.slice(0, 4),
    ]);

    setTimeout(() => {
      setIsAccelerating(false);
      soundFX.playExecute();
    }, 1800);

    onDispatchAction?.('Launch Boost Triggered (0-60mph 1.84s)');
  };

  const isCompactRow = config.gaugeLayout === 'single-row-compact';

  return (
    <div className="w-full h-full flex flex-col bg-[#030508] text-[#F5F8FF] overflow-y-auto selection:bg-[#39B8FF]/30">
      {/* Top Telemetry Header */}
      <header className="px-6 py-3.5 bg-[#060D18]/90 border-b border-[rgba(70,170,255,0.18)] flex flex-wrap items-center justify-between gap-3 shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-2.5 h-2.5 rounded-full bg-[#18CC8A] shadow-[0_0_10px_#18CC8A] animate-pulse" />
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm sm:text-base font-bold tracking-wider font-mono-jarvis text-[#F5F8FF] uppercase">
                {config.title || 'APEX HYPERION GT-V // LIVE TELEMETRY'}
              </h1>
              <span className="px-2 py-0.5 rounded-md text-[10px] font-mono-jarvis bg-[#FF294D]/15 text-[#FF294D] border border-[#FF294D]/40">
                {config.revision}
              </span>
            </div>
            <div className="text-[11px] text-[#8B98AA] font-mono">
              VIN: 1FA6P8CF9H • 840V SILICON CARBIDE ARCHITECTURE
            </div>
          </div>
        </div>

        {/* Drive Modes & Boost Trigger */}
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1 bg-[#050B14] p-1 rounded-xl border border-white/10 text-xs font-mono-jarvis">
            {(['COMFORT', 'SPORT', 'SPORT+', 'TRACK'] as const).map((mode) => (
              <button
                key={mode}
                onClick={() => {
                  soundFX.playBeep(620, 0.04);
                  setDriveMode(mode);
                  onDispatchAction?.(`Drive Mode changed to ${mode}`);
                }}
                className={`px-2.5 py-1 rounded-lg transition-all ${
                  driveMode === mode
                    ? 'bg-[#FF294D] text-white font-bold shadow-[0_0_10px_rgba(255,41,77,0.5)]'
                    : 'text-[#8B98AA] hover:text-white'
                }`}
              >
                {mode}
              </button>
            ))}
          </div>

          <button
            onClick={handleBoost}
            className="px-3.5 py-1.5 rounded-xl bg-gradient-to-r from-[#C81E40] to-[#FF294D] hover:from-[#FF294D] hover:to-[#FF476B] text-white font-semibold text-xs tracking-wider font-mono-jarvis flex items-center gap-1.5 shadow-[0_0_15px_rgba(255,41,77,0.4)] active:scale-95 transition-all"
          >
            <Flame className="w-3.5 h-3.5" /> BOOST
          </button>
        </div>
      </header>

      {/* Main Cockpit Telemetry Content */}
      <main className="flex-1 p-5 space-y-5 overflow-y-auto">
        
        {/* Quick KPI Status Bar */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className="p-3.5 rounded-2xl bg-[#060D18]/70 border border-[rgba(70,170,255,0.18)]">
            <div className="text-[10px] text-[#8B98AA] font-mono uppercase">BATTERY SOC (840V)</div>
            <div className="text-xl font-bold font-mono text-[#F5F8FF] flex items-center gap-2 mt-0.5">
              <span>92%</span>
              <span className="text-xs text-[#18CC8A] font-normal">418 mi</span>
            </div>
            <div className="w-full bg-white/10 h-1.5 rounded-full mt-2 overflow-hidden">
              <div className="bg-[#18CC8A] h-full" style={{ width: '92%' }} />
            </div>
          </div>

          <div className="p-3.5 rounded-2xl bg-[#060D18]/70 border border-[rgba(70,170,255,0.18)]">
            <div className="text-[10px] text-[#8B98AA] font-mono uppercase">TORQUE VECTORING</div>
            <div className="text-xl font-bold font-mono text-[#58D7FF] mt-0.5">
              38% F / 62% R
            </div>
            <div className="text-[10px] text-[#8B98AA] mt-1">Dual Permanent Magnet AC</div>
          </div>

          <div className="p-3.5 rounded-2xl bg-[#060D18]/70 border border-[rgba(70,170,255,0.18)]">
            <div className="text-[10px] text-[#8B98AA] font-mono uppercase">LATERAL G-LOAD</div>
            <div className="text-xl font-bold font-mono text-[#FF294D] mt-0.5">
              1.28 G
            </div>
            <div className="text-[10px] text-[#8B98AA] mt-1">Apex Active Aero Wing +4°</div>
          </div>

          <div className="p-3.5 rounded-2xl bg-[#060D18]/70 border border-[rgba(70,170,255,0.18)]">
            <div className="text-[10px] text-[#8B98AA] font-mono uppercase">THERMAL MANAGEMENT</div>
            <div className="text-xl font-bold font-mono text-[#18CC8A] mt-0.5">
              33.4°C
            </div>
            <div className="text-[10px] text-[#8B98AA] mt-1">Cryo-Loop Pump 100%</div>
          </div>
        </div>

        {/* GAUGES DISPLAY ZONE: Either Standard (Large dual) or Single-Row Compact */}
        {isCompactRow ? (
          /* ========================================================
             REVISION REV-002: COMPACT SINGLE ROW TELEMETRY GAUGES
             (Result of "Make the gauges smaller and move them into one row")
             ======================================================== */
          <div className="p-5 rounded-3xl bg-[#060D18]/85 border border-[rgba(70,170,255,0.3)] shadow-[0_0_30px_rgba(57,184,255,0.08)] space-y-3">
            <div className="flex items-center justify-between text-xs font-mono-jarvis text-[#8B98AA]">
              <span className="text-[#58D7FF] font-semibold flex items-center gap-1.5">
                <GaugeIcon className="w-3.5 h-3.5" /> REFACTORED: COMPACT SINGLE-ROW COCKPIT INSTRUMENTS
              </span>
              <span className="text-[11px] bg-[#18CC8A]/15 text-[#18CC8A] px-2 py-0.5 rounded border border-[#18CC8A]/30">
                ACTIVE REVISION: REV-002
              </span>
            </div>

            {/* 4 Smaller Gauges Aligned Side-by-Side in One Single Row */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-1">
              {/* Gauge 1: Speedometer */}
              <div className="p-3.5 rounded-2xl bg-[#03060C] border border-[rgba(70,170,255,0.2)] flex items-center gap-3">
                <div className="relative w-16 h-16 shrink-0 flex items-center justify-center">
                  <svg className="w-full h-full -rotate-90" viewBox="0 0 60 60">
                    <circle cx="30" cy="30" r="24" stroke="rgba(255,255,255,0.1)" strokeWidth="4" fill="none" />
                    <circle
                      cx="30"
                      cy="30"
                      r="24"
                      stroke="#58D7FF"
                      strokeWidth="4"
                      fill="none"
                      strokeDasharray="150"
                      strokeDashoffset={150 - (speed / 200) * 120}
                      strokeLinecap="round"
                    />
                  </svg>
                  <span className="absolute font-mono font-bold text-sm text-white">{speed}</span>
                </div>
                <div>
                  <div className="text-[10px] text-[#8B98AA] font-mono">VELOCITY</div>
                  <div className="text-xs font-bold text-[#F5F8FF]">MPH</div>
                  <div className="text-[10px] text-[#18CC8A] font-mono">GEAR {gear}</div>
                </div>
              </div>

              {/* Gauge 2: Powertrain RPM */}
              <div className="p-3.5 rounded-2xl bg-[#03060C] border border-[rgba(70,170,255,0.2)] flex items-center gap-3">
                <div className="relative w-16 h-16 shrink-0 flex items-center justify-center">
                  <svg className="w-full h-full -rotate-90" viewBox="0 0 60 60">
                    <circle cx="30" cy="30" r="24" stroke="rgba(255,255,255,0.1)" strokeWidth="4" fill="none" />
                    <circle
                      cx="30"
                      cy="30"
                      r="24"
                      stroke="#FF294D"
                      strokeWidth="4"
                      fill="none"
                      strokeDasharray="150"
                      strokeDashoffset={150 - (rpm / 9000) * 120}
                      strokeLinecap="round"
                    />
                  </svg>
                  <span className="absolute font-mono font-bold text-xs text-white">{(rpm/1000).toFixed(1)}k</span>
                </div>
                <div>
                  <div className="text-[10px] text-[#8B98AA] font-mono">MOTOR RPM</div>
                  <div className="text-xs font-bold text-[#F5F8FF]">{rpm} RPM</div>
                  <div className="text-[10px] text-[#FF294D] font-mono">REDLINE 9k</div>
                </div>
              </div>

              {/* Gauge 3: Power kW */}
              <div className="p-3.5 rounded-2xl bg-[#03060C] border border-[rgba(70,170,255,0.2)] flex items-center gap-3">
                <div className="relative w-16 h-16 shrink-0 flex items-center justify-center">
                  <svg className="w-full h-full -rotate-90" viewBox="0 0 60 60">
                    <circle cx="30" cy="30" r="24" stroke="rgba(255,255,255,0.1)" strokeWidth="4" fill="none" />
                    <circle
                      cx="30"
                      cy="30"
                      r="24"
                      stroke="#58D7FF"
                      strokeWidth="4"
                      fill="none"
                      strokeDasharray="150"
                      strokeDashoffset={150 - (powerKw / 800) * 120}
                      strokeLinecap="round"
                    />
                  </svg>
                  <span className="absolute font-mono font-bold text-xs text-white">{powerKw}</span>
                </div>
                <div>
                  <div className="text-[10px] text-[#8B98AA] font-mono">OUTPUT</div>
                  <div className="text-xs font-bold text-[#F5F8FF]">kW POWER</div>
                  <div className="text-[10px] text-[#58D7FF] font-mono">MAX 760 kW</div>
                </div>
              </div>

              {/* Gauge 4: Battery Pack Volt/Thermal */}
              <div className="p-3.5 rounded-2xl bg-[#03060C] border border-[rgba(70,170,255,0.2)] flex items-center gap-3">
                <div className="relative w-16 h-16 shrink-0 flex items-center justify-center">
                  <svg className="w-full h-full -rotate-90" viewBox="0 0 60 60">
                    <circle cx="30" cy="30" r="24" stroke="rgba(255,255,255,0.1)" strokeWidth="4" fill="none" />
                    <circle
                      cx="30"
                      cy="30"
                      r="24"
                      stroke="#18CC8A"
                      strokeWidth="4"
                      fill="none"
                      strokeDasharray="150"
                      strokeDashoffset={150 - (92 / 100) * 120}
                      strokeLinecap="round"
                    />
                  </svg>
                  <span className="absolute font-mono font-bold text-xs text-white">840V</span>
                </div>
                <div>
                  <div className="text-[10px] text-[#8B98AA] font-mono">HIGH VOLT</div>
                  <div className="text-xs font-bold text-[#F5F8FF]">PACK 92%</div>
                  <div className="text-[10px] text-[#18CC8A] font-mono">33.4°C NOMINAL</div>
                </div>
              </div>
            </div>
          </div>
        ) : (
          /* ========================================================
             STANDARD COCKPIT VIEW: Dual Large Dials
             ======================================================== */
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
            {/* Left Large Dial: Speedometer */}
            <div className="p-6 rounded-3xl bg-[#060D18]/85 border border-[rgba(70,170,255,0.22)] flex flex-col items-center justify-center relative shadow-[0_0_25px_rgba(57,184,255,0.06)]">
              <div className="text-xs font-mono-jarvis text-[#8B98AA] uppercase tracking-wider mb-2">
                Velocity & Transmission
              </div>

              {/* Dial Canvas */}
              <div className="relative w-48 h-48 flex items-center justify-center">
                <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100">
                  <circle cx="50" cy="50" r="40" stroke="rgba(255,255,255,0.06)" strokeWidth="6" fill="none" />
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    stroke="#58D7FF"
                    strokeWidth="6"
                    fill="none"
                    strokeDasharray="251"
                    strokeDashoffset={251 - (speed / 200) * 190}
                    strokeLinecap="round"
                    className="filter drop-shadow-[0_0_8px_rgba(88,215,255,0.6)] transition-all duration-300"
                  />
                </svg>

                <div className="absolute inset-0 flex flex-col items-center justify-center">
                  <span className="text-4xl font-bold font-mono tracking-tight text-white">{speed}</span>
                  <span className="text-xs font-mono text-[#8B98AA]">MPH</span>
                  <span className="text-xs font-mono font-bold text-[#FF294D] mt-1">{gear}</span>
                </div>
              </div>

              <div className="text-[11px] text-[#8B98AA] font-mono mt-3">
                0-100 MPH: 3.42s • SPEED LIMITER: OFF
              </div>
            </div>

            {/* Center Diagnostics: Tire Pressures & Chassis G-Force */}
            <div className="p-6 rounded-3xl bg-[#060D18]/85 border border-[rgba(70,170,255,0.22)] flex flex-col justify-between">
              <div className="flex items-center justify-between text-xs font-mono-jarvis text-[#8B98AA]">
                <span>CHASSIS TELEMETRY</span>
                <span className="text-[#18CC8A] flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" /> NOMINAL
                </span>
              </div>

              {/* Wireframe Vehicle / Tire Map */}
              <div className="relative py-4 flex items-center justify-center">
                <div className="w-28 h-36 rounded-2xl border border-white/10 bg-[#03060C] relative flex items-center justify-center">
                  <div className="text-[10px] font-mono text-[#8B98AA] text-center">
                    AERO WING<br />+4° DOWNFORCE
                  </div>

                  {/* Tires */}
                  <div className="absolute -left-3 top-2 px-1.5 py-0.5 rounded bg-[#060D18] border border-[rgba(70,170,255,0.4)] text-[9px] font-mono text-[#58D7FF]">
                    FL 35 PSI
                  </div>
                  <div className="absolute -right-3 top-2 px-1.5 py-0.5 rounded bg-[#060D18] border border-[rgba(70,170,255,0.4)] text-[9px] font-mono text-[#58D7FF]">
                    FR 35 PSI
                  </div>
                  <div className="absolute -left-3 bottom-2 px-1.5 py-0.5 rounded bg-[#060D18] border border-[rgba(70,170,255,0.4)] text-[9px] font-mono text-[#58D7FF]">
                    RL 36 PSI
                  </div>
                  <div className="absolute -right-3 bottom-2 px-1.5 py-0.5 rounded bg-[#060D18] border border-[rgba(70,170,255,0.4)] text-[9px] font-mono text-[#58D7FF]">
                    RR 36 PSI
                  </div>
                </div>
              </div>

              <div className="text-center text-xs font-mono text-[#58D7FF]">
                MICHELIN PILOT SPORT CUP 2R (OPTIMAL TEMP 72°C)
              </div>
            </div>

            {/* Right Large Dial: Powertrain RPM & Output kW */}
            <div className="p-6 rounded-3xl bg-[#060D18]/85 border border-[rgba(70,170,255,0.22)] flex flex-col items-center justify-center relative shadow-[0_0_25px_rgba(255,41,77,0.06)]">
              <div className="text-xs font-mono-jarvis text-[#8B98AA] uppercase tracking-wider mb-2">
                Electric Powertrain Load
              </div>

              {/* Dial Canvas */}
              <div className="relative w-48 h-48 flex items-center justify-center">
                <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100">
                  <circle cx="50" cy="50" r="40" stroke="rgba(255,255,255,0.06)" strokeWidth="6" fill="none" />
                  <circle
                    cx="50"
                    cy="50"
                    r="40"
                    stroke="#FF294D"
                    strokeWidth="6"
                    fill="none"
                    strokeDasharray="251"
                    strokeDashoffset={251 - (rpm / 9000) * 190}
                    strokeLinecap="round"
                    className="filter drop-shadow-[0_0_8px_rgba(255,41,77,0.6)] transition-all duration-300"
                  />
                </svg>

                <div className="absolute inset-0 flex flex-col items-center justify-center">
                  <span className="text-4xl font-bold font-mono tracking-tight text-white">{powerKw}</span>
                  <span className="text-xs font-mono text-[#8B98AA]">kW POWER</span>
                  <span className="text-xs font-mono font-bold text-[#58D7FF] mt-1">{rpm} RPM</span>
                </div>
              </div>

              <div className="text-[11px] text-[#8B98AA] font-mono mt-3">
                PEAK TORQUE: 1,050 Nm • INVERTER 840V SiC
              </div>
            </div>
          </div>
        )}

        {/* Live CAN-Bus Stream Section */}
        <div className="p-4 rounded-2xl bg-[#060D18]/70 border border-[rgba(70,170,255,0.18)]">
          <div className="flex items-center justify-between text-xs font-mono-jarvis text-[#8B98AA] mb-2">
            <span className="flex items-center gap-2">
              <span className="w-1.5 h-1.5 rounded-full bg-[#18CC8A] animate-ping" />
              LIVE VEHICLE CAN-BUS TELEMETRY (CAN-FD 5Mbps)
            </span>
            <span>Frames: 1,429/sec</span>
          </div>

          <div className="space-y-1 font-mono text-[11px] text-[#58D7FF] bg-[#03060C] p-3 rounded-xl border border-white/5">
            {canBusLogs.map((log, idx) => (
              <div key={idx} className="flex items-center gap-2">
                <span className="text-[#8B98AA]">{`>`}</span>
                <span>{log}</span>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
};
