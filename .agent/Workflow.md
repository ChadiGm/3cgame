# Workflow
The only active process-rules file for agents and human contributors in this repository.

## Task Read Order
Required for every task:
1. `Overview.md` (Quick-reference)
2. `CurrentState.md`
3. `TaskBoard.md`
4. latest relevant lines in `ChangeLog.md`
5. `Workflow.md`

Read when needed:
6. `Architecture.md`
7. `Design.md`

## Role Read Paths
- Main chat / human lead:
  - read the full required order
  - then open `Architecture.md` and `Design.md` only if the task changes system boundaries, gameplay behavior, or accepted intent
- Spawned worker chat:
  - read `CurrentState.md`, `TaskBoard.md`, relevant `ChangeLog.md` lines, and `Workflow.md`
  - open `Architecture.md` only when the task touches shared runtime ownership or technical rules
  - open `Design.md` only when the task changes player-facing behavior or feel
- Reviewer chat:
  - read the task row, the worker handoff, the relevant `ChangeLog.md` lines, and any touched truth docs
- Human contributor:
  - start with `AGENTS.md`
  - use `CurrentState.md` for project truth, `TaskBoard.md` for open work, and `Workflow.md` for process

Read the minimum set needed to act correctly. Do not force every worker through every document on every task.

## Authority Model
- One main chat is the default planning, review, and governance-doc owner.
- Spawned worker chats implement scoped tasks and return handoff packets.
- Spawned worker chats do not edit active governance docs unless the task explicitly includes governance scope.
- `.agent/legacy/` is frozen archive only.

## Chat Role Model
Default chat roles:
- Main chat: `Planner` by default
- Main chat: may also act as `Reviewer` when a separate review chat is not spawned
- Spawned chat: `Worker` by default
- Spawned review chat: optional and must be explicit

Use one active role per chat unless a role switch is explicitly announced in the task prompt, handoff, or review decision.
One chat may switch roles only by stating the new transition marker.

Simple role boundaries:
- `Planner`: creates or reshapes a task, then ends when the task row is implementation-ready
- `Worker`: implements the scoped task, then ends when the handoff is written
- `Reviewer`: accepts or returns the task, then ends when the review decision is logged

Task lifecycle state and chat role are different:
- `TaskBoard.md` tracks task state (`backlog`, `doing`, `review`, `blocked`)
- the chat prompt and handoff track the active role (`Planner`, `Worker`, `Reviewer`)

## Transition Markers
Use these exact transition markers in prompts, handoffs, and review decisions:
- `Planner started`
- `Planner complete`
- `Worker started`
- `Worker handoff ready`
- `Reviewer started`
- `Reviewer accepted`
- `Reviewer returned to doing`

Each role starts when its `... started` marker is stated and ends at its completion marker.
The goal is simple recovery across one or more chats without relying on chat memory alone.

## TaskID Rule
Use a lightweight `TaskID` format:
- format: `<AREA>-<number>`
- examples: `ARCH-203`, `UI-006`, `AI-002`, `GOV-103`

TaskID rules:
- the planner creates the `TaskID`
- use the next available number within that area
- never reuse old IDs
- determine the next number by checking `TaskBoard.md` and recent `ChangeLog.md` entries only

## Launch Contract
Every new task chat should start with the same minimal header:
- `Mode: Planner | Worker | Reviewer`
- `TaskID: <id>`
- `Transition: <marker>`
- `Start: <entry condition>`
- `End: <exit condition>`
- `Source of truth: .agent/CurrentState.md, .agent/TaskBoard.md, .agent/ChangeLog.md, .agent/Workflow.md`

This header is the standard launch contract for new chats.

Default launch shapes:
- Planner chat:
  - `Mode: Planner`
  - `Transition: Planner started`
  - Start when a task is new, unclear, or needs re-scope
  - End when the task row is implementation-ready
- Worker chat:
  - `Mode: Worker`
  - `Transition: Worker started`
  - Start when the task row is defined and assigned
  - End when the worker handoff is written and the task is ready for review
- Reviewer chat:
  - `Mode: Reviewer`
  - `Transition: Reviewer started`
  - Start when a worker handoff exists
  - End when the task is accepted or returned to `doing`

Common worker-launch prompt shape:
```text
Mode: Worker
TaskID: <id>
Transition: Worker started
Start: task row is defined and assigned
End: worker handoff ready
Source of truth: .agent/CurrentState.md, .agent/TaskBoard.md, .agent/ChangeLog.md, .agent/Workflow.md

Read the repo governance docs and the task row first.
Work only within scope and lock.
When complete, write the worker handoff into `.agent` through the required task-row update and one append-only `ChangeLog.md` entry.
```

## Shared Memory Rule
Treat `.agent` as the shared memory between chats.
- Do not rely on manual copy-paste as the default transition path
- Write task state in `TaskBoard.md`
- Write handoff and review history in `ChangeLog.md`
- A new chat should resume by reading the task row in `TaskBoard.md`, the latest matching `TaskID` lines in `ChangeLog.md`, and this workflow file

## Active Document Responsibilities
- `CurrentState.md`: accepted merged truth and active risks only
- `TaskBoard.md`: open task queue only
- `ChangeLog.md`: append-only outcomes and proof only
- `Architecture.md`: ownership map, authority boundaries, and technical architecture rules
- `Design.md`: gameplay and design intent only

