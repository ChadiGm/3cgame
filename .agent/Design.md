# Design
Last Updated: 2026-03-09
Gameplay and design intent only. Implementation truth lives in `CurrentState.md` and `Architecture.md`.

## Game Summary
ONE DROP is a 2D precision action-platformer where the player is a living water blob navigating a hostile volcanic world.

Core loop:
1. traverse hazards with precise movement
2. spend water to move, fight, and survive
3. recover through safe routing and refill opportunities

## Design Pillars
- Precision controls with readable feedback
- Water as one unified survival and action resource
- Environmental pressure through lava, enemies, timing, and vertical traversal

## Character
- The player is a water blob with expressive, deforming movement.
- Core abilities are run, jump, wall interaction, slide dash, shoot, and charged shot.
- Water should feel like both bodily integrity and tactical resource.

## Camera
- Follow framing should support platforming readability first.
- Camera motion should feel stable, with only enough anticipation to support movement clarity.

## Controls
- Controls should feel responsive and low-friction.
- Input complexity should stay visible and deliberate rather than hidden.
- Keyboard defaults should avoid jump and shoot fighting for the same button.

## Water Resource Design
- Water is the player's survival authority and primary shared gameplay resource.
- Movement, combat, and hazards may all affect the same water pool.
- Refill opportunities define recovery windows and route planning.

Design rule:
- meaningful player actions may cost water
- hazard spaces may bypass normal safety pacing when that increases tension intentionally

## Combat And Enemy Intent
- Combat should pressure movement choices, not replace platforming.
- Enemies should force resource spending, repositioning, and risk tradeoffs.
- Enemy behavior should remain readable even when it becomes more complex.

## Hazards And World Pressure
- Lava and similar environmental hazards are core tension drivers.
- Traversal spaces should teach mechanics, then combine them under pressure.
- Risk/reward routing should become more visible as level complexity grows.

## HUD And UX Intent
- HUD should be minimal, readable, and non-intrusive.
- Water state must stay readable during fast movement and combat.
- Damage and low-resource states should be obvious without adding clutter.

## Win / Fail
- Win: reach exits or progression goals.
- Fail: water depletion or lethal hazard state triggers death and respawn.

## Scope Notes
- This file is design intent only.
- Do not place implementation status, task planning, or migration notes here.
