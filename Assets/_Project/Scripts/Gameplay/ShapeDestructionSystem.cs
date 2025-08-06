using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Handles destruction and splitting of shapes when tiles are cleared
    /// </summary>
    public class ShapeDestructionSystem : MonoBehaviour
    {
        private GridManager gridManager;
        
        private void Start()
        {
            // Registration is now handled by GameManager
            // Get services when they're available
            StartCoroutine(InitializeServices());
        }
        
        private System.Collections.IEnumerator InitializeServices()
        {
            // Wait for services to be registered
            while (!Services.Has<GridManager>())
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            gridManager = Services.Get<GridManager>();
        }
        
        public int DestroyShapesAtPositions(List<Vector2Int> clearedPositions)
        {
            var allShapes = FindObjectsByType<Core.Shape>(FindObjectsSortMode.None);
            int affectedShapes = 0;
            
            foreach (var shape in allShapes)
            {
                if (!shape.IsPlaced) continue;
                
                Vector2Int shapeGridPos = shape.GetGridPosition();
                
                bool hasAffectedTiles = false;
                foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
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
                    RemoveClearedTilesFromShape(shape, shapeGridPos, clearedPositions);
                    affectedShapes++;
                }
            }
            
            return affectedShapes;
        }
        
        private void RemoveClearedTilesFromShape(Core.Shape shape, Vector2Int shapeGridPos, List<Vector2Int> clearedPositions)
        {
            List<Vector2Int> remainingOffsets = new List<Vector2Int>();
            
            foreach (Vector2Int shapeOffset in shape.ShapeOffsets)
            {
                Vector2Int tilePos = shapeGridPos + shapeOffset;
                if (!clearedPositions.Contains(tilePos))
                {
                    remainingOffsets.Add(shapeOffset);
                }
            }
            
            if (remainingOffsets.Count == 0)
            {
                Debug.Log($"Destroying shape {shape.gameObject.name} - all tiles were cleared");
                Destroy(shape.gameObject);
            }
            else if (remainingOffsets.Count < shape.ShapeOffsets.Count)
            {
                Vector3 currentPosition = shape.transform.position;
                CreatePartialShape(remainingOffsets, currentPosition, shape.gameObject.name + "_Partial", shape);
                
                Debug.Log($"Shape {shape.gameObject.name} split - {shape.ShapeOffsets.Count - remainingOffsets.Count} tiles removed");
                Destroy(shape.gameObject);
            }
        }
        
        private void CreatePartialShape(List<Vector2Int> offsets, Vector3 position, string name, Core.Shape originalShape)
        {
            GameObject newShapeObj = new GameObject(name);
            newShapeObj.transform.position = position;
            
            var newShape = newShapeObj.AddComponent<Core.Shape>();
            newShape.SetShapeOffsets(offsets);
            newShape.MarkAsPlaced();
            
            Vector2Int gridPos = gridManager.WorldToGridPosition(position);
            
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int pos = gridPos + offset;
                gridManager.OccupyCell(pos);
            }
            
            CopyOriginalTiles(originalShape, newShape, offsets);
        }
        
        private void CopyOriginalTiles(Core.Shape originalShape, Core.Shape newShape, List<Vector2Int> remainingOffsets)
        {
            Transform[] originalChildren = new Transform[originalShape.transform.childCount];
            for (int i = 0; i < originalShape.transform.childCount; i++)
            {
                originalChildren[i] = originalShape.transform.GetChild(i);
            }
            
            Color originalTileColor = Color.white;
            if (originalChildren.Length > 0 && originalChildren[0] != null)
            {
                var originalRenderer = originalChildren[0].GetComponent<SpriteRenderer>();
                if (originalRenderer != null)
                {
                    originalTileColor = originalRenderer.color;
                }
            }
            
            Dictionary<Vector2Int, GameObject> originalTileMap = new Dictionary<Vector2Int, GameObject>();
            for (int i = 0; i < originalChildren.Length && i < originalShape.ShapeOffsets.Count; i++)
            {
                if (originalChildren[i] != null)
                {
                    originalTileMap[originalShape.ShapeOffsets[i]] = originalChildren[i].gameObject;
                }
            }
            
            for (int i = 0; i < remainingOffsets.Count; i++)
            {
                Vector2Int offset = remainingOffsets[i];
                
                if (originalTileMap.ContainsKey(offset))
                {
                    GameObject originalTile = originalTileMap[offset];
                    GameObject newTile = Instantiate(originalTile, newShape.transform);
                    newTile.transform.localPosition = new Vector3(
                        offset.x * newShape.GridSize,
                        offset.y * newShape.GridSize,
                        0
                    );
                }
                else
                {
                    CreateSimpleTile(newShape, offset, originalTileColor);
                }
            }
            
            newShape.CacheTileRenderers();
        }
        
        private void CreateSimpleTile(Core.Shape shape, Vector2Int offset, Color tileColor)
        {
            GameObject tile = new GameObject("SimpleTile");
            tile.transform.SetParent(shape.transform);
            tile.transform.localPosition = new Vector3(offset.x * shape.GridSize, offset.y * shape.GridSize, 0);
            tile.transform.localScale = Vector3.one * shape.GridSize;
            
            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = tileColor;
            renderer.sortingOrder = 0;
        }
    }
}
