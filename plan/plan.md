# JARVIS — Three-Screen Build Experience Over Clean Backgrounds

A dark, premium JARVIS interface where a person describes an app, and JARVIS actually generates and
shows it live. Three cinematic screens — Home, Builder, Workstation — flow into a real, rendered preview.

## Who it's for
People who want to describe an application in plain language and watch JARVIS build and preview a real,
working result — presented inside the established cinematic JARVIS look, not a generic tool.

## Core features and experience
- **Three cinematic screens** rebuilt as live UI on top of the three new clean background images
  (blue observatory for Home, red/blue landscape for Builder, dark observatory for Workstation). The
  radars baked into the Home and Builder images stay decorative — no second radar is drawn over them.
- **Home**: the JARVIS wordmark, a Menu that opens the Builder, and a live command input near the lower
  center. Whatever is typed here is carried forward as the app description.
- **Builder**: the wordmark, a multiline application-description field (prefilled from Home), a single-select
  set of app types — Web App / Mobile App / AI Model (only one selected at a time) — and a Build action.
  The description and chosen type carry into the Workstation.
- **Workstation**: a left objective/conversation/command area (~35%) and a large application preview (~65%)
  on an opaque surface so the background scenery never shows through the rendered app. Controls: Preview/Run,
  Inspect (shows the real generated source), Full Screen / Exit Full Screen (in-app maximize that hides the
  left panel), and Edit (available once a real result exists; it preserves the objective).
- **A genuinely working preview.** When Build runs, JARVIS uses an AI model to generate a real, self-contained
  app from the description and type, and renders it live in the preview. This is real generated output, not a
  canned demo.
- **Honest states throughout.** The preview and conversation reflect the true situation at every moment:
  *generating* while the model works, *ready* when a real app renders, *failed* with the reason if generation
  errors, and *blocked* if the AI service isn't configured. No fabricated progress or fake success.

## User flow
1. **Home** — type a description in the lower-center input (or open the Menu → Builder), then continue.
2. **Builder** — the description is prefilled; pick one app type; press Build.
3. **Workstation** — the objective and type appear on the left; JARVIS generates the app and the preview
   renders the real result on an opaque surface.
4. **Preview / Run** — view the live rendered app. **Inspect** shows its actual source.
5. **Full Screen** — maximize the preview (left panel hides). **Exit Full Screen** restores the split view.
6. **Edit** — refine the app (the original objective is preserved) and re-render.

## UI/UX feel
Very dark, premium, restrained. Cool blue light only — no bright panels, no excessive glow. Live elements are
positioned relative to responsive layout containers over the background images, not pinned to fixed pixel
spots, so the intended 16:9 desktop composition holds and smaller screens stay usable. The existing approved
JARVIS wordmark/logo is reused.

## Implementation phases

### Phase 1 — MVP (built now)
- Rebuild Home, Builder, and Workstation as live UI over the three new clean background images, with the full
  flow Home → Builder → Workstation → Preview → Full Screen → Exit Full Screen → Edit.
- Wire the AI code-generation runtime: Build sends the description + type to JARVIS, which generates a real,
  self-contained web app and renders it live in the opaque preview surface.
- Web App type produces a fully rendered result; Mobile App shows the same real result inside a phone-style
  frame; AI Model produces a real, interactive generated interface.
- Inspect shows the true generated source; Edit lets the user refine and re-render while preserving the
  objective. All states (generating / ready / failed / blocked) are truthful.

### Phase 2 — Iterate and persist (later)
- Conversational refinement in the Workstation: follow-up directives that regenerate or patch the current app.
- Saved projects and revision history, reopening past builds, and download/share of a generated app.

### Phase 3 — Real execution & delivery (later)
- Multi-file projects and a fuller execution runtime, launching/deploying a built app, and true Mobile/AI-Model
  runtimes beyond the single-surface preview.

## Assumptions
- **AI model & key:** JARVIS will use the Emergent universal key with the default model `gpt-5.4` for generation.
  This draws from the account's credit balance; a personal provider key can be supplied instead at any time.
- **What "working preview" means in Phase 1:** the generated app is a single self-contained web document
  (HTML + inline styles/scripts, CDN allowed) rendered in a sandboxed, opaque preview surface. It is real
  generated output, but it is a front-end app preview — not a multi-service backend deployment (that is Phase 3).
- **AI Model type in Phase 1:** produces a real, interactive generated web interface for the described model
  rather than running an actual trained model in the browser.
- The three attached clean images become the new Home/Builder/Workstation backgrounds; the current
  Home/Builder background images and the procedural Workstation background are replaced by them.
