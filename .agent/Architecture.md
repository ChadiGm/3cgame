# Architecture
Last Updated: 2026-03-09
Active architecture, ownership boundaries, and technical implementation rules. Status tracking lives in `CurrentState.md` and task state lives in `TaskBoard.md`.

## Ownership Model
- Main agent owns active governance documents.
- Worker agents implement scoped tasks and report through handoff packets.
- `.agent/legacy/` is frozen reference only.

## Gameplay Authority Decisions
- Player survival authority is `OneDropWaterResource2D`.
- `HealthComponent` remains an engine-level utility but is not the intended player-path authority.
- `WaterBlobInput2D` is the intended input authority.
- `OneDropController2D` is the current movement and traversal authority.
- Enemy-owned damage handling uses the standardized `IDamageable` contract.

## Runtime Reference Policy
Preferred order:
1. Event-driven broadcasts (`EventBus.Publish`) for cross-domain state changes (e.g., UI listening for player health).
2. serialized reference
3. explicit runtime binding from an owner/bootstrap system
4. one controlled fallback lookup in a bootstrap or migration path

Technical rule:
- Repeated ad hoc object lookup (`GameObject.Find`, `FindAnyObjectByType`) in `Update()` loops is **strictly forbidden**.
- Any fallback lookup should be cached on success and should log only actionable failure states.
- **State Arbitration:** When multiple systems attempt to control the same physical property (e.g., Resource size vs. Power-Up size), a Scale Arbiter processes the stack. Systems must not overwrite each other frame-by-frame.

## Update Loop Rules
- `Update()`: input reads, timers, non-physics state prep, UI-only reactions
- `FixedUpdate()`: force/velocity updates, casts, movement resolution, physics-driven state changes
- `LateUpdate()`: camera and other post-motion presentation when needed
- Visual systems must not apply gameplay forces.

## Player
- Input authority: `Assets/Scripts/WaterBlob/WaterBlobInput2D.cs`
- Movement and traversal: `Assets/Scripts/WaterBlob/OneDropController2D.cs`
- Soft-body simulation: `Assets/Scripts/WaterBlob/WaterBlobCharacter2D.cs`
- Deformation visuals: `Assets/Scripts/WaterBlob/OneDropDeformation2D.cs`
- Scale and physical size arbitration: `Assets/Scripts/Core/ScaleArbiter.cs`

Current boundary note:
- `OneDropController2D` still coordinates part of blob-body behavior and remains broader than the target architecture.
- Character scale is now managed by `ScaleArbiter`, allowing future power-up integration.

## Water / Survival
- Water economy, depletion, refill, invulnerability, and death event: `Assets/Scripts/WaterBlob/OneDropWaterResource2D.cs`
- Refill pickup contract: `Assets/Scripts/World/WaterRefillCollectible.cs`

Current boundary note:
- Water is the accepted player survival authority in governance.
- Player-path code cleanup is still pending where `HealthComponent` remains wired into lifecycle.

## Combat And Damage Policy
- Combat should consume shared input state rather than polling devices independently.
- Projectile object reuse is preferred over per-shot instantiate/destroy for the projectile body.
- **Universal Contracts:** Systems must interact via interfaces (e.g., `IDamageable`, `IWaterConsumer`).
- Player-side systems should *request* enemy damage via `IDamageable.TakeDamage()`, never owning enemy destruction or calling `Destroy(enemy)` directly.

## Combat Runtime
- Player shooting, charge logic, projectile pooling, and hit resolution policy: `Assets/Scripts/Player/PlayerCombatController.cs`
- Player contact damage path: `Assets/Scripts/Player/PlayerHurtbox.cs`
- Bullet visual support and runtime fallback effects: `Assets/Scripts/bullets/*`

Current boundary note:
- Enemy destruction is still handled directly by player-side systems in places.
- A minimal enemy-owned damage receiver contract is adopted policy but not yet implemented.

## Collision And Physics Policy
- Prefer the Physics 2D Layer Collision Matrix for stable global collision behavior.
- Use runtime ignore logic only for targeted dynamic exceptions.
- Keep friction and movement behavior deterministic across core traversal interactions.
- Treat broad global collider scans in gameplay paths as a performance smell.

## Enemies
- FSM base: `Assets/Scripts/Core/State.cs`, `Assets/Scripts/Core/StateMachine.cs`
- Enemy runtime root: `Assets/Scripts/Enemies/EnemyController.cs`
- State set: `EnemyPatrolState`, `EnemyWaitState`, `EnemyChaseState`, `EnemyAttackState`

Enemy FSM policy:
- Keep patrol, wait, chase, and attack in explicit state classes.
- Avoid boolean-state sprawl in a single update loop.
- Keep transitions explicit and inspectable.

Current boundary note:
- Enemy locomotion, state transitions, and attack behavior (contact/melee) are functional and decoupled.
- Advanced AI (Range, LOS, Navigation) remains a future expansion area.

## UI / HUD
- Runtime HUD binding and display: `Assets/Scripts/WaterBlob/WaterResourceUI.cs`
- HUD bootstrap path: `Assets/Scripts/WaterBlob/WaterHUDAutoSpawner.cs`
- Editor-only helper path: `Assets/Scripts/WaterBlob/WaterBlobUISetup.cs`, `Assets/Editor/WaterHUDPrefabBuilder.cs`

UI policy:
- The preferred runtime path is authored HUD prefab first with procedural fallback.
- Critical HUD references must validate on scene load or runtime bind.
- Missing HUD references should be treated as blockers for HUD reliability.

Current boundary note:
- `WaterBlobUISetup.cs` is a helper path, not the authoritative runtime path.

## World
- Hazard damage and player detection: `Assets/Scripts/World/HazardImpact.cs`
- Lava visuals and background FX: `Assets/Scripts/WaterBlob/LavaPlatformVisual2D.cs`, `Assets/Scripts/WaterBlob/LavaBackgroundFX2D.cs`

## Lifecycle And Bootstrap
- Scene reload lifecycle: `Assets/Scripts/Core/GameManager.cs`
- Audio singleton: `Assets/Scripts/Core/AudioManager.cs`

Bootstrap policy:
- Bootstrap systems may auto-find, auto-bind, or auto-create only when that behavior is explicit and narrow.
- Bootstrap behavior should not silently become the default dependency strategy for unrelated systems.
- `GameManager`, `AudioManager`, and `WaterHUDAutoSpawner` are current bootstrap systems and should remain easy to reason about.

## Code Quality Rules
- Cache frequently used component references in `Awake` or `Start`.
- Avoid hidden side-effects in public methods.
- Prefer comments that explain non-obvious intent, not line-by-line behavior.
- Reduce routine debug noise when it weakens proof quality or obscures actual issues.

## Validation Policy
- Use Level A proof for internal refactors and doc-alignment work.
- Use Level B proof for gameplay logic, prefab wiring, and bootstrap changes.
- Use Level C proof for movement, combat, enemy behavior, HUD bootstrap, and scene bootstrap changes.
