# 2025-09-16 - Score pop animation

**Version:** 1.0.72

## Technical Notes

- `ScoreManager` now exposes `scorePopScale` and `scorePopDuration` (Assets/_Project/Scripts/Systems/Scoring/ScoreManager.cs) to drive a subtle sine-based pop whenever the score increases.
  - The base scale is captured once and reused, so UI layouts stay stable even if the text isn?t scaled at (1,1,1).
  - Pop animation uses unscaled time and eases with a sine wave for a quick lift and settle.
- Existing score updates automatically reuse the new animation; no prefab changes required.

### Unity setup checklist

1. Select the `ScoreManager` in your scene.
2. Adjust `Score Pop Scale` (default 1.08) and `Score Pop Duration` (default 0.22s) as desired.
3. Play the game and watch the score text subtly scale up and return each time points are awarded.

## Player Notes

- Score increases now have a gentle pop to better highlight your progress.