- The existing approved JARVIS wordmark/logo asset is reused; no new logo is created.
- Home's command input and Builder's Build both lead into the same generation flow; a directive is required
  before Build proceeds.
- Only one app type can be selected at a time; Web App is the default selection on the Builder screen.
- The existing keyboard shortcut to return Home and the current sound cues are preserved.

---

# Technical Implementation (for review before Build)

## How a real app is generated, executed, turned into an Artifact, and previewed

Today the Workstation truthfully reports "no execution runtime connected": `agentState` starts as `BLOCKED`,
the timeline logs `runtime=none dispatch=skipped`, and `artifact` stays `null` so nothing renders. This build
connects a real runtime. The honest chain is:

1. **Generate (backend, real LLM call).** Build posts the description + app type to a new endpoint
   `POST /api/builder/generate`. The backend uses `emergentintegrations` (`LlmChat`, model `gpt-5.4`) with a
   system prompt that requires the model to return **one self-contained HTML document** (inline CSS/JS; CDN
   allowed) — a complete, runnable web app. No fabricated text is ever rendered; only what the model returns.
2. **Create the Artifact.** The backend wraps the returned source in the existing `Artifact` shape
   (`id`, `name`, `revision`, `status`, `source`, `createdAt`, `appType`) and persists it to a new MongoDB
   collection `artifacts` (UUID ids, no ObjectId). Status is `verified` only when a non-empty HTML document
   with an `<html`/`<body` root was actually returned; otherwise `failed` with `failureReason`.
3. **Execute / serve.** There is **no server-side code execution** in this phase. The generated app is served
   as source and *run by the browser inside a sandboxed iframe* — this is the execution environment. It is a
   real, running app, not a mockup.
4. **Render in Preview.** The existing `GeneratedArtifact` component already renders `artifact.source` via
   `<iframe srcDoc={source} sandbox="allow-scripts">` on an opaque white surface. Preview is driven strictly
   by `artifact.status`: `generating` → "Building artifact"; `verified` + source → live iframe;
   `failed` → blocked panel with the reason. `agentState` moves `WORKING → ARTIFACT_READY` (or `BLOCKED`).

### Execution environment & isolation boundaries
- **Environment:** the browser, via an `<iframe>` with `srcDoc` and `sandbox="allow-scripts"` **without**
  `allow-same-origin`. The generated app therefore cannot read the parent DOM, JARVIS state, cookies, or the
  user's origin/session — it is fully isolated from the host app.
- **Boundary the build stops at (does not cross in Phase 1):** no multi-file projects, no backend/server
  processes for the generated app, no package install, no network deploy. Anything requiring a running server
  for the generated app is Phase 3.

### Required credentials / services
- `EMERGENT_LLM_KEY` in `backend/.env` (Emergent universal key, default model `gpt-5.4`) — or a personal
  provider key if preferred. **This is the exact first boundary:** if the key is absent/invalid, the endpoint
  returns an explicit blocked result and the Workstation shows the existing truthful `BLOCKED` state — never a
  fake success.
- Existing MongoDB via `MONGO_URL` / `DB_NAME`. No other external service.

## Files to change

**Backend**
- `backend/server.py` — add a builder router: `POST /api/builder/generate` (description, appType, sessionId →
  Artifact), `GET /api/builder/artifact/{id}`; add the LLM call, key check, and `artifacts` persistence.
- `backend/requirements.txt` — add `emergentintegrations`.
- `backend/.env` — add `EMERGENT_LLM_KEY` (only after you confirm the key choice).

**Frontend**
- `src/assets/images/` — add the three new clean images (home / builder / workstation).
- `src/components/JarvisHeroScreen.tsx` — rebuild over clean image 1: retire the baked-artwork + pixel-rect
  hit-target/masking approach; draw the live wordmark (JarvisLogo), Menu (Builder access), and lower-center
  command input as real React elements in responsive containers. Keep the image's blue radar decorative.
- `src/components/BuilderIntakeScreen.tsx` — rebuild over clean image 2: live wordmark, live multiline
  description (prefilled from Home), single-select Web/Mobile/AI type chips (Web default), Build button. Keep
  the red radar decorative.
- `src/components/Stage2Workspace.tsx` — set clean image 3 as an opaque, dark background; wire Build/Edit to
  call `/api/builder/generate` and drive `artifact` + `agentState`; keep the 35% / 65% split, Preview / Inspect
  / Full Screen (in-app maximize) / Edit. Edit sends a refine instruction on the same `sessionId` and preserves
  the objective.
- `src/components/GeneratedArtifact.tsx` — reuse as-is for rendering; ensure the surface stays fully opaque, and
  wrap the iframe in a phone-style frame when `appType === 'mobile'`.
