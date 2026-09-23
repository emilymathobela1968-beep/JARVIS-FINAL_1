# Jarvis V2 engineering review

## Evidence-led legacy findings

`DEFERRED_VOICE_FAULT.md` records that Kokoro synthesis completed and the speaker stream opened, but playback often stopped after a fragment. Text/reasoning remained operational. That makes received TTS bytes and even playback-start events insufficient evidence of a completed utterance.

The legacy UI owns realtime state while the Python bridge independently emits device, reasoning and playback events. The bridge also represents several responsibilities in one process: microphone, STT, TTS, playback, realtime provider and interruption. This permits asynchronous callbacks to change a shared turn without one serial owner. Speaker bleed/self-barge-in is suspected but unproven; V2 must instrument it rather than assume it is the sole cause.

## V2 decisions

- `Jarvis.Voice.VoiceSessionController` is the one authority for state, current turn and turn cancellation.
- Every turn creates a fresh cancellation source and a GUID. Stale callbacks cannot change state or complete a superseded turn.
- Adapters will report facts (speech, transcript, synthesized audio, playback completion); they cannot own state or cancel unrelated work.
- TTS generation and Windows playback remain separate. A turn completes only after the playback adapter reports completion for the same turn.
- Fish Audio is represented only by configuration options until its documented endpoint/authentication/model details and operator-provided secret/voice selection are available. Nothing is guessed or logged.
- Barehands remains copied as a reference asset but is not a dependency of voice stabilization.

## Gate status

| Gate | Status | Evidence |
|---|---|---|
| 0 desktop shell | Build verified; runtime launch pending | `dotnet build` succeeds |
| 1 TTS/playback | Passed 10/10 | Leon physically confirmed complete Fish Audio playback on ten runs |
| 2 microphone/STT | Passed 10/10 | Leon physically confirmed ten microphone-to-local-faster-whisper transcripts |
| 3 conversation | Passed 10/10 | Leon physically confirmed ten complete serial conversational turns; STT wording errors are tuning work |
| 4 barge-in | In implementation | Must be physically tested at beginning, middle, and end of playback |
| 5 capabilities | Existing non-voice capability reference copied | Must not be coupled to foundational voice work |

## External inputs still required

1. Fish Audio documented integration details and account credentials, stored locally outside source control.
2. Fish model and voice/reference identifier chosen by Leon.
3. Physical microphone and speaker selection plus audible confirmation during Gate 1 and ten-utterance Gate 2.
