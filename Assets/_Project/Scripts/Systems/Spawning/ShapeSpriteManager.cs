using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;
using ColorBlast.Game;

/// <summary>
/// Simple sprite theme for shapes
/// </summary>
[System.Serializable]
public class SpriteTheme
{
    public string themeName;
    [Header("Sprites")]
    [Tooltip("Primary sprite if multiple sprites list is empty.")]
    public Sprite tileSprite;
    [Tooltip("Optional list of sprites to add variation across tiles for this theme.")]
    public List<Sprite> tileSprites = new List<Sprite>();
    [Tooltip("If true, pick a random sprite per tile from tileSprites; otherwise use tileIndex % tileSprites.Count.")]
    public bool randomizeTileSprites = false;
    [Range(0f, 1f)]
    public float spawnWeight = 1f; // Higher weight = more likely to spawn
    
    [Header("Audio")]
    public AudioClip placementSound;
    public AudioClip clearSound;

    [Header("Effects/Animation")]
    [Tooltip("Optional particle/effect prefab to spawn when a tile of this theme is cleared.")]
    public GameObject clearEffectPrefab;
}

public class ShapeSpriteManager : MonoBehaviour
{
    [Header("Sprite Themes")]
    [SerializeField] private List<SpriteTheme> spriteThemes = new List<SpriteTheme>();
    
    [Header("Default FX/SFX (fallbacks)")]
    [SerializeField] private AudioClip defaultPlacementSound;
    [SerializeField] private AudioClip defaultClearSound;
    [SerializeField] private GameObject defaultClearEffectPrefab;

    [Header("SFX Volume")]
    [Tooltip("Volume for placement sound effects.")]
    [Range(0f,1f)]
    [SerializeField] private float placementSfxVolume = 1f;

    /// <summary>
    /// Placement SFX volume (0..1). Adjust at runtime from UI if needed.
    /// </summary>
    public float PlacementSfxVolume
    {
        get => placementSfxVolume;
        set => placementSfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Convenience setter for UI events.
    /// </summary>
    public void SetPlacementSfxVolume(float v) => PlacementSfxVolume = v;

    [Header("Spawning Rules")]
    [SerializeField] private bool randomizeThemes = true;
    [SerializeField] private bool allowSameThemeForAllShapes = true;
    [SerializeField] private int minDifferentThemes = 1;
    [SerializeField] private int maxDifferentThemes = 3;
    
    // Singleton-like access
    public static ShapeSpriteManager Instance { get; private set; }
    
    // Events
    public System.Action<Shape, SpriteTheme> OnShapeThemeApplied;
    
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
        
        Shape shape = shapeObject.GetComponent<Shape>();
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
    }
    
    /// <summary>
    /// Apply theme to a specific sprite renderer
    /// </summary>
    private void ApplyThemeToRenderer(SpriteRenderer renderer, SpriteTheme theme, int tileIndex, int totalTiles)
    {
        if (renderer == null) return;
        
        // Choose sprite with per-theme variation support
        if (theme.tileSprites != null && theme.tileSprites.Count > 0)
        {
            int pick = theme.randomizeTileSprites ? Random.Range(0, theme.tileSprites.Count) : (tileIndex % theme.tileSprites.Count);
            var chosen = theme.tileSprites[Mathf.Clamp(pick, 0, theme.tileSprites.Count - 1)];
            if (chosen != null) renderer.sprite = chosen;
        }
        else if (theme.tileSprite != null)
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
    /// Spawn a clear effect and sound at a world position using the given theme (falls back to defaults).
    /// </summary>
    public void PlayClearEffectAt(Vector3 worldPosition, SpriteTheme theme)
    {
        // Effect prefab spawn
        GameObject prefab = theme != null && theme.clearEffectPrefab != null ? theme.clearEffectPrefab : defaultClearEffectPrefab;
        if (prefab != null)
        {
            var fx = Instantiate(prefab, worldPosition, Quaternion.identity);
            // Auto-destroy if it doesn't auto-cleanup; try to detect particle system duration
            var ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax + 0.25f);
            }
            else
            {
                Destroy(fx, 1.5f);
            }
        }

