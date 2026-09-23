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

### Phase 2 — Hero screen source-of-truth implementation (DONE 2026-06)
- Artwork `src/assets/images/jarvis_home_hero.png` (1672×941) is the visual foundation — never recolored,
  never regenerated, never approximated in CSS. Old `jarvis_hero_bg.jpg` no longer used by the active app.
- `JarvisHeroScreen.tsx` rewritten as an artwork + transparent-overlay screen. No duplicated visual chrome:
  logo, core/radar, glass panel, avatar, Ready dot, greeting and icons all come from the artwork itself.
- Overlay coordinate system: control rects are stored in artwork pixel space (`RECT` in the component) and
  positioned as percentages of a JS-measured cover box (`scale = max(vw/1672, vh/941)`), so overlays stay
  pixel-aligned across resize/zoom/any 16:9-ish resolution. Verified: overlay centers map back to the
  artwork's measured control centers within ~1px (mic 1225,811 · send 1327,812 · attach 343,813 ·
  keyboard 1120,813 · menu 1554,62).
- Command input: real `<input>`; the artwork's baked placeholder is hidden by a blurred, exactly-aligned
  slice of the artwork itself (invisible seam), with a live placeholder matched to the artwork styling.
- MENU: dark-glass panel with the 7 existing destinations; closes on outside click and Escape. Non-Home
  destinations report "<dest>: not connected / Runtime pending integration".
- Microphone: no voice runtime — reports "Voice not connected / Realtime voice runtime pending integration".
  No fake listening, no fake transcript. (Web Speech usage was removed from the hero.)
- Attachment: real local file picker; shows the filename, uploads nothing.
- Keyboard icon: focuses the command field. Send/Enter: empty is a no-op; a real directive goes to Stage 2.
- Stage 1 → Stage 2: directive preserved verbatim; Builder opens with agent state
  **BLOCKED — "No execution runtime is connected"** plus a truthful "Directive received / dispatch=skipped"
  event. Preview stays empty.

### Phase 2b — Hero viewport scale refinement (DONE 2026-06)
- Foreground artwork now presented at `FOREGROUND_SCALE = 0.94` of a full cover fit (≈6% pulled back).
- Two-layer presentation: unobtrusive blurred/darkened cover copy of the same artwork fills the perimeter
  (`scale(1.05)`, `blur(26px) brightness(0.62) saturate(0.9)`), foreground stays sharp and undistorted with a
  ~2.2%/2.6% feathered edge mask so there is no seam, bar, frame or visible second image.
- All overlay hit targets still derive from the measured foreground rectangle; artwork-space coordinates
  were NOT changed and re-measure identical (mic 1225,811 · send 1327.5,812.5 · attach 343,813 ·
  keyboard 1120,813 · menu 1554,62 · input 747,813).

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
