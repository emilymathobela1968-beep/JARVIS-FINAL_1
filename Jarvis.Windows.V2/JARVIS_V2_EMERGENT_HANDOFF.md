# Jarvis V2 Emergent Handoff

This note is for an external repair pass in Emergent/Lucas AI. Codex remains the current owner of the Jarvis V2 implementation context, and the result from Emergent should be brought back to Codex before merging or replacing the working build.

## Project To Upload

Primary Jarvis V2 source folder:

```text
C:\JARVIS_CODEX\Jarvis.Windows.V2
```

This folder is not currently a Git repository. Upload/copy the full folder, or zip the full folder, instead of expecting a clone command to work.

Include these top-level items:

```text
Jarvis.sln
AGENTS.md
ENGINEERING_CHECKPOINT.md
ENGINEERING_REVIEW.md
DEFERRED_VOICE_FAULT.md
src\
Barehands\
artifacts\
```

The executable currently runs from:

```text
C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.App\bin\x64\Debug\net8.0-windows10.0.19041.0\Jarvis.App.exe
```

## Current State

Jarvis V2 is a Windows assistant built in C#/.NET/WinUI with Azure/OpenAI Realtime WebRTC voice transport, governed tools, Developer execution, Computer control, Media tools, AppBuilder, and Barehands assets.

The current app launches and shows the `JARVIS V2` window.

Most recent verified build command:

```powershell
dotnet build "C:\JARVIS_CODEX\Jarvis.Windows.V2\Jarvis.sln" -p:Configuration=Debug -p:Platform=x64 -p:BuildInParallel=false -m:1
```

Most recent build result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Recent Voice Fix

A Realtime/WebView microphone-readiness bug was fixed in:

```text
C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.App\RealtimeWebViewHost.cs
```

Problem:

The WebView sent `mic.stream.started` with microphone settings as a JSON object, but the C# host tried to read every payload as a string. That caused a rejected WebView message and prevented microphone evidence from reaching the readiness gate.

Fix:

The host now accepts string payloads normally and accepts object payloads as raw JSON.

Expected physical evidence after starting Realtime:

```text
mic.stream.started
data.channel.open
remote.audio.track
remote.audio.playing
```

There should be no new `realtime_webview_message_rejected` for the mic payload.

## Verified Tests

These test suites passed after the latest repair:

```text
REALTIME_TESTS_TOTAL passed=70 failed=0
APP_BUILDER_TESTS_TOTAL passed=36 failed=0
DEVELOPER_TESTS_TOTAL passed=11 failed=0
```

Run commands:

```powershell
dotnet run --project "C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.Realtime.Tests\Jarvis.Realtime.Tests.csproj" -p:Platform=x64 -p:BuildInParallel=false -m:1
dotnet run --project "C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.AppBuilder.Tests\Jarvis.AppBuilder.Tests.csproj" -p:Platform=x64 -p:BuildInParallel=false -m:1
dotnet run --project "C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.Developer.Tests\Jarvis.Developer.Tests.csproj" -p:Platform=x64 -p:BuildInParallel=false -m:1
```

## Non-Negotiable Boundaries

Do not replace the working WebRTC transport with a fake voice layer.

Do not bypass governed dispatch for Computer, Developer, Media, or Builder actions.

Do not hard-code local user secrets or print credentials.

Do not treat deterministic tests as physical voice acceptance. Physical acceptance requires actual mic stream, peer connection, data channel, remote audio track, playback, and interruption evidence.

Do not redesign the UI while debugging voice transport unless explicitly requested.

Do not use legacy BackTalk/Kokoro as a substitute for V2 Realtime voice.

## What Emergent Should Focus On

1. Verify the WebRTC physical path after the latest payload parser fix.
2. Confirm `mic.stream.started` and `data.channel.open` appear in logs during a real voice session.
3. Confirm Jarvis can hear a user utterance, respond once, return to listening, and stay silent.
4. Confirm barge-in still works without muting the microphone.
5. Evaluate and repair the Windows-control layer so Jarvis can operate Windows 11 quickly and reliably.
6. Evaluate and repair the app-building layer so Jarvis can plan, edit, build, test, and explain software work like a coding agent.
7. If Realtime still fails, isolate the first failing boundary instead of rewriting adjacent systems.

## Product Target: Windows Control

The expected Jarvis is not only a chatbot and not only a voice demo. The target is a voice-driven Windows operating assistant.

Jarvis should eventually be able to control Windows 11 deeply, quickly, and reliably. Examples:

```text
Open Microsoft Word
Open Microsoft Excel
Open a PDF document
Create or write a note
Find and open a file
Launch approved installed applications
Check system state
Navigate common Windows workflows
Use the desktop without long delays or vague capability excuses
```

The current implementation already has governed Computer tools and application-launch routing, but the user experience has not reached the desired level. Emergent should inspect why Jarvis still reports missing capability, missing workspace, missing work area, or unreachable context when the user expects direct Windows control.

Important: do not bypass governance or confirmation rules to fake Windows control. The repair should make the governed Windows-control path broader, faster, more reliable, and better explained.

## Emergent Scope: Do Not Change Voice

