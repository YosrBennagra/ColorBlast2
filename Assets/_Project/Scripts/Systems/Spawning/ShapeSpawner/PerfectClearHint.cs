using UnityEngine;
using ColorBlast.Game;

/// <summary>
/// Subtle pulsing hint applied to a spawned shape that represents a perfect-clear opportunity.
/// It gently pulses the localScale while the shape is unplaced, then self-destructs when placed.
/// </summary>
public class PerfectClearHint : MonoBehaviour
{
    [SerializeField, Range(0f, 0.2f)] private float amplitude = 0.05f;
    [SerializeField, Range(0.1f, 10f)] private float speed = 3f;

    private Vector3 baseScale;
    private Shape shape;

    private void Awake()
    {
        baseScale = transform.localScale;
        shape = GetComponent<Shape>();
    }

    private void Update()
    {
        if (shape == null)
        {
            Destroy(this);
            return;
        }
        if (shape.IsPlaced)
        {
            // Restore base and remove hint after placement
            transform.localScale = baseScale;
            Destroy(this);
            return;
        }
        float s = 1f + amplitude * Mathf.Sin(Time.time * speed);
        transform.localScale = baseScale * s;
    }
}