        // Audio
        var clip = theme != null && theme.clearSound != null ? theme.clearSound : defaultClearSound;
        if (clip != null)
        {
            PlaySfxRespectingMute(worldPosition, clip, 1f);
        }
    }

    /// <summary>
    /// Spawn a clear effect using a specific tile sprite/color when no prefab is provided, for material-consistent shattering.
    /// Falls back to prefab path if available.
    /// </summary>
    public void PlayClearEffectAt(Vector3 worldPosition, SpriteTheme theme, Sprite tileSprite, Color tileColor)
    {
        // If a prefab is defined, prefer it
        if (theme != null && theme.clearEffectPrefab != null)
        {
            PlayClearEffectAt(worldPosition, theme);
            return;
        }

        // Sprite-based shard burst (no ParticleSystem) to avoid renderer property sheet issues
        GameObject parent = new GameObject("TileClearFX_Shards");
        parent.transform.position = worldPosition;
        int count = Random.Range(10, 16);
        for (int i = 0; i < count; i++)
        {
            var shard = new GameObject("Shard");
            shard.transform.SetParent(parent.transform, worldPositionStays: false);
            var sr = shard.AddComponent<SpriteRenderer>();
            sr.sprite = tileSprite;
            sr.color = (tileColor.a > 0 ? tileColor : Color.white);
            sr.sortingOrder = 200;
            // random scale shards a bit
            float s = Random.Range(0.35f, 0.6f);
            shard.transform.localScale = new Vector3(s, s, 1f);

            // Animate using DOTween (move, rotate, fade, then cleanup)
            float life = Random.Range(0.45f, 0.7f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(0.4f, 1.0f);
            Vector3 endPos = worldPosition + new Vector3(dir.x, dir.y, 0f) * dist + new Vector3(0f, Random.Range(-0.6f, -1.2f), 0f);
            float rotZ = shard.transform.eulerAngles.z + Random.Range(180f, 720f) * (Random.value < 0.5f ? -1f : 1f);

            var seq = DOTween.Sequence();
            seq.Join(shard.transform.DOMove(endPos, life).SetEase(Ease.OutQuad));
            seq.Join(shard.transform.DORotate(new Vector3(0f, 0f, rotZ), life, RotateMode.FastBeyond360).SetEase(Ease.OutSine));
            if (sr != null)
            {
                var c = sr.color;
                seq.Join(sr.DOFade(0f, life).From(c.a).SetEase(Ease.InQuad));
            }
            seq.OnComplete(() => Object.Destroy(shard));
        }
        Object.Destroy(parent, 1.5f);

    var clip = theme != null && theme.clearSound != null ? theme.clearSound : defaultClearSound;
    if (clip != null) PlaySfxRespectingMute(worldPosition, clip, 1f);
    }

    /// <summary>
    /// Play a small placement pop/bounce animation on a shape's tiles.
    /// </summary>
    public void PlayPlacementAnimation(Shape shape)
    {
        if (shape == null) return;
        StartCoroutine(PlacementPopCoroutine(shape));
    }

    private IEnumerator PlacementPopCoroutine(Shape shape)
    {
        shape.CacheTileRenderers();
        var tiles = shape.TileRenderers;
        if (tiles == null || tiles.Length == 0) yield break;

        float dur = 0.18f;
        float elapsed = 0f;
        // Capture initial local scales
        var initial = new Vector3[tiles.Length];
        for (int i = 0; i < tiles.Length; i++) initial[i] = tiles[i] != null ? tiles[i].transform.localScale : Vector3.one;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            // Ease-out elastic lite
            float s = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].transform.localScale = initial[i] * s;
            }
            yield return null;
        }
        // Restore
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;
            tiles[i].transform.localScale = initial[i];
        }
    }
    /// <summary>
    /// Play placement sound at position using theme or default fallback.
    /// </summary>
    public void PlayPlacementAt(Vector3 worldPosition, SpriteTheme theme)
    {
        var clip = theme != null && theme.placementSound != null ? theme.placementSound : defaultPlacementSound;
        if (clip != null)
        {
            PlaySfxRespectingMute(worldPosition, clip, placementSfxVolume);
        }
    }

    private const string SfxMuteKey = "Audio.SfxMuted";
    private static bool IsSfxMuted() => PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;
    private static void PlaySfxRespectingMute(Vector3 position, AudioClip clip, float volume = 1f)
    {
        if (clip == null || IsSfxMuted()) return;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
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
