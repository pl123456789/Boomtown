Boomtown v0.5.2 — Gold Vein Foundation

Replace Assets/Boomtown/Scripts/WorldGeneration with the WorldGeneration folder.
Replace Assets/Boomtown/Gameplay/Prospecting/GoldPanningController.cs with the included file.

Adds:
- 1–2 random mineralized mountain regions per seed
- finite quartz veins with location, direction, width, depth and grade
- hard-rock gold statistics in DistrictData
- automatic geology assignment to GoldPanningController
- Boomtown > Debug > Show Gold Mountains and Veins

This checkpoint does not yet replace placer transport. It validates the source layer first.