- `src/utils/api.ts` (new) — small fetch helper using `process.env.REACT_APP_BACKEND_URL` (no hardcoded URLs).
- `src/utils/artwork.ts` — pixel-rect placement/masking is no longer used by the new screens; the full-bleed
  background helpers may be reused or retired.
- `src/types.ts` — add `appType` to `Artifact` (optional); lifecycle unchanged.

## Image → component mapping (confirmations you asked for)
- **Image 1** (blue observatory, blue radar) → `JarvisHeroScreen` (Home). Radar stays decorative; no second
  radar drawn.
- **Image 2** (red/blue landscape, red radar) → `BuilderIntakeScreen` (Builder). Radar stays decorative.
- **Image 3** (dark observatory, no radar) → `Stage2Workspace` (Workstation) background.
- **Logo:** the approved JARVIS wordmark (`JarvisLogo` SVG / `jarvis_logo_chrome.png`) remains a **separate live
  overlay element** on Home and Builder — it is not part of the background image.
- **No old text/controls bleed through:** the new images are clean, and every control is a live React element,
  so the previous baked buttons/labels/placeholders and their masking overlays are removed entirely. Nothing
  from the old artwork can show through.

## What this build delivers vs. later work
- **Delivered now (Phase 1):** the three live screens over the clean images; Build generates a real
  self-contained web app via the LLM; it renders live in the sandboxed, opaque preview; Inspect shows the true
  source; Full Screen/Exit and Edit work; all states are truthful. A blocked/failed preview is reported as such
  and is **never** presented as a working build.
- **Not in this build:** conversational multi-turn refinement history and saved projects (Phase 2);
  multi-file/server execution, deploy/launch, true Mobile/AI-Model runtimes (Phase 3). **Canva/Media
  integration is explicitly out of scope for this App Builder task.**

## Ordered implementation plan
1. Add the three clean images; replace the Home background and rebuild its live controls (wordmark, Menu,
   command input). Verify the directive carries to Builder.
2. Rebuild the Builder over image 2 with live description, single-select types (Web default), and Build; verify
   description + type carry to the Workstation.
3. Backend: add `EMERGENT_LLM_KEY`, `emergentintegrations`, and `POST /api/builder/generate` returning a real
   verified/failed Artifact from a self-contained-HTML system prompt; persist to `artifacts`.
4. Wire the Workstation to call the endpoint on Build: drive `generating → verified/failed`, render source in
   the sandboxed iframe over image 3; keep the 35/65 split opaque.
5. Wire Inspect (real source), Full Screen/Exit (in-app maximize), and Edit (refine + re-render, objective
   preserved); add the mobile phone-frame wrapper.
6. Backend tests, then (with your permission) frontend flow tests.

## Acceptance checks
**Build & preview a simple Web App (proves the runtime):**
- `POST /api/builder/generate` with `{ description: "a to-do list web app", appType: "web" }` returns 200 with
  `status: "verified"` and `source` containing an `<html>` document; the record exists in `artifacts`;
  `GET /api/builder/artifact/{id}` returns the same source.
- With no/invalid `EMERGENT_LLM_KEY`, the endpoint returns an explicit blocked/failed result (no fake success).

**End-to-end flow (frontend):**
- Home: typing a description and pressing Enter/Send opens Builder with the description prefilled.
- Builder: Web is selected by default, only one type can be active, Build carries description + type to the
  Workstation.
- Workstation: objective + type show on the left; preview shows `generating`, then a live rendered iframe
  (`verified`) on a fully opaque surface (background image never shows through the app).
- Inspect shows the real generated source; Full Screen hides the left panel and Exit restores the split; Edit is
  enabled only after a verified artifact and preserves the original objective on re-render.
- Truthful failure: if generation fails, the preview shows the blocked/failed panel with a reason — never a
  rendered "success".

---

# Amendments — First Working Web App Milestone (confirmations before Build)

This is the **first working Web App milestone**, not completion of the full App Builder and **not equivalent
to the Emergent platform**. The capabilities below are confirmed; the deferred ones are recorded as required
future milestones.

## 1. What "verified" means (real render + interaction, not just an HTML string)
The `Artifact` lifecycle `generating → unverified → verified / failed` is used literally:
- **generating** — the LLM call is in flight.
- **unverified** — the backend returned an HTML document that passed basic structural validation
  (non-empty, has `<html>`/`<body>` root). An HTML string alone stops here; it is **not** "verified".
