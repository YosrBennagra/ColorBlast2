using UnityEngine;
using System;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
 
[DisallowMultipleComponent]
[AddComponentMenu("Ads/Banner Ad")]
public class BannerAd : MonoBehaviour
{
  // Simple AdMob banner loader for Android
  private const string ANDROID_TEST_BANNER_ID = "ca-app-pub-3940256099942544/6300978111"; // Google test ID
  // This component is scene-local; banner shows only in allowed scenes.

  #if GOOGLE_MOBILE_ADS
  [SerializeField] AdPosition _admobPosition = AdPosition.Bottom;
  #endif

  [Header("Ad Unit ID")]
  [SerializeField] private string bannerId = "ca-app-pub-9594729661204695/4354026328"; // paste EXACTLY
  [SerializeField] private bool useTestBannerId = false; // set true to force Google test banner
  string _adUnitId = null; // This will remain null for unsupported platforms.
  [Header("Scene Restriction")]
  [SerializeField] private string allowedSceneName = "CoreGame";
  
  public bool IsLoaded { get; private set; }
  public bool IsShowing { get; private set; }

#if GOOGLE_MOBILE_ADS
  private BannerView _bannerView;
  private bool _creating;
#endif

  void Awake()
  {
  // Resolve Ad Unit ID as early as possible (Awake), so external callers can load immediately.
  _adUnitId = (bannerId ?? string.Empty).Trim();
#if DEVELOPMENT_BUILD
  // In Development builds, force the Google test banner to validate SDK path on device
  useTestBannerId = true;
#endif
  if (useTestBannerId) { _adUnitId = ANDROID_TEST_BANNER_ID; Debug.Log("[Ads] Forcing TEST banner ID in this build."); }
    if (string.IsNullOrEmpty(_adUnitId))
    {
      Debug.LogError("[BannerAd] Android Banner Ad Unit ID is empty.");
    }
    else
    {
      Debug.Log($"[Ads] Using banner ID: {_adUnitId}");
    }
  }

  void Start()
  {
    // Auto-load banner
  LoadBanner();
  }
 
  // Implement a method to call when the Load Banner button is clicked:
    public void LoadBanner()
  {
#if GOOGLE_MOBILE_ADS
    if (!IsAllowedScene())
    {
      Debug.Log("[BannerAd] Current scene is not allowed for banner (" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "). Skipping load.");
      return;
    }
    if (!AdsInitializer.Initialized)
    {
      Debug.LogWarning("[BannerAd] Mobile Ads not initialized yet. Delaying load by 1s.");
      CancelInvoke(nameof(LoadBanner));
      Invoke(nameof(LoadBanner), 1f);
      return;
    }
    CreateAndLoadBanner();
#else
    // AdMob SDK not present
    Debug.LogWarning("[BannerAd] Google Mobile Ads SDK not found. Install the plugin to enable banners.");
    IsLoaded = false;
#endif
  }

#if GOOGLE_MOBILE_ADS
  private void CreateAndLoadBanner()
  {
    // If already created in this scene, reuse instance
    if (_bannerView != null)
    {
      if (IsLoaded)
      {
        Debug.Log("[BannerAd] Banner already loaded, showing.");
        ShowBannerAd();
      }
      else
      {
        Debug.Log("[BannerAd] BannerView exists, reloading request.");
        var req = new AdRequest();
        _bannerView.LoadAd(req);
      }
      return;
    }
    if (string.IsNullOrWhiteSpace(_adUnitId))
    {
      Debug.LogError("[Ads] Banner ID is empty!");
      return;
    }
    if (_creating)
    {
      Debug.Log("[BannerAd] Banner creation already in progress; skipping duplicate call.");
      return;
    }
    _adUnitId = _adUnitId.Trim();
    Debug.Log("[Ads] Using banner ID: " + _adUnitId);

  // Create new 320x50 banner using configured position
  _creating = true;
    _bannerView = new BannerView(_adUnitId, AdSize.Banner, _admobPosition);

    _bannerView.OnBannerAdLoaded += () => { IsLoaded = true; _creating = false; ShowBannerAd(); };
    _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
    {
      IsLoaded = false; _creating = false;
      Debug.LogWarning("[BannerAd] Load failed for " + _adUnitId + ": " + error?.GetMessage());
      CancelInvoke(nameof(LoadBanner));
      Invoke(nameof(LoadBanner), 15f);
    };
    _bannerView.OnAdFullScreenContentClosed += () => { IsShowing = false; };
    _bannerView.OnAdFullScreenContentOpened += () => { IsShowing = true; };

    var request = new AdRequest();
    _bannerView.LoadAd(request);
  }
#endif

  // Implement a method to call when the Show Banner button is clicked:
    public void ShowBannerAd()
  {
    if (!IsAllowedScene())
    {
      Debug.Log("[BannerAd] Current scene is not allowed; hiding banner if visible.");
#if GOOGLE_MOBILE_ADS
      if (_bannerView != null) _bannerView.Hide();
#endif
      IsShowing = false;
      return;
    }
      if (!IsLoaded)
      {
          Debug.LogWarning("Banner not loaded yet, loading first...");
          LoadBanner();
          return;
      }
      
  // Always ensure position before showing
#if GOOGLE_MOBILE_ADS
  if (_bannerView != null)
  {
    Debug.Log("[BannerAd] Showing banner");
    _bannerView.Show();
    IsShowing = true;
  }
#else
  Debug.LogWarning("[BannerAd] Google Mobile Ads SDK not found; cannot show banner.");
#endif
  }

  // Implement a method to call when the Hide Banner button is clicked:
  public void HideBannerAd()
  {
      // Hide the banner:
#if GOOGLE_MOBILE_ADS
  if (_bannerView != null) { Debug.Log("[BannerAd] Hiding banner"); _bannerView.Hide(); }
#else
  // no-op
#endif
      IsShowing = false;
  }

  void OnBannerClicked() { }

  void OnDestroy()
  {
#if GOOGLE_MOBILE_ADS
  // Destroy the instance banner view when leaving the scene
  if (_bannerView != null)
  {
    try { _bannerView.Destroy(); } catch { }
    _bannerView = null;
  }
#endif
  }

  private bool IsAllowedScene()
  {
    var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    return string.Equals(active, allowedSceneName, StringComparison.Ordinal);
  }

  private void OnApplicationQuit()
  {
#if GOOGLE_MOBILE_ADS
    if (_bannerView != null)
    {
      try { _bannerView.Destroy(); } catch { }
      _bannerView = null;
      _creating = false;
    }
#endif
  }
  
}
