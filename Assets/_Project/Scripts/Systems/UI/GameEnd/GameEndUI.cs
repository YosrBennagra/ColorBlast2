using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace ColorBlast2.UI.GameEnd
{
    /// <summary>
    /// Handles GameEnd scene UI: shows final score/high score, replay and home buttons.
    /// </summary>
    public class GameEndUI : MonoBehaviour
    {
        [Header("Text References")] public TMP_Text scoreText;
        public TMP_Text highScoreText;
        [Tooltip("Optional label prefix for score.")] public string scorePrefix = "Score: ";
        [Tooltip("Optional label prefix for high score.")] public string highScorePrefix = "High Score: ";

        [Header("Buttons")] public Button playAgainButton; // to CoreGame
        public Button mainMenuButton; // to MainMenu

        [Header("Animation")] public float countDuration = 1.2f;
        public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Punch scale effect on finish")] public bool punchOnComplete = true;
        public float punchScale = 1.15f;
        public float punchDuration = 0.25f;

        [Header("Scenes")] public string coreGameSceneName = "CoreGame";
        public string mainMenuSceneName = "MainMenu";

        private int finalScore;
        private int highScore;
        private Coroutine animRoutine;

        private void Awake()
        {
            // Unified key name with GameOver saving (expecting "LastScore").
            finalScore = PlayerPrefs.GetInt("LastScore", PlayerPrefs.GetInt("LastRunScore", 0));
            highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (scoreText) scoreText.text = scorePrefix + "0";
            if (highScoreText) highScoreText.text = highScorePrefix + "0";
            WireButtons();
        }

        private void Start()
        {
            animRoutine = StartCoroutine(AnimateCounts());
        }

        private void WireButtons()
        {
            if (playAgainButton)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(() => LoadScene(coreGameSceneName));
            }
            if (mainMenuButton)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() => LoadScene(mainMenuSceneName));
            }
        }

        private IEnumerator AnimateCounts()
        {
            float t = 0f;
            int fromScore = 0;
            int fromHigh = 0;
            while (t < countDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / countDuration);
                float e = easeCurve != null ? easeCurve.Evaluate(p) : p;
                int curScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, finalScore, e));
                int curHigh = Mathf.RoundToInt(Mathf.Lerp(fromHigh, highScore, e));
                if (scoreText) scoreText.text = scorePrefix + curScore;
                if (highScoreText) highScoreText.text = highScorePrefix + curHigh;
                yield return null;
            }
            if (scoreText) scoreText.text = scorePrefix + finalScore;
            if (highScoreText) highScoreText.text = highScorePrefix + highScore;
            if (punchOnComplete && scoreText) StartCoroutine(Punch(scoreText.transform));
            if (punchOnComplete && highScoreText) StartCoroutine(Punch(highScoreText.transform));
        }

        private IEnumerator Punch(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = target.localScale;
            Vector3 end = orig * punchScale;
            float t = 0f;
            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / punchDuration;
                float s = Mathf.Sin(p * Mathf.PI); // up and back
                target.localScale = Vector3.LerpUnclamped(orig, end, s);
                yield return null;
            }
            target.localScale = orig;
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
