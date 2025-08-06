using UnityEngine;
using Systems.UI;
using ColorBlast.Core.Architecture;

namespace Systems.UI
{
    /// <summary>
    /// Integrates the UI system with the game systems
    /// </summary>
    public class UIIntegration : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private GameUIManager uiManager;
        [SerializeField] private ShapeSpawner shapeSpawner;
        
        [Header("Scoring Settings")]
        [SerializeField] private int pointsPerLine = 100;
        [SerializeField] private int bonusPerLevel = 50;
        [SerializeField] private int shapeBonus = 10;
        
        private int totalScore = 0;
        private int totalLines = 0;
        
        private void Start()
        {
            // Find components if not assigned
            if (uiManager == null)
                uiManager = FindFirstObjectByType<GameUIManager>();
                
            if (shapeSpawner == null)
                shapeSpawner = FindFirstObjectByType<ShapeSpawner>();
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            if (uiManager != null)
            {
                // UI event handlers
                uiManager.OnPlayAgain += HandlePlayAgain;
                uiManager.OnRestart += HandleRestart;
                uiManager.OnMainMenu += HandleMainMenu;
                uiManager.OnPause += HandlePause;
                uiManager.OnResume += HandleResume;
            }
            
            // Game system event handlers
            if (Services.IsRegistered<Gameplay.LineClearSystem>())
            {
                var lineClearSystem = Services.Get<Gameplay.LineClearSystem>();
                Gameplay.LineClearSystem.OnLinesCleared += HandleLinesCleared;
            }
            
            // Shape placement scoring
            if (Services.IsRegistered<Gameplay.PlacementSystem>())
            {
                var placementSystem = Services.Get<Gameplay.PlacementSystem>();
                // Subscribe to placement events if available
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event handlers
            if (uiManager != null)
            {
                uiManager.OnPlayAgain -= HandlePlayAgain;
                uiManager.OnRestart -= HandleRestart;
                uiManager.OnMainMenu -= HandleMainMenu;
                uiManager.OnPause -= HandlePause;
                uiManager.OnResume -= HandleResume;
            }
            
            Gameplay.LineClearSystem.OnLinesCleared -= HandleLinesCleared;
        }
        
        private void HandleLinesCleared(System.Collections.Generic.List<Vector2Int> clearedPositions)
        {
            if (clearedPositions == null || clearedPositions.Count == 0) return;
            
            // Calculate lines cleared (assuming grid width of 10)
            int gridWidth = 10;
            if (Services.IsRegistered<Gameplay.GridManager>())
            {
                gridWidth = Services.Get<Gameplay.GridManager>().GridWidth;
            }
            
            int linesCleared = clearedPositions.Count / gridWidth;
            totalLines += linesCleared;
            
            // Calculate score
            int levelMultiplier = uiManager.GetCurrentLevel();
            int lineScore = linesCleared * pointsPerLine * levelMultiplier;
            int bonus = linesCleared * bonusPerLevel;
            
            totalScore += lineScore + bonus;
            
            // Update UI
            uiManager.UpdateScore(totalScore);
            uiManager.UpdateLines(totalLines);
            
            Debug.Log($"Lines cleared: {linesCleared}, Score added: {lineScore + bonus}, Total: {totalScore}");
        }
        
        public void AddShapeBonus()
        {
            int bonus = shapeBonus * uiManager.GetCurrentLevel();
            totalScore += bonus;
            uiManager.UpdateScore(totalScore);
        }
        
        private void HandlePlayAgain()
        {
            Debug.Log("Play Again requested");
            
            // Reset game state
            totalScore = 0;
            totalLines = 0;
            
            // Clear all shapes
            if (shapeSpawner != null)
            {
                shapeSpawner.ClearCurrentShapes();
                shapeSpawner.ForceSpawnNewShapes();
            }
            
            // Clear grid
            if (Services.IsRegistered<Gameplay.GridManager>())
            {
                Services.Get<Gameplay.GridManager>().ClearAllOccupiedCells();
            }
            
            // Update UI
            uiManager.UpdateScore(totalScore);
            uiManager.UpdateLines(totalLines);
        }
        
        private void HandleRestart()
        {
            Debug.Log("Restart requested");
            HandlePlayAgain(); // Same as play again for now
        }
        
        private void HandleMainMenu()
        {
            Debug.Log("Main Menu requested");
            // Implement scene loading or menu display
            // For now, just restart the game
            HandlePlayAgain();
        }
        
        private void HandlePause()
        {
            Debug.Log("Game Paused");
            // Game logic is already paused by UI manager (Time.timeScale = 0)
        }
        
        private void HandleResume()
        {
            Debug.Log("Game Resumed");
            // Game logic is already resumed by UI manager (Time.timeScale = 1)
        }
        
        // Public methods for triggering game over
        public void TriggerGameOver()
        {
            if (uiManager != null)
            {
                uiManager.ShowGameOver();
            }
        }
        
        // Method to check if game should end (no valid moves)
        public bool CheckGameOver()
        {
            // This would need to be implemented based on your game logic
            // For now, return false to keep game running
            return false;
        }
        
        // Public getters
        public int GetTotalScore() => totalScore;
        public int GetTotalLines() => totalLines;
    }
}
