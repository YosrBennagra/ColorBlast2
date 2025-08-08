using UnityEngine;

/// <summary>
/// Simple example showing how to check what theme a shape has
/// </summary>
public class ThemeBasedBehavior : MonoBehaviour
{
    private ShapeThemeStorage themeStorage;
    
    void Start()
    {
        // Get the theme storage component
        themeStorage = GetComponent<ShapeThemeStorage>();
        
        // Log what theme this shape has
        if (themeStorage != null && themeStorage.CurrentTheme != null)
        {
            Debug.Log($"Shape {gameObject.name} has theme: {themeStorage.CurrentTheme.themeName}");
        }
    }
    
    /// <summary>
    /// Get the current theme name
    /// </summary>
    public string GetCurrentThemeName()
    {
        return themeStorage?.GetThemeName() ?? "None";
    }
    
    /// <summary>
    /// Check if this shape has a specific theme
    /// </summary>
    public bool HasTheme(string themeName)
    {
        return GetCurrentThemeName().Equals(themeName, System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Example method showing different behavior based on theme
    /// </summary>
    void OnMouseDown()
    {
        if (themeStorage?.CurrentTheme == null) return;
        
        string themeName = themeStorage.CurrentTheme.themeName;
        
        switch (themeName.ToLower())
        {
            case "water":
                Debug.Log("You clicked a water shape!");
                break;
                
            case "land":
                Debug.Log("You clicked a land shape!");
                break;
                
            default:
                Debug.Log($"You clicked a {themeName} shape!");
                break;
        }
    }
}
