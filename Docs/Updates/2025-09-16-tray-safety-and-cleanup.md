# 2025-09-16 — Tray Safety + Cleanup

## Technical Notes

- Prevent stray shape in grid at game start:
  - ShapeSpawner now enforces tray spawn points to be outside the gameplay grid.
  - New settings under ShapeSpawner → Tray Safety:
    - `autoKeepTrayOutsideGrid` (default: on)
    - `trayMarginFromGrid` (default: 0.25)
  - Works with both horizontal and vertical tray alignment modes.
- Removed SpriteSpriteManager editor helper:
  - Deleted Quick Setup (Water/Land) helper block and context menu.
  - Keeps runtime component lean and avoids confusion.

Files changed

- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Fields.cs` — added Tray Safety fields.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Layout.cs` — added `KeepTrayOutsideGrid()` invoked after alignment.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpriteManager.cs` — removed editor helper region and context menu.

## Player Notes

- Shapes reliably start in the tray, not on the board.
- No visible changes otherwise.

