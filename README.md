# The Endless Hallway 🕯️

> A tense, atmospheric first-person psychological horror and anomaly-detection experience built in **Unity 6 (URP)**.  
> Inspired by *P.T.* and *The Exit 8* — players loop endlessly through an unsettling hotel corridor known as **Marrow Point Residences**, hunting anomalies and surviving the Observer.

---

## 📸 Full In-Engine Screenshot Gallery

### 🛗 The USSR Elevator Entrance & Corridor End

| View | Screenshot |
|------|-----------|
| **USSR Elevator Entrance (glTF / URP Lit)** — Authentic vintage Soviet elevator entrance model with extracted 4K PBR textures and URP Lit shaders | ![USSR Elevator Entrance](Docs/Screenshots/ss_25_ussr_elevator_preview.png) |
| **Corridor Entrance** — Looking down from spawn, ceiling lights, door row visible, Observer entity mid-hallway | ![Corridor Entrance](Docs/Screenshots/ss_01_corridor_entrance.png) |
| **Far End / Elevator Bay** — Reverse angle looking back at the elevator bay and crossing Observer | ![Elevator End](Docs/Screenshots/ss_03_corridor_elevator.png) |
| **Low Angle — Horror Atmosphere** — Floor-level shot of the dark corridor with emergency lighting | ![Low Angle](Docs/Screenshots/ss_10_emergency_floor_lights.png) |
| **Room 210 Door** — Left corridor, hotel room door with worn wood panelling | ![Room 210](Docs/Screenshots/ss_08_room210_door.png) |
| **Room 214 Door** — Right corridor, opposing room door | ![Room 214](Docs/Screenshots/ss_09_room214_door.png) |
| **Wall Painting & Cork Noticeboard** — Anomaly-eligible dressing, cryptic message pinboard | ![Wall Painting](Docs/Screenshots/ss_06_wall_painting.png) |
| **Floor Clue** — Fallen incident report document visible on carpet | ![Floor Clue](Docs/Screenshots/ss_07_floor_clue.png) |

---

### 🪜 Staircase & Attic Headroom

| View | Screenshot |
|------|-----------|
| **Staircase Entrance** — From the corridor, looking up into the dark opening of the attic | ![Stair Entrance](Docs/Screenshots/ss_11_stair_entrance.png) |
| **Corridor to Attic** — Low angle looking up at the stone chimney gable framed by stair rails | ![Corridor to Attic](Docs/Screenshots/ss_19_corridor_to_attic.png) |
| **Staircase Full Ascent** — Headroom-cleared 16 steps from upper landing, unobstructed player clearance | ![Staircase Cleared](Docs/Screenshots/ss_22_attic_stairs_cleared.png) |
| **Staircase From Landing** — Looking down 16 steps from the upper landing, dark bilateral walkways visible | ![Staircase Full](Docs/Screenshots/ss_18_staircase_full.png) |

---

### 🏚️ The Restored Attic Room

| View | Screenshot |
|------|-----------|
| **Restored Fireplace & Chimney** — View from top of stairs facing the stone firebox and mantel at original Z=31.35m | ![Fireplace Restored](Docs/Screenshots/ss_21_attic_fireplace_restored.png) |
| **Elevated Attic View** — Full perspective of the 8.8m cathedral ceiling, raised collar ties, and perimeter knee-walls | ![Attic Top View](Docs/Screenshots/ss_23_attic_top_view.png) |
| **Overhead Attic View** — Bird's eye view of the full attic: central stairs, dual walkways, fireplace at far wall | ![Attic Overhead](Docs/Screenshots/ss_12_attic_overhead.png) |
| **West Path — Desk & Shelves** — Cluttered study desk, papers, spindle chair, metal shelving against knee-wall | ![Attic West Path](Docs/Screenshots/ss_13_attic_west_path.png) |
| **East Path — Crate & Rug** — Shipping crate, dark stained rug, near-black corner atmosphere | ![Attic East Path](Docs/Screenshots/ss_14_attic_east_path.png) |
| **Stone Fireplace Close-Up** — Hollow stone firebox, charred interior, timber mantel | ![Fireplace](Docs/Screenshots/ss_15_fireplace.png) |
| **Portrait Frames** — Left portrait flanking the chimney, warm rim-light from lantern | ![Portrait Left](Docs/Screenshots/ss_16_portrait_left.png) |
| **Wide Attic Concept** — Full room: bilateral walkways, railings, fireplace wall far end | ![Attic Wide](Docs/Screenshots/ss_20_attic_wide_concept.png) |

