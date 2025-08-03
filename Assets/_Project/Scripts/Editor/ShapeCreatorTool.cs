using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShapeCreatorTool : EditorWindow
{
    private GameObject tilePrefab;
    private float tileSize = 1f;
    
    [MenuItem("Tools/Puzzle Game/Shape Creator")]
    public static void ShowWindow()
    {
        GetWindow<ShapeCreatorTool>("Shape Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Quick Shape Creator", EditorStyles.boldLabel);
        
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        tileSize = EditorGUILayout.FloatField("Tile Size", tileSize);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Single Square"))
            CreateShape("Single Square", ShapePresets.SingleSquare, Color.red);
            
        if (GUILayout.Button("Create L-Shape"))
            CreateShape("L-Shape", ShapePresets.LShape, Color.blue);
            
        if (GUILayout.Button("Create T-Shape"))
            CreateShape("T-Shape", ShapePresets.TShape, Color.green);
            
        if (GUILayout.Button("Create I-Shape"))
            CreateShape("I-Shape", ShapePresets.IShape, Color.yellow);
            
        if (GUILayout.Button("Create O-Shape"))
            CreateShape("O-Shape", ShapePresets.OShape, Color.cyan);
            
        if (GUILayout.Button("Create Z-Shape"))
            CreateShape("Z-Shape", ShapePresets.ZShape, Color.magenta);
            
        if (GUILayout.Button("Create S-Shape"))
            CreateShape("S-Shape", ShapePresets.SShape, Color.white);
    }
    
    void CreateShape(string shapeName, List<Vector2Int> offsets, Color color)
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Please assign a tile prefab first!");
            return;
        }
        
        // Create main shape GameObject
        GameObject shapeObject = new GameObject(shapeName);
        
        // Add ShapeCreator component
        ShapeCreator creator = shapeObject.AddComponent<ShapeCreator>();
        
        // Create ShapeData asset
        ShapeData shapeData = ScriptableObject.CreateInstance<ShapeData>();
        shapeData.shapeName = shapeName;
        shapeData.tileOffsets = new List<Vector2Int>(offsets);
        shapeData.shapeColor = color;
        
        // Create the folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Shapes"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Shapes");
        }
        
        // Save the ScriptableObject
        string assetPath = $"Assets/_Project/Shapes/{shapeName}.asset";
        AssetDatabase.CreateAsset(shapeData, assetPath);
        
        // Use SerializedObject to set private fields properly
        SerializedObject serializedCreator = new SerializedObject(creator);
        
        SerializedProperty shapeDataProp = serializedCreator.FindProperty("shapeData");
        if (shapeDataProp != null)
            shapeDataProp.objectReferenceValue = shapeData;
            
        SerializedProperty tilePrefabProp = serializedCreator.FindProperty("tilePrefab");
        if (tilePrefabProp != null)
            tilePrefabProp.objectReferenceValue = tilePrefab;
            
        SerializedProperty tileSizeProp = serializedCreator.FindProperty("tileSize");
        if (tileSizeProp != null)
            tileSizeProp.floatValue = tileSize;
            
        serializedCreator.ApplyModifiedProperties();
        
        // Refresh the shape
        creator.RefreshShape();
        
        // Select the created object
        Selection.activeGameObject = shapeObject;
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
