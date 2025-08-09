using UnityEngine;
using TMPro;

namespace Systems.UI
{
    /// <summary>
    /// Simple Game UI Manager - displays only the current score
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        
        [Header("Camera (for Screen Space - Camera mode)")]
        [SerializeField] private Camera uiCamera;
        
        // Game state
        private int currentScore = 0;
        
        private void Awake()
        {
            SetupCanvas();
        }
        
        private void Start()
        {
            UpdateUI();
        }
        
        private void SetupCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                
                // Auto-assign camera if not set
                if (uiCamera == null)
                    uiCamera = Camera.main;
                    
                canvas.worldCamera = uiCamera;
            }
        }
        
        public void UpdateScore(int score)
        {
            currentScore = score;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore}";
        }
        
        public int GetCurrentScore() => currentScore;
        
        public void ResetScore()
        {
            currentScore = 0;
            UpdateUI();
        }
    }
}
