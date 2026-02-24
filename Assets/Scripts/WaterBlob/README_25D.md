# WaterBlob 2.5D Authoring Guide

## Core Rule
Gameplay interactions stay in Physics2D. Visible 3D meshes are presentation only unless explicitly needed for non-gameplay effects.

## Layer Policy
- Gameplay2D_Ground: hidden 2D ground proxies used by `WaterBlobCharacter2D.groundMask`
- Gameplay2D_Hazard: hidden 2D hazard proxies
- Visual3D_Only: non-gameplay world meshes

## Platform Setup (3D + 2D Proxy)
1. Create a 3D visual root object.
2. Create a child object for gameplay proxy with `Collider2D` (+ optional `Rigidbody2D` kinematic).
3. Add `World3DToGameplay2DProxy` to the proxy object.
4. Add `BouncyPlatform2D` or `RotatingObstacle2D` to the proxy object if needed.
5. Add `BouncyPlatform25D`, `RotatingObstacle25D`, or `Platform25DAuthoring` to the visual root and wire references.

## Character
- Keep `lockToGameplayPlane = true`.
- Keep `gameplayPlaneZ = 0` by default.
- If camera/mesh depth changes, adjust visual offsets before changing physics values.

## Camera
`WaterBlobCameraRig` defaults to perspective side-follow.
- Use `usePerspective = true` for 2.5D look.
- Toggle to orthographic fallback for gameplay regression checks.
