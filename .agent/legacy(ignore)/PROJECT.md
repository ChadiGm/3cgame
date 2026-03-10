# ONE DROP — Project Global Structure
**Version:** 2.0 — 06/03/2026
**Authors:** Abdelghafour Moumad (Programmer) · Mohamed Chadi Gmira (Designer & Artist)
**Engine:** Unity 6 (6000.x) · URP 17.3.0 · New Input System 1.17.0

> **Usage — Agents:** Read this file before modifying any script. It is the single source of truth for architecture, component wiring, and implementation state.
> **Usage — Designer:** Use this to understand what is currently playable, what is in progress, and what the rules are for each system.

---

## 1. Repository Layout

```
3cgame/
├── .agent/                    ← AI agent context files (YOU ARE HERE)
│   ├── PROJECT.md             ← Global structure (this file)
│   ├── GCD.md                 ← Game Concept Document
│   ├── TDD.md                 ← Technical Design Document
│   ├── AGENTS.md              ← Agent execution rules & conventions
│   └── TODO.md                ← Sprint backlog
│
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              ← Shared base classes (FSM, Health)
│   │   ├── Player/            ← Player combat & hurtbox
│   │   ├── WaterBlob/         ← Player controller, soft-body, camera, lava FX
│   │   ├── Enemies/           ← Enemy FSM states & controller
│   │   └── bullets/           ← Projectile visuals & effects
│   │
│   ├── Prefabs/               ← Prefab assets (Enemies, Projectiles, VFX)
│   ├── Scenes/                ← Unity scenes
│   ├── Materials/             ← URP materials
│   ├── Settings/              ← URP renderer data, post-processing profiles
│   ├── Shaders/               ← Custom shaders
│   ├── enemy/                 ← Enemy art assets
│   └── InputSystem_Actions.inputactions  ← New Input System asset
│
├── Document/
│   ├── GCD.pdf                ← Original Game Concept PDF (v1.0)
│   ├── TDD.pdf                ← Original Technical Design PDF (v1.0)
│   └── GCD_ONE_DROP_Modified.tex ← LaTeX source for GCD
│
└── tmpOldBlob.cs              ← Legacy archive (DO NOT reference)
```

---

## 2. Script Architecture Map

All scripts are in the `Assets/Scripts/` tree. Namespace `WaterBlob` is the project root namespace.

### 2.1 Core — `Assets/Scripts/Core/`
*Shared abstract systems used by both player and enemies.*

| Script | Namespace | Role | Key API |
|---|---|---|---|
| `State.cs` | `WaterBlob.AI` | Abstract FSM state base class | `Enter(StateMachine)`, `LogicUpdate()`, `PhysicsUpdate()`, `Exit()` |
| `StateMachine.cs` | `WaterBlob.AI` | FSM manager. Calls `LogicUpdate()` in `Update()`, `PhysicsUpdate()` in `FixedUpdate()` | `Initialize(State)`, `ChangeState(State)`, `CurrentState` |
| `OneDropWaterResource2D.cs` | `WaterBlob` | **Lifeblood System**. Handles HP, Water mass, I-Frames, and Stat scaling. Reactive event-driven API | `TakeDamage(float)`, `ConsumeShoot()`, `Refill()`, `OnWaterChanged`, `OnTakeDamage` |
| `GameManager.cs` | `WaterBlob` | **Global lifecycle manager**. Handles player death and scene reloads | `TriggerRespawn()`, `Instance` |
| `HazardImpact.cs` | `WaterBlob` | **Hazard component**. Environmental damage (lava) and knockback. Supports **I-frame bypass** for continuous drain. | `ProcessHazard()` |
| `WaterResourceUI.cs` | `WaterBlob` | **HUD component**. Visualizes water level with **Masked Liquid** rising effect and damage shake | `SetupRuntime(Image, Image, RectTransform)` |
| `WaterHUDAutoSpawner.cs` | `WaterBlob` | **Auto-Setup utility**. Procedurally builds the HUD hierarchy at runtime if missing | `RuntimeInitializeOnLoad` |

### 2.2 WaterBlob — `Assets/Scripts/WaterBlob/`
*Player movement, physics, visuals, camera, environment.*

