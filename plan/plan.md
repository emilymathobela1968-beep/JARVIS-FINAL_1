# JARVIS — Baseline Inspection (Read-Only)

## What this is
A strictly read-only inspection of the uploaded JARVIS ZIP (exported from Google AI
Studio). The goal is to establish the exact current baseline and hand back a written
report. Nothing is built, redesigned, restyled, refactored, or regenerated. No source
file is modified. No new application is created.

## Why approval is needed before the report can be produced
The uploaded archive has not been extracted, and in the current planning phase the tools
that could open it (archive extraction, file reads on the ZIP) are unavailable. The
file-analysis tool also cannot read `.zip` archives directly. As a result, the baseline
report cannot be generated during planning. Approving this plan unlocks the ability to
extract the archive into a temporary location and read its contents so the report can be
compiled.

## What will happen on approval
1. Extract the uploaded ZIP into a temporary working location (not into the project
   source tree). The archive contents are treated as reference material to read, not to
   run or alter.
2. Read the extracted files to identify the project root, framework/language, package
   manager, entry point, routing, major pages/components, styling system, and assets.
3. Produce a single baseline report with these 11 headings:
   1. PROJECT FOUND
   2. TECHNOLOGY STACK
   3. MAIN HERO SCREEN FILES
   4. APP BUILDER FILES
   5. PREVIEW SYSTEM
   6. VOICE / MICROPHONE COMPONENTS
   7. MENU / NAVIGATION COMPONENTS
   8. CURRENT ASSETS
   9. PLACEHOLDER / SIMULATED DATA FOUND
   10. GIT / PROJECT STATE
   11. SAFE NEXT IMPLEMENTATION POINT
4. Stop and wait for the separate implementation instruction.

## Explicit guardrails (will be honored)
- No building, no redesign, no new JARVIS version.
- No changes to source files, colors, assets, logos, dependencies, or lockfiles.
- No regeneration of any screen.
- No git repository will be initialized if one is not already present; git state is only
  reported if metadata exists inside the archive.
- No migrations, no deployment, no destructive installs, no source-changing upgrades.
- Any placeholder/demo/simulated preview data found is only reported, not removed.
- Dependency installation/build will not be run if it would change lockfiles or source
  state; run/build commands are only identified and reported, not executed.

## What the user gets at the end
The concise 11-section baseline report describing the existing JARVIS app exactly as it
is, plus a clearly marked "safe next implementation point" — then a pause for the next
instruction.

## Open question
- None. If, after extraction, the archive turns out to be missing files required to
  understand or run the app, that gap will be listed under section 11 rather than filled
  in or guessed.
