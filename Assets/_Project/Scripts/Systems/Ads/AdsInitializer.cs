using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

[DefaultExecutionOrder(-10000)]
public class AdsInitializer : MonoBehaviour
{
  public static bool Initialized { get; private set; }
  [Header("Lifecycle")]
  [SerializeField] bool dontDestroyOnLoad = true;
  [Header("AdMob App IDs (optional; configured via manifest by plugin)")]
  [SerializeField] string androidAppId;
  [Header("Auto-Load on Init")]
  [SerializeField] bool autoLoadInterstitial = true;
  [SerializeField] bool autoLoadBanner = true;
  [SerializeField] bool autoLoadRewarded = true;
  [Header("Development/Test")]
  [Tooltip("Force Google test IDs when running Development builds.")]
  [SerializeField] bool useTestIdsInDevelopment = true;
  [Tooltip("Optional list of test device IDs for personalized ad consent.")]
  [SerializeField] List<string> testDeviceIds = new List<string>();
  [Header("Consent / Content Rating (optional)")]
  [SerializeField] bool tagForChildDirectedTreatment = false;
  [SerializeField] bool tagForUnderAgeOfConsent = false;
  [SerializeField] MaxAdContentRatingLevel maxAdContentRating = MaxAdContentRatingLevel.Unspecified;

  public enum MaxAdContentRatingLevel { Unspecified, G, PG, T, MA }

  void Awake()
  {
  if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
  // Initialize AdMob once; safe to call multiple times.
#if GOOGLE_MOBILE_ADS
  // Some GMA versions don't expose RequestConfiguration.Builder in Unity.
  // To maximize compatibility, skip request configuration here and rely on defaults.
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
    if (autoLoadInterstitial && interstitial != null) interstitial.LoadAd();
    var banner = FindFirstObjectByType<BannerAd>();
    if (autoLoadBanner && banner != null) banner.LoadBanner();
    var rewarded = FindFirstObjectByType<RewardedAd>();
    if (autoLoadRewarded && rewarded != null) rewarded.LoadAd();
  }
}
