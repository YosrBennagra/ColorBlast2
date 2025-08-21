using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Gameplay;
using ColorBlast.Core.Architecture;

namespace ColorBlast.Game
{
    /// <summary>
    /// Represents a shape that can be placed on the grid
    /// </summary>
    public class Shape : MonoBehaviour
    {
        [Header("Shape Configuration")]
        [SerializeField] private List<Vector2Int> shapeOffsets = new List<Vector2Int>();
        [SerializeField] private float gridSize = 1f;

        private bool isPlaced = false;
        private Vector3 originalSpawnPosition;
        private SpriteRenderer[] tileRenderers;

        public List<Vector2Int> ShapeOffsets => shapeOffsets;
        public float GridSize => gridSize;
        public bool IsPlaced => isPlaced;
        public Vector3 OriginalSpawnPosition => originalSpawnPosition;
        public SpriteRenderer[] TileRenderers => tileRenderers;

        private void Start()
        {
            originalSpawnPosition = transform.position;

            if (shapeOffsets.Count == 0)
            {
                shapeOffsets.Add(Vector2Int.zero);
            }

            CacheTileRenderers();
        }

        public void SetShapeOffsets(List<Vector2Int> offsets)
        {
            shapeOffsets = new List<Vector2Int>(offsets);
        }

        public void SetSpawnPosition(Vector3 position)
        {
            originalSpawnPosition = position;
        }

        public void MarkAsPlaced()
        {
            isPlaced = true;

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        public void ResetShape()
        {
            if (!isPlaced) return;

            isPlaced = false;

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        public void CacheTileRenderers()
        {
            tileRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// Apply new offsets and reposition child tile renderers to match the grid-aligned layout.
        /// Assumes each tile corresponds to one offset in order. Extra tiles (if any) are left as-is.
        /// </summary>
        public void ApplyOffsetsAndRealign(List<Vector2Int> newOffsets)
        {
            if (newOffsets == null || newOffsets.Count == 0) return;
            shapeOffsets = new List<Vector2Int>(newOffsets);
            if (tileRenderers == null || tileRenderers.Length == 0)
            {
                CacheTileRenderers();
            }
            if (tileRenderers == null || tileRenderers.Length == 0) return;

            int count = Mathf.Min(tileRenderers.Length, shapeOffsets.Count);
            for (int i = 0; i < count; i++)
            {
                var sr = tileRenderers[i];
                if (sr == null) continue;
                var lp = sr.transform.localPosition;
                lp.x = shapeOffsets[i].x * gridSize;
                lp.y = shapeOffsets[i].y * gridSize;
                sr.transform.localPosition = lp;
                // Ensure no stray rotation on tiles for pixel-perfect visuals
                var lr = sr.transform.localRotation;
                lr = Quaternion.identity;
                sr.transform.localRotation = lr;
            }
        }

        public Vector2Int GetGridPosition()
        {
            // Use GridManager's conversion method for accurate positioning
            if (Services.Has<GridManager>())
            {
                var gridManager = Services.Get<GridManager>();
                return gridManager.WorldToGridPosition(transform.position);
            }

            // Fallback to old method if GridManager is not available
            return new Vector2Int(
                Mathf.RoundToInt(transform.position.x / gridSize),
                Mathf.RoundToInt(transform.position.y / gridSize)
            );
        }

        /// <summary>
        /// Returns the sprite at the given grid position if this shape covers it, otherwise null.
        /// </summary>
        public Sprite GetSpriteAtGridPos(Vector2Int gridPos)
        {
            Vector2Int basePos = GetGridPosition();
            for (int i = 0; i < shapeOffsets.Count; i++)
            {
                if (basePos + shapeOffsets[i] == gridPos && tileRenderers != null && i < tileRenderers.Length)
                {
                    return tileRenderers[i]?.sprite;
                }
            }
            return null;
        }
    }
}
