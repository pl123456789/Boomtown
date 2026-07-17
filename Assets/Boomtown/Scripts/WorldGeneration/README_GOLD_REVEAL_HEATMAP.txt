Boomtown v0.5.1.1 — Gold Reveal Heatmap Upgrade

Copy into:
Assets/Boomtown/Scripts/WorldGeneration/

This patch replaces the literal cell display with a readable end-game heatmap.

New controls:
- Reveal Spread: expands each real deposit into a visual halo only
- Minimum Visible Alpha: ensures low-grade areas remain readable
- Stronger logarithmic contrast
- Higher default surface offset to reduce terrain overlap
- Higher default opacity and intensity

IMPORTANT:
The overlay spread is visual only.
It does not move, add, or alter any gold.

Recommended starting settings:
Opacity: 0.82
Intensity: 4.0
Reveal Spread: 5
Minimum Visible Alpha: 0.18