- **verified** — only after the client actually renders the document in the sandboxed iframe **and** a
  verification harness confirms it (a) loaded without a fatal script error and rendered visible content, and
  (b) responded to one basic interaction (the first interactive control is programmatically exercised and
  throws no error). The harness runs **inside** the iframe and reports the result to JARVIS via
  `window.postMessage` (this works across the sandbox without `allow-same-origin`), correlated by a one-time
  nonce; a timeout counts as failure.
- **failed** — validation failed, the render threw, the interaction errored, or the harness timed out; the
  preview shows the honest failed panel with the reason.
- **blocked** — no runnable runtime (e.g., model key unavailable); the Workstation shows the existing truthful
  BLOCKED state. Failed and blocked are always shown honestly and are never presented as a working build.

## 2. Iframe isolation — CSP + hardened sandbox, and how it's tested
- **Sandbox:** `sandbox="allow-scripts"` only — deliberately omits `allow-same-origin`, `allow-forms`,
  `allow-popups`, `allow-top-navigation`, and `allow-modals`, so the app cannot reach JARVIS's origin/session,
  submit forms that navigate, open popups, or navigate the top window.
- **Content-Security-Policy** injected as a `<meta http-equiv="Content-Security-Policy">` into the generated
  document `<head>`:
  `default-src 'none'; script-src 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://cdn.tailwindcss.com;
  style-src 'unsafe-inline' https:; img-src data: https:; font-src https: data:; connect-src 'none';
  frame-src 'none'; object-src 'none'; base-uri 'none'; form-action 'none'`.
  Note `connect-src 'none'` blocks all external network requests (fetch / XHR / WebSocket) from the generated
  app, and the sandbox + `form-action 'none'` block navigation. CDN hosts are allow-listed only for scripts/
  styles the generated app needs.
- **How isolation is tested:** a test artifact that attempts `fetch('https://example.com')`, a top-level
  navigation, and a read of `window.parent.location`; the in-iframe harness listens for
  `securitypolicyviolation` and errors and reports via `postMessage`. Acceptance = the network request and
  navigation are blocked and the parent origin is untouched. This runs as part of the isolation test.

## 3. Only Web App builds in this milestone
- **Web App** is the only working, selectable build type and the default selection.
- **Mobile App** and **AI Model** chips are shown **disabled / deferred** with a visible "Soon" marker; they
  cannot be selected and the Build action never proceeds for them, so no button implies they work. If reached,
  the UI states plainly that the type is not available in this milestone.

## 4. Credits, key/model availability, and secret handling
- **Expected Emergent credit usage:** one Build = **one LLM completion** (`gpt-5.4`) generating a
  self-contained HTML app — on the order of a few thousand output tokens, i.e. roughly one small model
  completion's cost. **Verification uses no model call** (it is client-side render + interaction), so a
  verification test costs **0 additional credits**. A single generation + verification acceptance run therefore
  consumes the cost of exactly one completion. (Exact credits depend on final output size; this is an estimate,
  not a fixed number.)
- **Availability:** per the LLM integration playbook, the Emergent universal key (`EMERGENT_LLM_KEY`) is
  provided for this project and `gpt-5.4` (OpenAI) is listed as available and is the recommended default. Final
  confirmation is a single live test call during Build.
- **Secret handling:** the key lives only in `backend/.env` (server-side), must remain excluded from Git, and
  is **never** placed in frontend code or sent to the browser — the browser only calls the `/api` endpoint, and
  only the backend talks to the model. A build-time check confirms `.env` stays gitignored.

## 5. Exact acceptance demonstration
1. On **Home**, enter a simple Web App request (e.g., "a to-do list web app") → **Builder** opens with the
   description prefilled.
2. **Web App** is selected by default → press **Build** → the **Workstation** opens with the objective + type.
3. The artifact goes **generating → unverified**, then renders in the sandboxed preview and the harness marks
   it **verified**.
4. **Click a working control inside the rendered app** (e.g., add a to-do item) and see it respond live.
5. Enter **Full Screen** (left panel hides, preview maximizes) and **Exit Full Screen** (split view restores).
6. **Edit the objective** (change/append), re-generate — the original objective is preserved and a new revision
   renders.
7. **Genuine error:** with the model key unavailable, Build produces an honest **blocked** state (reason shown)
   — never a rendered success. A malformed/unrenderable generation produces an honest **failed** state.

## Required future milestones (toward the full product)
- **Milestone 2:** conversational multi-turn refinement with saved revision history and reopenable projects.
- **Milestone 3:** multi-file projects and a real server/backend execution runtime for generated apps.
- **Milestone 4:** working **Mobile App** build/preview runtime.
- **Milestone 5:** working **AI Model** build type.
These are prerequisites for the full App Builder and are explicitly not delivered in this first milestone.
Canva/Media integration remains out of scope for the App Builder task entirely.
