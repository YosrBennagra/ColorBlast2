using UnityEngine;
using UnityEngine.Advertisements;
using System;
 
public class InterstitialAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
  [SerializeField] string _androidAdUnitId = "Interstitial_Android";
  [SerializeField] string _iOSAdUnitId = "Interstitial_iOS";
  string _adUnitId;
  public bool IsLoaded { get; private set; }
  public event Action<string> OnLoaded;
  public event Action<string, UnityAdsLoadError, string> OnFailedToLoad;
  public event Action<string, UnityAdsShowError, string> OnShowFailure;
  public event Action<string> OnShowStartEvent;
  public event Action<string> OnShowClickEvent;
  public event Action<string, UnityAdsShowCompletionState> OnShowCompleteEvent;
 
  void Awake()
  {
    // Get the Ad Unit ID for the current platform:
    _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
    ? _iOSAdUnitId
    : _androidAdUnitId;
  }
 
  // Load content to the Ad Unit:
  public void LoadAd()
  {
    // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
    Debug.Log("Loading Ad: " + _adUnitId);
    Advertisement.Load(_adUnitId, this);
  }

  // Aliases for external systems
  public void RequestLoad() => LoadAd();
 
  // Show the loaded content in the Ad Unit:
  public void ShowAd()
  {
    // Note that if the ad content wasn't previously loaded, this method will fail
    Debug.Log("Showing Ad: " + _adUnitId);
    Advertisement.Show(_adUnitId, this);
  }
  public void Show() => ShowAd();
 
  // Implement Load Listener and Show Listener interface methods: 
  public void OnUnityAdsAdLoaded(string adUnitId)
  {
    Debug.Log($"Interstitial Ad loaded: {adUnitId}");
    IsLoaded = true;
    OnLoaded?.Invoke(adUnitId);
  }
 
  public void OnUnityAdsFailedToLoad(string _adUnitId, UnityAdsLoadError error, string message)
  {
    Debug.Log($"Error loading Ad Unit: {_adUnitId} - {error.ToString()} - {message}");
    IsLoaded = false;
    OnFailedToLoad?.Invoke(_adUnitId, error, message);
    // Retry after 5 seconds
    Invoke(nameof(LoadAd), 5f);
  }
 
  public void OnUnityAdsShowFailure(string _adUnitId, UnityAdsShowError error, string message)
  {
    Debug.Log($"Error showing Ad Unit {_adUnitId}: {error.ToString()} - {message}");
    OnShowFailure?.Invoke(_adUnitId, error, message);
  }
 
  public void OnUnityAdsShowStart(string _adUnitId) { OnShowStartEvent?.Invoke(_adUnitId); }
  public void OnUnityAdsShowClick(string _adUnitId) { OnShowClickEvent?.Invoke(_adUnitId); }
  public void OnUnityAdsShowComplete(string _adUnitId, UnityAdsShowCompletionState showCompletionState)
  {
    Debug.Log($"Interstitial Ad show complete: {_adUnitId}, state: {showCompletionState}");
    IsLoaded = false; // consume load
    OnShowCompleteEvent?.Invoke(_adUnitId, showCompletionState);
    // Auto-reload for next time
    LoadAd();
  }
}
