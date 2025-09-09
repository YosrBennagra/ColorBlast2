using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ColorBlast.Core.Architecture;
using ColorBlast2.Systems.Scoring;
using ColorBlast.Game;

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
            int totalBlocksCleared = 0;
            bool anySameSpriteLine = false;
            
            do
            {
                foundCompletedLines = false;
                // Collect full rows/cols without modifying the grid, then clear simultaneously
                var currentClearedPositions = new HashSet<Vector2Int>();
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
                // Determine completed rows and columns first
                var completedRows = new List<int>();
                var completedCols = new List<int>();
                foreach (int row in rowsToCheck) { if (IsHorizontalLineComplete(row)) completedRows.Add(row); }
                foreach (int col in colsToCheck) { if (IsVerticalLineComplete(col)) completedCols.Add(col); }

                // Nothing this cascade
                if (completedRows.Count == 0 && completedCols.Count == 0)
                {
                    // no changes this pass
                }
                else
                {
                    foundCompletedLines = true;
                    // Build union of cells to clear (rows + cols)
                    foreach (int row in completedRows)
                    {
                        var linePositions = new List<Vector2Int>(gridManager.GridWidth);
                        for (int col = 0; col < gridManager.GridWidth; col++)
                        {
                            linePositions.Add(ArrayIndicesToGridPosition(col, row));
                        }
                        // score helper before clearing
                        if (IsSameSpriteLine(linePositions)) anySameSpriteLine = true;
                        for (int i = 0; i < linePositions.Count; i++) currentClearedPositions.Add(linePositions[i]);
                    }
                    foreach (int col in completedCols)
                    {
                        var linePositions = new List<Vector2Int>(gridManager.GridHeight);
                        for (int row = 0; row < gridManager.GridHeight; row++)
                        {
                            linePositions.Add(ArrayIndicesToGridPosition(col, row));
                        }
                        if (IsSameSpriteLine(linePositions)) anySameSpriteLine = true;
                        for (int i = 0; i < linePositions.Count; i++) currentClearedPositions.Add(linePositions[i]);
                    }

                    // Fancy FX: line sweeps and intersection pulses
                    var fxMgr = ShapeSpriteManager.Instance;
                    if (fxMgr != null)
                    {
                        foreach (int row in completedRows)
                        {
                            Vector3 start = gridManager.GridToWorldPosition(new Vector2Int(0, row));
                            Vector3 end = gridManager.GridToWorldPosition(new Vector2Int(gridManager.GridWidth - 1, row));
                            fxMgr.PlayLineSweep(start, end);
                        }
                        foreach (int col in completedCols)
                        {
                            Vector3 start = gridManager.GridToWorldPosition(new Vector2Int(col, 0));
                            Vector3 end = gridManager.GridToWorldPosition(new Vector2Int(col, gridManager.GridHeight - 1));
                            fxMgr.PlayLineSweep(start, end);
                        }
                        // Intersections: cells that are in both a completed row and column
                        var rowSet = new HashSet<int>(completedRows);
                        var colSet = new HashSet<int>(completedCols);
                        if (rowSet.Count > 0 && colSet.Count > 0)
                        {
                            foreach (int r in rowSet)
                            {
                                foreach (int c in colSet)
                                {
                                    var wp = gridManager.GridToWorldPosition(new Vector2Int(c, r));
                                    fxMgr.PlayIntersectionPulse(wp);
                                }
                            }
                        }
                    }

                    // Build a per-cell visual snapshot before we clear (so sprites/colors are available for FX)
                    var visuals = CaptureTileVisuals(currentClearedPositions);

                    // Start sequential per-line clear FX (visual only; logic clears immediately below)
                    StartCoroutine(PlaySequentialClearFX(completedRows, completedCols, visuals, 0.035f));

                    // Now actually clear cells in one shot
                    foreach (var pos in currentClearedPositions)
                    {
                        if (gridManager.IsCellOccupied(pos))
                        {
                            gridManager.FreeCell(pos);
                        }
                    }
                    totalClearedPositions.AddRange(currentClearedPositions);
                    totalBlocksCleared += currentClearedPositions.Count;
                }
                cascadeLevel++;
                
                if (cascadeLevel > 10) break; // Prevent infinite loops
                
            } while (foundCompletedLines);
            
            if (totalClearedPositions.Count > 0)
            {
                int destroyedShapes = destructionSystem?.DestroyShapesAtPositions(totalClearedPositions) ?? 0;
                
                OnLinesCleared?.Invoke(totalClearedPositions);
                OnShapesDestroyed?.Invoke(destroyedShapes);
                
                // --- SCORING SYSTEM HOOKS ---
                var scoreManager = GameObject.FindAnyObjectByType<ScoreManager>();
                if (scoreManager != null)
                {
                    scoreManager.AddBlockPoints(totalBlocksCleared);
                    scoreManager.AddLineClearBonus(anySameSpriteLine);
                    // Perfect Clear: if no occupied cells remain after cascades
                    if (gridManager.GetOccupiedPositions().Count == 0)
                    {
                        scoreManager.AddPerfectClearBonus();
                    }
                }
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
            var shapes = FindObjectsByType<Shape>(FindObjectsSortMode.None);
            SpriteTheme theme = null;
            Vector3 world = gridManager.GridToWorldPosition(gridPos);
            Sprite tileSprite = null;
            Color tileColor = Color.white;
            for (int i = 0; i < shapes.Length; i++)
            {
                var s = shapes[i];
                if (s == null || !s.IsPlaced) continue;
                Vector2Int basePos = s.GetGridPosition();
                var offs = s.ShapeOffsets;
                s.CacheTileRenderers();
                var tiles = s.TileRenderers;
                for (int k = 0; k < offs.Count; k++)
                {
                    if (basePos + offs[k] == gridPos)
                    {
                        var st = s.GetComponent<ShapeThemeStorage>();
                        if (st != null) theme = st.CurrentTheme;
                        if (tiles != null && k < tiles.Length && tiles[k] != null)
                        {
                            tileSprite = tiles[k].sprite;
                            tileColor = tiles[k].color;
                        }
                        i = shapes.Length; // break outer
                        break;
                    }
                }
            }
            var mgr = ShapeSpriteManager.Instance;
            if (mgr != null)
            {
                // Use tile sprite/color to generate a material-consistent shatter when prefab not set
                mgr.PlayClearEffectAt(world, theme, tileSprite, tileColor);
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

        // Visual snapshot for a tile used to play FX after shapes/grid have been cleared
        private sealed class TileVisual
        {
            public Sprite sprite;
            public Color color;
        }

        private Dictionary<Vector2Int, TileVisual> CaptureTileVisuals(IEnumerable<Vector2Int> positions)
        {
            var dict = new Dictionary<Vector2Int, TileVisual>();
            if (positions == null) return dict;
            var shapes = FindObjectsByType<Shape>(FindObjectsSortMode.None);
            foreach (var pos in positions)
            {
                Sprite foundSprite = null; Color foundColor = Color.white;
                for (int i = 0; i < shapes.Length && foundSprite == null; i++)
                {
                    var s = shapes[i]; if (s == null || !s.IsPlaced) continue;
                    Vector2Int basePos = s.GetGridPosition();
                    var offs = s.ShapeOffsets;
                    s.CacheTileRenderers(); var tiles = s.TileRenderers;
                    for (int k = 0; k < offs.Count; k++)
                    {
                        if (basePos + offs[k] == pos)
                        {
                            if (tiles != null && k < tiles.Length && tiles[k] != null)
                            {
                                foundSprite = tiles[k].sprite;
                                foundColor = tiles[k].color;
                            }
                            break;
                        }
                    }
                }
                dict[pos] = new TileVisual { sprite = foundSprite, color = foundColor };
            }
            return dict;
        }

        private IEnumerator PlaySequentialClearFX(List<int> rows, List<int> cols, Dictionary<Vector2Int, TileVisual> visuals, float perStepDelay)
        {
            if (gridManager == null) yield break;
            var mgr = ShapeSpriteManager.Instance; if (mgr == null) yield break;
            var scheduled = new HashSet<Vector2Int>();
            float step = 0f;
            // rows: left -> right
            if (rows != null)
            {
                for (int rIndex = 0; rIndex < rows.Count; rIndex++)
                {
                    int row = rows[rIndex];
                    bool leftToRight = (rIndex % 2) == 0; // snake pattern for a more organic feel
                    if (leftToRight)
                    {
                        for (int x = 0; x < gridManager.GridWidth; x++)
                        {
                            var gp = ArrayIndicesToGridPosition(x, row);
                            if (visuals != null && visuals.ContainsKey(gp) && !scheduled.Contains(gp))
                            {
                                float jitter = UnityEngine.Random.Range(0f, perStepDelay * 0.2f);
                                StartCoroutine(DelayedPlayTileFX(gp, visuals[gp], step * perStepDelay + jitter));
                                scheduled.Add(gp);
                                step += 1f;
                            }
                        }
                    }
                    else
                    {
                        for (int x = gridManager.GridWidth - 1; x >= 0; x--)
                        {
                            var gp = ArrayIndicesToGridPosition(x, row);
                            if (visuals != null && visuals.ContainsKey(gp) && !scheduled.Contains(gp))
                            {
                                float jitter = UnityEngine.Random.Range(0f, perStepDelay * 0.2f);
                                StartCoroutine(DelayedPlayTileFX(gp, visuals[gp], step * perStepDelay + jitter));
                                scheduled.Add(gp);
                                step += 1f;
                            }
                        }
                    }
                }
            }
            // columns: top -> bottom
            if (cols != null)
            {
                for (int cIndex = 0; cIndex < cols.Count; cIndex++)
                {
                    int col = cols[cIndex];
                    bool topToBottom = (cIndex % 2) == 0;
                    if (topToBottom)
                    {
                        for (int y = gridManager.GridHeight - 1; y >= 0; y--)
                        {
                            var gp = ArrayIndicesToGridPosition(col, y);
                            if (visuals != null && visuals.ContainsKey(gp) && !scheduled.Contains(gp))
                            {
                                float jitter = UnityEngine.Random.Range(0f, perStepDelay * 0.2f);
                                StartCoroutine(DelayedPlayTileFX(gp, visuals[gp], step * perStepDelay + jitter));
                                scheduled.Add(gp);
                                step += 1f;
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < gridManager.GridHeight; y++)
                        {
                            var gp = ArrayIndicesToGridPosition(col, y);
                            if (visuals != null && visuals.ContainsKey(gp) && !scheduled.Contains(gp))
                            {
                                float jitter = UnityEngine.Random.Range(0f, perStepDelay * 0.2f);
                                StartCoroutine(DelayedPlayTileFX(gp, visuals[gp], step * perStepDelay + jitter));
                                scheduled.Add(gp);
                                step += 1f;
                            }
                        }
                    }
                }
            }
            yield break;
        }

        private IEnumerator DelayedPlayTileFX(Vector2Int gp, TileVisual vis, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var mgr = ShapeSpriteManager.Instance; if (mgr == null) yield break;
            Vector3 wp = gridManager.GridToWorldPosition(gp);
            mgr.PlayClearEffectAt(wp, null, vis != null ? vis.sprite : null, vis != null ? vis.color : Color.white);
        }
        
        // Helper to check if all blocks in a line are the same sprite (for bonus)
        private bool IsSameSpriteLine(List<Vector2Int> linePositions)
        {
            if (linePositions == null || linePositions.Count == 0) return false;
            Sprite firstSprite = null;
            foreach (var pos in linePositions)
            {
                var shape = FindShapeAtGridPos(pos);
                if (shape == null) return false;
                var sprite = shape.GetSpriteAtGridPos(pos);
                if (sprite == null) return false;
                if (firstSprite == null) firstSprite = sprite;
                else if (sprite != firstSprite) return false;
            }
            return true;
        }
        // Helper to find shape at a grid position
        private Shape FindShapeAtGridPos(Vector2Int pos)
        {
            var shapes = FindObjectsByType<Shape>(FindObjectsSortMode.None);
            foreach (var s in shapes)
            {
                if (!s.IsPlaced) continue;
                Vector2Int basePos = s.GetGridPosition();
                var offs = s.ShapeOffsets;
                for (int k = 0; k < offs.Count; k++)
                {
                    if (basePos + offs[k] == pos)
                        return s;
                }
            }
            return null;
        }
    }
}