| Script | Namespace | Role | Key API |
|---|---|---|---|
| `WaterBlobInput2D.cs` | `WaterBlob` | Input abstraction. Keyboard + Gamepad. Smoothed X axis | `Move (Vector2)`, `JumpHeld (bool)`, `ConsumeJumpPressed()` |
| `OneDropController2D.cs` | `WaterBlob` | **Main player controller**. Handles physics movement, jump, wall-climb, slide-dash, and water consumption. Signals visuals via events | All movement methods — see §3.1 |
| `OneDropDeformation2D.cs` | `WaterBlob` | **Procedural visual deformation**. Handles squash, stretch, and momentum-lean. Decoupled from physics | `ApplySoftBodyDeformation()` |
| `WaterBlobCharacter2D.cs` | `WaterBlob` | Soft-body **physics simulation**. Ring of `Rigidbody2D` points connected by `SpringJoint2D` to a core body | `RebuildBlob()`, `ApplyRadialStabilization()`, `IgnoreInternalCollisions()` |
| `WaterBlobMeshRenderer2D.cs` | `WaterBlob` | Renders the blob shape as a procedural mesh | — |
| `WaterBlobGooglyEyes2D.cs` | `WaterBlob` | Velocity-driven animated googly eyes. Purely visual | — |
| `OneDropWaterResource2D.cs` | `WaterBlob` | Water mass economy. Tracks `currentAmount` | `ConsumeShoot()`, `ConsumeDamage()`, `ConsumeMove()`, `ConsumeSlide()`, `ConsumeJump()`, `Refill()`, `Current` |
| `CameraFollow2D.cs` | `WaterBlob` | Smooth follow camera in `LateUpdate()`. Optional X/Y clamping | Inspector: `target`, `offset`, `smoothTime`, `clampX/Y`, `xLimits`, `yLimits` |
| `LavaBackgroundFX2D.cs` | `WaterBlob` | Lava parallax background FX | — |
| `LavaPlatformVisual2D.cs` | `WaterBlob` | Lava platform surface animation and FX | — |

### 2.3 Player — `Assets/Scripts/Player/`
*Player combat and damage reception.*

| Script | Namespace | Role | Key API |
|---|---|---|---|
| `PlayerCombatController.cs` | `WaterBlob` | Shooting system. Reads facing from `transform.localScale.x`. Consumes `OneDropWaterResource2D`. Spawns bullet, configures it via `BulletRuntimeHandler` | `ShootHorizontal()`, Inspector: `bulletSpeed`, `shootCooldown`, `bulletLifetime`, layer masks |
| `BulletRuntimeHandler` | `WaterBlob` | **(inline in `PlayerCombatController.cs`)** Handles bullet collision. 3-layer system: `attackableLayers`, `nonDestructibleLayers`, `ignoredLayers` | `Setup(...)`, `OnCollisionEnter2D()` |
| `PlayerHurtbox.cs` | `WaterBlob` | Player contact damage. `OnCollisionEnter2D` + `OnTriggerEnter2D`. Destroys enemy on touch. Calls `HealthComponent.TakeDamage(1)` + `WaterResource.ConsumeDamage()` | Inspector: `touchEnemyLayers`, VFX prefabs |

### 2.4 Enemies — `Assets/Scripts/Enemies/`
*Enemy AI via FSM.*

| Script | Namespace | Role | Key API |
|---|---|---|---|
| `EnemyController.cs` | `WaterBlob.Enemies` | Root enemy component. Requires `Rigidbody2D`, `Animator`, `StateMachine`. Dynamically adds states in `Awake()`. Initializes FSM in `Start()` | `FlipSprite()`, Inspector: `speed`, `patrolDistance`, `waitTime`, detection params |
| `EnemyPatrolState.cs` | `WaterBlob.Enemies` | Patrol movement. `PhysicsUpdate()`: wall check + edge check via `Physics2D.RaycastAll`. Transitions to `WaitState` on obstacle or distance limit | `PhysicsUpdate()`, `CheckObstacle()` |
| `EnemyWaitState.cs` | `WaterBlob.Enemies` | Pause and flip. `LogicUpdate()`: countdown timer, then flip `Direction *= -1` and return to `PatrolState` | `LogicUpdate()`, `Enter()` |
| `EnemyChaseState.cs` | `WaterBlob.Enemies` | Pursues player at high speed. Uses cached `Player` reference | `Enter()`, `PhysicsUpdate()` |
| `EnemyAttackState.cs` | `WaterBlob.Enemies` | Close-range combat stance | `Enter()`, `LogicUpdate()` |

### 2.5 Bullets — `Assets/Scripts/bullets/`
*Projectile visual effects.*

