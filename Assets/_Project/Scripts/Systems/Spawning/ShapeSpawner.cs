using UnityEngine;
using System.Collections.Generic;
using Core;

public class ShapeSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] shapePrefabs; // Array of shape prefabs to spawn
    [SerializeField] private Transform[] spawnPoints = new Transform[3]; // 3 spawn positions
    [SerializeField] private bool autoSpawnOnStart = true;
    
    [Header("Spawn Effects")]
    [SerializeField] private float spawnEffectDuration = 0.3f;
    [SerializeField] private float minSpawnScale = 0.1f;
    [SerializeField] private float maxSpawnScale = 1.0f;
    
    [Header("Legacy Settings")]
    [SerializeField] private float spawnCheckInterval = 2f; // How often to check if all shapes are placed (reduced frequency)
    
    private GameObject[] currentShapes = new GameObject[3]; // Currently spawned shapes
    private bool allShapesPlaced = false;
    private float lastCheckTime = 0f;
    private bool[] shapeStatusCache = new bool[3]; // Cache shape placement status
    
    void Start()
    {
        // Validate spawn points
        if (spawnPoints.Length != 3)
        {
            Debug.LogError("ShapeSpawner requires exactly 3 spawn points!");
            return;
        }
        
        // Subscribe to line clearing events to check for shape completion
        Gameplay.LineClearSystem.OnLinesCleared += OnLinesCleared;
        
        if (autoSpawnOnStart)
        {
            SpawnNewShapes();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        Gameplay.LineClearSystem.OnLinesCleared -= OnLinesCleared;
    }
    
    void Update()
    {
        // Only check if we haven't already detected all shapes are placed
        if (!allShapesPlaced && Time.time - lastCheckTime >= spawnCheckInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllShapesPlaced();
        }
    }
    
    private void CheckIfAllShapesPlaced()
    {
        if (allShapesPlaced) return;
        
        bool allPlaced = true;
        int placedCount = 0;
        bool statusChanged = false;
        
        for (int i = 0; i < currentShapes.Length; i++)
        {
            bool currentStatus = false;
            
            if (currentShapes[i] != null)
            {
                var shapeComponent = currentShapes[i].GetComponent<Core.Shape>();
                if (shapeComponent != null && shapeComponent.IsPlaced)
                {
                    currentStatus = true;
                    placedCount++;
                }
                else
                {
                    allPlaced = false;
                }
            }
            else
            {
                // Shape was destroyed (e.g., by line clearing), count as placed
                currentStatus = true;
                placedCount++;
            }
            
            // Check if status changed to avoid unnecessary updates
            if (shapeStatusCache[i] != currentStatus)
            {
                shapeStatusCache[i] = currentStatus;
                statusChanged = true;
            }
        }
        
        if (allPlaced && placedCount >= 3)
        {
            allShapesPlaced = true;
            Debug.Log("All shapes placed! Spawning new set...");
            
            // Small delay before spawning new shapes
            Invoke(nameof(SpawnNewShapes), 0.5f);
        }
        else if (statusChanged)
        {
            Debug.Log($"Shapes status update: {placedCount}/3 placed");
        }
    }
    
    private void SpawnNewShapes()
    {
        allShapesPlaced = false;
        
        // Clear references to old shapes and reset status cache
        for (int i = 0; i < currentShapes.Length; i++)
        {
            currentShapes[i] = null;
            shapeStatusCache[i] = false;
        }
        
        // Spawn 3 new shapes
        for (int i = 0; i < 3; i++)
        {
            if (spawnPoints[i] != null)
            {
                GameObject newShape = SpawnRandomShape(i);
                currentShapes[i] = newShape;
            }
        }
        
        Debug.Log("Spawned 3 new shapes!");
    }
    
    private GameObject SpawnRandomShape(int spawnIndex)
    {
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("No shape prefabs assigned to ShapeSpawner!");
            return null;
        }
        
        // Choose a random shape prefab
        int randomIndex = Random.Range(0, shapePrefabs.Length);
        GameObject shapePrefab = shapePrefabs[randomIndex];
        
        if (shapePrefab == null)
        {
            Debug.LogError($"Shape prefab at index {randomIndex} is null!");
            return null;
        }
        
        // Spawn at the designated spawn point
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        GameObject spawnedShape = Instantiate(shapePrefab, spawnPosition, Quaternion.identity);
        
        // Configure the spawned shape BEFORE Start() is called
        var shapeComponent = spawnedShape.GetComponent<Core.Shape>();
        var dragHandler = spawnedShape.GetComponent<Gameplay.DragHandler>();
        
        if (shapeComponent != null && dragHandler != null)
        {
            // Set name for identification
            spawnedShape.name = $"Shape_{spawnIndex}_{Random.Range(1000, 9999)}";
            
            // The spawn position is already set by the Instantiate call above
            // DragHandler will use the transform.position as the spawn position
        }
        
        // Add some visual feedback for spawning
        StartCoroutine(SpawnEffect(spawnedShape));
        
        Debug.Log($"Spawned shape {spawnedShape.name}");
        return spawnedShape;
    }
    
    private System.Collections.IEnumerator SpawnEffect(GameObject shape)
    {
        if (shape == null) yield break;
        
        // Simple spawn animation - scale from minSpawnScale to maxSpawnScale
        Vector3 originalScale = shape.transform.localScale;
        shape.transform.localScale = originalScale * minSpawnScale;
        
        float elapsed = 0f;
        
        while (elapsed < spawnEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnEffectDuration;
            
            // Ease out animation curve
            t = 1f - (1f - t) * (1f - t);
            
            float currentScale = Mathf.Lerp(minSpawnScale, maxSpawnScale, t);
            shape.transform.localScale = originalScale * currentScale;
            yield return null;
        }
        
        shape.transform.localScale = originalScale * maxSpawnScale;
    }
    
    private void OnLinesCleared(List<Vector2Int> clearedPositions)
    {
        // When lines are cleared, some shapes might be destroyed
        // Update our shape references
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] == null)
            {
                // Shape was destroyed, we'll consider this as "placed" for spawning purposes
                continue;
            }
        }
        
        // Check if we need to spawn new shapes after line clearing
        CheckIfAllShapesPlaced();
    }
    
    // Public methods for manual control
    public void ForceSpawnNewShapes()
    {
        SpawnNewShapes();
    }
    
    public void ClearCurrentShapes()
    {
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] != null)
            {
                Destroy(currentShapes[i]);
                currentShapes[i] = null;
            }
        }
        allShapesPlaced = false;
    }
    
    public bool AreAllShapesPlaced()
    {
        return allShapesPlaced;
    }
    
    public int GetPlacedShapeCount()
    {
        int count = 0;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] != null)
            {
                var shapeComponent = currentShapes[i].GetComponent<Core.Shape>();
                if (shapeComponent != null && shapeComponent.IsPlaced)
                {
                    count++;
                }
            }
            else
            {
                count++; // Destroyed shapes count as placed
            }
        }
        return count;
    }
    
    // Helper method to set spawn points programmatically
    public void SetSpawnPoints(Transform[] points)
    {
        if (points.Length == 3)
        {
            spawnPoints = points;
        }
        else
        {
            Debug.LogError("Must provide exactly 3 spawn points!");
        }
    }
    
    // Helper method to add shape prefabs
    public void SetShapePrefabs(GameObject[] prefabs)
    {
        shapePrefabs = prefabs;
    }
}
