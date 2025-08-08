using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple sprite theme for shapes
/// </summary>
[System.Serializable]
public class SpriteTheme
{
    public string themeName;
    public Sprite tileSprite;
    [Range(0f, 1f)]
    public float spawnWeight = 1f; // Higher weight = more likely to spawn
    
    [Header("Audio")]
    public AudioClip placementSound;
}

public class ShapeSpriteManager : MonoBehaviour
{
    [Header("Sprite Themes")]
    [SerializeField] private List<SpriteTheme> spriteThemes = new List<SpriteTheme>();
    
    [Header("Spawning Rules")]
    [SerializeField] private bool randomizeThemes = true;
    [SerializeField] private bool allowSameThemeForAllShapes = true;
    [SerializeField] private int minDifferentThemes = 1;
    [SerializeField] private int maxDifferentThemes = 3;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Singleton-like access
    public static ShapeSpriteManager Instance { get; private set; }
    
    // Events
    public System.Action<Core.Shape, SpriteTheme> OnShapeThemeApplied;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        ValidateThemes();
    }
    
    private void ValidateThemes()
    {
        if (spriteThemes.Count == 0)
        {
            Debug.LogWarning("ShapeSpriteManager: No sprite themes configured!");
            return;
        }
        
        // Ensure all themes have valid names
        for (int i = 0; i < spriteThemes.Count; i++)
        {
            if (string.IsNullOrEmpty(spriteThemes[i].themeName))
            {
                spriteThemes[i].themeName = $"Theme_{i}";
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"ShapeSpriteManager: Loaded {spriteThemes.Count} sprite themes");
        }
    }
    
    /// <summary>
    /// Apply random themes to a set of shapes
    /// </summary>
    public void ApplyRandomThemes(GameObject[] shapes)
    {
        if (shapes == null || shapes.Length == 0) return;
        
        List<SpriteTheme> selectedThemes = SelectThemesForShapes(shapes.Length);
        
        for (int i = 0; i < shapes.Length && i < selectedThemes.Count; i++)
        {
            if (shapes[i] != null)
            {
                ApplyThemeToShape(shapes[i], selectedThemes[i]);
            }
        }
    }
    
    /// <summary>
    /// Apply a random theme to a single shape
    /// </summary>
    public SpriteTheme ApplyRandomTheme(GameObject shape)
    {
        if (shape == null) return null;
        
        SpriteTheme selectedTheme = GetRandomTheme();
        ApplyThemeToShape(shape, selectedTheme);
        return selectedTheme;
    }
    
    /// <summary>
    /// Apply a specific theme to a shape
    /// </summary>
    public void ApplyThemeToShape(GameObject shapeObject, SpriteTheme theme)
    {
        if (shapeObject == null || theme == null) return;
        
        Core.Shape shape = shapeObject.GetComponent<Core.Shape>();
        if (shape == null)
        {
            Debug.LogWarning($"GameObject {shapeObject.name} doesn't have a Shape component!");
            return;
        }
        
        // Cache tile renderers if not already done
        shape.CacheTileRenderers();
        SpriteRenderer[] tileRenderers = shape.TileRenderers;
        
        if (tileRenderers == null || tileRenderers.Length == 0)
        {
            Debug.LogWarning($"Shape {shapeObject.name} has no tile renderers!");
            return;
        }
        
        // Apply theme to all tile renderers
        for (int i = 0; i < tileRenderers.Length; i++)
        {
            ApplyThemeToRenderer(tileRenderers[i], theme, i, tileRenderers.Length);
        }
        
        // Store theme reference on the shape for later use
        var themeStorage = shapeObject.GetComponent<ShapeThemeStorage>();
        if (themeStorage == null)
        {
            themeStorage = shapeObject.AddComponent<ShapeThemeStorage>();
        }
        themeStorage.SetTheme(theme);
        
        OnShapeThemeApplied?.Invoke(shape, theme);
        
        if (showDebugInfo)
        {
            Debug.Log($"Applied theme '{theme.themeName}' to shape {shapeObject.name}");
        }
    }
    
    /// <summary>
    /// Apply theme to a specific sprite renderer
    /// </summary>
    private void ApplyThemeToRenderer(SpriteRenderer renderer, SpriteTheme theme, int tileIndex, int totalTiles)
    {
        if (renderer == null) return;
        
        // Apply sprite
        if (theme.tileSprite != null)
        {
            renderer.sprite = theme.tileSprite;
        }
    }
    
    /// <summary>
    /// Get a random theme based on spawn weights
    /// </summary>
    public SpriteTheme GetRandomTheme()
    {
        if (spriteThemes.Count == 0) return null;
        if (spriteThemes.Count == 1) return spriteThemes[0];
        
        // Calculate total weight
        float totalWeight = spriteThemes.Sum(theme => theme.spawnWeight);
        if (totalWeight <= 0f)
        {
            // If no valid weights, choose randomly
            return spriteThemes[Random.Range(0, spriteThemes.Count)];
        }
        
        // Weighted random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var theme in spriteThemes)
        {
            currentWeight += theme.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return theme;
            }
        }
        
        // Fallback
        return spriteThemes[spriteThemes.Count - 1];
    }
    
    /// <summary>
    /// Select themes for multiple shapes based on spawning rules
    /// </summary>
    private List<SpriteTheme> SelectThemesForShapes(int shapeCount)
    {
        List<SpriteTheme> selectedThemes = new List<SpriteTheme>();
        
        if (!randomizeThemes)
        {
            // If not randomizing, just use the first theme for all
            SpriteTheme defaultTheme = spriteThemes.Count > 0 ? spriteThemes[0] : null;
            for (int i = 0; i < shapeCount; i++)
            {
                selectedThemes.Add(defaultTheme);
            }
            return selectedThemes;
        }
        
        if (allowSameThemeForAllShapes)
        {
            // Can use same theme for all shapes
            for (int i = 0; i < shapeCount; i++)
            {
                selectedThemes.Add(GetRandomTheme());
            }
        }
        else
        {
            // Ensure variety in themes
            int themesToUse = Mathf.Clamp(
                Random.Range(minDifferentThemes, maxDifferentThemes + 1),
                1,
                Mathf.Min(spriteThemes.Count, shapeCount)
            );
            
            // Select unique themes
            List<SpriteTheme> availableThemes = new List<SpriteTheme>(spriteThemes);
            List<SpriteTheme> uniqueThemes = new List<SpriteTheme>();
            
            for (int i = 0; i < themesToUse && availableThemes.Count > 0; i++)
            {
                SpriteTheme randomTheme = availableThemes[Random.Range(0, availableThemes.Count)];
                uniqueThemes.Add(randomTheme);
                availableThemes.Remove(randomTheme);
            }
            
            // Assign themes to shapes
            for (int i = 0; i < shapeCount; i++)
            {
                selectedThemes.Add(uniqueThemes[i % uniqueThemes.Count]);
            }
            
            // Shuffle the assignments
            for (int i = 0; i < selectedThemes.Count; i++)
            {
                int randomIndex = Random.Range(i, selectedThemes.Count);
                var temp = selectedThemes[i];
                selectedThemes[i] = selectedThemes[randomIndex];
                selectedThemes[randomIndex] = temp;
            }
        }
        
        return selectedThemes;
    }
    
    /// <summary>
    /// Get theme by name
    /// </summary>
    public SpriteTheme GetThemeByName(string themeName)
    {
        return spriteThemes.FirstOrDefault(theme => theme.themeName.Equals(themeName, System.StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get the theme applied to a shape
    /// </summary>
    public SpriteTheme GetShapeTheme(GameObject shapeObject)
    {
        if (shapeObject == null) return null;
        
        var themeStorage = shapeObject.GetComponent<ShapeThemeStorage>();
        return themeStorage?.CurrentTheme;
    }
    
    #region Editor Helper Methods
    
    [System.Serializable]
    public class EditorHelpers
    {
        [Header("Quick Setup")]
        public Sprite waterSprite;
        public Sprite landSprite;
    }
    
    [SerializeField] private EditorHelpers editorHelpers = new EditorHelpers();
    
    [ContextMenu("Setup Basic Water/Land Themes")]
    private void SetupBasicThemes()
    {
        spriteThemes.Clear();
        
        // Water theme
        if (editorHelpers.waterSprite != null)
        {
            SpriteTheme waterTheme = new SpriteTheme
            {
                themeName = "Water",
                tileSprite = editorHelpers.waterSprite,
                spawnWeight = 0.5f
            };
            spriteThemes.Add(waterTheme);
        }
        
        // Land theme
        if (editorHelpers.landSprite != null)
        {
            SpriteTheme landTheme = new SpriteTheme
            {
                themeName = "Land",
                tileSprite = editorHelpers.landSprite,
                spawnWeight = 0.5f
            };
            spriteThemes.Add(landTheme);
        }
        
        Debug.Log("Basic Water/Land themes setup complete!");
    }
    
    #endregion
}
