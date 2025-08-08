using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to set up the Shape Sprite Manager system in your scene
/// </summary>
public class ShapeThemeSetupTool : EditorWindow
{
    [MenuItem("Tools/ColorBlast2/Shape Theme Setup")]
    public static void ShowWindow()
    {
        GetWindow<ShapeThemeSetupTool>("Shape Theme Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Shape Theme System Setup", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This tool helps you set up the Shape Theme system for your ColorBlast2 game.\n\n" +
            "The system allows shapes to spawn with different visual themes like water, land, etc.",
            MessageType.Info
        );
        
        GUILayout.Space(10);

        if (GUILayout.Button("Create Shape Sprite Manager"))
        {
            CreateShapeSpriteManager();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Setup Basic Water/Land Themes"))
        {
            SetupBasicThemes();
        }

        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Click 'Create Shape Sprite Manager' to add the manager to your scene\n" +
            "2. Assign your water and land sprites to the manager\n" +
            "3. Configure spawn weights and other settings\n" +
            "4. The ShapeSpawner will automatically use themes when spawning shapes",
            MessageType.None
        );
    }

    void CreateShapeSpriteManager()
    {
        // Check if one already exists
        ShapeSpriteManager existing = FindObjectOfType<ShapeSpriteManager>();
        if (existing != null)
        {
            Debug.LogWarning("ShapeSpriteManager already exists in the scene!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create new GameObject with ShapeSpriteManager
        GameObject managerObject = new GameObject("ShapeSpriteManager");
        ShapeSpriteManager manager = managerObject.AddComponent<ShapeSpriteManager>();

        // Position it appropriately
        managerObject.transform.position = Vector3.zero;

        // Select it in the hierarchy
        Selection.activeGameObject = managerObject;

        Debug.Log("Created ShapeSpriteManager! Configure your themes in the inspector.");
    }

    void SetupBasicThemes()
    {
        ShapeSpriteManager manager = FindObjectOfType<ShapeSpriteManager>();
        if (manager == null)
        {
            Debug.LogError("No ShapeSpriteManager found! Create one first.");
            return;
        }

        // The manager has a context menu item to set up basic themes
        Debug.Log("Select the ShapeSpriteManager in the scene and use the context menu 'Setup Basic Water/Land Themes' or assign sprites manually in the inspector.");
        Selection.activeGameObject = manager.gameObject;
    }
}
