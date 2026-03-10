# ONE DROP — Technical Design Document (TDD)
**Version:** 1.0 (15/02/2026) · **v2.0 Update:** 06/03/2026 — Chadi Gmira
**Team:** Abdelghafour Moumad (Programmer) · Mohamed Chadi Gmira (Designer & Artist)

> **For agents:** This document defines the rules for writing code in this project. Cross-reference [`PROJECT.md`](./PROJECT.md) for the live script map and implementation status.

---

## 1. Game Presentation

**ONE DROP** is a 2D physics-based precision platformer built in Unity 6. The player controls a living water drop navigating a hostile volcanic environment. The core technical challenge is making movement feel **organic** (soft-body simulation, squash & stretch) while remaining **precise** (BoxCast detection, frame-consistent input).

**Core premise — technically:** The player blob is a soft-body physics simulation (`WaterBlobCharacter2D`) whose visual shape is driven by a ring of `Rigidbody2D` spring-joint points. Movement control (`OneDropController2D`) is entirely separate from this simulation. The blob's "mass" is also its ammo and health (`OneDropWaterResource2D`).

**Key scripts at a glance:**

| Script | Purpose |
|---|---|
| `OneDropController2D` | All player physics: move, jump, wall-climb, slide, deformation |
| `WaterBlobCharacter2D` | Soft-body spring simulation (visual physics only) |
| `OneDropWaterResource2D` | Water as shared economy: life + ammo |
| `PlayerCombatController` | Shooting — spawns, configures, and fires bullets |
| `PlayerHurtbox` | Receives contact damage from enemies |
| `EnemyController` | Enemy root — wires FSM states at runtime |
| `StateMachine` + `State` | Generic FSM used by all AI |
| `HazardImpact` | Continuous environmental damage (lava) |
| `CameraFollow2D` | Smooth follow camera |

---

## 2. Platforms & Hardware Specifications

| Spec | Minimum | Recommended |
|---|---|---|
| **CPU** | i5 4th Gen | i7 / Ryzen 5+ |
| **RAM** | 8 GB | 16 GB |
| **GPU** | Intel UHD / GTX 750 | GTX 1060+ |
| **OS** | Windows 10 | Windows 10/11 |
| **Target** | 60 FPS @ 1080p | 60 FPS @ 1080p |
| **Frame budget** | < 16.67 ms | < 16.67 ms |

---

## 3. Development Stack

| Tool | Version |
|---|---|
| **Engine** | Unity 6 — 6000.x |
| **Render Pipeline** | URP 17.3.0 — 2D Renderer |
| **IDE** | Visual Studio 2022 |
| **Input System** | Unity Input System 1.17.0 (`ENABLE_INPUT_SYSTEM` defined) |
| **UI** | Unity uGUI 2.0.0 |
| **Version Control** | Git / GitHub |
| **Planning** | Notion (backlog) · Discord (sync) |
| **Audio** | WAV format for SFX · OGG planned for music |

---

## 4. Architecture Principles

### 4.1 Single Responsibility (No God Scripts)
Every script has one well-defined job. The player character is assembled from cooperating components, not one monolithic class:
- `WaterBlobInput2D` — reads input only
- `OneDropController2D` — physics movement only
- `WaterBlobCharacter2D` — soft-body simulation only
- `PlayerCombatController` — shooting only
- `PlayerHurtbox` — damage reception only
- `OneDropWaterResource2D` — resource tracking only

### 4.2 Physics / Visual Decoupling
`Rigidbody2D` force application and any code that reads or modifies physics state must live in `FixedUpdate()`. Visual reactions (scaling, sprite flipping, mesh deformation driven by cached velocity values) are applied in `Update()` or, when they read velocity directly, still inside `FixedUpdate()` — but must **never apply forces themselves**.

### 4.3 Update Loop Rules

| Loop | Allowed content |
|---|---|
| `Update()` | Input reading, timer decrement, visual-only logic (non-physics) |
| `FixedUpdate()` | Force application, velocity setting, `Raycast`/`BoxCast` calls |
| `LateUpdate()` | Camera follow only (`CameraFollow2D`) |

