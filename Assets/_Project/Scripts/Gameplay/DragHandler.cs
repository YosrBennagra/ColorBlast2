using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using ColorBlast.Core.Architecture;
using UnityEngine.EventSystems;

namespace Gameplay
{
    /// <summary>
    /// Handles dragging mechanics for shapes (mouse + touch), with optional lift and sorting boost.
    /// </summary>
    [RequireComponent(typeof(Core.Shape))]
    public class DragHandler : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private bool returnToSpawnOnInvalidPlacement = true;
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private bool useReturnAnimation = true;
        [SerializeField] private bool showInvalidPlacementFeedback = true;

    [Header("Smoothing")]
    [Tooltip("If true, uses SmoothDamp to move toward the pointer for a softer feel.")]
    [SerializeField] private bool smoothDrag = true;
    [SerializeField, Min(0f)] private float dragSmoothTime = 0.06f;
    [SerializeField, Min(0f)] private float dragMaxSpeed = 100f;

    [Header("Drag Gating")]
    [Tooltip("Block dragging for a short time right after spawn (e.g., while pop-in animation plays).")]
    [SerializeField] private float dragLockDurationOnSpawn = 0.3f;
    [Tooltip("Require the pointer to move at least this many screen pixels before starting a drag.")]
    [SerializeField] private float dragStartThresholdPixels = 12f;
    [Tooltip("Apply the movement threshold only for touch drags. If false, applies to mouse too.")]
    [SerializeField] private bool thresholdOnlyOnTouch = true;
    [Tooltip("If true, begin dragging immediately on pointer down when pressing the shape (makes it pop up instantly).")]
    [SerializeField] private bool startDragOnPointerDown = true;

        [Header("Input")]
        [Tooltip("Ignore pointer/touch when over UI elements.")]
        [SerializeField] private bool ignoreUI = true;

        [Header("Drag Visibility")]
        [Tooltip("Lift the dragged shape above the finger by this many screen pixels.")]
        [SerializeField] private bool liftOnDrag = true;
        [SerializeField] private float dragLiftScreenPixels = 64f;
        [Tooltip("Apply the lift only for touch drags (mobile/simulator). If false, applies to mouse too.")]
        [SerializeField] private bool liftOnlyOnTouch = true;
    [Tooltip("Automatically compute a lift amount from the shape bounds in screen pixels.")]
    [SerializeField] private bool autoLiftByBounds = true;
    [Range(0.25f, 2f)]
    [SerializeField] private float autoLiftMultiplier = 0.9f;
    [SerializeField] private float extraLiftPixels = 32f;
        [Tooltip("Temporarily raise SpriteRenderer sorting order while dragging.")]
        [SerializeField] private bool boostSortingOrderOnDrag = true;
        [SerializeField] private int sortingOrderBoost = 200;

    [Header("Drag Sorting")]
    [Tooltip("Override sorting layer/order absolutely while dragging so the shape is always on top.")]
    [SerializeField] private bool useAbsoluteDragSorting = true;
    [Tooltip("Optional sorting layer name to use while dragging (leave empty to keep current layer).")]
    [SerializeField] private string dragSortingLayerName = "";
    [Tooltip("Sorting order to apply while dragging (very high keeps it above placed shapes).")]
    [SerializeField] private int dragSortingOrderAbsolute = 10000;

    [Header("Placement Size")]
    [Tooltip("When placement succeeds, set the shape back to this scale (original size by default).")]
    [SerializeField] private bool overrideScaleOnPlacement = true;
    [SerializeField] private Vector3 placedScale = Vector3.one;
    [Tooltip("When dragging starts, switch the shape to the placedScale (original size).")]
    [SerializeField] private bool scaleToPlacedOnDrag = true;

        private Core.Shape shape;
        private Camera cam;
        private Vector3 offset;
        private bool isDragging = false;
        private int activeTouchId = -1; // -1 for mouse
        private bool isTouchDrag = false;
    private SpriteRenderer[] cachedRenderers;
        private int[] originalSortingOrders;
    private int[] originalSortingLayerIDs;
    private Vector3 preDragScale;
    private float dragUnlockTime;
    private bool pressPrimed = false;
    private Vector2 primedPressScreenPos;
    private int primedTouchId = -1;
    private Vector3 dragVelocity = Vector3.zero;
        
    // Preview state
    private GameObject previewRoot;
    private SpriteRenderer[] previewRenderers;
        
