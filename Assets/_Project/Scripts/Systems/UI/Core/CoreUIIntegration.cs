using UnityEngine;

namespace ShapeBlaster.UI.Core
{
    /// <summary>
    /// Attach to a GameObject in CoreGame scene to connect UI events to game systems.
    /// Integrates UI with core game logic.
    /// </summary>
    public class CoreUIIntegration : MonoBehaviour
    {
        public CoreSettingsPanel settingsPanel;
        public CoreGameUIManager uiManager;

        private void Start()
        {            
            if (settingsPanel != null)
            {
                settingsPanel.OnResume += HandleResume;
                settingsPanel.OnRestart += HandleRestart;
                settingsPanel.OnHome += HandleMainMenu;
            }
        }

        // Called when the player clicks Resume in settings
        private void HandleResume()
        {
            ResumeGame();
        }

        // Called when the player clicks Restart in settings
        private void HandleRestart()
        {
            ResetGame();
            if (settingsPanel != null) settingsPanel.Hide();
        }

        // Called when the player clicks Main Menu in settings
        private void HandleMainMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // Reset the game state (implement your own logic here)
        private void ResetGame()
        {
            // Reset time scale first
            Time.timeScale = 1f;
            
            // Reload the current scene to restart the game
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        }

        // Resume the game (implement your own logic here)
        private void ResumeGame()
        {
            Time.timeScale = 1f;
            // Add additional resume logic as needed
        }
    }
}
