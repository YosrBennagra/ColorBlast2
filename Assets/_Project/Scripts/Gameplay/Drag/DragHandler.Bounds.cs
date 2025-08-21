using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Bounds, hit-testing and utility
    /// </summary>
    public partial class DragHandler
    {
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

            // Expand bounds for easier grabbing
            // Mobile gets even more generous hit area
            float expansion = 0.25f; // 25% larger by default
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (Input.touchCount > 0 || Application.isMobilePlatform)
            {
                expansion = 0.4f; // 40% larger for touch
            }
            #endif

            bounds.Expand(bounds.size.magnitude * expansion);

            return bounds.Contains(new Vector3(world.x, world.y, bounds.center.z));
        }

        public void SetDragLock(float seconds)
        {
            dragUnlockTime = Mathf.Max(dragUnlockTime, Time.time + Mathf.Max(0f, seconds));
        }
    }
}
