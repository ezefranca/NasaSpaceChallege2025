# Team Caravel to Mars – NASA Space Apps Hackathon 2025

<p align="center">
   <picture>
      <source media="(prefers-color-scheme: dark)" srcset="assets/media/images/Colorway=2-Color%20White.svg">
      <source media="(prefers-color-scheme: light)" srcset="assets/media/images/Colorway=2-Color%20Black.svg">
      <img src="assets/media/images/Colorway=2-Color%20Black.svg" alt="NASA Space Apps 2025 – Caravel to Mars" width="300" />
   </picture>
</p>

> Designing sustainable in‑situ recycling and manufacturing systems for long‑duration Mars missions through an interactive 3D serious game and evaluative simulation layer.

---
## Demo & Teaser Videos
<p align="center">
   <!-- Assumption: replace filenames if different -->
   <picture>
      <source media="(prefers-color-scheme: dark)" srcset="assets/media/images/Colorway=1-Color%20Black.svg">
      <img src="assets/media/images/Colorway=1-Color%20Black.svg" alt="Divider" width="1" height="1" style="opacity:0;" />
   </picture>
   <br/>
   <b>Simulator Walkthrough</b><br/>


https://github.com/user-attachments/assets/9f4b981d-9041-4cb4-b07b-05b957dece01

   <br/><br/>
   <b>Teaser Trailer</b><br/>



Uploading caravelv2.mp4…



</p>

*If the videos do not render on a given platform, open them directly:*
[`Simulator`](assets/media/videos/simulator-screen.mp4) · [`Teaser`](assets/media/videos/teaser.mp4)

---
## 1. Challenge Context
During a hypothetical 3‑year Mars mission (8 crew), an estimated **12,600 kg of inorganic waste** (packaging, textiles, structural polymers, metals, composites) would accumulate. Resupplying virgin material is mass‑expensive; returning trash is inefficient. We explore how mission crews can **close material loops** by:
- Identifying reusable waste streams
- Evaluating candidate manufacturing processes (FDM, reinforcement, forming, remelt, etc.)
- Balancing strength, energy, time, and recyclability
- Training decision‑making through an adaptive simulation + game interface

_Source: NASA Space Technology Mission Directorate – Mars surface recycling systems challenge framing._

---
## 2. Team
**Team Name:** Caravel to Mars (Lisbon, Portugal)

We blend **software engineering**, **human–computer interaction**, and **serious games research** to prototype decision support systems that turn *waste into mission capability*.

| Member | Handle | Location |
|--------|--------|----------|
| Ezequiel França dos Santos | @ezefranca | Portugal |
| Tomás Rodrigues | @tomasd | Portugal |
| Marco António dos Santos Marques | @marcomarques | Portugal |
| Sara Matos | @blumea | Portugal |
| Ana Oliveira | @anao | Portugal |
| Ana Rita Pestana Freitas Correia Moura | @ritamoura | Portugal |

<!-- Collaboration note removed per request -->

