Boomtown v0.6.0.2 — Automatic Scene Wiring

Replace:
Assets/Boomtown/Scripts/WorldGeneration

CHANGE
The manual menu item:
Boomtown > World Generator > Auto Populate Generated Data

has been removed.

Scene wiring is now an automatic world-generation stage:

1. Terrain and world modules
2. Hydrology
3. Geology and finite gold
4. Scene wiring
   - RiverData -> ProspectingLocationSensor
   - repair missing prospecting components
   - GeologyData -> GoldPanningController
   - refresh editor/debug references
5. Validation

USAGE
Only use:
Boomtown > World Generator > Open Generator

Then choose:
- Generate District (Current Seed)
- New Random District

You should never need to assign RiverData or GeologyData manually.
