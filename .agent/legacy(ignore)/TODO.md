# ONE DROP — Sprint Backlog & TODO
**Last updated:** 06/03/2026 — post-audit update

> For agent context: read `AGENTS.md` → `PROJECT.md` before touching any item here.
> Full implementation status → `PROJECT.md §6`. Full audit findings → `AUDIT_v1.md`.

---

## Current Sprint

- [x] **#3 — Enemy AI FSM:** Replaced `Run.cs` / `NewMonoBehaviourScript.cs` with formalized FSM (`EnemyController`, `EnemyPatrolState`, `EnemyWaitState`). Enemy patrols, detects walls/ledges, waits, and flips correctly. ✅ Complete.

- [x] **#2 — Refactor Combat & Health:** Split monolithic attack script into `HealthComponent.cs`, `PlayerHurtbox.cs`, `PlayerCombatController.cs`, and inline `BulletRuntimeHandler`. ✅ Complete.

- [x] **A1 — Slide Dash Water Cost (Bug Fix):** `StartSlide()` was not calling `waterResource.ConsumeSlide()`. Slide dash had zero water cost. Added the missing call — now costs 15 water. ✅ Fixed in `OneDropController2D.cs` line 381.

- [x] **A2 — Enable Wall Jump (Inspector Fix):** `allowWallJumpWhileClimbing` field in `OneDropController2D` defaults to `false`. ✅ Fixed/Reminded - now default in code/prefab.
- [x] **#7 — Climbing Input Refinement (v3.3):** Implemented double-tap Up/W for jumping and removed 'K' key. ✅ Complete.
- [x] **#8 — Point Sync Stability (v3.4):** Fixed growth bug in tight spaces. ✅ Complete.

- [x] **#1 — Decouple Player Physics:** `OneDropController2D` visual deformation extracted to `OneDropDeformation2D.cs`. Controller simplified to physics-only; signals visuals via events. Reference `TDD.md §4.2`, `AUDIT_v1.md §B1`. ✅ Complete 06/03/2026.

- [x] **#4 — Optimize Collision Matrix:** `PlayerCombatController.ApplyIgnoredLayerCollisions()` was expensive. Moved bullet↔player rules to the **Physics 2D Layer Collision Matrix**. Reference `PROJECT.md §5`, `TDD.md §9`. ✅ Complete.

- [x] **#5 — Premium HUD HUD Visuals:** Resolved "black square" rendering issue in `WaterHUDAutoSpawner.cs` by applying custom droplet and gradient assets. ✅ Complete.

- [x] **#6 — Climb & Hazard Refinement (v2.1):** Implemented kinetic push climbing and I-frame bypassing lava hazards. ✅ Complete.

---

## Backlog — Future Features

### Gameplay
- [x] **Charged Water Shot** — Hold Space → charge → release. ✅ Complete v3.2.
- [x] **Enemy Chase State** — `ChaseState.cs`. ✅ Complete v3.2.
- [x] **Enemy Attack State** — `AttackState.cs`. ✅ Complete v3.2.
- [ ] **Water Replenishment Collectibles** — Env pickups → `OneDropWaterResource2D.Refill()` or partial fill. Reference `GCD.md §5`.
- [x] **Game Over / Respawn Loop (Urgent):** `HealthComponent.OnDied` and `WaterResource.OnDied` now trigger a scene reload via `GameManager.cs`. Reference `GCD.md §6.2`, `AUDIT_v1.md §B6`. ✅ Complete.

### Polish & Systems
- [ ] **Enemy `obstacleMask` Scoping** — `EnemyController.obstacleMask` defaults to `~0` (all layers). Scope to `Ground + Wall` in all Enemy prefabs to prevent false stops on player/triggers. Reference `AUDIT_v1.md §B3`.
- [ ] **Camera Target Assignment** — `CameraFollow2D` uses `GameObject.Find("WaterBlobPlayer")` as fallback. Assign `target` in Inspector on the Camera prefab. Reference `AUDIT_v1.md §B5`.
- [ ] **HUD — Water & HP Display** — Visual indicators for `OneDropWaterResource2D.Current` and `HealthComponent.CurrentHealth`. Reference `GCD.md §13`.
- [x] **SFX Hooks** — Wire `HealthComponent.OnTakeDamage`, `OnDied` to AudioSource. ✅ Complete v3.2.
- [ ] **Post-Processing Profile** — Centralize URP global volume. Bloom, vignette, color grading. Reference `TDD.md §10`.
- [ ] **Projectile Pooling** — Replace `Instantiate`+`Destroy` in `PlayerCombatController` with object pool. Reference `TDD.md §10`, `AUDIT_v1.md §B4`.
