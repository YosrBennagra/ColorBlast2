using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShapeCreatorTool : EditorWindow
{
    private GameObject tilePrefab;
    private float gridSize = 1f;
    private Material tileMaterial;

    // Shape presets
    private static readonly List<Vector2Int> SingleSquare = new List<Vector2Int> { new Vector2Int(0, 0) };
    private static readonly List<Vector2Int> LShape = new List<Vector2Int> {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1)
    };
    private static readonly List<Vector2Int> TShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1)
    };
    private static readonly List<Vector2Int> TShapePM180 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1)
    };
    private static readonly List<Vector2Int> TShapeM90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(1, 0)
    };
    private static readonly List<Vector2Int> TShapeP90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, -1)
    };
    private static readonly List<Vector2Int> IShape = new List<Vector2Int> {
        new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)
    };
    private static readonly List<Vector2Int> OShape = new List<Vector2Int> {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
    };
    private static readonly List<Vector2Int> LongOShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(1, 1),
        new Vector2Int(1, 2)
    };
    private static readonly List<Vector2Int> BigOShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(1, 2),
        new Vector2Int(2, 0),
        new Vector2Int(2, 1),
        new Vector2Int(2, 2)
    };

    private static readonly List<Vector2Int> ZShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 1)
    };
    private static readonly List<Vector2Int> ZShapePM180 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1)

    };
    private static readonly List<Vector2Int> ZShapeP90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1)

    };
    private static readonly List<Vector2Int> ZShapeM90 = new List<Vector2Int> {
        new Vector2Int(-1, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 0),
        new Vector2Int(0, -1)
    };

    [MenuItem("Tools/ColorBlast2/Shape Creator")]
    public static void ShowWindow()
    {
        GetWindow<ShapeCreatorTool>("Shape Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("ColorBlast2 Shape Creator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox("This tool creates shape prefabs for the new ColorBlast2 architecture.", MessageType.Info);
        GUILayout.Space(10);

        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        tileMaterial = (Material)EditorGUILayout.ObjectField("Tile Material", tileMaterial, typeof(Material), false);
        gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);

        GUILayout.Space(10);
        GUILayout.Label("Quick Shape Creation:", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Single Square"))
            CreateShape("Single_Square", SingleSquare);

        if (GUILayout.Button("Create L-Shape"))
            CreateShape("L_Shape", LShape);

        if (GUILayout.Button("Create T-Shape"))
            CreateShape("T_Shape", TShape);
        if (GUILayout.Button("Create T-Shape-PM180"))
            CreateShape("T_Shape_PM180", TShapePM180);
        if (GUILayout.Button("Create T-Shape-P90"))
            CreateShape("T_Shape_P90", TShapeP90);
        if (GUILayout.Button("Create T-Shape-M90"))
            CreateShape("T_Shape_M90", TShapeM90);

        if (GUILayout.Button("Create I-Shape"))
            CreateShape("I_Shape", IShape);

        if (GUILayout.Button("Create O-Shape"))
            CreateShape("O_Shape", OShape);
        if (GUILayout.Button("Create Long-O-Shape"))
            CreateShape("Long_O_Shape", LongOShape);
        if (GUILayout.Button("Create Big-O-Shape"))
            CreateShape("Big_O_Shape", BigOShape);


        if (GUILayout.Button("Create Z-Shape"))
            CreateShape("Z_Shape", ZShape);
        if (GUILayout.Button("Create Z-Shape-PM180"))
            CreateShape("Z_Shape_PM180", ZShapePM180);
        if (GUILayout.Button("Create Z-Shape-P90"))
            CreateShape("Z_Shape_P90", ZShapeP90);
        if (GUILayout.Button("Create Z-Shape-M90"))
            CreateShape("Z_Shape_M90", ZShapeM90);
    }

    void CreateShape(string shapeName, List<Vector2Int> offsets)
    {
        // Create main shape GameObject
        GameObject shapeObject = new GameObject(shapeName);

        // Add required components
        Core.Shape shapeComponent = shapeObject.AddComponent<Core.Shape>();
        Gameplay.DragHandler dragHandler = shapeObject.AddComponent<Gameplay.DragHandler>();

        // Add a BoxCollider2D for mouse detection
        BoxCollider2D collider = shapeObject.AddComponent<BoxCollider2D>();

        // Configure the Shape component
        shapeComponent.SetShapeOffsets(offsets);

        // Create visual tiles if tile prefab is provided
        if (tilePrefab != null)
        {
            CreateVisualTiles(shapeObject, offsets);

            // Calculate collider bounds based on tiles
            CalculateColliderBounds(collider, offsets);
        }
        else
        {
            // Create simple visual representation with sprites
            CreateSimpleVisualTiles(shapeObject, offsets);
            CalculateColliderBounds(collider, offsets);
        }

        // Add ShapeThemeStorage component for theme support
        shapeObject.AddComponent<ShapeThemeStorage>();

        // Create prefab folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Shapes"))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Shapes");
        }

        // Save as prefab
        string prefabPath = $"Assets/_Project/Prefabs/Shapes/{shapeName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(shapeObject, prefabPath);

        // Clean up the scene object
        DestroyImmediate(shapeObject);

        // Select the created prefab
        Selection.activeObject = prefab;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created shape prefab: {shapeName} with {offsets.Count} tiles (Theme support included)");
    }

    void CreateVisualTiles(GameObject parent, List<Vector2Int> offsets)
    {
        foreach (Vector2Int offset in offsets)
        {
            GameObject tile = PrefabUtility.InstantiatePrefab(tilePrefab) as GameObject;
            tile.transform.SetParent(parent.transform);
            tile.transform.localPosition = new Vector3(offset.x * gridSize, offset.y * gridSize, 0);
            tile.name = $"Tile_{offset.x}_{offset.y}";

            // Apply material if provided
            if (tileMaterial != null)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = tileMaterial;
                }
            }
        }
    }

    void CreateSimpleVisualTiles(GameObject parent, List<Vector2Int> offsets)
    {
        foreach (Vector2Int offset in offsets)
        {
            GameObject tile = new GameObject($"Tile_{offset.x}_{offset.y}");
            tile.transform.SetParent(parent.transform);
            tile.transform.localPosition = new Vector3(offset.x * gridSize, offset.y * gridSize, 0);

            // Add SpriteRenderer with a simple square sprite
            SpriteRenderer spriteRenderer = tile.AddComponent<SpriteRenderer>();

            // Create a simple square texture
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            // Create sprite from texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            spriteRenderer.sprite = sprite;

            if (tileMaterial != null)
            {
                spriteRenderer.material = tileMaterial;
            }
        }
    }

    void CalculateColliderBounds(BoxCollider2D collider, List<Vector2Int> offsets)
    {
        if (offsets.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (Vector2Int offset in offsets)
        {
            float x = offset.x * gridSize;
            float y = offset.y * gridSize;

            minX = Mathf.Min(minX, x - gridSize * 0.5f);
            maxX = Mathf.Max(maxX, x + gridSize * 0.5f);
            minY = Mathf.Min(minY, y - gridSize * 0.5f);
            maxY = Mathf.Max(maxY, y + gridSize * 0.5f);
        }

        collider.size = new Vector2(maxX - minX, maxY - minY);
        collider.offset = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }
}
