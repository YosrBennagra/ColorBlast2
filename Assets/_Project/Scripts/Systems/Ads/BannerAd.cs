using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
 
[DisallowMultipleComponent]
[AddComponentMenu("Ads/Banner Ad")]
public class BannerAd : MonoBehaviour
{
  private const string ANDROID_TEST_BANNER_ID = "ca-app-pub-3940256099942544/6300978111"; // Google test ID
  // For the purpose of this example, these buttons are for functionality testing:
  [SerializeField] Button _loadBannerButton;
  [SerializeField] Button _showBannerButton;
  [SerializeField] Button _hideBannerButton;

  #if GOOGLE_MOBILE_ADS
  [SerializeField] AdPosition _admobPosition = AdPosition.Bottom;
  #endif

  [Header("Ad Unit IDs")]
  [SerializeField] string _androidAdUnitId = "ca-app-pub-9594729661204695/4354026328"; // Unity Ads or AdMob ID based on provider
  string _adUnitId = null; // This will remain null for unsupported platforms.
  public bool IsLoaded { get; private set; }
  public bool IsShowing { get; private set; }
  public event Action OnLoaded;
  public event Action<string> OnError;
  public event Action OnShown;
  public event Action OnHidden;

#if GOOGLE_MOBILE_ADS
  private BannerView _bannerView;
#endif

  void Awake()
  {
    // Resolve Ad Unit ID as early as possible (Awake), so external callers can load immediately.
    _adUnitId = _androidAdUnitId;
    if (string.IsNullOrEmpty(_adUnitId))
    {
      _adUnitId = ANDROID_TEST_BANNER_ID;
      Debug.LogWarning("[BannerAd] Android Banner Ad Unit ID not set. Using AdMob test ID.");
    }
  }

  void Start()
  {
    // No-op here for ID assignment; done in Awake.

  // Disable the buttons (if assigned) until an ad is ready to show:
  if (_showBannerButton != null) _showBannerButton.interactable = false;
  if (_hideBannerButton != null) _hideBannerButton.interactable = false;

  // Configure the Load Banner button to call the LoadBanner() method when clicked:
  if (_loadBannerButton != null)
  {
      _loadBannerButton.onClick.AddListener(LoadBanner);
      _loadBannerButton.interactable = true;
  }
        
  // Auto-load banner
  LoadBanner();

    if (Application.isEditor)
    {
      Debug.Log("[BannerAd] Running in Editor. Banner ads may not render in the Editor. Test on a device.");
    }
  }
 
  // Implement a method to call when the Load Banner button is clicked:
    public void LoadBanner()
  {
    if (string.IsNullOrEmpty(_adUnitId))
    {
      Debug.LogWarning("Banner Ad Unit ID is not set for current platform. Falling back to AdMob test ID.");
      _adUnitId = ANDROID_TEST_BANNER_ID;
    }
    if (!AdsInitializer.Initialized)
    {
      Debug.LogWarning("[BannerAd] Mobile Ads not initialized yet. Delaying load by 1s.");
      CancelInvoke(nameof(LoadBanner));
      Invoke(nameof(LoadBanner), 1f);
      return;
    }
      
  Debug.Log($"Loading Banner Ad: {_adUnitId}");
#if GOOGLE_MOBILE_ADS
  // Clean up old view if exists
  if (_bannerView != null)
  {
    _bannerView.Destroy();
    _bannerView = null;
  }
  // Create a new banner view with adaptive size for better device compatibility
  var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
  _bannerView = new BannerView(_adUnitId, adaptiveSize, _admobPosition);
  // Attach events
  _bannerView.OnBannerAdLoaded += () => { IsLoaded = true; OnLoaded?.Invoke(); ShowBannerAd(); };
  _bannerView.OnBannerAdLoadFailed += (LoadAdError error) => { IsLoaded = false; OnError?.Invoke(error.GetMessage()); Invoke(nameof(LoadBanner), 10f); };
  _bannerView.OnAdFullScreenContentClosed += () => { IsShowing = false; OnHidden?.Invoke(); };
  _bannerView.OnAdFullScreenContentOpened += () => { IsShowing = true; OnShown?.Invoke(); };
  // Load request
  var request = new AdRequest();
  _bannerView.LoadAd(request);
#else
  // AdMob SDK not present
  Debug.LogWarning("[BannerAd] Google Mobile Ads SDK not found. Install the plugin to enable banners.");
  IsLoaded = false;
#endif
  }

  // Implement code to execute when the loadCallback event triggers:
  void OnBannerLoaded()
  {
            Debug.Log("Banner loaded");
            IsLoaded = true;
            OnLoaded?.Invoke();

            if (_showBannerButton != null) _showBannerButton.onClick.AddListener(ShowBannerAd);
            if (_hideBannerButton != null) _hideBannerButton.onClick.AddListener(HideBannerAd);

            if (_showBannerButton != null) _showBannerButton.interactable = true;
            if (_hideBannerButton != null) _hideBannerButton.interactable = true;
            
            // Auto-show banner after loading
            ShowBannerAd();
  }

  // Implement code to execute when the load errorCallback event triggers:
  void OnBannerError(string message)
  {
            Debug.Log($"Banner Error: {message}");
            IsLoaded = false;
            OnError?.Invoke(message);
            // Retry after 10 seconds
            Invoke(nameof(LoadBanner), 10f);
  }

  // Implement a method to call when the Show Banner button is clicked:
    public void ShowBannerAd()
  {
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
    void OnBannerShown() { IsShowing = true; OnShown?.Invoke(); }
    void OnBannerHidden() { IsShowing = false; OnHidden?.Invoke(); }

  void OnDestroy()
  {
      // Clean up the listeners:
    if (_loadBannerButton != null) _loadBannerButton.onClick.RemoveAllListeners();
    if (_showBannerButton != null) _showBannerButton.onClick.RemoveAllListeners();
    if (_hideBannerButton != null) _hideBannerButton.onClick.RemoveAllListeners();

#if GOOGLE_MOBILE_ADS
    if (_bannerView != null)
    {
      _bannerView.Destroy();
      _bannerView = null;
    }
#endif
  }
  
}
