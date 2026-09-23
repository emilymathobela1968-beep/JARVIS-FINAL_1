// Speech Recognition and Speech Synthesis for JARVIS V2

type SpeechCallback = (text: string) => void;
type StateCallback = (isListening: boolean) => void;

interface IWindowWithSpeech extends Window {
  SpeechRecognition?: any;
  webkitSpeechRecognition?: any;
}

class JarvisVoiceEngine {
  private recognition: any = null;
  private isListening = false;
  private onResultCb: SpeechCallback | null = null;
  private onStateCb: StateCallback | null = null;

  constructor() {
    if (typeof window !== 'undefined') {
      const win = window as IWindowWithSpeech;
      const SpeechRecognition = win.SpeechRecognition || win.webkitSpeechRecognition;
      if (SpeechRecognition) {
        try {
          this.recognition = new SpeechRecognition();
          this.recognition.continuous = false;
          this.recognition.interimResults = false;
          this.recognition.lang = 'en-US';

          this.recognition.onstart = () => {
            this.isListening = true;
            this.onStateCb?.(true);
          };

          this.recognition.onend = () => {
            this.isListening = false;
            this.onStateCb?.(false);
          };

          this.recognition.onerror = () => {
            this.isListening = false;
            this.onStateCb?.(false);
          };

          this.recognition.onresult = (event: any) => {
            const transcript = event.results?.[0]?.[0]?.transcript;
            if (transcript && this.onResultCb) {
              this.onResultCb(transcript);
            }
          };
        } catch {
          this.recognition = null;
        }
      }
    }
  }

  public hasSpeechRecognition(): boolean {
    return !!this.recognition;
  }

  public startListening(onResult: SpeechCallback, onStateChange: StateCallback) {
    this.onResultCb = onResult;
    this.onStateCb = onStateChange;

    if (this.recognition) {
      try {
        this.recognition.start();
      } catch {
        // May already be running
      }
    } else {
      // Fallback: simulate voice input after prompt
      onStateChange(true);
      setTimeout(() => {
        onStateChange(false);
      }, 3000);
    }
  }

  public stopListening() {
    if (this.recognition && this.isListening) {
      try {
        this.recognition.stop();
      } catch {
        // ignore
      }
    }
    this.isListening = false;
    this.onStateCb?.(false);
  }

  public speak(text: string, onEnd?: () => void) {
    if (typeof window === 'undefined' || !window.speechSynthesis) {
      onEnd?.();
      return;
    }

    // Cancel current speech
    window.speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1.05;
    utterance.pitch = 0.95; // slightly lower, calm authoritative pitch

    // Try finding an English British or polished voice
    const voices = window.speechSynthesis.getVoices();
    const preferredVoice = voices.find(v => 
      v.lang.startsWith('en') && (v.name.includes('Daniel') || v.name.includes('George') || v.name.includes('David') || v.name.includes('Natural') || v.name.includes('English'))
    ) || voices.find(v => v.lang.startsWith('en')) || voices[0];

    if (preferredVoice) {
      utterance.voice = preferredVoice;
    }

    if (onEnd) {
      utterance.onend = onEnd;
      utterance.onerror = onEnd;
    }

    window.speechSynthesis.speak(utterance);
  }

  public stopSpeaking() {
    if (typeof window !== 'undefined' && window.speechSynthesis) {
      window.speechSynthesis.cancel();
    }
  }
}

export const jarvisVoice = new JarvisVoiceEngine();
