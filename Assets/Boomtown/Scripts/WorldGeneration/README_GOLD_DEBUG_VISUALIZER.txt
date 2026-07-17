Boomtown v0.5.3 — Gold Debug Visualizer

Replace:
Assets/Boomtown/Scripts/WorldGeneration

Open:
Boomtown > Debug > Gold Debug Visualizer

Controls:
- Show Gold Mountains and Veins
- Show Placer Heatmap
- Show Approximate Gold Flow Paths
- Hide All Gold Overlays

The flow paths are diagnostic approximations from each quartz vein to the
nearest generated RiverData sample. They do not yet alter placer generation.

The placer heatmap reads the real finite geology arrays.
The source overlay reads the real generated mineralized mountains and veins.
