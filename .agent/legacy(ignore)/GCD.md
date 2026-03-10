# ONE DROP — Game Concept Document (GCD)
**Version:** 1.0 (15/02/2026) · **v2.0 Update:** 06/03/2026 — Chadi Gmira
**Team:** Abdelghafour Moumad (Programmer) · Mohamed Chadi Gmira (Designer & Artist)

> **For agents:** This document defines the game's design intent. Before proposing a new mechanic or changing a system, verify it aligns with the vision here. Cross-reference [`TDD.md`](./TDD.md) for technical implementation and [`PROJECT.md`](./PROJECT.md) for current implementation status.

---

## 1. Project Summary

| Field | Value |
|---|---|
| **Genre** | 2D Precision Platformer — focused on movement |
| **Universe** | A volcanic world where the last water drop fights to survive |
| **Platform** | PC — Windows 10/11 |
| **Mode** | Solo — linear level progression |
| **Audience** | 13+ — players who enjoy demanding, expressive platformers |
| **References** | *Celeste* · *Ori and the Blind Forest* · *Hollow Knight* |
| **Camera** | 2D side-scrolling, orthographic smooth-follow (`CameraFollow2D`) |

---

## 2. Elevator Pitch

> *"You are the last drop of water in a world ruled by fire. Traverse dangerous volcanic zones with precise movement, wall-tech, and water shots. Reach the next safe point before the environment overwhelms you."*

---

## 3. USP & Key Selling Points

### 3.1 Unique Selling Point
A precision platformer where the player character is a **living, deformable water blob** — its body is simultaneously its health, its ammo, and its identity. Every action costs water. Running out means death.

### 3.2 Key Selling Points (KSP)

| # | Point | Design payoff |
|---|---|---|
| 1 | **Precision controls** | Fast, legible movement — no input delay |
| 2 | **Complete mobility kit** | Move · Jump · Wall Climb · Wall Jump · Slide Dash |
| 3 | **Water shot (normal + charged)** | Offensive and tactical layer (charged = backlog) |
| 4 | **Soft-body feel** | Squash, stretch, and spring physics give organic feedback |
| 5 | **Fire vs. Water identity** | Instantly readable visual contrast — blue blob in an orange world |

### 3.3 Reference Influences

| Reference | Influence |
|---|---|
| **Celeste** | Input legibility, technical demand, tight precision |
| **Ori and the Blind Forest** | Organic movement sensation, visual fluidity |
| **Hollow Knight** | Hostile atmosphere, environmental storytelling |

---

## 4. The 3C — Character, Camera, Controls

### 4.1 Character
**Who:** A conscious, living water drop — small, fragile, organic.

**Abilities (implemented ✅ unless noted):**

| Ability | Input | Notes |
|---|---|---|
| Horizontal move | A/D · Arrows / Left Stick | Accelerated, smoothed |
| Jump | Space / South Button | Ground jump and wall jump |
| Wall Jump (while climbing) | **Space** or **Double-Tap Up/W** | Instant detach and jump |
| Wall Climb | Hold Up or W while on wall | 11.0 effective vertical units/sec (7.5 base + 3.5 flow) |
| Wall Jump | Space or Double-Tap Up/W | Lateral + upward burst |
| Slide Dash | Double-tap direction | Costs 15 water — high-speed burst |
| Shoot | Space / Right Shoulder | Costs 5 water per shot (Space is shared with Jump-detach) |
| Charged Shot | Hold Space | Costs 15 water — 2.5x larger, 50% faster bullet |

### 4.2 Camera
`CameraFollow2D` script — smooth follow via `Vector3.SmoothDamp` in `LateUpdate()`.
- Offset: `(0, +1.2, -10)` — slight upward anticipation
- Optional X/Y axis clamping (configurable per level via Inspector)
- Auto-finds `"WaterBlobPlayer"` if target is unset

### 4.3 Controls Reference

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | A/D or Arrow Keys | Left/Right |
| Jump | **Space** (Ground) / **Up Arrow / W** (Hold to move, Double-tap to jump) | - |
| Slide Dash | Double-tap A or D | L1 |
| Shoot | **Space** · Left Click | R2 / Right Shoulder |
| Charged Shot | Hold **Space** · Hold Click | Hold R2 |

---

## 5. The Water Resource System
*This is the core design loop. Water = Life + Ammo + Energy.*

