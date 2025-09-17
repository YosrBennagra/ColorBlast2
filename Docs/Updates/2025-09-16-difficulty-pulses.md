# 2025-09-16 — Dynamic Difficulty Pulses

## Technical Notes

Added a lightweight difficulty “mood” system to vary set composition:

- Moods: Relaxed, Balanced, Tricky, Hard.
- A mood persists for a few sets (configurable) and biases selection:
  - Relaxed: promote highly placeable shapes (more options).
  - Balanced: default adaptive composition.
  - Tricky: uses challenge sets (two shapes’ best placements overlap).
  - Hard: injects a scarce (low-placeability) shape.
- Uses existing board-aware scoring and variant logic to remain context-sensitive.

Files changed

- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Fields.cs`
  - Added Difficulty Pulses config and runtime state.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Selection.cs`
  - Mood selection, set adjustment, placeability scoring helpers.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Spawn.cs`
  - Hook mood lifecycle around each spawn.

Inspector setup

- ShapeSpawner → Difficulty Pulses
  - enableDifficultyPulses (default: on)
  - minSetsPerMood / maxSetsPerMood (e.g., 2–5)
  - moodWeights (Relaxed, Balanced, Tricky, Hard)

## Player Notes

- Gameplay now varies between easier and tougher sets.
- Some sets encourage quick play; others require more planning.
- Occasional “thinky” moments where two pieces compete for the same spots.

