# MigrationLedger
Section-level migration ledger for the Stage 1 governance cleanup. Legacy files remain frozen; this ledger records what survives, where it belongs, and what is intentionally discarded.

Dispositions:
- `keep`: retained in the same active file with a narrower responsibility
- `move`: migrated to a different active target
- `archive`: kept only in `.agent/legacy/`
- `discard`: not carried forward into active truth

## Late Rename Map
- Stage 1 names remain unchanged.
- Stage 2 rename targets:
  - `Systems.md` + `TDD.md` -> `Architecture.md`
  - `GCD.md` -> `Design.md`

## Active Files
| Source Section | Target File | Disposition | Verification Source |
|---|---|---|---|
| `AGENTS.md / Read order` | `.agent/AGENTS.md` | keep | active workflow model + Stage 1 proposal |
| `AGENTS.md / rules about active doc roles` | `.agent/AGENTS.md` | keep | Stage 1 proposal |
| `AGENTS.md / `ContinuationPrompts.md` as active read-order input` | `.agent/ContinuationPrompts.md` | move | Stage 1 demotion decision |
| `Workflow.md / Task Read Order` | `.agent/Workflow.md` | keep | active governance + Stage 1 proposal |
| `Workflow.md / Document Dependency and Data Flow` | `.agent/Workflow.md` | keep | active governance |
| `Workflow.md / Mode + Lane Rules` | `.agent/Workflow.md` | move | simplified into authority, backlog contract, and lock rules |
| `Workflow.md / Standard Lane Set` | `.agent/Backlog.md` | discard | lane becomes optional routing metadata |
| `Workflow.md / Mandatory Update Contract` | `.agent/Workflow.md` | keep | active governance |
| `Workflow.md / No-Duplication Rule` | `.agent/Workflow.md` | keep | active governance |
| `Workflow.md / Scope and No-Crossover Rules` | `.agent/Workflow.md` | keep | active governance |
| `Workflow.md / Overlap Classes` | `.agent/Workflow.md` | move | simplified into explicit lock and authority-boundary rules |
| `Workflow.md / Backlog Planning Fields` | `.agent/Backlog.md` | move | replaced by Stage 1 backlog contract |
| `Workflow.md / Validation Requirements` | `.agent/Workflow.md` | keep | active governance + proposal validation levels |
| `Workflow.md / Consistency Check` | `.agent/Workflow.md` | keep | Stage 1 governance audit |
| `Workflow.md / Unity MCP Quick Checks` | `.agent/TDD.md` | move | technical validation policy |
| `Workflow.md / Reference Ownership` | `.agent/Workflow.md` | keep | active document responsibility model |
| `Workflow.md / Review Rule` | `.agent/Workflow.md` | keep | active governance |
| `Workflow.md / Wave Execution Plan` | `.agent/Backlog.md` | discard | planning order removed from truth docs |
| `Workflow.md / Autonomous Continuation` | `.agent/ContinuationPrompts.md` | discard | demoted derived guidance |
| `Workflow.md / Minimal Governance Checks` | `.agent/Workflow.md` | keep | Stage 1 consistency rules |
| `CurrentState.md / Project Snapshot` | `.agent/CurrentState.md` | keep | active truth |
| `CurrentState.md / Subsystem Status` | `.agent/CurrentState.md` | keep | active truth |
| `CurrentState.md / Active Risks` | `.agent/CurrentState.md` | keep | active truth |
| `CurrentState.md / Next Priorities` | `.agent/Backlog.md` | discard | planning removed from truth doc |
| `CurrentState.md / Execution Plan` | `.agent/Backlog.md` | discard | planning removed from truth doc |
| `Backlog.md / task field definitions` | `.agent/Backlog.md` | keep | Stage 1 backlog contract |
| `Backlog.md / execution waves` | `.agent/Backlog.md` | discard | replaced by explicit priority and dependency |
| `Backlog.md / open task rows` | `.agent/Backlog.md` | keep | active work queue |
| `DevLog.md / active format line` | `.agent/DevLog.md` | keep | active schema |
| `DevLog.md / migration-phase notes in header` | `.agent/DevLog.md` | discard | superseded by active schema |
| `GCD.md / all design sections` | `.agent/GCD.md` | keep | active design doc |
| `Systems.md / lane-domain defaults` | `.agent/Systems.md` | discard | process metadata moved out of ownership map |
| `Systems.md / ownership sections` | `.agent/Systems.md` | keep | active ownership map |
| `TDD.md / technical scope` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / architecture principles` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / update loop rules` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / system responsibility map` | `.agent/Systems.md` | move | ownership belongs in Systems |
| `TDD.md / collision and physics policy` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / enemy FSM policy` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / UI/HUD technical notes` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / code quality rules` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / MCP-oriented dev rules` | `.agent/TDD.md` | keep | technical validation policy |
| `TDD.md / input and combat policy` | `.agent/TDD.md` | keep | active technical policy |
| `TDD.md / scope notes` | `.agent/TDD.md` | keep | active doc responsibility note |

## Legacy Files
| Source Section | Target File | Disposition | Verification Source |
|---|---|---|---|
| `legacy/PROJECT.md / Repository Layout` | `.agent/Systems.md` | move | repo structure remains relevant; status text does not |
| `legacy/PROJECT.md / Script Architecture Map` | `.agent/Systems.md` | move | active ownership map |
| `legacy/PROJECT.md / System Deep-Dives` | `.agent/Systems.md`, `.agent/TDD.md` | move | only still-valid architecture and technical wiring |
| `legacy/PROJECT.md / Component Assembly Guide` | `.agent/legacy/PROJECT.md` | archive | active governance no longer carries prefab setup guides |
| `legacy/PROJECT.md / Layer Collision Matrix Rules` | `.agent/TDD.md` | move | still-valid technical policy |
| `legacy/PROJECT.md / Implementation Status` | `.agent/legacy/PROJECT.md` | archive | status stays in `CurrentState.md` only |
| `legacy/PROJECT.md / Agent Rules Quick Reference` | `.agent/legacy/PROJECT.md` | archive | superseded by current governance docs |
| `legacy/AUDIT_v1.md / feature sections` | `.agent/TDD.md`, `.agent/Backlog.md`, `.agent/CurrentState.md` | move | only unresolved still-valid technical findings become policy, risks, or tasks |
| `legacy/AUDIT_v1.md / Summary of Issues & Actions Required` | `.agent/Backlog.md`, `.agent/CurrentState.md` | move | unresolved items only |
| `legacy/AUDIT_v1.md / Changes Made to Project Files` | `.agent/legacy/AUDIT_v1.md` | archive | historical narrative stays archived |
| `legacy/GCD.md / Project Summary, Elevator Pitch, USP` | `.agent/GCD.md` | move | still-valid design framing |
| `legacy/GCD.md / 3C sections` | `.agent/GCD.md` | move | still-valid design intent |
| `legacy/GCD.md / Water Resource System` | `.agent/GCD.md` | move | still-valid design intent |
| `legacy/GCD.md / Win/Fail, Hazards, Enemies, HUD, Level Design` | `.agent/GCD.md` | move | still-valid design intent |
| `legacy/GCD.md / Universe & Story, Graphic Direction, Sound Direction` | `.agent/GCD.md` | discard | omit until actively needed as current design truth |
| `legacy/GCD.md / Implementation Status (Designer View)` | `.agent/legacy/GCD.md` | archive | status belongs in `CurrentState.md` |
| `legacy/TDD.md / Game Presentation, Platforms, Development Stack` | `.agent/TDD.md` | move | still-valid technical scope |
| `legacy/TDD.md / Architecture Principles` | `.agent/TDD.md` | move | still-valid policy |
| `legacy/TDD.md / Script Architecture, Player System, Enemy AI, Combat System` | `.agent/Systems.md`, `.agent/TDD.md` | move | split into ownership vs technical policy |
| `legacy/TDD.md / Physics & Collision` | `.agent/TDD.md` | move | still-valid policy |
| `legacy/TDD.md / Rendering` | `.agent/TDD.md` | discard | omit until rendering policy needs active governance truth |
| `legacy/TDD.md / Code Conventions` | `.agent/TDD.md` | move | still-valid technical policy |
| `legacy/TDD.md / Implementation Status` | `.agent/legacy/TDD.md` | archive | status belongs in `CurrentState.md` |
| `legacy/TDD.md / Project Management` | `.agent/legacy/TDD.md` | archive | superseded by current workflow docs |
| `legacy/TODO.md / Current Sprint` | `.agent/Backlog.md` | discard | old planning board not revived |
| `legacy/TODO.md / Backlog - Future Features` | `.agent/Backlog.md` | move | only unresolved still-valid items become modern backlog rows |
