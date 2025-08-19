using UnityEngine;
using UnityEngine.UI;

namespace ColorBlast2.Systems.Ads
{
    /// <summary>
    /// Shows a colored placeholder where the banner ad will appear.
    /// Attach this to any GameObject under a Canvas (Screen Space) to preview banner position.
    /// In Play Mode it can auto-hide once a real banner is ready/shown.
    /// Works even if UNITY_ADS is not defined.
    /// </summary>
    [ExecuteAlways]
    public class BannerPreview : MonoBehaviour
    {
        public enum PreviewBannerPosition
        {
            TOP_LEFT, TOP_CENTER, TOP_RIGHT,
            BOTTOM_LEFT, BOTTOM_CENTER, BOTTOM_RIGHT,
            CENTER
        }

        [Header("Preview Settings")] 
        [SerializeField] private PreviewBannerPosition position = PreviewBannerPosition.BOTTOM_CENTER;
        [SerializeField] private Vector2 bannerSizeReference = new Vector2(320, 50); // typical phone banner
        [Tooltip("Scale banner size by current screen DPI vs 160 (approx). 0 disables dynamic scaling.")]
        [SerializeField] private float dpiScaleBase = 160f;
        [SerializeField] private Color fillColor = new Color(0f, 0.5f, 1f, 0.25f);
        [SerializeField] private Color borderColor = new Color(0f, 0.5f, 1f, 0.9f);
        [SerializeField] private float borderThickness = 2f;
        [SerializeField] private bool label = true;
        [SerializeField] private string labelText = "BANNER";
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField] private int labelFontSize = 20;
        [Tooltip("Hide preview automatically in Play Mode when a real banner is ready (needs AdService).")]
        [SerializeField] private bool autoHideWhenBannerReady = true;
        [Tooltip("Keep preview visible even after banner is shown (debug overlay).")]
        [SerializeField] private bool keepVisibleOverride = false;

        private RectTransform rect;
        private Image fillImage;
        private Outline outline;
        private Text labelUi;

        private void Awake()
        {
            EnsureComponents();
            Apply();
        }

        private void OnEnable()
        {
            EnsureComponents();
            Apply();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                Apply();
            }
            else if (autoHideWhenBannerReady && !keepVisibleOverride)
            {
#if UNITY_ADS
                if (ColorBlast2.Systems.Ads.AdService.Exists && ColorBlast2.Systems.Ads.AdService.Instance.IsBannerReady())
                {
                    gameObject.SetActive(false);
                }
#endif
            }
        }

        private void EnsureComponents()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
                if (fillImage == null) fillImage = gameObject.AddComponent<Image>();
            }
            if (outline == null)
            {
                outline = GetComponent<Outline>();
                if (outline == null) outline = gameObject.AddComponent<Outline>();
            }
            if (label && labelUi == null)
            {
                var existing = GetComponentInChildren<Text>();
                if (existing != null && existing.gameObject != gameObject)
                {
                    labelUi = existing;
                }
                else
                {
                    var go = new GameObject("BannerPreviewLabel");
                    go.transform.SetParent(transform);
                    var rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                    labelUi = go.AddComponent<Text>();
                    labelUi.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private void Apply()
        {
            if (rect == null) return;

            float scale = 1f;
            if (dpiScaleBase > 0 && Screen.dpi > 0)
            {
                scale = Screen.dpi / dpiScaleBase;
                scale = Mathf.Clamp(scale, 0.75f, 2.2f);
            }
            Vector2 size = bannerSizeReference * scale;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            Vector2 aMin, aMax, anchored;
            GetAnchors(out aMin, out aMax, out anchored);
            rect.anchorMin = aMin; rect.anchorMax = aMax; rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = anchored;

            if (fillImage != null)
            {
                fillImage.color = fillColor;
                fillImage.raycastTarget = false;
            }
            if (outline != null)
            {
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(borderThickness, borderThickness);
            }
            if (label)
            {
                if (labelUi != null)
                {
                    labelUi.text = labelText;
                    labelUi.color = labelColor;
                    labelUi.fontSize = labelFontSize;
                    labelUi.raycastTarget = false;
                    if (labelUi.font == null) labelUi.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            else if (labelUi != null)
            {
                labelUi.gameObject.SetActive(false);
            }
        }

        private void GetAnchors(out Vector2 aMin, out Vector2 aMax, out Vector2 anchored)
        {
            aMin = aMax = new Vector2(0.5f, 0.5f); // default center
            anchored = Vector2.zero;
            switch (position)
            {
                case PreviewBannerPosition.TOP_LEFT:
                    aMin = aMax = new Vector2(0f, 1f); anchored = new Vector2(bannerSizeReference.x/2f, -bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.TOP_CENTER:
                    aMin = aMax = new Vector2(0.5f, 1f); anchored = new Vector2(0f, -bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.TOP_RIGHT:
                    aMin = aMax = new Vector2(1f, 1f); anchored = new Vector2(-bannerSizeReference.x/2f, -bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.BOTTOM_LEFT:
                    aMin = aMax = new Vector2(0f, 0f); anchored = new Vector2(bannerSizeReference.x/2f, bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.BOTTOM_CENTER:
                    aMin = aMax = new Vector2(0.5f, 0f); anchored = new Vector2(0f, bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.BOTTOM_RIGHT:
                    aMin = aMax = new Vector2(1f, 0f); anchored = new Vector2(-bannerSizeReference.x/2f, bannerSizeReference.y/2f); break;
                case PreviewBannerPosition.CENTER:
                    aMin = aMax = new Vector2(0.5f,0.5f); anchored = Vector2.zero; break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureComponents();
                Apply();
            }
        }
#endif
    }
}
