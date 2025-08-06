using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpawnLayoutSettings
{
    [Header("Layout Configuration")]
    public float spacing = 2f;
    public float spawnPointSize = 2f;
    public TextAnchor childAlignment = TextAnchor.MiddleCenter;
    
    [Header("Visual Feedback")]
    public bool showSpawnAreas = true;
    public Color spawnAreaColor = Color.green;
}

public class SpawnAreaManager : MonoBehaviour
{
    [SerializeField] private SpawnLayoutSettings layoutSettings = new SpawnLayoutSettings();
    [SerializeField] private Transform[] spawnPoints = new Transform[3];
    
    private HorizontalLayoutGroup layoutGroup;
    
    void Start()
    {
        SetupLayoutGroup();
        SetupSpawnPoints();
    }
    
    void SetupLayoutGroup()
    {
        // Add Horizontal Layout Group if not present
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        
        // Configure layout group
        layoutGroup.spacing = layoutSettings.spacing;
        layoutGroup.childAlignment = layoutSettings.childAlignment;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
    }
    
    void SetupSpawnPoints()
    {
        // Create spawn points if they don't exist
        for (int i = 0; i < 3; i++)
        {
            string spawnPointName = $"SpawnPoint{i + 1}";
            Transform existingPoint = transform.Find(spawnPointName);
            
            if (existingPoint == null)
            {
                // Create new spawn point
                GameObject newSpawnPoint = new GameObject(spawnPointName);
                newSpawnPoint.transform.SetParent(transform);
                existingPoint = newSpawnPoint.transform;
                
                // Add Layout Element
                LayoutElement layoutElement = newSpawnPoint.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = layoutSettings.spawnPointSize;
                layoutElement.preferredHeight = layoutSettings.spawnPointSize;
                
                // Add visualizer for editor
                if (layoutSettings.showSpawnAreas)
                {
                    SpawnPointVisualizer visualizer = newSpawnPoint.AddComponent<SpawnPointVisualizer>();
                    // Set visualizer color if the script exists
                }
            }
            
            spawnPoints[i] = existingPoint;
        }
    }
    
    // Get spawn points for ShapeSpawner
    public Transform[] GetSpawnPoints()
    {
        return spawnPoints;
    }
    
    // Auto-assign to ShapeSpawner
    [ContextMenu("Auto-Assign to ShapeSpawner")]
    public void AutoAssignToShapeSpawner()
    {
        ShapeSpawner spawner = FindFirstObjectByType<ShapeSpawner>();
        if (spawner != null)
        {
            spawner.SetSpawnPoints(spawnPoints);
            Debug.Log("Spawn points automatically assigned to ShapeSpawner!");
        }
        else
        {
            Debug.LogWarning("No ShapeSpawner found in scene!");
        }
    }
    
    // Refresh layout when settings change
    void OnValidate()
    {
        if (Application.isPlaying && layoutGroup != null)
        {
            layoutGroup.spacing = layoutSettings.spacing;
            layoutGroup.childAlignment = layoutSettings.childAlignment;
            
            // Update layout elements
            for (int i = 0; i < transform.childCount; i++)
            {
                LayoutElement element = transform.GetChild(i).GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredWidth = layoutSettings.spawnPointSize;
                    element.preferredHeight = layoutSettings.spawnPointSize;
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (layoutSettings.showSpawnAreas)
        {
            Gizmos.color = layoutSettings.spawnAreaColor;
            
            // Draw container area
            Gizmos.DrawWireCube(transform.position, new Vector3(
                layoutSettings.spawnPointSize * 3 + layoutSettings.spacing * 2,
                layoutSettings.spawnPointSize,
                0.1f
            ));
        }
    }
}
