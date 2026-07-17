Boomtown v0.5.0 — District Generator 2.0

THIS IS ONE COMPLETE REPLACEMENT FOLDER

Replace:
Assets/Boomtown/Scripts/WorldGeneration

with the WorldGeneration folder in this ZIP.

WHAT IS PRESERVED
- Current procedural canyon terrain
- Shared river spine and river carving
- Fraser River water shader and animated water component
- Terrain painting
- Prototype forest generation
- Bill and Ted spawning and NavMesh build
- Finite gold data and extraction API
- HOPE1858 target of 2,100 finite placer ounces
- Existing panning integration remains compatible

WHAT IS NEW
- DistrictData asset as the persistent generated-district record
- BoomtownWorldGenerationContext shared by the generation coordinator
- Generation stages and per-module reports
- Validation report after every generation
- District statistics: river samples, finite ounces, deposit cells and trees
- Explicit deterministic replay behaviour:
  * Generate Current Seed = same world again
  * Generate New Random District = genuinely different seed and map
- Reserved DistrictData fields for future:
  * watersheds
  * randomized mineralized mountains
  * quartz veins

IMPORTANT
This version does NOT pretend the future watershed and hard-rock mountain
simulation is already complete. It creates the backbone those modules will
plug into while preserving the working world instead of starting over.

VERIFY
1. Let Unity compile.
2. Open Boomtown > World Generator > Open Generator.
3. Generate Current Seed.
4. Confirm a generated asset appears:
   Hope_1858_DistrictData.asset
5. Select it and inspect module reports and validation.
6. Click Generate New Random District and verify the seed and terrain change.
