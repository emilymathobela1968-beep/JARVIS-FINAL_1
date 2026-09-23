# JARVIS Builder Design Intelligence V1

Status: Synthesis baseline locked  
Purpose: Permanent, implementation-independent design-intelligence contracts for JARVIS Builder / ALEXIS.

## Governing pipeline

User Intent
→ Context Model
→ User / Role Model
→ Task Model
→ Domain Authority
→ Information Architecture
→ Screen Design Spec
→ Visual Design
→ Render
→ Structural Inspection
→ Visual Inspection
→ Domain / Task Critique
→ Revision
→ Human Acceptance
→ Developer Handoff

## Core invariant

Rendering MUST NOT precede explicit design reasoning.

HTML, React, QML, XAML, WinUI, or any future output technology is downstream of `ScreenDesignSpec`.

## Contract set

1. `01_DESIGN_DOCTRINE.md`
2. `02_SCREEN_DESIGN_SPEC.md`
3. `03_CONTEXT_SAFETY_POLICY.md`
4. `04_ALEXIS_DIAGNOSTIC_AUTHORITY.md`
5. `05_DESIGN_CRITIC_STANDARD.md`
6. `06_CONVERSATIONAL_REVISION_MODEL.md`
7. `07_VISUAL_ITERATION_LOOP.md`

## Authority precedence

1. Safety / governance constraints
2. Verified domain truth
3. Explicit operating context
4. User task and required evidence
5. Accepted / locked design decisions
6. Accessibility requirements
7. Design doctrine
8. Current conversational design request
9. Visual refinement / stylistic preference

This order governs generation, critique, and revision.

## Acceptance authority

Critics may reject, request revision, or pass a candidate for review.
Critics may not mark a design human-accepted.
`Locked` requires explicit human action.

## Next engineering step

Do NOT redesign System Scan yet.

The first bounded implementation stage, after these contracts are accepted, should be:

ContextModel
+ ScreenDesignSpec
+ Persistent DesignRevision
+ one existing System Scan path consuming that specification

Excluded from that first implementation:
- Qt migration
- 3D
- advanced animation
- broad code-generation changes
- generic “premium” restyling
- autonomous large-scale refactoring
- full self-optimizing critique before the model is stable
