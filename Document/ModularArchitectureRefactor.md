# ONE DROP (3cgame) - Modular Architecture Refactor Blueprint

Goal: faster iteration, fewer “spaghetti” dependencies, and a clean separation between gameplay logic, physics/motor, and presentation.

This document is tailored to the current repo structure and known coupling points.

## Target Shape (Unity-Pragmatic)

### 1) Three-Layer Separation (per feature)
- **Gameplay (Rules/State)**
  - deterministic decisions, timers, costs, cooldowns, state transitions
  - no `Transform` writes, no physics forces, no scene searches
- **Motor/Physics (Movement + Collisions)**
  - owns `Rigidbody2D`, casts, velocity/forces, collision interpretation
  - consumes gameplay “intent” (move axis, jump request, slide request)
  - publishes “observations” (grounded, wall contact, impact strength)
- **Presentation (Visual/Audio/UI/VFX)**
  - reads gameplay/motor signals and plays visuals
  - may subscribe to events, but never affects authoritative gameplay state

This maps well to Unity’s component model and keeps refactors incremental.

## Current Pain Points (Verified In Repo)
- `OneDropController2D` depends on `WaterBlobCharacter2D.Radius` for casts, so movement is coupled to soft-body internals.
- Facing/scale is written by both controller and deformation by default (`transform.localScale` vs `visualRoot.localScale`).
- Enemy death is owned by player-side code (`PlayerHurtbox` and bullet logic `Destroy(target)`).
- `OneDropWaterResource2D` plays audio directly and logs every change (tight coupling + noisy proof).
- `GameManager` binds to death via scene `FindWithTag/Find` and listens to two death sources.

## Concrete Restructure Recommendations

### A) Fix Transform Ownership First (cheap, huge payoff)
Recommended player hierarchy:
- `WaterBlobPlayer` (root)
  - `Rigidbody2D`, colliders, `OneDropController2D`, `OneDropWaterResource2D`, `PlayerCombatController`, `PlayerHurtbox`
- `VisualRoot` (child)
  - mesh/eyes/deformation (`OneDropDeformation2D` points here)
- `SoftBodyRoot` (optional child)
  - if you want soft-body points not to clutter root; otherwise keep as-is

Rules:
- Controller never writes `transform.localScale`.
- Presentation writes to `VisualRoot` only.
- Facing becomes a float/int signal (ex: `FacingSign`) emitted by the motor/controller.

### B) Decouple Movement From Soft-Body Internals
Introduce a tiny “shape provider” contract so the controller can cast without knowing about springs/points.

Example contract:
- `ICharacterShape2D`
  - `float CastRadius { get; }`
  - `Vector2 CastExtents { get; }` (or collider-based query)

Implementations:
- `ColliderShape2D` reads from the real collider bounds (preferred).
- `SoftBodyShape2D` adapts `WaterBlobCharacter2D` if you must keep radius-based casts.

End state:
- `OneDropController2D` depends on `ICharacterShape2D`, not `WaterBlobCharacter2D`.
- Soft-body can be swapped/tuned without touching motor logic.

### C) Make Damage Enemy-Owned
Replace “player destroys enemy” with a minimal receiver.

Contracts:
- `IDamageable` (or `IDamageReceiver`)
  - `void ApplyDamage(DamageContext ctx)`
- `DamageContext`
  - source (player/bullet), amount/type, hit point/normal, impulse, etc

Migration steps:
- Bullets: on hit, try `GetComponentInParent<IDamageable>` and call it.
- Touch hurtbox: on enemy contact, call `IDamageable` rather than `Destroy(enemyRoot)`.
- Enemy owns death effects and despawn, not the player.

### D) Stop Service Calls From Domain Logic
Right now water resource triggers audio directly. For cleaner separation:
- `OneDropWaterResource2D` raises signals only (`OnTakeDamage`, `OnDied`, `OnWaterChanged`)
- `AudioManager` (or an `AudioPresenter`) subscribes to those signals and plays SFX

Same principle for VFX: emit “damage happened” signals, let presentation decide visuals.

### E) Remove Repeated Find() With One Narrow Registry
Add a `PlayerRegistry` (or `GameContext`) component that lives once per scene (or as DDOL).
- On player spawn/enable: registers its `PlayerFacade` (references to water resource, controller, etc).
- Interested systems subscribe to `OnPlayerChanged(PlayerFacade facade)`.

Result:
- `GameManager`, `EnemyController`, `WaterResourceUI` no longer need scattered `Find(...)` fallbacks.

### F) Use asmdefs to Enforce Boundaries + Faster Compiles
Suggested assemblies (minimal):
- `OneDrop.Core` (utilities, state machine base, registry/event bus)
- `OneDrop.WaterBlob.Runtime` (controller, water resource, softbody runtime)
- `OneDrop.WaterBlob.Presentation` (deformation, mesh, eyes, HUD visuals)
- `OneDrop.Enemies.Runtime`
- `OneDrop.World.Runtime`
- `OneDrop.Player.Runtime` (combat/hurtbox)
- `OneDrop.Tests` (PlayMode/EditMode)

Rules via assembly references:
- Presentation assemblies can reference Runtime, but Runtime should not reference Presentation.

## Iterative Refactor Plan (Small Batches)
1. **VisualRoot split**
   - Add/require a `VisualRoot` child and route `OneDropDeformation2D.visualRoot` to it.
   - Remove controller scale writes; use a facing signal instead.
2. **Shape provider**
   - Add `ICharacterShape2D` adapter reading from collider bounds.
   - Replace controller’s `blobCharacter.Radius` cast path.
3. **Enemy damage contract**
   - Add `IDamageable` and implement on enemy root.
   - Switch bullet and touch paths to call the contract.
4. **Death authority cleanup**
   - Make `OneDropWaterResource2D` the only player death source.
   - Remove `HealthComponent` from the player path (keep as generic utility for other entities).
5. **Registry/binding**
   - Add `PlayerRegistry` and remove remaining `Find(...)` usage in core systems.
6. **asmdefs + tests**
   - Introduce assemblies and add at least one PlayMode smoke test that verifies bindings and no console spam.

## Acceptance Criteria (What “Good” Looks Like)
- Movement works if deformation/visuals are removed.
- Visuals can be disabled without changing gameplay.
- Enemies control their own death and reactions.
- Scene bootstrap happens in one obvious place; no repeated `Find` calls in gameplay loops.
- A new feature can be added by touching a small, feature-scoped set of scripts and prefabs.

