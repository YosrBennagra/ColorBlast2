using UnityEngine;

/// <summary>
/// Component that stores the current sprite theme applied to a shape
/// </summary>
public class ShapeThemeStorage : MonoBehaviour
{
    [SerializeField] private SpriteTheme currentTheme;
    [SerializeField] private string themeName; // For inspector display
    
    public SpriteTheme CurrentTheme => currentTheme;
    
    /// <summary>
    /// Set the current theme for this shape
    /// </summary>
    public void SetTheme(SpriteTheme theme)
    {
        currentTheme = theme;
        themeName = theme?.themeName ?? "None";
    }
    
    /// <summary>
    /// Play the placement sound for the current theme
    /// </summary>
    public void PlayPlacementSound()
    {
        if (currentTheme?.placementSound != null)
        {
            // Try to find an AudioSource
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            float vol = 1f;
            if (ShapeSpriteManager.Instance != null)
            {
                // reflectively access private field if necessary or expose a getter; here we use a safe default path
                // Prefer calling manager API instead of direct play to keep behavior consistent
                ShapeSpriteManager.Instance.PlayPlacementAt(transform.position, currentTheme);
                return;
            }
            audioSource.PlayOneShot(currentTheme.placementSound, vol);
        }
    }
    
    /// <summary>
    /// Get the theme name for display purposes
    /// </summary>
    public string GetThemeName()
    {
        return themeName;
    }
}