The player's blob body is made of water. **Every action drains it.** Running out = evaporation = death. This creates a constant tension between aggression (shooting) and survival (conserving).

| Event | Water Change | Implemented |
|---|---|---|
| Shoot (normal shot) | −5 | ✅ |
| Take enemy contact damage | −10 | ✅ |
| Moving (per second) | −2 | ✅ |
| Slide Dash | −15 | ✅ |
| Jump | −12 | ✅ |
| Reach refill/safe zone | → +100 (full) | ✅ (`Refill()`) |
| Lava Contact (continuous) | −20 / sec (bypasses I-frames) | ✅ (`HazardImpact`) |
| Water collectibles in environment | +amount | ❌ Backlog |

> **Design rule:** Any new action that expends effort from the player should have a water cost. Hazards can bypass the default 0.8s invulnerability window to create high-tension zones.

---

## 6. Win / Fail Conditions

### 6.1 Win
Reach the **end zone** (level exit) or a **refill/checkpoint** point. These also restore water to full.

### 6.2 Fail
Any of the following triggers evaporation and respawn at the last checkpoint:
- Fall out of bounds
- `HealthComponent.OnDied` fires (HP reaches 0)
- Water resource reaches 0 *(Game Over loop — currently triggers OnDied but full respawn loop is backlog)*

---

## 7. Hazards & Enemies

### 7.1 Environmental Hazards

| Hazard | Behavior | Implemented |
|---|---|---|
| **Lava zones** | Contact → immediate water loss / death | ✅ (trigger collider + `HazardImpact`) |
| **Out of bounds** | Fall = fail | ✅ |
| **Lava platform surfaces** | Continuous water loss + Sizzle FX | ✅ (`HazardImpact` auto-attached) |

### 7.2 Enemies

| Enemy Type | Behavior | Implemented |
|---|---|---|
| **Vaporizing Patrol Enemy** | Patrols left/right, detects walls & ledges, waits and flips at endpoints | ✅ (FSM: `PatrolState` + `WaitState`) |
| Contact with player | → player takes −1 HP + −10 water; enemy vaporizes | ✅ (`PlayerHurtbox`) |
| Shot by player | → enemy vaporizes + splash FX | ✅ (`BulletRuntimeHandler`) |
| **Chase Enemy** | Detects player, pursues | ✅ (FSM: `ChaseState`) |
| **Attack Enemy** | Deals ranged/melee attack | ✅ (FSM: `AttackState` — basic stance) |

---

## 8. Gameplay Obstacles (Gameplay Bricks)

| Type | Design Purpose | Requires |
|---|---|---|
| **Fixed platforms** | Basic traversal | Move |
| **Narrow ledges** | Precision landing | Move + Jump |
| **Long momentum gaps** | Air mastery | Jump + momentum |
| **Vertical climb shafts** | Wall system mastery | Wall Climb + Jump |
| **Wall-jump corridors** | Multi-wall chaining | Wall Jump |
| **Slide-dash corridors** | Speed mechanical mastery | Slide Dash |
| **Lava pools** | Consequence zone | Any mobility |
| **Projectile interaction points** | Tactical use of shot | Shoot |
| **Safe zones / refill stations** | Tension release, resource restore | — |
| **Checkpoints** | Mid-section safety net | — |

---

## 9. OCR Loops (Objective – Challenge – Reward)

| Loop Type | Scope | Example |
|---|---|---|
| **Micro** | Single obstacle cell | Clear a wall-jump corridor |
| **Mid** | Full section | Reach the checkpoint after a climb shaft series |
| **Macro** | Full level | Complete level, advance to next biome |

---

## 10. Universe & Story

- **World context:** Heat dominates everything. Water is extinct except for the protagonist. Ruins of a once-balanced world are submerged in lava.
- **Protagonist:** A living water drop — last of its kind. No dialogue. The world tells the story.
- **Narration style:** Environmental storytelling — burnt ruins, fractured platforms, lava rising from below.
- **Tone:** Urgent but playful. The blob is small and vulnerable, but quick and expressive.

---

## 11. Graphic Direction

### 11.1 Visual Style
2D stylized art — readable silhouettes, strong contrast, minimal clutter.
- Player: animated water blob with googly eyes (`WaterBlobGooglyEyes2D`) and reactive deformation
- Environment: volcanic rock, lava pools, amber-lit ruins
- FX: lava surface animation (`LavaPlatformVisual2D`), water splash impacts, vaporization particles, hazard sizzling (`HazardImpact`)

