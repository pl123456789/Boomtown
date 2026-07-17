Boomtown v0.5.3.1 — Debug Menu Cleanup

Replace:
Assets/Boomtown/Scripts/WorldGeneration

IMPORTANT
Copying folders in Windows normally merges them. This patch includes a neutral
BoomtownGoldSourceOverlay.cs file to overwrite the old duplicate menu script.

The final intended Unity menu is:

Boomtown
  Debug
    Gold
      Open Visualizer
      Show Mountains & Veins
      Show Placer Heatmap
      Show Flow Paths
      Hide All Overlays

Removed/deprecated menu entries:
- Gold Reveal Overlay
- top-level Show Gold Mountains and Veins
- top-level Hide Gold Mountains and Veins

After copying:
1. Let Unity compile.
2. Close and reopen the Boomtown menu.
3. If an old duplicate still appears, delete this old file if present:
   Assets/Boomtown/Scripts/WorldGeneration/Editor/BoomtownGoldSourceOverlay.cs
   Then immediately copy the new compatibility version from this patch back
   into the same location.
