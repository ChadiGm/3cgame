# Release Notes

## 2026-03-24

### New Features
- Added dynamic earthquake camera effect to `CameraFollow2D` with:
  - Smooth Perlin-noise shake
  - Optional low-frequency rumble
  - Optional rotation shake
  - Enter/exit zone events (`onEnterEarthquakeZone`, `onExitEarthquakeZone`)
- Added support for multiple earthquake zones in `CameraFollow2D`:
  - Existing single zone (`earthquakeZoneStart` / `earthquakeZoneEnd`) still supported
  - New `additionalZones` list for Zone 2, Zone 3, etc.
- Added `FallWhenPlayerClose2D` component to trigger object falls when player is nearby.
- Added `EarthquakeReceiver2D` helper to route earthquake enter/exit calls via UnityEvents.

### Fixes
- Fixed earthquake activation reliability:
  - Removed unstable fallback behavior
  - Cleaned zone detection to avoid unintended activation
- Refactored earthquake camera follow flow to reduce jitter and drift:
  - Stabilized follow/shake composition
  - Prevented cumulative rotation drift
- Fixed immediate-fall issue in `FallWhenPlayerClose2D`:
  - Added startup hold (`holdInPlaceUntilTriggered`) to lock physics until trigger
  - Improved initial proximity handling (`ignoreIfPlayerStartsInside`)
- Fixed `fallDelay` reliability in `FallWhenPlayerClose2D`:
  - Replaced coroutine delay path with deterministic timer logic
  - Added `requireStayInRangeForDelay` option

### Notes
- Recent implementation focused on Unity 2D behavior while keeping optional 3D Rigidbody fallback in proximity-fall logic.
