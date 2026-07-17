Boomtown v0.4.5 — Fraser River Water

This ZIP contains the COMPLETE consolidated WorldGeneration folder.

INSTALL
1. Close Unity or allow it to finish compiling.
2. Replace:
   Assets/Boomtown/Scripts/WorldGeneration
   with the WorldGeneration folder in this ZIP.
3. Reopen/return to Unity.
4. Let Unity compile and import BT_FraserRiverWater.shader.
5. Generate Hope 1858 once.

THE GENERATOR WILL AUTOMATICALLY CREATE
- a five-strip river mesh instead of a single flat two-edge ribbon
- animated downstream surface motion
- deeper blue-green channel colour
- lighter shallow edges
- shoreline foam
- stronger whitewater in faster/narrower river sections
- procedural normals and highlights
- a BoomtownRiverWater component for future flood/weather control
- the verified finite 2,100 oz HOPE1858 gold budget

NOTES
- This is the first visual water pass, not final production water.
- No external water package is required.
- The shader targets Unity URP.
- The generated river remains driven by RiverData and the existing shared spine.
- If the old river appears brown, regenerate the world so BT_River.mat and the
  river mesh are rebuilt with this package.
