using UnityEngine;

public class SpawnPointVisualizer : MonoBehaviour
{
    [Header("Visualization")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoSize = 1f;
    [SerializeField] private bool showInGame = false;
    
    void OnDrawGizmos()
    {
        // Draw wireframe cube to show spawn point location
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * gizmoSize);
        
        // Draw a small solid cube at the center
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Gizmos.DrawCube(transform.position, Vector3.one * (gizmoSize * 0.2f));
    }
    
    void OnDrawGizmosSelected()
    {
        // Highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * (gizmoSize * 1.2f));
    }
    
    void Start()
    {
        if (!showInGame)
        {
            // Remove this component at runtime to avoid unnecessary overhead
            Destroy(this);
        }
    }
}
