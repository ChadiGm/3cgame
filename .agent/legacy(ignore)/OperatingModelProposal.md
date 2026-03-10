# ONE DROP Operating Model Proposal v2

This version replaces the first proposal with a stricter and more adoptable model.

The first proposal was directionally correct, but too aggressive about renaming files, too light on Unity-specific collision points, and not explicit enough about migration cost, worker handoff, and unresolved gameplay ownership.

This document is the cleaned proposal.

## Executive Decision

ONE DROP does not need more process. It needs:

- fewer authoritative sources
- one clear documentation owner
- tighter task scopes
- explicit asset locks
- a standard worker handoff
- a small number of hard architectural decisions made now instead of later

The project should not jump straight from the current `.agent` layout to a renamed minimal set in one pass.

The correct path is:

1. tighten ownership and document responsibilities inside the current file set
2. remove derived or low-value docs
3. only rename or merge files after the new behavior is stable

That avoids governance churn while still simplifying the system.

## 1. Project Architecture Summary

### High-level gameplay architecture

The current project has a workable gameplay spine:

- `WaterBlobInput2D` reads player input
- `OneDropController2D` drives traversal and player physics decisions
- `OneDropWaterResource2D` is the central water, damage, refill, and death resource
- `PlayerCombatController` spends water to shoot
- `WaterBlobCharacter2D` builds the soft-body physics body
- `OneDropDeformation2D` handles squash/stretch feedback
- `WaterResourceUI` displays water state
- `WaterHUDAutoSpawner` ensures HUD presence at runtime
- `EnemyController` and FSM states own enemy locomotion/state changes
- `HazardImpact` and `WaterRefillCollectible` apply world pressure and recovery
- `GameManager` handles death/respawn at scene level

The strongest part of the architecture is already the core game idea:

- water is both health and resource
- many systems already route through that one model

That is the right base for this game.

### Current system responsibilities

#### Input
- Primary owner: `WaterBlobInput2D`
- Intended role: read raw input once, cache player intent, expose it to movement and combat
- Current state: mostly correct, but still transitional because consumers retain direct-input fallback

#### Movement
- Primary owner: `OneDropController2D`
- Current role: move, jump, climb, slide, detect ground/walls/ceiling, flip/rotate, spend movement water, coordinate some blob-body behavior
- Current state: functional but too broad

#### Water / survival
- Primary owner: `OneDropWaterResource2D`
- Current role: water amount, action costs, refill, damage, invulnerability, death event, water-changed event
- Current state: strongest gameplay authority in the project

#### Combat
- Primary owner: `PlayerCombatController`
- Current role: shoot/charge logic, cooldowns, pooled bullets, impact logic
- Current state: acceptable for prototype scale, but missing a proper shared damage contract

#### Blob physics and visuals
- `WaterBlobCharacter2D`: soft-body physics
- `OneDropDeformation2D`: visual squash/stretch response
- `WaterBlobMeshRenderer2D`: blob rendering
- Current state: improved split, but still not fully decoupled from controller logic

#### Enemies
- Primary owner: `EnemyController` plus state scripts
- Current role: patrol/chase/attack transitions and simple movement
- Current state: early and incomplete

#### UI
- Primary owner: `WaterResourceUI`
- Support owner: `WaterHUDAutoSpawner`
- Current role: bind to water resource and ensure a usable HUD exists
- Current state: relatively mature compared with the rest of the project because it has smoke-test evidence

#### Lifecycle
- Primary owner: `GameManager`
- Current role: detect player death and reload scene
- Current state: simple and acceptable, but still split across two death models

### Data and control flow

The intended game flow is already visible in code:

1. input enters through `WaterBlobInput2D`
2. movement and combat consume that input
3. movement, combat, hazards, and pickups all route through `OneDropWaterResource2D`
4. water changes feed UI, blob scaling, and death logic
5. `GameManager` reacts to death and resets the scene

This is the architecture worth preserving.

### What is solid

