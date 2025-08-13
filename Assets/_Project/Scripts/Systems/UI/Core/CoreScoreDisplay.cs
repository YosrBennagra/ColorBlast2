using UnityEngine;
using TMPro;

namespace ColorBlast2.UI.Core
{
    /// <summary>
    /// Handles score and high score display and animation.
    /// </summary>
    /// <summary>
    /// Attach to a UI GameObject for score display in CoreGame scene.
    /// Assign scoreText and highScoreText in the Inspector.
    /// </summary>
    public class CoreScoreDisplay : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;

        public void SetScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString("N0");
        }

        public void SetHighScore(int highScore)
        {
            if (highScoreText != null)
                highScoreText.text = highScore.ToString("N0");
        }
    }
}
