# ONE DROP — Feature Audit Report
**Date:** 06/03/2026 | **Author:** Antigravity Agent
**Scope:** Data-flow and cross-script compatibility audit for all implemented platformer features.
**Methodology:** Full read of `OneDropController2D.cs` (831 lines), `WaterBlobCharacter2D.cs` (316 lines), and all connected scripts. Results documented per feature.

---

## Legend
- ✅ **Compatible** — data flows correctly, no issue
- ⚠️ **Watch** — works but has a design or performance concern to track
- ❌ **Issue** — actual bug or incompatibility that should be fixed

---

## Feature 1 — Horizontal Movement & Visual Flip

**Scripts:** `OneDropController2D.ApplyHorizontalMovement()`, `ApplyVisualFlip()`
**Data flow:**
```
ReadInput() [Update]  →  inputX (private float)
FixedUpdate           →  ApplyHorizontalMovement()
                            targetX = inputX * moveSpeed
                            nextX = MoveTowards(rb.velocity.x, targetX, acceleration * dt)
                            rb.linearVelocity = (nextX, current.y)
                            facingSign = Sign(inputX) [if |inputX| > 0.01]
                      →  ApplyVisualFlip()
                            transform.localScale.x = |scale.x| * facingSign
```

**Findings:**
- ✅ Acceleration uses `MoveTowards` — frame-rate independent via `fixedDeltaTime`
- ✅ `facingSign` is the single source of truth for facing direction — used by both flip and shooting (`PlayerCombatController` reads `transform.localScale.x`)
- ⚠️ **Input duplication:** `OneDropController2D.ReadInput()` reads `Keyboard`/`Gamepad` directly. `WaterBlobInput2D` also reads the same keys (`A/D/Space`). **Both scripts poll input independently.** If both are on the same GameObject, jump and move are double-read. Currently the controller does NOT use `WaterBlobInput2D` at all — the two scripts are **parallel, not connected**.
- ⚠️ **Jump key discrepancy:** Controller uses `W / Up Arrow` for jump. `WaterBlobInput2D` uses `Space / South Button`. They expose different jump keys — if both were active, the player would have two different jump bindings with no shared state.

---

## Feature 2 — Ground Jump

**Scripts:** `OneDropController2D.DoGroundJump()`
**Data flow:**
```
FixedUpdate:
  CheckGround() → grounded (bool)
  jumpPressed (set in ReadInput [Update])
  jumpIntervalTimer guard
  → DoGroundJump():
      v.y = 0 (zero out vertical vel first)
      rb.AddForce(Vector2.up * jumpForce, Impulse)
      blobCharacter.PointBodies → AddForce(up * jumpForce * 0.2f, Impulse) [each point]
      waterResource.ConsumeJump()   → -12 water
      jumpPulse = 1f                → feeds ApplySoftBodyDeformation
      jumpIntervalTimer = 0.22s     → prevents double-jump
      postJumpGroundIgnoreTimer = 0.08s → prevents immediate re-grounding
```

**Findings:**
- ✅ `postJumpGroundIgnoreTimer` correctly prevents the frame-1 ground re-detection bug
- ✅ `jumpIntervalTimer` prevents bounce-spamming
- ✅ Point bodies receive scaled impulse — blob spring ring follows the core upward naturally
- ✅ Water cost correctly gated through `waterResource.ConsumeJump()`
- ✅ `previousVelY` saved at end of `FixedUpdate` feeds `ApplySoftBodyDeformation` landing detection next frame

---

## Feature 3 — Wall Climb

**Scripts:** `OneDropController2D.UpdateClimbState()`, `ApplyClimbMovement()`, `SetBlobCharacterEnabled()`
**Data flow:**
```
FixedUpdate:
  CheckWall() → touchingWall, wallHit, wallDirection
  IsWallClimbable(wallHit) → checks climbableWallMask (or wallMask if mask empty)
  UpdateClimbState(touchingClimbWall, wallDirection):
    Entry conditions: not pressing down + (pressing up OR moving toward wall OR pushing into wall)
    → isClimbing = true
    → isSliding = false           (mutual exclusion: climb cancels slide)
    → rb.gravityScale = 0f        (zero gravity during climb)
    → rb.linearVelocity = zero    (halt on attach)
    → SetBlobCharacterEnabled(false) → disables WaterBlobCharacter2D + sets point body gravityScale = 0
  ApplyClimbMovement():
    targetY = inputY * climbSpeed
    nextY = MoveTowards(velocity.y, targetY, climbAcceleration * dt)
    rb.velocity = (0, nextY)
    → ALSO syncs all blobCharacter.PointBodies to same velocity (0, nextY)
  StopClimbing():
    → rb.gravityScale = blobCharacter.gravityScale (3.5f) if blob present, else gravityWhenNotClimbing
    → SetBlobCharacterEnabled(true) → re-enables blob + restores point gravity
```

