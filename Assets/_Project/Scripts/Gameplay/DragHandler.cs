using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Handles dragging mechanics for shapes
    /// </summary>
    [RequireComponent(typeof(Core.Shape))]
    public class DragHandler : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private bool returnToSpawnOnInvalidPlacement = true;
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private bool useReturnAnimation = true;
        [SerializeField] private bool showInvalidPlacementFeedback = true;
        
        private Core.Shape shape;
        private Camera cam;
        private Vector3 offset;
        private bool isDragging = false;
        
        private void Start()
        {
            shape = GetComponent<Core.Shape>();
            cam = Camera.main;
        }
        
        private void Update()
        {
            if (shape.IsPlaced) return;
            
            // Don't allow dragging until services are initialized
            if (!AreServicesReady())
                return;
            
            var leftButton = Mouse.current.leftButton;
            
            if (leftButton.wasPressedThisFrame)
            {
                TryStartDrag();
            }
            
            if (isDragging)
            {
                if (leftButton.isPressed)
                {
                    UpdateDrag();
                }
                else
                {
                    EndDrag();
                }
            }
        }
        
        private bool AreServicesReady()
        {
            return Services.Has<PlacementSystem>() && Core.GameManager.Instance != null && Core.GameManager.Instance.IsInitialized();
        }
        
        private void TryStartDrag()
        {
            if (isDragging || shape.IsPlaced) return;
            
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
            
            var bounds = GetBounds();
            if (bounds.size != Vector3.zero && bounds.Contains(new Vector3(mouseWorldPos.x, mouseWorldPos.y, transform.position.z)))
            {
                StartDrag(mouseWorldPos);
            }
        }
        
        private void StartDrag(Vector3 mouseWorldPos)
        {
            isDragging = true;
            mouseWorldPos.z = transform.position.z;
            offset = transform.position - mouseWorldPos;
        }
        
        private void UpdateDrag()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
            mouseWorldPos.z = transform.position.z;
            transform.position = mouseWorldPos + offset;
        }
        
        private void EndDrag()
        {
            isDragging = false;
            
            // Check if services are available before using them
            if (!Services.Has<PlacementSystem>())
            {
                Debug.LogWarning("PlacementSystem not found! Make sure GameManager is in the scene and has initialized.");
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
