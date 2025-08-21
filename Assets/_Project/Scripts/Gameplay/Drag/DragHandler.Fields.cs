using UnityEngine;
using ColorBlast.Game;

namespace Gameplay
{
    /// <summary>
    /// DragHandler fields and configuration
    /// </summary>
    [RequireComponent(typeof(Shape))]
    public partial class DragHandler : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private bool returnToSpawnOnInvalidPlacement = true;
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private bool useReturnAnimation = true;
        [SerializeField] private bool showInvalidPlacementFeedback = true;

        [Header("Smoothing")]
        [Tooltip("If true, uses SmoothDamp to move toward the pointer for a softer feel.")]
        [SerializeField] private bool smoothDrag = true;
        [SerializeField, Min(0f)] private float dragSmoothTime = 0.03f;
        [SerializeField, Min(0f)] private float dragMaxSpeed = 150f;

        [Header("Drag Gating")]
        [Tooltip("Block dragging for a short time right after spawn (e.g., while pop-in animation plays).")]
        [SerializeField] private float dragLockDurationOnSpawn = 0.1f;
        [Tooltip("Require the pointer to move at least this many screen pixels before starting a drag.")]
        [SerializeField] private float dragStartThresholdPixels = 5f;
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
        [SerializeField] private float dragLiftScreenPixels = 160f;
        [Tooltip("Apply the lift only for touch drags (mobile/simulator). If false, applies to mouse too.")]
        [SerializeField] private bool liftOnlyOnTouch = true;
        [Tooltip("Automatically compute a lift amount from the shape bounds in screen pixels.")]
        [SerializeField] private bool autoLiftByBounds = true;
        [Range(0.25f, 2f)]
        [SerializeField] private float autoLiftMultiplier = 0.9f;
        [SerializeField] private float extraLiftPixels = 32f;

        [Header("Press Lift")]
        [Tooltip("Add extra lift in screen pixels on the first frame when the shape is pressed and drag begins.")]
        [SerializeField] private bool addExtraLiftOnPress = true;
        [SerializeField] private float pressExtraLiftPixels = 180f;

        [Header("Uniform Alignment")]
        [Tooltip("If true, keeps the bottom of the shape at a fixed number of pixels above the finger regardless of shape size.")]
        [SerializeField] private bool alignBottomToPointer = true;
        [Tooltip("How many screen pixels above the finger the bottom edge of the shape should sit while dragging.")]
        [SerializeField] private float uniformLiftPixels = 200f;

        [Header("Pointer Boost")]
        [Tooltip("If true, the dragged shape leads the finger based on recent motion to help players move faster.")]
        [SerializeField] private bool enablePointerSpeedBoost = true;
        [Tooltip("Multiplier for how much the shape should lead the finger. 1 = no boost.")]
        [Range(1f, 2.5f)]
        [SerializeField] private float pointerSpeedBoost = 1.25f;
        [Tooltip("Maximum extra lead distance in screen pixels to prevent overshooting.")]
        [SerializeField] private float maxLeadPixels = 90f;
        [Tooltip("If true, use displacement-based boost so the shape is consistently further than the finger from drag start.")]
        [SerializeField] private bool useCumulativeBoost = true;
        [Tooltip("Displacement multiplier from the drag start position. 1 = follow finger, >1 = shape further than finger.")]
        [Range(1f, 3f)]
        [SerializeField] private float displacementBoost = 1.7f;
        [Tooltip("Clamp for cumulative lead (in screen pixels). Larger allows further lead.")]
        [SerializeField] private float maxCumulativeLeadPixels = 160f;
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

        // Runtime state
        private Shape shape;
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
        private Vector2 lastDragScreenPos;
        private Vector2 dragStartScreenPos;

        // Preview state
        private GameObject previewRoot;
        private SpriteRenderer[] previewRenderers;
    }
}
