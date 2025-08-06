using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Systems.UI
{
    /// <summary>
    /// Simplified UI Manager for the main game scene
    /// Shows only Score, High Score, and Settings button
    /// </summary>
    public class SimpleGameUI : MonoBehaviour
    {
        [Header("Main UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;
        
        [Header("Game Area")]
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private float gameAreaPadding = 50f;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button settingsButton;
        
        [Header("Mobile Settings")]
        [SerializeField] private bool autoConfigureMobile = true;
        [SerializeField] private Vector2 mobileReferenceResolution = new Vector2(1080, 1920);
        [SerializeField] private float mobileMatchValue = 0.5f;
        
        // Game state
        private int currentScore = 0;
        private int highScore = 0;
        
        // Events
        public System.Action OnSettings;
        
        private void Awake()
        {
            ConfigureMobileUI();
            LoadHighScore();
        }
        
        private void Start()
        {
            SetupButtons();
            PositionGameArea();
            UpdateUI();
        }
        
        private void ConfigureMobileUI()
        {
            if (!autoConfigureMobile) return;
            
            // Configure Canvas for mobile
            if (mainCanvas == null)
                mainCanvas = GetComponent<Canvas>();
                
            if (mainCanvas != null)
            {
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 100;
            }
            
            // Configure CanvasScaler for mobile
            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();
                
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = mobileReferenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = mobileMatchValue;
                canvasScaler.referencePixelsPerUnit = 100;
            }
            
            // Add GraphicRaycaster if missing
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }
        
        private void SetupButtons()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
        }
        
        private void PositionGameArea()
        {
            if (gameArea == null) return;
            
            // Center the game area on screen
            gameArea.anchorMin = new Vector2(0.5f, 0.5f);
            gameArea.anchorMax = new Vector2(0.5f, 0.5f);
            gameArea.pivot = new Vector2(0.5f, 0.5f);
            gameArea.anchoredPosition = Vector2.zero;
            
            // Calculate size based on screen
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float aspectRatio = screenWidth / screenHeight;
            
            // Mobile portrait optimization
            if (aspectRatio < 1f) // Portrait
            {
                float gameWidth = screenWidth - (gameAreaPadding * 2);
                float gameHeight = screenHeight * 0.7f; // 70% of screen height for game area
                gameArea.sizeDelta = new Vector2(gameWidth, gameHeight);
            }
            else // Landscape
            {
                float gameWidth = screenWidth * 0.8f; // 80% of screen width
                float gameHeight = screenHeight - (gameAreaPadding * 2);
                gameArea.sizeDelta = new Vector2(gameWidth, gameHeight);
            }
        }
        
        public void UpdateScore(int score)
        {
            currentScore = score;
            
            // Update high score if needed
            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
            }
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore:N0}";
                
            if (highScoreText != null)
                highScoreText.text = $"High Score: {highScore:N0}";
        }
        
        private void OpenSettings()
        {
            Debug.Log("Settings button pressed");
            OnSettings?.Invoke();
        }
        
        private void LoadHighScore()
        {
            highScore = PlayerPrefs.GetInt("HighScore", 0);
        }
        
        private void SaveHighScore()
        {
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
        
        // Public methods for external systems
        public int GetCurrentScore() => currentScore;
        public int GetHighScore() => highScore;
        
        // Method to position grid relative to game area
        public Vector3 GetGameAreaCenter()
        {
            if (gameArea == null) return Vector3.zero;
            
            Vector3[] corners = new Vector3[4];
            gameArea.GetWorldCorners(corners);
            
            // Return center of game area in world space
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            return Camera.main.ScreenToWorldPoint(new Vector3(center.x, center.y, Camera.main.nearClipPlane));
        }
        
        // Reset score (for when starting new game)
        public void ResetScore()
        {
            currentScore = 0;
            UpdateUI();
        }
        
        private void OnValidate()
        {
            if (autoConfigureMobile && Application.isPlaying)
            {
                ConfigureMobileUI();
                PositionGameArea();
            }
        }
    }
}
