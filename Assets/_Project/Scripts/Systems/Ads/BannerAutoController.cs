using UnityEngine;

namespace ColorBlast2.Systems.Ads
{
    /// <summary>
    /// Automatically shows banner on Start and hides on destroy (attach in CoreGame scene).
    /// </summary>
    public class BannerAutoController : MonoBehaviour
    {
        [SerializeField] private bool hideDuringGameOver = true;

        private void Start()
        {
#if UNITY_ADS
            if (AdService.Exists)
            {
                if (AdService.Instance.IsBannerReady()) AdService.Instance.ShowBanner();
                else AdService.Instance.LoadBanner();
            }
#endif
            if (hideDuringGameOver)
            {
                var goMgr = FindFirstObjectByType<ColorBlast2.UI.Core.GameOverManager>();
                if (goMgr != null)
                {
                    goMgr.gameObject.AddComponent<BannerVisibilityBridge>();
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_ADS
            if (AdService.Exists)
            {
                AdService.Instance.HideBanner();
            }
#endif
        }
    }

    /// <summary>
    /// Bridge that listens to GameOverManager panel visibility via simple hooks (requires minor exposure if needed).
    /// Currently polls activeSelf; lightweight.
    /// </summary>
    public class BannerVisibilityBridge : MonoBehaviour
    {
        private ColorBlast2.UI.Core.GameOverManager mgr;
        private bool lastState;
        private float checkInterval = 0.25f; float next;
        private void Awake(){ mgr = GetComponent<ColorBlast2.UI.Core.GameOverManager>(); }
        private void Update(){ if(Time.unscaledTime < next) return; next = Time.unscaledTime + checkInterval; if(mgr==null) return; bool panel = IsPanelVisible(); if(panel != lastState){ lastState = panel; if(panel) HideBanner(); else ShowBanner(); } }
        private bool IsPanelVisible(){ var f = typeof(ColorBlast2.UI.Core.GameOverManager).GetField("gameOverPanel", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); if(f==null) return false; var go = f.GetValue(mgr) as GameObject; return go!=null && go.activeSelf; }
    private void HideBanner(){
#if UNITY_ADS
        if(AdService.Exists) AdService.Instance.HideBanner();
#endif
    }
    private void ShowBanner(){
#if UNITY_ADS
        if(AdService.Exists && AdService.Instance.IsBannerReady()) AdService.Instance.ShowBanner();
#endif
    }
    }
}
