# CONVERSATIONAL_REVISION_MODEL

Status: Normative  
Scope: Natural-language modification of an existing design.

## 1. Core guarantee

A local request produces a local change unless the user explicitly requests broader redesign.

More precisely:

A local request may change:
1. the explicitly requested property, and
2. the minimum constraint-driven collateral properties required to preserve a valid design.

Unrelated accepted decisions must remain unchanged.

## 2. Revision pipeline

Reference Resolution
→ Intent Resolution
→ Property Resolution
→ Constraint Check
→ Minimal Change Plan
→ Preserve Accepted State
→ Create Revision
→ Render
→ Inspect
→ Diff
→ Report

## 3. Reference resolution

ReferenceResolution:
- candidateComponents
- selectedComponent
- confidence
- evidence

Rules:
- high confidence → perform bounded edit
- medium confidence → use active selection/screen context if sufficient
- low confidence → ask for clarification

JARVIS must never silently choose an arbitrary component.

## 4. Semantic component identity

Reference resolution should prioritize:
1. active selection
2. stable semanticId
3. explicit component name
4. current region/context
5. spatial description
6. conversational history

Coordinates alone are not identity.

## 5. Intent resolution

Identify whether the user intends:
- resize
- move
- reorder
- restyle
- hide/show
- change hierarchy
- change density
- change typography
- change interaction
- change state behavior
- restore prior decision
- compare revisions
- broader redesign

## 6. Property resolution

Map natural language to design properties.

Example:
“Make the left panel 20% narrower.”

Target:
- alexis.systemScan.moduleNavigator

Property:
- layout.width

Change:
- relative width delta -20%

## 7. Constraint check

Before applying, test:
- min/max size
- viewport constraints
- neighboring region constraints
- accepted design decisions
- accessibility
- task support
- domain/safety rules

If the request conflicts with higher authority, explain the conflict and preserve the higher rule.

## 8. Direct and collateral changes

Report separately.

Example:

Requested:
- moduleNavigator width -20%

Direct:
- moduleNavigator.width 28% → 22.4%

Constraint-driven:
- networkTopology.width 52% → 57.6%

Preserved:
- evidence inspector
- typography
- component order
- status semantics
- tokens
- interaction model

## 9. DesignRevision

DesignRevision:
- revisionId
- parentRevisionId
- originatingInstruction
- origin
  - human
  - critic
  - systemConstraint
- affectedComponents
- directChanges
- collateralChanges
- preservedDecisions
- criticFindingsBefore
- criticFindingsAfter
- designSpecSnapshot
- renderArtifact
- screenshotArtifact
- semanticTreeArtifact
- acceptanceState
- timestamp

## 10. Revision states

- Draft
- Candidate
- CriticRejected
- CriticPassed
- HumanRejected
- Accepted
- Superseded
- Locked

A locked revision is immutable.
Later changes create descendants.

## 11. Selective rollback

A user may request:
- restore one property from a prior revision
- restore one component
- restore one design decision
- keep newer unrelated changes

Example:
“Put the topology spacing back to Revision 17 but keep the narrower module panel.”

This creates a new revision; it does not rewrite history.

## 12. Report after edit

Builder should report:
- resolved target
- direct changes
- collateral changes
- preserved decisions
- new revision ID
- critic result
- any remaining findings

## 13. Ambiguity behavior

If confidence is insufficient:
- do not guess,
- do not apply broad edits,
- ask the smallest clarifying question necessary.
