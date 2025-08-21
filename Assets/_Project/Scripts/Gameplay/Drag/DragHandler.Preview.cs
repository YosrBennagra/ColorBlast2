using UnityEngine;
using ColorBlast.Core.Architecture;
using ColorBlast.Game;

namespace Gameplay
{
    /// <summary>
    /// Placement preview building and updating
    /// </summary>
    public partial class DragHandler
    {
        private void UpdatePlacementPreview()
        {
            if (!Services.Has<GridManager>()) { DestroyPreview(); return; }
            var grid = Services.Get<GridManager>();
            if (shape == null) { DestroyPreview(); return; }

            var gridPos = grid.WorldToGridPosition(transform.position);

            bool isValid = false;
            if (Services.Has<PlacementSystem>())
            {
                var placement = Services.Get<PlacementSystem>();
                isValid = placement != null && placement.CanPlaceShape(shape, gridPos);
            }
            else
            {
                // Fallback: only bounds check
                isValid = true;
                var offsets = shape.ShapeOffsets;
                for (int i = 0; i < offsets.Count; i++)
                {
                    if (!grid.IsValidGridPosition(gridPos + offsets[i])) { isValid = false; break; }
                }
            }

            if (!isValid)
            {
                if (previewRoot != null) previewRoot.SetActive(false);
                return;
            }

            var worldSnap = grid.GridToWorldPosition(gridPos);
            if (previewRoot == null) BuildPreviewObjects();
            if (previewRoot != null)
            {
                previewRoot.SetActive(true);
                previewRoot.transform.position = worldSnap;
            }
        }

        private void BuildPreviewObjects()
        {
            DestroyPreview();
            previewRoot = new GameObject("PlacementPreview");
            previewRoot.transform.SetPositionAndRotation(transform.position, Quaternion.identity);

            var srcTiles = shape != null ? shape.TileRenderers : GetComponentsInChildren<SpriteRenderer>();
            if (srcTiles == null || srcTiles.Length == 0) return;
            previewRenderers = new SpriteRenderer[srcTiles.Length];

            for (int i = 0; i < srcTiles.Length; i++)
            {
                var src = srcTiles[i];
                if (src == null) continue;

                var child = new GameObject("TilePreview_" + i);
                child.transform.SetParent(previewRoot.transform, worldPositionStays: false);
                child.transform.position = src.transform.position; // world match
                child.transform.rotation = src.transform.rotation;
                child.transform.localScale = src.transform.lossyScale; // approximate

                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = src.sprite;
                sr.color = new Color(1f, 1f, 1f, 0.55f);
                sr.sortingLayerID = src.sortingLayerID;
                sr.sortingOrder = (originalSortingOrders != null && i < originalSortingOrders.Length) ? originalSortingOrders[i] : src.sortingOrder;
                previewRenderers[i] = sr;
            }
        }

        private void DestroyPreview()
        {
            if (previewRoot != null)
            {
                Destroy(previewRoot);
                previewRoot = null;
                previewRenderers = null;
            }
        }

        private float ComputeAutoLiftPixels()
        {
            if (cam == null) cam = Camera.main;
            var b = GetBounds();
            if (b.size == Vector3.zero || cam == null) return dragLiftScreenPixels;
            var min = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, transform.position.z));
            var max = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, transform.position.z));
            float screenHeight = Mathf.Abs(max.y - min.y);
            return screenHeight * Mathf.Max(0.1f, autoLiftMultiplier) + Mathf.Max(0f, extraLiftPixels);
        }
    }
}