---
## 3. System Architecture Overview
```
┌──────────────────────────────────────────────────────────────────┐
│                         MATH / AI LAYER                         │
│──────────────────────────────────────────────────────────────────│
│ • Material property models (strength, melt point, density)       │
│ • Process efficiency & energy equations (FDM, SLS, Bonding)      │
│ • Thermal & mechanical simulation (empirical/AI approximations)  │
│ • Evaluation engine: MfgModels.Evaluate()                        │
│   → Computes strength, energy, time, recyclability metrics       │
│                                                                  │
│        INPUTS:  Task ID, Material ID, Process ID, Parameters      │
│        OUTPUTS: EvalResult {meetsStrength, time, energy, etc.}    │
└──────────────────────────────────────────────────────────────────┘
                │
                │ JSON or internal data exchange
                ▼
┌──────────────────────────────────────────────────────────────────┐
│                     3D SERIOUS GAME SIMULATOR                    │
│──────────────────────────────────────────────────────────────────│
│ • Unity Core Systems                                             │
│   – Player Controller (movement, camera, physics)                │
│   – ManufacturingConsole (loads data & connects math layer)      │
│   – ManufacturingTerminal (interactive UI terminal)              │
│                                                                  │
│ • Gameplay Mechanics                                             │
│   – Waste sorting, recycling, and 3D printing challenges         │
│   – Real-time parameter tuning (mass, temperature, cycles)       │
│   – Immediate feedback overlay (OnGUI / HUD)                     │
│                                                                  │
│ • 3D Visualization                                               │
│   – Martian base environment                                     │
│   – Astronaut avatar & equipment                                 │
│   – Printing station model (dynamic 3D preview of object)        │
│                                                                  │
│ COMMUNICATION: Player input → Evaluator → Visual feedback        │
└──────────────────────────────────────────────────────────────────┘
                │
                │ Evaluation Results
                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    FEEDBACK / TRAINING LAYER                     │
│──────────────────────────────────────────────────────────────────│
│ • Real-time mission feedback (OK / FAIL / Try Again)             │
│ • Display of key metrics: σ, t, E, recyclability                 │
│ • Adaptive hints: suggest better material/process combinations   │
│ • Log of performance for analysis or AI retraining               │
│ • Possible integration with:                                     │
│   – ESA/NASA Human Factors data                                  │
│   – Educational analytics dashboard                              │
└──────────────────────────────────────────────────────────────────┘
```

---
## 4. Core Game Mechanics
| Mechanic | Purpose | Implementation (Current) | Future Extension |
|----------|---------|--------------------------|------------------|
| Waste Object Targeting | Select a part to repurpose | `ManufacturablePart` + raycast controller | Add waste category tagging + wear state |
| Parameter Exploration | Teach trade-offs (process vs strength/time/energy) | Keys to cycle Material/Process, dynamic mass sampling | Multi-variable sliders + process constraints |
| Evaluation | Quantify feasibility quickly | `MfgModels.Evaluate()` returns `EvalResult` | Confidence intervals / degradation curves |
| Immediate Feedback HUD | Reinforces learning loop | Color-coded result line + pass/fail icons | TextMeshPro + accessibility modes |
| Recycling Loops | Highlight diminishing returns | Recycle cycle param affects strength decay | Dynamic polymer aging curves & contamination |
| Reinforcement & Cross-Layer | Show anisotropy & reinforcement benefits | Boolean toggles (future) | Microstructure simulation / AI estimator |
| Progress / Scoring | Encourage optimization | (Planned) Efficiency index = f(Strength margin, Energy, Time, Reuse %) | Mission phase objectives & leaderboard |

### Evaluation Flow
1. Player looks at a recyclable part (waste item or placeholder geometry)
2. Chooses material + manufacturing process candidate
3. System computes: estimated printed/mechanical strength, energy expenditure (Wh), production time (min)
4. Checks against task requirements (min strength, thermal envelope)
5. Feedback: PASS / FAIL + diagnostic metrics
6. Player iterates to minimize time and energy while satisfying constraints

### Learning Objectives
- Understand anisotropy and reinforcement trade-offs
- Appreciate energy/time cost of manufacturing choices on Mars
- Practice selecting recycling pathways that preserve mission mass budget

---
## 5. Data & Simulation Layer
**Manufacturing Database (`manufacturing_db.json`):**
- Materials: baseline strength (MPa), melt point, density, cp, latent heat, classes
- Processes: IDs (e.g., `FDM_FFF`, `SLM`, `Thermal_Bonding`), heuristics & placeholders for refinement
- Tasks: required properties (min strength, temperature window, part context)

**Current Models (Simplified / Placeholder):**
- Strength estimation: base * anisotropy * porosity * recycling decay * (reinforcement multiplier)
- Energy: sensible + latent heating + motion / overhead (FDM) or process constants (SLM / remelt)
- Time: volumetric flow approximation (FDM) or coarse cycle duration heuristics

**Planned Enhancements:**
- Data-driven calibration from literature / NASA tech notes
- Stochastic variation (Monte Carlo) to show uncertainty bands
- Material aging vs. radiation & thermal shock (degradation curves)

