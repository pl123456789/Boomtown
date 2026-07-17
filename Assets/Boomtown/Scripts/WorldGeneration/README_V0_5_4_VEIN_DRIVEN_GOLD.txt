Boomtown v0.5.4 — Vein-Driven Gold

COMPLETE WORLDGENERATION REPLACEMENT

Replace:
Assets/Boomtown/Scripts/WorldGeneration

Also replace the included:
Assets/Boomtown/Gameplay/Prospecting/GoldPanningController.cs

WHAT CHANGED
- Removed fixed 1,000–3,000 oz district scaling
- Removed the HOPE1858 fixed 2,100 oz override
- Removed fallback placer deposits
- Every placer ounce now originates in a generated quartz vein
- Historical weathering permanently removes that mass from hard-rock reserves
- Coarse gold settles near its source
- Fine gold moves farther
- Flour gold can travel through most of the district
- Trace finite gold settles throughout downstream reaches
- Hydraulic traps use velocity, depth, gravel, bank type and river curvature
- Placer cells retain the source quartz-vein ID
- Debug flow paths now follow the actual river downstream from each vein entry

WHAT DID NOT CHANGE
- Terrain generation
- River and water
- Forest placeholders
- Bill and Ted spawning
- Panning UI and inventory
- Finite extraction API
- District seed replay system
- Debug visualizer menu

VERIFY
1. Let Unity compile.
2. Generate Current Seed.
3. Console should mention:
   - mineralized mountains
   - quartz veins
   - historically weathered ounces
   - finite placer ounces
   - remaining hard-rock ounces
4. Open Boomtown > Debug > Gold > Open Visualizer.
5. Show Mountains & Veins, Placer Heatmap, and Flow Paths.
6. Generate a New Random District and confirm all three change.

This is the first version where the source layer actually creates the placer
layer. It intentionally does not simulate tributary meshes yet; each vein
enters the closest main-river sample and then follows generated RiverData.
