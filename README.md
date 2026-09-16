# The Endless Hallway 🕯️

A tense, atmospheric first-person psychological horror and anomaly-detection experience built in **Unity 6 (URP)**. Inspired by classic psychological horror titles like *P.T.* and *The Exit 8*, players find themselves trapped in an unsettling, looping hotel corridor known as the **Marrow Point Residences**.

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

### 🏚️ The Central Attic Room & Grand Staircase
- **Central Staircase (`X = 0.0m`)**: Centered directly in line with the hallway doorway, 16 grand steps ascend seamlessly into the dark attic above.
- **Bilateral Walkways**: Generous 2.5m-wide paths on both the left and right sides of the central stair opening, allowing complete 360° circulation around the room.
- **Grand Stone Fireplace & Chimney**: Centered on the far North wall directly in front of the stair arrival, featuring a 3D hollow firebox with charred interior, stone surround, and timber mantel.
- **Atmospheric Horror Lighting**:
  - Lit by a single warm 2700K overhead hanging lantern fixture at the ceiling apex with soft shadow casting.
  - High-smoothness wet-look wooden plank floors reflecting ambient light.
  - Pitched timber A-frame roof rafters and near-black ambient corners.
  - Distant beacon light glowing down in the corridor void below the stairs.
- **Environmental Props**: Metal/wood library shelving unit, cluttered study desk, spindle chair, shipping crate, and stained floor rug.

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

## 🏗️ Technical Architecture & Tools

- **Engine Version**: Unity 6 (`6000.3.10f1`)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Editor Utilities**:
  - `AtticRoomBuilder.cs`: Automated builder for the central staircase, bilateral floor paths, balustrades, fireplace, and lighting. Available via menu: **Tools > Endless Hallway > Build Complete Dark Attic Room**.
  - `HotelExpansionBuilder.cs`: Builds and connects expanded hotel rooms (Room 210, Room 212, Utility Room 216). Available via: **Tools > Endless Hallway > Build All Hotel Expansions**.
  - `SceneSetupUtility.cs`: Automated scene validation, light calibration, and camera setup.
  - `RetroUIBuilder.cs`: Retro CRT canvas and menu builder.

---

## 🚀 Future Updates & Roadmap

We have an extensive development roadmap planned for future updates to expand the world of *The Endless Hallway*:

### Phase 1: Gameplay & Anomaly Expansion
- [ ] **Expanded Anomaly Catalog**: Over 50 unique procedural anomalies, including spatial distortions, flickering silhouettes, backwards audio, and shifting wall messages.
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

---

## 📄 License & Credits

Developed with love for horror gaming by **Basit** and pair-programmed with **Antigravity**.  
All rights reserved © 2026.