---

### 💡 Cathedral Ceiling & Lantern

| View | Screenshot |
|------|-----------|
| **Lantern & Rafters** — Looking straight up at the hanging 2700K lantern under the elevated 8.8m A-frame apex | ![Lantern Rafters](Docs/Screenshots/ss_17_lantern_rafters.png) |
| **Headroom & Walkways Overview** — Perspective from landing facing chimney, showing 8.8m cathedral headroom, collar ties, and lateral walkways | ![Headroom Profile](Docs/Screenshots/ss_24_attic_headroom.png) |

---

## 🎮 Game Overview

### 🌀 The Anomaly Loop System
- **Observation is Survival**: Every loop presents subtle deviations from reality — study paintings, door numbers, lighting, sounds, and room layouts.
- **Anomaly Detection**: Correctly identifying an anomaly and taking the right path advances your escape. Missing it triggers the Observer.
- **The Observer**: A hostile entity that patrols the corridor — avoid eye contact, don't run when it's watching.

### 🏃 First-Person Controller
- Smooth WASD movement with sprinting, crouching, and responsive mouse look.
- `Adventure_Character` asset integrated at eye height (`Y = 1.65m`) with correct camera rig.
- Interactive inspection system for examining hotel logs, incident reports, cryptic notes.
- Dynamic head bob, footstep audio, interaction reticles with contextual HUD prompts (`[E] Inspect`).

### 🏚️ The Central Attic Room
- **16-step central grand staircase** at `X = 0.0m`, ascending from the corridor into the dark attic.
- **Bilateral walkways** (2.5m each side) allowing full 360° circulation around the stair opening.
- **Cathedral A-frame ceiling**: apex at **8.00m**, knee walls at **4.10m**, collar ties at **7.10m**.
- **Grand Stone Fireplace**: hollow firebox, stone surround, timber mantel, flanked by portrait frames.
- **Single warm lantern** at `Y = 6.05m` casting a tight 2700K pool of light — near-black corners.

---

## 🕹️ Controls

| Action | Key | Controller |
|--------|-----|-----------|
| **Move** | `W A S D` | Left Stick |
| **Look** | Mouse | Right Stick |
| **Sprint** | `Left Shift` | L3 |
| **Crouch** | `Ctrl` / `C` | `B` |
| **Interact** | `E` | `X` |
| **Pause** | `Escape` | `Start` |

---

## 📋 Development History

### ✅ v1.0 — Character Integration
- `Adventure_Character` asset imported from `Assets/` and rigged to `PlayerController.cs`.
- First-person camera at eye height (`Y = 1.65m`) matching character rig.
- `InteractionSystem` raycast hooked to camera forward. `LoopManager` + `AnomalyManager` references preserved.

### ✅ v2.0 — Main Menu Fix
- **Root Cause**: `PlayerController.Update()` unconditionally set `Cursor.lockState = Locked` every frame — killing menu clicks.
- Added `isMenuOpen` game-state guard. Confirmed single `EventSystem` and `GraphicRaycaster`. Fixed button sizing/anchors.

### ✅ v3.0 — Attic Room Overhaul
- Rebuilt `AtticRoomBuilder` with **centrally-positioned grand staircase** at `X = 0.0m`.
- Bilateral walkways, stone chimney on far North wall, wet-look reflective plank flooring.
- Warm 2700K hanging lantern, near-black ambient corners, horror mood.

### ✅ v4.0 — Roof Height & Stair Climbing Fix
- **Bug**: Character stopped at first stair riser (`CharacterController` snagging on 90° collider faces).
- **Fix**: Invisible smooth `Stair_MovementRamp` at `31.89°` replaces individual step colliders.
- **Roof**: Apex elevated to **8.00m** (was 5.85m), giving cathedral headroom throughout.
- **Verified** with automated raycast + physics simulation: `CLIMB SUCCESSFUL`, `DESCENT SUCCESSFUL`.

