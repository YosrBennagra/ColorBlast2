using UnityEngine;
using System;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
 
public class InterstitialAd : MonoBehaviour
{
  [SerializeField] string _androidAdUnitId = "ca-app-pub-9594729661204695/5639203748";
  string _adUnitId;
  public bool IsLoaded { get; private set; }
  public event Action<string> OnLoaded;
#if GOOGLE_MOBILE_ADS
  public event Action<string, LoadAdError, string> OnFailedToLoad;
  public event Action<string, AdError, string> OnShowFailure;
#endif
  public event Action<string> OnShowStartEvent;
  public event Action<string> OnShowClickEvent;
#if GOOGLE_MOBILE_ADS
  public event Action<string, AdValue> OnPaidEvent;
#endif
  public event Action<string, bool> OnShowCompleteEvent; // bool: completed
  
#if GOOGLE_MOBILE_ADS
  private GoogleMobileAds.Api.InterstitialAd _interstitial;
#endif
 
  void Awake()
  {
  // Android only
  _adUnitId = _androidAdUnitId;
  }
 
  // Load content to the Ad Unit:
  public void LoadAd()
  {
    // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
    Debug.Log("Loading Ad: " + _adUnitId);
    if (!AdsInitializer.Initialized)
    {
      Debug.LogWarning("[InterstitialAd] Mobile Ads not initialized yet. Delaying load by 1s.");
      CancelInvoke(nameof(LoadAd));
      Invoke(nameof(LoadAd), 1f);
      return;
    }
#if GOOGLE_MOBILE_ADS
      // Clean up existing instance
      if (_interstitial != null) { _interstitial.Destroy(); _interstitial = null; }
    var request = new AdRequest();
      GoogleMobileAds.Api.InterstitialAd.Load(_adUnitId, request, (GoogleMobileAds.Api.InterstitialAd ad, LoadAdError err) =>
      {
        if (err != null || ad == null)
        {
          var msg = err != null ? err.GetMessage() : "null ad returned";
          Debug.Log($"Error loading Ad Unit: {_adUnitId} - {msg}");
          IsLoaded = false;
          OnFailedToLoad?.Invoke(_adUnitId, err, msg);
          Invoke(nameof(LoadAd), 5f);
          return;
        }
        _interstitial = ad;
        // Wire events
        _interstitial.OnAdFullScreenContentOpened += () => { OnShowStartEvent?.Invoke(_adUnitId); };
        _interstitial.OnAdFullScreenContentClosed += () => { IsLoaded = false; OnShowCompleteEvent?.Invoke(_adUnitId, true); RequestLoad(); };
        _interstitial.OnAdClicked += () => { OnShowClickEvent?.Invoke(_adUnitId); };
        _interstitial.OnAdImpressionRecorded += () => { /* optional */ };
        _interstitial.OnAdFullScreenContentFailed += (AdError error) => { OnShowFailure?.Invoke(_adUnitId, error, error.GetMessage()); };
        _interstitial.OnAdPaid += (AdValue val) => { OnPaidEvent?.Invoke(_adUnitId, val); };
        IsLoaded = true; OnLoaded?.Invoke(_adUnitId);
      });
#else
  Debug.LogWarning("[InterstitialAd] Google Mobile Ads SDK not found. Install the plugin to enable interstitials.");
  IsLoaded = false;
#endif
  }

  // Aliases for external systems
  public void RequestLoad() => LoadAd();
 
  // Show the loaded content in the Ad Unit:
  public void ShowAd()
  {
    // Note that if the ad content wasn't previously loaded, this method will fail
    Debug.Log("Showing Ad: " + _adUnitId);
#if GOOGLE_MOBILE_ADS
    if (_interstitial != null && _interstitial.CanShowAd())
    {
      _interstitial.Show();
    }
    else
    {
      Debug.LogWarning("Interstitial not ready");
    }
#else
  Debug.LogWarning("[InterstitialAd] Google Mobile Ads SDK not found; cannot show interstitial.");
#endif
  }
  public void Show() => ShowAd();
 
}
