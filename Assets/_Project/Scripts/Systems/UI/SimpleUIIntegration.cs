using UnityEngine;
using Systems.UI;
using ColorBlast.Core.Architecture;

namespace Systems.UI
{
    /// <summary>
    /// Simple integration for the main game scene UI
    /// Handles scoring and connects to game systems
    /// </summary>
    public class SimpleUIIntegration : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private SimpleGameUI gameUI;
        
        [Header("Scoring Settings")]
        [SerializeField] private int pointsPerLine = 100;
        [SerializeField] private int bonusPerLevel = 50;
        [SerializeField] private int shapeBonus = 10;
        
        private int totalScore = 0;
        private int totalLines = 0;
        
        private void Start()
        {
            // Find components if not assigned
            if (gameUI == null)
                gameUI = FindFirstObjectByType<SimpleGameUI>();
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            if (gameUI != null)
            {
                // UI event handlers
                gameUI.OnSettings += HandleSettings;
            }
            
            // Game system event handlers - subscribe to line clearing
            if (Services.IsRegistered<Gameplay.LineClearSystem>())
            {
                Gameplay.LineClearSystem.OnLinesCleared += HandleLinesCleared;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event handlers
            if (gameUI != null)
            {
                gameUI.OnSettings -= HandleSettings;
            }
            
            Gameplay.LineClearSystem.OnLinesCleared -= HandleLinesCleared;
        }
        
        private void HandleLinesCleared(System.Collections.Generic.List<Vector2Int> clearedPositions)
        {
            if (clearedPositions == null || clearedPositions.Count == 0) return;
            
            // Calculate lines cleared (assuming grid width)
            int gridWidth = 10;
            if (Services.IsRegistered<Gameplay.GridManager>())
            {
                gridWidth = Services.Get<Gameplay.GridManager>().GridWidth;
            }
            
            int linesCleared = clearedPositions.Count / gridWidth;
            totalLines += linesCleared;
            
            // Calculate score - simple scoring for the main game
            int lineScore = linesCleared * pointsPerLine;
            
            // Add level bonus (every 10 lines = 1 level)
            int currentLevel = (totalLines / 10) + 1;
            int bonus = linesCleared * bonusPerLevel * currentLevel;
            
            totalScore += lineScore + bonus;
            
            // Update UI
            gameUI.UpdateScore(totalScore);
            
            Debug.Log($"Lines cleared: {linesCleared}, Score added: {lineScore + bonus}, Total: {totalScore}");
        }
        
        public void AddShapeBonus()
        {
            // Simple shape placement bonus
            totalScore += shapeBonus;
            gameUI.UpdateScore(totalScore);
        }
        
        private void HandleSettings()
        {
            Debug.Log("Settings requested - implement scene transition here");
            // This is where you would load the settings scene
            // For now, just log
        }
        
        // Public methods for external systems
        public int GetTotalScore() => totalScore;
        public int GetTotalLines() => totalLines;
        
        // Method to reset game state (if needed)
        public void ResetGame()
        {
            totalScore = 0;
            totalLines = 0;
            gameUI.ResetScore();
        }
    }
}
