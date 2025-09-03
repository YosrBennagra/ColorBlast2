using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Thin bridge over your AdsInitializer, InterstitialAd, and BannerAd scripts.
/// Provides simple methods the game can call without duplicating SDK logic.
/// Assumes these components exist in the active scene and are configured in the Inspector.
/// </summary>
public class AdsBridge : MonoBehaviour
{
    public static AdsBridge Instance { get; private set; }

    [Header("Optional: References (auto-assigned if left empty)")]
    [SerializeField] private AdsInitializer adsInitializer;
    [SerializeField] private InterstitialAd interstitial;
    [SerializeField] private BannerAd banner;
    [SerializeField] private RewardedAd rewarded;

    [Header("Simulation Fallback")]
    [SerializeField] private bool simulateWhenNotReady = true;
    [SerializeField] private float simulateInterstitialSeconds = 1.5f;
    [SerializeField] private float simulateRewardedSeconds = 2.5f;

    [Header("Banner Persistence")]
    [SerializeField] private bool keepAcrossScenes = true;
    [SerializeField] private bool alwaysShowBanner = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (keepAcrossScenes) DontDestroyOnLoad(gameObject);
        AutoAssign();
        if (alwaysShowBanner)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            // Ensure banner shows right away on first scene if requested
            ShowBanner();
        }
    }

    private void AutoAssign()
    {
        if (adsInitializer == null) adsInitializer = FindFirstObjectByType<AdsInitializer>();
        if (interstitial == null) interstitial = FindFirstObjectByType<InterstitialAd>();
        if (banner == null)
        {
            banner = FindFirstObjectByType<BannerAd>();
            if (banner == null)
            {
                // Create a lightweight BannerAd host for this scene only. Do NOT persist it.
                var go = new GameObject("_BannerAd");
                banner = go.AddComponent<BannerAd>();
            }
        }
        if (rewarded == null) rewarded = FindFirstObjectByType<RewardedAd>();
    }

    public void InitializeIfNeeded()
    {
        AutoAssign();
        // Rely on AdsInitializer to handle initialization in Awake/InitializeAds
        // Nothing else needed here, but we can poke it if required.
        if (adsInitializer == null)
        {
            Debug.LogWarning("[AdsBridge] AdsInitializer not found in scene. Ad SDK may not be initialized.");
        }
    }

    // ---------------- Interstitial ----------------
    public bool IsInterstitialReady()
    {
        AutoAssign();
        return interstitial != null && interstitial.IsLoaded;
    }

    public void LoadInterstitial()
    {
        AutoAssign();
        if (interstitial == null) { Debug.LogWarning("[AdsBridge] InterstitialAd not found."); return; }
        interstitial.RequestLoad();
    }

    public void ShowInterstitial(Action onCompleted)
    {
        AutoAssign();
    if (interstitial != null && interstitial.IsLoaded)
        {
        void CompleteAdmob(string id, bool completed) { interstitial.OnShowCompleteEvent -= CompleteAdmob; onCompleted?.Invoke(); }
        interstitial.OnShowCompleteEvent += CompleteAdmob;
            interstitial.Show();
            return;
        }

        if (simulateWhenNotReady)
        {
            Debug.Log("[AdsBridge] Interstitial not ready, simulating...");
            LoadInterstitial();
            StartCoroutine(Simulate(onCompleted, simulateInterstitialSeconds));
        }
        else
        {
            Debug.LogWarning("[AdsBridge] Interstitial not ready and simulation disabled.");
            onCompleted?.Invoke();
        }
    }

    private System.Collections.IEnumerator Simulate(Action done, float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        done?.Invoke();
    }

    // ---------------- Banner ----------------
    public bool IsBannerReady()
    {
        AutoAssign();
        return banner != null && banner.IsLoaded;
    }

    public void LoadBanner()
    {
        AutoAssign();
        if (banner == null) { Debug.LogWarning("[AdsBridge] BannerAd not found."); return; }
        banner.LoadBanner();
    }

    public void ShowBanner()
    {
        AutoAssign();
        if (banner == null) { Debug.LogWarning("[AdsBridge] BannerAd not found."); return; }
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.Equals(active, "CoreGame", StringComparison.Ordinal))
        {
            if (!banner.IsLoaded) banner.LoadBanner();
            banner.ShowBannerAd();
        }
        else
        {
            banner.HideBannerAd();
        }
    }

    public bool IsBannerShowing()
    {
        AutoAssign();
        return banner != null && banner.IsShowing;
    }

    public void HideBanner()
    {
        AutoAssign();
        if (banner == null) return;
        banner.HideBannerAd();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!alwaysShowBanner) return;
    ShowBanner();
    }

    private void OnDestroy()
    {
        if (alwaysShowBanner)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // ---------------- Rewarded ----------------
    public bool IsRewardedReady()
    {
        AutoAssign();
        return rewarded != null && rewarded.IsLoaded;
    }

    public void ShowRewarded(Action<bool> onCompleted)
    {
        AutoAssign();
        if (rewarded == null)
        {
            // No component in scene
            if (simulateWhenNotReady)
            {
                StartCoroutine(Simulate(() => onCompleted?.Invoke(true), simulateRewardedSeconds));
            }
            else
            {
                onCompleted?.Invoke(false);
            }
            return;
        }

        StartCoroutine(ShowRewardedRoutine(onCompleted));
    }

    private IEnumerator ShowRewardedRoutine(Action<bool> onCompleted)
    {
        // Ensure loaded
        float timeout = 10f;
        float t = 0f;
        if (!rewarded.IsLoaded)
        {
            rewarded.LoadAd();
            // Wait until loaded or timeout
            while (!rewarded.IsLoaded && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!rewarded.IsLoaded)
        {
            if (simulateWhenNotReady)
            {
                yield return Simulate(() => onCompleted?.Invoke(true), simulateRewardedSeconds);
            }
            else
            {
                onCompleted?.Invoke(false);
            }
            yield break;
        }

#if GOOGLE_MOBILE_ADS
        void HandlerAdmob(string adUnitId, bool completed)
        {
            rewarded.OnShowCompleteEvent -= HandlerAdmob;
            onCompleted?.Invoke(completed);
        }
        rewarded.OnShowCompleteEvent += HandlerAdmob;
#endif
        rewarded.Show();
    }
}
