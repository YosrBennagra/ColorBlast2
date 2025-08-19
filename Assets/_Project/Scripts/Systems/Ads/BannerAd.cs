using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using System;
using System.Collections;
 
public class BannerAd : MonoBehaviour
{
  // For the purpose of this example, these buttons are for functionality testing:
  [SerializeField] Button _loadBannerButton;
  [SerializeField] Button _showBannerButton;
  [SerializeField] Button _hideBannerButton;

  [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;

  [SerializeField] string _androidAdUnitId = "Banner_Android";
  [SerializeField] string _iOSAdUnitId = "Banner_iOS";
    string _adUnitId = null; // This will remain null for unsupported platforms.
    public bool IsLoaded { get; private set; }
    public bool IsShowing { get; private set; }
    public event Action OnLoaded;
    public event Action<string> OnError;
    public event Action OnShown;
    public event Action OnHidden;

  void Start()
  {
    // Get the Ad Unit ID for the current platform:
    #if UNITY_IOS
    _adUnitId = _iOSAdUnitId;
    #elif UNITY_ANDROID
    _adUnitId = _androidAdUnitId;
  #elif UNITY_EDITOR
  _adUnitId = _androidAdUnitId; // Use Android unit for editor testing config
    #endif

  // Disable the buttons (if assigned) until an ad is ready to show:
  if (_showBannerButton != null) _showBannerButton.interactable = false;
  if (_hideBannerButton != null) _hideBannerButton.interactable = false;

    // Set the banner position:
    _bannerPosition = BannerPosition.BOTTOM_CENTER;
    Advertisement.Banner.SetPosition(_bannerPosition);

        // Configure the Load Banner button to call the LoadBanner() method when clicked:
        if (_loadBannerButton != null)
        {
            _loadBannerButton.onClick.AddListener(LoadBanner);
            _loadBannerButton.interactable = true;
        }
        
    // Auto-load banner if Unity Ads is already initialized
    if (Advertisement.isInitialized)
    {
      LoadBanner();
    }
    else
    {
      // Wait for initialization then load
      StartCoroutine(WaitForInitAndLoad());
    }

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
          Debug.LogWarning("Banner Ad Unit ID is not set for current platform");
          return;
      }
      
      Debug.Log($"Loading Banner Ad: {_adUnitId}");
      // Set up options to notify the SDK of load events:
      BannerLoadOptions options = new BannerLoadOptions
      {
          loadCallback = OnBannerLoaded,
          errorCallback = OnBannerError
      };

      // Load the Ad Unit with banner content:
    Advertisement.Banner.Load(_adUnitId, options);
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
      
      // Always ensure bottom center position before showing
      Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
      
      // Set up options to notify the SDK of show events:
      BannerOptions options = new BannerOptions
      {
          clickCallback = OnBannerClicked,
          hideCallback = OnBannerHidden,
          showCallback = OnBannerShown
      };

      // Show the loaded Banner Ad Unit:
    Advertisement.Banner.Show(_adUnitId, options);
  }

  // Implement a method to call when the Hide Banner button is clicked:
    public void HideBannerAd()
  {
            // Hide the banner:
            Advertisement.Banner.Hide();
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
  }

  private System.Collections.IEnumerator WaitForInitAndLoad()
  {
    float timeout = 30f;
    float timer = 0f;
    
    while (!Advertisement.isInitialized && timer < timeout)
    {
      timer += Time.unscaledDeltaTime;
      yield return null;
    }
    
    if (Advertisement.isInitialized)
    {
      LoadBanner();
    }
    else
    {
      Debug.LogWarning("[BannerAd] Unity Ads failed to initialize within timeout period");
    }
  }
}
