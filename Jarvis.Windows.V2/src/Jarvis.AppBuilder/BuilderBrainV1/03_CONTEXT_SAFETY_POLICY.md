# CONTEXT_SAFETY_POLICY

Status: Normative  
Scope: How operating context changes design permissions and safety behavior.

## 1. Core rule

ALEXIS MUST NOT use one generic automotive UI profile.

Design behavior is selected from:
- operating context,
- user role,
- task,
- vehicle state,
- risk,
- environment.

## 2. Context classes

### 2.1 inVehicleMoving

Design for:
- minimal visual demand,
- minimal manual demand,
- low cognitive demand,
- interruptible tasks,
- concise messaging,
- driver-controlled pacing,
- strong lockouts for unsuitable operations.

Dense workstation behavior is prohibited.

### 2.2 stationaryUnsecured

Design for:
- explicit vehicle-state verification,
- conservative interaction,
- restricted hazardous actions,
- clear preconditions,
- visible safety state,
- recoverable operations,
- avoidance of assumptions that the vehicle cannot move.

### 2.3 workshopStationary

Design may permit:
- dense technical information,
- multiple coordinated views,
- persistent inspectors,
- topology,
- tables,
- graphs,
- search/filtering,
- keyboard shortcuts,
- expert controls,
- long-form technical explanation,
- side-by-side comparison.

Still mandatory:
- truthful state,
- safety lockouts,
- explicit consequences,
- evidence preservation,
- clear hierarchy,
- error recovery,
- data freshness.

## 3. Risk classes

Recommended conceptual classes:
- informational
- lowRisk
- controlled
- highRisk
- destructive
- safetyCritical

Exact implementation values may evolve, but every meaningful action must have an explicit risk classification.

## 4. Preconditions-first rule

Risky actions are designed from operational rules first.

For each action determine:
- allowedWhen
- blockedWhen
- warningWhen
- authorizationNeeded
- evidenceImpact
- cancellationPolicy
- recoveryPath

The visual control is downstream of those facts.

## 5. Example: actuator test

Allowed when:
- context == workshopStationary
- vehicle secured
- valid ignition state
- battery condition acceptable
- communication stable
- module supports operation
- required authorization present

Blocked when:
- movement possible
- safety precondition fails
- communication degraded
- target module unavailable
- operation unsupported

If blocked:
- explain why
- list required conditions
- preserve context
- offer safe next action

## 6. Action availability semantics

A control may be:
- available
- availableWithWarning
- pendingPrecondition
- unavailable
- unsafe
- unsupported
- running
- completed
- failed
- recoverable
- irreversible

A disabled control without explanation is insufficient when the reason matters.

## 7. Destructive or evidence-affecting actions

Before operations such as clearing DTCs, resets, coding, programming, or calibration, the design should consider:
- what evidence may be erased,
- what modules are affected,
- what preconditions apply,
- what can be saved first,
- what will happen if interrupted,
- what verification follows.

## 8. Context transition

If the operating context changes, the interface must reevaluate permissions.

Examples:
- stationary → moving
- communication stable → degraded
- battery sufficient → insufficient
- secured → unsecured

The interface MUST NOT assume permissions remain valid after a context change.

## 9. Safety presentation

Safety state should be communicated using more than color:
- text,
- iconography,
- position,
- grouping,
- explicit condition labels,
- consequence descriptions.

## 10. Safety authority

Safety/governance outranks:
- user styling requests,
- accepted visual decisions,
- convenience,
- layout preference,
- animation,
- aesthetics.
