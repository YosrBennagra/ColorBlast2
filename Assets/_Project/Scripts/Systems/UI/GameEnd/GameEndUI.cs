using UnityEngine;
using TMPro;

namespace ColorBlast2.UI.GameEnd
{
    /// <summary>
    /// Handles GameEnd scene UI: shows final score/high score, replay and home buttons.
    /// </summary>
    public class GameEndUI : MonoBehaviour
    {
        [Header("Buttons")] public UnityEngine.UI.Button replayButton;
        public UnityEngine.UI.Button homeButton;
        [Header("Text References")] public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;

        private int finalScore;
        private int highScore;

        private void Awake()
        {
            finalScore = PlayerPrefs.GetInt("LastRunScore", 0);
            highScore = PlayerPrefs.GetInt("HighScore", 0);
            UpdateTexts();
            if (replayButton != null) { replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(Replay); }
            if (homeButton != null) { homeButton.onClick.RemoveAllListeners(); homeButton.onClick.AddListener(Home); }
        }

        private void UpdateTexts()
        {
            if (scoreText != null) scoreText.text = $"Score: {finalScore}";
            if (highScoreText != null) highScoreText.text = $"High Score: {highScore}";
        }

        private void Replay()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CoreGame");
        }

        private void Home()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
