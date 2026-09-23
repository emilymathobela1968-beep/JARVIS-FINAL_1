# Deferred voice fault — 2026-09-13

**Status: DEGRADED / DEFERRED**

Local TTS playback begins but often stops after the first word/short fragment.
Realtime reasoning and full text output remain operational.
Further audio diagnosis deferred.

## Preserved evidence

- Kokoro synthesis completes.
- The local speaker stream opens and `tts_audio_start` fires.
- Speaker bleed / self-barge-in remains suspected, but is not fully proven.
- The stale-cancel repair exists.
- Fish is not authoritative in the running voice path.

## Containment contract

Voice is an optional subsystem. A local TTS failure must be logged as a Voice
unavailable/degraded condition and must not set Jarvis core to OFFLINE, restart
or reconnect Realtime reasoning, interrupt text responses, or disable tools.

No audio diagnosis, provider swap, automatic recovery loop, or Fish integration
is authorized by this record. Preserve the Kokoro, Fish, profile, barge-in,
Realtime-input, adapter, fallback, and log artifacts for a separately scoped
diagnosis.
