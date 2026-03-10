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



