using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Systems.UI
{
    /// <summary>
    /// Mobile-optimized UI Manager for ColorBlast2
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("Main UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        
        [Header("Game Area")]
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private float gameAreaPadding = 50f;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject pausePanel;
        
        [Header("Game UI Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI linesText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        
        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Pause UI")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseMenuButton;
        
        [Header("Mobile Settings")]
        [SerializeField] private bool autoConfigureMobile = true;
        [SerializeField] private Vector2 mobileReferenceResolution = new Vector2(1080, 1920);
        [SerializeField] private float mobileMatchValue = 0.5f;
        
        // Game state
        private int currentScore = 0;
        private int currentLines = 0;
        private int currentLevel = 1;
        private int highScore = 0;
        private bool isPaused = false;
        private bool isGameOver = false;
        
        // Events
        public System.Action OnPlayAgain;
        public System.Action OnMainMenu;
        public System.Action OnPause;
        public System.Action OnResume;
        public System.Action OnRestart;
        
        private void Awake()
        {
            ConfigureMobileUI();
            LoadHighScore();
        }
        
        private void Start()
        {
            SetupButtons();
            ShowGamePanel();
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
            if (graphicRaycaster == null)
            {
                graphicRaycaster = GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                    graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        private void SetupButtons()
        {
            // Game UI buttons
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);
            
            // Game Over buttons
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(PlayAgain);
                
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            
            // Pause buttons
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
                
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
                
            if (pauseMenuButton != null)
                pauseMenuButton.onClick.AddListener(GoToMainMenu);
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
                float gameHeight = screenHeight * 0.6f; // 60% of screen height for game area
                gameArea.sizeDelta = new Vector2(gameWidth, gameHeight);
            }
            else // Landscape
            {
                float gameWidth = screenWidth * 0.7f; // 70% of screen width
                float gameHeight = screenHeight - (gameAreaPadding * 2);
                gameArea.sizeDelta = new Vector2(gameWidth, gameHeight);
            }
        }
        
        public void UpdateScore(int score)
        {
            currentScore = score;
            UpdateUI();
        }
        
        public void UpdateLines(int lines)
        {
            currentLines = lines;
            currentLevel = (lines / 10) + 1; // Level up every 10 lines
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore:N0}";
                
            if (linesText != null)
                linesText.text = $"Lines: {currentLines}";
                
            if (levelText != null)
                levelText.text = $"Level: {currentLevel}";
        }
        
        public void ShowGameOver()
        {
            isGameOver = true;
            
            // Update high score
            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
            }
            
            // Update game over UI
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {currentScore:N0}";
                
            if (highScoreText != null)
                highScoreText.text = $"High Score: {highScore:N0}";
            
            // Show game over panel
            ShowGameOverPanel();
        }
        
        public void PauseGame()
        {
            if (isGameOver) return;
            
            isPaused = true;
            Time.timeScale = 0f;
            ShowPausePanel();
            OnPause?.Invoke();
        }
        
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            ShowGamePanel();
            OnResume?.Invoke();
        }
        
        public void PlayAgain()
        {
            isGameOver = false;
            isPaused = false;
            Time.timeScale = 1f;
            
            // Reset game stats
            currentScore = 0;
            currentLines = 0;
            currentLevel = 1;
            
            UpdateUI();
            ShowGamePanel();
            OnPlayAgain?.Invoke();
        }
        
        public void RestartGame()
        {
            PlayAgain();
            OnRestart?.Invoke();
        }
        
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            OnMainMenu?.Invoke();
        }
        
        private void ShowGamePanel()
        {
            if (gamePanel != null) gamePanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }
        
        private void ShowGameOverPanel()
        {
            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
        }
        
        private void ShowPausePanel()
        {
            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
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
        public int GetCurrentLines() => currentLines;
        public int GetCurrentLevel() => currentLevel;
        public int GetHighScore() => highScore;
        public bool IsGameOver() => isGameOver;
        public bool IsPaused() => isPaused;
        
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
