# 2025-09-16 — Spawn Polish (Animation, Particles, SFX, Parenting)

## Technical Notes

Enhancements to shape spawn visuals and scene organization:

- New pop-in animation with optional drop and wobble
  - Two-phase scale (overshoot then settle), optional vertical drop, gentle rotation settle.
  - Per-tile staggered fade-in of sprites.
  - Tuning fields added under ShapeSpawner → “Spawn Effects”.
- Optional FX hooks
  - Particle spawn at center and/or per tile (`spawnParticlePrefab`).
  - Spawn SFX routed via `AudioManager` (respects SFX mute) using `AudioShim.PlaySfxAt`.
- Hierarchy parenting
  - Spawned shapes are parented under a single container (default name: `Shap Partial`).
  - ShapeSpawner can auto-create or use a provided `shapesParent` Transform.

Files changed

- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Fields.cs`
  - Added FX, wobble/drop, fade-in, and hierarchy fields.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Utilities.cs`
  - Rewrote `SpawnEffect(...)`; added particle and cleanup helpers; SFX via `AudioShim`.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Spawn.cs`
  - Parent newly spawned shapes under `shapesParent`.
- `Assets/_Project/Scripts/Systems/Spawning/ShapeSpawner/ShapeSpawner.Lifecycle.cs`
  - `EnsureShapesParent()` creates/finds the parent container on enable/start.
- `Assets/_Project/Scripts/Audio/AudioManager.cs`
  - Added SFX mute state and `PlaySfxAt(...)`; persisted `Audio.SfxMuted`.

Inspector setup

- ShapeSpawner
  - Spawn Effects: adjust `spawnEffectDuration`, `maxSpawnScale`, `spawnFadeIn`, `spawnTileStagger`, `spawnWobble`, `spawnDrop`.
  - Spawn Effects (FX): assign `spawnParticlePrefab`, toggle center/per-tile, set `spawnSfx`.
  - Hierarchy: set `shapesParent` or keep auto-create (default name `Shap Partial`).
- Audio
  - Optionally add a UI toggle to call `AudioManager.Instance.SetSfxMuted(bool)`.

## Player Notes

- New, livelier spawn animations: shapes pop in with a satisfying bounce.
- Subtle drop and sparkle effects on spawn; soft sound cue (respects mute).
- Cleaner scene organization (no impact to gameplay).

