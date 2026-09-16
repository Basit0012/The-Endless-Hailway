# The Endless Hallway 🕯️

A tense, atmospheric first-person psychological horror and anomaly-detection experience built in **Unity 6 (URP)**. Inspired by classic psychological horror titles like *P.T.* and *The Exit 8*, players find themselves trapped in an unsettling, looping hotel corridor known as the **Marrow Point Residences**.

---

## 📸 In-Engine Screenshots

### Cathedral Attic — Grand Central Staircase
*Player eye-level view ascending the 16-step central staircase towards the stone fireplace under the soaring 8m A-frame cathedral ceiling*

![Cathedral Concept View](Docs/Screenshots/cathedral_concept.png)

### Ascending the Staircase
*Looking up from midway on the stairs — warm lantern glow, timber A-frame rafters at 8m apex, bilateral walkways visible on both sides*

![Stair Climb View](Docs/Screenshots/stair_climb.png)

### Descending the Stairwell
*Looking down from the upper landing towards the lower hallway entrance — stair railings, stone fireplace flanked by portrait frames*

![Stair Down View](Docs/Screenshots/stair_down.png)

---

## 🎮 Game Overview & Core Mechanics

### 🌀 The Anomaly Loop System
- **Observation is Survival**: Every loop through the corridor presents subtle or overt deviations from reality.
- **Anomaly Detection**: Players must scrutinize paintings, lighting fixtures, door numbers, ambient sounds, and room layouts.
- **Loop Progression**: Identifying an anomaly and taking the correct path advances your escape; failing to notice one resets the loop or triggers hostile encounters with the unseen **Observer**.

### 🏃 First-Person Controller & Interaction
- Smooth first-person character movement with WASD traversal, sprinting, crouching, and responsive mouse look.
- Interactive inspection system (`Examinable`) for picking up and examining hotel logs, room service slips, old newspapers, and cryptic notes.
- Dynamic head bob, footstep audio, and interaction reticles with contextual HUD prompts (`[E] Inspect`).
- **Adventure_Character** asset integrated at eye-height (`Y = 1.65m`) with correct camera rig, InteractionSystem raycast hooked to camera forward vector.

### 🏚️ The Central Attic Room & Grand Staircase
- **Central Staircase (`X = 0.0m`)**: Centered directly in line with the hallway doorway — 16 grand steps (`rise = 0.175m`, `run = 0.28m`) ascending seamlessly into the dark attic above. Smooth invisible movement ramp prevents step-snagging.
- **Bilateral Walkways**: Generous 2.5m-wide paths on both the left and right sides of the central stair opening, allowing complete 360° circulation around the room.
- **Cathedral Ceiling**: A-frame pitched roof with apex at **8.00m**, knee walls at **4.10m**, and horizontal collar tie beams at **7.10m** — providing massive open vertical headroom throughout.
- **Grand Stone Fireplace & Chimney**: Centered on the far North wall directly facing the stair arrival. 3D hollow firebox with charred interior cavity, stone surround pillars, heavy lintel, timber mantel shelf, and chimney shaft rising to the apex.
- **Atmospheric Horror Lighting**:
  - Single warm 2700K overhead hanging lantern at **Y = 6.05m** with soft shadow casting (intensity `6.0f`, range `15m`).
  - High-smoothness wet-look wooden plank floors reflecting ambient light.
  - Pitched timber A-frame roof rafters and near-black ambient corners.
  - Distant beacon light glowing down in the corridor void below the stairs.
- **Environmental Props**: Metal/wood library shelving unit, cluttered study desk, spindle chair, shipping crate, and stained floor rug.
- **Symmetrical Balustrades**: Timber railings with 24 balusters each along the left and right stair sides. Solid fall-prevention colliders on both sides.

---

## 🕹️ Controls

| Action | Primary Key | Secondary / Controller |
|---|---|---|
| **Move** | `W` `A` `S` `D` | Left Stick |
| **Look** | `Mouse Movement` | Right Stick |
| **Sprint** | `Left Shift` | Left Stick Click |
| **Crouch** | `Left Ctrl` / `C` | `B` / Circle |
| **Interact / Inspect** | `E` | `X` / Square |
| **Close Clue / Dismiss** | `E` / `Escape` | `B` / Circle |
| **Pause / Settings** | `Escape` / `Tab` | `Start` / Menu |

---

## 📋 Development History — What Has Been Done

### ✅ v1.0 — Character Integration & Controller Rigging
- **Adventure_Character asset** imported from `Assets/` and rigged to the existing `PlayerController.cs`.
- First-person camera attached at **eye height (`Y = 1.65m`)** matching the character rig.
- WASD movement, mouse look, sprint, and crouch all preserved from existing controller.
- `InteractionSystem` raycast hooked to camera forward vector — no new controller created.
- `LoopManager` and `AnomalyManager` references preserved intact.

### ✅ v2.0 — Main Menu Fix (Buttons & Cursor)
- Diagnosed and fixed non-clickable main menu buttons.
- **Root Cause**: `PlayerController.Update()` was unconditionally setting `Cursor.lockState = CursorLockMode.Locked` and `Cursor.visible = false` every frame — locking the cursor even on the main menu screen.
- Added `isMenuOpen` game-state guard so cursor is only locked while actually in-game.
- Confirmed single `EventSystem` in the main menu scene.
- Confirmed `Canvas` has `GraphicRaycaster` enabled, render mode `Screen Space - Overlay`.
- Buttons resized and anchor/pivot corrected for all five menu items (Continue, New Game, Settings & Accessibility, Credits, Quit to Desktop).

