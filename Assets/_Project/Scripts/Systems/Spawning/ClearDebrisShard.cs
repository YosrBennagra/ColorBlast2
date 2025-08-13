using UnityEngine;

/// <summary>
/// Lightweight sprite shard motion and fade, used for clear FX without ParticleSystem.
/// </summary>
public class ClearDebrisShard : MonoBehaviour
{
    private Vector2 velocity;
    private float angularVelocity;
    private float lifetime;
    private float gravity;
    private float age;
    private SpriteRenderer sr;
    private Color startColor;

    public void Initialize(Vector2 velocity, float angularVel, float lifetime, float gravity)
    {
        this.velocity = velocity;
        this.angularVelocity = angularVel;
        this.lifetime = Mathf.Max(0.05f, lifetime);
        this.gravity = gravity;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color; else startColor = Color.white;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        // Integrate motion (simple)
        velocity.y += gravity * dt;
        transform.position += new Vector3(velocity.x, velocity.y, 0f) * dt;
        transform.Rotate(0f, 0f, angularVelocity * dt);

        // Fade out
        float t = Mathf.Clamp01(age / lifetime);
        float alpha = 1f - t;
        if (sr != null)
        {
            var c = startColor; c.a *= alpha; sr.color = c;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
