# Look Capture & Compare Report — Iteration 0 (Baseline)

**Generated:** 2026-11-02 06:09:55  
**Reference:** `Assets/_Project/References/corridor_reference.png`  

| Metric | Target | Acceptable Range | Shot 1 (Elevator) | Shot 2 (Z=8) | Shot 3 (Door 214) | Shot 4 (Look Back) | Status |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Mean Luminance** | 0.38 | 0.30–0.45 | 0.312 | **0.192** | **0.154** | **0.156** | ❌ FAIL |
| **Pixels < 0.10** | 7% | 4–14% | **23.3%** | **35.5%** | **42.1%** | **37.7%** | ❌ FAIL |
| **Pixels < 0.20** | 27% | 20–36% | 35.0% | **53.4%** | **65.8%** | **73.8%** | ❌ FAIL |
| **95th percentile** | 0.73 | 0.62–0.82 | 0.631 | **0.467** | **0.375** | **0.415** | ❌ FAIL |
| **99th percentile** | 0.85 | 0.00–0.90 | 0.683 | 0.538 | 0.533 | 0.549 | ✅ PASS |
| **Median Hue (lit)** | 189° | 180–200° | **38°** | **38°** | **38°** | **37°** | ❌ FAIL |
| **Median Saturation** | 0.54 | 0.45–0.62 | 0.450 | **0.392** | **0.405** | **0.417** | ❌ FAIL |
| **Mean R** | 0.24 | 0.18–0.29 | **0.372** | 0.223 | **0.176** | 0.182 | ❌ FAIL |
| **Mean G** | 0.41 | 0.35–0.47 | **0.305** | **0.189** | **0.153** | **0.154** | ❌ FAIL |
| **Mean B** | 0.44 | 0.38–0.50 | **0.203** | **0.135** | **0.111** | **0.107** | ❌ FAIL |
| **Floor Luminance** | 0.15 | 0.10–0.20 | **0.030** | **0.028** | **0.061** | **0.025** | ❌ FAIL |
| **Far Corridor End** | 0.12 | 0.07–0.16 | 0.152 | 0.104 | **0.277** | **0.258** | ❌ FAIL |
| **Right Wall** | 0.54 | 0.45–0.63 | **0.275** | **0.122** | **0.294** | **0.190** | ❌ FAIL |
| **Left Wall** | 0.39 | 0.30–0.48 | **0.553** | 0.331 | **0.113** | **0.182** | ❌ FAIL |
| **Center Luminance** | 0.20 | 0.10–0.28 | 0.152 | 0.104 | 0.277 | 0.258 | ✅ PASS |
| **Corner Luminance** | 0.36 | 0.28–0.45 | **0.265** | **0.140** | **0.136** | **0.074** | ❌ FAIL |
| **Film Grain Std** | 0.02 | 0.02–0.03 | **0.008** | **0.006** | **0.005** | **0.005** | ❌ FAIL |

**Overall Compliance:** 17/68 checks (25.0% passed)

### Side-by-Side Captures (Reference Left | Render Right)

- **shot1_elevator:** `LookReports/iteration_0/shot1_elevator.png`
- **shot2_z8:** `LookReports/iteration_0/shot2_z8.png`
- **shot3_door214:** `LookReports/iteration_0/shot3_door214.png`
- **shot4_looking_back:** `LookReports/iteration_0/shot4_looking_back.png`
