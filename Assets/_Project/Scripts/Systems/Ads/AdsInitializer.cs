using UnityEngine;
using UnityEngine.Advertisements;
 
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
  [SerializeField] string androidGameId;
  [SerializeField] string iOSGameId;
  [SerializeField] bool testMode = true;
  private string gameId;
 
  void Awake()
  {
    if (Advertisement.isInitialized)
    {
      // If Ads already initialized (e.g., from a previous scene), trigger post-init behaviors
      OnInitializationComplete();
    }
    else
    {
      SetupAds();
    }
  }
 
  public void SetupAds()
  {
    #if UNITY_IOS
    gameId = iOSGameId;
    #elif UNITY_ANDROID
    gameId = androidGameId;
    #elif UNITY_EDITOR
    gameId = androidGameId; //Only for testing the functionality in the Editor
    #endif
 
    if (!Advertisement.isInitialized && Advertisement.isSupported)
     {
       Advertisement.Initialize(gameId, testMode, this);
     }
  }

  public void OnInitializationComplete()
  {
    Debug.Log("Unity Ads initialization complete.");
    // Trigger auto-loading of ads after successful initialization
    var interstitial = FindFirstObjectByType<InterstitialAd>();
    if (interstitial != null) interstitial.LoadAd();
    
    var banner = FindFirstObjectByType<BannerAd>();
    if (banner != null) banner.LoadBanner();
    
    // Use reflection to find and load RewardedAd without compilation dependency
    var rewardedAdType = System.Type.GetType("RewardedAd");
    if (rewardedAdType != null)
    {
      var rewarded = FindFirstObjectByType(rewardedAdType) as MonoBehaviour;
      if (rewarded != null)
      {
        var loadMethod = rewardedAdType.GetMethod("LoadAd");
        if (loadMethod != null)
        {
          loadMethod.Invoke(rewarded, null);
          Debug.Log("Auto-loading RewardedAd after initialization");
        }
      }
    }
  }
 
  public void OnInitializationFailed(UnityAdsInitializationError error, string message)
  {
    Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
  }
}
