# ONE DROP (3cgame) - AI Project Snapshot Prompt (Repo-Verified)

Copy/paste this whole file as a single prompt into an AI assistant when you want architecture/refactor help.

## Context
- Engine: Unity `6000.3.0f1`
- Game: 2D precision action-platformer
- Active scene: `Assets/Scenes/SampleScene.unity`
- Core player: soft-body water blob with a single shared water resource used for survival and actions

## Source Of Truth (Internal)
- Accepted truth: `.agent/CurrentState.md`
- Architecture/authority rules: `.agent/Architecture.md`

## Code Layout (Assets/Scripts)
- `Core/`
  - `GameManager` (respawn loop via scene reload)
  - `AudioManager` (DontDestroyOnLoad SFX singleton)
  - `HealthComponent` (generic utility; still partially wired, but not intended as player survival authority)
  - `State`, `StateMachine` (FSM base in namespace `WaterBlob.AI`)
- `WaterBlob/` (player movement + softbody + water + HUD binding)
  - `WaterBlobInput2D` (device input, smoothing, queued jump/slide)
  - `OneDropController2D` (movement/traversal motor; consumes cached input; spends water)
  - `WaterBlobCharacter2D` (soft-body physics, spring ring + core; scales radius based on water)
  - `OneDropDeformation2D` (presentation + soft-body point sync; listens to damage; writes to `visualRoot`)
  - `OneDropWaterResource2D` (water economy + invulnerability + death event)
  - `WaterResourceUI` + `WaterHUDAutoSpawner` (HUD binding/spawn)
- `Player/`
  - `PlayerCombatController` (shoot + charge shot + local bullet pool)
  - `PlayerHurtbox` (touch damage to player; currently also destroys enemies on touch)
- `Enemies/`
  - `EnemyController` + states: `EnemyPatrolState`, `EnemyWaitState`, `EnemyChaseState`, `EnemyAttackState`
- `World/`
  - `HazardImpact` (hazards)
  - `WaterRefillCollectible` (refill pickup contract -> `OneDropWaterResource2D.TryRefill(...)`)
- `bullets/`
  - bullet runtime/visual helpers (used by `PlayerCombatController`)

## Authority Decisions (Intended Architecture)
- Input authority: `WaterBlobInput2D`
- Survival authority: `OneDropWaterResource2D` (player death when water reaches 0)
- Movement/traversal authority: `OneDropController2D`
- Presentation authority: `OneDropDeformation2D` and other visuals; visuals must not apply gameplay forces
- Enemy damage should become enemy-owned via a minimal receiver contract (not implemented yet)

## Runtime Event Flows (Verified)

### Input -> Gameplay
- `WaterBlobInput2D` exposes:
  - `Move: Vector2`
  - `JumpHeld: bool`
  - `ShootHeld: bool`
  - `ConsumeJumpPressed(): bool` (one-shot)
  - `ConsumeSlidePressed(): bool` (one-shot)
- `OneDropController2D` reads from `WaterBlobInput2D` and performs physics movement in `FixedUpdate` style code paths.

### Water Resource -> UI + Deformation + Death
- `OneDropWaterResource2D` events:
  - `OnWaterChanged(current, max)`
  - `OnTakeDamage`
  - `OnDied`
- `WaterResourceUI` binds to `OnWaterChanged` and `OnTakeDamage`
- `OneDropDeformation2D` subscribes to `OnTakeDamage` for squash feedback
- `GameManager` subscribes to:
  - `OneDropWaterResource2D.OnDied` (C# event)
  - `HealthComponent.OnDied` (UnityEvent) if present (legacy split still exists)
- Respawn: `GameManager.TriggerRespawn()` -> coroutine delay -> `SceneManager.LoadScene(activeScene.buildIndex)`

### Combat -> Water Spend + Enemy Interaction
- `PlayerCombatController` reads `ShootHeld` from `WaterBlobInput2D` when present (fallback device reads exist).
- `PlayerCombatController` spends water via `OneDropWaterResource2D` (shoot/charge costs) and plays SFX through `AudioManager`.
- Bullets currently destroy targets directly via `Destroy(target)` based on layer masks.
- `PlayerHurtbox` currently destroys enemies on touch and also calls `OneDropWaterResource2D.TakeDamage(...)`.

## Known Coupling / Tech Debt (Important For Refactors)
- Death authority is still split: `GameManager` listens to both `HealthComponent` and `OneDropWaterResource2D`.
- Presentation/transform ownership is mixed by default:
  - `OneDropDeformation2D` defaults `visualRoot = transform` and writes `visualRoot.localScale`
  - `OneDropController2D` flips `transform.localScale` for facing
- Movement depends on soft-body internals:
  - `OneDropController2D` uses `WaterBlobCharacter2D.Radius` to choose CircleCast sizes.
- Enemy destruction is player-owned in multiple places (`PlayerHurtbox`, bullet hit logic).
- `OneDropWaterResource2D` plays audio directly (gameplay logic calls presentation singleton).
- Several systems use `Find(...)` fallbacks (`GameManager`, `EnemyController`, `WaterResourceUI`).

## Constraints For AI Refactor Suggestions
- Prefer incremental, reversible refactors that keep `SampleScene` playable.
- Prefer explicit serialized references or a single bootstrap binder over repeated `Find(...)`.
- Keep physics/gameplay decisions out of visual scripts.
- Avoid introducing heavy frameworks unless the benefits are clearly measurable for this small project.

## What I Want From You (the AI)
1. Propose a modular Unity architecture that cleanly separates:
   - gameplay rules/state
   - physics/motor
   - presentation (deformation/VFX/audio/UI)
2. Provide a staged refactor plan that can be done in small PRs without breaking Play Mode.
3. Identify concrete contracts/interfaces to introduce (example: damage receiver, player registry, input source).
4. Recommend a folder and asmdef layout that enforces boundaries and speeds iteration.

