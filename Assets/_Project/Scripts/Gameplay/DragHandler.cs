using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using ColorBlast.Core.Architecture;
using UnityEngine.EventSystems;

namespace Gameplay
{
    /// <summary>
    /// Handles dragging mechanics for shapes (mouse + touch)
    /// </summary>
    [RequireComponent(typeof(Core.Shape))]
    public class DragHandler : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private bool returnToSpawnOnInvalidPlacement = true;
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private bool useReturnAnimation = true;
        [SerializeField] private bool showInvalidPlacementFeedback = true;

        [Header("Input")]
        [Tooltip("Ignore pointer/touch when over UI elements.")]
        [SerializeField] private bool ignoreUI = true;

        private Core.Shape shape;
        private Camera cam;
        private Vector3 offset;
        private bool isDragging = false;
        private int activeTouchId = -1;
        
        private void Start()
        {
            shape = GetComponent<Core.Shape>();
            cam = Camera.main;
        }
        
        private void Update()
        {
            if (shape.IsPlaced) return;
            if (cam == null) cam = Camera.main;
            
            // Allow starting drag even if services not fully ready; placement will validate at drop.
            if (!isDragging)
            {
                if (WasPointerPressedThisFrame(out Vector2 screenPos, out int touchId))
                {
                    if (ignoreUI && IsOverUI(touchId)) return;
                    TryStartDrag(ScreenToWorld(screenPos));
                    activeTouchId = touchId; // -1 for mouse, or touch id for touch
                }
            }
            
            if (isDragging)
            {
                if (IsPointerDown(activeTouchId))
                {
                    if (TryGetPointerPosition(activeTouchId, out Vector2 screenPos))
                    {
                        UpdateDrag(ScreenToWorld(screenPos));
                    }
                }
                else if (WasPointerReleasedThisFrame(activeTouchId))
                {
                    EndDrag();
                    activeTouchId = -1;
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
        
        private bool AreServicesReady()
        {
            return Services.Has<PlacementSystem>() && Core.GameManager.Instance != null && Core.GameManager.Instance.IsInitialized();
        }
        
        private void TryStartDrag(Vector3 pointerWorld)
        {
            if (isDragging || shape.IsPlaced) return;
            
            var bounds = GetBounds();
            if (bounds.size != Vector3.zero && bounds.Contains(new Vector3(pointerWorld.x, pointerWorld.y, transform.position.z)))
            {
                StartDrag(pointerWorld);
            }
        }
        
        private void StartDrag(Vector3 pointerWorld)
        {
            isDragging = true;
            pointerWorld.z = transform.position.z;
            offset = transform.position - pointerWorld;
        }
        
        private void UpdateDrag(Vector3 pointerWorld)
        {
            pointerWorld.z = transform.position.z;
            transform.position = pointerWorld + offset;
        }
        
        private void EndDrag()
        {
            isDragging = false;
            
            // Check if services are available before using them
            if (!Services.Has<PlacementSystem>())
            {
                ReturnToSpawn();
                return;
            }
            
            var placementSystem = Services.Get<PlacementSystem>();
            if (placementSystem != null)
            {
                if (!placementSystem.TryPlaceShape(shape))
                {
                    if (showInvalidPlacementFeedback)
                    {
                        StartCoroutine(ShowInvalidFeedback());
                    }
                    
                    if (returnToSpawnOnInvalidPlacement)
                    {
                        ReturnToSpawn();
                    }
                }
            }
        }
        
        private void ReturnToSpawn()
        {
            if (useReturnAnimation)
            {
                StartCoroutine(ReturnToSpawnCoroutine());
            }
            else
            {
                transform.position = shape.OriginalSpawnPosition;
            }
        }
        
        private IEnumerator ReturnToSpawnCoroutine()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = shape.OriginalSpawnPosition;
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            
            while (elapsed < returnAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnAnimationDuration;
                t = 1f - (1f - t) * (1f - t); // Ease-out
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                
                float scaleEffect = 1f + (0.1f * Mathf.Sin(t * Mathf.PI));
                transform.localScale = originalScale * scaleEffect;
                
                yield return null;
            }
            
            transform.position = targetPos;
            transform.localScale = originalScale;
        }
        
        private IEnumerator ShowInvalidFeedback()
        {
            var renderers = shape.TileRenderers;
            Color[] originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalColors[i] = renderers[i].color;
            }
            
            // Flash red
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = Color.red;
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // Restore colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = originalColors[i];
            }
        }
        
        private Bounds GetBounds()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;
            
            var col2D = GetComponent<Collider2D>();
            if (col2D != null) return col2D.bounds;
            
            var col3D = GetComponent<Collider>();
            if (col3D != null) return col3D.bounds;
            
            return new Bounds();
        }
    }
}
