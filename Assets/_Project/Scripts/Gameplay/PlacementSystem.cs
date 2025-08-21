using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColorBlast.Core.Architecture;
using ColorBlast.Game;

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
        
        public bool CanPlaceShape(Shape shape, Vector2Int gridPosition)
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
            
            // For strict checking, all tiles must be valid
            if (strictBoundsChecking && strictOccupancyChecking)
            {
                bool canPlace = validTiles == totalTiles;
                if (!canPlace)
                {
                    Debug.Log($"PlacementSystem: Cannot place shape at ({gridPosition.x}, {gridPosition.y}) - only {validTiles}/{totalTiles} tiles valid");
                }
                return canPlace;
            }
            
            // For non-strict checking, use allowPartialOverlap setting
            return allowPartialOverlap ? validTiles > 0 : validTiles == totalTiles;
        }
        
        public bool TryPlaceShape(Shape shape)
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

            // Award score for shape placement
            var scoreManager = GameObject.FindAnyObjectByType<ColorBlast2.Systems.Scoring.ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.AddShapePlacementPoints(shape.ShapeOffsets.Count);
            }
            // Play placement sound via manager (uses per-theme or default fallback)
            var themeStorage = shape.GetComponent<ShapeThemeStorage>();
            var manager = ShapeSpriteManager.Instance;
            if (manager != null)
            {
                var theme = themeStorage != null ? themeStorage.CurrentTheme : null;
                var pos = shape.transform.position;
                manager.PlayPlacementAt(pos, theme);
                manager.PlayPlacementAnimation(shape);
            }
            
            // Check for line clears
            bool cleared = false;
            if (lineClearSystem != null)
            {
                var clearedLines = lineClearSystem.CheckAndClearLines();
                cleared = clearedLines != null && clearedLines.Count > 0;
            }
            // Combo system: if no line was cleared, notify ScoreManager
            if (!cleared && scoreManager != null)
            {
                scoreManager.OnShapePlacedNoClear();
            }
            
            return true;
        }
        
        private void PlaceShapeOnGrid(Shape shape, Vector2Int gridPosition)
        {
            foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
            {
                Vector2Int pos = gridPosition + shapeOffset;
                gridManager.OccupyCell(pos);
            }
        }
        
        public void RemoveShapeFromGrid(Shape shape)
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
