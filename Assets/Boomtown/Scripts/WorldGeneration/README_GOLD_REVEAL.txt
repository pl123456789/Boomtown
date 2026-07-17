Boomtown v0.5.1 — Editor Gold Reveal Overlay

COPY THESE FOLDERS INTO:
Assets/Boomtown/Scripts/WorldGeneration/

Files:
Runtime/BoomtownGoldRevealOverlay.cs
Editor/BoomtownGoldRevealOverlayWindow.cs
Shaders/BT_GoldRevealOverlay.shader

USE
1. Let Unity compile.
2. Open:
   Boomtown > Debug > Gold Reveal Overlay
3. Click Auto Detect Generated Data.
4. Choose:
   - Original: where all generated placer gold began
   - Remaining: what has not been mined
   - Recovered: what the player has removed
5. Click Create / Refresh Overlay.
6. Use Remove Overlay before normal gameplay.

The overlay conforms to the terrain and reads the real finite geology arrays.
It does not modify or generate gold.
