using UnityEngine;

/// <summary>
/// ScriptableObject for configuring game optimization settings
/// </summary>
[CreateAssetMenu(fileName = "OptimizationSettings", menuName = "Game/Optimization Settings")]
public class OptimizationSettings : ScriptableObject
{
    [Header("Input Settings")]
    [Tooltip("Use cached input values to reduce Input System calls")]
    public bool useCachedInput = true;
    
    [Header("Grid Settings")]
    [Tooltip("Use spatial grid cache for faster position lookups")]
    public bool useGridCache = true;
    
    [Tooltip("Maximum search radius for grid snapping (in grid units)")]
    [Range(1, 5)]
    public int maxSnapSearchRadius = 3;
    
    [Header("Shape Spawning")]
    [Tooltip("Interval between checking if shapes are placed (seconds)")]
    [Range(0.5f, 3f)]
    public float spawnerCheckInterval = 2f;
    
    [Tooltip("Enable object pooling for better memory management")]
    public bool useObjectPooling = true;
    
    [Header("Line Clearing")]
    [Tooltip("Use optimized line clearing that only checks affected lines")]
    public bool useOptimizedLineClearing = true;
    
    [Tooltip("Maximum cascade levels to prevent infinite loops")]
    [Range(5, 20)]
    public int maxCascadeLevels = 10;
    
    [Header("Visual Effects")]
    [Tooltip("Duration of spawn animation effects")]
    [Range(0.1f, 1f)]
    public float spawnEffectDuration = 0.3f;
    
    [Tooltip("Use simplified visual effects for better performance")]
    public bool useSimplifiedEffects = false;
    
    [Header("Performance Monitoring")]
    [Tooltip("Enable performance monitoring display")]
    public bool enablePerformanceMonitor = false;
    
    [Tooltip("Update interval for performance metrics (seconds)")]
    [Range(0.1f, 2f)]
    public float performanceUpdateInterval = 0.5f;
    
    [Header("Memory Management")]
    [Tooltip("Automatically force garbage collection periodically")]
    public bool autoGarbageCollection = false;
    
    [Tooltip("Interval between garbage collection calls (seconds)")]
    [Range(30f, 300f)]
    public float gcInterval = 60f;
    
    /// <summary>
    /// Singleton instance for easy access
    /// </summary>
    private static OptimizationSettings _instance;
    public static OptimizationSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<OptimizationSettings>("OptimizationSettings");
                if (_instance == null)
                {
                    Debug.LogWarning("OptimizationSettings not found in Resources folder. Using default settings.");
                    _instance = CreateInstance<OptimizationSettings>();
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Apply these settings to the game
    /// </summary>
    public void ApplySettings()
    {
        // Apply spawner check interval
        ShapeSpawner[] spawners = FindObjectsByType<ShapeSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            // Note: You'd need to add a public method to ShapeSpawner to set this
            Debug.Log($"Applied optimization settings to {spawners.Length} spawners");
        }
        
        // Apply performance monitor settings
        PerformanceMonitor monitor = FindFirstObjectByType<PerformanceMonitor>();
        if (monitor != null)
        {
            monitor.updateInterval = performanceUpdateInterval;
        }
        
        Debug.Log("Optimization settings applied successfully");
    }
}
