
using UnityEngine;
using UnityEngine.UI;

namespace ColorBlast2.UI.MainMenu
{
    /// <summary>
    /// Main UI controller for the MainMenu scene. Only handles Play button.
    /// Attach this to a root UI GameObject in the MainMenu scene.
    /// </summary>
    public class MainMenuUIManager : MonoBehaviour
    {

    public UnityEngine.UI.Button playButton;
    public TMPro.TMP_Text highScoreText;


        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            UpdateHighScoreDisplay();
        }

        private void UpdateHighScoreDisplay()
        {
            if (highScoreText != null)
            {
                int highScore = PlayerPrefs.GetInt("HighScore", 0);
                highScoreText.text = highScore.ToString("N0");
            }
        }

        private void OnPlayClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CoreGame");
        }
    }
}
