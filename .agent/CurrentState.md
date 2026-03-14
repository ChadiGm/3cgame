# CurrentState
Last Updated: 2026-03-09
Accepted merged truth for the current implementation.

## Project Snapshot
- Project: ONE DROP (`3cgame`)
- Engine: Unity `6000.3.0f1`
- Active scene: `Assets/Scenes/SampleScene.unity`
- Current branch: `mydesktop(renderproblem)`

## Subsystem Status
Allowed statuses: `Done`, `In Progress`, `Blocked`, `Backlog`.

| Subsystem | Status | Confidence | Last Verified | Evidence |
|---|---|---|---|---|
| Core lifecycle (death/respawn) | Done | High | 2026-03-10 | `GameManager` subscribes only to `OneDropWaterResource2D.OnDied`; `HealthComponent` deleted (ARCH-201 accepted 2026-03-10) |
| Player movement/controller | Done | Medium | 2026-03-10 | `OneDropController2D` restored stability with `CircleCast` while remaining independent of softbody internals; verified in ARCH-203 REDO |
| Deformation/soft-body visuals | Done | High | 2026-03-10 | `OneDropDeformation2D` owns visual scaling/flipping; `ScaleArbiter` manages physical radius independently of resource loop (ARCH-205) |
| Combat/projectiles | Done | High | 2026-03-10 | `PlayerCombatController` and `BulletRuntimeHandler` use `IDamageable` contract (ARCH-202); bullet pooling verified in code |
| Water pickups/refills | Done | High | 2026-03-10 | `WaterRefillCollectible` routes through `IWaterReceiver` proxy (ARCH-206) |
| Enemy FSM | Done | High | 2026-03-10 | Patrol/Wait/Chase/Attack states functional; IDamageable, IWaterReceiver, and LOS/Aggro paths verified (AI-003) |
| UI/HUD | Done | High | 2026-03-10 | `WaterResourceUI` is now event-driven; `Update()` polling removed (ARCH-204) |
| World hazards/lava | Done | High | 2026-03-10 | `HazardImpact` routes through `IWaterReceiver` proxy (ARCH-206) |
| Documentation/process | Done | High | 2026-03-09 | Active governance now uses `Workflow.md`, `Architecture.md`, and `Design.md` |

## Accepted Governance Truth
- `Workflow.md` is the only active process-rules document.
- `TaskBoard.md` is the active open-work board.
- `ChangeLog.md` is the active append-only task history and proof log.
- `Architecture.md` is the active architecture and technical reference.
- `Design.md` is design intent only.
- `.agent/legacy/` is frozen reference only.

- **[Done]** Play Mode smoke test (`TEST-101`) for hazard/combat damage paths, pooled shooting, and refill collection confirmed via `Editor.log`.
- **[Resolved]** Gameplay scripts decoupled from `AudioManager` singleton via `EventBus` (ARCH-207).
- HUD coverage is centered on `SampleScene`; additional gameplay scenes will need equivalent smoke checks when added.
- Authored HUD is loaded from `Resources` path (`UI/WaterHUD`); future UI scale may require a different asset-loading strategy.
- **[Resolved]** `EnemyController.obstacleMask` auto-scopes at runtime to exclude player/interactive layers; player discovery is now event-driven (AI-001).
- **[Resolved]** Repo hygiene: Stale references to deleted scripts cleared (CLEAN-101); legacy assets and temp files quarantined (CLEAN-102).
- **[Resolved]** Fixed compilation blockers caused by duplicate `BulletRuntimeHandler` (FIX-101) and missing `OneDropWaterResource2D` API consumers (FIX-102).
- **[Resolved]** Restored missing player action inputs (Jump, Climb, Attack) by aligning controller detection casts to respect dynamic softbody radius scaling, and resolving bullet self-destruction on softbody collision points (FIX-103).
- **[Resolved]** Restored interaction loops (refills/damage) via `IWaterReceiver` and implemented elegant tag-based bullet ghosting to prevent softbody physics stalls (FIX-104).
