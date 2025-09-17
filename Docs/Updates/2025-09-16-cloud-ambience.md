# 2025-09-16 - Cloud ambience layer

**Version:** 1.0.73

## Technical Notes

- Added `CloudAmbience` component (`Assets/_Project/Scripts/Systems/Ambience/CloudAmbience.cs`).
  - Spawns a configurable number of cloud sprites, drifts them across the screen, and recycles them for seamless ambience.
  - Supports random sprite selection, speed, scale, optional horizontal flip, tint, and sorting layer controls.
  - Works entirely in local space so you can parent it under a world or UI transform.

### Unity setup checklist

1. Create an empty GameObject (for example `Clouds`) in the scene where you want ambience.
2. Add the `CloudAmbience` component and assign your cloud sprites to the list.
3. Adjust `Horizontal Span` to cover the visible width (default 18 units for the sky strip).
4. Tweak `Height Range` (defaults 2?6) so clouds stay in the sky band, along with `Scale Range` (defaults 0.45?0.75), `Speed Range`, and `Recycle Padding`. 
5. Set `Sorting Layer` and `Order` so clouds render behind gameplay (the default order of 20 renders above most tilemaps; lower the value if you want them behind).
6. Press play - the clouds will animate automatically and re-use sprites once they wrap past the right edge.

## Player Notes

- Background now features gentle drifting clouds to keep the scene feeling alive.







