# Jarvis Windows V2 Engineering Checkpoint

## Project

Jarvis Windows V2

## Workspace

`C:\JARVIS_CODEX\Jarvis.Windows.V2`

## Locked Architecture

Realtime conversational controller -> governance/tool authorization -> governed capability registry -> evidence/verification -> conversational response.

Do not bypass governance, authorization, bounded execution, evidence collection, or native ephemeral-client-secret brokering.

## Realtime Physical Status

PARTIAL:

- WebRTC transport observed
- Microphone streaming observed
- Peer connection observed
- Remote realtime audio observed
- Persistent conversation observed
- Live Computer tool execution bridge is NOT physically accepted

## Physically Accepted Computer Baseline

- Realtime voice and conversation
- Governed Microsoft Word launch and graceful close
- Governed Notepad launch and graceful close
- Application close by human-readable name
- Calculator physically launches and can be closed, but truthful launch verification remains pending physical retest after the bounded polling correction

## Current Remaining Realtime Issue

- Physical Word launch failed on a stale deployed build: Jarvis narrated "checking status" / "launch still in progress" / "Word still launching" without visible Computer tool invocation, operation ID, launch result, or verification evidence
- Read-only GA audit identified a deployed/source mismatch: the physically tested deployment contained `/openai/v1/realtime/calls?webrtcfilter=on`, while current source uses `/openai/v1/realtime/calls`
- The Debug x64 deployment has been reconciled to the current source and now requires physical acceptance on the new build
- Half-second transcript lag is real but lower priority; provider appears to emit final/late transcription rather than genuine word-by-word input deltas
- Final physical barge-in acceptance remains to be closed
- Calculator launch verification previously returned a false negative because `calc.exe` was observed before the packaged `CalculatorApp` window identity appeared
- Bounded post-launch observation polling is implemented; Calculator physical verification retest is pending

## Reconciled Realtime Deployment Evidence

- Source/deployed mismatch identified: stale deployed `Jarvis.Realtime.dll` contained filtered WebRTC calls endpoint
- Stale filtered deployment removed/reconciled by normal Debug x64 build into `C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.App\bin\x64\Debug\net8.0-windows10.0.19041.0`
- Effective calls URL shape: `https://<redacted-resource>/openai/v1/realtime/calls`
- `webrtcfilter=on` absent from deployed `Jarvis.Realtime.dll`
- `Jarvis.Realtime.dll` ModuleVersionId: `6b550cf5-fb8c-4da1-a476-c06f25a1031d`
- `Jarvis.Realtime.dll` LastWriteTimeUtc: `2026-09-19T08:00:10.3863119Z`
- Reconciled Jarvis launched as `Jarvis.App` PID `12848` from the Debug x64 output
- Runtime deployment identity evidence logged with matching `Jarvis.Realtime.dll` ModuleVersionId
- `session.updated` validation and GA function-call event observation are instrumented and pending user-started Realtime / physical request
- Physical acceptance: PENDING

## Turn Timing

1800 ms. Keep unless later physical evidence justifies a change.

## Current Configuration Contract

- `AZURE_OPENAI_REALTIME_ENDPOINT`
- `AZURE_OPENAI_REALTIME_DEPLOYMENT`
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT`

## Secret Storage

- Storage mechanism: Windows Credential Manager generic credential
- Non-secret credential identifier: `Jarvis.Windows.V2/AzureOpenAIRealtime`
- Retrieval component: `JarvisRealtimeConfigurationProvider` / `WindowsCredentialSecretStore`
- Local non-secret configuration: `%LOCALAPPDATA%\Jarvis.Windows.V2\realtime.local.json`
- Recovery procedure: load local non-secret config, retrieve API key from Windows Credential Manager, report only `PRESENT` / `MISSING`

## Durable Realtime Configuration Continuity

- Machine-local non-secret Azure Realtime configuration belongs only at `%LOCALAPPDATA%\Jarvis.Windows.V2\realtime.local.json`
- The Azure Realtime secret remains only in Windows Credential Manager under `Jarvis.Windows.V2/AzureOpenAIRealtime`
- Ordinary builds and deployment reconciliation must never delete, replace, or treat machine-local runtime configuration as build output
- Clean-process verification with Azure and Fish environment variables absent resolves endpoint, deployment, credential, and transcription deployment as present
- Configuration provider result: `Configured=True`, source `local_config+windows_credential`
- Current GA WebRTC calls endpoint remains unfiltered; `webrtcfilter=on` is absent from the deployed runtime
- Physical acceptance requires Realtime voice, barge-in, governed tool calling, Computer execution, and application verification in the same session

Never record API keys, tokens, client secrets, authorization headers, or ephemeral credentials in source, logs, prompts, checkpoints, tests, or documentation.

## Known Working Capabilities

- Realtime
- Computer
- Developer
- Media/ALEXIS
- Governed tool bridge

## Current Next Implementation

Repeat physical acceptance on the reconciled Debug x64 deployment. If it still fails, preserve the first real GA `session.updated` / function-call / native-router evidence and stop at that boundary.

## Next Physical Acceptance

1. A live spoken request to open Word produces a Computer tool invocation in the activity/audit log.
2. The invocation has an operation ID, launch result, and verification evidence.
3. Word visibly opens or the failure is reported from Computer evidence, not assistant narration.
4. Barge-in physically interrupts Jarvis.

## If Passed

Only close the Realtime gate after the live Computer bridge path is physically verified.

## Next Locked Product Phase

Premium Jarvis operating UI.

## Visual Authority

- Near/deep black base
- Premium restrained metallic gold
- Controlled red operational/critical accents
- Executive engineered appearance
- No generic grey engineering console
- No blue dashboard
- No purple neon
- No gaming aesthetic
- No excessive glow

## Current Deterministic Results

- Build Debug x64: PASS (`Build succeeded. 0 Warning(s). 0 Error(s).`)
- Realtime tests: PASS (`REALTIME_TESTS_TOTAL passed=58 failed=0`)
- Computer tests: PASS (`COMPUTER_TESTS_TOTAL passed=22 failed=0`)
- Developer tests: PASS (`DEVELOPER_TESTS_TOTAL passed=11 failed=0`)
- Media tests: PASS (`MEDIA_TESTS_TOTAL passed=10 failed=0`)
- Voice deterministic tests: PASS (`DETERMINISTIC_TOTAL passed=75 failed=0`)
- Gate 4 deterministic tests: PASS (`GATE4_DETERMINISTIC_TESTS_PASSED`)
- Gate 5 tests: PASS (`GATE5_APPLICATION_TESTS passed=173 failed=0`)
- Capabilities tests: PASS (`Capability tests passed: 12 audited explicit requests; packaged Barehands loopback lifecycle and port release verified twice.`)
- Mission Intelligence tests: PASS (`MISSION_INTELLIGENCE_TESTS passed=14 failed=0`)

## Current Clean-Process Realtime Evidence

- Running Jarvis PID: `20516`
- Runtime `Jarvis.Realtime.dll` ModuleVersionId: `6b550cf5-fb8c-4da1-a476-c06f25a1031d`
- WebView ready, microphone stream started, peer connection established, remote audio track/playback available, and data channel opened
- Effective session validation: `computer_launch_application`, `computer_get_status`, and `computer_operation` present; `tool_choice=auto`; validation passed
- No physical application request was performed by Codex; real GA function-call event and Computer execution evidence remain pending Leon's physical acceptance
