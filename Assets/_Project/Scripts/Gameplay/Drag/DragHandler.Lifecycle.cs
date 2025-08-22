using UnityEngine;
using ColorBlast.Game;

namespace Gameplay
{
    /// <summary>
    /// DragHandler lifecycle: Start/Update
    /// </summary>
    public partial class DragHandler : MonoBehaviour
    {
        private void Start()
        {
            shape = GetComponent<Shape>();
            cam = Camera.main;
            dragUnlockTime = Time.time + Mathf.Max(0f, dragLockDurationOnSpawn);
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (cachedRenderers != null && cachedRenderers.Length > 0)
            {
                originalSortingOrders = new int[cachedRenderers.Length];
                originalSortingLayerIDs = new int[cachedRenderers.Length];
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null)
                    {
                        originalSortingOrders[i] = cachedRenderers[i].sortingOrder;
                        originalSortingLayerIDs[i] = cachedRenderers[i].sortingLayerID;
                    }
                }
            }
        }

        private void Update()
        {
            if (shape != null && shape.IsPlaced) return;
            if (cam == null) cam = Camera.main;

            // Start drag (gated by cooldown and movement threshold)
            if (!isDragging)
            {
                // Prime a press if allowed
                if (WasPointerPressedThisFrame(out Vector2 pressScreen, out int pressId))
                {
                    if (ignoreUI && IsOverUI(pressId)) return;
                    if (Time.time < dragUnlockTime) return; // still locked
                    if (!IsPointerOnShape(pressScreen)) return;
                    // Either start drag immediately for instant pop, or prime for thresholded start
                    if (startDragOnPointerDown)
                    {
                        Vector3 world = ScreenToWorld(pressScreen);
                        StartDrag(world);
                        // Play move SFX on grab begin
                        var mgr = ShapeSpriteManager.Instance != null ? ShapeSpriteManager.Instance : Object.FindFirstObjectByType<ShapeSpriteManager>();
                        if (mgr != null)
                        {
                            var theme = mgr.GetShapeTheme(gameObject);
                            mgr.PlayMoveAt(transform.position, theme);
                        }
                        activeTouchId = pressId;
                        isTouchDrag = activeTouchId >= 0;
                        if (boostSortingOrderOnDrag) BoostSortingOrder();
                        preDragScale = transform.localScale;
                        if (scaleToPlacedOnDrag)
                        {
                            transform.localScale = placedScale;
                        }
                        lastDragScreenPos = pressScreen;
                        dragStartScreenPos = pressScreen;
                        // Initial pop this frame using lift even before movement
                        Vector2 initialScreen = pressScreen;
                        if (alignBottomToPointer)
                        {
                            initialScreen.y += uniformLiftPixels;
                        }
                        else if (liftOnDrag && (!liftOnlyOnTouch || isTouchDrag))
                        {
                            float lift = dragLiftScreenPixels;
                            if (autoLiftByBounds)
                            {
                                lift = Mathf.Max(dragLiftScreenPixels, ComputeAutoLiftPixels());
                            }
                            initialScreen.y += lift;
                        }
                        if (addExtraLiftOnPress)
                        {
                            initialScreen.y += pressExtraLiftPixels;
                        }
                        UpdateDrag(ScreenToWorld(initialScreen));
                        // Create initial preview
                        UpdatePlacementPreview();
                        pressPrimed = false;
                        primedTouchId = -1;
                    }
                    else
                    {
                        pressPrimed = true;
                        primedPressScreenPos = pressScreen;
                        primedTouchId = pressId; // -1 mouse, >=0 touch id
                    }
                }

                // If primed, check movement threshold to actually start dragging
                if (pressPrimed)
                {
                    if (IsPointerDown(primedTouchId))
                    {
                        if (TryGetPointerPosition(primedTouchId, out Vector2 curScreen))
                        {
                            float moved = (curScreen - primedPressScreenPos).magnitude;
                            bool applyThreshold = (!thresholdOnlyOnTouch) || (primedTouchId >= 0);
                            if (!applyThreshold || moved >= dragStartThresholdPixels)
                            {
                                // Start drag now
                                Vector3 world = ScreenToWorld(curScreen);
                                StartDrag(world);
                                // Play move SFX on grab begin
                                var mgr = ShapeSpriteManager.Instance != null ? ShapeSpriteManager.Instance : Object.FindFirstObjectByType<ShapeSpriteManager>();
                                if (mgr != null)
                                {
                                    var theme = mgr.GetShapeTheme(gameObject);
                                    mgr.PlayMoveAt(transform.position, theme);
                                }
                                activeTouchId = primedTouchId;
                                isTouchDrag = activeTouchId >= 0;
                                if (boostSortingOrderOnDrag) BoostSortingOrder();
                                // Switch to placed scale if requested
                                preDragScale = transform.localScale;
                                if (scaleToPlacedOnDrag)
                                {
                                    transform.localScale = placedScale;
                                }
                                // Initial pop for thresholded start
                                Vector2 initialScreen2 = curScreen;
                                if (alignBottomToPointer)
                                {
                                    initialScreen2.y += uniformLiftPixels;
                                }
                                else if (liftOnDrag && (!liftOnlyOnTouch || isTouchDrag))
                                {
                                    float lift2 = dragLiftScreenPixels;
                                    if (autoLiftByBounds)
                                    {
                                        lift2 = Mathf.Max(dragLiftScreenPixels, ComputeAutoLiftPixels());
                                    }
                                    initialScreen2.y += lift2;
                                }
                                if (addExtraLiftOnPress)
                                {
                                    initialScreen2.y += pressExtraLiftPixels;
                                }
                                UpdateDrag(ScreenToWorld(initialScreen2));
                                // Create initial preview
                                UpdatePlacementPreview();
                                lastDragScreenPos = curScreen;
                                dragStartScreenPos = curScreen;
                                pressPrimed = false;
                            }
                        }
                    }
                    else if (WasPointerReleasedThisFrame(primedTouchId))
                    {
                        // Tap without moving enough: cancel
                        pressPrimed = false;
                        primedTouchId = -1;
                    }
                }
            }
            else // dragging
            {
                if (IsPointerDown(activeTouchId))
                {
                    if (TryGetPointerPosition(activeTouchId, out Vector2 screenPos))
                    {
                        var rawScreen = screenPos;

                        // Optional pointer lead boost (in screen space)
                        if (enablePointerSpeedBoost)
                        {
                            if (useCumulativeBoost)
                            {
                                // Lead based on displacement from drag start (consistent "further than finger")
                                Vector2 disp = rawScreen - dragStartScreenPos;
                                float gain = Mathf.Max(1f, displacementBoost);
                                Vector2 boosted = dragStartScreenPos + disp * gain;
                                Vector2 extra = boosted - rawScreen;
                                if (maxCumulativeLeadPixels > 0f)
                                {
                                    float mag = extra.magnitude;
                                    if (mag > maxCumulativeLeadPixels) extra *= maxCumulativeLeadPixels / Mathf.Max(0.0001f, mag);
                                }
                                screenPos += extra;
                            }
                            else
                            {
                                // Lead based on recent velocity
                                Vector2 delta = rawScreen - lastDragScreenPos;
                                float boostFactor = Mathf.Max(1f, pointerSpeedBoost);
                                Vector2 extra = delta * (boostFactor - 1f);
                                if (maxLeadPixels > 0f)
                                {
                                    float mag = extra.magnitude;
                                    if (mag > maxLeadPixels) extra *= maxLeadPixels / Mathf.Max(0.0001f, mag);
                                }
                                screenPos += extra;
                            }
                        }

                        // Lift/align
                        if (alignBottomToPointer)
                        {
                            screenPos.y += uniformLiftPixels;
                        }
                        else if (liftOnDrag && (!liftOnlyOnTouch || isTouchDrag))
                        {
                            float lift = dragLiftScreenPixels;
                            if (autoLiftByBounds)
                            {
                                lift = Mathf.Max(dragLiftScreenPixels, ComputeAutoLiftPixels());
                            }
                            screenPos.y += lift;
                        }
                        UpdateDrag(ScreenToWorld(screenPos));

                        // Update last raw pointer screen position for next frame's boost
                        lastDragScreenPos = rawScreen;

                        // Update placement preview at snapped grid position
                        UpdatePlacementPreview();
                    }
                }
                else if (WasPointerReleasedThisFrame(activeTouchId))
                {
                    EndDrag();
                    activeTouchId = -1;
                    isTouchDrag = false;
                }
            }
        }
    }
}
