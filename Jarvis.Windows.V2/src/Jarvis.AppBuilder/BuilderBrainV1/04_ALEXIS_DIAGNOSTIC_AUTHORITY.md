# ALEXIS_DIAGNOSTIC_AUTHORITY

Status: Normative  
Scope: Domain truth used by Builder when designing automotive diagnostic experiences.

## 1. Purpose

This contract ensures that ALEXIS visual design is grounded in diagnostic reality.

A visual designer may not invent automotive truth to improve composition.

## 2. Domain areas

Builder must reason about:
- vehicle identity
- ECUs/modules
- networks/buses
- gateways
- communication state
- DTC semantics
- freeze-frame data
- live data
- diagnostic sessions
- supported identifiers
- routines
- active tests
- service functions
- coding
- programming
- preconditions
- evidence
- verification
- reporting/history

## 3. Provenance

Domain-sensitive information should use:
- verified
- derived
- configured
- observed
- userProvided
- unknown

Examples:
- DTC meaning → verified OEM/standard source
- network relationship → verified vehicle definition
- expected sensor range → verified/configured vehicle source
- diagnostic hypothesis → derived
- current PID value → observed

## 4. Unknown truth policy

When evidence is insufficient, represent:
- UNKNOWN
- UNAVAILABLE
- UNSUPPORTED
- NOT_YET_DISCOVERED

Do not fabricate:
- module names,
- DTC meanings,
- expected values,
- topology relationships,
- coding options,
- repair procedures,
- fault severity,
- vehicle identity.

## 5. Diagnostic-state separation

Do not conflate:
- communication failure with component fault,
- scan completion with vehicle health,
- historical fault with active fault,
- pending fault with confirmed fault,
- stale live data with current live data,
- unsupported parameter with failed parameter acquisition.

## 6. DTC representation

A professional DTC representation may include:
- identifier
- description
- source ECU
- status
- confirmed/pending/historical
- occurrence evidence where authoritative
- freeze-frame availability
- test conditions
- related evidence
- repair/guidance link
- verification status

The exact fields depend on available authority.

## 7. Freeze-frame semantics

Freeze-frame is event-linked evidence.

The design should support:
- associated DTC/event
- parameter name
- value
- unit
- timestamp/event identity
- authoritative expected range if available
- comparison with current data
- sorting/filtering
- copy/export
- related-parameter navigation

Do not show freeze-frame as an undifferentiated wall of numbers.

## 8. Live-data semantics

Live data may require:
- parameter selection
- source ECU
- units
- current value
- min/max
- authoritative expected range if available
- timestamp
- update rate
- freshness
- pause
- recording
- zoom/pan
- cursor inspection
- multi-signal comparison
- threshold markers
- outlier emphasis

The UI must distinguish current, stale, unavailable, unsupported, missing, invalid, and failed.

## 9. Topology semantics

Vehicle topology should represent meaningful network relationships, not decorative node placement.

Important concepts:
- ECU/module identity
- bus/network membership
- gateway relationships
- communication state
- selected path
- failure boundaries
- discovered vs verified relationships
- focus-plus-context
- stable layout where possible

## 10. Guided diagnostics

Guided diagnostic experiences should preserve:
- hypothesis
- evidence already gathered
- test prerequisites
- expected outcome
- actual outcome
- next decision
- branching rationale
- ability to resume after interruption

## 11. Verification doctrine

A repair is not complete merely because a command succeeded.

Verification may require:
- rescan
- before/after comparison
- current-state validation
- communication validation
- fault recurrence check
- live-data confirmation
- preserved evidence/report

## 12. Canonical fixture: System Scan

Fixture: ALEXIS_SYSTEM_SCAN_001

Context:
- operatingContext: workshopStationary
- userRole: technician
- primaryTask: scan

Scenario:
- supportedModules: 34
- respondingModules: 32
- activeDTCs: 4
- communicationFailures: 2
- scanState: complete

Expected truths:
- scanState == complete
- connectionCoverage == partial
- diagnosticState includes activeFaults
- diagnosticState includes communicationFaults

Prohibited conclusion:
- vehicleHealthy

Required capabilities:
- moduleFiltering
- topologyNavigation
- faultDrilldown
- retryFailedModules
- report
- comparison

## 13. Canonical validation suite

Required architectural fixtures:
1. System Scan
2. DTC Investigation
3. Vehicle Topology
4. Live Data
5. Guided Diagnostic Test
6. Verification / Rescan

These are architecture tests, not merely screenshots.
