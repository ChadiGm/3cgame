# WaterBlob 2.5D Authoring Guide

## Core Rule
Gameplay interactions stay in Physics2D. Visible 3D meshes are presentation-only unless explicitly needed for non-gameplay effects.

## Layer Policy
- `Gameplay2D_Ground`: hidden 2D ground proxies used by `WaterBlobCharacter2D.groundMask`
- `Gameplay2D_Hazard`: hidden 2D hazard proxies
- `Visual3D_Only`: non-gameplay world meshes

## Water System Architecture (Decoupled)
Water state is now separated from softbody movement:

- `WaterBlobWaterLevel2D`
  - Owns the player water value (`0..1`)
  - Exposes: `SetWaterLevel`, `ChangeWaterLevel`, `AddWater`, `RemoveWater`
  - Emits `WaterLevelChanged` when value changes
- `WaterBlobCharacter2D`
  - No longer stores the water variable directly
  - Reads water from `WaterBlobWaterLevel2D`
  - Uses water only to scale softbody radius (`waterSizeFactor`)
  - Keeps compatibility wrappers (`AddWater`, `RemoveWater`, `ChangeWaterLevel`) that forward to `WaterBlobWaterLevel2D`

This lets any other gameobject/component/script affect water without being linked to the character locomotion logic.

## UI Water Level (Simple Round Icon)
`WaterBlobWaterLevelHud` was rebuilt as a minimal circular icon:

- Round masked fill only (no panel, no percentage text)
- Simple vertical gradient fill (`gradientBottom -> gradientTop`)
- Fill amount is driven by `WaterBlobWaterLevel2D.WaterLevel`
- Auto-targets the first active `WaterBlobWaterLevel2D` if `autoFindTarget` is enabled

## Platform Setup (3D + 2D Proxy)
1. Create a 3D visual root object.
2. Create a child object for gameplay proxy with `Collider2D` (+ optional `Rigidbody2D` kinematic).
3. Add `World3DToGameplay2DProxy` to the proxy object.
4. Add one 2D behavior component to the proxy:
   - `BouncyPlatform2D`
   - `WaterPlatform2D`
   - `LavaPlatform2D`
   - `RotatingObstacle2D`
5. Add the matching 2.5D wrapper to the visual root:
   - `WaterPlatform25D`
   - `LavaPlatform25D`
   - or generic `Platform25DAuthoring`

## Interaction Behavior
- Water platform:
  - Resolves `WaterBlobWaterLevel2D` from collider rigidbody or parent hierarchy
  - Adds configured water amount
- Lava platform:
  - Resolves `WaterBlobWaterLevel2D` the same way
  - Removes configured water amount
  - Applies speed damp to the owner body when available

## Softbody Radius Rule
Softbody size remains driven by:

- `runtimeRadius = baseRadius * (1 + waterLevel * waterSizeFactor)`

When water changes, `WaterBlobCharacter2D` rebuilds its spring network so collision/shape stay consistent.

## Inconsistencies Found and Fixed
- Removed hard coupling where `WaterBlobCharacter2D` auto-created HUD (`EnsureInScene`), which mixed gameplay and UI responsibilities.
- Removed direct water ownership inside `WaterBlobCharacter2D`; moved to `WaterBlobWaterLevel2D`.
- Updated water/lava platforms to stop depending on `WaterBlobCharacter2D` specifically.
- Corrected docs text encoding issues and aligned naming to current scripts.

## Notes for Existing Scenes/Prefabs
- `WaterBlobCharacter2D` now requires `WaterBlobWaterLevel2D`.
- Existing serialized `waterLevel` data is migrated through legacy field mapping (`FormerlySerializedAs("waterLevel")`).
- If a HUD does not exist in scene, add `WaterBlobWaterLevelHud` to a gameobject and enable `autoFindTarget`.
