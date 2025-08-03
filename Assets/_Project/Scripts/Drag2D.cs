using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Drag2D : MonoBehaviour
{
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
    
    // Method to clear all occupied positions (useful for restarting level)
    public static void ClearAllOccupiedPositions()
    {
        occupiedGridPositions.Clear();
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
