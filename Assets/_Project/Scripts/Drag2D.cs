using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class Drag2D : MonoBehaviour
{
    // Events for line clearing
    public static event Action<List<Vector2Int>> OnLinesCleared;
    public static event Action<int> OnShapesDestroyed;
    
    private Vector3 offset;
    private Camera cam;
    private bool isDragging = false;
    private bool isPlaced = false; // Track if shape has been placed on grid
    
    [Header("Shape Configuration")]
    [SerializeField] private List<Vector2Int> shapeOffsets = new List<Vector2Int>(); // Relative positions of each tile in the shape
    [SerializeField] private float gridSize = 1f; // Size of each grid cell
    
    [Header("Visual Feedback")]
    [SerializeField] private Color placedColor = Color.gray; // Color when shape is placed
    private Color originalColor; // Store original color
    private SpriteRenderer[] tileRenderers; // References to all tile renderers
    private Color[] originalTileColors; // Store original colors of each tile
    
    // Reference to occupied grid positions (shared across all shapes)
    private static HashSet<Vector2Int> occupiedGridPositions = new HashSet<Vector2Int>();
    
    void Start()
    {
        // Cache the camera
        cam = Camera.main;
        
        // If no shape offsets defined, default to single square
        if (shapeOffsets.Count == 0)
        {
            shapeOffsets.Add(Vector2Int.zero);
        }
        
        // Cache all sprite renderers for visual feedback
        tileRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (tileRenderers.Length > 0)
        {
            originalColor = tileRenderers[0].color;
            
            // Store original colors of all tiles
            originalTileColors = new Color[tileRenderers.Length];
            for (int i = 0; i < tileRenderers.Length; i++)
            {
                if (tileRenderers[i] != null)
                {
                    originalTileColors[i] = tileRenderers[i].color;
                }
            }
        }
    }

    void Update()
    {
        // Don't allow dragging if shape is already placed
        if (isPlaced) return;
        
        // Detect mouse click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            if (cam != null)
            {
                Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
                
                // Check if mouse is over this object
                Bounds bounds = GetBounds();
                if (bounds.size != Vector3.zero)
                {
                    if (bounds.Contains(new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z)))
                    {
                        StartDragging();
                    }
                }
            }
        }
        
        // Continue dragging
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
            mouseWorldPos.z = transform.position.z;
            transform.position = mouseWorldPos + offset;
        }
        else if (isDragging && !Mouse.current.leftButton.isPressed)
        {
            // Stop dragging when mouse button is released and snap to grid
            isDragging = false;
            SnapToClosestGrid();
        }
    }
    
    private void StartDragging()
    {
        // Don't allow dragging if shape is already placed
        if (isPlaced) return;
        
        if (cam == null) return;
        
        isDragging = true;
        
        // Calculate offset for smooth dragging
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
        mouseWorldPos.z = transform.position.z;
        
        offset = transform.position - mouseWorldPos;
    }
    
    private Bounds GetBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;
            
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
            return col2D.bounds;
            
        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
            return col3D.bounds;
            
        return new Bounds();
    }

    private void SnapToClosestGrid()
    {
        if (GridGenerator.gridPositions == null || GridGenerator.gridPositions.Count == 0)
            return;

        float closestDistance = Mathf.Infinity;
        Vector3 closestTile = transform.position;
        Vector2Int bestGridPos = Vector2Int.zero;

        foreach (Vector3 tilePos in GridGenerator.gridPositions)
        {
            Vector2Int gridPos = WorldToGridPosition(tilePos);
            
            // Check if this shape can be placed at this position
            if (CanPlaceShapeAt(gridPos))
            {
                float distance = Vector3.Distance(transform.position, tilePos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTile = tilePos;
                    bestGridPos = gridPos;
                }
            }
        }

        // Only snap if we found a valid position
        if (closestDistance < float.MaxValue)
        {
            // Remove old position if this shape was already placed
            RemoveShapeFromGrid();
            
            // Place shape at new position
            transform.position = closestTile;
            PlaceShapeOnGrid(bestGridPos);
            
            // Mark as placed and update visual appearance
            MarkAsPlaced();
            
            // Check for line clearing after placing the shape
            List<Vector2Int> clearedPositions = CheckAndClearLines();
            if (clearedPositions.Count > 0)
            {
                // Handle shape destruction for cleared positions
                int destroyedShapes = DestroyShapesAtPositions(clearedPositions);
                
                // Invoke events
                OnLinesCleared?.Invoke(clearedPositions);
                OnShapesDestroyed?.Invoke(destroyedShapes);
                
                // Optional: Add visual effects or score updates here
                Debug.Log($"Cleared {clearedPositions.Count} tiles from completed lines!");
            }
        }
        else
        {
            // No valid position found, return to original position or handle as needed
            Debug.Log("No valid position found for shape placement!");
        }
    }
    
    private bool CanPlaceShapeAt(Vector2Int gridPosition)
    {
        // Get grid bounds from GridGenerator
        GridGenerator gridGen = FindFirstObjectByType<GridGenerator>();
        if (gridGen == null)
        {
            Debug.LogWarning("GridGenerator not found! Cannot validate shape placement bounds.");
            return false;
        }
        
        foreach (Vector2Int shapeOffset in shapeOffsets)
        {
            Vector2Int checkPos = gridPosition + shapeOffset;
            
            // Check if this position is within valid grid bounds
            if (!IsPositionWithinGridBounds(checkPos, gridGen))
            {
                return false;
            }
            
            // Check if this position is occupied by another shape
            if (occupiedGridPositions.Contains(checkPos))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private bool IsPositionWithinGridBounds(Vector2Int gridPos, GridGenerator gridGen)
    {
        // Convert grid position to expected array indices
        // The grid starts at gridGen.transform.position and extends in positive X, negative Y
        Vector3 gridStartPos = gridGen.transform.position;
        Vector3 worldPos = GridPositionToWorld(gridPos);
        
        // Calculate which array index this world position corresponds to
        float xOffset = worldPos.x - gridStartPos.x;
        float yOffset = gridStartPos.y - worldPos.y; // Y is inverted in grid generation
        
        int arrayX = Mathf.RoundToInt(xOffset / (gridGen.tileSize.x + gridGen.spacing));
        int arrayY = Mathf.RoundToInt(yOffset / (gridGen.tileSize.y + gridGen.spacing));
        
        // Check if the array indices are within valid bounds
        return arrayX >= 0 && arrayX < gridGen.columns && arrayY >= 0 && arrayY < gridGen.rows;
    }
    
    private Vector3 GridPositionToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0);
    }
    
    private void PlaceShapeOnGrid(Vector2Int gridPosition)
    {
        foreach (Vector2Int shapeOffset in shapeOffsets)
        {
            Vector2Int pos = gridPosition + shapeOffset;
            occupiedGridPositions.Add(pos);
        }
    }
    
    private void RemoveShapeFromGrid()
    {
        // Remove this shape's tiles from occupied positions
        Vector2Int currentGridPos = WorldToGridPosition(transform.position);
        foreach (Vector2Int shapeOffset in shapeOffsets)
        {
            Vector2Int pos = currentGridPos + shapeOffset;
            occupiedGridPositions.Remove(pos);
        }
    }
    
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridSize),
            Mathf.RoundToInt(worldPos.y / gridSize)
        );
    }
    
    // Static version for use in static methods
    private static Vector2Int WorldToGridPositionStatic(Vector3 worldPos, float gridSize = 1f)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridSize),
            Mathf.RoundToInt(worldPos.y / gridSize)
        );
    }
    
    private void MarkAsPlaced()
    {
        isPlaced = true;
        
        // Change visual appearance to indicate it's placed
        foreach (SpriteRenderer renderer in tileRenderers)
        {
            if (renderer != null)
            {
                renderer.color = placedColor;
            }
        }
        
        // Optionally disable collider to prevent further interaction
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        Debug.Log($"Shape {gameObject.name} has been placed and is now locked.");
    }
    
    // Public method to reset shape (useful for level restart or undo functionality)
    public void ResetShape()
    {
        if (!isPlaced) return;
        
        // Remove from grid
        RemoveShapeFromGrid();
        
        // Reset placement state
        isPlaced = false;
        
        // Restore original appearance
        foreach (SpriteRenderer renderer in tileRenderers)
        {
            if (renderer != null)
            {
                renderer.color = originalColor;
            }
        }
        
        // Re-enable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        Debug.Log($"Shape {gameObject.name} has been reset and can be moved again.");
    }
    
    // Public method to check if shape is placed
    public bool IsPlaced()
    {
        return isPlaced;
    }
    
    // Public method to set shape offsets from ShapeCreator
    public void SetShapeOffsets(List<Vector2Int> offsets)
    {
        shapeOffsets = new List<Vector2Int>(offsets);
    }
    
    // Method to refresh tile renderers after tiles are destroyed
    public void RefreshTileRenderers()
    {
        tileRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (tileRenderers.Length > 0 && originalColor == Color.clear)
        {
            originalColor = tileRenderers[0].color;
        }
        
        // Also refresh original tile colors if needed
        if (originalTileColors == null || originalTileColors.Length != tileRenderers.Length)
        {
            originalTileColors = new Color[tileRenderers.Length];
            for (int i = 0; i < tileRenderers.Length; i++)
            {
                if (tileRenderers[i] != null)
                {
                    originalTileColors[i] = tileRenderers[i].color;
                }
            }
        }
    }
    
    // Method to clear all occupied positions (useful for restarting level)
    public static void ClearAllOccupiedPositions()
    {
        occupiedGridPositions.Clear();
    }
    
    // Line clearing functionality with cascading support
    public static List<Vector2Int> CheckAndClearLines()
    {
        List<Vector2Int> totalClearedPositions = new List<Vector2Int>();
        GridGenerator gridGen = FindFirstObjectByType<GridGenerator>();
        
        if (gridGen == null)
        {
            Debug.LogWarning("GridGenerator not found! Cannot check for line clearing.");
            return totalClearedPositions;
        }
        
        bool foundCompletedLines;
        int cascadeLevel = 0;
        
        // Keep checking for completed lines until none are found (cascading clears)
        do
        {
            foundCompletedLines = false;
            List<Vector2Int> currentClearedPositions = new List<Vector2Int>();
            
            // Check for horizontal lines
            for (int row = 0; row < gridGen.rows; row++)
            {
                if (IsHorizontalLineComplete(row, gridGen))
                {
                    List<Vector2Int> linePositions = ClearHorizontalLine(row, gridGen);
                    currentClearedPositions.AddRange(linePositions);
                    foundCompletedLines = true;
                    Debug.Log($"Cleared horizontal line at row {row} (cascade level {cascadeLevel})");
                }
            }
            
            // Check for vertical lines
            for (int col = 0; col < gridGen.columns; col++)
            {
                if (IsVerticalLineComplete(col, gridGen))
                {
                    List<Vector2Int> linePositions = ClearVerticalLine(col, gridGen);
                    currentClearedPositions.AddRange(linePositions);
                    foundCompletedLines = true;
                    Debug.Log($"Cleared vertical line at column {col} (cascade level {cascadeLevel})");
                }
            }
            
            totalClearedPositions.AddRange(currentClearedPositions);
            cascadeLevel++;
            
            // Prevent infinite loops
            if (cascadeLevel > 10)
            {
                Debug.LogWarning("Maximum cascade level reached! Breaking to prevent infinite loop.");
                break;
            }
            
        } while (foundCompletedLines);
        
        if (cascadeLevel > 1)
        {
            Debug.Log($"Cascade clear completed! {cascadeLevel - 1} cascade levels processed.");
        }
        
        return totalClearedPositions;
    }
    
    private static bool IsHorizontalLineComplete(int row, GridGenerator gridGen)
    {
        for (int col = 0; col < gridGen.columns; col++)
        {
            Vector2Int gridPos = ArrayIndicesToGridPosition(col, row, gridGen);
            if (!occupiedGridPositions.Contains(gridPos))
            {
                return false;
            }
        }
        return true;
    }
    
    private static bool IsVerticalLineComplete(int col, GridGenerator gridGen)
    {
        for (int row = 0; row < gridGen.rows; row++)
        {
            Vector2Int gridPos = ArrayIndicesToGridPosition(col, row, gridGen);
            if (!occupiedGridPositions.Contains(gridPos))
            {
                return false;
            }
        }
        return true;
    }
    
    private static List<Vector2Int> ClearHorizontalLine(int row, GridGenerator gridGen)
    {
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        
        for (int col = 0; col < gridGen.columns; col++)
        {
            Vector2Int gridPos = ArrayIndicesToGridPosition(col, row, gridGen);
            if (occupiedGridPositions.Contains(gridPos))
            {
                occupiedGridPositions.Remove(gridPos);
                clearedPositions.Add(gridPos);
            }
        }
        
        return clearedPositions;
    }
    
    private static List<Vector2Int> ClearVerticalLine(int col, GridGenerator gridGen)
    {
        List<Vector2Int> clearedPositions = new List<Vector2Int>();
        
        for (int row = 0; row < gridGen.rows; row++)
        {
            Vector2Int gridPos = ArrayIndicesToGridPosition(col, row, gridGen);
            if (occupiedGridPositions.Contains(gridPos))
            {
                occupiedGridPositions.Remove(gridPos);
                clearedPositions.Add(gridPos);
            }
        }
        
        return clearedPositions;
    }
    
    // Helper method to convert array indices to grid position
    private static Vector2Int ArrayIndicesToGridPosition(int col, int row, GridGenerator gridGen)
    {
        Vector3 gridStartPos = gridGen.transform.position;
        Vector3 worldPos = new Vector3(
            gridStartPos.x + col * (gridGen.tileSize.x + gridGen.spacing),
            gridStartPos.y - row * (gridGen.tileSize.y + gridGen.spacing),
            0
        );
        
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / 1f), // Assuming gridSize is 1f
            Mathf.RoundToInt(worldPos.y / 1f)
        );
    }
    
    // Method to handle shape destruction when their tiles are cleared
    public static int DestroyShapesAtPositions(List<Vector2Int> clearedPositions)
    {
        Drag2D[] allShapes = FindObjectsByType<Drag2D>(FindObjectsSortMode.None);
        int affectedShapes = 0;
        
        foreach (Drag2D shape in allShapes)
        {
            if (!shape.IsPlaced()) continue;
            
            Vector2Int shapeGridPos = WorldToGridPositionStatic(shape.transform.position, shape.gridSize);
            
            // Check if any part of this shape was cleared
            bool hasAffectedTiles = false;
            foreach (Vector2Int shapeOffset in shape.shapeOffsets)
            {
                Vector2Int tilePos = shapeGridPos + shapeOffset;
                if (clearedPositions.Contains(tilePos))
                {
                    hasAffectedTiles = true;
                    break;
                }
            }
            
            if (hasAffectedTiles)
            {
                // Remove only the cleared tiles from this shape's offsets
                RemoveClearedTilesFromShape(shape, shapeGridPos, clearedPositions);
                affectedShapes++;
            }
        }
        
        return affectedShapes;
    }
    
    // Helper method to remove only the cleared tiles from a shape
    private static void RemoveClearedTilesFromShape(Drag2D shape, Vector2Int shapeGridPos, List<Vector2Int> clearedPositions)
    {
        List<Vector2Int> remainingOffsets = new List<Vector2Int>();
        
        // Keep only the tiles that weren't cleared
        foreach (Vector2Int shapeOffset in shape.shapeOffsets)
        {
            Vector2Int tilePos = shapeGridPos + shapeOffset;
            if (!clearedPositions.Contains(tilePos))
            {
                remainingOffsets.Add(shapeOffset);
            }
        }
        
        // If no tiles remain, destroy the entire shape
        if (remainingOffsets.Count == 0)
        {
            Debug.Log($"Destroying shape {shape.gameObject.name} - all tiles were cleared");
            Destroy(shape.gameObject);
        }
        else if (remainingOffsets.Count < shape.shapeOffsets.Count)
        {
            // Some tiles were cleared - we need to create a new visual representation
            // For now, let's destroy the whole shape and create a new one with remaining tiles
            Vector3 currentPosition = shape.transform.position;
            
            // Create a new shape with only the remaining tiles
            CreatePartialShape(remainingOffsets, currentPosition, shape.gameObject.name + "_Partial", shape);
            
            Debug.Log($"Shape {shape.gameObject.name} split - {shape.shapeOffsets.Count - remainingOffsets.Count} tiles removed, {remainingOffsets.Count} tiles remain");
            
            // Destroy the original shape
            Destroy(shape.gameObject);
        }
    }
    
    // Helper method to create a new shape with specific offsets
    private static void CreatePartialShape(List<Vector2Int> offsets, Vector3 position, string name, Drag2D originalShape)
    {
        // Create new GameObject
        GameObject newShapeObj = new GameObject(name);
        newShapeObj.transform.position = position;
        
        // Add Drag2D component and configure it
        Drag2D newShape = newShapeObj.AddComponent<Drag2D>();
        newShape.shapeOffsets = new List<Vector2Int>(offsets);
        newShape.gridSize = originalShape.gridSize;
        newShape.placedColor = originalShape.placedColor;
        newShape.originalColor = originalShape.originalColor;
        
        // Copy original tile colors
        if (originalShape.originalTileColors != null)
        {
            newShape.originalTileColors = new Color[originalShape.originalTileColors.Length];
            for (int i = 0; i < originalShape.originalTileColors.Length; i++)
            {
                newShape.originalTileColors[i] = originalShape.originalTileColors[i];
            }
        }
        
        // Mark as placed and add to grid
        newShape.isPlaced = true;
        Vector2Int gridPos = WorldToGridPositionStatic(position, newShape.gridSize);
        
        // Add to occupied positions
        foreach (Vector2Int offset in offsets)
        {
            Vector2Int pos = gridPos + offset;
            occupiedGridPositions.Add(pos);
        }
        
        // For visual representation, copy the original tiles that remain
        CopyOriginalTiles(originalShape, newShape, offsets);
    }
    
    // Copy original tiles that should remain in the partial shape
    private static void CopyOriginalTiles(Drag2D originalShape, Drag2D newShape, List<Vector2Int> remainingOffsets)
    {
        // Get all child objects from the original shape
        Transform[] originalChildren = new Transform[originalShape.transform.childCount];
        for (int i = 0; i < originalShape.transform.childCount; i++)
        {
            originalChildren[i] = originalShape.transform.GetChild(i);
        }
        
        // Copy tiles that correspond to remaining offsets
        for (int i = 0; i < remainingOffsets.Count && i < originalChildren.Length; i++)
        {
            if (originalChildren[i] != null)
            {
                // Create a copy of the original tile
                GameObject newTile = UnityEngine.Object.Instantiate(originalChildren[i].gameObject, newShape.transform);
                newTile.transform.localPosition = new Vector3(
                    remainingOffsets[i].x * newShape.gridSize, 
                    remainingOffsets[i].y * newShape.gridSize, 
                    0
                );
                
                // Restore the original color from before placement
                SpriteRenderer renderer = newTile.GetComponent<SpriteRenderer>();
                if (renderer != null && originalShape.originalTileColors != null && i < originalShape.originalTileColors.Length)
                {
                    renderer.color = originalShape.originalTileColors[i];
                }
            }
        }
        
        // If we couldn't copy enough tiles, create simple ones for the remaining
        if (remainingOffsets.Count > originalChildren.Length)
        {
            for (int i = originalChildren.Length; i < remainingOffsets.Count; i++)
            {
                CreateSimpleTile(newShape, remainingOffsets[i], newShape.placedColor);
            }
        }
        
        // Refresh tile renderers
        newShape.RefreshTileRenderers();
    }
    
    // Create a simple tile for missing tiles
    private static void CreateSimpleTile(Drag2D shape, Vector2Int offset, Color color)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tile.transform.SetParent(shape.transform);
        tile.transform.localPosition = new Vector3(offset.x * shape.gridSize, offset.y * shape.gridSize, 0);
        tile.transform.localScale = Vector3.one * shape.gridSize;
        
        // Remove collider as we don't need it for placed tiles
        Collider col = tile.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set color and material properties
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material instance to avoid affecting other objects
            Material newMaterial = new Material(renderer.material);
            newMaterial.color = color;
            renderer.material = newMaterial;
        }
    }
    
    // Method to reset all shapes in the scene
    public static void ResetAllShapes()
    {
        Drag2D[] allShapes = FindObjectsByType<Drag2D>(FindObjectsSortMode.None);
        foreach (Drag2D shape in allShapes)
        {
            shape.ResetShape();
        }
        ClearAllOccupiedPositions();
    }
    
    // Method to get occupied positions (useful for checking win conditions)
    public static HashSet<Vector2Int> GetOccupiedPositions()
    {
        return new HashSet<Vector2Int>(occupiedGridPositions);
    }
    
    // Method to get count of placed shapes
    public static int GetPlacedShapeCount()
    {
        Drag2D[] allShapes = FindObjectsByType<Drag2D>(FindObjectsSortMode.None);
        int count = 0;
        foreach (Drag2D shape in allShapes)
        {
            if (shape.IsPlaced())
                count++;
        }
        return count;
    }
    
    // Public method to manually trigger line clearing (useful for testing or special abilities)
    public static int TriggerLineClear()
    {
        List<Vector2Int> clearedPositions = CheckAndClearLines();
        if (clearedPositions.Count > 0)
        {
            int destroyedShapes = DestroyShapesAtPositions(clearedPositions);
            
            // Invoke events
            OnLinesCleared?.Invoke(clearedPositions);
            OnShapesDestroyed?.Invoke(destroyedShapes);
            
            Debug.Log($"Manual line clear: Cleared {clearedPositions.Count} tiles and destroyed {destroyedShapes} shapes!");
            return clearedPositions.Count;
        }
        return 0;
    }
    
    // Method to get info about potential line clears without actually clearing them
    public static LineClearInfo GetLineClearInfo()
    {
        GridGenerator gridGen = FindFirstObjectByType<GridGenerator>();
        if (gridGen == null) return new LineClearInfo();
        
        LineClearInfo info = new LineClearInfo();
        
        // Check horizontal lines
        for (int row = 0; row < gridGen.rows; row++)
        {
            if (IsHorizontalLineComplete(row, gridGen))
            {
                info.completedHorizontalLines.Add(row);
            }
        }
        
        // Check vertical lines
        for (int col = 0; col < gridGen.columns; col++)
        {
            if (IsVerticalLineComplete(col, gridGen))
            {
                info.completedVerticalLines.Add(col);
            }
        }
        
        return info;
    }

    [System.Serializable]
    public class LineClearInfo
    {
        public List<int> completedHorizontalLines = new List<int>();
        public List<int> completedVerticalLines = new List<int>();
        
        public int TotalCompletedLines => completedHorizontalLines.Count + completedVerticalLines.Count;
        public bool HasCompletedLines => TotalCompletedLines > 0;
    }

    // Fallback methods for OnMouse events (in case they work with your setup)
    void OnMouseDown()
    {
        // Don't allow dragging if shape is already placed
        if (isPlaced) return;
        
        StartDragging();
    }

    void OnMouseDrag()
    {
        // Don't allow dragging if shape is already placed
        if (isPlaced) return;
        
        if (!isDragging) return;
        
        if (cam == null) return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
        mouseWorldPos.z = transform.position.z;
        
        transform.position = mouseWorldPos + offset;
    }
    
    void OnMouseUp()
    {
        // Don't process if shape is already placed
        if (isPlaced) return;
        
        isDragging = false;
        SnapToClosestGrid();
    }
}
