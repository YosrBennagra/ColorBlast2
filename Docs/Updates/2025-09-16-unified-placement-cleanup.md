# 2025-09-16 — Unified Placement Visuals: Cleanup

## Technical Notes

- Placement animation now always treats a shape as one unit.
  - Kept `PlacementAsOneCoroutine` (root-scale pop + ring/ripple).
  - Removed legacy per-tile placement variants and settings:
    - Removed: Cascade, Simple (per-tile), Orbit, Magnetic modes.
    - Removed: per-tile spark FX helper.
    - Removed: enum/config fields for legacy modes (stagger, orbit distances, etc.).
- Uniform sprite per shape retained (single repeated tile across all blocks) via `uniformSpritePerShape`.

Files changed

- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpriteManager.cs`
  - Simplified `PlayPlacementAnimation` to always use unified coroutine.
  - Deleted unused per-tile animation coroutines and spark helper.
  - Removed unused placement mode fields.

Why

- Consistent, cohesive feel: shapes appear and place as a solid unit.
- Less code and fewer toggles to maintain.

## Player Notes

- Shapes now place as one piece with a clean, unified animation.
- Visuals are more consistent and easier to read.

