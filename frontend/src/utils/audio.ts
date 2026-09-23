// Cinematic Web Audio synthesizer for JARVIS
class SoundFX {
  private ctx: AudioContext | null = null;

  private getContext(): AudioContext | null {
    if (typeof window === 'undefined') return null;
    if (!this.ctx) {
      const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      if (AudioCtx) {
        this.ctx = new AudioCtx();
      }
    }
    if (this.ctx && this.ctx.state === 'suspended') {
      this.ctx.resume().catch(() => {});
    }
    return this.ctx;
  }

  // Cinematic Arc Reactor Power Surge (Ignition to Blue)
  playIgnite() {
    try {
      const ctx = this.getContext();
      if (!ctx) return;
      const now = ctx.currentTime;

      // Sub-bass resonant surge
      const subOsc = ctx.createOscillator();
      const subGain = ctx.createGain();
      subOsc.type = 'sine';
      subOsc.frequency.setValueAtTime(65, now);
      subOsc.frequency.exponentialRampToValueAtTime(180, now + 0.35);
      subGain.gain.setValueAtTime(0.001, now);
      subGain.gain.linearRampToValueAtTime(0.08, now + 0.12);
      subGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.45);
      subOsc.connect(subGain);
      subGain.connect(ctx.destination);
      subOsc.start(now);
      subOsc.stop(now + 0.45);

      // High-frequency energy chirp
      const chirpOsc = ctx.createOscillator();
      const chirpGain = ctx.createGain();
      chirpOsc.type = 'triangle';
      chirpOsc.frequency.setValueAtTime(800, now + 0.05);
      chirpOsc.frequency.exponentialRampToValueAtTime(2400, now + 0.3);
      chirpGain.gain.setValueAtTime(0.001, now + 0.05);
      chirpGain.gain.linearRampToValueAtTime(0.03, now + 0.15);
      chirpGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.35);
      chirpOsc.connect(chirpGain);
      chirpGain.connect(ctx.destination);
      chirpOsc.start(now + 0.05);
      chirpOsc.stop(now + 0.35);
    } catch {
      // Audio might be waiting for user interaction
    }
  }

  // Cinematic Standby Return (Transition back to Red/Maroon)
  playPowerDown() {
    try {
      const ctx = this.getContext();
      if (!ctx) return;
      const now = ctx.currentTime;

      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(280, now);
      osc.frequency.exponentialRampToValueAtTime(70, now + 0.35);
      gain.gain.setValueAtTime(0.05, now);
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.35);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(now);
      osc.stop(now + 0.35);
    } catch {
      // ignore
    }
  }

  // Clean cyber tone
  playBeep(freq = 880, duration = 0.08, type: OscillatorType = 'sine') {
    try {
      const ctx = this.getContext();
      if (!ctx) return;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(freq, ctx.currentTime);
      gain.gain.setValueAtTime(0.035, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + duration);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + duration);
    } catch {
      // ignore
    }
  }

  // Double chime acknowledgment
  playAcknowledge() {
    this.playBeep(720, 0.06, 'sine');
    setTimeout(() => this.playBeep(1080, 0.09, 'sine'), 65);
  }

  // Execution confirmation
  playExecute() {
    this.playBeep(440, 0.05, 'triangle');
    setTimeout(() => this.playBeep(880, 0.07, 'triangle'), 50);
    setTimeout(() => this.playBeep(1400, 0.12, 'sine'), 110);
  }

  // Idle pulse heartbeat
  playPulse() {
    this.playBeep(160, 0.18, 'sine');
  }
}

export const soundFX = new SoundFX();
