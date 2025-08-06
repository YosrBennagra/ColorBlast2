using UnityEngine;

/// <summary>
/// Performance monitoring tool for ColorBlast2
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool displayStats = true;
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showMemory = true;
    [SerializeField] private bool showDetailedInfo = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    
    [Header("Update Settings")]
    public float updateInterval = 0.5f; // Make public so OptimizationSettings can access it
    
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float memoryUsage = 0.0f;
    private int frameCount = 0;
    private float lastUpdateTime = 0.0f;
    
    private void Start()
    {
        lastUpdateTime = Time.realtimeSinceStartup;
    }
    
    private void Update()
    {
        // Toggle display
        if (Input.GetKeyDown(toggleKey))
        {
            displayStats = !displayStats;
        }
        
        // Calculate delta time
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        frameCount++;
        
        // Update stats at intervals
        if (Time.realtimeSinceStartup - lastUpdateTime >= updateInterval)
        {
            fps = frameCount / (Time.realtimeSinceStartup - lastUpdateTime);
            memoryUsage = System.GC.GetTotalMemory(false) / 1024.0f / 1024.0f; // MB
            frameCount = 0;
            lastUpdateTime = Time.realtimeSinceStartup;
        }
    }
    
    void OnGUI()
    {
        if (!displayStats) return;
        
        string text = "";
        
        if (showFPS)
        {
            float msec = deltaTime * 1000.0f;
            text += $"FPS: {fps:F1} ({msec:F1}ms)\n";
        }
        
        if (showMemory)
        {
            text += $"Memory: {memoryUsage:F1} MB\n";
        }
        
        if (showDetailedInfo)
        {
            text += $"Objects: {FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length}\n";
            
            // Get grid manager for shape/tile info
            if (ColorBlast.Core.Architecture.Services.IsRegistered<Gameplay.GridManager>())
            {
                var gridManager = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
                var occupiedPositions = gridManager.GetOccupiedPositions();
                text += $"Active Shapes: {FindObjectsByType<Core.Shape>(FindObjectsSortMode.None).Length}\n";
                text += $"Occupied Tiles: {occupiedPositions.Count}\n";
            }
            else
            {
                text += $"Active Shapes: N/A (GridManager not ready)\n";
                text += $"Occupied Tiles: N/A (GridManager not ready)\n";
            }
        }
        
        text += $"\nPress {toggleKey} to toggle";
        
        // Background box
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 250, 100), "");
        
        // Text
        GUI.color = Color.white;
        GUI.Label(new Rect(15, 15, 240, 90), text);
    }
}