
using UnityEngine;
using TMPro;
using Gameplay;
using System.Collections;





namespace ColorBlast2.Systems.Scoring
{
    public class ScoreManager : MonoBehaviour
    {
        [Header("UI References")]
    public ColorBlast2.UI.Core.CoreScoreDisplay scoreDisplay;
        public TextMeshProUGUI comboText;
        public Transform comboSpriteParent;
        public GameObject comboSpritePrefab;

        [Header("Scoring Settings")]
        public int pointsPerBlock = 10;
        public int lineClearBonus = 100;
        public int sameSpriteLineBonus = 200;
        public float comboTimeWindow = 2f;
        public float comboSpritePopScale = 1.5f;
        public float comboSpritePopDuration = 0.4f;

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
            PopComboSprite();
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

        private void AnimateScore()
        {
            if (scoreDisplay && scoreDisplay.scoreText)
            {
                if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
                scorePunchRoutine = StartCoroutine(PunchScale(scoreDisplay.scoreText.transform));
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
                if (highScoreAnimRoutine != null) StopCoroutine(highScoreAnimRoutine);
                highScoreAnimRoutine = StartCoroutine(PunchScale(scoreDisplay.highScoreText.transform, Color.yellow));
            }
        }

        private IEnumerator PunchScale(Transform target, Color? flashColor = null)
        {
            Vector3 original = target.localScale;
            Vector3 punch = original * 1.2f;
            float t = 0f;
            float duration = 0.25f;
            var text = target.GetComponent<TMP_Text>();
            Color origColor = text ? text.color : Color.white;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                target.localScale = Vector3.LerpUnclamped(original, punch, Mathf.Sin(p * Mathf.PI));
                if (flashColor.HasValue && text)
                    text.color = Color.Lerp(origColor, flashColor.Value, Mathf.PingPong(p * 2f, 1f));
                yield return null;
            }
            target.localScale = original;
            if (text) text.color = origColor;
        }

        private void PopComboSprite()
        {
            if (comboSpritePrefab == null || comboSpriteParent == null) return;
            if (comboSpriteRoutine != null) StopCoroutine(comboSpriteRoutine);
            comboSpriteRoutine = StartCoroutine(PopComboSpriteRoutine());
        }

        private IEnumerator PopComboSpriteRoutine()
        {
            GameObject obj = Instantiate(comboSpritePrefab, comboSpriteParent);
            // Center in grid
            var gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
            if (gridManager != null) {
                int w = gridManager.GridWidth;
                int h = gridManager.GridHeight;
                // Center of grid in world space
                Vector2Int centerCell = new Vector2Int((w-1)/2, (h-1)/2);
                obj.transform.position = gridManager.GridToWorldPosition(centerCell);
            }
            obj.transform.localScale = Vector3.one; // native size
            float t = 0f;
            while (t < comboSpritePopDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / comboSpritePopDuration;
                obj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * comboSpritePopScale, Mathf.Sin(p * Mathf.PI));
                yield return null;
            }
            obj.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.5f);
            Destroy(obj);
        }

        public void ResetScore()
        {
            score = 0;
            comboCount = 0;
            comboActive = false;
            UpdateScoreUI();
            if (comboText) comboText.gameObject.SetActive(false);
            StopRainbowAnimations();
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
