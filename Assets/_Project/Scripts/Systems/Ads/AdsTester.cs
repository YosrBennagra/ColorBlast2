using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test script to verify Unity Ads integration works.
/// Attach to a GameObject and use buttons to test ad functionality.
/// </summary>
public class AdsTester : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button testInterstitialButton;
    [SerializeField] private Button testBannerButton;
    [SerializeField] private Button testRewardedButton;
    [SerializeField] private Text statusText;

    private void Start()
    {
        // Setup button listeners
        if (testInterstitialButton != null)
            testInterstitialButton.onClick.AddListener(TestInterstitial);
        
        if (testBannerButton != null)
            testBannerButton.onClick.AddListener(TestBanner);
        
        if (testRewardedButton != null)
            testRewardedButton.onClick.AddListener(TestRewarded);

        UpdateStatus("Ads Tester Ready");
    }

    private void TestInterstitial()
    {
        UpdateStatus("Testing Interstitial Ad...");
        
        if (AdsBridge.Instance != null)
        {
            if (AdsBridge.Instance.IsInterstitialReady())
            {
                AdsBridge.Instance.ShowInterstitial(() => 
                {
                    UpdateStatus("Interstitial Ad Completed");
                });
            }
            else
            {
                UpdateStatus("Interstitial Ad Not Ready");
            }
        }
        else
        {
            UpdateStatus("AdsBridge Not Found");
        }
    }

    private void TestBanner()
    {
        UpdateStatus("Testing Banner Ad...");
        
        if (AdsBridge.Instance != null)
        {
            if (AdsBridge.Instance.IsBannerShowing())
            {
                AdsBridge.Instance.HideBanner();
                UpdateStatus("Banner Ad Hidden");
            }
            else
            {
                AdsBridge.Instance.ShowBanner();
                UpdateStatus("Banner Ad Showing");
            }
        }
        else
        {
            UpdateStatus("AdsBridge Not Found");
        }
    }

    private void TestRewarded()
    {
        UpdateStatus("Testing Rewarded Ad...");
        
        if (AdsBridge.Instance != null)
        {
            if (AdsBridge.Instance.IsRewardedReady())
            {
                AdsBridge.Instance.ShowRewarded((success) => 
                {
                    UpdateStatus(success ? "Rewarded Ad Success!" : "Rewarded Ad Failed");
                });
            }
            else
            {
                UpdateStatus("Rewarded Ad Not Ready");
            }
        }
        else
        {
            UpdateStatus("AdsBridge Not Found");
        }
    }

    private void UpdateStatus(string message)
    {
        Debug.Log($"[AdsTester] {message}");
        if (statusText != null)
            statusText.text = message;
    }

    private void Update()
    {
        // Update button states based on ad readiness
        if (testInterstitialButton != null && AdsBridge.Instance != null)
        {
            testInterstitialButton.interactable = AdsBridge.Instance.IsInterstitialReady();
        }
        
        if (testRewardedButton != null && AdsBridge.Instance != null)
        {
            testRewardedButton.interactable = AdsBridge.Instance.IsRewardedReady();
        }
    }
}
