using System;
using UnityEngine;

namespace ColorBlast2.Systems.Ads
{
    /// <summary>
    /// Wrapper that matches the expected AdService API used around the project,
    /// but delegates to your AdsInitializer/InterstitialAd/BannerAd (or AdsBridge if present).
    /// Rewarded ads are simulated unless you add a RewardedAd component.
    /// </summary>
    public class AdService : MonoBehaviour
    {
        public static AdService Instance { get; private set; }
        public static bool Exists => Instance != null;

        [Header("Simulation Fallbacks")] 
        [SerializeField] private bool simulateWhenUnavailable = true;
        [SerializeField] private float simulateInterstitialSeconds = 1.0f;
        [SerializeField] private float simulateRewardedSeconds = 2.5f;
        [SerializeField] private bool verboseLogging = false;

        private InterstitialAd interstitial;
        private BannerAd banner;
        private AdsInitializer initializer;
        private AdsBridge bridge; // optional helper if present

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            FindAdapters();
        }

        private void FindAdapters()
        {
            // Optional bridge
            bridge = FindFirstObjectByType<AdsBridge>();
            if (initializer == null) initializer = FindFirstObjectByType<AdsInitializer>();
            if (interstitial == null) interstitial = FindFirstObjectByType<InterstitialAd>();
            if (banner == null) banner = FindFirstObjectByType<BannerAd>();
        }

        private void Log(string msg)
        {
            if (verboseLogging) Debug.Log("[AdService] " + msg);
        }

        public void ShowInterstitial(Action completed, Action fallback)
        {
            FindAdapters();
            // Prefer bridge
            if (bridge != null)
            {
                bridge.ShowInterstitial(() => { completed?.Invoke(); });
                return;
            }

            if (interstitial != null && interstitial.IsLoaded)
            {
                void OnCompleteAdmob(string id, bool done)
                {
                    interstitial.OnShowCompleteEvent -= OnCompleteAdmob;
                    completed?.Invoke();
                }
                interstitial.OnShowCompleteEvent += OnCompleteAdmob;
                interstitial.Show();
                return;
            }

            // Try to load for next time and fallback now
            if (interstitial != null) interstitial.RequestLoad();
            if (simulateWhenUnavailable)
            {
                Log("Interstitial not ready, simulating...");
                StartCoroutine(SimDelay(simulateInterstitialSeconds, () => completed?.Invoke()));
            }
            else
            {
                fallback?.Invoke();
            }
        }

        public void ShowRewarded(Action<bool> completed, Action fallback)
        {
            FindAdapters();
            // No dedicated RewardedAd component provided; use simulation or fallback
            if (simulateWhenUnavailable)
            {
                Log("Rewarded not implemented, simulating completion...");
                StartCoroutine(SimDelay(simulateRewardedSeconds, () => completed?.Invoke(true)));
            }
            else
            {
                fallback?.Invoke();
            }
        }

        private System.Collections.IEnumerator SimDelay(float seconds, Action done)
        {
            float t = 0f; while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; } done?.Invoke();
        }

        public void LoadInterstitial()
        {
            FindAdapters();
            if (bridge != null) { bridge.LoadInterstitial(); return; }
            if (interstitial != null) interstitial.RequestLoad();
        }

        public void LoadRewarded() { /* no-op until RewardedAd is added; simulation handles flow */ }

        public void ShowBanner()
        {
            FindAdapters();
            if (bridge != null) { bridge.ShowBanner(); return; }
            if (banner == null) { Log("BannerAd not found"); return; }
            if (!banner.IsLoaded) banner.LoadBanner();
            banner.ShowBannerAd();
        }

        public void HideBanner()
        {
            FindAdapters();
            if (bridge != null) { bridge.HideBanner(); return; }
            if (banner != null) banner.HideBannerAd();
        }

        public bool IsBannerReady()
        {
            FindAdapters();
            if (bridge != null) return bridge.IsBannerReady();
            return banner != null && banner.IsLoaded;
        }

        public bool IsInterstitialReady()
        {
            FindAdapters();
            if (bridge != null) return bridge.IsInterstitialReady();
            return interstitial != null && interstitial.IsLoaded;
        }

        public bool IsRewardedReady() { return simulateWhenUnavailable; }
    }
}
