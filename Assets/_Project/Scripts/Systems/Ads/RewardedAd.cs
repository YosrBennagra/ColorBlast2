using UnityEngine;
using System;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
 
public class RewardedAd : MonoBehaviour
{
  private const string ANDROID_TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917"; // Google test ID
  [SerializeField] string _androidAdUnitId = "ca-app-pub-9594729661204695/7016968243";
  string _adUnitId;
  public bool IsLoaded { get; private set; }
  public event Action<string> OnLoaded;
#if GOOGLE_MOBILE_ADS
  public event Action<string, LoadAdError, string> OnFailedToLoad;
  public event Action<string, AdError, string> OnShowFailure;
#endif
  public event Action<string> OnShowStartEvent;
  public event Action<string> OnShowClickEvent;
  public event Action<string, bool> OnShowCompleteEvent; // bool: rewarded success

#if GOOGLE_MOBILE_ADS
  private GoogleMobileAds.Api.RewardedAd _rewarded;
  private bool _earnedReward;
#endif
 
  void Awake()
  {
  // Android only - validate and trim
  var id = (_androidAdUnitId ?? string.Empty).Trim();
  if (string.IsNullOrEmpty(id) || !id.StartsWith("ca-app-pub-") || !id.Contains("/"))
  {
    Debug.LogWarning("[RewardedAd] Invalid or empty Android Ad Unit ID. Using AdMob test rewarded ID.");
    _adUnitId = ANDROID_TEST_REWARDED_ID;
  }
  else
  {
    _adUnitId = id;
  }
  }
 
  // Load content to the Ad Unit:
  public void LoadAd()
  {
    // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
    Debug.Log("Loading Rewarded Ad: " + _adUnitId);
    if (!AdsInitializer.Initialized)
    {
      Debug.LogWarning("[RewardedAd] Mobile Ads not initialized yet. Delaying load by 1s.");
      CancelInvoke(nameof(LoadAd));
      Invoke(nameof(LoadAd), 1f);
      return;
    }
#if GOOGLE_MOBILE_ADS
    // Clean existing
    if (_rewarded != null) { _rewarded.Destroy(); _rewarded = null; }
  var request = new AdRequest();
    GoogleMobileAds.Api.RewardedAd.Load(_adUnitId, request, (GoogleMobileAds.Api.RewardedAd ad, LoadAdError err) =>
    {
      if (err != null || ad == null)
      {
    var msg = err != null ? err.GetMessage() : "null ad returned";
    Debug.Log($"Error loading Rewarded Ad Unit: {_adUnitId} - {msg}");
        IsLoaded = false;
        OnFailedToLoad?.Invoke(_adUnitId, err, msg);
        Invoke(nameof(LoadAd), 5f);
        return;
      }
      _rewarded = ad;
      WireRewardedCallbacks();
      IsLoaded = true; OnLoaded?.Invoke(_adUnitId);
    });
#else
  Debug.LogWarning("[RewardedAd] Google Mobile Ads SDK not found. Install the plugin to enable rewarded ads.");
  IsLoaded = false;
#endif
  }

  // Aliases for external systems
  public void RequestLoad() => LoadAd();
 
  // Show the loaded content in the Ad Unit:
  public void ShowAd()
  {
    // Note that if the ad content wasn't previously loaded, this method will fail
    Debug.Log("Showing Rewarded Ad: " + _adUnitId);
#if GOOGLE_MOBILE_ADS
    if (_rewarded != null && _rewarded.CanShowAd())
    {
      _earnedReward = false;
      _rewarded.Show((Reward reward) => { _earnedReward = true; });
    }
    else
    {
      Debug.LogWarning("Rewarded not ready");
    }
#else
  Debug.LogWarning("[RewardedAd] Google Mobile Ads SDK not found; cannot show rewarded ad.");
#endif
  }
  public void Show() => ShowAd();
 
#if GOOGLE_MOBILE_ADS
  private void WireRewardedCallbacks()
  {
    if (_rewarded == null) return;
    _rewarded.OnAdFullScreenContentOpened += () => { OnShowStartEvent?.Invoke(_adUnitId); };
    _rewarded.OnAdFullScreenContentClosed += () =>
    {
      IsLoaded = false;
      OnShowCompleteEvent?.Invoke(_adUnitId, _earnedReward);
      LoadAd(); // auto reload
    };
    _rewarded.OnAdFullScreenContentFailed += (AdError err) => { OnShowFailure?.Invoke(_adUnitId, err, err.GetMessage()); };
    _rewarded.OnAdClicked += () => { OnShowClickEvent?.Invoke(_adUnitId); };
  }
#endif
}
