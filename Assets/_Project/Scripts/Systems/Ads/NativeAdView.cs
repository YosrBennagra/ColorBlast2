using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
using GoogleMobileAds.Api;
#endif

namespace ShapeBlaster.Systems.Ads
{
    /// <summary>
    /// Handles loading and displaying a single AdMob Native Advanced ad inside a Unity UI layout.
    /// Drop this on the root of your native ad prefab/layout and wire the optional bindings.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ads/AdMob Native Advanced View")]
    public class NativeAdView : MonoBehaviour
    {
        private const string ANDROID_TEST_NATIVE_ID = "ca-app-pub-3940256099942544/2247696110";
        private const string MissingNativeSupportMessage = "Google Mobile Ads native plugin not detected. Import the native templates package and add ADMOB_NATIVE_ADS to Scripting Define Symbols.";

        [Header("Ad Unit")]
        [SerializeField] private string androidAdUnitId = "ca-app-pub-9594729661204695/9030134902";
        [Tooltip("Use Google's test native ad ID when running in Editor/Development.")]
        [SerializeField] private bool forceTestIdInEditor = true;
        [Tooltip("Automatically request a native ad whenever this object becomes enabled.")]
        [SerializeField] private bool autoLoadOnEnable = true;
        [Tooltip("Delay before retrying after a load failure (seconds). Set <= 0 to disable retry.")]
        [SerializeField] private float retryDelaySeconds = 30f;

        [Header("UI Bindings")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Graphic headlineGraphic;
        [SerializeField] private Graphic bodyGraphic;
        [SerializeField] private Graphic advertiserGraphic;
        [SerializeField] private Graphic callToActionGraphic;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image mediaImage;
        [SerializeField] private Button callToActionButton;
        [SerializeField] private List<GameObject> extraClickableViews = new();

        public bool IsLoaded { get; private set; }
        public bool IsLoading { get; private set; }

        public event Action<NativeAdView> OnAdLoaded;
        public event Action<NativeAdView, string> OnAdFailedToLoad;

        private string resolvedAdUnitId;
        private readonly List<Sprite> generatedSprites = new();
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
        private AdLoader adLoader;
        private NativeAd nativeAd;
#endif

        private void Awake()
        {
            if (contentRoot == null) contentRoot = gameObject;
            resolvedAdUnitId = (androidAdUnitId ?? string.Empty).Trim();
            EnsureGraphicsDefaults();
        }

        private void OnEnable()
        {
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
            if (autoLoadOnEnable && !IsLoaded && !IsLoading)
            {
                LoadAd();
            }
#else
            if (contentRoot != null) contentRoot.SetActive(false);
#endif
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(LoadAd));
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(LoadAd));
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
            DisposeNativeAd();
#endif
            ClearGeneratedSprites();
        }

        /// <summary>
        /// Requests a new native ad. If one is already loading, the call is ignored.
        /// </summary>
        public void LoadAd()
        {
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
            if (IsLoading) return;

            string adUnitId = ResolveAdUnitId();
            if (string.IsNullOrEmpty(adUnitId))
            {
                Debug.LogError("[NativeAdView] No ad unit ID configured for this platform.");
                return;
            }

            if (!AdsInitializer.Initialized)
            {
                Debug.LogWarning("[NativeAdView] Google Mobile Ads is not initialized yet. Retrying in 1 second.");
                CancelInvoke(nameof(LoadAd));
                Invoke(nameof(LoadAd), 1f);
                return;
            }

            DisposeNativeAd();
            ClearGeneratedSprites();

            var builder = new AdLoader.Builder(adUnitId);
            builder.ForNativeAd();
            adLoader = builder.Build();
            adLoader.OnNativeAdLoaded += HandleNativeAdLoaded;
            adLoader.OnAdFailedToLoad += HandleNativeAdFailedToLoad;

            try
            {
                IsLoading = true;
                adLoader.LoadAd(new AdRequest());
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Debug.LogError("[NativeAdView] Exception while requesting native ad: " + ex.Message);
                ScheduleRetry();
            }
#else
            Debug.LogWarning("[NativeAdView] " + MissingNativeSupportMessage);
            IsLoading = false;
            IsLoaded = false;
            OnAdFailedToLoad?.Invoke(this, "Native ads unavailable in this build.");
#endif
        }

        /// <summary>
        /// Shows the view (enables the content root) and triggers a load if needed.
        /// </summary>
        public void Show()
        {
            if (contentRoot != null) contentRoot.SetActive(true);
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
            if (!IsLoaded && !IsLoading)
            {
                LoadAd();
            }
#endif
        }

        /// <summary>
        /// Hides the view (disables the content root).
        /// </summary>
        public void Hide()
        {
            if (contentRoot != null) contentRoot.SetActive(false);
        }

#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
        private void HandleNativeAdLoaded(NativeAd ad)
        {
            IsLoading = false;
            nativeAd = ad;
            IsLoaded = true;
            ApplyAdToView(ad);
            OnAdLoaded?.Invoke(this);
        }

