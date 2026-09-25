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

### Phase 3 — App Builder screens (DONE 2026-06)
- Stage machine in `App.tsx`: `hero` → (MENU ▸ Builder) → `intake` → (Build) → `builder`. Hero direct submit
  still goes straight to `builder`. Directive and selected app type are preserved verbatim.
- **Screen A — Builder Intake** (`BuilderIntakeScreen.tsx`): supplied artwork
  `src/assets/images/jarvis_builder_intake.png` (1672×941) as the visual layer, with transparent overlays for
  the description input (masked baked placeholder), microphone (truthful "voice not connected"), blue Build
  button, and Web/Mobile/AI selectors (selection shown by a bright blue rule over the artwork's own underline).
  Measured rects: input 360,546 686×48 · mic 1044,602 · build 1128,601 182×61 · selectors 526/775/1023, 730.
- **Screen B — Builder Workspace** (`Stage2Workspace.tsx`): rebuilt as live UI. Top nav (Home, Computer,
  Developer, Media, Builder active, Barehands, System + search). Left = one open scrollable console surface
  (no boxed cards; user turns marked by a thin blue rule, agent output by a small J glyph, execution records
  with icon + Cascadia evidence). Large dark-glass composer (attach, text, mic, send). Right = Preview /
  Inspect / Full Screen + Edit, blue active states, empty truthful preview. Removed: nav rail, browser dots,
  avatar chip, Launch, Share, "Running" chrome, duplicate branding.
- Hero foreground scale reduced 3.5% → `FOREGROUND_SCALE = 0.907` in the shared `src/utils/artwork.ts`
  (hook + rect mapping now shared by hero and intake); overlay alignment re-verified unchanged.
- Validated by the testing agent: 44/44 frontend assertions passed, zero issues (report
  `/app/test_reports/iteration_1.json`).

### Phase 3.1 — Visual refinement (DONE 2026-06)
- Hero untouched. Builder Intake artwork untouched except brand consistency: baked plain wordmark masked by an
  aligned blurred slice of its own artwork, with the accepted chrome JARVIS logo (alpha-keyed crop of the hero
  artwork → `src/assets/images/jarvis_logo_chrome.png`) overlaid at artwork coords 40,21 250×78.
- Workspace: removed the large perimeter border/frame around the right preview canvas (it now dissolves into the
  workspace), removed the left column border/glow (surface lift only), page surface lifted #030508 → #050A14 /
  left pane #070E1C, and all cyan/turquoise treatments replaced with a sapphire system
  (#2F7CFF → #1B55CC gradients, rgba(47,124,255,.24) inactive edges, #5C9DFF/#7FB4FF accents).
  Compact rectangular controls, no pills; left console remains open and unboxed.

### Phase 3.2 — Final scale / spacing / control size (DONE 2026-06)
- Shared `FOREGROUND_SCALE` 0.907 → **0.88** (hero ~3% smaller; overlay coords unchanged and re-verified).
- Builder Intake artwork: panel assembly (glass panel + mic + Build + selector row) composited 33px lower as one
  unit with seam cross-fades; overlay rects shifted +33px in y accordingly.
- Workspace top controls (Preview / Inspect / Full Screen / Edit): height 47px, px-5, gap-2.5, 18px icons,
  15px text, same sapphire treatment and radius.

### Phase 4 / Milestone 1 — Real Web App generation (DONE 2026-07)
- Backend wired for the first time. `backend/.env` restored with MONGO_URL / DB_NAME / CORS_ORIGINS and
  EMERGENT_LLM_KEY. `frontend/.env` created with REACT_APP_BACKEND_URL; `vite.config.ts` `envPrefix`
  extended to expose `REACT_APP_`.
- New endpoints (`backend/server.py`): `POST /api/builder/generate` (SSE, streams start/delta/done/error;
  real single self-contained HTML doc via OpenAI **gpt-5.4** through emergentintegrations; persisted to
  `db.generations`), `POST /api/builder/verify/{id}`. Unsupported app_type -> 422, empty objective -> 400.
- Frontend (`Stage2Workspace.tsx`, `GeneratedArtifact.tsx`, `utils/api.ts`): streams generation into the
  timeline with honest char-count progress; renders the result in a sandboxed iframe (`allow-scripts`).
  Verification is REAL — a tiny injected probe postMessages `render` (on load) and `interaction` (first
  click/key/input) from inside the sandbox; status flips generating -> unverified -> verified only when
  BOTH are observed. Failed runs show failure reason + evidence. Stop button aborts a run.
- Edit objective (regenerates) + composer refinements (regenerate with change applied). Full Screen/Exit,
  Preview/Inspect retained. Deferred items visibly BLOCKED/unavailable: Mobile App, AI Model, Media,
  Computer/Developer/System/Barehands/Search, attachments, voice, saved history, deployment.
- Verified via curl + automated browser: to-do & tip-calculator apps generated, rendered, and flipped to
  verified on real interaction; mobile-type shows honest blocked state. Backend testing agent: all 3
  endpoints pass.


### Phase 5 — Clean-background live overlays (DONE 2026-06)
- Backgrounds locked and never edited: Home = `bg_a.png`, Builder = `bg_b.png`, Workstation = `bg_c.png`.
  Temporary review label + bottom switcher removed. All UI is live React/CSS above the artwork.
- `components/HomeScreen.tsx` (bg_a): live JARVIS logo top-left (10px/10px), live `JarvisMenu` MENU button
  top-right (glass + blue glow), command panel centered in the lower third (`bottom-7vh`, max 720px) with
  JARVIS badge, wordmark, status dot, prompt line, input, attach, mic, send. Nothing touches the radar circle.
- `components/JarvisMenu.tsx`: 7 destinations; Home + Builder live, others honest "Soon / not connected yet".
- `components/BuilderScreen.tsx` (bg_b): same logo position/size, build panel low (`bottom-5vh`, max 760px)
  with placeholder "Describe the application you want to build...", live mic (honest notice) and Build button;
  live app-type selector below — Web App enabled, Mobile App / AI Model disabled with "Soon".
- `Stage2Workspace.tsx` (bg_c): background artwork layer added, glass top nav (Home, Computer, Developer,
  Media, Builder active, Barehands, System + search), left pane 35% translucent (objective, edit objective,
  timeline, refinement composer), right 65% preview with **Preview / Edit / Inspect / Full Screen**.
  Edit = editable source + "Apply & re-render" (flips artifact back to unverified). Full Screen collapses the
  left pane and shows Exit Full Screen.
- Flow: Home collects the objective → Builder (confirm description + app type) → Build → Workstation.
- Verified by browser automation: tip-calculator generated via SSE, rendered in the sandbox, flipped to
  `verified` on real interaction; full screen, edit mode and mobile (390px) checked, zero horizontal overflow.

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