### 3.2 Resources (`OneDropWaterResource2D`)
Integrated survival and ammo system.
- **Unified Logic:** Acts as Health (damage logic + I-frames) and Ammo (shoot cost).
- **Physical Integration:** Drives `radius` scaling in `WaterBlobCharacter2D`.
- **Stat Integration:** Drives `speed` and `jumpPower` multipliers in `OneDropController2D`.
- **API:**
    - `TakeDamage(float amount, bool ignoreInvulnerability = false)`: Deducts water. Hazards can bypass I-frames for continuous drain.
    - `ConsumeShoot()`, `ConsumeJump()`, `ConsumeSlide()`: Logic-gated reduction.
    - `OnWaterChanged(float current, float max)`: Core event for UI and simulation.
n other scripts.

### 4.4 FSM for All AI
Any entity with more than one behavior state **must** use the `StateMachine` + `State` pattern in `WaterBlob.AI`. No boolean flag chains (`if (isWaiting)`, `if (isPatrolling)`) in `Update()`.

### 4.5 Water Resource as Single Source of Truth
All actions that cost water (shoot, jump, slide, damage, move) **must** route through `OneDropWaterResource2D`. Never subtract resources inline in other scripts.

---

## 5. Script Architecture

> Full details with API signatures → [`PROJECT.md §2`](./PROJECT.md)

### Folder Structure
```
Assets/Scripts/
├── Core/        → State.cs, StateMachine.cs, HealthComponent.cs
├── Player/      → PlayerCombatController.cs, PlayerHurtbox.cs
├── WaterBlob/   → OneDropController2D.cs, WaterBlobCharacter2D.cs,
│                   WaterBlobInput2D.cs, OneDropWaterResource2D.cs,
│                   CameraFollow2D.cs, WaterBlobMeshRenderer2D.cs,
│                   WaterBlobGooglyEyes2D.cs, LavaBackgroundFX2D.cs,
│                   LavaPlatformVisual2D.cs
├── World/       → HazardImpact.cs
├── Enemies/     → EnemyController.cs, EnemyPatrolState.cs, EnemyWaitState.cs
└── bullets/     → bullet.cs, BulletWaterBlobVisual.cs,
                    ShootMuzzleEffect.cs, WaterSplashEffect.cs
```

---

## 6. Player System

### 6.1 Input — `WaterBlobInput2D`
Decoupled input reader. Publishes smoothed values so the controller never touches `Keyboard`/`Gamepad` APIs directly.

| Input | Keyboard | Gamepad |
|---|---|---|
| Move | A/D · Arrow Keys | Left Stick |
| Jump | **Space** / **Up Arrow / W** (Double-tap while climbing) | South Button (A) |
| Shoot | **Space** | Right Shoulder / West Button |

- Horizontal input smoothed: `riseRate = 12`, `fallRate = 18`
- Jump uses **consume pattern**: `ConsumeJumpPressed()` returns true once per press (prevents ghost inputs)

### 6.2 Movement — `OneDropController2D` (831 lines)
All player physics is here. Detection uses `BoxCast` with layer masks.

**`FixedUpdate()` execution order:**
1. `CheckGround()` → `BoxCast` down → sets `isGrounded`
2. `CheckWall()` → `BoxCast` left/right → sets `isOnWall`, `wallNormal`
3. `CheckCeiling()` → `BoxCast` up → sets `isCeilingAbove`
4. `UpdateClimbState()` → enters wall-climb + applies `climbEntryBoost` impulse
5. `ProcessSlideInput()` → double-tap detection → `StartSlide()`
6. Movement application: `ApplyHorizontalMovement()` / `ApplySlideMovement()` / `ApplyClimbMovement()`
    - `ApplyClimbMovement` (v3.1): `velocity.y = (inputY * climbSpeed) + flow.y`. Momentum is additive.
7. Jump resolution: `DoGroundJump()` / `DoWallJump()`
8. `ApplyRotation()` → tilt toward surface normal
9. `ApplyVisualFlip()` → mirror `localScale.x`
10. `ApplySoftBodyDeformation()` → squash & stretch from velocity

