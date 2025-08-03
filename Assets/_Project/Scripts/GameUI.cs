using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text linesClearedText;
    [SerializeField] private Button resetButton;
    
    private int totalLinesCleared = 0;
    
    void Start()
    {
        // Subscribe to line clearing events
        Drag2D.OnLinesCleared += OnLinesCleared;
        Drag2D.OnScoreChanged += OnScoreChanged;
        
        // Setup reset button
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetGame);
        }
        
        // Initialize UI
        UpdateScoreDisplay(Drag2D.GetCurrentScore());
        UpdateLinesClearedDisplay(0, 0);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        Drag2D.OnLinesCleared -= OnLinesCleared;
        Drag2D.OnScoreChanged -= OnScoreChanged;
        
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetGame);
        }
    }
    
    private void OnLinesCleared(int rowsCleared, int columnsCleared)
    {
        totalLinesCleared += rowsCleared + columnsCleared;
        UpdateLinesClearedDisplay(rowsCleared, columnsCleared);
        
        // You can add visual effects here, like particle systems or screen shake
        Debug.Log($"Lines cleared! Rows: {rowsCleared}, Columns: {columnsCleared}");
    }
    
    private void OnScoreChanged(int newScore)
    {
        UpdateScoreDisplay(newScore);
    }
    
    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    private void UpdateLinesClearedDisplay(int rowsCleared, int columnsCleared)
    {
        if (linesClearedText != null)
        {
            if (rowsCleared > 0 || columnsCleared > 0)
            {
                linesClearedText.text = $"Lines Cleared: {totalLinesCleared}\nLast Clear: {rowsCleared}R + {columnsCleared}C";
            }
            else
            {
                linesClearedText.text = $"Lines Cleared: {totalLinesCleared}";
            }
        }
    }
    
    private void ResetGame()
    {
        Drag2D.ResetAllShapes();
        totalLinesCleared = 0;
        UpdateLinesClearedDisplay(0, 0);
        Debug.Log("Game Reset!");
    }
}
