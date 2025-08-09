using UnityEngine;
using UnityEditor;
using Gameplay;

namespace ColorBlast.Editor
{
    /// <summary>
    /// Minimal editor for the pixel-art GridManager
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
            EditorGUILayout.LabelField("Grid Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Cells: {gridManager.GridWidth} x {gridManager.GridHeight} = {gridManager.GridWidth * gridManager.GridHeight}");

            float totalWidth = gridManager.GridWidth * gridManager.CellSize;
            float totalHeight = gridManager.GridHeight * gridManager.CellSize;
            EditorGUILayout.LabelField($"Total Grid Size: {totalWidth:F3} x {totalHeight:F3} units");

            EditorGUILayout.LabelField($"Cell: {gridManager.CellSizePixels}px @ {gridManager.PixelsPerUnit} PPU  →  {gridManager.CellSize:F3} units");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Gizmos show the grid in the Scene view. No runtime objects are created.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            if (GUILayout.Button("Snap Origin To Pixel Grid"))
            {
                Undo.RecordObject(gridManager.transform, "Snap Origin To Pixel Grid");
                gridManager.transform.position = gridManager.SnapToPixel(gridManager.transform.position);
                EditorUtility.SetDirty(gridManager);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Validate Grid"))
            {
                bool ok = gridManager.ValidateGridPositioning();
                EditorUtility.DisplayDialog("Grid Validation", ok ? "Grid is valid." : "Conversion check failed.", "OK");
            }

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }
    }
}