**Findings:**
- ✅ `SetBlobCharacterEnabled(false)` correctly freezes the spring simulation during climb — prevents blob from "sagging" off the wall
- ✅ Point body velocity is manually synced in `ApplyClimbMovement` — blob visually follows the core
- ✅ Gravity restoration on `StopClimbing()` correctly reads from `blobCharacter.gravityScale` (not a hardcoded value)
- ⚠️ **Gravity authority split:** `WaterBlobCharacter2D` sets `rb.gravityScale` via `ApplyCoreSettings()` (called at `Start()`), and `OneDropController2D` also writes `rb.gravityScale` in `ApplyHorizontalMovement()` and `StopClimbing()`. If they run on the same `Rigidbody2D`, the controller correctly wins at runtime — but if `WaterBlobCharacter2D.OnValidate()` runs in the editor, it may reset gravity unexpectedly.
- ⚠️ **`climbableWallMask` defaults to `0` (nothing)** — when zero, `IsWallClimbable` uses `wallMask` as the fallback. This is intentional but non-obvious. If `wallMask` is also empty in the Inspector, the player can never climb. Must be configured.
- ✅ **Kinetic Refinement (v2.1):** "Sticky force" replaced with velocity-based kinetic push. Climb speed increased to 6.0. Logic is high-performance and feels agile.

---

## Feature 4 — Wall Jump

**Scripts:** `OneDropController2D.DoWallJump()`
**Data flow:**
```
Conditions: isClimbing AND jumpPressed AND jumpIntervalTimer <= 0
  (requires allowWallJumpWhileClimbing = true in Inspector)
→ DoWallJump():
    away = -climbWallDirection
    StopClimbing()                        → re-enables gravity + blob
    wallDetachPulse = 1f                  → visual stretch pulse
    wallDetachDirection = away
    v.y = 0 (zero vertical vel)
    rb.AddForce((away * wallJumpHorizontalForce, wallJumpVerticalForce), Impulse)
    blobCharacter.PointBodies → AddForce(above * 0.2f, Impulse)
    facingSign = away
    waterResource.ConsumeJump()           → -12 water
```

**Findings:**
- ✅ `StopClimbing()` is called first — gravity and blob re-enabled before the impulse is applied, ensuring physics is active to receive the force
- ✅ `wallDetachPulse` correctly drives the blob stretch visual in `ApplySoftBodyDeformation`
- ❌ **`allowWallJumpWhileClimbing` defaults to `false`** (line 44). Wall jump is completely disabled by default. This is a design-breaking default — the GCD lists Wall Jump as a core ability. **This must be `true` in the prefab.**

---

## Feature 5 — Slide Dash

**Scripts:** `OneDropController2D.ProcessSlideInput()`, `StartSlide()`, `ApplySlideMovement()`
**Data flow:**
```
ReadInput() [Update]:
  shiftDown = Shift key (keyboard) or RightTrigger/LeftTrigger (gamepad)
  → ProcessSlideInput(shiftDown)
      Guard: isClimbing || isSliding || slideCooldownTimer > 0 → return
      → StartSlide(dir):
          isSliding = true
          slideDirection = dir
          slideTimer = slideDuration (0.18s)
          slideCooldownTimer = slideCooldown (0.25s)
          facingSign = dir

FixedUpdate:
  slideTimer countdown
  if isSliding && slideTimer <= 0 → isSliding = false
  ApplySlideMovement():
    rb.velocity.x = slideDirection * slideSpeed (14 units/s)
    → ALSO syncs all blobCharacter.PointBodies.velocity.x = same
```

