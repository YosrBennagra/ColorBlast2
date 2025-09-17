
using UnityEngine;
using TMPro;
using Gameplay;
using System.Collections;





namespace ShapeBlaster.Systems.Scoring
{
    public class ScoreManager : MonoBehaviour
    {
    public static System.Action<int> OnScoreChanged;
    private enum ComboSpawnMode { GridCenter, AboveScoreText }
        [Header("UI References")]
    public ShapeBlaster.UI.Core.CoreScoreDisplay scoreDisplay;
        public TextMeshProUGUI comboText;
        public Transform comboSpriteParent;
        public GameObject comboSpritePrefab;
    [Header("Combo Sprite Placement Preview")] 
    [Tooltip("Show a gizmo where the combo sprite will spawn (editor only).")]
    [SerializeField] private bool showComboSpawnGizmo = true;
    [Tooltip("Extra world offset applied to the computed grid center position.")]
    [SerializeField] private Vector3 comboWorldOffset = Vector3.zero;
    [Tooltip("If true and parent is a Screen Space / Camera canvas, position will be converted to anchored UI coordinates.")]
    [SerializeField] private bool adaptToParentCanvas = true;
    [Tooltip("Optional override canvas; if null uses comboSpriteParent canvas (if any).")]
    [SerializeField] private Canvas comboCanvasOverride;
    [Tooltip("Lifetime (seconds) before the combo sprite is auto destroyed (excludes pop animation time).")]
    [SerializeField] private float comboSpriteExtraLifetime = 0.5f;
    [Tooltip("Donâ€™t destroy spawned combo sprite automatically (useful for debugging placement).")]
    [SerializeField] private bool keepComboSpriteForDebug = false;
    [Tooltip("Scale multiplier applied after pop animation (1 = original). Useful to audition final resting size.")]
    [SerializeField] private float postPopRestScale = 1f;
    [Tooltip("Choose where the combo sprite appears.")]
    [SerializeField] private ComboSpawnMode comboSpawnMode = ComboSpawnMode.GridCenter;
    [Tooltip("Offset applied when spawning AboveScoreText (local UI units if UI element, world units otherwise).")]
    [SerializeField] private Vector3 scoreTextOffset = new Vector3(0f, 60f, 0f);
    [Tooltip("If true, use an absolute final scale instead of multiplier.")]
    [SerializeField] private bool useAbsoluteFinalScale = true;
    [Tooltip("Absolute scale to apply after pop animation when useAbsoluteFinalScale is true (taken from your reference screenshot).")]
    [SerializeField] private Vector3 absoluteFinalScale = new Vector3(0.2910315f, 0.2427592f, 1f);
    [Tooltip("Force sprite renderers on the popup to this sorting order (keeps it above score text). -1 disables.")]
    [SerializeField] private int forceSortingOrder = 1200;
    [Tooltip("Master toggle to enable/disable the combo sprite popup visuals (prefab instantiation). Combo text still works.")]
    [SerializeField] private bool enableComboSprite = false;

        [Header("Scoring Settings")]
        public int pointsPerBlock = 10;
        public int lineClearBonus = 100;
        public int sameSpriteLineBonus = 200;
        public float comboTimeWindow = 2f;
        public float comboSpritePopScale = 1.5f;
        public float comboSpritePopDuration = 0.4f;
    [Tooltip("Bonus points when the board is entirely cleared in one cascade.")]
    public int perfectClearBonus = 1000;

        [Header("Score Animation")]
        [SerializeField, Range(1.0f, 1.5f)] private float scorePopScale = 1.08f;
        [SerializeField, Range(0.05f, 0.6f)] private float scorePopDuration = 0.22f;

    private int score = 0;
    private int highScore = 0;
    private int displayedScore = 0;
    private int displayedHighScore = 0;
        private int comboCount = 0;
        private float comboTimer = 0f;
        private bool comboActive = false;

