using UnityEngine;
using System.Collections;
using ColorBlast.Core.Architecture;
using ColorBlast.Game;

namespace Gameplay
{
    /// <summary>
    /// Drag start/update/end and return-to-spawn
    /// </summary>
    public partial class DragHandler
    {
        private void TryStartDrag(Vector3 pointerWorld)
        {
            if (isDragging || (shape != null && shape.IsPlaced)) return;

            var bounds = GetBounds();
            if (bounds.size != Vector3.zero && bounds.Contains(new Vector3(pointerWorld.x, pointerWorld.y, transform.position.z)))
            {
                StartDrag(pointerWorld);
                // Play move SFX using the shape's theme
                var mgr = ShapeSpriteManager.Instance != null ? ShapeSpriteManager.Instance : Object.FindFirstObjectByType<ShapeSpriteManager>();
                var theme = mgr != null ? mgr.GetShapeTheme(gameObject) : null;
                if (mgr != null)
                {
                    mgr.PlayMoveAt(transform.position, theme);
                }
                // On drag start switch to original scale if requested
                preDragScale = transform.localScale;
                if (scaleToPlacedOnDrag)
                {
                    transform.localScale = placedScale;
                }
                // Create initial preview
                UpdatePlacementPreview();
            }
        }

        private void StartDrag(Vector3 pointerWorld)
        {
            isDragging = true;
            pointerWorld.z = transform.position.z;
            offset = transform.position - pointerWorld;
            dragVelocity = Vector3.zero;
        }

        private void UpdateDrag(Vector3 pointerWorld)
        {
            pointerWorld.z = transform.position.z;
            Vector3 target;
            if (alignBottomToPointer)
            {
                // Align the bottom of the shape's bounds to the lifted pointer position (uniform across shapes)
                var b = GetBounds();
                float bottomDelta = transform.position.y - b.min.y; // distance from transform pivot to bottom
                target = new Vector3(pointerWorld.x + offset.x, pointerWorld.y + bottomDelta, transform.position.z);
            }
            else
            {
                target = pointerWorld + offset;
            }
            if (smoothDrag)
                transform.position = Vector3.SmoothDamp(transform.position, target, ref dragVelocity, dragSmoothTime, dragMaxSpeed);
            else
                transform.position = target;
        }

        private void EndDrag()
        {
            isDragging = false;
            dragVelocity = Vector3.zero;
            if (boostSortingOrderOnDrag) RestoreSortingOrder();
            DestroyPreview();

            // Check if services are available before using them
            if (!Services.Has<PlacementSystem>())
            {
                ReturnToSpawn();
                return;
            }

            var placementSystem = Services.Get<PlacementSystem>();
            if (placementSystem != null)
            {
                if (placementSystem.TryPlaceShape(shape))
                {
                    if (overrideScaleOnPlacement)
                    {
                        transform.localScale = placedScale;
                    }
                }
                else
                {
                    if (returnToSpawnOnInvalidPlacement)
                    {
                        ReturnToSpawn();
                    }
                }
            }
        }

        private void ReturnToSpawn()
        {
            if (shape == null) return;
            // If we changed scale on drag, restore tray scale before animating back
            if (scaleToPlacedOnDrag && preDragScale != Vector3.zero)
            {
                transform.localScale = preDragScale;
            }
            if (useReturnAnimation)
            {
                StartCoroutine(ReturnToSpawnCoroutine());
            }
            else
            {
                transform.position = shape.OriginalSpawnPosition;
                // Ensure final scale is tray scale
                if (scaleToPlacedOnDrag && preDragScale != Vector3.zero)
                {
                    transform.localScale = preDragScale;
                }
            }
        }

        private IEnumerator ReturnToSpawnCoroutine()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = shape.OriginalSpawnPosition;
            // Pulse around the current scale (already set to preDragScale if applicable)
            Vector3 baseScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < returnAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnAnimationDuration;
                t = 1f - (1f - t) * (1f - t); // Ease-out

                transform.position = Vector3.Lerp(startPos, targetPos, t);

                float scaleEffect = 1f + (0.1f * Mathf.Sin(t * Mathf.PI));
                transform.localScale = baseScale * scaleEffect;

                yield return null;
            }

            transform.position = targetPos;
            transform.localScale = baseScale;
        }
    }
}
