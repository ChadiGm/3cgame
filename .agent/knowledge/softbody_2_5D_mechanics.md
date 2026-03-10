# 2.5D Softbody Platformer Mechanics & Best Practices

For a 2D/3D (2.5D) platformer featuring a softbody (slime/water) player, development is split into three core pillars: **Movement**, **Deformation**, and **Perspective**.

---

## 1. 🧠 Core Character Movement (The "Brain")
The underlying character controller should ideally be separated from the visual "squishy" mesh. 

### Approach A: Rigidbody (Recommended for Physics Heavy Games)
- Use a central `Rigidbody` (or `Rigidbody2D`) for the player's core mass.
- Move using `AddForce()` or directly manipulating `velocity` in `FixedUpdate()`.
- **Pros:** Interacts naturally with world physics (bouncing, pushing objects, gravity).
- **Cons:** Can feel "floaty" or "slidey" without careful friction/drag tuning. Requires freezing Z-position and X/Y rotations to keep the 2D plane locked.

### Approach B: Unity CharacterController
- Used for kinematic movement (ignores regular physics, moves via `Move()` method).
- **Pros:** Snappy, precise platformer controls (instant stops, exact jump heights).
- **Cons:** Cannot naturally push physics objects or be pushed by them (must be scripted manually via `OnControllerColliderHit`).

> [!TIP]
> **Best Practice:** Keep the physics/movement logic on an invisible parent object. Let the softbody visual mesh be a child that follows and reacts to the parent's velocity and collision events.

---

## 2. 💧 Softbody Physics & Deformation (The "Squish")
There are several ways to make a character feel like fluid or slime.

### Technique 1: Sprite Skinning & Spring Joints (2D Native - Current 3cgame Approach)
1. Use the **2D Animation** package to rig a sprite with bones around the perimeter and one in the center.
2. Add a `Sprite Skin` component and generate bone GameObjects.
3. Attach a `Rigidbody2D` and `CircleCollider2D` to each perimeter bone.
4. Connect all perimeter bones to their neighbors (and the center bone) using `SpringJoint2D`.
5. **Tuning:** Adjust `Frequency` (stiffness) and `Damping Ratio` (bounciness) on the springs to get the perfect slime jiggle.

### Technique 2: 2D Sprite Shape
- Use Unity's **2D Sprite Shape** package to create a dynamic outline.
- Write a script to manipulate the shape's "control points" (vertices) in real-time based on the character's velocity or collision impact.
- **Example:** When moving right, pull the left control points further left to create a "stretching/trailing" effect.

### Technique 3: Shader-Based Deformation (High Performance)
- Use **Shader Graph** to visually warp the character mesh using a Vertex Shader.
- Pass the character's velocity vector into the shader to stretch the mesh in the direction of travel (Squash and Stretch).
- Pass collision points into the shader to create a ripple or indent effect entirely on the GPU.
- **Pros:** Highly performant, requires no complex physics joints.

### Technique 4: Custom Verlet Integration (The "Elegant" Code Approach)
- Bypass Unity's built-in Rigidbody/Joint system entirely for the softbody envelope.
- Write a custom script simulating points and sticks (Verlet Integration).
- **Pros:** Massively more stable than Unity Spring Joints at high speeds. No "jitter" or fighting with the PhysX solver. Gives extremely precise control over volume preservation.
- **Cons:** Requires custom collision detection against the environment (often done via quick Raycasts or CircleCasts per point).

---

## 3. 🎥 2.5D Visuals & Environment (The "Illusion")
Blending 2D gameplay with 3D depth.

### Camera Setup
- **Cinemachine:** Use a Virtual Camera constrained to X/Y movement, ignoring Z-depth changes.
- **FOV / Orthographic:** 
  - *Perspective Camera (Low FOV):* Keeps 3D depth but minimizes distortion at the edges of the screen.
  - *Orthographic Camera:* Completely flattens depth; sprites won't scale based on distance. Good for retro styling.

### Lighting & Depth
- Give 2D sprites a 3D physical presence (e.g., normal maps) so they react to 3D lighting dynamically.
- Use Post-Processing: Depth of Field (blurring the deep background), subtle Bloom, and God Rays dramatically enhance the 2.5D feel.
- **Foreground objects:** Place out-of-focus elements closer to the camera than the player to create a parallax effect.

### Level Design
- **Whiteboxing:** Build the levels using **ProBuilder** in 3D first to test jump heights and pacing before applying art.
- Restrict player movement completely on the Z-axis using Rigidbody constraints.

---

## ⚙️ Softbody Troubleshooting & Optimization

When using the **SpringJoint2D** approach, physics instability is the biggest hurdle.

### Fixing Collision Bugs (Tunneling / Passing through walls)
- **High Speed Tunneling:** If the softbody points pass through walls during fast movement (dashes/falls):
  1. Set the Core and Point Rigidbodies' `Collision Detection` to **Continuous**.
  2. Decrease `Fixed Timestep` in Project Settings (e.g., from `0.02` to `0.01` or `0.005`) for higher physics fidelity.
- **Spring Oscillation Tunneling:** If the springs are too bouncy, a point might get shoved through a wall and get stuck on the other side.
  - **Fix:** Increase the `Damping Ratio` on the SpringJoint2Ds to reduce violent snapping, or enforce a maximum velocity on the point Rigidbodies.
- **Joint Separation:** If the blob hits a sharp corner and splits open, ensure the ring colliders slightly overlap so there's no gap for sharp geometry to wedge into.

### Optimization Strategies (Getting 60+ FPS)
Softbodies are notoriously CPU heavy because every point is a Rigidbody constantly calculating collisions.
- **Reduce Point Count:** A blob rarely needs more than 8-12 points around the perimeter. Diminishing visual returns kick in heavily past 16 points, but physics costs scale linearly.
- **Simple Colliders Only:** Never use PolygonCollider2D for softbody points. Stick exclusively to `CircleCollider2D`.
- **Prebake Collision Meshes:** In Player Settings, enable `Prebake Collision Meshes`.
- **Layer Collisions:** Go to `Edit > Project Settings > Physics 2D` and uncheck the matrix box where the "BlobPoint" layer intersects with itself. **Blob points should never physically collide with each other**, only rely on the SpringJoints to keep them apart.
- **Disable Auto Sync Transforms:** In Physics Settings, turn off `Auto Sync Transforms`. This prevents Unity from rebuilding the physics world every time a transform changes outside of FixedUpdate.

---

## Summary of Synergies
For a slime character, the **Sprite Skinning + SpringJoint2D** method combined with a central invisible **Rigidbody** creates a highly dynamic softbody that can squeeze through gaps, bounce naturally, and squash on impact. By placing this in a **2.5D lit environment** with **Cinemachine** locking the axis, you get modern, tactile gameplay with massive visual depth. By aggressively paring down the Rigidbody point count and tuning Continuous collision, you maintain high performance.
