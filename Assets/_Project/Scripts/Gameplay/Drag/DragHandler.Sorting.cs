using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Sorting layer/order adjustments while dragging
    /// </summary>
    public partial class DragHandler
    {
        private void BoostSortingOrder()
        {
            if (cachedRenderers == null) return;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == null) continue;
                if (useAbsoluteDragSorting)
                {
                    // Switch layer if provided
                    if (!string.IsNullOrEmpty(dragSortingLayerName))
                    {
                        int lid = SortingLayer.NameToID(dragSortingLayerName);
                        if (lid != 0) cachedRenderers[i].sortingLayerID = lid;
                    }
                    cachedRenderers[i].sortingOrder = dragSortingOrderAbsolute;
                }
                else
                {
                    cachedRenderers[i].sortingOrder = (originalSortingOrders != null && i < originalSortingOrders.Length)
                        ? originalSortingOrders[i] + sortingOrderBoost
                        : cachedRenderers[i].sortingOrder + sortingOrderBoost;
                }
            }
        }

        private void RestoreSortingOrder()
        {
            if (cachedRenderers == null) return;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == null) continue;
                if (originalSortingOrders != null && i < originalSortingOrders.Length)
                {
                    cachedRenderers[i].sortingOrder = originalSortingOrders[i];
                }
                if (originalSortingLayerIDs != null && i < originalSortingLayerIDs.Length)
                {
                    cachedRenderers[i].sortingLayerID = originalSortingLayerIDs[i];
                }
            }
        }
    }
}
