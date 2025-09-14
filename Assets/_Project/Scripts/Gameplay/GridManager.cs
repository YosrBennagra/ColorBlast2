using UnityEngine;
using System.Collections.Generic;
using ColorBlast.Core.Architecture;
using ColorBlast.Core.Interfaces;

namespace Gameplay
{
    /// <summary>
    /// Lightweight, pixel-art friendly grid manager.
    /// - Single uniform cell size
    /// - No runtime line renderers or background objects
    /// - Pixel-perfect snapping via Pixels Per Unit (PPU)
    /// - Simple editor gizmo preview only
    /// </summary>
    public class GridManager : MonoBehaviour, IGridManager
    {
        [Header("Grid Size")]
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;

        [Header("Pixel Art Settings")]
        [Tooltip("Pixels Per Unit used by your sprites (e.g. 16, 32, 100). Must match your art/Pixel Perfect Camera.")]
        [SerializeField] private int pixelsPerUnit = 16;
        [Tooltip("Cell size in pixels (e.g. 16 for a 16x16 tile).")]
        [SerializeField] private int cellSizePixels = 16;
        [Tooltip("Snap positions to the nearest pixel for crisp rendering.")]
        [SerializeField] private bool pixelSnap = true;

        [Header("Gizmo Preview (Editor Only)")]
        [SerializeField] private bool showGridGizmos = true;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color boundaryColor = Color.yellow;

        [Header("Background (Optional)")]
        [SerializeField] private bool showBackground = false;
        [Tooltip("Sprite to use as the grid background. Leave empty to use a simple built-in vignette.")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = Color.white;
        [Tooltip("Extra background size around the grid, in pixels (converted using PPU).")]
        [SerializeField] private Vector2Int backgroundPaddingPixels = Vector2Int.zero;
        [Tooltip("Sorting order for the background (lower draws behind).")]
        [SerializeField] private int backgroundSortingOrder = -10;
        [Tooltip("Optional Z offset for the background object (most 2D setups can leave this at 0).")]
        [SerializeField] private float backgroundZOffset = 0f;

        [Header("Centering")]
        [Tooltip("Keep the grid horizontally centered to the main camera at runtime.")]
        [SerializeField] private bool centerHorizontallyToCamera = true;

        // Internal state
        private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        private Vector3 gridStartPosition; // bottom-left world position of the grid
        private SpriteRenderer backgroundRenderer; // cached renderer
        private Sprite generatedBackgroundSprite;  // cached fallback vignette

        // Public properties
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int PixelsPerUnit => pixelsPerUnit;
        public int CellSizePixels => cellSizePixels;
        // Keep compatibility: CellSize is in world units
        public float CellSize => Mathf.Max(1, cellSizePixels) / Mathf.Max(1, (float)pixelsPerUnit);
        // Backward-compat shims for existing code
        public float CellWidth => CellSize;
        public float CellHeight => CellSize;
        public float CellSpacingX => 0f;
        public float CellSpacingY => 0f;

        private void Awake()
        {
            RecalculateGridStart();
            Services.Register<GridManager>(this);
            // Optional: snap the origin to pixels on load for consistency
            if (pixelSnap)
            {
                transform.position = SnapToPixel(transform.position);
                RecalculateGridStart();
            }
            UpdateBackground();
            // Initial center (runtime)
            CenterToCameraX_Runtime();
        }

        private void OnValidate()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
            cellSizePixels = Mathf.Max(1, cellSizePixels);

            if (pixelSnap)
            {
                transform.position = SnapToPixel(transform.position);
            }

            RecalculateGridStart();
            UpdateBackground();
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        private void LateUpdate()
        {
            CenterToCameraX_Runtime();
        }

        private void CenterToCameraX_Runtime()
        {
            if (!centerHorizontallyToCamera) return;
            var cam = Camera.main;
            if (cam == null) return;
            var pos = transform.position;
            pos.x = cam.transform.position.x;
            if (pixelSnap) pos = SnapToPixel(pos);
            if (pos.x != transform.position.x)
            {
                transform.position = pos;
                RecalculateGridStart();
                UpdateBackground();
            }
        }

        private void RecalculateGridStart()
        {
            // Total grid size in world units
            float totalWidth = gridWidth * CellSize;
            float totalHeight = gridHeight * CellSize;

            // Anchor the grid around this transform (centered)
            gridStartPosition = transform.position;
            gridStartPosition.x -= totalWidth * 0.5f;
            gridStartPosition.y -= totalHeight * 0.5f;
        }

        // Pixel helpers
        public Vector3 SnapToPixel(Vector3 worldPos)
        {
            float unitPerPixel = 1f / Mathf.Max(1, (float)pixelsPerUnit);
            worldPos.x = Mathf.Round(worldPos.x / unitPerPixel) * unitPerPixel;
            worldPos.y = Mathf.Round(worldPos.y / unitPerPixel) * unitPerPixel;
            return worldPos;
        }

        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            var grid = WorldToGridPosition(worldPos);
            return GridToWorldPosition(grid);
        }

