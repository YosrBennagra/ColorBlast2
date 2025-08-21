using UnityEngine;
using System;

namespace ColorBlast2.UI.Core
{
    /// <summary>
    /// Simple game over detector: shows a Game Over panel when none of the current tray shapes
    /// can be placed anywhere on the grid (i.e. no valid moves remain).
    /// For now it ONLY activates the panel (no score logic, no restart handling).
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ShapeSpawner that manages the current tray of shapes.")]
        [SerializeField] private ShapeSpawner shapeSpawner;
        [Tooltip("Optional explicit GridManager reference (auto-found if left null).")]
        [SerializeField] private Gameplay.GridManager gridManager;
        [Tooltip("The Game Over UI panel to activate when no moves remain.")]
        [SerializeField] private GameObject gameOverPanel;
    [Header("Audio")]
    [Tooltip("Optional AudioSource used for playing the game over SFX (will be added automatically if missing).")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Clip played once when game over triggers.")]
    [SerializeField] private AudioClip gameOverSfx;
    [Range(0f,1f)] [SerializeField] private float gameOverSfxVolume = 1f;

    [Header("Sorting / Layering")]
    [Tooltip("Sorting order to apply to the Game Over panel's Canvas (must be higher than other UI panels).")]
    [SerializeField] private int gameOverSortingOrder = 2000;
    [Tooltip("Force the Game Over canvas to Screen Space Overlay (safest to appear above world sprites).")]
    [SerializeField] private bool forceScreenSpaceOverlay = true;
    [Tooltip("Dynamically raise sorting order above all SpriteRenderers when shown.")]
    [SerializeField] private bool autoRaiseAboveSpritesOnShow = true;
    [Tooltip("Extra sorting order padding added atop the highest SpriteRenderer order.")]
    [SerializeField] private int spriteOrderPadding = 50;
    [Tooltip("Continuously enforce overrideSorting & sorting order every frame while panel is visible.")]
    [SerializeField] private bool enforceWhileVisible = true;

        [Header("Detection Settings")]
        [Tooltip("How often (in seconds) to check for no-move condition.")]
        [SerializeField] private float checkInterval = 0.5f;
        [Tooltip("Log a message to the console when game over triggers.")]
        [SerializeField] private bool logOnGameOver = true;

        private bool gameOverTriggered = false;
        private float lastCheckTime = 0f;

    [Header("Ad / Revive UI")]
    [Tooltip("Countdown seconds before auto interstitial + end.")]
    [SerializeField] private float countdownSeconds = 5f;
    [Tooltip("Text element that displays remaining seconds.")]
    [SerializeField] private TMPro.TextMeshProUGUI countdownText;
    [Tooltip("Revive (watch ad) button.")]
    [SerializeField] private UnityEngine.UI.Button watchAdButton;
    [Tooltip("Animator for panel show / countdown pulse.")]
    [SerializeField] private Animator panelAnimator;
    [Tooltip("Name of the trigger to play show animation.")]
    [SerializeField] private string showTrigger = "Show";
    [Tooltip("Name of the trigger to play pulse animation each second.")]
    [SerializeField] private string tickTrigger = "Tick";
    [Tooltip("Scene name to load when game ends (after short ad).")]
    [SerializeField] private string gameEndSceneName = "GameEnd";
    [Tooltip("Force at least this many guaranteed-fit shapes on revive (2 per spec).")]
    [SerializeField] private int guaranteedFitShapesOnRevive = 2;
    [Tooltip("Total shapes to spawn on revive (fallback 3).")]
    [SerializeField] private int reviveSpawnTotal = 3;

    private bool countdownActive = false;
    private float countdownRemaining;
    private bool reviveChosen = false;
    private bool awaitingAd = false;
    [Header("Ads Integration")]
    [Tooltip("If true, will attempt to use AdService (Unity Ads / provider) instead of simulation.")]
    [SerializeField] private bool useRealAds = true;
    [Tooltip("Fallback simulate durations if real ads not available.")]
    [SerializeField] private float simulateInterstitialDuration = 1.0f;
    [SerializeField] private float simulateRewardedDuration = 2.5f;
    [Tooltip("Log simulation fallback messages.")]
    [SerializeField] private bool logSimFallback = true;

        private void Awake()
        {
            // Auto-find references if not assigned
            if (shapeSpawner == null) shapeSpawner = FindFirstObjectByType<ShapeSpawner>();
            if (gridManager == null) gridManager = FindFirstObjectByType<Gameplay.GridManager>();
            if (gameOverPanel != null) gameOverPanel.SetActive(false); // Ensure hidden at start
            SetupPanelCanvas();
            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveAllListeners();
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            }
            if (audioSource == null && gameOverSfx != null)
            {
                audioSource = gameOverPanel != null ? gameOverPanel.GetComponent<AudioSource>() : null;
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void SetupPanelCanvas()
        {
            if (gameOverPanel == null) return;
            var canvas = gameOverPanel.GetComponent<Canvas>();
            if (canvas == null) canvas = gameOverPanel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            if (forceScreenSpaceOverlay)
            {
                canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            }
            // Only raise sorting order if current is lower
            if (canvas.sortingOrder < gameOverSortingOrder)
                canvas.sortingOrder = gameOverSortingOrder;
            // Ensure a GraphicRaycaster exists so buttons work atop paused gameplay
            if (gameOverPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameOverPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        private void Update()
        {
            // If visible and enforcement enabled, keep override sorting applied
            if (enforceWhileVisible && gameOverPanel != null && gameOverPanel.activeSelf)
            {
                EnforceChildCanvasOverride();
            }

            if (gameOverTriggered) return; // Already shown
            if (shapeSpawner == null) return; // Can't evaluate
            if (gameOverPanel == null) return; // Nothing to show yet

            if (Time.unscaledTime - lastCheckTime < checkInterval) return;
            lastCheckTime = Time.unscaledTime;

            // If all shapes are placed the spawner will shortly spawn a new set; not a game over
            if (shapeSpawner.AreAllShapesPlaced()) return;

            // HasAnyValidMove() now returns true if either: there is at least one unplaced shape with a valid spot OR no unplaced shapes exist.
            // We only trigger game over when there is at least one unplaced shape AND no valid moves for it.
            bool anyValid = shapeSpawner.HasAnyValidMove();
            if (anyValid) return;

            TriggerGameOver();
        }

        private void TriggerGameOver()
        {
            gameOverTriggered = true;
            // Re-assert canvas sorting just before showing
            SetupPanelCanvas();
            if (autoRaiseAboveSpritesOnShow)
            {
                RaiseAboveAllSprites();
            }
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            EnforceChildCanvasOverride();
            if (logOnGameOver)
            {
                Debug.Log("GameOverManager: No valid moves remain. Game Over panel displayed.");
            }
            PlayGameOverSfx();
            // Start countdown for auto short ad -> end
            StartCountdown();
            if (panelAnimator != null && !string.IsNullOrEmpty(showTrigger)) panelAnimator.SetTrigger(showTrigger);
            // (Keep it simple: don't alter Time.timeScale here per user request.)
        }

        private void PlayGameOverSfx()
        {
            if (gameOverSfx == null) return;
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
            audioSource.PlayOneShot(gameOverSfx, gameOverSfxVolume);
        }

        // Scan all SpriteRenderers and push canvas sorting above highest order
        private void RaiseAboveAllSprites()
        {
            if (gameOverPanel == null) return;
            var canvas = gameOverPanel.GetComponent<Canvas>();
            if (canvas == null) return;
            int maxOrder = int.MinValue;
            var sprites = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                if (sprites[i].sortingOrder > maxOrder) maxOrder = sprites[i].sortingOrder;
            }
            if (maxOrder < 0) maxOrder = 0;
            int target = maxOrder + Mathf.Abs(spriteOrderPadding);
            if (target <= canvas.sortingOrder) return;
            canvas.sortingOrder = target;
            EnforceChildCanvasOverride();
        }

        private void EnforceChildCanvasOverride()
        {
            if (gameOverPanel == null) return;
            var canvases = gameOverPanel.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvases[i].overrideSorting = true;
                if (canvases[i].sortingOrder < gameOverSortingOrder)
                    canvases[i].sortingOrder = gameOverSortingOrder;
            }
        }

        private void StartCountdown()
        {
            countdownRemaining = countdownSeconds;
            countdownActive = true;
            UpdateCountdownLabel(force:true);
        }

        private void LateUpdate()
        {
            if (!countdownActive || reviveChosen || awaitingAd) return;
            if (countdownRemaining > 0f)
            {
                float prev = Mathf.Ceil(countdownRemaining);
                countdownRemaining -= Time.unscaledDeltaTime;
                float now = Mathf.Ceil(countdownRemaining);
                if (now != prev && panelAnimator != null && !string.IsNullOrEmpty(tickTrigger))
                {
                    panelAnimator.SetTrigger(tickTrigger);
                }
                UpdateCountdownLabel();
            }
            else
            {
                countdownActive = false;
                // Auto interstitial ad then end game
                PlayInterstitialThenEnd();
            }
        }

        private void UpdateCountdownLabel(bool force = false)
        {
            if (countdownText == null) return;
            int seconds = Mathf.Max(0, Mathf.CeilToInt(countdownRemaining));
            countdownText.text = seconds.ToString();
        }

        private void OnWatchAdClicked()
        {
            if (reviveChosen || awaitingAd) return;
            reviveChosen = true;
            countdownActive = false;
        PlayRewardedThenRevive();
        }

    private void PlayInterstitialThenEnd()
        {
            if (awaitingAd) return;
            awaitingAd = true;
            // Prefer AdsBridge (uses your Ads scripts, simulates if not ready)
            var bridge = FindFirstObjectByType<AdsBridge>();
            if (useRealAds && bridge != null)
            {
        bridge.ShowInterstitial(HandleInterstitialFinished);
            }
            else
            {
                FallbackSimInterstitial();
            }
        }

    private void HandleInterstitialFinished()
        {
            EndGame();
        }

    private void PlayRewardedThenRevive()
        {
            if (awaitingAd) return;
            awaitingAd = true;
            var bridge = FindFirstObjectByType<AdsBridge>();
            if (useRealAds && bridge != null)
            {
        bridge.ShowRewarded(success => { if (success) HandleRewardedFinished(); else HandleInterstitialFinished(); });
            }
            else
            {
                FallbackSimRewarded();
            }
        }

    private void HandleRewardedFinished()
        {
            RevivePlayer();
        }

        private void RevivePlayer()
        {
            awaitingAd = false;
            // Hide panel
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            gameOverTriggered = false; // allow future game over
            reviveChosen = false;
            // Spawn new shapes with guarantee
            SpawnReviveShapes();
        }

        private void SpawnReviveShapes()
        {
            if (shapeSpawner == null)
            {
                shapeSpawner = FindFirstObjectByType<ShapeSpawner>();
                if (shapeSpawner == null) return;
            }
            // Force spawn new shapes
            shapeSpawner.DestroyUnplacedTrayShapes();
            shapeSpawner.ForceSpawnNewShapes();
            // After spawning, we ensure at least guaranteedFitShapesOnRevive shapes are placeable by re-spawning if needed (limited attempts)
            int attempts = 3;
            while (attempts-- > 0)
            {
                int placeable = CountPlaceableTrayShapes();
                if (placeable >= guaranteedFitShapesOnRevive) break;
                shapeSpawner.DestroyUnplacedTrayShapes();
                shapeSpawner.ForceSpawnNewShapes();
            }
        }

        private int CountPlaceableTrayShapes()
        {
            if (shapeSpawner == null) return 0;
            var gm = gridManager != null ? gridManager : FindFirstObjectByType<Gameplay.GridManager>();
            if (gm == null) return 0;
            var field = typeof(ShapeSpawner).GetField("currentShapes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return 0;
            var arr = field.GetValue(shapeSpawner) as System.Array;
            if (arr == null) return 0;
            int count = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                var go = arr.GetValue(i) as GameObject;
                if (go == null) continue;
                var s = go.GetComponent<ColorBlast.Game.Shape>();
                if (s == null || s.IsPlaced) continue;
                var offs = s.ShapeOffsets;
                if (offs == null || offs.Count == 0) continue;
                if (HasPlacement(offs, gm)) count++;
            }
            return count;
        }

        private bool HasPlacement(System.Collections.Generic.List<Vector2Int> offs, Gameplay.GridManager gm)
        {
            int W = gm.GridWidth, H = gm.GridHeight;
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (gm.CanPlaceShape(new Vector2Int(x, y), offs)) return true;
                }
            }
            return false;
        }

