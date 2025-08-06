using UnityEngine;
using System.Collections.Generic;
using ColorBlast.Core.Architecture;
using ColorBlast.Core.Interfaces;

namespace Gameplay
{
    /// <summary>
    /// Simple, reliable grid manager for ColorBlast2
    /// Handles grid positioning, visualization, and state management
    /// </summary>
    public class GridManager : MonoBehaviour, IGridManager
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 0.8f;
        
        [Header("Advanced Cell Sizing")]
        [SerializeField] private bool useUniformCellSize = true;
        [SerializeField] private float cellWidth = 0.8f;
        [SerializeField] private float cellHeight = 0.8f;
        
        [Header("Cell Spacing")]
        [SerializeField] private float cellSpacing = 0.0f;
        [SerializeField] private bool useUniformSpacing = true;
        [SerializeField] private float cellSpacingX = 0.0f;
        [SerializeField] private float cellSpacingY = 0.0f;
        
        [Header("Visual Settings")]
        [SerializeField] private bool showVisualGrid = false; // Changed default to false
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Material gridMaterial;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.05f);
        
        // Grid state
        private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        private Transform gridContainer;
        private Vector3 gridStartPosition;
        
        // Properties for external access
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        public float CellWidth => useUniformCellSize ? cellSize : cellWidth;
        public float CellHeight => useUniformCellSize ? cellSize : cellHeight;
        public bool UseUniformCellSize => useUniformCellSize;
        public float CellSpacing => cellSpacing;
        public float CellSpacingX => useUniformSpacing ? cellSpacing : cellSpacingX;
        public float CellSpacingY => useUniformSpacing ? cellSpacing : cellSpacingY;
        public bool UseUniformSpacing => useUniformSpacing;
        
        /// <summary>
        /// Set cell size at runtime and refresh grid calculations
        /// </summary>
        public void SetCellSize(float newCellSize)
        {
            if (newCellSize <= 0f)
            {
                Debug.LogWarning("Cell size must be greater than 0");
                return;
            }
            
            cellSize = newCellSize;
            if (useUniformCellSize)
            {
                cellWidth = newCellSize;
                cellHeight = newCellSize;
            }
            CalculateGridStartPosition();
            
            if (showVisualGrid && Application.isPlaying)
            {
                RefreshVisualGrid();
            }
        }
        
        /// <summary>
        /// Set cell spacing at runtime
        /// </summary>
        public void SetCellSpacing(float newSpacing)
        {
            if (newSpacing < 0f)
            {
                Debug.LogWarning("Cell spacing cannot be negative");
                return;
            }
            
            cellSpacing = newSpacing;
            if (useUniformSpacing)
            {
                cellSpacingX = newSpacing;
                cellSpacingY = newSpacing;
            }
            CalculateGridStartPosition();
            
            if (showVisualGrid && Application.isPlaying)
            {
                RefreshVisualGrid();
            }
        }
        
        private void Awake()
        {
            // Ensure fields are properly initialized
            InitializeFields();
            CalculateGridStartPosition();
            Services.Register<GridManager>(this);
        }
        
        /// <summary>
        /// Initialize fields to ensure proper defaults
        /// </summary>
        private void InitializeFields()
        {
            // Ensure cell sizing fields are initialized
            if (cellWidth == 0f) cellWidth = cellSize;
            if (cellHeight == 0f) cellHeight = cellSize;
            
            // Ensure spacing fields are initialized
            if (cellSpacingX == 0f && !useUniformSpacing) cellSpacingX = cellSpacing;
            if (cellSpacingY == 0f && !useUniformSpacing) cellSpacingY = cellSpacing;
        }
        
        private void Start()
        {
            // Clean up any existing visual grid GameObjects
            CleanupVisualGrid();
        }
        
        /// <summary>
        /// Clean up any existing visual grid GameObjects
        /// </summary>
        private void CleanupVisualGrid()
        {
            if (gridContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(gridContainer.gameObject);
                }
                else
                {
                    DestroyImmediate(gridContainer.gameObject);
                }
                gridContainer = null;
            }
            
            // Also clean up any "GridVisual" objects that might exist
            GameObject existingGrid = GameObject.Find("GridVisual");
            if (existingGrid != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingGrid);
                }
                else
                {
                    DestroyImmediate(existingGrid);
                }
            }
        }
        
        private void CalculateGridStartPosition()
        {
            // Grid starts at bottom-left, centered horizontally
            // Total size includes cells plus spacing between them
            float totalWidth = gridWidth * CellWidth + (gridWidth - 1) * CellSpacingX;
            float totalHeight = gridHeight * CellHeight + (gridHeight - 1) * CellSpacingY;
            
            gridStartPosition = transform.position;
            gridStartPosition.x -= totalWidth * 0.5f;
            gridStartPosition.y -= totalHeight * 0.5f;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            // Calculate position from grid start, accounting for cell size and spacing
            float worldX = gridPos.x * (CellWidth + CellSpacingX) + CellWidth * 0.5f;
            float worldY = gridPos.y * (CellHeight + CellSpacingY) + CellHeight * 0.5f;
            
            return gridStartPosition + new Vector3(worldX, worldY, 0f);
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridStartPosition;
            
            // Account for spacing when converting to grid coordinates
            int gridX = Mathf.FloorToInt(localPos.x / (CellWidth + CellSpacingX));
            int gridY = Mathf.FloorToInt(localPos.y / (CellHeight + CellSpacingY));
            
            return new Vector2Int(gridX, gridY);
        }
        
        /// <summary>
        /// Check if grid position is within bounds
        /// </summary>
        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridWidth &&
                   gridPos.y >= 0 && gridPos.y < gridHeight;
        }
        
        /// <summary>
        /// Check if grid position is occupied
        /// </summary>
        public bool IsCellOccupied(Vector2Int gridPos)
        {
            return occupiedCells.Contains(gridPos);
        }
        
        /// <summary>
        /// Occupy a grid cell
        /// </summary>
        public void OccupyCell(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
            {
                occupiedCells.Add(gridPos);
            }
        }
        
        /// <summary>
        /// Free a grid cell
        /// </summary>
        public void FreeCell(Vector2Int gridPos)
        {
            occupiedCells.Remove(gridPos);
        }
        
        /// <summary>
        /// Check if multiple cells are available for placement
        /// </summary>
        public bool CanPlaceShape(Vector2Int startPos, List<Vector2Int> shapeOffsets)
        {
            foreach (var offset in shapeOffsets)
            {
                Vector2Int checkPos = startPos + offset;
                if (!IsValidGridPosition(checkPos) || IsCellOccupied(checkPos))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Place a shape on the grid
        /// </summary>
        public bool PlaceShape(Vector2Int startPos, List<Vector2Int> shapeOffsets)
        {
            if (!CanPlaceShape(startPos, shapeOffsets))
            {
                return false;
            }
            
            foreach (var offset in shapeOffsets)
            {
                Vector2Int pos = startPos + offset;
                OccupyCell(pos);
            }
            
            return true;
        }
        
        /// <summary>
        /// Get all occupied positions
        /// </summary>
        public HashSet<Vector2Int> GetOccupiedPositions()
        {
            return new HashSet<Vector2Int>(occupiedCells);
        }
        
        /// <summary>
        /// Clear all occupied cells
        /// </summary>
        public void ClearAllOccupiedCells()
        {
            occupiedCells.Clear();
            Debug.Log("All grid cells cleared");
        }
        
        /// <summary>
        /// Clear all occupied cells (interface implementation)
        /// </summary>
        public void ClearGrid()
        {
            ClearAllOccupiedCells();
        }
        
        /// <summary>
        /// Get grid center position in world space
        /// </summary>
        public Vector3 GetGridCenter()
        {
            Vector2Int centerGridPos = new Vector2Int(gridWidth / 2, gridHeight / 2);
            return GridToWorldPosition(centerGridPos);
        }
        
        /// <summary>
        /// Test grid positioning conversions for debugging
        /// </summary>
        public bool ValidateGridPositioning()
        {
            bool allValid = true;
            Vector2Int testGridPos = new Vector2Int(0, 0);
            
            Vector3 worldPos = GridToWorldPosition(testGridPos);
            Vector2Int convertedBack = WorldToGridPosition(worldPos);
            
            if (convertedBack != testGridPos)
            {
                Debug.LogWarning($"Position conversion failed: {testGridPos} → {worldPos} → {convertedBack}");
                allValid = false;
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Get cell center position in world space
        /// </summary>
        public Vector3 GetCellCenter(Vector2Int gridPos)
        {
            return GridToWorldPosition(gridPos);
        }
        
        /// <summary>
        /// Create visual grid tiles for runtime display
        /// </summary>
        public void CreateVisualGrid()
        {
            // Clear existing visual grid
            if (gridContainer != null)
            {
                DestroyImmediate(gridContainer.gameObject);
            }
            
            // Create container
            GameObject containerObj = new GameObject("GridVisual");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            gridContainer = containerObj.transform;
            
            // Create grid tiles
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(gridPos);
                    
                    GameObject tile = CreateGridTile(worldPos, gridPos);
                    tile.transform.SetParent(gridContainer);
                }
            }
        }
        
        /// <summary>
        /// Create a single grid tile
        /// </summary>
        private GameObject CreateGridTile(Vector3 position, Vector2Int gridPosition)
        {
            GameObject tile;
            
            if (tilePrefab != null)
            {
                tile = Instantiate(tilePrefab);
            }
            else
            {
                tile = new GameObject($"GridTile_{gridPosition.x}_{gridPosition.y}");
                tile.AddComponent<SpriteRenderer>();
            }
            
            tile.transform.position = position;
            tile.transform.localScale = new Vector3(CellWidth * 0.9f, CellHeight * 0.9f, 1f);
            
            var renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (renderer.sprite == null)
                {
                    renderer.sprite = CreateSimpleSprite();
                }
                // Use transparent color so grid tiles are barely visible or invisible
                renderer.color = new Color(1f, 1f, 1f, 0f); // Completely transparent
                renderer.material = gridMaterial;
                renderer.sortingOrder = -1; // Behind gameplay elements
            }
            
            return tile;
        }
        
        /// <summary>
        /// Create a simple square sprite for grid tiles
        /// </summary>
        private Sprite CreateSimpleSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }
        
        /// <summary>
        /// Refresh the visual grid
        /// </summary>
        public void RefreshVisualGrid()
        {
            // Disabled - no runtime GameObjects created
            // Only gizmos are used for editor preview
        }
        
        private void OnDrawGizmos()
        {
            if (!showVisualGrid) return;
            
            CalculateGridStartPosition();
            
            // Draw grid cells
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.6f);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 cellCenter = GridToWorldPosition(new Vector2Int(x, y));
                    Vector3 cellSize = new Vector3(CellWidth * 0.9f, CellHeight * 0.9f, 0.1f);
                    Gizmos.DrawWireCube(cellCenter, cellSize);
                }
            }
            
            // Draw grid boundary
            Gizmos.color = Color.yellow;
            Vector3 gridCenter = transform.position;
            float totalWidth = gridWidth * CellWidth + (gridWidth - 1) * CellSpacingX;
            float totalHeight = gridHeight * CellHeight + (gridHeight - 1) * CellSpacingY;
            Vector3 gridSize = new Vector3(totalWidth, totalHeight, 0.1f);
            Gizmos.DrawWireCube(gridCenter, gridSize);
            
            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
        
        private void OnValidate()
        {
            // Ensure valid values
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            cellSize = Mathf.Max(0.1f, cellSize);
            cellWidth = Mathf.Max(0.1f, cellWidth);
            cellHeight = Mathf.Max(0.1f, cellHeight);
            cellSpacing = Mathf.Max(0f, cellSpacing);
            cellSpacingX = Mathf.Max(0f, cellSpacingX);
            cellSpacingY = Mathf.Max(0f, cellSpacingY);
            
            // Update calculations when values change in inspector
            InitializeFields();
            CalculateGridStartPosition();
            
            #if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
            #endif
        }
    }
}
