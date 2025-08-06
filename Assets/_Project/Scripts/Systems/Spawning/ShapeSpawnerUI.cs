using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShapeSpawnerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI shapesPlacedText;
    [SerializeField] private Button forceSpawnButton;
    [SerializeField] private Button clearShapesButton;
    
    [Header("Progress Bar (Optional)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFill;
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private Color completeColor = Color.gold;
    
    private ShapeSpawner shapeSpawner;
    
    void Start()
    {
        // Find the ShapeSpawner
        shapeSpawner = FindFirstObjectByType<ShapeSpawner>();
        
        if (shapeSpawner == null)
        {
            Debug.LogWarning("ShapeSpawner not found! UI functionality will be limited.");
        }
        
        // Setup button events
        if (forceSpawnButton != null)
        {
            forceSpawnButton.onClick.AddListener(ForceSpawn);
        }
        
        if (clearShapesButton != null)
        {
            clearShapesButton.onClick.AddListener(ClearShapes);
        }
        
        // Setup progress bar
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = 3;
            progressSlider.value = 0;
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (shapeSpawner == null) return;
        
        int placedCount = shapeSpawner.GetPlacedShapeCount();
        bool allPlaced = shapeSpawner.AreAllShapesPlaced();
        
        // Update status text
        if (statusText != null)
        {
            if (allPlaced)
            {
                statusText.text = "All shapes placed! New shapes incoming...";
                statusText.color = completeColor;
            }
            else
            {
                statusText.text = $"Place all shapes to get new ones";
                statusText.color = Color.white;
            }
        }
        
        // Update shapes placed counter
        if (shapesPlacedText != null)
        {
            shapesPlacedText.text = $"Shapes Placed: {placedCount}/3";
        }
        
        // Update progress bar
        if (progressSlider != null)
        {
            progressSlider.value = placedCount;
            
            if (progressFill != null)
            {
                progressFill.color = allPlaced ? completeColor : progressColor;
            }
        }
    }
    
    private void ForceSpawn()
    {
        if (shapeSpawner != null)
        {
            shapeSpawner.ForceSpawnNewShapes();
            Debug.Log("Forced new shape spawn!");
        }
    }
    
    private void ClearShapes()
    {
        if (shapeSpawner != null)
        {
            shapeSpawner.ClearCurrentShapes();
            Debug.Log("Cleared all current shapes!");
        }
    }
    
    // Public method to show spawn notification
    public void ShowSpawnNotification()
    {
        if (statusText != null)
        {
            StartCoroutine(FlashText("New shapes spawned!", 2f));
        }
    }
    
    private System.Collections.IEnumerator FlashText(string message, float duration)
    {
        if (statusText == null) yield break;
        
        string originalText = statusText.text;
        Color originalColor = statusText.color;
        
        statusText.text = message;
        statusText.color = Color.yellow;
        
        yield return new WaitForSeconds(duration);
        
        statusText.text = originalText;
        statusText.color = originalColor;
    }
}
