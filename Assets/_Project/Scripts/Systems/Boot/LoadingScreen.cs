using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace ColorBlast2.Systems.Boot
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("Scenes")] [SerializeField] private string firstSceneName = "MainMenu"; // or CoreGame
        [Header("UI")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private CanvasGroup fadeGroup;
        [Header("Timing")]
        [SerializeField] private float minScreenTime = 1.0f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private bool autoStart = true;
        [Tooltip("Smooth the displayed progress to avoid jumps.")] [SerializeField] private float progressSmoothing = 6f;

        [Header("Ads Integration (Optional)")]
        [Tooltip("If true, will wait for AdService initialization (or timeout) before activating next scene.")]
        [SerializeField] private bool waitForAdsInitialization = true;
        [SerializeField, Tooltip("Seconds to wait max for ads before continuing anyway.")] private float adsInitTimeout = 5f;
        [SerializeField, Tooltip("Weight (0-1) of ads init inside overall displayed progress when waiting.")] [Range(0.05f,0.5f)] private float adsProgressWeight = 0.25f;

        [Header("Tips")]
        [TextArea] [SerializeField] private string[] tips;
        [SerializeField] private bool cycleTips = true;
        [SerializeField] private float tipChangeInterval = 3.5f;

        private float displayedProgress = 0f;
        private float nextTipTime;
        private int tipIndex;

        private void Start()
        {
            if (autoStart) Begin();
        }

        [ContextMenu("Begin Load")] public void Begin()
        { StartCoroutine(LoadRoutine()); }

        private IEnumerator LoadRoutine()
        {
            if (fadeGroup) fadeGroup.alpha = 1f;
            float startTime = Time.realtimeSinceStartup;
            AsyncOperation op = SceneManager.LoadSceneAsync(firstSceneName);
            op.allowSceneActivation = false;

            // Tip init
            if (tipText && tips != null && tips.Length > 0)
            {
                tipIndex = UnityEngine.Random.Range(0, tips.Length);
                tipText.text = tips[tipIndex];
                nextTipTime = Time.realtimeSinceStartup + tipChangeInterval;
            }

            bool adsWaitSatisfied = !waitForAdsInitialization;
            float adsStartTime = Time.realtimeSinceStartup;

            while (!op.isDone)
            {
                // Scene progress (0..1)
                float sceneProgress = Mathf.Clamp01(op.progress / 0.9f);

                // Ads progress (0..1) if waiting
                float adsProgress = 1f;
                if (waitForAdsInitialization && !adsWaitSatisfied)
                {
                    adsProgress = 0f;
#if UNITY_ADS
                    if (Systems.Ads.AdService.Exists)
                    {
                        // Consider initialized when Advertisement.isInitialized or AdService reported callback (flag accessible? For now rely on Unity Ads global state)
                        bool init = UnityEngine.Advertisements.Advertisement.isInitialized;
                        if (init) adsProgress = 1f; else adsProgress = Mathf.Clamp01((Time.realtimeSinceStartup - adsStartTime)/adsInitTimeout * 0.8f); // time-based partial fill
                        if (init || (Time.realtimeSinceStartup - adsStartTime) >= adsInitTimeout)
                        {
                            adsWaitSatisfied = true;
                            adsProgress = 1f;
                        }
                    }
                    else
                    {
                        // No service present -> don't block
                        adsWaitSatisfied = true; adsProgress = 1f;
                    }
#else
                    adsWaitSatisfied = true; // Not compiling ads -> don't wait
#endif
                }

                // Combine for display only
                float combined = sceneProgress;
                if (waitForAdsInitialization)
                {
                    float w = adsProgressWeight;
                    combined = Mathf.Clamp01(sceneProgress * (1f - w) + adsProgress * w);
                }

                // Smooth
                displayedProgress = Mathf.MoveTowards(displayedProgress, combined, progressSmoothing * Time.unscaledDeltaTime * Mathf.Max(0.01f, 1f - displayedProgress));

                if (progressBar) progressBar.value = displayedProgress;
                if (progressText) progressText.text = Mathf.RoundToInt(displayedProgress * 100f) + "%";

                // Cycle tips
                if (cycleTips && tipText && tips != null && tips.Length > 1 && Time.realtimeSinceStartup >= nextTipTime)
                {
                    tipIndex = (tipIndex + 1) % tips.Length;
                    tipText.text = tips[tipIndex];
                    nextTipTime = Time.realtimeSinceStartup + tipChangeInterval;
                }

                bool timeOk = Time.realtimeSinceStartup - startTime >= minScreenTime;
                if (sceneProgress >= 1f && adsWaitSatisfied && timeOk)
                    break;

                yield return null;
            }

            // Fade out
            if (fadeGroup)
            {
                float t = 0f; float a0 = fadeGroup.alpha;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    fadeGroup.alpha = Mathf.Lerp(a0, 0f, t / fadeDuration);
                    yield return null;
                }
                fadeGroup.alpha = 0f;
            }
            op.allowSceneActivation = true;
        }
    }
}