### 6.3 Combat & Shooting — `PlayerCombatController`
- **Normal Shot**: `shootCost = 5`.
- **Charged Shot**: `chargeShootCost = 15`. Hold **Space** for 0.8s.
  - Bullet scale x2.5, speed +50%.
  - Visuals: Gun pulses during charge; resets on release.
- **AudioManager**: Plays `Shoot` or `ChargeShoot` clips via singleton.

### 6.4 Soft-Body — `WaterBlobCharacter2D`
A ring of `Rigidbody2D` point masses, each connected to a central core body via `SpringJoint2D`. Ring neighbors are also spring-connected. This gives the blob its organic feel without any custom physics solver.

- `RebuildBlob()` reconstructs the ring at runtime
- `IgnoreInternalCollisions()` prevents ring nodes from colliding with each other
- Pure simulation — zero input or control logic here

### 6.4 Water Resource Economy

| Action | Cost | Fails if insufficient? |
|---|---|---|
| Shoot water bullet | 5 | Yes — `ConsumeShoot()` returns `false`, blocks shot |
| Take enemy damage | 10 | No — always deducts |
| Moving (per second) | 2 | No |
| Slide dash | 15 | No |
| Jump | 12 | No |
| Refill (safe zone) | → 100 | N/A |

---

## 7. Enemy AI System

### 7.1 FSM Architecture
All enemies use `StateMachine` (namespace `WaterBlob.AI`). States are `MonoBehaviour` components added dynamically — never manually placed in the Inspector.

```
State.cs (abstract)
  ├── Enter(StateMachine fsm)     → called when entering state
  ├── LogicUpdate()               → called by StateMachine.Update()
  ├── PhysicsUpdate()             → called by StateMachine.FixedUpdate()
  └── Exit()                      → called when leaving state
```

### 7.2 Enemy Setup — `EnemyController`
`Awake()` dynamically adds `EnemyPatrolState` + `EnemyWaitState`. `Start()` calls `StateMachine.Initialize(PatrolState)`.

**Inspector parameters:**

| Field | Default | Description |
|---|---|---|
| `speed` | 3 | Movement speed (units/sec) |
| `patrolDistance` | 5 | Max distance from spawn X before turning |
| `waitTime` | 0.5 | Seconds paused at each endpoint |
| `wallCheckDistance` | 0.2 | Raycast distance for wall detection |
| `edgeCheckDistance` | 0.5 | Raycast distance for ledge detection |
| `groundCheckOffset` | (0.3, -0.5) | Local offset for ground check ray origin |

### 7.3 Patrol State — `EnemyPatrolState`
- **Behavior**: Horizontal movement with Raycast wall/ledge detection.
- **Transition**: Switches to `WaitState` on obstacle OR `ChaseState` if player detected within `detectionRange`.

### 7.4 Wait State — `EnemyWaitState`
- **Behavior**: Pauses at patrol endpoints. Looks for player.
- **Transition**: Switches to `PatrolState` after `waitTime` OR `ChaseState` if player detected.

### 7.5 Chase State — `EnemyChaseState`
- **Behavior**: Pursues player at $1.5 \times speed$.
- **Transition**: Loses player if distance $> 1.5 \times detectionRange$ (returns to Wait). Switches to `AttackState` if close.

### 7.6 Attack State — `EnemyAttackState`
- **Behavior**: Combat stance.
- **Transition**: Switches back to `ChaseState` if player moves away.

---

## 8. Combat System

### 8.1 Shooting — `PlayerCombatController`
- Fires horizontally based on `transform.localScale.x` (positive = right, negative = left)
- Rate-limited by `shootCooldown = 0.12s`
- Consumes `waterResource.ConsumeShoot()` — aborts if `false`
- Bullet velocity: `bulletSpeed = 20`, `gravityScale = 0` (zero-gravity projectile)
- Lifetime: `bulletLifetime = 4s`

**Bullet layer configuration (3-mask system):**

| Mask | Behavior |
|---|---|
| `bulletAttackableLayers` | Hit → destroy target + spawn vaporization FX |
| `bulletNonDestructibleLayers` | Hit → bullet dies, target survives |
| `bulletIgnoredLayers` | Bullet passes through — no interaction |

