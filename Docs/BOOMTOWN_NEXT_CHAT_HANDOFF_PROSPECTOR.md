# Boomtown — Next Chat Handoff

## Current milestone

**Achievement: Living Rivers and Regional Forest Foundation**

Boomtown now has a working procedural simulation pipeline that generates terrain, geology, rivers, hydrology, placer-gold movement, prospecting data, and a region-aware forest.

### Hydrology completed

- Connected river sample points
- River slope
- Width and depth
- Velocity
- Bend classification
- Sediment transport capacity
- Deposition potential
- Simple downstream placer-gold transport
- Hydrology debug visualization

This marks the transition from a world that is merely generated to a world that begins to behave.

### Forest completed for now

- Five regional species:
  - Douglas-fir
  - Western red cedar
  - Western hemlock
  - Lodgepole pine
  - Engelmann spruce
- Species selection influenced by elevation, wetness, and slope
- Deterministic tree age, height, diameter, timber quality, merchantable length, and board-foot estimates
- Up to 1,200 generated trees
- Finished procedural tree prefabs are built before forest spawning
- Each generated tree uses two renderers:
  - Trunk
  - Combined canopy
- Branch-fan canopy architecture provides species-specific silhouettes while remaining lightweight

Forest work is considered good enough for the current prototype. Future improvements can include textures, LODs, dead trees, stumps, logging states, and stronger individual variation.

## Latest tree milestone

Latest tree commit at handoff:

`66bc6dd — v0.9.10 - Add clustered procedural branch architecture`

The procedural forest pipeline is now functioning correctly. Generated trees visibly use the new meshes rather than the earlier sphere or stacked-cone prototypes.

## Known items to revisit

1. Unity occasionally reports a BatchRendererGroup/GPU Resident Drawer error after rebuilding generated mesh assets:

   `A BatchDrawCommand was submitted with an invalid Batch, Mesh, or Material ID.`

   This appears related to Editor rendering state while procedural mesh assets are replaced. It should be investigated separately from tree appearance.

2. The World Generator editor window has previously shown:

   `EndLayoutGroup: BeginLayoutGroup must be called first.`

   This is an editor GUI layout issue and should receive its own small fix.

3. Rare procedural terrain pits have been observed. Suspected cause is an invalid or extreme terrain height sample. Add defensive NaN/infinity validation and suspicious-neighbour checks later.

## Next development step

**Briefly begin the prospector character model pipeline.**

The starting brothers are:

- William Parsons — Bill — player-controlled founder
- Theodore Parsons — Ted — first employee and partner

Recommended immediate scope:

1. Define the modular prospector body structure.
2. Create a simple game-ready Bill prototype.
3. Reuse the same rig and body system for Ted and future NPCs.
4. Keep character assets modular:
   - head
   - hair and facial hair
   - hat
   - shirt or coat
   - trousers
   - boots
   - belt and equipment
5. Preserve the existing player, employee selection, camera, locomotion, waypoint, and prospecting systems.

Do not replace gameplay scripts while introducing the model. Swap the visual child under the existing Bill and Ted controller roots so current behaviour remains intact.

## Git check requested

Before substantial new work, verify:

- active local branch
- upstream tracking branch
- local status is clean
- recent commit chain includes v0.9.8, v0.9.9, and v0.9.10
- remote contains the same HEAD
- `v0.9.0-alpha` achievement tag exists remotely

Useful local commands:

```bash
git status
git branch -vv
git log --oneline --decorate -12
git remote -v
git tag -n
```

## Achievement note

> **Living Rivers and Regional Forest Foundation** — Boomtown now procedurally generates not only terrain and content, but interacting natural systems. Rivers calculate flow behaviour and placer deposition, while regional forests generate species, age, timber yield, and lightweight species-specific tree forms. The next production focus begins with the modular prospector character pipeline.