- unified water resource concept
- input centralization direction
- HUD runtime resilience and test coverage
- simple enemy FSM approach
- simple scene-reload lifecycle

### What is transitional

- `OneDropController2D` is still a god-object
- `HealthComponent` still competes with water as a survival authority
- enemy attack ownership is not real yet
- runtime reference lookup is inconsistent
- validation quality is uneven across systems

## 2. Structural Problems And Blind Spots

### A. Gameplay ownership problems

#### `OneDropController2D` is still too broad
It currently owns too many unrelated concerns:

- movement rules
- input fallback
- environment detection
- facing and rotation
- water movement spending
- climb/body coordination
- deformation-facing events and state
- runtime physics material setup

This is the main gameplay script most likely to keep accumulating debt.

#### Water is the real player health, but the project has not fully committed to it
`GameManager` still listens to both `HealthComponent` and `OneDropWaterResource2D`.

That means the project has not actually made the architectural decision it claims to have made.

The project needs one of these decisions:

- `HealthComponent` is removed from the player path and retained only for enemies or future non-water entities
- or `HealthComponent` remains the true survival model and water becomes a separate economy

Given the game concept, the first option is the correct one.

#### Enemy damage is not owned by the enemy system
Right now:

- `EnemyAttackState` mostly logs
- `PlayerHurtbox` destroys enemies directly on touch
- pooled bullets destroy targets directly by layer

This means there is no durable damage contract.

That is fine for a very early prototype. It becomes a blocker as soon as enemies need:

- health
- hit reactions
- armor or resistances
- bosses
- non-lethal interactions

#### Blob separation is only partial
`OneDropDeformation2D` exists, which is good. But `OneDropController2D` still directly manages point-body state and toggles parts of blob behavior.

That is not a clean boundary yet.

### B. Runtime reference and bootstrap problems

#### Lookup policy is duplicated
Different systems each implement their own lookup rules using names, tags, and broad searches.

That already appears in:

- `GameManager`
- `CameraFollow2D`
- `EnemyController`
- `WaterResourceUI`
- `LavaBackgroundFX2D`
- `HazardImpact`

This is manageable in one scene. It will become fragile as soon as player spawning and scene count grow.

#### Bootstrap ownership is not formalized
The project uses runtime bootstrap in several places:

- `WaterHUDAutoSpawner`
- `GameManager`
- `AudioManager`
- editor helper scripts

The code is not wrong, but the project has no rule for which systems are allowed to auto-create, auto-find, or persist.

Without that rule, new bootstrap scripts will appear ad hoc.

### C. Repo hygiene problems the first proposal underweighted

These are real sources of future confusion:

- stale `OneDropAttack2D` reference in `OneDropWaterResource2D`
- compatibility script `Assets/Scripts/bullets/bullet.cs`
- old `Assets/enemy/*` content beside active `Assets/Scripts/Enemies/*`
- temp files at repo root such as `tmpOldBlob.cs`
- mixed naming and folder ownership conventions

Even with a better operating model, these artifacts will keep confusing agents and humans about what is active.

### D. Validation problems

UI has real Play Mode coverage. Most other systems do not.

That means the project currently has:

- good governance detail
- weaker runtime proof than the docs imply

That imbalance will cause false confidence if not corrected.

### E. Unity-specific asset collision problems

The first proposal correctly mentioned scenes and prefabs, but that is not enough.

For this repo, shared-edit collision also includes:

- `ProjectSettings/*`
- `Assets/InputSystem_Actions.inputactions`
- URP assets and render profiles in `Assets/Settings/*`
- tag/layer configuration
- `Resources` assets
- shared materials
- globally used prefabs
- singletons and bootstrap entrypoints

Without treating those as lockable assets, "non-overlapping task scope" is not actually safe.

## 3. Revised Governance Proposal

### Core rule

Use one main coordination agent as:

- planner
- reviewer
- documentation owner
- merge-truth owner

Worker agents should implement scoped tasks and return handoff packets.

Worker agents should not normally edit `.agent` docs.

This is the single most important simplification.

