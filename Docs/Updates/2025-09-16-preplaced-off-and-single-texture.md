# 2025-09-16 — Pre-placed Tiles Off + Single Texture

## Technical Notes

- Adventure pre-placed tiles disabled by default:
  - `AdventureManager.usePrePlacedTiles` added (default: false).
  - Pre-placed tiles in level data are ignored unless explicitly enabled.
  - Prevents starting boards from containing tiles at positions like (4,4), (3,4), (3,5).

- SpriteTheme simplified to a single texture per shape:
  - `ShapeSpriteManager.ApplyThemeToRenderer` now always uses `theme.tileSprite`.
  - `tileSprites` and `randomizeTileSprites` are hidden and unused (kept only for asset compatibility).
  - Works with `uniformSpritePerShape` to render the whole shape as a consistent repeated texture.

Files changed

- `Assets/_Project/Scripts/Adventure/AdventureManager.cs`: added `usePrePlacedTiles` gate.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpriteManager.cs`: hide unused list fields; always use `tileSprite`.

## Player Notes

- New games start with a clean board (no tiles pre-placed).
- Shape visuals use one consistent texture for a clear, cohesive look.

