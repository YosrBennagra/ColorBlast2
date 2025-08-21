using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Gameplay
{
    /// <summary>
    /// DragHandler input helpers
    /// </summary>
    public partial class DragHandler
    {
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
    }
}
