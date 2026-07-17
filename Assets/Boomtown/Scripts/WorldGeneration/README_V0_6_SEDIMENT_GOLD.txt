Boomtown v0.6.0 — Sediment Gold Foundation

This is a complete WorldGeneration replacement.

Professional model:
- Quartz veins remain the only source of gold.
- Historical weathering releases finite gold from those veins.
- Transport separates coarse, fine and flour gold.
- River traps also accumulate sand, gravel and black sand.
- Compatibility placer arrays are rebuilt from the three gold classes.
- Existing panning remains compatible.
- Extraction depletes the particle-class inventories proportionally.
- No fixed district target.
- No fallback gold.
- No gold creation during extraction.

This is intentionally a deterministic sediment FIELD, not millions of
individual GameObjects or physics particles. It provides the behaviour and
data needed for panning, sluices, rockers, dredges and future floods without
unacceptable CPU or save-file costs.

Install:
1. Replace Assets/Boomtown/Scripts/WorldGeneration.
2. Replace GoldPanningController.cs with the included gameplay file.
3. Let Unity compile.
4. Generate Current Seed.
5. Verify the Console and Gold Debug Visualizer.