## Task Lifecycle
1. Main chat creates or re-scopes a task row before implementation.
2. Owner moves the task to `doing` before editing implementation files.
3. Owner edits only inside the declared `Scope` and `Lock`.
4. Owner validates the task to the required proof level.
5. Worker returns a handoff packet or the main chat appends a `ChangeLog.md` entry directly.
6. Reviewer either accepts the task and removes it from `TaskBoard.md`, or returns it to `doing` with a reason.
7. `CurrentState.md`, `Architecture.md`, and `Design.md` are updated only when accepted truth changes.

Default operating split:
- Main chat:
  - owns planning, review, backlog shaping, and active governance docs
- Spawned worker chat:
  - owns one implementation task with one clear scope and lock set
- Cleanup / migration worker:
  - owns repo cleanup, naming cleanup, doc migration support, or non-gameplay ambiguity reduction

Use task type, not subsystem name, to decide the split:
- gameplay change: one worker owns the full behavior change
- architecture change: one worker owns the authority change end to end
- cleanup change: one worker owns the cleanup without changing behavior

Do not split one authority change, one gameplay behavior change, or one shared lock across multiple workers at the same time.

## Task Board Contract
Required fields:
- `TaskID`
- `Task`
- `Priority`
- `Status`
- `Owner`
- `Scope`
- `Lock`
- `Depends On`
- `Done When`

Optional field:
- `Lane`

Allowed `Status` values:
- `backlog`
- `doing`
- `review`
- `blocked`

`TaskBoard.md` must never contain:
- completed task history
- long review narrative
- architecture explanations

## Scope Rules
- One owner per task.
- Scope should be exact whenever possible.
- Prefer exact files, one folder with one ownership reason, one scene, or one prefab family.
- Avoid broad scopes like `Assets/Scripts/*`, `Assets/Prefabs/*`, or `Assets/Scenes/*` unless the task is explicitly a refactor wave.
- If a task needs files outside its declared scope, stop and re-scope the task row before editing.

## Lock Rules
Every task must declare either `Lock=none` or one or more explicit locks.

Lock categories:
- Scene lock: a specific scene file
- Shared prefab lock: a shared prefab or prefab family
- Core script lock: cross-cutting scripts such as `OneDropController2D`, `OneDropWaterResource2D`, `PlayerCombatController`, `WaterBlobInput2D`, or `GameManager`
- Global asset lock: `ProjectSettings/*`, `Assets/InputSystem_Actions.inputactions`, `Assets/Settings/*`, tag/layer config, globally loaded `Resources` assets, or other globally shared assets

Parallel work is allowed only if:
- scopes do not overlap
- locks do not overlap
- both tasks do not change the same authority boundary

Authority-boundary changes must be serialized. This includes:
- input contract changes
- player death contract changes
- water-resource API changes
- shared HUD bootstrap changes
- enemy damage contract changes

*Architecture Relaxation Rule:*
Once universal contracts (e.g., `IDamageable`, `IWaterConsumer`) and Event Channels are established, agents implementing the *receipt* or *broadcast* of a contract do not need to lock the shared systems. For example, building a new enemy that implements `IDamageable` does not require locking the Combat Controller.

## Worker Handoff Packet
Every worker handoff must include:
- `TaskID`
- changed files
- exact tests or runtime proof
- unresolved risks
- scope confirmation: `in scope` or `out of scope`
- doc impact:
  - `none`
  - `CurrentState`
  - `Architecture`
  - `Design`
- transition marker:
  - normally `Worker handoff ready`

## Validation Levels
- Level A: code-only proof
  - acceptable for internal refactors, comment/doc alignment, or dead-code cleanup
- Level B: compile or console proof
  - required for gameplay logic changes, prefab wiring changes, and bootstrap changes
- Level C: Play Mode or automated test proof
  - required for movement, combat, enemy behavior, HUD bootstrap, and scene bootstrap changes

Each `ChangeLog.md` entry must include:
- `TaskID`
- changed files
- at least one proof
- outcome
- risks
- `ScopeCheck=yes/no`

## Update Rules
Always update:
1. the task's own row in `TaskBoard.md`
2. one appended entry in `ChangeLog.md` when the task is handed to review, returned, or accepted

Standard governance edit exception:
- workers may update the task row in `TaskBoard.md` and append one `ChangeLog.md` entry as part of normal handoff, even when governance docs are otherwise out of scope

Update when accepted truth changed:
1. `CurrentState.md`
2. `Architecture.md`
3. `Design.md`

## Fallback Reviewer Rule
If the main chat is unavailable, one temporary reviewer may update only:
- the task row in `TaskBoard.md`
- one append-only `ChangeLog.md` entry

They must not rewrite `Workflow.md`, `Architecture.md`, or `Design.md`.

## Consistency Checks
After each governance batch:
1. `CurrentState.md` contains no planning section.
2. `TaskBoard.md` contains open work only.
3. `ChangeLog.md` uses the active schema in its header.
4. `Architecture.md` contains architecture truth only, not status.
5. `Design.md` contains design intent only, not implementation status.
6. `.agent/legacy/` remains untouched.