        private void EndGame()
        {
            awaitingAd = false;
            // Load end scene
            if (!string.IsNullOrEmpty(gameEndSceneName))
            {
                // Persist final score for GameEnd scene UI
                var scoreMgr = FindFirstObjectByType<ColorBlast2.Systems.Scoring.ScoreManager>();
                if (scoreMgr != null)
                {
                    PlayerPrefs.SetInt("LastScore", scoreMgr.GetScore());
                    PlayerPrefs.Save();
                }
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameEndSceneName);
            }
        }

        private void FallbackSimInterstitial()
        {
            if (logSimFallback) Debug.Log("[GameOverManager] Interstitial fallback simulation");
            StartCoroutine(SimRoutine(simulateInterstitialDuration, HandleInterstitialFinished));
        }

        private void FallbackSimRewarded()
        {
            if (logSimFallback) Debug.Log("[GameOverManager] Rewarded fallback simulation");
            StartCoroutine(SimRoutine(simulateRewardedDuration, HandleRewardedFinished));
        }

        private System.Collections.IEnumerator SimRoutine(float d, Action done)
        {
            float t = 0f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            done?.Invoke();
        }

        /// <summary>
        /// Optional manual reset (e.g. after restart). Hides the panel and resumes detection.
        /// </summary>
        public void ResetGameOver()
        {
            gameOverTriggered = false;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            lastCheckTime = 0f;
            SetupPanelCanvas(); // Re-assert layering after reset (e.g., on scene reload)
        }
    }
}
