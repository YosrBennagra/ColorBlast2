using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;
using ColorBlast.Game;
using ColorBlast.Core.Architecture;
using Gameplay;

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
    [Tooltip("Sound when a shape starts moving from the tray (drag begin).")]
    public AudioClip moveSound;

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
    [SerializeField] private AudioClip defaultMoveSound;
    [SerializeField] private AudioClip defaultClearSound;
    [SerializeField] private GameObject defaultClearEffectPrefab;

    [Header("SFX Volume")]
    [Tooltip("Volume for placement sound effects.")]
    [Range(0f,1f)]
    [SerializeField] private float placementSfxVolume = 1f;
    [Tooltip("Volume for move sound effects (on drag begin).")]
    [Range(0f,1f)]
    [SerializeField] private float moveSfxVolume = 1f;

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

    /// <summary>
    /// Move SFX volume (0..1). Adjust at runtime from UI if needed.
    /// </summary>
    public float MoveSfxVolume
    {
        get => moveSfxVolume;
        set => moveSfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Convenience setter for UI events.
    /// </summary>
    public void SetMoveSfxVolume(float v) => MoveSfxVolume = v;

    [Header("Clear FX Settings")]
    [Tooltip("Use a minimal, smooth clear effect instead of shard bursts.")]
    [SerializeField] private bool useSimpleClearFx = true;
    [SerializeField, Range(0f, 1f)] private float clearFlashIntensity = 0.22f;
    [SerializeField, Range(1.0f, 1.5f)] private float clearScaleUp = 1.12f;
    [SerializeField, Range(0.05f, 0.6f)] private float clearDuration = 0.22f;
    [SerializeField, Range(0.8f, 1f)] private float clearPopScaleDown = 0.94f;
    [SerializeField, Range(0f, 0.3f)] private float clearUpOffset = 0.12f;
    [SerializeField] private bool perTilePopSound = true;
    [SerializeField, Range(0f,1f)] private float perTilePopVolume = 0.18f;
    [SerializeField, Min(1)] private int perTilePopSoundEveryN = 3;

    [Header("Spawning Rules")]
    [SerializeField] private bool randomizeThemes = true;
    [SerializeField] private bool allowSameThemeForAllShapes = true;
    [SerializeField] private int minDifferentThemes = 1;
    [SerializeField] private int maxDifferentThemes = 3;
    
    // Singleton-like access
    public static ShapeSpriteManager Instance { get; private set; }
    private AudioSource _oneShot2D;
    
    // Events
    public System.Action<Shape, SpriteTheme> OnShapeThemeApplied;
    
    // Placement FX configuration
    public enum PlacementFxMode { Simple, Cascade, OrbitAssemble, MagneticSnap }
    [Header("Placement FX Settings")]
    [SerializeField] private PlacementFxMode placementFxMode = PlacementFxMode.Simple;
    [SerializeField, Range(0f, 0.2f)] private float placementShakeStrength = 0.06f;
    [SerializeField, Range(0f, 0.3f)] private float orbitOutDistance = 0.25f;
    [SerializeField, Range(0f, 0.5f)] private float orbitOutDuration = 0.12f;
    [SerializeField, Range(0f, 0.6f)] private float orbitReturnDuration = 0.22f;
    [SerializeField, Range(0f, 0.15f)] private float orbitStaggerPerUnit = 0.02f;
    [SerializeField] private bool placementUseGridCrossRipple = true;
    
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
        // Prepare 2D one-shot source so tray move SFX don't attenuate with distance
        _oneShot2D = GetComponent<AudioSource>();
        if (_oneShot2D == null)
        {
            _oneShot2D = gameObject.AddComponent<AudioSource>();
        }
        _oneShot2D.playOnAwake = false;
        _oneShot2D.loop = false;
        _oneShot2D.spatialBlend = 0f; // 2D playback
    }

    // Lazy-generated solid-white sprite used for lightweight FX to avoid relying on Unity built-ins
    private static Sprite _whiteSprite;
    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.name = "GeneratedWhite1x1";
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        _whiteSprite.name = "GeneratedWhiteSprite";
        return _whiteSprite;
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
        if (prefab != null && !useSimpleClearFx)
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
        else
        {
            // Simple, smooth puff
            SpawnSimpleClearFX(worldPosition, null, Color.white);
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
        if (!useSimpleClearFx && theme != null && theme.clearEffectPrefab != null)
        {
            PlayClearEffectAt(worldPosition, theme);
            return;
        }
        // Simple, smooth puff
        SpawnSimpleClearFX(worldPosition, tileSprite, tileColor.a > 0 ? tileColor : Color.white);

        var clip = theme != null && theme.clearSound != null ? theme.clearSound : defaultClearSound;
        if (clip != null) PlaySfxRespectingMute(worldPosition, clip, 1f);
    }

    private void SpawnSimpleClearFX(Vector3 worldPosition, Sprite tileSprite, Color tint)
    {
        var go = new GameObject("TileClearFX_Simple");
        go.transform.position = worldPosition;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite != null ? tileSprite : GetWhiteSprite();
        // keep alpha, lift brightness a touch
        var baseColor = new Color(tint.r, tint.g, tint.b, 0.9f);
        sr.color = baseColor;
        sr.sortingOrder = 210;
        float dur = Mathf.Max(0.05f, clearDuration);
        float up = clearUpOffset;
        float dx = Random.Range(-0.025f, 0.025f);
        var seq = DOTween.Sequence();
        // overall upward drift
        seq.Insert(0f, go.transform.DOMove(worldPosition + new Vector3(dx, up, 0f), dur).SetEase(Ease.OutSine));
        // two-stage pop: slight in, then up and out
        seq.Append(go.transform.DOScale(clearPopScaleDown, dur * 0.25f).From(1f).SetEase(Ease.OutSine));
        seq.Append(go.transform.DOScale(clearScaleUp, dur * 0.75f).SetEase(Ease.OutBack));
        // fade and flash over total duration
        seq.Insert(0f, sr.DOFade(0f, dur).From(baseColor.a).SetEase(Ease.InSine));
        seq.Insert(0f, sr.DOColor(Color.Lerp(baseColor, Color.white, clearFlashIntensity), dur * 0.5f).From(baseColor));
        // light per-tile pop sound, rate-limited
        seq.OnStart(() =>
        {
            if (perTilePopSound && perTilePopSoundEveryN > 0)
            {
                _perTilePopCounter++;
                if ((_perTilePopCounter % perTilePopSoundEveryN) == 0)
                {
                    var clip = defaultClearSound != null ? defaultClearSound : defaultPlacementSound;
                    if (clip != null) PlaySfxRespectingMute(worldPosition, clip, Mathf.Clamp01(perTilePopVolume));
                }
            }
        });
        seq.OnComplete(() => Destroy(go));
    }

    private static int _perTilePopCounter = 0;

    /// <summary>
    /// Draw a quick glowing sweep between two world points (used for line clears).
    /// </summary>
    public void PlayLineSweep(Vector3 worldStart, Vector3 worldEnd, float duration = 0.25f, float thickness = 0.15f)
    {
        var go = new GameObject("LineSweepFX");
        var sr = go.AddComponent<SpriteRenderer>();
        // Use a default sprite as a simple white strip (fallback-safe)
        sr.sprite = GetWhiteSprite();
        sr.color = new Color(1f, 1f, 1f, 0.6f);
        sr.sortingOrder = 300;
        // Position between start and end
        Vector3 mid = (worldStart + worldEnd) * 0.5f;
        go.transform.position = mid;
        // Align/scale
        Vector3 dir = (worldEnd - worldStart);
        float len = dir.magnitude + 0.1f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);
        go.transform.localScale = new Vector3(len, thickness, 1f);
        // Animate fade + slight expand
        if (sr.sprite != null)
        {
            var seq = DOTween.Sequence();
            seq.Join(sr.DOFade(0f, duration).From(sr.color.a).SetEase(Ease.OutSine));
            seq.Join(go.transform.DOScaleY(thickness * 1.25f, duration).SetEase(Ease.OutSine));
            seq.OnComplete(() => Destroy(go));
        }
        else
        {
            Destroy(go);
        }
    }

    /// <summary>
    /// Play a pulse burst at a world position for row x column intersections.
    /// </summary>
    public void PlayIntersectionPulse(Vector3 worldPosition, float duration = 0.25f)
    {
        var go = new GameObject("IntersectionPulseFX");
        go.transform.position = worldPosition;
        var sr = go.AddComponent<SpriteRenderer>();
        // Use a built-in square as a pulse (circle not guaranteed across Unity versions)
        sr.sprite = GetWhiteSprite();
        sr.color = new Color(1f, 1f, 1f, 0.95f);
        sr.sortingOrder = 320;
        float start = 0.2f, end = 0.8f;
        go.transform.localScale = new Vector3(start, start, 1f);
        if (sr.sprite != null)
        {
            var seq = DOTween.Sequence();
            seq.Join(go.transform.DOScale(end, duration).SetEase(Ease.OutQuad));
            seq.Join(sr.DOFade(0f, duration).From(0.9f).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(go));
        }
        else
        {
            Destroy(go);
        }
    }

    /// <summary>
    /// Play placement animation according to configured mode.
    /// </summary>
    public void PlayPlacementAnimation(Shape shape)
    {
        if (shape == null) return;
        // Simplified, smooth placement by default (ignores mode to ensure clean look)
        StartCoroutine(PlacementSimpleCoroutine(shape));
    }

    private IEnumerator PlacementCascadeCoroutine(Shape shape)
    {
        shape.CacheTileRenderers();
        var tiles = shape.TileRenderers;
        if (tiles == null || tiles.Length == 0) yield break;

        // Compute shape center in world space for wave delays
        Vector3 center = Vector3.zero;
        int live = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue; center += tiles[i].transform.position; live++;
        }
        if (live > 0) center /= live; else center = shape.transform.position;

        // Configure timing
        float baseDelay = 0.0f;
        float delayPerUnit = 0.025f; // wavefront speed
        float popIn = 0.12f;
        float settle = 0.08f;

        // Kick off per-tile animations with distance-based stagger
        for (int i = 0; i < tiles.Length; i++)
        {
            var sr = tiles[i]; if (sr == null) continue;
            Vector3 wp = sr.transform.position;
            float d = Vector3.Distance(wp, center);
            float delay = baseDelay + d * delayPerUnit;

            // Capture initial state
            var startScale = sr.transform.localScale;
            var startColor = sr.color;

            // Sequence: slight scale down then pop up, tiny rotation jiggle, color flash to white, then settle
            var seq = DOTween.Sequence();
            seq.SetDelay(delay);
            seq.Append(sr.transform.DOScale(startScale * 0.9f, popIn * 0.35f).SetEase(Ease.OutSine));
            seq.Append(sr.transform.DOScale(startScale * 1.12f, popIn * 0.65f).SetEase(Ease.OutBack));
            seq.Join(sr.transform.DORotate(new Vector3(0,0,Random.Range(-6f,6f)), popIn, RotateMode.Fast).SetEase(Ease.OutSine));
            seq.Join(sr.DOColor(Color.white, popIn * 0.5f).From(startColor));
            seq.Append(sr.transform.DOScale(startScale, settle).SetEase(Ease.InSine));
            seq.Join(sr.transform.DORotate(Vector3.zero, settle));
            // Per-tile spark
            seq.OnStart(() => SpawnTileSpark(wp, startColor));
        }

        // Wait maximum stagger duration to keep coroutine alive until all tiles done
        // Estimate max distance to center to compute tail delay
        float maxD = 0f;
        for (int i = 0; i < tiles.Length; i++) if (tiles[i] != null) { float d = Vector3.Distance(tiles[i].transform.position, center); if (d > maxD) maxD = d; }
        float maxDelay = baseDelay + maxD * delayPerUnit + popIn + settle + 0.02f;
        float t0 = Time.time;
        while (Time.time - t0 < maxDelay) yield return null;
    }

    private IEnumerator PlacementSimpleCoroutine(Shape shape)
    {
        shape.CacheTileRenderers();
        var tiles = shape.TileRenderers;
        if (tiles == null || tiles.Length == 0) yield break;

        float dur = 0.14f;
        for (int i = 0; i < tiles.Length; i++)
        {
            var sr = tiles[i]; if (sr == null) continue;
            var baseScale = sr.transform.localScale;
            var baseColor = sr.color;
            // Smooth scale-in from 0.96x to 1.0x, gentle brightness lift
            var seq = DOTween.Sequence();
            seq.Join(sr.transform.DOScale(baseScale * 0.96f, dur * 0.25f).From(baseScale).SetEase(Ease.OutCubic));
            seq.Join(sr.DOColor(Color.Lerp(baseColor, Color.white, 0.15f), dur * 0.5f).From(baseColor).SetEase(Ease.OutSine));
            seq.OnComplete(() => { if (sr != null) { sr.transform.localScale = baseScale; sr.color = baseColor; } });
        }
        yield return new WaitForSeconds(dur + 0.02f);
    }

    private IEnumerator PlacementOrbitCoroutine(Shape shape)
    {
        shape.CacheTileRenderers();
        var tiles = shape.TileRenderers;
        if (tiles == null || tiles.Length == 0) yield break;

        // Center in local space
        Vector3 centerLocal = Vector3.zero;
        int count = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue; centerLocal += tiles[i].transform.localPosition; count++;
        }
        if (count > 0) centerLocal /= count;

        // Animate each tile: slight outward orbit then snap back
        for (int i = 0; i < tiles.Length; i++)
        {
            var sr = tiles[i]; if (sr == null) continue;
            Vector3 finalLocal = sr.transform.localPosition;
            Vector3 dir = (finalLocal - centerLocal).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle.normalized;
            float outDist = orbitOutDistance * Random.Range(0.9f, 1.15f);
            // Pick an out point rotated a bit
            float rot = Random.Range(-35f, 35f) * Mathf.Deg2Rad;
            Vector3 rdir = new Vector3(
                dir.x * Mathf.Cos(rot) - dir.y * Mathf.Sin(rot),
                dir.x * Mathf.Sin(rot) + dir.y * Mathf.Cos(rot),
                0f);
            Vector3 outLocal = finalLocal + rdir * outDist;
            float d = Vector3.Distance(finalLocal, centerLocal);
            float delay = d * Mathf.Max(0f, orbitStaggerPerUnit);

            var seq = DOTween.Sequence();
            seq.SetDelay(delay);
            // trail from outLocal to final world pos
            Vector3 worldFinal = sr.transform.position;
            Vector3 worldOut = shape.transform.TransformPoint(outLocal);
            seq.OnStart(() => SpawnTileTrail(worldOut, worldFinal, sr.color));
            // jump out then return
            seq.Append(sr.transform.DOLocalMove(outLocal, Mathf.Max(0.01f, orbitOutDuration)).SetEase(Ease.OutQuad));
            // slight spin while returning
            seq.Append(sr.transform.DOLocalMove(finalLocal, Mathf.Max(0.01f, orbitReturnDuration)).SetEase(Ease.OutBack));
            seq.Join(sr.transform.DORotate(new Vector3(0,0, Random.Range(-15f,15f)), orbitReturnDuration * 0.9f));
            // subtle white flash
            seq.Join(sr.DOColor(Color.white, orbitReturnDuration * 0.4f).From(sr.color));
            // spark on settle
            seq.OnComplete(() => SpawnTileSpark(worldFinal, sr.color));
        }

        // Rough wait for the longest tween to finish
        float maxD = 0f;
        for (int i = 0; i < tiles.Length; i++) if (tiles[i] != null) { float d = Vector3.Distance(tiles[i].transform.localPosition, centerLocal); if (d > maxD) maxD = d; }
        float wait = maxD * orbitStaggerPerUnit + orbitOutDuration + orbitReturnDuration + 0.05f;
        float t0 = Time.time; while (Time.time - t0 < wait) yield return null;
    }

    private IEnumerator PlacementMagneticCoroutine(Shape shape)
    {
        // Magnetic snap: quick squish toward center then expand to final, with per-tile glow
        shape.CacheTileRenderers(); var tiles = shape.TileRenderers; if (tiles == null || tiles.Length == 0) yield break;
        Vector3 center = shape.transform.position;
        float squish = 0.1f; float durIn = 0.08f; float durOut = 0.16f;
        for (int i = 0; i < tiles.Length; i++)
        {
            var sr = tiles[i]; if (sr == null) continue;
            Vector3 p = sr.transform.position;
            Vector3 toward = Vector3.Lerp(p, center, 0.25f);
            var seq = DOTween.Sequence();
            seq.Append(sr.transform.DOMove(toward, durIn).SetEase(Ease.InQuad));
            seq.Join(sr.transform.DOScale(sr.transform.localScale * (1f - squish), durIn));
            seq.Append(sr.transform.DOMove(p, durOut).SetEase(Ease.OutBack));
            seq.Join(sr.transform.DOScale(sr.transform.localScale, durOut));
            seq.Join(sr.DOColor(Color.white, durOut * 0.4f).From(sr.color));
            seq.OnStart(() => SpawnTileSpark(p, sr.color));
        }
        yield return new WaitForSeconds(durIn + durOut + 0.05f);
    }

    private void SpawnTileTrail(Vector3 worldStart, Vector3 worldEnd, Color tint)
    {
        var go = new GameObject("PlacementTrailFX");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = new Color(tint.r, tint.g, tint.b, 0.6f);
        sr.sortingOrder = 255;
        Vector3 mid = (worldStart + worldEnd) * 0.5f;
        go.transform.position = mid;
        Vector3 dir = worldEnd - worldStart; float len = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0,0,angle);
        go.transform.localScale = new Vector3(len, 0.05f, 1f);
        var seq = DOTween.Sequence();
        seq.Join(sr.DOFade(0f, 0.22f).From(0.6f));
        seq.Join(go.transform.DOScaleY(0.0f, 0.22f));
        seq.OnComplete(() => Destroy(go));
    }

    private void TryPlayGridCrossRipple(Vector3 worldCenter)
    {
        var gm = Services.Has<GridManager>() ? Services.Get<GridManager>() : Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;
        var gridPos = gm.WorldToGridPosition(worldCenter);
        Vector3 rowStart = gm.GridToWorldPosition(new Vector2Int(0, gridPos.y));
        Vector3 rowEnd = gm.GridToWorldPosition(new Vector2Int(gm.GridWidth - 1, gridPos.y));
        Vector3 colStart = gm.GridToWorldPosition(new Vector2Int(gridPos.x, 0));
        Vector3 colEnd = gm.GridToWorldPosition(new Vector2Int(gridPos.x, gm.GridHeight - 1));
        PlayLineSweep(rowStart, rowEnd, 0.18f, 0.08f);
        PlayLineSweep(colStart, colEnd, 0.18f, 0.08f);
    }

    private void SpawnTileSpark(Vector3 worldPos, Color tint)
    {
        var go = new GameObject("PlacementSparkFX");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = new Color(tint.r, tint.g, tint.b, 0.9f);
        sr.sortingOrder = 260;
        go.transform.position = worldPos + new Vector3(0f, 0f, 0f);
        float life = 0.25f;
        Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.35f).normalized;
        float dist = Random.Range(0.15f, 0.35f);
        go.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
        var seq = DOTween.Sequence();
        seq.Join(go.transform.DOMove(worldPos + new Vector3(dir.x, dir.y, 0f) * dist, life).SetEase(Ease.OutQuad));
        seq.Join(go.transform.DOScale(0.02f, life).SetEase(Ease.OutSine));
        seq.Join(sr.DOFade(0f, life).From(0.9f));
        seq.OnComplete(() => Destroy(go));
    }

    private void PlayPlacementRing(Vector3 worldPosition)
    {
        var go = new GameObject("PlacementRingFX");
        go.transform.position = worldPosition;
        var sr = go.AddComponent<SpriteRenderer>();
        // Use a safe built-in sprite; no dependency on Knob.psd
        sr.sprite = GetWhiteSprite();
        sr.color = new Color(1f, 1f, 1f, 0.5f);
        sr.sortingOrder = 250;
        float dur = 0.35f;
        go.transform.localScale = Vector3.zero;
        if (sr.sprite != null)
        {
            var seq = DOTween.Sequence();
            seq.Join(go.transform.DOScale(1.6f, dur).SetEase(Ease.OutQuad));
            seq.Join(sr.DOFade(0f, dur).From(0.5f).SetEase(Ease.InSine));
            seq.OnComplete(() => Destroy(go));
        }
        else
        {
            Destroy(go);
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

    /// <summary>
    /// Play a short move SFX at position when dragging begins.
    /// </summary>
    public void PlayMoveAt(Vector3 worldPosition, SpriteTheme theme)
    {
        // Choose clip: theme move > default move > theme placement > default placement
        AudioClip clip = null;
        if (theme != null && theme.moveSound != null)
            clip = theme.moveSound;
        else if (defaultMoveSound != null)
            clip = defaultMoveSound;
        else if (theme != null && theme.placementSound != null)
            clip = theme.placementSound;
        else
            clip = defaultPlacementSound;
        if (clip != null)
        {
            PlaySfx2DRespectingMute(clip, moveSfxVolume);
        }
    }

    private const string SfxMuteKey = "Audio.SfxMuted";
    private static bool IsSfxMuted() => PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;
    private static void PlaySfxRespectingMute(Vector3 position, AudioClip clip, float volume = 1f)
    {
        if (clip == null || IsSfxMuted()) return;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    private void PlaySfx2DRespectingMute(AudioClip clip, float volume = 1f)
    {
        if (clip == null || IsSfxMuted()) return;
        if (_oneShot2D != null)
        {
            _oneShot2D.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
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

    // --- Adventure/Level helpers ---
    /// <summary>
    /// Find a theme by its name (case-insensitive). Returns null if not found.
    /// </summary>
    public SpriteTheme FindThemeByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < spriteThemes.Count; i++)
        {
            var t = spriteThemes[i];
            if (t != null && string.Equals(t.themeName, name, System.StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    /// <summary>
    /// Get all theme names for level generation.
    /// </summary>
    public List<string> GetThemeNames()
    {
        var list = new List<string>();
        for (int i = 0; i < spriteThemes.Count; i++)
        {
            var t = spriteThemes[i];
            if (t != null && !string.IsNullOrEmpty(t.themeName)) list.Add(t.themeName);
        }
        return list;
    }
}