### 8.2 Bullet Runtime Handler — `BulletRuntimeHandler`
Internal class (inside `PlayerCombatController.cs`). Added at spawn via `ConfigureBulletRuntime()`. Handles all bullet collision logic via `OnCollisionEnter2D`. The legacy `bullet.cs` script (in `Scripts/bullets/`) handles **eyes only** — no collision code goes there.

### 8.3 Player Damage — `PlayerHurtbox`
Listens on both `OnCollisionEnter2D` and `OnTriggerEnter2D`. On enemy contact:
1. Destroys the enemy `GameObject`
2. `HealthComponent.TakeDamage(1)` → starts i-frame timer
3. `WaterResource.ConsumeDamage()` → −10 water
4. **Hazard Detection (v2.1):** `HazardImpact.cs` uses `GetComponentInParent/Children` and `transform.root` to robustly find the player through soft-body point colliders.

---

## 9. Physics & Collision

### 9.1 Layer Collision Matrix
Configure in **Project Settings → Physics 2D → Layer Collision Matrix**.

| Pair | Rule |
|---|---|
| Bullet ↔ Player | Ignore (own shots) |
| Bullet ↔ WaterResource | Ignore |
| Bullet ↔ Enemy | Collide → attackable |
| Bullet ↔ Ground/Wall | Collide → non-destructible |
| Soft-body nodes | Ignore each other (handled by `WaterBlobCharacter2D.IgnoreInternalCollisions()`) |

### 9.2 Zero Friction
`OneDropController2D` auto-creates a zero-friction `PhysicsMaterial2D` at runtime (`CreateAndApplyZeroFrictionMaterial()`) to prevent wall-stick artifacts.

---

## 10. Rendering

- **Pipeline:** URP 2D Renderer with runtime materials
- **Post-processing:** Global volumes exist; centralized profile not yet set up (backlog)
- **Projectile pooling:** Not yet implemented — bullets use `Instantiate` + `Destroy` with lifetime
- **Lava FX:** `LavaBackgroundFX2D` (parallax) + `LavaPlatformVisual2D` (surface animation)

---

## 11. Code Conventions

### 11.1 Naming

| Element | Convention | Example |
|---|---|---|
| Classes & Methods | `PascalCase` | `EnemyPatrolState`, `ApplyJump()` |
| Private fields | `camelCase` or `_camelCase` | `moveSpeed`, `_jumpForce` |
| `[SerializeField]` | `camelCase` | `[SerializeField] float jumpForce` |
| Constants | `UPPER_CASE` | `MAX_JUMP_COUNT` |
| Namespaces | `WaterBlob`, `WaterBlob.AI`, `WaterBlob.Enemies` | — |

### 11.2 Best Practices
- Cache `GetComponent<T>()` in `Awake()` / `Start()` — never in `Update()` loops
- No `Find()` in runtime loops
- Write XML `<summary>` docs for all `public` methods
- Use `[DisallowMultipleComponent]` on components that must be unique per GameObject
- Use `[RequireComponent(...)]` to declare hard dependencies

### 11.3 Script Header Template
```csharp
/// <summary>
/// Author: [Name]
/// Date: [DD/MM/YYYY]
/// Purpose: [Single-sentence description of what this script is responsible for]
/// </summary>
```

---

## 12. Implementation Status

> See [`PROJECT.md §6`](./PROJECT.md) for the full table.

**Sprint complete (✅):** Movement, jumping, wall-climb (kinetic), slide-dash, soft-body, camera, water economy, health, shooting, charged shot, player damage, enemy patrol/chase/attack FSM, lava hazards (I-frame bypass).
**In progress (🔄):** Collision matrix cleanup (TODO #4), `OneDropController2D` visual decoupling (TODO #1).
**Backlog (❌):** Water collectibles, Game Over/respawn loop, post-processing.

---

## 13. Project Management

| Topic | Detail |
|---|---|
| **Methodology** | Scrum — 2-week sprints |
| **Sync** | Discord daily |
| **Async** | GitHub PRs · Notion backlog |
| **Definition of Done** | Play mode verified · No console errors · Code review (min. 1) · Inspector params exposed |
| **Risk mitigation** | Fallback simple assets for blocked features · Weekly design sync to prevent vision drift |
