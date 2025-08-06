using UnityEngine;
using UnityEditor;
using Gameplay;

namespace ColorBlast.Editor
{
    /// <summary>
    /// Simple, reliable editor for the new GridManager
    /// </summary>
    [CustomEditor(typeof(GridManager))]
    public class GridManagerNewEditor : UnityEditor.Editor
    {
        private GridManager gridManager;
        
        void OnEnable()
        {
            gridManager = (GridManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // Grid info
            EditorGUILayout.LabelField("Grid Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Cells: {gridManager.GridWidth}x{gridManager.GridHeight} = {gridManager.GridWidth * gridManager.GridHeight}");
            
            // Calculate total grid size including spacing
            float totalWidth = gridManager.GridWidth * gridManager.CellWidth + (gridManager.GridWidth - 1) * gridManager.CellSpacingX;
            float totalHeight = gridManager.GridHeight * gridManager.CellHeight + (gridManager.GridHeight - 1) * gridManager.CellSpacingY;
            EditorGUILayout.LabelField($"Total Grid Size: {totalWidth:F1} x {totalHeight:F1} units (including spacing)");
            
            if (gridManager.UseUniformCellSize)
            {
                EditorGUILayout.LabelField($"Cell Size: {gridManager.CellSize:F1} (uniform)");
            }
            else
            {
                EditorGUILayout.LabelField($"Cell Size: {gridManager.CellWidth:F1} x {gridManager.CellHeight:F1} (W x H)");
            }
            
            if (gridManager.UseUniformSpacing)
            {
                EditorGUILayout.LabelField($"Cell Spacing: {gridManager.CellSpacing:F1} (uniform)");
            }
            else
            {
                EditorGUILayout.LabelField($"Cell Spacing: {gridManager.CellSpacingX:F1} x {gridManager.CellSpacingY:F1} (X x Y)");
            }
            
            EditorGUILayout.Space();
            
            // Visual indicators
            EditorGUILayout.HelpBox(
                "Preview Colors:\n" +
                "• Blue wireframes = Grid cells\n" +
                "• Yellow box = Grid boundary\n" +
                "• Red cube = Grid center/origin", 
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            // Buttons
            if (GUILayout.Button("Refresh Grid Preview"))
            {
                SceneView.RepaintAll();
            }
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                // Cell size controls
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cell Size:", GUILayout.Width(70));
                float newCellSize = EditorGUILayout.FloatField(gridManager.CellSize);
                if (newCellSize != gridManager.CellSize && newCellSize > 0)
                {
                    gridManager.SetCellSize(newCellSize);
                }
                EditorGUILayout.EndHorizontal();
                
                // Cell spacing controls
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cell Spacing:", GUILayout.Width(70));
                float newSpacing = EditorGUILayout.FloatField(gridManager.CellSpacing);
                if (newSpacing != gridManager.CellSpacing && newSpacing >= 0)
                {
                    gridManager.SetCellSpacing(newSpacing);
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Refresh Visual Grid"))
                {
                    gridManager.RefreshVisualGrid();
                }
                
                if (GUILayout.Button("Clear All Occupied Cells"))
                {
                    gridManager.ClearAllOccupiedCells();
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Validate Grid"))
                {
                    bool isValid = gridManager.ValidateGridPositioning();
                    EditorUtility.DisplayDialog("Grid Validation", 
                        isValid ? "Grid is working correctly!" : "Grid has issues - check console for details", 
                        "OK");
                }
            }
            
            // Force repaint when values change
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }
    }
}