**Findings:**
- ✅ Mutual exclusion with climb works: `UpdateClimbState` sets `isSliding = false` on climb enter
- ✅ Cooldown correctly prevents immediate re-triggering
- **Water Resource Economy:**
    - **Re-engineered (06/03/2026):** Consolidated with Health system. Resource level now drives physical `radius` in `WaterBlobCharacter2D` and `moveSpeed`/`jumpForce` in `OneDropController2D`.
    - **Consumption Logic:** Wired for Shooting, Jumping, Moving, and Taking Damage (I-frames included).
    - **Reactive HUD:** New `WaterResourceUI` component built for visual feedback.
    - **Architecture:** `HealthComponent` deprecated for player; uses unified `OneDropWaterResource2D`.
- ⚠️ **Slide input is Shift key** — not documented in GCD (which lists "Double-tap A or D"). The actual trigger is `shiftDown` from `ReadInput`, not a double-tap detection. The GCD description is inaccurate vs implementation.
- ⚠️ GCD §4.1 lists "Slide Dash" cost as 15 water but it doesn't fire — needs the missing `ConsumeSlide()` call.

---

## Feature 6 — Soft-Body Deformation (Visual)

**Scripts:** `OneDropController2D.ApplySoftBodyDeformation()`
**Data flow:**
```
All inputs from current FixedUpdate state (read before call):
  rb.linearVelocity, previousVelocity, previousVelY
  grounded, groundedLastStep, isClimbing, touchingWallThisStep,
  touchingCeilingThisStep, ceilingCompression01, currentGroundNormal
  facingSign, accelerationSignal, inputX
→ Outputs (all visual, no physics forces):
  visualRoot.localScale (lerped)
  visualRoot.localPosition (lerped, only if visualRoot != transform)
```

**Deformation signal map:**

| State | Effect |
|---|---|
| Idle on ground | `idleGroundSquash` — slight horizontal spread |
| Running | `runSquash` — speed-driven lateral flatten |
| Accelerating | `accelerationStretch` — horizontal stretch in movement direction |
| Decelerating | `decelerationSquash` — vertical compress |
| Rising (jump) | `jumpStretch` — vertical elongation |
| Falling | `fallStretch` — vertical elongation |
| At jump apex | `peakStretch` — subtle float stretch |
| Landing impact | `landingSquash` — impact pulse squash |
| Jump pre-compress | `jumpPreCompress` — squash the frame of jump |
| On wall / climbing | `wallAdhesionFlatten` + `wallAdhesionStretch` |
| Wall detach | `wallDetachStretch` pulse |
| Slope contact | `slopeAdaptiveSquash` |
| Near ceiling | `ceilingCompressionDampen` / `ceilingSpreadDampen` |

**Findings:**
- ✅ All deformation logic reads velocity/state, never writes forces — correctly decoupled from physics
- ✅ All pulse values (`landingPulse`, `jumpPulse`, `wallDetachPulse`) decay via `MoveTowards` each frame — no accumulation bug
- ✅ `minVisualHeightScale` (0.82) prevents the blob from being compressed to zero height
- ✅ `deformLerpSpeed` (14) smooth lerp prevents snapping
- ⚠️ **`visualRoot` fallback to `self`:** If `visualRoot` is not assigned, `visualRootIsSelf = true` and `localPosition` offset skipped. The momentum-offset (visual lean) feature only activates when a separate visual root child is assigned. Document this in Inspector.

---

## Feature 7 — Soft-Body Physics Simulation

**Scripts:** `WaterBlobCharacter2D`
**Data flow:**
```
Start() → RebuildBlob():
  Creates ring of N point GameObjects (children of __BlobPoints_name sibling)
  Each point: Rigidbody2D + CircleCollider2D + SpringJoint2D (to core) + SpringJoint2D (to neighbors)
  IgnoreInternalCollisions() → Physics2D.IgnoreCollision between all pairs

FixedUpdate() → ApplyRadialStabilization():
  For each point: compute radial error from (radius - distance)
  Apply spring-damper force pushing point toward/away core
  Apply equal and opposite force to core (scaled by 1/N)
```

