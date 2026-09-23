# SCREEN_DESIGN_SPEC

Status: Normative  
Scope: Renderer-independent source of truth for every Builder screen.

## 1. Core rule

`ScreenDesignSpec` contains semantic design intent only.

It MUST NOT contain:
- HTML elements,
- CSS declarations,
- QML anchors,
- XAML row/column syntax,
- React implementation code,
- framework-specific layout instructions,
- renderer-specific assumptions.

Rendering path:

ScreenDesignSpec
→ Renderer Adapter
→ HTML / React / QML / XAML / future target

## 2. Top-level contract

ScreenDesignSpec
- metadata
- contextModel
- userModel
- taskModel
- domainModel
- informationArchitecture
- visualHierarchy
- layoutModel
- componentTree
- stateModel
- actionModel
- interactionModel
- visualizationModel
- tokenReferences
- accessibilityRequirements
- safetyRequirements
- evidenceRequirements
- acceptanceCriteria
- designRationale

## 3. Metadata

Minimum:
- projectId
- screenId
- screenName
- revisionId
- designProfile
- schemaVersion
- createdAt
- createdBy
- parentRevisionId

## 4. Context model

Must include:
- operatingContext
- environment
- vehicleContext
- interactionMode
- timePressure
- hardwareContext
- safetyContext

OperatingContext:
- inVehicleMoving
- stationaryUnsecured
- workshopStationary

## 5. User model

Must include:
- role
- expertise
- expected terminology
- interaction preferences
- accessibility needs if known

Roles:
- driver
- technician
- engineer
- serviceAdvisor

## 6. Task model

Must include:
- primaryTask
- userGoal
- primaryDecision
- secondaryDecisions
- completionCondition
- evidenceNeeded
- allowedInterruptions
- recoveryExpectations

Typical primary tasks:
- identifyVehicle
- scan
- investigateFault
- inspectTopology
- inspectFreezeFrame
- liveData
- actuatorTest
- serviceFunction
- coding
- programming
- guidedDiagnosis
- verification

## 7. Information architecture

Must classify:
- primaryInformation
- secondaryInformation
- supportingEvidence
- technicalDetail
- disclosureRules
- persistentContext
- cross-screen relationships

## 8. Visual hierarchy

Must define:
- primary focal region
- secondary focal regions
- low-priority regions
- alert priority
- reading/scanning order
- emphasis rationale

## 9. Layout model

Renderer-independent concepts only:
- viewportIntent
- gridIntent
- regions
- constraints
- resizingRules
- minimumUsableSize
- density
- alignment
- distribution
- overflowPolicy

## 10. Component tree

Every meaningful component requires stable semantic identity.

ComponentSpec:
- semanticId
- componentType
- purpose
- parent
- children
- properties
- constraints
- bindings
- states
- interactions
- provenance

Example semantic IDs:
- alexis.systemScan.vehicleContext
- alexis.systemScan.scanProgress
- alexis.systemScan.moduleNavigator
- alexis.systemScan.networkTopology
- alexis.systemScan.findingsSummary
- alexis.systemScan.evidenceInspector
- alexis.systemScan.primaryActions

Coordinates and proximity are NOT identity.

## 11. State model

State dimensions are orthogonal.

MotionState:
- unknown
- stationary
- moving

IgnitionState:
- unknown
- off
- accessory
- on
- engineRunning

ConnectionState:
- disconnected
- connecting
- connected
- degraded
- timedOut
- unsupported

ScanState:
- idle
- running
- paused
- cancelled
- partial
- complete
- failed

DiagnosticState may include:
- noFaults
- activeFaults
- historicalFaults
- pendingFaults
- communicationFaults
- evidenceAvailable

DataState:
- current
- stale
- unavailable
- unsupported
- missing
- invalid
- failed
- unknown

OperationState:
- idle
- ready
- blocked
- awaitingConfirmation
- running
- paused
- completed
- failed
- cancelled
- recoverable

SafetyState:
- unknown
- safe
- caution
- blocked
- unsafe

## 12. Normative data semantics

- zero: valid measured value equal to 0
- missing: expected data was not supplied
- unavailable: data cannot currently be obtained
- unsupported: source/function does not provide the data
- stale: previously valid data is no longer sufficiently current
- invalid: received value failed validation
- failed: attempt to acquire/process failed
- unknown: insufficient evidence to classify

These terms MUST NOT be substituted.

## 13. Action model

Each meaningful operation uses `ActionDesignSpec`:

- actionId
- purpose
- riskClass
- preconditions
- permissions
- availableState
- warningState
- blockedState
- unsupportedState
- confirmationRequirements
- progressState
- cancellationPolicy
- successState
- failureState
- recoveryActions
- evidenceImpact

## 14. Visualization model

Must define:
- data source
- data semantics
- task purpose
- encoding intent
- comparison model
- selection model
- time model
- units
- freshness
- thresholds
- interaction constraints

A graph without a task purpose should not exist.

## 15. Evidence requirements

Must define:
- required evidence,
- provenance,
- timestamps,
- units,
- expected ranges where authoritative,
- links between evidence and fault,
- what must be preserved before destructive actions.

## 16. Acceptance criteria

Criteria must be explicit and testable where possible.

Examples:
- all required regions fit target viewport,
- no critical state relies on color alone,
- communication failure remains distinct from active component fault,
- scan completion does not imply health,
- units/timestamps shown where required,
- primary task remains obvious,
- safe recovery path exists for blocked operations.
