using UnityEngine;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Debug tool for testing grid position conversions
    /// </summary>
    public class GridPositionDebugger : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private bool enableDebugGizmos = true;
        [SerializeField] private bool logPositionConversions = true;
        [SerializeField] private Color debugColor = Color.red;
        
        [Header("Test Position")]
        [SerializeField] private Vector2Int testGridPosition = new Vector2Int(0, 0);
        [SerializeField] private Vector3 testWorldPosition = Vector3.zero;
        
        private GridManager gridManager;
        
        private void Start()
        {
            if (Services.Has<GridManager>())
            {
                gridManager = Services.Get<GridManager>();
            }
        }
        
        private void Update()
        {
            if (gridManager == null && Services.Has<GridManager>())
            {
                gridManager = Services.Get<GridManager>();
            }
            
            if (gridManager == null) return;
            
            if (logPositionConversions)
            {
                TestPositionConversions();
            }
        }
        
        private void TestPositionConversions()
        {
            // Test grid to world conversion
            Vector3 worldPos = gridManager.GridToWorldPosition(testGridPosition);
            Vector2Int convertedBack = gridManager.WorldToGridPosition(worldPos);
            
            if (convertedBack != testGridPosition)
            {
                Debug.LogWarning($"⚠️ Position conversion mismatch! " +
                    $"Original: {testGridPosition}, Converted back: {convertedBack}, " +
                    $"World position: {worldPos}");
            }
            
            // Test world to grid conversion
            Vector2Int gridPos = gridManager.WorldToGridPosition(testWorldPosition);
            Vector3 convertedBackWorld = gridManager.GridToWorldPosition(gridPos);
            
            Debug.Log($"🔍 Grid Position Debug:\n" +
                $"Test Grid Position {testGridPosition} → World Position {worldPos} → Back to Grid {convertedBack}\n" +
                $"Test World Position {testWorldPosition} → Grid Position {gridPos} → Back to World {convertedBackWorld}\n" +
                $"Grid Start Position: {gridManager.transform.position}\n" +
                $"Cell Size: {gridManager.CellWidth}x{gridManager.CellHeight}\n" +
                $"Cell Spacing: {gridManager.CellSpacingX}x{gridManager.CellSpacingY}");
        }
        
        private void OnDrawGizmos()
        {
            if (!enableDebugGizmos || gridManager == null) return;
            
            Gizmos.color = debugColor;
            
            // Draw test grid position
            Vector3 worldPos = gridManager.GridToWorldPosition(testGridPosition);
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.2f);
            
            // Draw test world position
            Gizmos.color = Color.blue;
            Vector2Int gridPos = gridManager.WorldToGridPosition(testWorldPosition);
            Vector3 snappedWorld = gridManager.GridToWorldPosition(gridPos);
            Gizmos.DrawWireCube(snappedWorld, Vector3.one * 0.15f);
            
            // Draw grid bounds
            Gizmos.color = Color.yellow;
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    Vector3 cellCenter = gridManager.GridToWorldPosition(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(cellCenter, new Vector3(gridManager.CellWidth, gridManager.CellHeight, 0.1f));
                }
            }
        }
        
        [ContextMenu("Test Grid Positioning")]
        public void TestGridPositioning()
        {
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found!");
                return;
            }
            
            Debug.Log("🧪 Testing Grid Positioning...");
            
            // Test corners
            Vector2Int[] testPositions = {
                new Vector2Int(0, 0), // Bottom-left
                new Vector2Int(gridManager.GridWidth - 1, 0), // Bottom-right
                new Vector2Int(0, gridManager.GridHeight - 1), // Top-left
                new Vector2Int(gridManager.GridWidth - 1, gridManager.GridHeight - 1) // Top-right
            };
            
            foreach (var pos in testPositions)
            {
                Vector3 worldPos = gridManager.GridToWorldPosition(pos);
                Vector2Int convertedBack = gridManager.WorldToGridPosition(worldPos);
                bool isValid = gridManager.IsValidGridPosition(pos);
                
                Debug.Log($"Grid {pos} → World {worldPos} → Grid {convertedBack} (Valid: {isValid})");
            }
        }
    }
}