### Do not rename the active docs immediately

The repo already depends on the current file names and read order in:

- [AGENTS.md](c:/Users/gmira/Documents/GitRepos/3cgame/AGENTS.md)
- [.agent/AGENTS.md](c:/Users/gmira/Documents/GitRepos/3cgame/.agent/AGENTS.md)

So phase 1 should keep the current names and tighten responsibilities first.

### Phase 1 authoritative file set

Keep active:

- `AGENTS.md`
- `Workflow.md`
- `Backlog.md`
- `CurrentState.md`
- `DevLog.md`
- `Systems.md`
- `GCD.md`
- `TDD.md`

Demote immediately:

- `ContinuationPrompts.md`

Reason:

- `ContinuationPrompts.md` is derived from backlog and current priorities
- it creates drift without adding durable truth

### Phase 1 responsibility cleanup

#### `AGENTS.md`
Purpose:
- entrypoint
- read order
- short operating contract

Should not contain:
- duplicated workflow rules
- status tables
- architecture details

#### `Workflow.md`
Purpose:
- the only process rules document
- task lifecycle
- scope rules
- lock rules
- handoff/review rules

Should not contain:
- architecture ownership
- design intent
- project status

#### `Backlog.md`
Purpose:
- open tasks only
- current owner
- exact scope
- lock
- dependency
- done criteria

Should not contain:
- completed narrative
- architecture commentary

#### `CurrentState.md`
Purpose:
- accepted truth only
- active risks
- what is verified versus still pending

Should not contain:
- next priorities
- execution plans
- task ordering

Those belong in `Backlog.md`.

#### `DevLog.md`
Purpose:
- concise append-only outcome history
- proof summary
- review result

Should not contain:
- governance redesign narrative every time docs change
- planning notes

#### `Systems.md`
Purpose:
- current system ownership map
- authority boundaries
- runtime dependency map

Should not contain:
- status
- task plans

#### `GCD.md`
Purpose:
- gameplay and player experience intent

#### `TDD.md`
Purpose:
- technical constraints and implementation policy

### Phase 2 optional simplification

Only after phase 1 is stable:

- merge `Systems.md` + `TDD.md` into `Architecture.md`
- rename `GCD.md` to `Design.md`
- possibly fold `Workflow.md` essentials into `.agent/AGENTS.md`

Do not do this before the team has already stopped using the old files as overlapping authorities.

## 4. Revised Multi-Agent Operating Model

### Main agent role

The main agent owns:

- task creation
- scope definition
- lock assignment
- dependency ordering
- review and acceptance
- `.agent` updates
- architecture consistency decisions

By default, only the main agent writes:

- `Backlog.md`
- `CurrentState.md`
- `DevLog.md`
- `Workflow.md`
- `Systems.md`
- `GCD.md`
- `TDD.md`
- `.agent/AGENTS.md`

This does create a coordination bottleneck, but that is still cheaper than letting many workers race on governance files.

### Worker agent role

The worker owns:

- implementation inside the assigned scope
- validation inside the assigned scope
- reporting results to the main agent

The worker should not modify project governance docs unless the task explicitly says so.

### Required worker handoff packet

Every worker handoff should include:

- `TaskID`
- changed files
- exact tests or runtime proof
- unresolved risks
- scope confirmation: `in scope` or `out of scope`
- doc impact:
  - `none`
  - `CurrentState`
  - `Systems`
  - `GCD`
  - `TDD`

This gives the main agent enough information to update the docs without requiring the worker to touch them.

### Fallback if the main agent is unavailable

If the main agent is unavailable, one temporary reviewer may update only:

- the task row in `Backlog.md`
- one append-only `DevLog.md` entry

They should not rewrite architecture or workflow docs.

That prevents total workflow stall without reopening full governance overlap.

## 5. Scope And Lock Rules

### Scope rule

Task scope must be exact whenever possible.

Prefer:

- exact files
- one folder with a single ownership reason
- one scene
- one prefab family

Avoid:

- `Assets/Scripts/*`
- `Assets/Prefabs/*`
- `Assets/Scenes/*`

unless the task is explicitly a refactor wave.

### Lock rule

Locks are more important than lanes.

Every task should declare either:

- `Lock=none`
- or one or more explicit locks

### Lock categories

#### Scene locks
- `Assets/Scenes/SampleScene.unity`

#### Shared prefab locks
- any prefab used by multiple scenes or multiple systems

#### Core script locks
- cross-cutting scripts such as:
  - `OneDropController2D`
  - `OneDropWaterResource2D`
  - `PlayerCombatController`
  - `WaterBlobInput2D`
  - `GameManager`

#### Global asset locks
- `ProjectSettings/*`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Settings/*`
- tag/layer changes
- `Resources` assets used globally

### Parallel safety rule

Parallel work is allowed only if:

- scopes do not overlap
- locks do not overlap
- both tasks do not change the same authority boundary

Authority-boundary changes must be serialized.

Examples:

- input contract changes
- player death contract changes
- water-resource API changes
- shared HUD bootstrap changes
- enemy damage contract changes

## 6. Runtime Reference Standard

The first proposal said lookup was a problem. This version defines the rule.

### Preferred order

1. serialized reference on prefab or scene object
2. explicit runtime binding from an owner/bootstrap system
3. one controlled fallback lookup in a bootstrap layer

### Not allowed as normal gameplay behavior

Do not let ordinary gameplay systems repeatedly do ad hoc:

- `GameObject.Find(...)`
- `FindWithTag(...)`
- `FindAnyObjectByType(...)`

That should be limited to bootstrap or recovery paths only.

### Practical repo rule

Only these categories may do fallback lookup:

- bootstrap systems
- editor utilities
- temporary scene-compatibility bridges during migration

When a fallback lookup exists, it should be:

- logged once if it fails
- cached if it succeeds
- treated as a migration path, not the final architecture

## 7. Immediate Architecture Decisions To Make Now

These are not optional cleanup items. They directly affect future work quality.

### Decision 1: player survival authority

Adopt this rule:

- `OneDropWaterResource2D` is the player survival authority
- `HealthComponent` is not used for the player path

`HealthComponent` may remain only if repurposed for non-player actors.

### Decision 2: enemy damage contract

Add a minimal damage receiver contract before enemy complexity increases.

It does not need to be overbuilt.

A simple interface or component-level contract is enough:

- projectile/touch systems request damage
- enemy-owned health or death component decides what happens

This is the minimum required to stop player systems from owning enemy destruction directly.

### Decision 3: controller decomposition target

Do not split `OneDropController2D` immediately into many scripts.

Do define the target split now:

- input authority stays in `WaterBlobInput2D`
- movement/traversal remains in `OneDropController2D`
- environment sensing can eventually move to a dedicated sensor/helper
- blob-body coordination should move away from controller over time

This avoids random partial extractions.

### Decision 4: HUD authoring path

Keep:

- one preferred authored HUD prefab path
- one procedural fallback path

Remove:

- extra setup paths that look authoritative but are not

For this repo, `WaterBlobUISetup` reads as legacy/editor-helper only, not a core runtime path.

## 8. Validation And Evidence Rules

The project currently has stronger governance detail than runtime evidence in some areas.

That should be corrected.

### Minimum evidence levels

#### Level A: code-only proof
Acceptable for:

- internal refactors
- comment/doc alignment
- dead-code cleanup

#### Level B: compile or console proof
Required for:

- gameplay logic changes
- prefab wiring changes
- bootstrap changes

#### Level C: Play Mode or automated test proof
Required for:

- player movement changes
- combat changes
- enemy behavior changes
- UI bootstrap changes
- scene bootstrap changes

### Logging discipline

Console logs are currently noisy in places like `OneDropWaterResource2D`.

If logs remain this verbose, proof-by-console becomes low quality.

So the team should treat "reduce routine debug noise" as a technical hygiene task, not a cosmetic one.

## 9. Recommended Final Operating Rules

### Do this

- keep one main agent as planner, reviewer, and documentation owner
- make workers return handoff packets instead of editing governance docs
- keep current doc filenames in phase 1, but narrow their purpose
- remove planning content from `CurrentState.md`
- remove `ContinuationPrompts.md` from active governance
- declare explicit locks for scenes, core scripts, global assets, and shared prefabs
- serialize authority-boundary changes
- standardize runtime references around serialized refs first, bootstrap fallback second
- decide now that player survival is owned by `OneDropWaterResource2D`
- add a small enemy damage contract before more enemy features land
- clean repo leftovers that blur active ownership

### Do not do this

- do not rename the whole `.agent` structure in one move
- do not let many workers update `.agent` files by default
- do not keep `CurrentState.md` as both truth and planning board
- do not rely on scope text alone without explicit locks
- do not let ad hoc `Find(...)` calls spread into normal gameplay systems
- do not keep direct enemy destruction as the long-term combat contract
- do not split `OneDropController2D` into many files without a defined target boundary

## 10. Recommended Minimal File Set

### Phase 1 target set

These should remain active after cleanup, using current names:

| File | Purpose | Owner | Update Timing | Must Not Store |
|---|---|---|---|---|
| `.agent/AGENTS.md` | entrypoint and short read-order contract | main agent | only when workflow entry rules change | status tables, architecture detail, planning text |
| `.agent/Workflow.md` | the only process-rules file | main agent | when task lifecycle or lock policy changes | architecture map, design intent, project status |
| `.agent/Backlog.md` | open tasks only | main agent | when planning, assigning, blocking, or closing work | completed history, architecture commentary |
| `.agent/CurrentState.md` | accepted merged truth and risks | main agent | only after accepted truth changes | task ordering, next-step plan |
| `.agent/DevLog.md` | concise append-only outcomes and proof | main agent | on acceptance, rejection, or important review return | planning notes, large governance essays |
| `.agent/Systems.md` | ownership map and system boundaries | main agent | when accepted architecture changes land | status, backlog data |
| `.agent/GCD.md` | gameplay/design intent | main agent or human design owner | when design intent changes | implementation detail, task log |
| `.agent/TDD.md` | technical policy and constraints | main agent | when technical policy changes | project status, task queue |

### Phase 2 optional set

Only after phase 1 stabilizes:

| New File | Replaces | Notes |
|---|---|---|
| `.agent/Architecture.md` | `Systems.md` + `TDD.md` | merge only after both docs stop overlapping in practice |
| `.agent/Design.md` | `GCD.md` | rename only if the team wants simpler wording |

### Files to demote or archive

- `.agent/ContinuationPrompts.md`
- any legacy governance file not used as active truth

## 11. Adoption Plan

### Step 1
Clean governance responsibilities without renaming files.

Actions:

- strip planning content from `CurrentState.md`
- make `Workflow.md` the only process-rules file
- demote `ContinuationPrompts.md`
- add worker handoff format to `Workflow.md`
- add lock taxonomy to `Workflow.md`

### Step 2
Clean repo ambiguity.

Actions:

- remove or quarantine dead compatibility scripts
- remove stale references such as `OneDropAttack2D` mentions
- identify legacy folders versus active folders
- mark or delete temp root files

### Step 3
Make the hard gameplay ownership decisions.

Actions:

- declare `OneDropWaterResource2D` as player survival authority
- define minimal enemy damage contract
- define controller decomposition target

### Step 4
Only after the above is stable, consider doc merge/rename.

That is the pragmatic path.

## Final Decision

The project is still small enough to simplify aggressively, but not so small that it can afford governance churn or ambiguous ownership.

The right cleaned model is:

- one main agent owns planning, review, and docs
- workers implement only scoped tasks
- current docs are tightened first, renamed later
- locks become more important than lanes
- player survival authority is explicitly water-based
- enemy damage gets a minimal shared contract before more features land
- runtime lookup is standardized instead of tolerated

That is the operating model most likely to stay readable as the project grows.