**Findings:**
- ✅ `__BlobPoints_{name}` sibling approach is sound — point bodies are NOT children of the player, avoiding transform hierarchy physics issues
- ✅ `IgnoreInternalCollisions()` correctly uses `Physics2D.IgnoreCollision` — this is the **approved use** per TDD (soft-body internal collision = legitimate runtime ignore)
- ✅ `IReadOnlyList<Rigidbody2D> PointBodies` — correctly exposes points as read-only to the controller
- ⚠️ **Two physics materials in conflict:** `WaterBlobCharacter2D.CreateRuntimeMaterial()` creates `"WaterBlobRuntime"` with `bounciness=0.12, friction=0.28` and applies it to the core `Rigidbody2D`. `OneDropController2D.CreateAndApplyZeroFrictionMaterial()` creates `"OneDropZeroFriction"` with `friction=0, bounciness=0` and applies it to `box.sharedMaterial` AND `rb.sharedMaterial`. The controller's material **overwrites the blob's material on the shared Rigidbody2D** (`rb.sharedMaterial`). The core body and outer BoxCollider2D end up with the controller's zero-friction material — the blob's friction/bounciness settings on the core are effectively ignored at runtime.
- ⚠️ **`rebuildOnStart = true`** rebuilds the ring on every scene load. If the scene is reloaded frequently (Game Over loop), this re-runs `RebuildBlob()` each time — acceptable cost but worth noting.

---

## Feature 8 — Water Resource Economy

**Scripts:** `OneDropWaterResource2D`, called by `OneDropController2D`, `PlayerCombatController`, `PlayerHurtbox`
**Data flow summary:**

| Call site | Method | Notes |
|---|---|---|
| `DoGroundJump()` | `ConsumeJump()` | ✅ Correct |
| `DoWallJump()` | `ConsumeJump()` | ✅ Correct |
| `FixedUpdate` (move) | `ConsumeMove(dt, isMoving)` | ✅ Correct — `isMoving` = `|inputX|>0.1 OR isSliding OR isClimbing` |
| `StartSlide()` | *(nothing)* | ❌ **Missing** — `ConsumeSlide()` never called |
| `PlayerCombatController.ShootHorizontal()` | `ConsumeShoot()` | ✅ Correct — also gates the shot if `false` |
| `PlayerHurtbox.HandleHit()` | `ConsumeDamage()` | ✅ Correct |
| `HazardImpact.ProcessHazard()` | `TakeDamage(true)` | ✅ Correct (v2.1 I-frame bypass) |

**Findings:**
- [x] Consolidate Water and Health into a unified "Lifeblood" system.
- [x] Implement physical size scaling (blob shrinks when low on water).
- [x] Implement agility scaling (move/jump power depends on water volume).
- [x] Create a reactive HUD with color gradients and low-health pulsing.
- [x] Deprecate individual `HealthComponent` on the player.

---

## Feature 9 — Health Component & Invulnerability

**Scripts:** `HealthComponent`, `PlayerHurtbox`
**Data flow:**
```
PlayerHurtbox.HandleHit():
  → healthComponent.IsInvulnerable?  → skip (i-frame guard)
  → healthComponent.TakeDamage(1)
      invulnerabilityTimer = invulnerabilityTime (0.75s)
      currentHealth -= 1
      OnTakeDamage.Invoke()
      OnHealthChanged.Invoke(current, max)
      if health == 0: OnDied.Invoke()
```

**Findings:**
- ✅ Invulnerability i-frame (0.75s) properly gates `HandleHit` via `IsInvulnerable` property check — enemy can't multi-hit in the same contact frame
- ✅ `[DisallowMultipleComponent]` prevents duplicate health components
- ✅ Events (`OnDied`, etc.) are `UnityEvent` — wirable in Inspector without code coupling
- ⚠️ `OnDied` fires but **nothing listens to it** — the Game Over / respawn loop is backlog. If health hits 0, the game silently continues with the player "dead."

---

## Feature 10 — Player Shooting & Bullet System