The voice system must remain Azure/OpenAI Realtime exactly as currently implemented in the folder.

Emergent should not replace, remove, downgrade, fake, or reroute the voice layer. Do not switch to another voice provider. Do not fall back to legacy BackTalk or Kokoro. Do not rebuild the voice architecture unless there is a narrow bug fix required to preserve the current Azure Realtime path.

If Emergent asks about voice, the answer is:

```text
Voice stays Azure/OpenAI Realtime.
Keep the current WebRTC/audio transport.
Do not replace voice.
Do not redesign the voice stack.
Work around the existing governed voice system.
```

## Emergent Scope: UI / UX Upgrade

Emergent should make visible, meaningful changes to the Jarvis UI/UX.

The application should feel like a premium Jarvis operating surface, not an engineering dashboard, debug console, or generic Windows form. The target is a polished, cinematic, high-end assistant interface with better graphics, layout, artwork, and workflow presentation.

The current UI should be improved toward:

```text
premium Jarvis command center
clear listening/thinking/working states
central active workspace
high-end visual treatment
clean composer/input area
discreet capability launcher
diagnostics moved out of the main experience
no giant permanent engineering blocks as the primary UI
```

Emergent must keep the app functional while improving the look. Do not make a static mockup only. The result must still build and run.

## Product Target: Application Building

Jarvis is also expected to function as an application-building assistant similar to Codex: it should understand the active project, inspect code, make scoped edits, run tests/builds, and report evidence.

Expected behavior:

```text
Understand the active app/project context
Know what workspace is authorized
Plan implementation steps
Edit code safely
Run focused tests
Run builds
Report exact evidence
Avoid generic advice when it can act
Ask only for real blockers such as authorization, credentials, destructive actions, or missing workspace
```

The current system includes Developer, AppBuilder, Builder context, and governed dispatch layers, but the user has repeatedly hit failures where Jarvis says it lacks a workspace, work area, or capability. Emergent should inspect that recovery path and recommend or implement a clean fix that preserves authorization boundaries.

## Critical Behavior Problem To Fix

Jarvis currently understands high-level requests but often does not start the work.

Example user request:

```text
Jarvis, I am building the ALEXIS intelligent diagnostics platform.
Design two mock screens for System Scan and Live Data / Freeze Frame.
Show me what those screens could look like.
```

Current bad behavior:

```text
Jarvis says it understands.
Jarvis compliments the idea.
Jarvis gives generic planning language.
Jarvis asks: "Where do you want to start?"
Jarvis does not produce the mock screens or begin the build.
```

Expected behavior:

```text
Jarvis should infer the first useful step.
Jarvis should create the requested mock screens or start the governed Builder workflow.
Jarvis should use the active Builder/AppBuilder context.
Jarvis should only ask a question if a real blocker exists.
Jarvis should not make the non-technical user choose implementation steps.
```

The user is not a software engineer and should not be asked to decide where implementation begins when the request already contains a clear outcome. Jarvis should choose the next reasonable step, state the assumption briefly, and proceed.

## What Emergent Must Deliver Back

Emergent should return a changed Jarvis project, not just advice.

Expected deliverables:

```text
1. A visibly upgraded UI/UX.
2. Preserved Azure/OpenAI Realtime voice.
3. Improved Windows-control capability path.
4. Improved app-building / mock-screen workflow.
5. Fewer generic "where do you want to start" loops.
6. Clear build instructions.
7. A short report listing changed files and what was improved.
8. Evidence that the app builds successfully.
```

If Emergent cannot implement something fully, it should leave a precise note naming the blocked file, component, and missing dependency. Do not replace missing implementation with generic chatbot advice.

## Questions Emergent Should Not Ask The User

Emergent should not ask the user broad discovery questions such as:

```text
What voice provider do you want?
What framework should we use?
Where do you want to start?
What platform is this for?
What development tools do you use?
Should I redesign the whole architecture?
```

Use the existing project. Preserve the existing stack. Make the best implementation decision from the codebase and the instructions in this handoff.

Only ask the user if a real human decision is required, such as credentials, destructive changes, payment, or a subjective visual preference that cannot be inferred.

## Useful Runtime Logs

Logs are written under the built app folder:

```text
C:\JARVIS_CODEX\Jarvis.Windows.V2\src\Jarvis.App\bin\x64\Debug\net8.0-windows10.0.19041.0\logs\
```

Important logs:

```text
desktop-lifecycle.jsonl
realtime-conversation.jsonl
desktop-voice.jsonl
developer-tasks.jsonl
computer-operations.jsonl
```

## Current Working Theory

The system is close. The dangerous prior self-listening loop was addressed at the turn gate. The latest observed issue was not that Windows lacked a microphone; Windows reported the Realtek microphone as available. The specific defect was host-side parsing of object payloads from the WebView transport.

If physical Realtime still fails, the next useful distinction is:

```text
Browser/WebView obtains mic stream
        ↓
C# host records mic.stream.started
        ↓
Peer connection establishes
        ↓
Data channel opens
        ↓
Remote audio track arrives and plays
        ↓
User transcript final reaches turn gate
        ↓
Jarvis answers once and returns to listening
```

Stop at the first missing evidence item and fix that boundary only.
