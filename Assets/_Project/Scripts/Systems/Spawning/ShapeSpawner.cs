using UnityEngine;
using System.Collections.Generic;
using Core;

[ExecuteAlways]
public class ShapeSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] shapePrefabs; // Array of shape prefabs to spawn
    [SerializeField] private Transform[] spawnPoints = new Transform[3]; // 3 spawn positions
    [SerializeField] private bool autoSpawnOnStart = true;
    
    [Header("Adaptive Assist")]
    [Range(0f, 1f)]
    [SerializeField] private float assistLevel = 0.6f; // 0=random valid, 1=most helpful

    [Header("Editor Preview & Layout")]
    [SerializeField] private bool alignSpawnPointsVertically = true;
    [Tooltip("Spacing between spawn points in world units (Y).")]
    [SerializeField] private float verticalSpacing = 2f;
    [Tooltip("Use this X value to vertically align all spawn points. If 0, uses this GameObject's X.")]
    [SerializeField] private float alignAtX = 0f;

    [SerializeField] private bool alignSpawnPointsHorizontally = false;
    [Tooltip("Spacing between spawn points in world units (X).")]
    [SerializeField] private float horizontalSpacing = 2f;
    [Tooltip("Use this Y value to horizontally align all spawn points. If 0, uses this GameObject's Y.")]
    [SerializeField] private float alignAtY = 0f;

    [SerializeField] private bool showSpawnGizmos = true;
    [SerializeField] private Color spawnGizmoColor = new Color(0.3f, 0.9f, 1f, 0.6f);
    [SerializeField] private Vector2 previewSize = new Vector2(2f, 2f);
    [Tooltip("When spawning, shift shapes so their bounds center aligns with the spawn gizmo center.")]
    [SerializeField] private bool centerSpawnedShapesInGizmo = true;
    
    [Header("Sprite Theme Settings")]
    [SerializeField] private ShapeSpriteManager spriteManager; // Reference to sprite manager
    [SerializeField] private bool useRandomThemes = true; // Whether to apply random themes to spawned shapes
    
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

    [Tooltip("Use SpriteRenderer bounds to center shapes (more accurate when visuals have pivots/scales).")]
    [SerializeField] private bool centerByRenderers = true;

    private void OnEnable()
    {
        // Keep layout tidy in editor
        AlignSpawnPointsIfNeeded();
    }

    private void OnValidate()
    {
        AlignSpawnPointsIfNeeded();
    }
    
    void Start()
    {
        if (!Application.isPlaying) return;

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
        if (!Application.isPlaying) return;
        // Unsubscribe from events
        Gameplay.LineClearSystem.OnLinesCleared -= OnLinesCleared;
    }

    private void AlignSpawnPointsIfNeeded()
    {
        if (spawnPoints == null || spawnPoints.Length < 3) return;

        // Horizontal alignment takes precedence if enabled
        if (alignSpawnPointsHorizontally)
        {
            float baseY = Mathf.Abs(alignAtY) > Mathf.Epsilon ? alignAtY : transform.position.y;

            int leftIndex = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null) { leftIndex = i; break; }
            }
            if (spawnPoints[leftIndex] == null) return;

            float leftX = spawnPoints[leftIndex].position.x;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;
                Vector3 p = spawnPoints[i].position;
                p.y = baseY;
                p.x = leftX + i * Mathf.Abs(horizontalSpacing);
                spawnPoints[i].position = p;
            }
            return;
        }

        if (!alignSpawnPointsVertically) return;
        // Determine base X
        float baseX = Mathf.Abs(alignAtX) > Mathf.Epsilon ? alignAtX : transform.position.x;

        // Use the first non-null point as the top reference Y
        int topIndex = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) { topIndex = i; break; }
        }
        if (spawnPoints[topIndex] == null) return;

        float topY = spawnPoints[topIndex].position.y;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            Vector3 p = spawnPoints[i].position;
            p.x = baseX;
            p.y = topY - i * Mathf.Abs(verticalSpacing);
            spawnPoints[i].position = p;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos || spawnPoints == null) return;
        Gizmos.color = spawnGizmoColor;
        foreach (var t in spawnPoints)
        {
            if (t == null) continue;
            Gizmos.DrawWireCube(t.position, new Vector3(previewSize.x, previewSize.y, 0.01f));
        }

        // Draw index labels
        #if UNITY_EDITOR
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var t = spawnPoints[i];
            if (t == null) continue;
            UnityEditor.Handles.color = spawnGizmoColor;
            UnityEditor.Handles.Label(t.position + Vector3.up * (previewSize.y * 0.6f), $"Spawn {i+1}");
        }
        #endif
    }
    
    void Update()
    {
        if (!Application.isPlaying) return;
        // Only check if we haven't already detected all shapes are placed
        if (!allShapesPlaced && Time.time - lastCheckTime >= spawnCheckInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllShapesPlaced();
        }
    }
    
    private void CheckIfAllShapesPlaced()
    {
        if (!Application.isPlaying) return;
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
            
            if (Application.isPlaying)
            {
                // Small delay before spawning new shapes
                Invoke(nameof(SpawnNewShapes), 0.5f);
            }
        }
        else if (statusChanged)
        {
            // Optional: debug log could be noisy; keep only in play mode
            // Debug.Log($"Shapes status update: {placedCount}/3 placed");
        }
    }
    
    private void SpawnNewShapes()
    {
        if (!Application.isPlaying) return;
        allShapesPlaced = false;
        
        // Clear references to old shapes and reset status cache
        for (int i = 0; i < currentShapes.Length; i++)
        {
            currentShapes[i] = null;
            shapeStatusCache[i] = false;
        }
        
        // Spawn 3 new shapes
        List<GameObject> newlySpawnedShapes = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            if (spawnPoints[i] != null)
            {
                GameObject newShape = SpawnRandomShape(i);
                currentShapes[i] = newShape;
                if (newShape != null)
                {
                    newlySpawnedShapes.Add(newShape);
                }
            }
        }
        
        // Apply themes to all spawned shapes
        ApplyThemesToShapes(newlySpawnedShapes.ToArray());
    }

    private GameObject SpawnRandomShape(int spawnIndex)
    {
        if (!Application.isPlaying) return null;
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("No shape prefabs assigned to ShapeSpawner!");
            return null;
        }
        
        // Use adaptive selector to pick a helpful shape via reflection (avoids hard dependency during lint)
        GameObject shapePrefab = null;
        var selectorType = System.Type.GetType("AdaptiveShapeSelector");
        if (selectorType != null)
        {
            var method = selectorType.GetMethod("SelectPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                try
                {
                    shapePrefab = (GameObject)method.Invoke(null, new object[] { shapePrefabs, assistLevel });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Adaptive selection failed: {ex.Message}. Falling back to random.");
                }
            }
        }
        if (shapePrefab == null)
        {
            shapePrefab = shapePrefabs[Random.Range(0, shapePrefabs.Length)];
        }
        
        if (shapePrefab == null)
        {
            Debug.LogError("Selector returned null shape prefab!");
            return null;
        }
        
        // Spawn at the designated spawn point
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        GameObject spawnedShape = Instantiate(shapePrefab, spawnPosition, Quaternion.identity);
        
        // Center the shape inside the gizmo if requested
        if (centerSpawnedShapesInGizmo)
        {
            TryCenterShapeToSpawn(spawnedShape, spawnPosition);
        }
        
        // Configure the spawned shape BEFORE Start() is called
        var shapeComponent = spawnedShape.GetComponent<Core.Shape>();
        var dragHandler = spawnedShape.GetComponent<Gameplay.DragHandler>();
        
        if (shapeComponent != null && dragHandler != null)
        {
            spawnedShape.name = $"Shape_{spawnIndex}_{Random.Range(1000, 9999)}";
        }
        
        StartCoroutine(SpawnEffect(spawnedShape));
        return spawnedShape;
    }

    private void TryCenterShapeToSpawn(GameObject shapeGO, Vector3 targetCenter)
    {
        if (shapeGO == null) return;

        if (centerByRenderers)
        {
            if (TryCenterByRenderers(shapeGO, targetCenter)) return;
        }

        var shape = shapeGO.GetComponent<Core.Shape>();
        if (shape == null || shape.ShapeOffsets == null || shape.ShapeOffsets.Count == 0) return;

        float cell = 1f;
        Gameplay.GridManager gm = null;
        if (ColorBlast.Core.Architecture.Services.Has<Gameplay.GridManager>())
            gm = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
        else
            gm = Object.FindFirstObjectByType<Gameplay.GridManager>();
        if (gm != null) cell = gm.CellSize; else if (shape.GridSize > 0f) cell = shape.GridSize;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var o in shape.ShapeOffsets)
        {
            if (o.x < minX) minX = o.x;
            if (o.x > maxX) maxX = o.x;
            if (o.y < minY) minY = o.y;
            if (o.y > maxY) maxY = o.y;
        }
        float centerX = (minX + maxX) * 0.5f * cell;
        float centerY = (minY + maxY) * 0.5f * cell;
        Vector3 worldOffsetFromOrigin = new Vector3(centerX, centerY, 0f);
        Vector3 desired = targetCenter - worldOffsetFromOrigin;
        if (gm != null) desired = gm.SnapToPixel(desired);
        shapeGO.transform.position = desired;
    }

    private bool TryCenterByRenderers(GameObject go, Vector3 targetCenter)
    {
        var renderers = go.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) return false;
        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            b.Encapsulate(renderers[i].bounds);
        }
        Vector3 offset = b.center - go.transform.position;
        Vector3 desired = targetCenter - offset;
        Gameplay.GridManager gm = null;
        if (ColorBlast.Core.Architecture.Services.Has<Gameplay.GridManager>())
            gm = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
        else
            gm = Object.FindFirstObjectByType<Gameplay.GridManager>();
        if (gm != null) desired = gm.SnapToPixel(desired);
        go.transform.position = desired;
        return true;
    }

    private System.Collections.IEnumerator SpawnEffect(GameObject shape)
    {
        if (shape == null) yield break;
        Vector3 originalScale = shape.transform.localScale;
        shape.transform.localScale = originalScale * minSpawnScale;
        float elapsed = 0f;
        while (elapsed < spawnEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnEffectDuration;
            t = 1f - (1f - t) * (1f - t);
            float currentScale = Mathf.Lerp(minSpawnScale, maxSpawnScale, t);
            shape.transform.localScale = originalScale * currentScale;
            yield return null;
        }
        shape.transform.localScale = originalScale * maxSpawnScale;
    }

    private void OnLinesCleared(List<Vector2Int> clearedPositions)
    {
        if (!Application.isPlaying) return;
        CheckIfAllShapesPlaced();
    }

    // Public methods for manual control
    public void ForceSpawnNewShapes()
    {
        if (!Application.isPlaying) return;
        SpawnNewShapes();
    }
    
    public void ClearCurrentShapes()
    {
        if (!Application.isPlaying) return;
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
        if (points != null && points.Length == 3)
        {
            spawnPoints = points;
        }
        else
        {
            Debug.LogError("Must provide exactly 3 spawn points!");
        }
    }
    
    private void ApplyThemesToShapes(GameObject[] shapes)
    {
        if (!Application.isPlaying) return;
        if (!useRandomThemes) return;
        if (shapes == null || shapes.Length == 0) return;
        
        if (spriteManager != null)
        {
            spriteManager.ApplyRandomThemes(shapes);
        }
        else if (ShapeSpriteManager.Instance != null)
        {
            ShapeSpriteManager.Instance.ApplyRandomThemes(shapes);
        }
        else
        {
            Debug.LogWarning("No ShapeSpriteManager available! Shapes will use default sprites.");
        }
    }
    
    public void ApplyThemeToShape(GameObject shape, string themeName)
    {
        if (!Application.isPlaying) return;
        ShapeSpriteManager manager = spriteManager ?? ShapeSpriteManager.Instance;
        if (manager != null)
        {
            var theme = manager.GetThemeByName(themeName);
            if (theme != null)
            {
                manager.ApplyThemeToShape(shape, theme);
            }
        }
    }
    
    // Helper method to add shape prefabs
    public void SetShapePrefabs(GameObject[] prefabs)
    {
        shapePrefabs = prefabs;
    }
}
