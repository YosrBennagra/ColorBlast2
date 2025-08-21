using UnityEngine;
using UnityEditor;
using ColorBlast.Game;
using System.Collections.Generic;

public class ShapeCreatorTool : EditorWindow
{
    private GameObject tilePrefab;
    private float gridSize = 1f;
    private Material tileMaterial;

    // Shape presets
    private static readonly List<Vector2Int> SingleSquare = new List<Vector2Int> { new Vector2Int(0, 0) };
    private static readonly List<Vector2Int> LShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(1, 0)
    };
    private static readonly List<Vector2Int> LShapePM180 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -2),
        new Vector2Int(0, -1)
    };
    private static readonly List<Vector2Int> LShapeM90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(-2, 0),
        new Vector2Int(0, 1)
    };
    private static readonly List<Vector2Int> LShapeP90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(0, -1)
    };
    private static readonly List<Vector2Int> RLShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(-1, 0)
    };
    private static readonly List<Vector2Int> RLShapePM180 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, -2),
        new Vector2Int(0, -1)
    };
    private static readonly List<Vector2Int> RLShapeM90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(-2, 0),
        new Vector2Int(0, -1)
    };
    private static readonly List<Vector2Int> RLShapeP90 = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(0, 1)
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
        new Vector2Int(0, 0)
    };
    private static readonly List<Vector2Int> IIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1)
    };
    private static readonly List<Vector2Int> IIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2)
    };
    private static readonly List<Vector2Int> IIIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(0, 3)
    };
    private static readonly List<Vector2Int> IIIIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2),
        new Vector2Int(0, 3),
        new Vector2Int(0, 4)
    };

    private static readonly List<Vector2Int> RIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0)
    };
    private static readonly List<Vector2Int> RIIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0)
    };
    private static readonly List<Vector2Int> RIIIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(3, 0)
    };
    private static readonly List<Vector2Int> RIIIIIShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(3, 0),
        new Vector2Int(4, 0)
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
    private static readonly List<Vector2Int> RLongOShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(2, 0),
        new Vector2Int(2, 1),
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

      private static readonly List<Vector2Int> iiSlashShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 1),
    };
          private static readonly List<Vector2Int> iiiSlashShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 1),
        new Vector2Int(2, 2),
    };
      private static readonly List<Vector2Int> RiiSlashShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 1),
    };
          private static readonly List<Vector2Int> RiiiSlashShape = new List<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(-2, 2),
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
        if (GUILayout.Button("Create L-ShapePM180"))
            CreateShape("L_ShapePM180", LShapePM180);
        if (GUILayout.Button("Create L-ShapeP90"))
            CreateShape("L_ShapeP90", LShapeP90);
        if (GUILayout.Button("Create L-ShapeM90"))
            CreateShape("L_ShapeM90", LShapeM90);

        if (GUILayout.Button("Create RL-Shape"))
            CreateShape("RL_Shape", RLShape);
        if (GUILayout.Button("Create RL-ShapePM180"))
            CreateShape("RL_ShapePM180", RLShapePM180);
        if (GUILayout.Button("Create RL-ShapeP90"))
            CreateShape("RL_ShapeP90", RLShapeP90);
        if (GUILayout.Button("Create RL-ShapeM90"))
            CreateShape("RL_ShapeM90", RLShapeM90);

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
        if (GUILayout.Button("Create II-Shape"))
            CreateShape("II_Shape", IIShape);
        if (GUILayout.Button("Create III-Shape"))
            CreateShape("III_Shape", IIIShape);
        if (GUILayout.Button("Create IIII-Shape"))
            CreateShape("IIII_Shape", IIIIShape);
        if (GUILayout.Button("Create IIIII-Shape"))
            CreateShape("IIIII_Shape", IIIIIShape);

        if (GUILayout.Button("Create RII-Shape"))
            CreateShape("RII_Shape", RIIShape);
        if (GUILayout.Button("Create RIII-Shape"))
            CreateShape("RIII_Shape", RIIIShape);
        if (GUILayout.Button("Create RIIII-Shape"))
            CreateShape("RIIII_Shape", RIIIIShape);
        if (GUILayout.Button("Create RIIIII-Shape"))
            CreateShape("RIIIII_Shape", RIIIIIShape);

        if (GUILayout.Button("Create Slash-Shape"))
            CreateShape("Slash_Shape", iiSlashShape);
        if (GUILayout.Button("Create Slash-Shape-iii"))
            CreateShape("Slash_Shape_iii", iiiSlashShape);
        if (GUILayout.Button("Create R-Slash-Shape"))
            CreateShape("R_Slash_Shape", RiiSlashShape);
        if (GUILayout.Button("Create R-Slash-Shape-iii"))
            CreateShape("R_Slash_Shape_iii", RiiiSlashShape);

        


        if (GUILayout.Button("Create O-Shape"))
            CreateShape("O_Shape", OShape);
        if (GUILayout.Button("Create Long-O-Shape"))
            CreateShape("Long_O_Shape", LongOShape);
        if (GUILayout.Button("Create RLong-O-Shape"))
            CreateShape("RLong_O_Shape", RLongOShape);

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
        Shape shapeComponent = shapeObject.AddComponent<Shape>();
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
                    renderer.sharedMaterial = tileMaterial; // avoid creating instances
                }
            }
        }
    }

    private static Sprite cachedWhiteSprite;
    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color[4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        cachedWhiteSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 64);
        return cachedWhiteSprite;
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
            spriteRenderer.sprite = GetWhiteSprite();

            if (tileMaterial != null)
            {
                spriteRenderer.sharedMaterial = tileMaterial; // avoid instances
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

        // Make collider 30% larger for easier grabbing
        float sizeX = (maxX - minX) * 1.3f;
        float sizeY = (maxY - minY) * 1.3f;
        
        collider.size = new Vector2(sizeX, sizeY);
        collider.offset = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }
}