### ✅ v5.0 — Staircase Headroom & Elevation Calibration
- **Bug**: Character blocked when climbing upper staircase flight due to descending collar ties and low knee walls.
- **Elevation Tuning**:
  - Elevated roof apex height from **8.00m** to **8.80m**.
  - Raised perimeter knee walls from **4.10m** to **4.90m** ($2.10\text{m}$ clearance above floor level).
  - Raised collar tie horizontal beams from **7.10m** to **7.90m**.
  - Extended stone chimney shaft height to **8.90m** at exact original anchor position ($Z = 31.35\text{m}$).
- **Collision Optimization**: Disabled blocky box colliders on rafters and collar ties that protruded into the stair flight path.
- **Verified**: Simulated character controller ascent from $Z = 24.20\text{m}$ to $Z = 30.36\text{m}$ landing with zero blockage.

### ✅ v6.0 — Authentic USSR Elevator Entrance & URP Lit Shader Fix
- **Asset Integration**: Integrated authentic vintage model [`old_ussr_elevator_entrance.glb`](Assets/old_ussr_elevator_entrance.glb) via glTFast (`com.unity.cloud.gltfast` 6.20.0).
- **PBR Textures & Materials**:
  - Extracted 5 authentic 4K textures to `Assets/_Project/Textures/Elevator/`.
  - Built 5 custom `Universal Render Pipeline/Lit` materials with proper Unity 6 / URP 17 `AssetVersion` headers.
- **Root Cause & Fix (Pink Shader)**:
  - Discovered and repaired escaped `{{}}` double-braces in 10 `.meta` files across materials and textures that caused Unity to drop their GUIDs.
  - Eliminated placeholder primitive cubes with null materials (`{fileID: 0}`) that blocked the elevator.
  - Restored cabin walls to native [`M_Elevator_Interior.mat`](Assets/_Project/Materials/M_Elevator_Interior.mat).
- **Editor Tooling**: Created [`ElevatorMaterialFixer.cs`](Assets/_Project/Scripts/Editor/ElevatorMaterialFixer.cs) with one-click menu command: **Tools > Endless Hallway > Fix Elevator Materials (URP Lit)**.

---


## 🏗️ Technical Architecture

- **Engine**: Unity 6 (`6000.3.10f1`) — Universal Render Pipeline (URP)
- **Editor Builders**:
  - [`AtticRoomBuilder.cs`](Assets/_Project/Scripts/Editor/AtticRoomBuilder.cs) — Procedural attic, stairs, fireplace, lighting. Menu: **Tools > Build Complete Dark Attic Room**
  - [`HotelExpansionBuilder.cs`](Assets/_Project/Scripts/Editor/HotelExpansionBuilder.cs) — Hotel rooms 210, 212, 216
  - [`SceneSetupUtility.cs`](Assets/_Project/Scripts/Editor/SceneSetupUtility.cs) — Scene validation and light calibration
- **Player Scripts**:
  - [`PlayerController.cs`](Assets/_Project/Scripts/Player/PlayerController.cs) — Movement, sprint, crouch, mouse look, menu-state cursor guard
  - [`GameManager.cs`](Assets/_Project/Scripts/Core/GameManager.cs) — Loop state, anomaly references, scene management

---

## 🚀 Future Roadmap

### Phase 1 — Gameplay Expansion
- [ ] 50+ unique procedural anomalies (spatial distortions, flickering silhouettes, reversed audio, shifting wall text)
- [ ] Attic puzzles: chimney compartment, clockwork portrait mechanism, collectible keys
- [ ] Flashlight with battery management, camera with Polaroid snapshot to document anomalies

### Phase 2 — AI & Threat
- [ ] Observer AI overhaul: state-machine wandering, line-of-sight detection, footstep listening
- [ ] Sanity & hallucination system: distorted vision, audio hallucinations in dark corners

### Phase 3 — Audio & Immersion
- [ ] Binaural 3D corridor acoustics with positional occlusion, creaking floorboards
- [ ] Adaptive horror soundtrack — context-sensitive tension layers

### Phase 4 — Platform & QoL
- [ ] JSON-based save system (discovered anomalies, best loop streaks)
- [ ] Accessibility suite: subtitles, screen-shake toggle, high-contrast UI, remappable inputs
- [ ] VR Mode (OpenXR / SteamVR experimental)
- [ ] Hidden sub-attic crawlspace via false chimney brickwork

---

## 📄 Credits

Developed by **Basit** with pair-programming assistance from **Antigravity (Google DeepMind)**.  
All rights reserved © 2026.
