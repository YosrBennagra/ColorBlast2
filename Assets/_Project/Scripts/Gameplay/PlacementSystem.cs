using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Handles shape placement validation and logic
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField] private bool strictBoundsChecking = true;
        [SerializeField] private bool strictOccupancyChecking = true;
        [SerializeField] private bool allowPartialOverlap = false;
        
        private GridManager gridManager;
        private LineClearSystem lineClearSystem;
        
        private void Start()
        {
            // Registration is now handled by GameManager
            // Get services when they're available
            StartCoroutine(InitializeServices());
        }
        
        private System.Collections.IEnumerator InitializeServices()
        {
            // Wait for services to be registered
            while (!Services.Has<GridManager>() || !Services.Has<LineClearSystem>())
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            gridManager = Services.Get<GridManager>();
            lineClearSystem = Services.Get<LineClearSystem>();
        }
        
        public bool CanPlaceShape(Core.Shape shape, Vector2Int gridPosition)
        {
            if (gridManager == null || shape == null) return false;
            
            int validTiles = 0;
            int totalTiles = shape.ShapeOffsets.Count;
            
            foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
            {
                Vector2Int checkPos = gridPosition + shapeOffset;
                
                if (!gridManager.IsValidGridPosition(checkPos))
                {
                    if (strictBoundsChecking) return false;
                }
                else
                {
                    if (gridManager.IsCellOccupied(checkPos))
                    {
                        if (strictOccupancyChecking) return false;
                    }
                    else
                    {
                        validTiles++;
                    }
                }
            }
            
            if (!strictBoundsChecking && !strictOccupancyChecking)
            {
                return allowPartialOverlap ? validTiles > 0 : validTiles == totalTiles;
            }
            
            return true;
        }
        
        public bool TryPlaceShape(Core.Shape shape)
        {
            Vector2Int gridPosition = shape.GetGridPosition();
            
            if (!CanPlaceShape(shape, gridPosition))
            {
                return false;
            }
            
            // Remove old position if already placed
            if (shape.IsPlaced)
            {
                RemoveShapeFromGrid(shape);
            }
            
            // Place shape
            PlaceShapeOnGrid(shape, gridPosition);
            
            // Update visual position
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPosition);
            shape.transform.position = worldPos;
            
            // Mark as placed
            shape.MarkAsPlaced();
            // Play placement sound via manager (uses per-theme or default fallback)
            var themeStorage = shape.GetComponent<ShapeThemeStorage>();
            var manager = ShapeSpriteManager.Instance;
            if (manager != null)
            {
                var theme = themeStorage != null ? themeStorage.CurrentTheme : null;
                var pos = shape.transform.position;
                manager.PlayPlacementAt(pos, theme);
            }
            
            // Check for line clears
            if (lineClearSystem != null)
            {
                lineClearSystem.CheckAndClearLines();
            }
            
            return true;
        }
        
        private void PlaceShapeOnGrid(Core.Shape shape, Vector2Int gridPosition)
        {
            foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
            {
                Vector2Int pos = gridPosition + shapeOffset;
                gridManager.OccupyCell(pos);
            }
        }
        
        public void RemoveShapeFromGrid(Core.Shape shape)
        {
            Vector2Int currentGridPos = shape.GetGridPosition();
            foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
            {
                Vector2Int pos = currentGridPos + shapeOffset;
                gridManager.FreeCell(pos);
            }
        }
        
        public Vector2Int SnapToGrid(Vector3 worldPosition)
        {
            return gridManager.WorldToGridPosition(worldPosition);
        }
    }
}