---
## 6. Technologies
| Layer | Tech / Approach | Notes |
|-------|-----------------|-------|
| Engine | Unity (URP assumed) | Rapid prototyping & cross-platform |
| Language | C# | Gameplay + simulation glue |
| UI (Prototype) | IMGUI (OnGUI) | Fast iteration; to be replaced with uGUI/TextMeshPro |
| Data | JSON (Resources) | Loaded via `ManufacturingConsole` |
| Simulation | Custom parametric equations | Extensible to ML surrogates |
| Version Control | (Git assumed) | README to be placed at repo root in production |
| Potential ML | Lightweight regression later | For process time/energy fidelity |

---
## 7. NASA / Reference Data Sources
(Used to inform constraints, waste classes, and mission context; parsing & modeling still in-progress)  
1. **Human Exploration of Mars Design Reference Architecture 5.0 Addendum**  
   https://www.nasa.gov/wp-content/uploads/2015/09/373667main_nasa-sp-2009-566-add.pdf?emrc=8b2e96
2. **NASA: Non-Metabolic Waste Categories, Items, Materials, and Commercial Equivalents**  
   https://drive.google.com/file/d/12cxywjd3-mkEroySLN04595gx7K7AVmQ/view?usp=sharing
3. **Waste Materials Recycling for In-Space Manufacturing (NASA Technical Report)**  
   https://ntrs.nasa.gov/citations/20240004496
4. **Mars Exploration Quick Facts (CSA / NASA)**  
   https://www.asc-csa.gc.ca/eng/astronomy/mars/

> These sources guide scenario realism; the current prototype uses simplified, tunable parameters not yet validated against full empirical datasets.

---
## 8. Getting Started (Prototype)
1. Open the Unity project (recommended: 2022 LTS or newer)  
2. Ensure `manufacturing_db.json` resides in `Assets/Resources/` (already present).  
3. Load the test scene containing a `ManufacturingConsole`, `ManufacturingTerminal`, player controller, and sample `ManufacturablePart` objects.  
4. Play Mode Controls (current):  
   - `F` Evaluate candidate  
   - `1` / `2` Cycle material / process (raycast HUD)  
   - Backquote (`) Compact HUD toggle  
   - Terminal (if placed): `T/M/P` cycle Task/Material/Process, arrows adjust mass, `C` cross-layer, `R` reinforce, `F` evaluate

---
## 9. Roadmap
| Phase | Focus | Milestones |
|-------|-------|------------|
| Alpha (Hackathon) | Core loop | Evaluation function, basic HUD, data load |
| Beta | UX & Fidelity | TextMeshPro UI, richer material sets, energy calibration |
| Gamma | Training Depth | Scoring system, adaptive hints, recycling chain progression |
| Extension | ML Surrogates | Data capture -> surrogate regression -> uncertainty surfacing |

---
## 10. Contributing
Currently not seeking external collaborators beyond issue reporting and bug tracking. Please use GitHub Issues for:
- Bug reports / incorrect evaluation logic
- Data accuracy flags (with source citation)
- Non-intrusive polish suggestions (UI clarity, accessibility)

---
## 11. Ethics & Educational Positioning
This project is **exploratory** and **educational**; outputs are *not* intended for direct engineering certification without rigorous validation. We aim to enhance mission literacy, systems thinking, and sustainability awareness.

---
## 12. Disclaimer
NASA Space Apps name and logo belong to their respective owners. This repository is an independent hackathon project and is **not an official NASA product**. All referenced NASA documents are public domain or publicly accessible resources. Any simplifications or assumptions are the responsibility of the team.

---
## 13. License
MIT License – see `LICENSE` file.

```
MIT License

Copyright (c) 2025 Caravel to Mars Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---
## 14. Contact
Primary Contact: GitHub Issues (open a ticket with category: question / bug / data-source)  
Location: Lisbon, Portugal (Team base)

---
*Caravel to Mars – Iterating toward circular resource loops for sustainable exploration.*
