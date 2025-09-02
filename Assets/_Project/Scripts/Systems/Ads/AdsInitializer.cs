using UnityEngine;
using System.Collections;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

[DefaultExecutionOrder(-10000)]
public class AdsInitializer : MonoBehaviour
{
  public static bool Initialized { get; private set; }
  [Header("AdMob App IDs (optional; configured via manifest by plugin)")]
  [SerializeField] string androidAppId;

  void Awake()
  {
  // Initialize AdMob once; safe to call multiple times.
#if GOOGLE_MOBILE_ADS
  MobileAds.Initialize(initStatus =>
  {
    Debug.Log("AdMob initialization complete.");
    Initialized = true;
    OnInitializedCommon();
  });
#else
  Debug.LogWarning("[AdsInitializer] Google Mobile Ads SDK not found. Install the plugin to enable ads.");
  // Still call common to let simulation or future logic run
  Initialized = false;
  OnInitializedCommon();
#endif
  }

  private void OnInitializedCommon()
  {
    // Small delay to ensure Android Activity is fully ready before loading ads
    StartCoroutine(DelayedAutoLoad());
  }

  private IEnumerator DelayedAutoLoad()
  {
    yield return new WaitForSecondsRealtime(0.5f);
    var interstitial = FindFirstObjectByType<InterstitialAd>();
    if (interstitial != null) interstitial.LoadAd();
    var banner = FindFirstObjectByType<BannerAd>();
    if (banner != null) banner.LoadBanner();
    var rewarded = FindFirstObjectByType<RewardedAd>();
    if (rewarded != null) rewarded.LoadAd();
  }
}