### 11.2 Color Palette

| Role | Colors |
|---|---|
| **Dominant (world)** | Orange · Red · Amber (fire / lava) |
| **Player / water** | Blue · Cyan (high contrast against world) |
| **Danger feedback** | Flash red / white — `damageFlashColor` in `PlayerHurtbox` |

### 11.3 Rendering
- **Pipeline:** URP 2D Renderer (Unity 6)
- **Post-processing:** Global volumes set up; centralized profile pending (backlog)
- **Soft-body visual:** `WaterBlobMeshRenderer2D` renders procedural blob mesh driven by `WaterBlobCharacter2D` spring-joint ring

---

## 12. Sound Direction

### 12.1 Music
- Minimalist atmospheric score
- Progressive tension as environmental danger increases
- Calm "breathing room" in safe/refill zones

### 12.2 SFX

| Sound | Trigger |
|---|---|
| Splash / landing | Player lands on ground |
| Water shot | Bullet fired (`PlayerCombatController`) |
| Enemy vaporize | Enemy destroyed (`BulletRuntimeHandler` / `PlayerHurtbox`) |
| Player damage | `HealthComponent.OnTakeDamage` | ✅ (`AudioManager`) |
| Lava ambient loop | Lava zone proximity |
| UI feedback | Menu interactions |

---

## 13. Menus & HUD

- **HUD philosophy:** Minimalist — nothing that obscures gameplay
- **Shot charge indicator:** Visual feedback for charged shot charge-up *(pending charged shot implementation)*
- **Water/HP display:** Should visualize `OneDropWaterResource2D.Current` and `HealthComponent.CurrentHealth`
- **Danger feedback:** Environmental proximity signal when near lava or at low water

---

## 14. Level Design

### 14.1 Global Progression Structure
```
Onboarding → Learning → Combination → Difficulty Peak → Resolution
```
Each level introduces mechanics progressively. No new mechanic is thrown at the player without a "safe" practice space first.

### 14.2 Level 1 — *"Coulée initiale"* (Initial Flow)
- **Biome:** Volcanic ground, first lava introduction
- **Focus:** Basic movement, small gaps, discovering the water resource
- **Mechanics trained:** Move, Jump, basic Shoot

### 14.3 Level 2 — *"Parois brûlantes"* (Burning Walls)
- **Biome:** Vertical shaft sections
- **Focus:** Wall system mastery, pressure corridors
- **Mechanics trained:** Wall Climb, Wall Jump, resource management under pressure

### 14.4 Level 3 — *"Pression magmatique"* (Magmatic Pressure)
- **Biome:** High-rhythm lava surge zones
- **Focus:** Full kit fluency
- **Mechanics trained:** Slide Dash, long momentum gaps, Projectile interaction points

### 14.5 Level Flow Template
```
[Start]
  ↓ Onboarding platform (move + jump)
  ↓ Climb shaft
  ↓ Wall-jump corridor
  ↓ [Checkpoint / Refill]
  ↓ Slide-dash section
  ↓ Projectile puzzle gate
  ↓ [Level Exit]
```

### 14.6 Gameplay Brick Set (Designer Toolbox)

| Brick | Category |
|---|---|
| Fixed platforms, Narrow ledges, Long momentum gaps | Traversal |
| Lava pools, Vertical climb shafts, Wall-jump corridors | Hazard |
| Slide-dash corridors, Projectile interaction points | Advanced tech |
| Safe zones / Refill stations, Checkpoints | Recovery |

---

## 15. Implementation Status (Designer View)

> Full technical status → [`PROJECT.md §6`](./PROJECT.md)

### ✅ Playable Now
- Full movement kit: move, jump, wall-climb (kinetic), wall-jump, slide-dash
- Water resource drain on all actions
- Normal & Charged water shots with enemy vaporization
- Enemy AI: Patrol (wall/ledge detection), Chase (detection/pursuit), Attack (close-range)
- Contact damage (player takes damage + enemy vaporizes on touch)
- Soft-body deformation and googly eyes
- Smooth follow camera
- Lava platform hazards (continuous damage, I-frame bypass)
- Lava background FX
- Centralized AudioManager for all SFX

### ❌ Design Decisions Pending Implementation
| Feature | Design Intent | Status |
|---|---|---|
| Water collectibles | Pools/droplets → `Refill()` | Backlog |
| Post-processing | Advanced visual grading | Backlog |

