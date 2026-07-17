Boomtown v0.6.0.1 — Missing Prospecting Script Repair

CAUSE
The Gameplay/Prospecting folder was replaced without preserving the original
GoldPanningController.cs.meta GUID. Unity therefore kept the old scene
component as Missing (Mono Script).

INSTALL
1. Replace Assets/Boomtown/Scripts/WorldGeneration with this folder.
2. Keep the current compile-correct GoldPanningController.cs in:
   Assets/Boomtown/Gameplay/Prospecting/
3. Let Unity compile.

RUN ONCE
Boomtown > Tools > Repair Prospecting Components

The repair:
- removes missing Mono Script components from prospecting characters
- restores GoldPanningController where GoldInventory and
  ProspectingLocationSensor are present
- assigns the latest generated GeologyData
- assigns the existing GoldPanningUI
- marks the scene dirty so it can be saved

Afterward:
1. Save the scene with Ctrl+S.
2. Regenerate the current district.
3. Confirm the Console says geology was assigned to the panning controller.