        // Core conversions
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            float worldX = (gridPos.x + 0.5f) * CellSize + gridStartPosition.x;
            float worldY = (gridPos.y + 0.5f) * CellSize + gridStartPosition.y;
            var result = new Vector3(worldX, worldY, transform.position.z);
            return pixelSnap ? SnapToPixel(result) : result;
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 local = worldPos - gridStartPosition;
            int gx = Mathf.FloorToInt(local.x / CellSize);
            int gy = Mathf.FloorToInt(local.y / CellSize);
            return new Vector2Int(gx, gy);
        }

        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight;
        }

        // Occupancy API
        public bool IsCellOccupied(Vector2Int gridPos) => occupiedCells.Contains(gridPos);
        public void OccupyCell(Vector2Int gridPos) { if (IsValidGridPosition(gridPos)) occupiedCells.Add(gridPos); }
        public void FreeCell(Vector2Int gridPos) { occupiedCells.Remove(gridPos); }

        public bool CanPlaceShape(Vector2Int startPos, List<Vector2Int> shapeOffsets)
        {
            foreach (var o in shapeOffsets)
            {
                var p = startPos + o;
                if (!IsValidGridPosition(p) || IsCellOccupied(p)) return false;
            }
            return true;
        }

        public bool PlaceShape(Vector2Int startPos, List<Vector2Int> shapeOffsets)
        {
            if (!CanPlaceShape(startPos, shapeOffsets)) return false;
            foreach (var o in shapeOffsets) OccupyCell(startPos + o);
            return true;
        }

        public HashSet<Vector2Int> GetOccupiedPositions() => new HashSet<Vector2Int>(occupiedCells);

        public void ClearAllOccupiedCells()
        {
            occupiedCells.Clear();
        }

        // IGridManager
        public void ClearGrid() => ClearAllOccupiedCells();

        public Vector3 GetGridCenter()
        {
            return transform.position;
        }

        public bool ValidateGridPositioning()
        {
            var p = new Vector2Int(0, 0);
            var w = GridToWorldPosition(p);
            var back = WorldToGridPosition(w);
            return back == p;
        }

        public Vector3 GetCellCenter(Vector2Int gridPos) => GridToWorldPosition(gridPos);

        // Runtime setters kept for compatibility
        public void SetCellSize(float newCellSize)
        {
            // Convert world units to pixels and keep it integer to stay pixel-perfect
            int newPixels = Mathf.Max(1, Mathf.RoundToInt(newCellSize * pixelsPerUnit));
            if (newPixels == cellSizePixels) return;
            cellSizePixels = newPixels;
            RecalculateGridStart();
            UpdateBackground();
        }

        // Gizmo preview (editor only)
        private void OnDrawGizmos()
        {
            if (!showGridGizmos) return;
            RecalculateGridStart();

            // Draw cell borders lightly
            Gizmos.color = gridLineColor;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 center = GridToWorldPosition(new Vector2Int(x, y));
                    Vector3 size = new Vector3(CellSize, CellSize, 0.01f);
                    Gizmos.DrawWireCube(center, size);
                }
            }

            // Draw outer boundary
            Gizmos.color = boundaryColor;
            float totalW = gridWidth * CellSize;
            float totalH = gridHeight * CellSize;
            Vector3 gridCenter = transform.position;
            Gizmos.DrawWireCube(gridCenter, new Vector3(totalW, totalH, 0.01f));

            // Draw origin dot
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * (CellSize * 0.1f));
        }

        // --- Background helpers ---
        private void UpdateBackground()
        {
            if (!showBackground)
            {
                if (backgroundRenderer != null)
                {
                    backgroundRenderer.gameObject.SetActive(false);
                }
                return;
            }

            EnsureBackgroundRenderer();

            // Pick sprite: user-provided or generated vignette
            if (backgroundSprite != null)
            {
                backgroundRenderer.sprite = backgroundSprite;
            }
            else
            {
                if (generatedBackgroundSprite == null)
                {
                    generatedBackgroundSprite = CreateVignetteSprite(64, 64);
                }
                backgroundRenderer.sprite = generatedBackgroundSprite;
            }

            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingOrder = backgroundSortingOrder;

            // Compute target size in world units
            float totalW = gridWidth * CellSize + backgroundPaddingPixels.x / Mathf.Max(1f, pixelsPerUnit);
            float totalH = gridHeight * CellSize + backgroundPaddingPixels.y / Mathf.Max(1f, pixelsPerUnit);

            // Scale sprite to fit
            Vector2 spriteSize = backgroundRenderer.sprite != null ? backgroundRenderer.sprite.bounds.size : Vector2.one;
            if (spriteSize.x <= 0f) spriteSize.x = 1f;
            if (spriteSize.y <= 0f) spriteSize.y = 1f;

            // Center and align to pixels if enabled
            Vector3 center = transform.position;
            if (pixelSnap) center = SnapToPixel(center);

            var tf = backgroundRenderer.transform;
            tf.position = new Vector3(center.x, center.y, center.z + backgroundZOffset);
            tf.localScale = new Vector3(totalW / spriteSize.x, totalH / spriteSize.y, 1f);
            tf.localRotation = Quaternion.identity;

            backgroundRenderer.gameObject.SetActive(true);
        }

        private void EnsureBackgroundRenderer()
        {
            if (backgroundRenderer != null) return;
            Transform t = transform.Find("GridBackground");
            if (t != null)
            {
                backgroundRenderer = t.GetComponent<SpriteRenderer>();
            }
            if (backgroundRenderer == null)
            {
                var go = new GameObject("GridBackground");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                backgroundRenderer = go.AddComponent<SpriteRenderer>();
                backgroundRenderer.drawMode = SpriteDrawMode.Simple; // minimal, we scale via transform
                backgroundRenderer.sharedMaterial = null; // default sprite mat
            }
        }

        private Sprite CreateVignetteSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // crisp for pixel art
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
            float maxDist = Vector2.Distance(Vector2.zero, center);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    // Darker at edges, lighter in center
                    float v = Mathf.Lerp(0.85f, 0.6f, Mathf.SmoothStep(0f, 1f, d));
                    pixels[y * w + x] = new Color(v, v, v, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            // Use the grid PPU so it scales consistently
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(1, pixelsPerUnit));
        }

#if UNITY_EDITOR
        [ContextMenu("Grid/Snap Origin To Pixel Grid")]
        private void Editor_SnapOriginToPixels()
        {
            transform.position = SnapToPixel(transform.position);
            RecalculateGridStart();
            UnityEditor.SceneView.RepaintAll();
        }

        [ContextMenu("Grid/Rebuild Background")] 
        private void Editor_RebuildBackground()
        {
            UpdateBackground();
            UnityEditor.SceneView.RepaintAll();
        }

        [ContextMenu("Grid/Center Horizontally To Camera Now")] 
        private void Editor_CenterToCameraXNow()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var pos = transform.position;
            pos.x = cam.transform.position.x;
            if (pixelSnap) pos = SnapToPixel(pos);
            transform.position = pos;
            RecalculateGridStart();
            UpdateBackground();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }
}