| Script | Namespace | Role | Key API |
|---|---|---|---|
| `bullet.cs` | *(none / global)* | **Legacy visual only.** Adds googly eyes to bullet prefab. Collision logic is fully in `BulletRuntimeHandler` — do NOT add collision code here | `CreateGooglyEyes()` |
| `BulletWaterBlobVisual.cs` | `WaterBlob` | Applies blob visual styling to bullet mesh | — |
| `ShootMuzzleEffect.cs` | *(WaterBlob)* | Runtime muzzle flash FX when no prefab is set | — |
| `WaterSplashEffect.cs` | *(WaterBlob)* | Runtime water splash FX on bullet impact | — |

---

## 3. System Deep-Dives

### 3.1 Player Controller — `OneDropController2D.cs`

The main player controller. **831 lines.** Depends on `WaterBlobInput2D`, `WaterBlobCharacter2D`, and `OneDropWaterResource2D`.

```
FixedUpdate() loop:
  ├── CheckGround() / CheckWall() / CheckCeiling()
  ├── UpdateClimbState()
  ├── Movement: ApplyHorizontalMovement() or ApplySlideMovement() or ApplyClimbMovement()
  ├── DoGroundJump() / DoWallJump() — fires `OnJumped` / `OnLanded` / `OnWallDetached` events
  ├── ApplyRotation()
  └── ApplyVisualFlip()

Update() loop (input only):
  └── ReadInput()
```

**Key design rule:** **Visual decoupling.** `OneDropController2D` no longer handles mesh scaling or offsets. It exposes its physical state via public properties and signals landing/jump impacts via events. `OneDropDeformation2D` consumes this data to drive procedural visuals.
```

**Key design rule:** `ApplySoftBodyDeformation()` runs inside `FixedUpdate()` because it reads physics velocity — but it only modifies mesh/scale values, not forces. This is intentional.

### 3.2 Water Resource Economy

Managed by `OneDropWaterResource2D`. Water = health + ammo combined.

| Action | Cost | Method |
|---|---|---|
| Shoot water bullet | 5 units | `ConsumeShoot()` |
| **Take enemy damage** | **15 units** | `TakeDamage(amount)` — includes 0.8s I-frames ✅ |
| Moving horizontally | 1.5 units/sec | `ConsumeMove(deltaTime, isMoving)` |
| **Slide dash** | **10 units** | `ConsumeSlide()` |
| Jump | 8 units | `ConsumeJump()` |
| Refill (safe zone) | → 100 units | `Refill()` |

> **Agent rule:** Never implement a new action that costs water without calling the appropriate `OneDropWaterResource2D` method. If a new consume type is needed, add a method to that class — do not put cost logic inline in other scripts.

### 3.3 Enemy FSM Flow

```
EnemyController.Awake()
  └── Adds EnemyPatrolState + EnemyWaitState as components

EnemyController.Start()
  └── StateMachine.Initialize(PatrolState)
              ↓
        [EnemyPatrolState]
         PhysicsUpdate():
           Wall ahead? ──────────────────→ [EnemyWaitState]
           No ground ahead? ─────────────→ [EnemyWaitState]
           Reached patrolDistance? ──────→ [EnemyWaitState]
           else: move via linearVelocity
              ↓
        [EnemyWaitState]
         Enter(): halt velocity, play idle anim, set timer
         LogicUpdate(): countdown
           Timer done → Direction *= -1 → [EnemyPatrolState]
```

### 3.4 Combat & Bullet Flow

```
PlayerCombatController.Update()
  └── ReadShootPressedThisFrame() (**Space key** / Gamepad RB or West)
      └── ShootHorizontal()
            ├── Check cooldown (0.12s)
            ├── Check waterResource.ConsumeShoot() → abort if false
            ├── SpawnMuzzleEffect()
            ├── Instantiate(bulletPrefab, firePoint.position)
            ├── EnsureBlobVisual() → adds BulletWaterBlobVisual if flagged
            ├── ConfigureBulletRuntime() → sets up BulletRuntimeHandler
            ├── Set linearVelocity + gravityScale = 0
            ├── ApplyIgnoredLayerCollisions() → Physics2D.IgnoreCollision loop
            └── Destroy(bulletObj, bulletLifetime)

BulletRuntimeHandler.OnCollisionEnter2D()
  ├── IsLayerInMask(ignoredLayers) → skip
  ├── SpawnImpactEffect()
  ├── IsLayerInMask(attackableLayers) + NOT nonDestructibleLayers
  │     → SpawnVaporizationEffect() + Destroy(target)
  └── Destroy(gameObject) [bullet dies]
