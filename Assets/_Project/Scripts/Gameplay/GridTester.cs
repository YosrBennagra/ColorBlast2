using UnityEngine;
using ColorBlast.Core.Architecture;

namespace Gameplay
{
    /// <summary>
    /// Simple test script to validate grid functionality
    /// </summary>
    public class GridTester : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logResults = true;

        private void Start()
        {
            if (runTestOnStart)
            {
                TestGrid();
            }
        }

        [ContextMenu("Test Grid")]
        public void TestGrid()
        {
            var gridManager = Services.Get<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridTester: No GridManager found in services!");
                return;
            }

            bool allTestsPassed = true;

            // Test 1: Basic grid properties
            if (gridManager.GridWidth <= 0 || gridManager.GridHeight <= 0)
            {
                Debug.LogError("GridTester: Invalid grid dimensions");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log($"GridTester: Grid size {gridManager.GridWidth}x{gridManager.GridHeight} ✓");
            }

            // Test 2: Cell size validation
            if (gridManager.CellSize <= 0)
            {
                Debug.LogError("GridTester: Invalid cell size");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log($"GridTester: Cell size {gridManager.CellSize} ✓");
            }

            // Test 3: Coordinate conversion
            Vector2Int testPos = new Vector2Int(0, 0);
            Vector3 worldPos = gridManager.GridToWorldPosition(testPos);
            Vector2Int convertedBack = gridManager.WorldToGridPosition(worldPos);

            if (testPos != convertedBack)
            {
                Debug.LogError($"GridTester: Coordinate conversion failed! {testPos} -> {worldPos} -> {convertedBack}");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log($"GridTester: Coordinate conversion {testPos} -> {worldPos} -> {convertedBack} ✓");
            }

            // Test 4: Cell placement
            Vector2Int centerPos = new Vector2Int(gridManager.GridWidth / 2, gridManager.GridHeight / 2);
            
            if (!gridManager.IsValidGridPosition(centerPos))
            {
                Debug.LogError($"GridTester: Center position {centerPos} is not valid");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log($"GridTester: Center position {centerPos} is valid ✓");
            }

            // Test 5: Occupy/Free cell
            if (gridManager.IsCellOccupied(centerPos))
            {
                gridManager.FreeCell(centerPos);
            }
            
            gridManager.OccupyCell(centerPos);
            if (!gridManager.IsCellOccupied(centerPos))
            {
                Debug.LogError("GridTester: OccupyCell failed");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log("GridTester: OccupyCell works ✓");
            }

            gridManager.FreeCell(centerPos);
            if (gridManager.IsCellOccupied(centerPos))
            {
                Debug.LogError("GridTester: FreeCell failed");
                allTestsPassed = false;
            }
            else if (logResults)
            {
                Debug.Log("GridTester: FreeCell works ✓");
            }

            // Final result
            if (allTestsPassed)
            {
                Debug.Log("🎉 GridTester: ALL TESTS PASSED - Grid is working correctly!");
            }
            else
            {
                Debug.LogError("❌ GridTester: SOME TESTS FAILED - Check grid configuration!");
            }
        }
    }
}
