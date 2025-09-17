# 2025-09-16 - Native Advanced AdMob Placement

**Version:** 1.0.70

## Technical Notes

- Added `NativeAdView` component (`Assets/_Project/Scripts/Systems/Ads/NativeAdView.cs`).
  - Handles AdMob Native Advanced requests through the Google Mobile Ads `AdLoader`.
  - Exposes `Show`, `Hide`, `LoadAd`, `IsLoaded` and UnityEvent-style callbacks.
  - Supports binding headline/body/advertiser/CTA text plus icon image; auto-switches to AdMob test ID in editor/dev builds when `forceTestIdInEditor` is enabled.
- `AdsBridge` now wires an optional `NativeAdView` reference and offers `LoadNative`, `ShowNative`, `HideNative`, and `IsNativeReady()` wrappers.
- `AdService` mirrors those APIs so gameplay code can stay ignorant of the underlying SDK.
- `AdsInitializer` gains an `autoLoadNative` toggle so native ads can warm up alongside banners/interstitials/rewarded.

### Unity setup checklist

1. Import the latest Google Mobile Ads Unity plugin and ensure the `GOOGLE_MOBILE_ADS` scripting define is set (Project Settings -> Player -> Scripting Define Symbols) and add `ADMOB_NATIVE_ADS` once the native plugin is imported.
2. Drop a `NativeAdView` component on the UI container that should host the ad (for example, a `MainMenu` panel). Optionally set `contentRoot` if the ad lives under a child object.
3. Assign the provided Android ad unit ID `ca-app-pub-9594729661204695/9030134902` in the `androidAdUnitId` field. Leave `forceTestIdInEditor` enabled while testing.
4. Wire the optional bindings:
   - `headlineGraphic`, `bodyGraphic`, `advertiserGraphic`, and `callToActionGraphic` to `TMP_Text` or `Text` components in the layout.
   - `iconImage` to an `Image` that should show the ad icon.
   - `callToActionButton` plus any extra clickable GameObjects to register with AdMob.
5. Call `NativeAdView.Show()` (or `AdService.Instance.ShowNative()`) when the placement becomes visible. The component will auto-request on enable if `autoLoadOnEnable` is left on.
6. If using `AdsInitializer`, leave `autoLoadNative` enabled to prime the placement during boot.
7. Without `ADMOB_NATIVE_ADS`, the component stays in placeholder mode (logs warnings and hides the container) so builds still compile before the native plugin is added.

## Player Notes

- Main menu now surfaces a polished native ad placement. It blends with the UI, but only appears once an ad is ready so the space never feels empty.