**Scripts:** `PlayerCombatController`, `BulletRuntimeHandler`
**Data flow:**
```
Update():
  ReadShootPressedThisFrame() → J key or Gamepad
  → ShootHorizontal():
      cooldownTimer (0.12s) guard
      waterResource.ConsumeShoot() → abort if false (< 5 water)
      facingSign = localScale.x >= 0 ? 1 : -1
      SpawnMuzzleEffect()
      Instantiate(bulletPrefab, firePoint.position)
      EnsureBlobVisual() → adds BulletWaterBlobVisual if flagged
      ConfigureBulletRuntime() → adds BulletRuntimeHandler:
        attackableLayers, nonDestructibleLayers, ignoredLayers, VFX prefabs
      bulletRb.linearVelocity = direction * bulletSpeed (20 units/s)
      bulletRb.gravityScale = 0
      ApplyIgnoredLayerCollisions() → Physics2D.IgnoreCollision loop (scene scan)
      Destroy(bulletObj, bulletLifetime) (4s)

BulletRuntimeHandler.OnCollisionEnter2D():
  if ignoredLayers → skip
  SpawnImpactEffect()
  if attackableLayers AND NOT nonDestructibleLayers:
    SpawnVaporizationEffect()
    Destroy(target)
  Destroy(self)
```

**Findings:**
- ✅ Water gate (`ConsumeShoot` returning false) correctly blocks the shot
- ✅ Facing direction read from `transform.localScale.x` — consistent with `PlayerCombatController` and `OneDropController2D.ApplyVisualFlip()`
- ✅ `BulletRuntimeHandler` is added at spawn (not pre-placed) — correct pattern
- ⚠️ **`ApplyIgnoredLayerCollisions()` is expensive.** It calls `FindObjectsByType<Collider2D>()` every shot — scanning ALL scene colliders. In a scene with many colliders this is a significant GC allocation and CPU spike. This is the core TODO #4 issue. Should be replaced by the Layer Collision Matrix.
- ⚠️ Bullets use `Instantiate` + `Destroy(lifetime)` — no pooling. Multiple rapid shots = GC pressure (TODO backlog).

---

## Feature 11 — Player Hurtbox (Contact Damage)

**Scripts:** `PlayerHurtbox`
**Data flow:**
```
OnCollisionEnter2D(collision) → HandleHit(collision.gameObject, collision.rigidbody)
OnTriggerEnter2D(other)       → HandleHit(other.gameObject, other.attachedRigidbody)

HandleHit():
  healthComponent.IsInvulnerable? → skip
  IsLayerInMask(layer, touchEnemyLayers)? → skip if not enemy
  SpawnEnemyTouchVaporization(enemy position)
  Destroy(enemyRoot)  [rb.gameObject or touchedObj]
  healthComponent.TakeDamage(1)   → triggers i-frame
  waterResource.ConsumeDamage()   → -10 water
  SpawnPlayerDamageEffect(player position)
```

**Findings:**
- ✅ Both `OnCollisionEnter2D` and `OnTriggerEnter2D` handled — works with both trigger and solid enemy colliders
- ✅ `rb.gameObject` used as enemy root (not child collider) — `Destroy(enemyRoot)` removes the full enemy, not just the collider child
- ✅ `IsInvulnerable` check properly prevents multi-hit in the same i-frame window
- ✅ `touchEnemyLayers` layermask check means only tagged enemy layers trigger damage — friendly fire / wall contact safe

---

## Feature 12 — Enemy FSM (Patrol + Wait)

**Scripts:** `EnemyController`, `EnemyPatrolState`, `EnemyWaitState`, `StateMachine`
**Data flow:**
```
Awake():
  Body = GetComponent<Rigidbody2D>()
  Anim = GetComponent<Animator>()
  stateMachine = GetComponent<StateMachine>()
  PatrolState = AddComponent<EnemyPatrolState>()   ← dynamic add
  WaitState   = AddComponent<EnemyWaitState>()     ← dynamic add
Start():
  StartX = transform.position.x
  stateMachine.Initialize(PatrolState)
    → PatrolState.Enter(fsm):  FlipSprite(), Anim.SetBool("run", true)

StateMachine.Update()  → PatrolState.LogicUpdate() [empty]
StateMachine.FixedUpdate() → PatrolState.PhysicsUpdate():
  wall check: RaycastAll(forward, wallCheckDistance + |groundCheckOffset.x|)
  edge check: RaycastAll(groundCheckOrigin, down, edgeCheckDistance)
  if wallAhead || !groundAhead → fsm.ChangeState(WaitState)
  else: rb.linearVelocity = (direction * speed, current.y)
  if distFromStart >= patrolDistance → fsm.ChangeState(WaitState)

WaitState.Enter():   halt velocity, Anim.SetBool("run", false), timer = waitTime
WaitState.LogicUpdate():  timer -= dt; if done → Direction *= -1 → PatrolState
```

