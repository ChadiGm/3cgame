# Overview
Quick-reference guide to the `.agent/` multi-agent coordination system.
Read this first for a high-level understanding, then dive into individual docs as needed.

---

## File Map

| File | Purpose | Key Rule |
|---|---|---|
| `AGENTS.md` | Entry point & read order | Gateway for any new chat |
| `CurrentState.md` | Accepted truth — subsystem status, risks | Never contains planning |
| `TaskBoard.md` | Open work queue — active tasks only | Done tasks are **removed** |
| `ChangeLog.md` | Append-only outcome history & proof | Never edited, only appended |
| `Workflow.md` | Process rules, roles, lifecycle, contracts | **Only** source of process truth |
| `Architecture.md` | Ownership map & technical policies | Read when touching boundaries |
| `Design.md` | Gameplay pillars & design intent | Read when changing player-facing behavior |
| `legacy/` | Frozen archive of older docs | **Never edited** |

---

## 3-Role Chat Model

```
  PLANNER ──► WORKER ──► REVIEWER
 (main chat)  (spawned)   (main or separate)
```

| Role | Default Chat | Starts When | Ends When |
|---|---|---|---|
| **Planner** | Main chat | Task is new or needs re-scope | Task row is implementation-ready |
| **Worker** | Spawned chat | Task row is defined & assigned | Handoff is written |
| **Reviewer** | Main or separate | Worker handoff exists | Task accepted or returned |

**Transition markers** (use exactly):
`Planner started` · `Planner complete` · `Worker started` · `Worker handoff ready` · `Reviewer started` · `Reviewer accepted` · `Reviewer returned to doing`

---

## Task Lifecycle

```
backlog → doing → review → accepted (removed) or returned (doing)
```

1. Planner creates task row in `TaskBoard.md`
2. Worker moves to `doing`, implements within declared scope/lock
3. Worker writes handoff packet (files, proof, risks, scope check)
4. Reviewer accepts → task removed, logged in `ChangeLog` — or returns to `doing`
5. Truth docs updated only when accepted truth changes

---

## Scope & Lock (Conflict Prevention)

Every task declares:
- **Scope** — exact files/folders the worker can touch
- **Lock** — scene, shared prefab, core script, global asset, or `none`

> Parallel work is safe **only** when scopes AND locks don't overlap.
> Authority-boundary changes (input, death, water API, HUD bootstrap, enemy damage) must be serialized.

---

## Architectural Policies (Agent Safety)

To prevent agents from stepping on each other, systems must communicate via **contracts and events**, not direct references. 

### Latent Anti-Patterns (Avoid These)

| System | Avoid This Pattern | Why It Breaks Multi-Agent Workflows |
|---|---|---|
| **Combat vs Enemy** | Calling `Destroy(enemy)` directly | Combat and Enemy agents fight over destruction logic; locks collide. |
| **Physics vs PowerUps**| Reading scale directly from abstract resource | Size-altering powerups constantly fight the generic resource loop. |
| **UI Data Flow** | Polling (`FindAnyObjectByType`) in `Update()` | Destroys performance; breaks completely in isolated UI Sandbox scenes. |
| **World Interactions** | Deep `GetComponentInParent` chains | Changing player prefab hierarchy silently breaks hazards and pickups. |
| **Audio Systems** | Direct Singleton calls (`AudioManager.Instance`)| Audio agents cannot swap logic without locking every gameplay file. |

### Decoupled Target Architecture

| Policy | Goal | Implementation Strategy |
|---|---|---|
| **Universal Contracts** | Unblock Combat & Enemy Agents | Use Interfaces (`IDamageable`, `IWaterConsumer`). Receivers handle their own state. |
| **Global Event Bus** | Unblock UI & Sequence Agents | Broadcast states; eliminate brittle string lookups (`GameObject.Find`). |
| **State Arbitration** | Unblock Physics & System Agents | Use Arbiters to process competing requests (e.g., Resource size vs PowerUp size). |
| **Economy Decoupling** | Unblock Combat & World Agents | Source dictates cost via Command Pattern; strip hardcoded logic from Resource script. |
| **Audio Event Channel**| Unblock Audio & Gameplay Agents| Replace Singleton calls with fire-and-forget `OnAudioRequested` broadcasts. |

---

## Validation Levels

| Level | Required For | Proof |
|---|---|---|
| **A** | Refactors, comments, dead code | Code-only |
| **B** | Gameplay logic, prefab wiring, bootstrap | Compile / console clean |
| **C** | Movement, combat, enemies, HUD | Play Mode runtime verification |

---

## Launch Contract (New Chat Header)

```
Mode: Planner | Worker | Reviewer
TaskID: <AREA-number>
Transition: <marker>
Start: <entry condition>
End: <exit condition>
Source of truth: .agent/CurrentState.md, .agent/TaskBoard.md,
                 .agent/ChangeLog.md, .agent/Workflow.md
```

---

## 30-Second Bootstrap for Any New Chat

1. `CurrentState.md` — confirm the accepted project truth and risks
2. `TaskBoard.md` — find the task row and its state
3. `ChangeLog.md` — find the latest handoff/review on that `TaskID`
4. `Workflow.md` — confirm role model, markers, and launch contract
5. **Resume from `.agent/`** — don't rely on copied chat context

---

## Worker Handoff Packet (Required Fields)

- `TaskID`
- Changed files
- Exact tests or runtime proof
- Unresolved risks
- Scope confirmation: `in scope` / `out of scope`
- Doc impact: `none` / `CurrentState` / `Architecture` / `Design`
- Transition marker: `Worker handoff ready`

---

## Recommended Future Improvements

The current workflow relies on "honor system" markdown editing. As the project scales, consider these upgrades:

1. **Automated Validation** — Add script tools (e.g., `validate-handoff.sh`) to check `TaskBoard.md` for overlapping locks or missing handoff fields before a task can be accepted.
2. **Rolling Archives** — Prevent `ChangeLog.md` from becoming a massive, token-heavy file by periodically moving old tasks to `.agent/legacy/ChangeLog_Archive.md`.
3. **Launch Contracts via CLI** — Build a command (e.g., `npm run agent:start UI-006`) that automatically reads `TaskBoard` and generates the precise launch prompt for new chats.
4. **Test-Driven Acceptance** — Require Level C tasks to include a passing `job_id` from the Unity Test Runner (via MCP) in the handoff packet, removing self-reported runtime verification.
5. **Ghost Lock Prevention** — Track adding a `Started Date` in `TaskBoard.md` so the Planner or Reviewer can safely revert tasks stuck in `doing` for >48 hours back to `backlog`.
