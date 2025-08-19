using UnityEngine;
using System;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace ColorBlast2.Systems.Ads
{
    /// <summary>
    /// Centralized ad wrapper. Configure in inspector (singleton). Supports Unity Ads; can be extended.
    /// </summary>
    public class AdService : MonoBehaviour
    #if UNITY_ADS
        , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    #endif
    {
        public static AdService Instance { get; private set; }
        public static bool Exists => Instance != null;

        [Header("Unity Ads Settings")]
        [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private string androidGameId = "5922679"; // Provided Android Game ID
        [SerializeField] private string iosGameId = "";
    [SerializeField, Tooltip("Unity Ads test mode flag actually passed to initialize (runtime overridden below). ")] private bool testMode = true;
    [SerializeField, Tooltip("Use test mode while running inside the Unity Editor.")] private bool testModeInEditor = true;
    [SerializeField, Tooltip("Force live (testMode=false) when running on actual device build.")] private bool forceLiveOnDevice = true;
    [SerializeField, Tooltip("Show small OnGUI debug overlay with ad readiness (Editor / Development builds only). ")] private bool showDebugOverlay = true;
    [Header("Diagnostics")] [SerializeField, Tooltip("Verbose console logging for ad lifecycle events.")] private bool verboseLogging = true;
    [SerializeField, Tooltip("Seconds before init watchdog retries initialization.")] private float initWatchdogSeconds = 8f;
    private bool initCallbackReceived = false; private float initStartTime = 0f;
        [SerializeField] private string interstitialPlacementId = "Interstitial_Android";
        [SerializeField] private string rewardedPlacementId = "Rewarded_Android";
        [SerializeField] private string bannerPlacementId = "Banner_Android";
        [SerializeField] private bool autoLoadInterstitial = true;
        [SerializeField] private bool autoLoadRewarded = true;
        [SerializeField] private bool autoLoadBanner = true;
        [SerializeField] private bool showBannerOnInit = true;
    #if UNITY_ADS
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
    #else
    [SerializeField, Tooltip("Banner position placeholder (define UNITY_ADS to use real BannerPosition enum)." )]
    private string bannerPosition = "BOTTOM_CENTER"; // Placeholder only when UNITY_ADS not defined
    #endif

        private bool interstitialLoaded; 
        private bool rewardedLoaded; 
        private bool bannerLoaded;

    [Header("Retry Settings")] 
    [SerializeField] private int maxLoadRetry = 5; 
    [SerializeField] private float retryBaseDelay = 2f; // seconds (exponential backoff)
    private int interstitialRetryCount; 
    private int rewardedRetryCount; 
    private int bannerRetryCount;

        private Action interstitialCallback;
        private Action<bool> rewardedCallback;
        private Action bannerCallback;

#if UNITY_ADS
        private string GameId => (Application.platform == RuntimePlatform.IPhonePlayer) ? iosGameId : androidGameId;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Decide real testMode value per environment
#if UNITY_EDITOR
            testMode = testModeInEditor;
#else
            if (forceLiveOnDevice) testMode = false; // live ads on device
#endif
#if UNITY_ADS
            if (initializeOnAwake)
            {
                initStartTime = Time.realtimeSinceStartup;
                if (verboseLogging) Debug.Log($"[AdService] Initializing Unity Ads (gameId={GameId} test={testMode})");
                Advertisement.Initialize(GameId, testMode, this);
            }
#else
            Debug.LogWarning("[AdService] UNITY_ADS symbol not defined. Ads disabled. Install/enable Advertisement package or add scripting define UNITY_ADS.");
#endif
        }

    public void InitializeIfNeeded()
    {
#if UNITY_ADS
        if (!Advertisement.isInitialized)
        {
        initStartTime = Time.realtimeSinceStartup;
        if (verboseLogging) Debug.Log($"[AdService] InitializeIfNeeded -> Initialize (test={testMode})");
        Advertisement.Initialize(GameId, testMode, this);
        }
#endif
    }

    private void Update()
    {
#if UNITY_ADS
        if (!initCallbackReceived && initializeOnAwake && initStartTime > 0f && (Time.realtimeSinceStartup - initStartTime) > initWatchdogSeconds && !Advertisement.isInitialized)
        {
        if (verboseLogging) Debug.LogWarning("[AdService] Init watchdog retry");
        initStartTime = Time.realtimeSinceStartup;
        Advertisement.Initialize(GameId, testMode, this);
        }
#endif
    }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;
#if !UNITY_EDITOR
            if (!Debug.isDebugBuild) return; // show only in dev builds outside editor
#endif
            const int pad = 8; int w = 220; int h = 70; int x = pad; int y = pad;
            GUI.color = new Color(0,0,0,0.6f); GUI.Box(new Rect(x,y,w,h), GUIContent.none);
            GUI.color = Color.white;
            string txt = "Ads Init: " +
#if UNITY_ADS
                (UnityEngine.Advertisements.Advertisement.isInitialized?"Yes":"No") +
#else
                "No (UNITY_ADS missing)" +
#endif
                "\nInterstitial: " + (IsInterstitialReady()?"Ready":"Loading") +
                "\nRewarded: " + (IsRewardedReady()?"Ready":"Loading") +
                "\nBanner: " + (IsBannerReady()?"Ready":"Loading") + (testMode?" (Test)":" (Live)");
            GUI.Label(new Rect(x+10,y+8,w-20,h-16), txt);
        }

        public void ShowInterstitial(Action completed, Action fallback)
        {
#if UNITY_ADS
            interstitialCallback = completed;
            if (interstitialLoaded)
            {
                if (verboseLogging) Debug.Log("[AdService] Show interstitial");
                Advertisement.Show(interstitialPlacementId, this);
            }
            else
            {
                if (fallback != null) fallback();
                // Attempt load if not already
                if (!interstitialLoaded) LoadInterstitial();
            }
#else
            fallback?.Invoke();
#endif
        }

        public void ShowRewarded(Action<bool> completed, Action fallback)
        {
#if UNITY_ADS
            rewardedCallback = completed;
            if (rewardedLoaded)
            {
                if (verboseLogging) Debug.Log("[AdService] Show rewarded");
                Advertisement.Show(rewardedPlacementId, this);
            }
            else
            {
                if (fallback != null) fallback();
                if (!rewardedLoaded) LoadRewarded();
            }
#else
            fallback?.Invoke();
#endif
        }

        public void ShowBanner()
        {
#if UNITY_ADS
            if (!Advertisement.isInitialized) return;
            if (verboseLogging) Debug.Log("[AdService] Show banner");
            Advertisement.Banner.SetPosition(bannerPosition);
            Advertisement.Banner.Show(bannerPlacementId, new BannerOptions{ showCallback=()=>{ if(verboseLogging) Debug.Log("[AdService] Banner shown"); }, hideCallback=()=>{ if(verboseLogging) Debug.Log("[AdService] Banner hidden"); }, clickCallback=()=>{ if(verboseLogging) Debug.Log("[AdService] Banner clicked"); }});
#endif
        }

        public void HideBanner()
        {
#if UNITY_ADS
            Advertisement.Banner.Hide();
#endif
        }

    public bool IsInterstitialReady() => interstitialLoaded;
    public bool IsRewardedReady() => rewardedLoaded;
    public bool IsBannerReady() => bannerLoaded;

#if UNITY_ADS
        // Initialization
        public void OnInitializationComplete()
        {
            if (autoLoadInterstitial) LoadInterstitial();
            if (autoLoadRewarded) LoadRewarded();
            if (autoLoadBanner) LoadBanner();
        }
        public void OnInitializationFailed(UnityAdsInitializationError error, string message) { Debug.LogWarning($"[AdService] Init failed: {error} {message}"); }

        // Loading helpers
    public void LoadInterstitial() { if(verboseLogging) Debug.Log("[AdService] Load interstitial"); Advertisement.Load(interstitialPlacementId, this); }
    public void LoadRewarded() { if(verboseLogging) Debug.Log("[AdService] Load rewarded"); Advertisement.Load(rewardedPlacementId, this); }
    public void LoadBanner() { if(verboseLogging) Debug.Log("[AdService] Load banner"); Advertisement.Banner.Load(bannerPlacementId, new BannerLoadOptions{ loadCallback=()=>{bannerLoaded=true; bannerRetryCount=0; if(verboseLogging) Debug.Log("[AdService] Banner loaded"); if(showBannerOnInit) ShowBanner();}, errorCallback=e=>{ Debug.LogWarning($"[AdService] Banner load failed {e}"); RetryBanner(); } }); }
    public void ForceReloadAll(){ #if UNITY_ADS if(verboseLogging) Debug.Log("[AdService] ForceReloadAll"); interstitialLoaded=false; rewardedLoaded=false; bannerLoaded=false; LoadInterstitial(); LoadRewarded(); LoadBanner(); #endif }

        public void OnInitializationComplete()
        {
            initCallbackReceived = true;
            if (verboseLogging) Debug.Log("[AdService] Initialization complete");
            if (autoLoadInterstitial) LoadInterstitial();
            if (autoLoadRewarded) LoadRewarded();
            if (autoLoadBanner) LoadBanner();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        { Debug.LogWarning($"[AdService] Init failed: {error} {message}"); }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId == interstitialPlacementId) { interstitialLoaded = true; interstitialRetryCount=0; if(verboseLogging) Debug.Log("[AdService] Interstitial loaded"); }
            else if (placementId == rewardedPlacementId) { rewardedLoaded = true; rewardedRetryCount=0; if(verboseLogging) Debug.Log("[AdService] Rewarded loaded"); }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"[AdService] Load failed {placementId} {error} {message}");
            if (placementId == interstitialPlacementId) RetryInterstitial();
            else if (placementId == rewardedPlacementId) RetryRewarded();
        }
        }

        // Show callbacks
        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    { Debug.LogWarning($"[AdService] Show failure {placementId} {error} {message}"); InvokeFallback(placementId); }
        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }
        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state)
        {
            if (verboseLogging) Debug.Log($"[AdService] Show complete {placementId} state={state}");
            if (placementId == interstitialPlacementId)
            {
                var cb = interstitialCallback; interstitialCallback = null; cb?.Invoke(); interstitialLoaded = false; if(autoLoadInterstitial) LoadInterstitial();
            }
            else if (placementId == rewardedPlacementId)
            {
                var cb = rewardedCallback; rewardedCallback = null; bool rewarded = state == UnityAdsShowCompletionState.COMPLETED; cb?.Invoke(rewarded); rewardedLoaded = false; if(autoLoadRewarded) LoadRewarded();
            }
        }

        private void InvokeFallback(string placementId)
        {
            if (placementId == interstitialPlacementId)
            { var cb = interstitialCallback; interstitialCallback = null; cb?.Invoke(); }
            if (placementId == rewardedPlacementId)
            { var cbR = rewardedCallback; rewardedCallback = null; cbR?.Invoke(false); }
        }

        private void RetryInterstitial()
        {
            if (interstitialRetryCount >= maxLoadRetry) return;
            float delay = retryBaseDelay * Mathf.Pow(2f, interstitialRetryCount);
            interstitialRetryCount++;
            Invoke(nameof(LoadInterstitial), delay);
        }
        private void RetryRewarded()
        {
            if (rewardedRetryCount >= maxLoadRetry) return;
            float delay = retryBaseDelay * Mathf.Pow(2f, rewardedRetryCount);
            rewardedRetryCount++;
            Invoke(nameof(LoadRewarded), delay);
        }
        private void RetryBanner()
        {
            if (bannerRetryCount >= maxLoadRetry) return;
            float delay = retryBaseDelay * Mathf.Pow(2f, bannerRetryCount);
            bannerRetryCount++;
            Invoke(nameof(LoadBanner), delay);
        }
#endif
    }
}