        private void Start()
        {
            shape = GetComponent<Core.Shape>();
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
                        activeTouchId = pressId;
                        isTouchDrag = activeTouchId >= 0;
                        if (boostSortingOrderOnDrag) BoostSortingOrder();
                        preDragScale = transform.localScale;
                        if (scaleToPlacedOnDrag)
                        {
                            transform.localScale = placedScale;
                        }
                        // Initial pop this frame using lift even before movement
                        Vector2 initialScreen = pressScreen;
                        if (liftOnDrag && (!liftOnlyOnTouch || isTouchDrag))
                        {
                            float lift = dragLiftScreenPixels;
                            if (autoLiftByBounds)
                            {
                                lift = Mathf.Max(dragLiftScreenPixels, ComputeAutoLiftPixels());
                            }
                            initialScreen.y += lift;
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
                                activeTouchId = primedTouchId;
                                isTouchDrag = activeTouchId >= 0;
                                if (boostSortingOrderOnDrag) BoostSortingOrder();
                                // Switch to placed scale if requested
                                preDragScale = transform.localScale;
                                if (scaleToPlacedOnDrag)
                                {
                                    transform.localScale = placedScale;
                                }
                                // Create initial preview
                                UpdatePlacementPreview();
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
                        if (liftOnDrag && (!liftOnlyOnTouch || isTouchDrag))
                        {
                            float lift = dragLiftScreenPixels;
                            if (autoLiftByBounds)
                            {
                                lift = Mathf.Max(dragLiftScreenPixels, ComputeAutoLiftPixels());
                            }
                            screenPos.y += lift;
                        }
                        UpdateDrag(ScreenToWorld(screenPos));

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
        
        private Vector3 ScreenToWorld(Vector2 screen)
        {
            float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
            w.z = transform.position.z;
            return w;
        }

        private bool WasPointerPressedThisFrame(out Vector2 screenPos, out int touchId)
        {
            screenPos = default;
            touchId = -1; // mouse
            
            // Touch (mobile/simulator)
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    touchId = touch.touchId.ReadValue();
                    return true;
                }
                // Also allow any new touch
                foreach (var t in Touchscreen.current.touches)
                {
                    if (t.press.wasPressedThisFrame)
                    {
                        screenPos = t.position.ReadValue();
                        touchId = t.touchId.ReadValue();
                        return true;
                    }
                }
            }
            
            // Mouse
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
            
            return false;
        }

        private bool IsPointerDown(int touchId)
        {
            if (touchId >= 0 && Touchscreen.current != null)
            {
                foreach (var t in Touchscreen.current.touches)
                {
                    if (t.touchId.ReadValue() == touchId)
                        return t.press.isPressed;
                }
                return false;
            }
            
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        private bool WasPointerReleasedThisFrame(int touchId)
        {
            if (touchId >= 0 && Touchscreen.current != null)
            {
                foreach (var t in Touchscreen.current.touches)
                {
                    if (t.touchId.ReadValue() == touchId)
                        return t.press.wasReleasedThisFrame;
                }
                return true; // touch disappeared
            }
            
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        }

        private bool TryGetPointerPosition(int touchId, out Vector2 screenPos)
        {
            screenPos = default;
            if (touchId >= 0 && Touchscreen.current != null)
            {
                foreach (var t in Touchscreen.current.touches)
                {
                    if (t.touchId.ReadValue() == touchId)
                    {
                        screenPos = t.position.ReadValue();
                        return true;
                    }
                }
                return false;
            }
            
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
            return false;
        }

        private bool IsOverUI(int pointerId)
        {
            if (EventSystem.current == null) return false;
            if (pointerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(pointerId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }
        
        private void TryStartDrag(Vector3 pointerWorld)
        {
            if (isDragging || (shape != null && shape.IsPlaced)) return;
            
            var bounds = GetBounds();
            if (bounds.size != Vector3.zero && bounds.Contains(new Vector3(pointerWorld.x, pointerWorld.y, transform.position.z)))
            {
                StartDrag(pointerWorld);
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
            var target = pointerWorld + offset;
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
        
    // Removed red-flash feedback by request
        
        private Bounds GetBounds()
        {
            // Prefer combined bounds of child SpriteRenderers for accurate hit testing
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            if (srs != null && srs.Length > 0)
            {
                Bounds b = new Bounds(srs[0].bounds.center, Vector3.zero);
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i] == null) continue;
                    b.Encapsulate(srs[i].bounds);
                }
                return b;
            }

            var renderer = GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            var col2D = GetComponent<Collider2D>();
            if (col2D != null) return col2D.bounds;

            var col3D = GetComponent<Collider>();
            if (col3D != null) return col3D.bounds;

            return new Bounds();
        }

        private bool IsPointerOnShape(Vector2 screenPos)
        {
            Vector3 world = ScreenToWorld(screenPos);
            var bounds = GetBounds();
            if (bounds.size == Vector3.zero) return false;
            return bounds.Contains(new Vector3(world.x, world.y, bounds.center.z));
        }

        public void SetDragLock(float seconds)
        {
            dragUnlockTime = Mathf.Max(dragUnlockTime, Time.time + Mathf.Max(0f, seconds));
        }

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
