# DESIGN_CRITIC_STANDARD

Status: Normative  
Scope: Evaluation of rendered Builder candidates.

## 1. Core rule

Builder MUST use specialized critics rather than one overall aesthetic score.

Do not use a single “87/100” score as the acceptance mechanism.

## 2. Critic set

### StructuralCritic
Checks:
- clipping
- overflow
- collisions
- invalid geometry
- broken constraints
- inaccessible content
- inconsistent sizing
- viewport violations

### VisualCritic
Checks:
- hierarchy
- composition
- spacing
- alignment
- typography
- contrast
- grouping
- visual balance
- density
- focal competition
- dead space
- consistency
- generic/template appearance
- excessive decorative effects

### TaskCritic
Checks:
- primary task obvious
- primary decision supported
- important evidence visible
- next meaningful action understandable
- unnecessary navigation avoided
- persistent context preserved
- expert workflow efficient

### DomainCritic
Checks:
- automotive meaning correct
- state distinctions correct
- module relationships truthful
- communication failure not conflated with component fault
- units/timestamps retained where required
- stale/missing/zero not confused
- evidence relationships correct

### SafetyCritic
Checks:
- risky actions exposed appropriately
- preconditions present
- consequences explicit
- recovery available
- destructive actions visually and behaviorally distinct
- safety state truthful

### AccessibilityCritic
Checks:
- contrast
- keyboard operation
- focus behavior
- readability
- non-color-only semantics
- scalable text
- meaningful status communication

### RegressionCritic
Checks:
- what changed
- what was supposed to change
- what should not have changed
- accepted decisions preserved
- unintended regressions absent

## 3. Critic precedence

Conflict-resolution order:

1. SafetyCritic
2. DomainCritic
3. TaskCritic
4. AccessibilityCritic
5. StructuralCritic
6. VisualCritic
7. Refinement

The prettier interpretation does not beat the truer interpretation.

## 4. Defect classes

### BLOCKER
Candidate must not proceed to human acceptance.

Examples:
- critical region clipped
- unsafe action exposed incorrectly
- domain state materially misleading
- primary workflow unusable

### MAJOR
Meaningfully damages usability, truth, safety, task flow, or evidence interpretation.

### MINOR
Noticeable quality deficiency that does not break the primary workflow.

### REFINEMENT
Polish opportunity only.

## 5. Gate priority

Defects should be prioritized by impact on:
1. safety
2. diagnostic truth
3. primary task
4. evidence interpretation
5. usability
6. accessibility
7. visual consistency
8. refinement

## 6. Stop gates

A candidate may be presented for human review only when:
- Blockers == 0
- Safety Majors == 0
- DomainTruth Majors == 0
- Task Majors == 0
- required acceptance criteria == PASS
- required accessibility criteria == PASS
- unintended regressions == none
- iteration budget not exceeded

Minor/refinement findings may remain visible.

## 7. Critic report

A valid report should identify:
- critic
- severity
- affected component(s)
- evidence
- violated rule
- recommended correction
- confidence
- whether correction is safe to automate

Example:

Candidate ready for review.
- 0 Blockers
- 0 Safety Majors
- 0 Domain Majors
- 0 Task Majors
- 2 Minor visual findings
- 3 Refinement opportunities

## 8. Self-rejection

Builder may reject its own candidate when:
- any blocker exists,
- required acceptance criteria fail,
- a higher-priority critic vetoes a lower-priority decision,
- unintended regression is detected.

Builder must record why it rejected the candidate.

## 9. Visual-inspection evidence

Where available, critique should use:
- screenshot
- semantic component tree
- component geometry
- state metadata
- previous revision screenshot
- previous revision semantic tree

Visual appearance alone is insufficient for domain or safety conclusions.