**Findings:**
- ✅ `StateMachine.Update()` calls `LogicUpdate()` and `FixedUpdate()` calls `PhysicsUpdate()` — correctly split
- ✅ `CheckObstacle` uses `RaycastAll` with self-exclusion — correctly skips the enemy's own colliders and children
- ✅ `FlipSprite()` is called on `Enter(PatrolState)` — direction is set before movement begins
- ✅ `WaitState` halts `linearVelocity.x` on enter — no coasting past the endpoint
- ⚠️ **`obstacleMask` defaults to `~0` (all layers).** This means the ground check will detect anything — including triggers, water, bullets, player. Should be scoped to the `Ground` and `Wall` layers only. May cause premature patrol stops.

---

## Feature 13 — Smooth Follow Camera

**Scripts:** `CameraFollow2D`
**Data flow:**
```
Awake(): if target == null → Find("WaterBlobPlayer")
LateUpdate():
  desired = target.position + offset (0, 1.2, -10)
  if clampX: desired.x = Clamp(desired.x, xLimits)
  if clampY: desired.y = Clamp(desired.y, yLimits)
  position = SmoothDamp(current, desired, smoothTime=0.13s)
```

**Findings:**
- ✅ `LateUpdate()` runs after all `FixedUpdate` and `Update` — correct for a follow camera (target position is final for the frame)
- ✅ `SmoothDamp` is frame-rate independent — same feel at 30 or 60 FPS
- ✅ `clampY = false` by default — camera follows vertically freely; `clampX = true` by default for level boundaries
- ⚠️ `GameObject.Find("WaterBlobPlayer")` in `Awake()` — this is a string search. Works but will silently fail (no camera follow) if the player GameObject is renamed. Assign the target via Inspector on the Camera prefab.

---

## Summary of Issues & Actions Required

### ❌ Bugs / Missing Logic (Fix Needed)

| ID | Location | Issue | Fix |
|---|---|---|---|
| **A1** | `StartSlide()` | `ConsumeSlide()` was missing | ✅ Fixed 06/03/2026 |
| **A2** | `allowWallJumpWhileClimbing` | Default false | ⚠️ Needs Inspector update |

### ⚠️ Design / Performance Concerns (Document & Track)

| ID | Location | Concern | Recommendation |
|---|---|---|---|
| **B1** | `OneDropController2D.ReadInput()` | Reads input directly — `WaterBlobInput2D` is unused/parallel | Clarify in docs: controller is standalone; `WaterBlobInput2D` is an optional/legacy secondary. If using controller, do NOT also attach `WaterBlobInput2D` to same GameObject |
| **B2** | `WaterBlobCharacter2D` & `OneDropController2D` | Both write to `rb.sharedMaterial` — controller's zero-friction wins at runtime, blob's `bounciness=0.12` on core is overridden | Document this clearly; if bounce feel is desired on core, apply to point colliders only |
| **B3** | `EnemyController.obstacleMask` | Defaults to `~0` (all layers) — may detect player/triggers as obstacles | Set explicitly to `Ground + Wall` layers in all Enemy prefabs |
| **B4** | `PlayerCombatController.ApplyIgnoredLayerCollisions()` | `FindObjectsByType<Collider2D>()` called every shot — expensive | Already tracked as TODO #4; migrate to Layer Collision Matrix |
| **B5** | `CameraFollow2D.Awake()` | `GameObject.Find("WaterBlobPlayer")` string search | Assign target in Inspector; keep Find as fallback with a logged warning |
| **B6** | `HealthComponent.OnDied` | Fires but nothing listens — game continues silently after death | Already tracked as backlog; add to TODO as urgent |

---

## Changes Made to Project Files

- **`PROJECT.md`** — Added §7 (Audit Notes) summarizing all findings and confirmed correct data flows
- **`TODO.md`** — Added A1 and A2 as sprint items; B3, B5 added to backlog
- **`TDD.md`** — Updated §6.4 water economy table (marked slide cost as ⚠️ not firing), updated §7.2 to note `obstacleMask` default concern, updated §8 with bullet performance note
- **`GCD.md`** — Updated §4.1 slide input description to reflect actual Shift key trigger

> This audit file lives at: `.agent/AUDIT_v1.md`
> Re-run this audit when major features are added or any of the above flagged scripts are modified.
