# JARVIS — Product Requirements & State

## Original problem statement
A JARVIS web application (exported from Google AI Studio, React 18 + Vite 6 + TS + Tailwind v4) is being
moved from a simulated demo export into a production-oriented product. The user drives work in strictly
scoped phases and requires: no unrequested redesigns, no fabricated data, no fake execution theatre.

Architecture that must be preserved:
- **Stage 1** — JARVIS hero / intake screen (approved visual direction, background asset locked).
- **Stage 2** — App Builder workspace: left conversation/execution activity, right preview surface,
  bottom command composer, left navigation rail, top preview controls.

## Current state (2026-06, Phase 1 complete)
- Active app lives at `/app/frontend` (Vite, `yarn start` → `vite --host 0.0.0.0 --port 3000`).
- `.NET` desktop solution carried over untouched at `/app/Jarvis.Windows.V2` + source zip (not part of the web build).
- No backend wired. `/app/backend` is the platform default FastAPI service; the web app does not call it yet.

### Phase 1 — Clean baseline (DONE 2026-06)
Removed from the active app:
- fake timed build progress, hardcoded 68% start, timer-to-100% loop
- fake timestamps, fake execution evidence, pre-seeded conversation, REV-001 "compiling" message
- canned/phrase-matched revision responses
- hardcoded automotive telemetry, cloud cluster, fintech account/transaction, AI inference fake data
- hardcoded fake INSPECT source, fake visual parameter editor
- `alert()`-based Launch / Share / Copy / Attach behaviour
- automatic fake preview initialization + hero's forced default automotive directive

Added (truthful replacements):
- `AgentState` = WAITING | WORKING | ACTION_REQUIRED | BLOCKED | ARTIFACT_READY (initial: WAITING)
- `Artifact` lifecycle = generating | unverified | verified | failed; preview is driven ONLY by this
- Empty states: `PREVIEW / Waiting for first verified render / Planning • No artifact created`,
  "No execution activity yet", "No source to inspect", "Nothing to edit"
- Unimplemented controls are disabled or report "Not configured" (no fake actions)
- Typography: Plus Jakarta Sans for human UI, Cascadia Code (`font-mono-jarvis`, Tailwind `font-mono`)
  for IDs/timestamps/evidence/source only

Retained intentionally (user decision: stability over deletion):
`src/components/views/*`, `NavigationMenu.tsx`, `Stage1Intake.tsx`, `JarvisBackground.tsx`,
`JarvisRadar.tsx`, `ArcReactorVisualizer.tsx`, `HotkeysModal.tsx`, `QuickActionsModal.tsx`,
`UniversalCommandBar.tsx`, `AutomotiveArtifact.tsx` — all orphaned, none imported by the active app.
`AppCategory` was relocated into `src/types.ts`; `GeneratedArtifact` no longer imports from `Stage1Intake`.

Validation: `tsc --noEmit` clean, `vite build` clean, Stage 1 visually unchanged, Stage 2 verified empty.

## Backlog
### P0 (next)
- New JARVIS interface implementation + real execution architecture (awaiting user's visual reference).
- Real agent runtime: directive dispatch, streamed execution events, artifact generation + verification gate.
### P1
- Wire composer capabilities for real: attachments, plan, context retrieval.
- Persist sessions/conversation (backend + Mongo) instead of in-memory state.
- Real Launch / Share once artifacts exist.
### P2
- Decide fate of retained legacy components once the new interface lands.
- Voice output (TTS) to pair with the existing Web Speech input.
