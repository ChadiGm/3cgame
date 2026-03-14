# Task Board
Open work only. No closed/completed narrative.

Fields:
- `TaskID`: required unique task identifier
- `Task`: required task summary
- `Priority`: `P0`, `P1`, `P2`
- `Status`: `backlog`, `doing`, `review`, `blocked`
- `Owner`: required when task is active
- `Scope`: exact implementation scope whenever possible
- `Lock`: explicit lock name(s) or `none`
- `Depends On`: optional dependency
- `Done When`: concrete completion condition
- `Lane`: optional routing label

Rules:
- Completed tasks are removed from this file after review acceptance and logging in `ChangeLog.md`.
- `Lock` is more important than `Lane`.
- Scope must be re-written before implementation expands beyond the declared boundary.
- `Lock` may reference scenes, shared prefabs, core scripts, global assets, or `none`.

| TaskID | Task | Priority | Status | Owner | Scope | Lock | Depends On | Done When | Lane |
|---|---|---|---|---|---|---|---|---|---|
| ARCH-209 | Core Controller Scope Narrowing | P2 | backlog | none | Assets/Scripts/WaterBlob/OneDropController2D.cs | OneDropController2D | ARCH-203 | OneDropController2D no longer handles softbody-internal logic; coordinate/radius logic moved to WaterBlobCharacter2D. | Refactor |
| FIX-101 | Fix BulletRuntimeHandler conflict | P0 | review | none | Assets/Scripts/WaterBlob/OneDropAttack2D.cs | OneDropAttack2D | none | Duplicate class name resolved and compiles | Bug |
| FIX-102 | Fix OneDropWaterResource2D consumers | P0 | review | none | Assets/Scripts/Core/, Assets/Scripts/Player/, Assets/Scripts/WaterBlob/ | OneDropWaterResource2D | FIX-101 | GameManager, ScaleArbiter, PlayerHurtbox, OneDropDeformation2D compile | Bug |
| FIX-103 | Fix Softbody Control Raycasts & Combats | P0 | review | none | Assets/Scripts/WaterBlob/OneDropController2D.cs, Assets/Scripts/Player/PlayerCombatController.cs | Controls | FIX-102 | Fixed controller ground check radii and bullet self-destruction on softbody layer | Bug |
| FIX-104 | Restore Interactions & Bullet Unity Physics | P0 | done | none | Assets/Scripts/WaterBlob/OneDropWaterResource2D.cs, Assets/Scripts/Player/PlayerCombatController.cs | Controls | FIX-103 | Player receives damage via IWaterReceiver, bullets trigger physically instantly | Bug |