```

### 3.5 Player Damage Flow

```
PlayerHurtbox (OnCollisionEnter2D / OnTriggerEnter2D)
  ├── IsInvulnerable? → skip
  ├── Is enemy layer in touchEnemyLayers? → skip if not
  ├── SpawnEnemyTouchVaporization(enemy position)
  ├── Destroy(enemyRoot)
  ├── HealthComponent.TakeDamage(1) → triggers invulnerability timer
  ├── WaterResource.ConsumeDamage()  → -10 water
  └── SpawnPlayerDamageEffect(player position)
```

---

## 4. Component Assembly Guide

*Which components go on which GameObject. Use this when setting up scenes or prefabs.*

### 4.1 Player GameObject (`WaterBlobPlayer`)

| Component | Script | Notes |
|---|---|---|
| `Rigidbody2D` | Unity built-in | Gravity Scale ≠ 0; Freeze Z Rotation ON |
| `BoxCollider2D` | Unity built-in | Must use Zero Friction Physics Material (auto-created by controller) |
| `OneDropController2D` | `WaterBlob/` | **Central controller** — ⚠️ Set `allowWallJumpWhileClimbing = true` in Inspector |
| `OneDropDeformation2D` | `WaterBlob/` | **Visuals** — extracted squash/stretch logic. Requires `visualRoot` assignment |
| `WaterBlobCharacter2D` | `WaterBlob/` | Soft-body simulation ring |
| `WaterBlobMeshRenderer2D` | `WaterBlob/` | Blob mesh rendering |
| `WaterBlobGooglyEyes2D` | `WaterBlob/` | Eye visual |
| `OneDropWaterResource2D` | `WaterBlob/` | Water economy |
| `HealthComponent` | `Core/` | HP tracking |
| `PlayerHurtbox` | `Player/` | Contact damage |
| `PlayerCombatController` | `Player/` | Shooting |

> ⚠️ **Prefab rule:** After modifying any player script attachment, go to the Prefab and **Overrides → Apply All** to avoid broken scene instances.

### 4.2 Camera GameObject

| Component | Notes |
|---|---|
| `CameraFollow2D` | Target = `WaterBlobPlayer`. Auto-finds by `GameObject.Find("WaterBlobPlayer")` if unset |
| `Camera` (Unity) | Orthographic, URP renderer |

### 4.3 Enemy GameObject (Prefab)

| Component | Script | Notes |
|---|---|---|
| `Rigidbody2D` | Unity | Freeze Z Rotation ON; Collision Detection = Continuous recommended |
| Collider | Unity | Box or Capsule |
| `Animator` | Unity | Must have `"run"` bool parameter |
| `StateMachine` | `Core/` | **Required component** — `EnemyController` gets it via `GetComponent` |
| `EnemyController` | `Enemies/` | Adds `EnemyPatrolState` + `EnemyWaitState` dynamically in `Awake()` — do NOT add them manually in the Inspector. ⚠️ Set `obstacleMask` to `Ground + Wall` layers only (default `~0` is too broad) |

> ⚠️ `EnemyPatrolState` and `EnemyWaitState` are added at **runtime** by `EnemyController.Awake()`. Do NOT pre-attach them in the Inspector or the FSM will break.

### 4.4 Bullet Prefab

| Component | Notes |
|---|---|
| `Rigidbody2D` | `gravityScale = 0` (set by `PlayerCombatController` at spawn) |
| `Collider2D` | Circle or Box collider |
| `bullet.cs` | Optional — adds googly eyes |
| `BulletRuntimeHandler` | Added at runtime by `PlayerCombatController.ConfigureBulletRuntime()` |

### 4.5 Environment — Lava Platform

| Component | Notes |
|---|---|
| Collider2D (trigger) | Set to trigger for overlap damage |
| `LavaPlatformVisual2D` | Surface animation |

---

## 5. Layer Collision Matrix Rules

> Located in **Project Settings → Physics 2D → Layer Collision Matrix**

| Layer pair | Relationship | Notes |
|---|---|---|
| Bullet ↔ Player | **Ignore** | Player should not be hit by own bullets |
| Bullet ↔ WaterResource | **Ignore** | Collectibles must not be vaporized |
| Bullet ↔ Enemy | **Collide** | Vaporization on hit via `attackableLayers` |
| Bullet ↔ Wall/Ground | **Collide** (non-destructible) | Bullet dies, no destroy |
| Soft-body nodes ↔ each other | **Ignore** | `WaterBlobCharacter2D.IgnoreInternalCollisions()` handles this at runtime |
| Player ↔ Enemy | **Trigger or Collide** | `PlayerHurtbox` listens on both |

> **Agent rule:** New collision relationships must be configured here first. Only use `Physics2D.IgnoreCollision()` at runtime for **dynamic, same-layer** scenarios (e.g., soft-body node self-collision). See TODO item #4.

---

## 6. Implementation Status

### ✅ Done (Playable)

| Feature | Scripts Involved |
|---|---|
| Player horizontal movement with acceleration | `OneDropController2D`, `WaterBlobInput2D` |
| Ground jump | `OneDropController2D.DoGroundJump()` |
| Wall climb & wall jump | `OneDropController2D.UpdateClimbState()`, `DoWallJump()` |
| Slide dash (double-tap) | `OneDropController2D.ProcessSlideInput()`, `StartSlide()` |
| Soft-body deformation (visual) | `OneDropDeformation2D` (extracted 06/03/2026) |
| Soft-body physics simulation | `WaterBlobCharacter2D` (spring-joint ring) |
| Googly eyes | `WaterBlobGooglyEyes2D`, `bullet.cs` |
| Smooth follow camera | `CameraFollow2D` |
| Water resource economy (Unified) | `OneDropWaterResource2D` (v2.0 06/03/2026) |
| Dynamic character scaling | `WaterBlobCharacter2D` (Size scales with water) |
| Agility scaling (Speed/Jump) | `OneDropController2D` (Stats scale with water) |
| Water Resource HUD | `WaterResourceUI` (Gradient fill bar) |
| **Kinetic Climbing (v3.1)** | `OneDropController2D` (Speed 7.5 + Boost 8.5 + Flow 3.5) |
| **Hazard Robustness (v2.1)** | `HazardImpact` (Parent/Root discovery, improved knockback) |
| **Lava Hazards (v2.1)** | `HazardImpact` (Continuous drain, I-frame bypass) |
| **Charged Water Shot (v3.2)** | `PlayerCombatController` (Hold Space to charge) |
| **Enemy AI States (v3.2)** | `EnemyChaseState`, `EnemyAttackState` |

### 🔄 In Progress / Partial

| Feature | Status | Notes |
|---|---|---|
| Collision matrix cleanup | Partial | Bullet layer ignoring still uses `Physics2D.IgnoreCollision` loops — TODO #4 |
| Player decoupling | **COMPLETE** | Visual deformation moved to `OneDropDeformation2D` ✅ |

### ❌ Backlog (Not Implemented)

| Feature | Notes |
|---|---|
| **Charged water shot** | ✅ **COMPLETE** — v3.2 Space-hold charging |
| **Enemy Chase state** | ✅ **COMPLETE** — v3.2 ChaseState pursuit |
| **Enemy Attack state** | ✅ **COMPLETE** — v3.2 AttackState logic |
| **Water loss on lava** | ✅ **COMPLETE** — v2.1 Kinetic/Bypass implementation |
| **Water replenishment collectibles** | Pools/collectibles that call `WaterResource.Refill()` |
| **Game Over / Respawn loop** | ✅ **COMPLETE** — handled by `GameManager.cs` |
| **Sound effect hooks** | ✅ **COMPLETE** — v3.2 AudioManager hooks |
| **Post-processing** | URP global volumes exist but no centralized post-processing profile |

---

## 7. Agent Rules Quick Reference

1. **Read GCD.md** before proposing new mechanics — check design intent.
2. **Read TDD.md** before writing code — check conventions and architectural rules.
3. **Read §2 (Script Architecture Map)** and **§4 (Component Assembly)** before touching any GameObject or Prefab.
4. **Check §6 (Implementation Status)** before implementing a feature — it may already exist.
5. **Water costs** → use `OneDropWaterResource2D` methods only. Never inline cost math.
6. **Enemy states** → extend `State.cs`. Never use boolean flags in `Update()`.
7. **Physics forces** → `FixedUpdate()` only. Input reading → `Update()` only.
8. **After Prefab changes** → remind or trigger **Overrides → Apply All**.
9. **New collision rules** → Physics 2D Layer Collision Matrix first. `Physics2D.IgnoreCollision()` is a last resort.
10. **Naming** → `PascalCase` classes/methods, `camelCase`/`_camelCase` fields, `UPPER_CASE` constants.
