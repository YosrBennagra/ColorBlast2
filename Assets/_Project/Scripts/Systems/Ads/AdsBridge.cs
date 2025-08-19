using System;
using UnityEngine;
using UnityEngine.Advertisements;
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AutoAssign();
    }

    private void AutoAssign()
    {
        if (adsInitializer == null) adsInitializer = FindFirstObjectByType<AdsInitializer>();
        if (interstitial == null) interstitial = FindFirstObjectByType<InterstitialAd>();
        if (banner == null) banner = FindFirstObjectByType<BannerAd>();
    if (rewarded == null) rewarded = FindFirstObjectByType<RewardedAd>();
    }

    public void InitializeIfNeeded()
    {
        AutoAssign();
        // Rely on AdsInitializer to handle initialization in Awake/InitializeAds
        // Nothing else needed here, but we can poke it if required.
        if (adsInitializer == null)
        {
            Debug.LogWarning("[AdsBridge] AdsInitializer not found in scene. Unity Ads may not be initialized.");
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
            void Complete(string id, UnityAdsShowCompletionState state)
            {
                interstitial.OnShowCompleteEvent -= Complete;
                onCompleted?.Invoke();
            }
            interstitial.OnShowCompleteEvent += Complete;
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
        if (!banner.IsLoaded) banner.LoadBanner();
        banner.ShowBannerAd();
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

        Action<string, UnityAdsShowCompletionState> handler = null;
        handler = (adUnitId, state) =>
        {
            // Unsubscribe to avoid leaks
            rewarded.OnShowCompleteEvent -= handler;
            bool success = state == UnityAdsShowCompletionState.COMPLETED;
            onCompleted?.Invoke(success);
        };
        rewarded.OnShowCompleteEvent += handler;
        rewarded.Show();
    }
}
