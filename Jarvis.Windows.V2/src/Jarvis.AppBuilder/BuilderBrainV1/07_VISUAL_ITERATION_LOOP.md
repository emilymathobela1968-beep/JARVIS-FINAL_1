# VISUAL_ITERATION_LOOP

Status: Normative  
Scope: Controlled generation, inspection, critique, revision, and human acceptance.

## 1. Purpose

The loop makes high-quality design repeatable and inspectable.

It is not an unbounded autonomous beautification process.

## 2. Controlled loop

1. Interpret user objective.
2. Build context model.
3. Build user/role model.
4. Build task model.
5. Consult domain authority.
6. Establish information architecture.
7. Produce ScreenDesignSpec.
8. Produce candidate visual design.
9. Render candidate.
10. Capture screenshot.
11. Capture semantic component tree.
12. Capture geometry/state metadata.
13. Run StructuralCritic.
14. Run VisualCritic.
15. Run TaskCritic.
16. Run DomainCritic.
17. Run SafetyCritic.
18. Run AccessibilityCritic.
19. Classify defects.
20. Correct Blockers.
21. Correct Major defects.
22. Rerender.
23. Run RegressionCritic.
24. Compare with previous candidate.
25. Repeat within bounded iteration budget.
26. Present candidate to human.
27. Human accepts, rejects, or requests revision.

## 3. Iteration limits

Builder must use a bounded iteration budget.

The exact numeric budget is implementation-configurable, but the policy is:
- do not iterate indefinitely,
- do not silently redesign,
- do not hide unresolved defects,
- surface persistent conflicts to the human.

## 4. Automated correction boundary

Safe for automated correction only when:
- target component is high-confidence,
- change is local,
- higher-priority authority is not affected,
- collateral changes are bounded,
- regression checks can verify preservation.

Otherwise escalate for human direction.

## 5. Human acceptance

The acceptance chain:

Draft
→ Candidate
→ CriticRejected or CriticPassed
→ HumanRejected or HumanAccepted
→ Accepted
→ Locked

`Locked` requires explicit human action.

## 6. Full-canvas evaluation modes

Builder should support four conceptual modes:

### DESIGN
Shows:
- structure
- authority
- context
- rationale
- critique metadata

### PREVIEW
Shows:
- full available application canvas
- no unnecessary Builder chrome

### COMPARE
Shows:
- current revision
- selected previous revision
- relevant differences

### INSPECT
Shows:
- semantic component identity
- geometry
- properties
- constraints
- state
- critique findings

## 7. Canonical validation suite

The loop should eventually be tested against:
1. System Scan
2. DTC Investigation
3. Vehicle Topology
4. Live Data
5. Guided Diagnostic Test
6. Verification / Rescan

Each fixture tests architecture, not just appearance.

## 8. Developer handoff boundary

Developer/Codex handoff occurs only after:
- design spec exists,
- critical critique gates pass,
- human accepts the design,
- design decisions are recorded,
- accepted revision is stable.

Implementation MUST consume the accepted design contract rather than silently redesigning it.

## 9. First implementation boundary

The first engineering stage after specification acceptance should be limited to:

ContextModel
+ ScreenDesignSpec
+ Persistent DesignRevision
+ one existing System Scan path consuming that specification

Explicitly excluded:
- new visual redesign
- Qt migration
- 3D
- advanced animation
- broad code-generation changes
- generic “premium” styling
- autonomous large-scale refactoring
- full self-optimizing critique before the model is stable