### ✅ v3.0 — Central Staircase & Attic Room Overhaul
- Completely rebuilt `AtticRoomBuilder.BuildCompleteAtticRoom()` from right-side staircase to **centrally-positioned grand staircase**.
- Central staircase at `X = 0.0m`, bilateral walkways `2.5m` wide on each side.
- Stone chimney and fireplace relocated to the **far North wall** directly across from the stair top.
- 3D hollow firebox with charred cavity, stone surround, timber mantel, and portrait frames.
- Lantern fixture hanging from apex with warm 2700K lighting.
- Wet-look reflective plank flooring with procedurally generated normal and gloss mask textures.
- Near-black ambient corners, dark mood, horror atmosphere.

### ✅ v4.0 — Roof Height Elevation & Stair Climbing Fix
- **Bug**: Player character could not climb the stairs.
  - **Root Cause 1**: All 16 step treads and risers had active `BoxCollider` components, causing `CharacterController` to stop at the first vertical riser face.
  - **Root Cause 2**: Roof apex was at `5.85m` with collar ties at `4.90m`, giving cramped overhead feeling as player rose `2.80m` above ground.
  - **Root Cause 3**: Landing slab front edge presented a vertical lip threshold at `Z = 29.28m`.
- **Fix 1 — Visual-Only Steps**: All stair treads, risers, under-supports, stringers, and bulkheads made collider-free (visual geometry only).
- **Fix 2 — Smooth Movement Ramp**: Dedicated `Stair_MovementRamp` invisible ramp starting at `Y = -0.05m` (submerged into ground) and rising at `31.89°` to landing at `Y = 2.80m`.
- **Fix 3 — Landing Transition Pad**: `Stair_LandingPad` collider spans the threshold at `Z = 29.20m – 29.50m` to eliminate any lip.
- **Fix 4 — Cathedral Roof Elevation**:
  - Apex: **8.00m** (was 5.85m)
  - Knee walls: **4.10m** (was 3.35m)
  - Collar ties: **7.10m** (was 4.90m)
  - Hanging lantern: **6.05m** (was 4.35m) with cord from `7.00m`
  - Chimney shaft: extended through `8.10m`
- **Verified** with in-engine CharacterController simulation: `CLIMB SUCCESSFUL` and `DESCENT SUCCESSFUL`.
- **Verified** with overhead raycasts: `NO HIT (infinite open headroom)` on all 16 steps at all X positions.

---

## 🏗️ Technical Architecture & Tools

- **Engine Version**: Unity 6 (`6000.3.10f1`)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Editor Utilities**:
  - [`AtticRoomBuilder.cs`](Assets/_Project/Scripts/Editor/AtticRoomBuilder.cs): Automated procedural builder for the central staircase, bilateral floor paths, balustrades, hollow fireplace, cathedral ceiling, and atmospheric lighting. Menu: **Tools > Endless Hallway > Build Complete Dark Attic Room**.
  - [`HotelExpansionBuilder.cs`](Assets/_Project/Scripts/Editor/HotelExpansionBuilder.cs): Builds and connects expanded hotel rooms (Room 210, Room 212, Utility Room 216).
  - [`SceneSetupUtility.cs`](Assets/_Project/Scripts/Editor/SceneSetupUtility.cs): Automated scene validation, light calibration, and camera setup.
  - [`MasterBuildUtility.cs`](Assets/_Project/Scripts/Editor/MasterBuildUtility.cs): One-click build pipeline for all systems.
  - [`RetroUIBuilder.cs`](Assets/_Project/Scripts/Editor/): Retro CRT canvas and menu builder.
- **Player Scripts**:
  - [`PlayerController.cs`](Assets/_Project/Scripts/Player/PlayerController.cs): First-person movement, sprint, crouch, mouse look with menu-state cursor guard.
  - [`GameManager.cs`](Assets/_Project/Scripts/Core/GameManager.cs): Loop state, anomaly references, scene management.

---

## 🚀 Future Updates & Roadmap

### Phase 1: Gameplay & Anomaly Expansion
- [ ] **Expanded Anomaly Catalog**: Over 50 unique procedural anomalies including spatial distortions, flickering silhouettes, backwards audio, and shifting wall messages.
- [ ] **Interactive Attic Puzzles**: Secret compartment puzzle in the stone chimney, clockwork mechanism behind the framed portraits, and collectible attic keys.
- [ ] **Inventory & Item System**: Flashlight with battery management, camera with polaroid snapshot functionality to document anomalies.

### Phase 2: AI Entity & Threat Mechanics
- [ ] **The Observer AI Overhaul**: State-machine-driven wandering entity with dynamic line-of-sight detection, footstep listening, and hiding spots (under beds, inside wardrobes).
- [ ] **Sanity & Hallucination System**: Prolonged exposure to dark corners and anomalous rooms distorts player vision and induces audio hallucinations.

### Phase 3: Audio & Immersion
- [ ] **Binaural 3D Audio**: Dynamic corridor acoustics with positional occlusion, creaking floorboards, and distant elevator hum.
- [ ] **Adaptive Horror Soundtrack**: Context-sensitive music layers that build tension as the player approaches critical loop thresholds.

### Phase 4: Platform & Quality of Life
- [ ] **Save & Progress Persistence**: JSON-based save system tracking discovered anomalies, best loop streaks, and player statistics.
- [ ] **Accessibility Suite**: Full subtitle customization, screen-shake toggle, high-contrast UI mode, and remappable inputs.
- [ ] **Steam Achievements & Cloud Save Support**.
- [ ] **VR Mode Exploration**: Experimental OpenXR / SteamVR support for total immersion.
- [ ] **Attic Room Extended Content**: Additional hidden rooms accessible via false chimney brickwork, breakable weakened plank floor sections, and a secret sub-attic crawlspace.

---

## 📄 License & Credits

Developed with love for horror gaming by **Basit** and pair-programmed with **Antigravity**.  
All rights reserved © 2026.
