using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Handles line clearing mechanics and cascading effects
    /// </summary>
    public class LineClearSystem : MonoBehaviour
    {
        public static event Action<List<Vector2Int>> OnLinesCleared;
        public static event Action<int> OnShapesDestroyed;
        
        private GridManager gridManager;
        private ShapeDestructionSystem destructionSystem;
        
        private void Start()
        {
            // Registration is now handled by GameManager
            // Get services when they're available
            StartCoroutine(InitializeServices());
        }
        
        private System.Collections.IEnumerator InitializeServices()
        {
            // Wait for services to be registered
            while (!Services.Has<GridManager>() || !Services.Has<ShapeDestructionSystem>())
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            gridManager = Services.Get<GridManager>();
            destructionSystem = Services.Get<ShapeDestructionSystem>();
        }
        
        public List<Vector2Int> CheckAndClearLines()
        {
            List<Vector2Int> totalClearedPositions = new List<Vector2Int>();
            bool foundCompletedLines;
            int cascadeLevel = 0;
            
            do
            {
                foundCompletedLines = false;
                List<Vector2Int> currentClearedPositions = new List<Vector2Int>();
                
                HashSet<int> rowsToCheck = new HashSet<int>();
                HashSet<int> colsToCheck = new HashSet<int>();
                
                if (cascadeLevel == 0)
                {
                    for (int i = 0; i < gridManager.GridHeight; i++) rowsToCheck.Add(i);
                    for (int i = 0; i < gridManager.GridWidth; i++) colsToCheck.Add(i);
                }
                else
                {
                    foreach (Vector2Int pos in totalClearedPositions)
                    {
                        Vector2Int arrayIndices = GridPositionToArrayIndices(pos);
                        if (arrayIndices.x >= 0 && arrayIndices.x < gridManager.GridWidth)
                            colsToCheck.Add(arrayIndices.x);
                        if (arrayIndices.y >= 0 && arrayIndices.y < gridManager.GridHeight)
                            rowsToCheck.Add(arrayIndices.y);
                    }
                }
                
                // Check horizontal lines
                foreach (int row in rowsToCheck)
                {
                    if (IsHorizontalLineComplete(row))
                    {
                        List<Vector2Int> linePositions = ClearHorizontalLine(row);
                        currentClearedPositions.AddRange(linePositions);
                        foundCompletedLines = true;
                    }
                }
                
                // Check vertical lines
                foreach (int col in colsToCheck)
                {
                    if (IsVerticalLineComplete(col))
                    {
                        List<Vector2Int> linePositions = ClearVerticalLine(col);
                        currentClearedPositions.AddRange(linePositions);
                        foundCompletedLines = true;
                    }
                }
                
                totalClearedPositions.AddRange(currentClearedPositions);
                cascadeLevel++;
                
                if (cascadeLevel > 10) break; // Prevent infinite loops
                
            } while (foundCompletedLines);
            
            if (totalClearedPositions.Count > 0)
            {
                int destroyedShapes = destructionSystem?.DestroyShapesAtPositions(totalClearedPositions) ?? 0;
                
                OnLinesCleared?.Invoke(totalClearedPositions);
                OnShapesDestroyed?.Invoke(destroyedShapes);
            }
            
            return totalClearedPositions;
        }
        
        private bool IsHorizontalLineComplete(int row)
        {
            for (int col = 0; col < gridManager.GridWidth; col++)
            {
                Vector2Int gridPos = ArrayIndicesToGridPosition(col, row);
                if (!gridManager.IsCellOccupied(gridPos))
                {
                    return false;
                }
            }
            return true;
        }
        
        private bool IsVerticalLineComplete(int col)
        {
            for (int row = 0; row < gridManager.GridHeight; row++)
            {
                Vector2Int gridPos = ArrayIndicesToGridPosition(col, row);
                if (!gridManager.IsCellOccupied(gridPos))
                {
                    return false;
                }
            }
            return true;
        }
        
    private List<Vector2Int> ClearHorizontalLine(int row)
        {
            List<Vector2Int> clearedPositions = new List<Vector2Int>();
            
            for (int col = 0; col < gridManager.GridWidth; col++)
            {
                Vector2Int gridPos = ArrayIndicesToGridPosition(col, row);
                if (gridManager.IsCellOccupied(gridPos))
                {
            // Play clear FX using theme of any shape that covers this tile
            PlayTileClearFX(gridPos);
            gridManager.FreeCell(gridPos);
                    clearedPositions.Add(gridPos);
                }
            }
            
            return clearedPositions;
        }
        
        private List<Vector2Int> ClearVerticalLine(int col)
        {
            List<Vector2Int> clearedPositions = new List<Vector2Int>();
            
            for (int row = 0; row < gridManager.GridHeight; row++)
            {
                Vector2Int gridPos = ArrayIndicesToGridPosition(col, row);
                if (gridManager.IsCellOccupied(gridPos))
                {
                    PlayTileClearFX(gridPos);
                    gridManager.FreeCell(gridPos);
                    clearedPositions.Add(gridPos);
                }
            }
            
            return clearedPositions;
        }

        private void PlayTileClearFX(Vector2Int gridPos)
        {
            // Locate a placed shape whose offsets include this cell and use its theme
            var shapes = FindObjectsByType<Core.Shape>(FindObjectsSortMode.None);
            SpriteTheme theme = null;
            Vector3 world = gridManager.GridToWorldPosition(gridPos);
            for (int i = 0; i < shapes.Length; i++)
            {
                var s = shapes[i];
                if (s == null || !s.IsPlaced) continue;
                Vector2Int basePos = s.GetGridPosition();
                var offs = s.ShapeOffsets;
                for (int k = 0; k < offs.Count; k++)
                {
                    if (basePos + offs[k] == gridPos)
                    {
                        var st = s.GetComponent<ShapeThemeStorage>();
                        if (st != null) theme = st.CurrentTheme;
                        i = shapes.Length; // break outer
                        break;
                    }
                }
            }
            var mgr = ShapeSpriteManager.Instance;
            if (mgr != null)
            {
                mgr.PlayClearEffectAt(world, theme);
            }
        }
        
        private Vector2Int ArrayIndicesToGridPosition(int col, int row)
        {
            // Use simple grid position mapping
            return new Vector2Int(col, row);
        }
        
        private Vector2Int GridPositionToArrayIndices(Vector2Int gridPos)
        {
            // Grid position is already in array indices format
            return gridPos;
        }
    }
}