    private Coroutine scoreAnimRoutine;
    private Coroutine scorePunchRoutine;
    private Coroutine highScoreAnimRoutine;
    private Coroutine comboSpriteRoutine;
    private Coroutine rainbowScoreRoutine;
    private Coroutine rainbowHighScoreRoutine;

        private void Awake()
        {
            highScore = PlayerPrefs.GetInt("HighScore", 0);
            displayedScore = score;
            displayedHighScore = highScore;
            UpdateScoreUI();
        }

        private void OnValidate()
        {
            scorePopScale = Mathf.Clamp(scorePopScale, 1f, 1.5f);
            scorePopDuration = Mathf.Clamp(scorePopDuration, 0.05f, 0.6f);
            scoreBaseScaleCaptured = false;
        }

        private void Update()
        {
            if (comboActive)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    comboActive = false;
                    comboCount = 0;
                    if (comboText) comboText.gameObject.SetActive(false);
                }
            }
        }

        // Call this when a shape is placed (before line clear check)
        public void AddShapePlacementPoints(int shapeSize)
        {
            int placementPoints = Mathf.Max(1, shapeSize) * Mathf.RoundToInt(pointsPerBlock * 0.5f);
            AddScore(placementPoints);
        }

        public void AddBlockPoints(int count)
        {
            AddScore(count * pointsPerBlock);
            placementsSinceLastClear = 0; // Reset placement counter on clear
        }
    private int placementsSinceLastClear = 0;

        public void AddLineClearBonus(bool isSameSpriteLine)
        {
            int bonus = lineClearBonus;
            if (isSameSpriteLine) bonus += sameSpriteLineBonus;
            AddScore(bonus);
            IncrementCombo();
        }

        private void AddScore(int amount)
        {
            int oldScore = score;
            score += amount;
            bool wasAboveHighScore = (score - amount) > highScore;
            bool isNowAboveHighScore = score > highScore;
            if (score > highScore)
            {
                highScore = score;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
                AnimateHighScore();
                AnimateHighScoreIncrement(displayedHighScore, highScore);
            }
            AnimateScore();
            AnimateScoreIncrement(displayedScore, score);

            // Notify listeners (e.g., Adventure objectives)
            OnScoreChanged?.Invoke(score);

            // Rainbow animation for score text only when ACTUAL score is above high score
            if (isNowAboveHighScore)
            {
                if (rainbowScoreRoutine == null && scoreDisplay && scoreDisplay.scoreText)
                    rainbowScoreRoutine = StartCoroutine(RainbowTextRoutine(scoreDisplay.scoreText));
                if (rainbowHighScoreRoutine == null && scoreDisplay && scoreDisplay.highScoreText)
                    rainbowHighScoreRoutine = StartCoroutine(RainbowTextRoutine(scoreDisplay.highScoreText));
            }
            else
            {
                if (rainbowScoreRoutine != null)
                {
                    StopCoroutine(rainbowScoreRoutine);
                    rainbowScoreRoutine = null;
                    // Reset text color to white
                    if (scoreDisplay && scoreDisplay.scoreText)
                    {
                        var text = scoreDisplay.scoreText;
                        text.ForceMeshUpdate();
                        var mesh = text.mesh;
                        var colors = mesh.colors32;
                        for (int i = 0; i < colors.Length; i++)
                            colors[i] = Color.white;
                        mesh.colors32 = colors;
                        text.canvasRenderer.SetMesh(mesh);
                    }
                }
                if (rainbowHighScoreRoutine != null)
                {
                    StopCoroutine(rainbowHighScoreRoutine);
                    rainbowHighScoreRoutine = null;
                    // Reset high score text color to white
                    if (scoreDisplay && scoreDisplay.highScoreText)
                    {
                        var text = scoreDisplay.highScoreText;
                        text.ForceMeshUpdate();
                        var mesh = text.mesh;
                        var colors = mesh.colors32;
                        for (int i = 0; i < colors.Length; i++)
                            colors[i] = Color.white;
                        mesh.colors32 = colors;
                        text.canvasRenderer.SetMesh(mesh);
                    }
                }
            }
        }

        // Call when a full-board clear happens
        public void AddPerfectClearBonus()
        {
            if (perfectClearBonus <= 0) return;
            AddScore(perfectClearBonus);
            // Optional: immediate combo popup even if no combo active
            if (enableComboSprite) PopComboSprite();
        }

        private void IncrementCombo()
        {
            comboCount++;
            comboTimer = comboTimeWindow;
            comboActive = true;
            placementsSinceLastClear = 0;
            if (comboText)
            {
                if (comboCount > 1)
                {
                    comboText.text = $"COMBO x{comboCount}!";
                    comboText.gameObject.SetActive(true);
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }
            if (enableComboSprite) PopComboSprite();
        }

        // Call this after every shape placement (even if no line is cleared)
        public void OnShapePlacedNoClear()
        {
            placementsSinceLastClear++;
            if (comboActive && placementsSinceLastClear >= 2)
            {
                comboActive = false;
                comboCount = 0;
                if (comboText) comboText.gameObject.SetActive(false);
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreDisplay)
            {
                scoreDisplay.SetScore(displayedScore);
                scoreDisplay.SetHighScore(displayedHighScore);
            }
        }

        private Vector3 scoreBaseScale = Vector3.one;
        private bool scoreBaseScaleCaptured = false;

        private void AnimateScore()
        {
            if (scoreDisplay && scoreDisplay.scoreText)
            {
                RectTransform rect = scoreDisplay.scoreText.rectTransform;
                if (!scoreBaseScaleCaptured)
                {
                    scoreBaseScale = rect.localScale;
                    scoreBaseScaleCaptured = true;
                }

                if (scorePunchRoutine != null)
                {
                    StopCoroutine(scorePunchRoutine);
                    rect.localScale = scoreBaseScale;
                }

                scorePunchRoutine = StartCoroutine(SafePunchScore(rect));
            }
        }
        private void AnimateScoreIncrement(int from, int to)
        {
            if (scoreAnimRoutine != null) StopCoroutine(scoreAnimRoutine);
            scoreAnimRoutine = StartCoroutine(AnimateScoreValue(from, to));
        }

        private void AnimateHighScoreIncrement(int from, int to)
        {
            if (highScoreAnimRoutine != null) StopCoroutine(highScoreAnimRoutine);
            highScoreAnimRoutine = StartCoroutine(AnimateHighScoreValue(from, to));
        }

        private IEnumerator AnimateScoreValue(int from, int to)
        {
            float duration = Mathf.Clamp(Mathf.Log(Mathf.Abs(to - from) + 1) * 0.25f, 0.2f, 1.2f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Pow(t / duration, 0.7f); // ease out
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(from, to, p));
            UpdateScoreUI();
                yield return null;
            }
            displayedScore = to;
            UpdateScoreUI();
        }
            // Stops the rainbow color animation for score and high score
            private void StopRainbowAnimations()
            {
                if (rainbowScoreRoutine != null)
                {
                    StopCoroutine(rainbowScoreRoutine);
                    rainbowScoreRoutine = null;
                    if (scoreDisplay && scoreDisplay.scoreText)
                        scoreDisplay.scoreText.ForceMeshUpdate();
                }
                if (rainbowHighScoreRoutine != null)
                {
                    StopCoroutine(rainbowHighScoreRoutine);
                    rainbowHighScoreRoutine = null;
                    if (scoreDisplay && scoreDisplay.highScoreText)
                        scoreDisplay.highScoreText.ForceMeshUpdate();
                }
            }

        private IEnumerator AnimateHighScoreValue(int from, int to)
        {
            float duration = Mathf.Clamp(Mathf.Log(Mathf.Abs(to - from) + 1) * 0.25f, 0.2f, 1.2f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Pow(t / duration, 0.7f);
                displayedHighScore = Mathf.RoundToInt(Mathf.Lerp(from, to, p));
            UpdateScoreUI();
                yield return null;
            }
            displayedHighScore = to;
            UpdateScoreUI();
        }

        private void StartRainbowAnimations()
        {
            if (scoreDisplay && scoreDisplay.scoreText)
            {
                if (rainbowScoreRoutine != null) StopCoroutine(rainbowScoreRoutine);
                rainbowScoreRoutine = StartCoroutine(RainbowTextRoutine(scoreDisplay.scoreText));
            }
            if (scoreDisplay && scoreDisplay.highScoreText)
            {
                if (rainbowHighScoreRoutine != null) StopCoroutine(rainbowHighScoreRoutine);
                rainbowHighScoreRoutine = StartCoroutine(RainbowTextRoutine(scoreDisplay.highScoreText));
            }
        }

        private IEnumerator RainbowTextRoutine(TMP_Text text)
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime * 1.5f;
                int len = text.text.Length;
                text.ForceMeshUpdate();
                var mesh = text.mesh;
                var colors = mesh.colors32;
                for (int i = 0; i < len; i++)
                {
                    float hue = Mathf.Repeat((t * 0.5f) + (i * 0.18f), 1f);
                    Color32 c = Color.HSVToRGB(hue, 0.8f, 1f);
                    int charIndex = text.textInfo.characterInfo[i].vertexIndex;
                    if (charIndex + 3 < colors.Length)
                    {
                        colors[charIndex + 0] = c;
                        colors[charIndex + 1] = c;
                        colors[charIndex + 2] = c;
                        colors[charIndex + 3] = c;
                    }
                }
                mesh.colors32 = colors;
                text.canvasRenderer.SetMesh(mesh);
                yield return null;
            }
        }

        private void AnimateHighScore()
        {
            if (scoreDisplay && scoreDisplay.highScoreText)
            {
                // Remove scaling effect: just do a brief color flash
                if (highScoreAnimRoutine != null) StopCoroutine(highScoreAnimRoutine);
                highScoreAnimRoutine = StartCoroutine(FlashHighScoreColor(scoreDisplay.highScoreText, Color.yellow, 0.45f));
            }
        }

        private IEnumerator FlashHighScoreColor(TMP_Text text, Color flashColor, float duration)
        {
            if (text == null) yield break;
            Color original = text.color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.PingPong(t * 4f, 1f); // fast pulse
                text.color = Color.Lerp(original, flashColor, p);
                yield return null;
            }
            text.color = original;
        }

        private IEnumerator SafePunchScore(RectTransform target)
        {
            if (target == null) yield break;

            Vector3 baseScale = scoreBaseScaleCaptured ? scoreBaseScale : target.localScale;
            scoreBaseScale = baseScale;
            scoreBaseScaleCaptured = true;

            float duration = Mathf.Max(0.01f, scorePopDuration);
            float elapsed = 0f;
            float maxMultiplier = Mathf.Max(1f, scorePopScale);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Sin(progress * Mathf.PI);
                float multiplier = Mathf.LerpUnclamped(1f, maxMultiplier, eased);
                target.localScale = baseScale * multiplier;
                yield return null;
            }

            target.localScale = baseScale;
            scorePunchRoutine = null;
        }
        private void PopComboSprite()
        {
            if (!enableComboSprite) return; // safety gate
            if (comboSpritePrefab == null || comboSpriteParent == null) return;
            if (comboSpriteRoutine != null) StopCoroutine(comboSpriteRoutine);
            comboSpriteRoutine = StartCoroutine(PopComboSpriteRoutine());
        }

        private IEnumerator PopComboSpriteRoutine()
        {
            GameObject obj = Instantiate(comboSpritePrefab, comboSpriteParent);
            PositionComboSprite(obj);
            obj.transform.localScale = Vector3.one; // ensure starting scale
            float t = 0f;
            while (t < comboSpritePopDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / comboSpritePopDuration;
                obj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * comboSpritePopScale, Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            obj.transform.localScale = Vector3.one * postPopRestScale;
            if (useAbsoluteFinalScale) obj.transform.localScale = absoluteFinalScale;
            if (!keepComboSpriteForDebug)
            {
                yield return new WaitForSeconds(comboSpriteExtraLifetime);
                if (obj) Destroy(obj);
            }
        }

        // Computes and applies the intended placement for a combo sprite object
        private void PositionComboSprite(GameObject obj)
        {
            if (obj == null) return;
            Vector3 worldPos = ComputeComboWorldPosition();
            // If we adapt to canvas & parent is UI, convert
            if (adaptToParentCanvas)
            {
                Canvas targetCanvas = comboCanvasOverride;
                if (targetCanvas == null && comboSpriteParent != null)
                    targetCanvas = comboSpriteParent.GetComponentInParent<Canvas>();
                if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace)
                {
                    // Convert world to screen, then to canvas local
                    Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
                    if (obj.transform is RectTransform rt)
                    {
                        RectTransform canvasRT = targetCanvas.transform as RectTransform;
                        Vector2 localPoint;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, cam, out localPoint))
                        {
                            // If spawning above score text, offset in UI space
                            if (comboSpawnMode == ComboSpawnMode.AboveScoreText && scoreDisplay && scoreDisplay.scoreText && scoreDisplay.scoreText.transform is RectTransform scoreRT)
                            {
                                Vector3 anchored = scoreRT.anchoredPosition + (Vector2)scoreTextOffset;
                                rt.anchoredPosition = anchored;
                            }
                            else
                            {
                                rt.anchoredPosition = localPoint;
                            }
                        }
                    }
                    else
                    {
                        obj.transform.position = worldPos; // fallback
                    }
                    ApplySortingOverride(obj);
                    return;
                }
            }
            obj.transform.position = worldPos;
            ApplySortingOverride(obj);
        }

        private Vector3 ComputeComboWorldPosition()
        {
            // Default to this manager position
            Vector3 pos = transform.position;
            var gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
            if (comboSpawnMode == ComboSpawnMode.GridCenter)
            {
                if (gridManager != null)
                {
                    int w = gridManager.GridWidth;
                    int h = gridManager.GridHeight;
                    Vector2Int centerCell = new Vector2Int((w - 1) / 2, (h - 1) / 2);
                    pos = gridManager.GridToWorldPosition(centerCell);
                }
                pos += comboWorldOffset;
            }
            else if (comboSpawnMode == ComboSpawnMode.AboveScoreText && scoreDisplay && scoreDisplay.scoreText)
            {
                // Use score text world position plus offset (treat offset as world if not UI object)
                pos = scoreDisplay.scoreText.transform.position + comboWorldOffset + scoreTextOffset;
            }
            return pos;
        }

        private void ApplySortingOverride(GameObject obj)
        {
            if (forceSortingOrder < 0 || obj == null) return;
            var srs = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (sr == null) continue;
                sr.sortingOrder = forceSortingOrder;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showComboSpawnGizmo) return;
            if (comboSpritePrefab == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.65f);
            Vector3 p = ComputeComboWorldPosition();
            Gizmos.DrawSphere(p, 0.25f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.3f);
            Gizmos.DrawWireCube(p, Vector3.one * 0.8f);
        }

        [ContextMenu("Test Combo Popup (Play Mode)")]
        private void ContextTestCombo()
        {
            if (Application.isPlaying)
            {
                PopComboSprite();
            }
        }

        public void ResetScore()
        {
            score = 0;
            comboCount = 0;
            comboActive = false;
            UpdateScoreUI();
            if (comboText) comboText.gameObject.SetActive(false);
            StopRainbowAnimations();
            OnScoreChanged?.Invoke(score);
        }

        public int GetScore() => score;
        public int GetHighScore() => highScore;
        public int GetCombo() => comboCount;

        // TESTING: Call this from the Unity Inspector or via script to reset high score
        [ContextMenu("Reset High Score")]
        public void ResetHighScoreForTesting()
        {
            highScore = 0;
            displayedHighScore = 0;
            PlayerPrefs.SetInt("HighScore", 0);
            PlayerPrefs.Save();
            UpdateScoreUI();
        }
    }
}