        private void HandleNativeAdFailedToLoad(LoadAdError error)
        {
            IsLoading = false;
            IsLoaded = false;
            string msg = error != null ? error.GetMessage() : "unknown";
            Debug.LogWarning("[NativeAdView] Failed to load native ad: " + msg);
            OnAdFailedToLoad?.Invoke(this, msg);
            ScheduleRetry();
        }

        private void ApplyAdToView(NativeAd ad)
        {
            if (ad == null)
            {
                Debug.LogWarning("[NativeAdView] ApplyAdToView called with null native ad.");
                return;
            }

            if (contentRoot != null) contentRoot.SetActive(true);

            SetGraphicText(headlineGraphic, ad.GetHeadlineText());
            SetGraphicText(bodyGraphic, ad.GetBodyText());
            SetGraphicText(advertiserGraphic, ad.GetAdvertiserText());
            SetGraphicText(callToActionGraphic, ad.GetCallToActionText());

            if (callToActionButton != null)
            {
                callToActionButton.gameObject.SetActive(!string.IsNullOrEmpty(ad.GetCallToActionText()));
            }

            ApplyTexture(iconImage, ad.GetIconTexture());
            ApplyTexture(mediaImage, ad.GetImageTexture(0));

            RegisterClickableViews(ad);
        }

        private void RegisterClickableViews(NativeAd ad)
        {
            if (ad == null) return;

            List<GameObject> clickableViews = new();
            if (callToActionButton != null) clickableViews.Add(callToActionButton.gameObject);
            if (extraClickableViews != null)
            {
                foreach (var go in extraClickableViews)
                {
                    if (go != null && !clickableViews.Contains(go)) clickableViews.Add(go);
                }
            }

            var impressionRoot = contentRoot != null ? contentRoot : gameObject;

            try
            {
                ad.RegisterGameObjectForImpression(impressionRoot, clickableViews);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NativeAdView] Failed to register native ad views: " + ex.Message);
            }
        }

        private void DisposeNativeAd()
        {
            if (nativeAd != null)
            {
                try { nativeAd.Destroy(); }
                catch (Exception ex) { Debug.LogWarning("[NativeAdView] Exception destroying native ad: " + ex.Message); }
                nativeAd = null;
            }
            IsLoaded = false;
        }
#endif

        private void ScheduleRetry()
        {
#if GOOGLE_MOBILE_ADS && ADMOB_NATIVE_ADS
            if (retryDelaySeconds > 0f)
            {
                CancelInvoke(nameof(LoadAd));
                Invoke(nameof(LoadAd), retryDelaySeconds);
            }
#endif
        }

        private string ResolveAdUnitId()
        {
#if UNITY_EDITOR
            if (forceTestIdInEditor) return ANDROID_TEST_NATIVE_ID;
            return resolvedAdUnitId;
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                return string.IsNullOrEmpty(resolvedAdUnitId) ? ANDROID_TEST_NATIVE_ID : resolvedAdUnitId;
            }
            Debug.LogWarning("[NativeAdView] Native ads only configured for Android in this build.");
            return null;
#endif
        }

        private void EnsureGraphicsDefaults()
        {
            if (headlineGraphic != null) SetGraphicEnabled(headlineGraphic, false);
            if (bodyGraphic != null) SetGraphicEnabled(bodyGraphic, false);
            if (advertiserGraphic != null) SetGraphicEnabled(advertiserGraphic, false);
            if (callToActionGraphic != null) SetGraphicEnabled(callToActionGraphic, false);
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (mediaImage != null) mediaImage.gameObject.SetActive(false);
            if (callToActionButton != null) callToActionButton.gameObject.SetActive(false);
        }

        private void SetGraphicText(Graphic graphic, string value)
        {
            if (graphic == null) return;

            bool hasValue = !string.IsNullOrEmpty(value);
            SetGraphicEnabled(graphic, hasValue);

#if TMP_PRESENT
            if (graphic is TMP_Text tmpText)
            {
                tmpText.text = hasValue ? value : string.Empty;
                return;
            }
#endif
            if (graphic is Text uiText)
            {
                uiText.text = hasValue ? value : string.Empty;
                return;
            }
        }

        private void SetGraphicEnabled(Graphic graphic, bool enabled)
        {
            if (graphic == null) return;
            graphic.gameObject.SetActive(enabled);
        }

        private void ApplyTexture(Image imageTarget, Texture2D texture)
        {
            if (imageTarget == null)
            {
                return;
            }

            if (texture == null)
            {
                imageTarget.sprite = null;
                imageTarget.gameObject.SetActive(false);
                return;
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            generatedSprites.Add(sprite);
            imageTarget.sprite = sprite;
            imageTarget.gameObject.SetActive(true);
        }

        private void ClearGeneratedSprites()
        {
            if (generatedSprites.Count == 0) return;
            foreach (var sprite in generatedSprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }
            generatedSprites.Clear();
        }
    }
}

