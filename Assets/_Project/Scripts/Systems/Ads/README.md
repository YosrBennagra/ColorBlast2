# Unity Ads Integration Guide

## Overview
Your Unity Ads system is now fully integrated with real Unity Ads SDK. The system includes:

- **AdsInitializer**: Handles Unity Ads initialization and auto-loads all ad types
- **InterstitialAd**: Component for interstitial ads with auto-retry logic
- **BannerAd**: Component for banner ads with auto-retry logic  
- **RewardedAd**: Component for rewarded ads with auto-retry logic
- **AdsBridge**: Singleton API for easy game integration
- **AdsTester**: Test utility to verify ad functionality

## Setup Instructions

### 1. Add Components to Scene
Add these components to a GameObject in your main scene:
- `AdsInitializer` (handles initialization)
- `InterstitialAd` (for interstitial ads)
- `BannerAd` (for banner ads) 
- `RewardedAd` (for rewarded ads)
- `AdsBridge` (singleton API)

### 2. Configure Game IDs
In the `AdsInitializer` component:
- Set your **Android Game ID** 
- Set your **iOS Game ID**
- Toggle **Test Mode** on/off

### 3. Configure Ad Unit IDs
In each ad component, set the appropriate ad unit IDs:
- **InterstitialAd**: Set Android/iOS Interstitial ad unit IDs
- **BannerAd**: Set Android/iOS Banner ad unit IDs
- **RewardedAd**: Set Android/iOS Rewarded ad unit IDs

## Usage in Code

### Simple API via AdsBridge
```csharp
// Check if ads are ready
bool interstitialReady = AdsBridge.Instance.IsInterstitialReady();
bool bannerShowing = AdsBridge.Instance.IsBannerShowing();
bool rewardedReady = AdsBridge.Instance.IsRewardedReady();

// Show interstitial ad
AdsBridge.Instance.ShowInterstitial(() => {
    Debug.Log("Interstitial completed");
});

// Show/hide banner
AdsBridge.Instance.ShowBanner();
AdsBridge.Instance.HideBanner();

// Show rewarded ad
AdsBridge.Instance.ShowRewarded((success) => {
    if (success) {
        // Give reward to player
        Debug.Log("Player earned reward!");
    }
});
```

### Direct Component Access
You can also access the ad components directly:
```csharp
var interstitial = FindFirstObjectByType<InterstitialAd>();
if (interstitial != null && interstitial.IsLoaded) {
    interstitial.ShowAd();
}
```

## Key Features

### Auto-Retry Logic
- All ad components automatically retry loading if they fail
- Failed loads retry after 5 seconds
- Ads auto-reload after being shown

### Event System
Each ad component exposes events for monitoring:
```csharp
interstitialAd.OnLoaded += (adUnitId) => Debug.Log("Ad loaded");
interstitialAd.OnFailedToLoad += (adUnitId, error, message) => Debug.Log("Load failed");
interstitialAd.OnShowCompleteEvent += (adUnitId, state) => Debug.Log("Show complete");
```

### Simulation Fallback
`AdsBridge` can simulate ads when real ads aren't available:
- Enable "Simulate When Not Ready" in AdsBridge
- Useful for testing without real ad setup

## Testing

### Using AdsTester
1. Add `AdsTester` component to a GameObject
2. Connect UI buttons and text to the component
3. Use buttons to test each ad type
4. Check status text and console logs

### Real Ads Testing
1. Set **Test Mode = false** in AdsInitializer
2. Use real Game IDs and Ad Unit IDs from Unity Ads Dashboard
3. Test on device (ads don't work in editor)
4. Check Unity console for detailed logging

## Integration with GameOverManager

Your `GameOverManager` is already updated to use the new system:
```csharp
// Shows interstitial ad on game over
AdsBridge.Instance.ShowInterstitial(() => {
    // Continue with game over flow
});
```

## Troubleshooting

### Compilation Issues
- All components use reflection to avoid cross-references
- If you see "RewardedAd not found" errors, Unity will resolve them after first compilation

### Ads Not Loading
1. Check Game ID and Ad Unit ID configuration
2. Verify internet connection
3. Check Unity Ads Dashboard for ad availability
4. Enable test mode for debugging

### Real Ads Not Showing
1. Disable test mode
2. Use production Game IDs
3. Test on actual device (not editor)
4. Check Unity Analytics integration

## Best Practices

1. **Initialize Early**: Add AdsInitializer to your first scene
2. **Check Readiness**: Always check if ads are ready before showing
3. **Handle Failures**: Provide fallback behavior if ads fail
4. **Respect User**: Don't show ads too frequently
5. **Test Thoroughly**: Test both simulation and real ads

## File Structure
```
Assets/_Project/Scripts/Systems/Ads/
├── AdsInitializer.cs      # Initialization & auto-loading
├── InterstitialAd.cs      # Interstitial ad component
├── BannerAd.cs           # Banner ad component  
├── RewardedAd.cs         # Rewarded ad component
├── AdsBridge.cs          # Unified API
├── AdsTester.cs          # Testing utility
└── AdService.cs          # Legacy (unused)
```

Your ads system is now ready for production use with real Unity Ads!
