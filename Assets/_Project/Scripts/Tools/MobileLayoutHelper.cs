using UnityEngine;
using Gameplay;
using ColorBlast.Game;

namespace ColorBlast.Tools
{
    /// <summary>
    /// Helper tool for quickly setting up mobile-friendly grid and spawn positioning
    /// Add this to any GameObject and run in Play mode or use the context menu
    /// </summary>
    public class MobileLayoutHelper : MonoBehaviour
    {
        [Header("Mobile Layout Settings")]
        [SerializeField] private float gridSize = 0.8f;
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;
        [SerializeField] private Vector3 gridPosition = new Vector3(0, -3, 0);
        
        [Header("Spawn Point Settings")]
        [SerializeField] private Vector3 leftSpawnPoint = new Vector3(-2.5f, 4, 0);
        [SerializeField] private Vector3 centerSpawnPoint = new Vector3(0, 4, 0);
        [SerializeField] private Vector3 rightSpawnPoint = new Vector3(2.5f, 4, 0);
        
        [Header("Camera Settings")]
        [SerializeField] private float cameraSize = 10f;
        
        [ContextMenu("Setup Mobile Layout")]
        public void SetupMobileLayout()
        {
            SetupGrid();
            SetupSpawnPoints();
            SetupCamera();
            UpdateShapeScales();
            
            Debug.Log("Mobile layout setup complete!");
        }
        
        private void SetupGrid()
        {
            var gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                gridManager.transform.position = gridPosition;
                
                // Note: Grid size, width, and height need to be set manually in inspector
                Debug.Log($"📐 GridManager positioned at {gridPosition}");
                Debug.Log($"⚠️ Remember to set Grid Size: {gridSize}, Grid Width: {gridWidth}, Grid Height: {gridHeight} in GridManager inspector");
            }
            else
            {
                Debug.LogWarning("❌ GridManager not found! Create one first.");
            }
        }
        
        private void SetupSpawnPoints()
        {
            var spawnPoints = FindSpawnPoints();
            
            if (spawnPoints.Length >= 3)
            {
                spawnPoints[0].transform.position = leftSpawnPoint;
                spawnPoints[1].transform.position = centerSpawnPoint;
                spawnPoints[2].transform.position = rightSpawnPoint;
                
                Debug.Log("📍 Spawn points positioned for mobile layout");
            }
            else
            {
                Debug.LogWarning($"❌ Found {spawnPoints.Length} spawn points. Need 3 spawn points named SpawnPoint_1, SpawnPoint_2, SpawnPoint_3");
            }
        }
        
        private void SetupCamera()
        {
            var camera = Camera.main;
            if (camera != null && camera.orthographic)
            {
                camera.orthographicSize = cameraSize;
                Debug.Log($"📷 Camera size set to {cameraSize}");
            }
            else
            {
                Debug.LogWarning("❌ Main camera not found or not orthographic");
            }
        }
        
        private void UpdateShapeScales()
        {
            // Find all shape prefabs in the project
            var shapePrefabs = Resources.FindObjectsOfTypeAll<Shape>();
            int updated = 0;
            
            foreach (var shape in shapePrefabs)
            {
                if (shape.gameObject.scene.name == null) // This means it's a prefab
                {
                    Debug.Log($"⚠️ Shape prefab '{shape.name}' found - manually set Grid Size to {gridSize} in inspector");
                    updated++;
                }
            }
            
            if (updated > 0)
            {
                Debug.Log($"📦 Found {updated} shape prefabs. Remember to update their Grid Size to {gridSize}");
            }
        }
        
        private Transform[] FindSpawnPoints()
        {
            // Support both naming conventions: SpawnPoint_1..3 and SpawnPoint1..3
            var spawnPoint1 = (GameObject.Find("SpawnPoint_1") ?? GameObject.Find("SpawnPoint1"))?.transform;
            var spawnPoint2 = (GameObject.Find("SpawnPoint_2") ?? GameObject.Find("SpawnPoint2"))?.transform;
            var spawnPoint3 = (GameObject.Find("SpawnPoint_3") ?? GameObject.Find("SpawnPoint3"))?.transform;
            
            var points = new System.Collections.Generic.List<Transform>();
            if (spawnPoint1 != null) points.Add(spawnPoint1);
            if (spawnPoint2 != null) points.Add(spawnPoint2);
            if (spawnPoint3 != null) points.Add(spawnPoint3);
            
            return points.ToArray();
        }
        
        [ContextMenu("Show Current Layout Info")]
        public void ShowCurrentLayoutInfo()
        {
            Debug.Log("=== Current Layout Info ===");
            
            var gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                Debug.Log($"📐 GridManager Position: {gridManager.transform.position}");
            }
            
            var spawnPoints = FindSpawnPoints();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Debug.Log($"📍 SpawnPoint_{i + 1}: {spawnPoints[i].position}");
            }
            
            var camera = Camera.main;
            if (camera != null)
            {
                Debug.Log($"📷 Camera Size: {camera.orthographicSize}");
            }
        }
    }
}
