using UnityEngine;
using System.Collections.Generic;
using Core;

[ExecuteAlways]
public class ShapeSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] shapePrefabs; // Array of shape prefabs to spawn
    [SerializeField] private Transform[] spawnPoints = new Transform[3]; // 3 spawn positions
    [SerializeField] private bool autoSpawnOnStart = true;
    
    [Header("Adaptive Assist")]
    [Range(0f, 1f)]
    [SerializeField] private float assistLevel = 0.6f; // 0=random valid, 1=most helpful

    [Header("Editor Preview & Layout")]
    [SerializeField] private bool alignSpawnPointsVertically = true;
    [Tooltip("Spacing between spawn points in world units (Y).")]
    [SerializeField] private float verticalSpacing = 2f;
    [Tooltip("Use this X value to vertically align all spawn points. If 0, uses this GameObject's X.")]
    [SerializeField] private float alignAtX = 0f;

    [SerializeField] private bool alignSpawnPointsHorizontally = false;
    [Tooltip("Spacing between spawn points in world units (X).")]
    [SerializeField] private float horizontalSpacing = 2f;
    [Tooltip("Use this Y value to horizontally align all spawn points. If 0, uses this GameObject's Y.")]
    [SerializeField] private float alignAtY = 0f;

    [SerializeField] private bool showSpawnGizmos = true;
    [SerializeField] private Color spawnGizmoColor = new Color(0.3f, 0.9f, 1f, 0.6f);
    [SerializeField] private Vector2 previewSize = new Vector2(2f, 2f);
    [Tooltip("When spawning, shift shapes so their bounds center aligns with the spawn gizmo center.")]
    [SerializeField] private bool centerSpawnedShapesInGizmo = true;
    
    [Header("Sprite Theme Settings")]
    [SerializeField] private ShapeSpriteManager spriteManager; // Reference to sprite manager
    [SerializeField] private bool useRandomThemes = true; // Whether to apply random themes to spawned shapes
    
    [Header("Selection Mode")]
    [Tooltip("If enabled, picking shapes is deterministic (no randomness) and avoids repeats.")]
    [SerializeField] private bool useDeterministicSelection = true;
    [Tooltip("Avoid duplicates within the same set of 3 spawns.")]
    [SerializeField] private bool preventDuplicatesInSet = true;
    [Tooltip("Ensure each set includes at least one placeable shape.")]
    [SerializeField] private bool ensurePlaceableInSet = true;
    [Tooltip("Keep a short recent history to avoid immediate repeats across sets (0 disables).")]
    [SerializeField, Min(0)] private int noRepeatWindow = 4;
    [Tooltip("Log deterministic selection details for debugging.")]
    [SerializeField] private bool debugSelection = false;
    [Tooltip("Avoid picking multiple shapes from the same family (e.g., L/I/T) within one set of 3.")]
    [SerializeField] private bool preventSameFamilyInSet = true;
    [Tooltip("Optional family labels aligned with shapePrefabs (e.g., L, I, T, O, Z). Empty entries fallback to prefab name prefix.")]
    [SerializeField] private string[] shapeFamilyLabels = new string[0];

    [Header("Shape Variants (Orientation)")]
    [Tooltip("Allow rotated/mirrored variants when spawning so sets don't look identical.")]
    [SerializeField] private bool enableOrientationVariants = true;
    [Tooltip("Include 90° rotations.")]
    [SerializeField] private bool allowRotate90 = true;
    [Tooltip("Include 180° rotation.")]
    [SerializeField] private bool allowRotate180 = true;
    [Tooltip("Include 270° rotation.")]
    [SerializeField] private bool allowRotate270 = true;
    [Tooltip("Include horizontal mirror (flip X).")]
    [SerializeField] private bool allowMirrorX = true;
    [Tooltip("Include vertical mirror (flip Y).")]
    [SerializeField] private bool allowMirrorY = false;

    [Header("Spawn Effects")]
    [SerializeField] private float spawnEffectDuration = 0.3f;
    [SerializeField] private float minSpawnScale = 0.1f;
    [SerializeField] private float maxSpawnScale = 1.0f;
    
    [Header("Legacy Settings")]
    [SerializeField] private float spawnCheckInterval = 2f; // How often to check if all shapes are placed (reduced frequency)
    
    private GameObject[] currentShapes = new GameObject[3]; // Currently spawned shapes
    private bool allShapesPlaced = false;
    private float lastCheckTime = 0f;
    private bool[] shapeStatusCache = new bool[3]; // Cache shape placement status
    [SerializeField] private bool centerByRenderers = true;

    // Deterministic bag state
    private readonly List<int> bag = new List<int>();
    private int bagCursor = 0;
    private readonly Queue<int> recentIndices = new Queue<int>();
    private readonly Queue<int> deferredIndices = new Queue<int>();

    [Header("Shape Size Control")]
    [Tooltip("Global base scale applied to all spawned shapes before spawn effect.")]
    [SerializeField] private Vector3 globalShapeScale = Vector3.one;
    [Tooltip("Optional per-slot scale overrides (size 3). Leave zero to use global scale.")]
    [SerializeField] private Vector3[] perSlotScale = new Vector3[3];
    [Tooltip("Whether to multiply spawn effect by the base scale or animate from it.")]
    [SerializeField] private bool spawnEffectFromBaseScale = true;

    [Header("Anti-Overlap in Tray")]
    [Tooltip("Clamp spawned shapes to fit within the preview box to avoid overlapping neighbors.")]
    [SerializeField] private bool fitShapesToPreviewBox = true;
    [Tooltip("Padding inside the preview box when fitting, in world units.")]
    [SerializeField] private float previewPadding = 0.05f;
    [Tooltip("Ensure spawn point spacing is at least the preview size + margin to avoid overlaps.")]
    [SerializeField] private bool enforceMinSpacingFromPreview = true;
    [Tooltip("Extra margin added on top of preview size for spacing checks (world units).")]
    [SerializeField] private float spacingMargin = 0.1f;

    [Tooltip("Adjust child sprite sorting orders per slot for clear layering in the tray.")]
    [SerializeField] private bool setSpawnSortingOrders = true;
    [SerializeField] private int spawnSortingOrderBase = 0;
    [SerializeField] private int spawnSortingOrderStep = 1;

    [Tooltip("Clamp the spawn 'pop' so it never exceeds the preview box.")]
    [SerializeField] private bool clampSpawnEffectToPreview = true;

    private void OnEnable()
    {
        // Keep layout tidy in editor
        AlignSpawnPointsIfNeeded();
    }

    private void OnValidate()
    {
        AlignSpawnPointsIfNeeded();
        // Keep family labels array aligned with prefabs for inspector setup
        if (shapePrefabs != null)
        {
            if (shapeFamilyLabels == null || shapeFamilyLabels.Length != shapePrefabs.Length)
            {
                var newArr = new string[shapePrefabs.Length];
                if (shapeFamilyLabels != null)
                {
                    for (int i = 0; i < Mathf.Min(shapeFamilyLabels.Length, newArr.Length); i++) newArr[i] = shapeFamilyLabels[i];
                }
                shapeFamilyLabels = newArr;
            }
        }
    }
    
    void Start()
    {
        if (!Application.isPlaying) return;

        // Validate spawn points
        if (spawnPoints.Length != 3)
        {
            Debug.LogError("ShapeSpawner requires exactly 3 spawn points!");
            return;
        }
        
        // Subscribe to line clearing events to check for shape completion
        Gameplay.LineClearSystem.OnLinesCleared += OnLinesCleared;
        
        if (autoSpawnOnStart)
        {
            SpawnNewShapes();
        }
    }
    
    void OnDestroy()
    {
        if (!Application.isPlaying) return;
        // Unsubscribe from events
        Gameplay.LineClearSystem.OnLinesCleared -= OnLinesCleared;
    }

    private void AlignSpawnPointsIfNeeded()
    {
        if (spawnPoints == null || spawnPoints.Length < 3) return;

        // Optional spacing enforcement based on preview box
        if (enforceMinSpacingFromPreview)
        {
            if (alignSpawnPointsHorizontally)
            {
                float minSpace = previewSize.x + Mathf.Abs(spacingMargin);
                if (horizontalSpacing < minSpace) horizontalSpacing = minSpace;
            }
            else if (alignSpawnPointsVertically)
            {
                float minSpace = previewSize.y + Mathf.Abs(spacingMargin);
                if (verticalSpacing < minSpace) verticalSpacing = minSpace;
            }
        }

        // Horizontal alignment takes precedence if enabled
        if (alignSpawnPointsHorizontally)
        {
            float baseY = Mathf.Abs(alignAtY) > Mathf.Epsilon ? alignAtY : transform.position.y;

            int leftIndex = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null) { leftIndex = i; break; }
            }
            if (spawnPoints[leftIndex] == null) return;

            float leftX = spawnPoints[leftIndex].position.x;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;
                Vector3 p = spawnPoints[i].position;
                p.y = baseY;
                p.x = leftX + i * Mathf.Abs(horizontalSpacing);
                spawnPoints[i].position = p;
            }
            return;
        }

        if (!alignSpawnPointsVertically) return;
        // Determine base X
        float baseX = Mathf.Abs(alignAtX) > Mathf.Epsilon ? alignAtX : transform.position.x;

        // Use the first non-null point as the top reference Y
        int topIndex = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) { topIndex = i; break; }
        }
        if (spawnPoints[topIndex] == null) return;

        float topY = spawnPoints[topIndex].position.y;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            Vector3 p = spawnPoints[i].position;
            p.x = baseX;
            p.y = topY - i * Mathf.Abs(verticalSpacing);
            spawnPoints[i].position = p;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos || spawnPoints == null) return;
        Gizmos.color = spawnGizmoColor;
        foreach (var t in spawnPoints)
        {
            if (t == null) continue;
            Gizmos.DrawWireCube(t.position, new Vector3(previewSize.x, previewSize.y, 0.01f));
        }

        // Draw index labels
        #if UNITY_EDITOR
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var t = spawnPoints[i];
            if (t == null) continue;
            UnityEditor.Handles.color = spawnGizmoColor;
            UnityEditor.Handles.Label(t.position + Vector3.up * (previewSize.y * 0.6f), $"Spawn {i+1}");
        }
        #endif
    }
    
    void Update()
    {
        if (!Application.isPlaying) return;
        // Only check if we haven't already detected all shapes are placed
        if (!allShapesPlaced && Time.time - lastCheckTime >= spawnCheckInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllShapesPlaced();
        }
    }
    
    private void CheckIfAllShapesPlaced()
    {
        if (!Application.isPlaying) return;
        if (allShapesPlaced) return;
        
        bool allPlaced = true;
        int placedCount = 0;
        bool statusChanged = false;
        
        for (int i = 0; i < currentShapes.Length; i++)
        {
            bool currentStatus = false;
            
            if (currentShapes[i] != null)
            {
                var shapeComponent = currentShapes[i].GetComponent<Core.Shape>();
                if (shapeComponent != null && shapeComponent.IsPlaced)
                {
                    currentStatus = true;
                    placedCount++;
                }
                else
                {
                    allPlaced = false;
                }
            }
            else
            {
                // Shape was destroyed (e.g., by line clearing), count as placed
                currentStatus = true;
                placedCount++;
            }
            
            // Check if status changed to avoid unnecessary updates
            if (shapeStatusCache[i] != currentStatus)
            {
                shapeStatusCache[i] = currentStatus;
                statusChanged = true;
            }
        }
        
        if (allPlaced && placedCount >= 3)
        {
            allShapesPlaced = true;
            Debug.Log("All shapes placed! Spawning new set...");
            
            if (Application.isPlaying)
            {
                // Small delay before spawning new shapes
                Invoke(nameof(SpawnNewShapes), 0.5f);
            }
        }
        else if (statusChanged)
        {
            // Optional: debug log could be noisy; keep only in play mode
            // Debug.Log($"Shapes status update: {placedCount}/3 placed");
        }
    }
    
    private void SpawnNewShapes()
    {
        if (!Application.isPlaying) return;
        allShapesPlaced = false;
        
        for (int i = 0; i < currentShapes.Length; i++)
        {
            currentShapes[i] = null;
            shapeStatusCache[i] = false;
        }

        if (useDeterministicSelection)
        {
            var indices = DetermineNextSetIndices();
            if (debugSelection)
            {
                Debug.Log($"[ShapeSpawner] Next set indices: {indices[0]}, {indices[1]}, {indices[2]} | deferred={deferredIndices.Count} recent={recentIndices.Count} cursor={bagCursor}");
            }
            var newlySpawnedShapes = new List<GameObject>(3);
            for (int i = 0; i < 3; i++)
            {
                if (spawnPoints[i] == null) continue;
                GameObject newShape = SpawnShapeByIndex(indices[i], i);
                currentShapes[i] = newShape;
                if (newShape != null) newlySpawnedShapes.Add(newShape);
                // update recent history
                if (noRepeatWindow > 0)
                {
                    recentIndices.Enqueue(indices[i]);
                    while (recentIndices.Count > noRepeatWindow) recentIndices.Dequeue();
                }
            }
            ApplyThemesToShapes(newlySpawnedShapes.ToArray());
            return;
        }

        // Existing random/adaptive path
        List<GameObject> newlySpawned = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            if (spawnPoints[i] != null)
            {
                GameObject newShape = SpawnRandomShape(i);
                currentShapes[i] = newShape;
                if (newShape != null) newlySpawned.Add(newShape);
            }
        }
        ApplyThemesToShapes(newlySpawned.ToArray());
    }

    private int[] DetermineNextSetIndices()
    {
        int n = shapePrefabs != null ? shapePrefabs.Length : 0;
        var result = new int[3] { 0, 0, 0 };
        if (n == 0) return result;
        RefillBagIfNeeded(n);

        var disallow = new HashSet<int>();
        var usedFamilies = new HashSet<string>();
        // avoid duplicates within set
        for (int i = 0; i < 3; i++)
        {
            int idx = NextIndexPreferDeferred(n, disallow, allowRepeatIfNeeded: false, familyBlock: preventSameFamilyInSet ? usedFamilies : null);
            result[i] = idx;
            if (preventDuplicatesInSet) disallow.Add(idx);
            if (preventSameFamilyInSet)
            {
                var fam = GetFamilyLabel(idx);
                if (!string.IsNullOrEmpty(fam)) usedFamilies.Add(fam);
            }
        }

        if (ensurePlaceableInSet)
        {
            bool anyPlaceable = false;
            var gm = GetGridManager();
            for (int i = 0; i < 3; i++)
            {
                if (IsPrefabPlaceable(result[i], gm)) { anyPlaceable = true; break; }
            }
            if (!anyPlaceable)
            {
                // find a placeable candidate from bag, replace last slot
                for (int tries = 0; tries < n; tries++)
                {
                    HashSet<string> famBlock = null;
                    if (preventSameFamilyInSet)
                    {
                        famBlock = new HashSet<string>();
                        for (int k = 0; k < 3; k++) famBlock.Add(GetFamilyLabel(result[k]));
                    }
                    int candidate = NextIndexPreferDeferred(n, preventDuplicatesInSet ? new HashSet<int>(result) : null, allowRepeatIfNeeded: true, familyBlock: famBlock);
                    if (IsPrefabPlaceable(candidate, gm))
                    {
                        // Defer the replaced index so it shows up soon in next sets
                        int replaced = result[2];
                        if (replaced != candidate) deferredIndices.Enqueue(replaced);
                        result[2] = candidate;
                        break;
                    }
                }
            }
        }
        return result;
    }

    private void RefillBagIfNeeded(int n)
    {
        if (bag.Count != n)
        {
            bag.Clear();
            for (int i = 0; i < n; i++) bag.Add(i);
            bagCursor = 0;
        }
        if (bagCursor >= bag.Count) bagCursor = 0;
    }

    private int NextIndexFromBag(int n, HashSet<int> disallow, bool allowRepeatIfNeeded = false, HashSet<string> familyBlock = null)
    {
        // Try scanning the bag linearly starting at cursor, respecting recent history and disallow set
        for (int pass = 0; pass < 2; pass++)
        {
            for (int step = 0; step < n; step++)
            {
                int idx = bag[(bagCursor + step) % n];
                if (disallow != null && disallow.Contains(idx)) continue;
                if (familyBlock != null)
                {
                    var fam = GetFamilyLabel(idx);
                    if (!string.IsNullOrEmpty(fam) && familyBlock.Contains(fam)) continue;
                }
                if (!allowRepeatIfNeeded && noRepeatWindow > 0 && recentIndices.Contains(idx)) continue;
                bagCursor = (bagCursor + step + 1) % n;
                return idx;
            }
            // On second pass, ignore recent history to guarantee progress
            allowRepeatIfNeeded = true;
        }
        int fallback = bag[bagCursor];
        bagCursor = (bagCursor + 1) % n;
        return fallback;
    }

    private int NextIndexPreferDeferred(int n, HashSet<int> disallow, bool allowRepeatIfNeeded = false, HashSet<string> familyBlock = null)
    {
        // Try deferred indices first to avoid starving shapes that were replaced previously
        int tries = deferredIndices.Count;
        while (tries-- > 0 && deferredIndices.Count > 0)
        {
            int idx = deferredIndices.Dequeue();
            if (disallow != null && disallow.Contains(idx)) { deferredIndices.Enqueue(idx); continue; }
            if (familyBlock != null)
            {
                var fam = GetFamilyLabel(idx);
                if (!string.IsNullOrEmpty(fam) && familyBlock.Contains(fam)) { deferredIndices.Enqueue(idx); continue; }
            }
            if (!allowRepeatIfNeeded && noRepeatWindow > 0 && recentIndices.Contains(idx)) { deferredIndices.Enqueue(idx); continue; }
            // Valid deferred pick
            return idx;
        }
        // Fallback to bag
        return NextIndexFromBag(n, disallow, allowRepeatIfNeeded, familyBlock);
    }

    private string GetFamilyLabel(int prefabIndex)
    {
        if (shapePrefabs == null || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length) return string.Empty;
        if (shapeFamilyLabels != null && shapeFamilyLabels.Length == shapePrefabs.Length)
        {
            var lab = shapeFamilyLabels[prefabIndex];
            if (!string.IsNullOrEmpty(lab)) return lab.Trim();
        }
        // Fallback: derive a family from prefab name prefix up to first '_' or '-'
        var name = shapePrefabs[prefabIndex] != null ? shapePrefabs[prefabIndex].name : string.Empty;
        if (string.IsNullOrEmpty(name)) return string.Empty;
        int cut = name.IndexOf('_');
        if (cut < 0) cut = name.IndexOf('-');
        return cut > 0 ? name.Substring(0, cut) : name;
    }

    private bool IsPrefabPlaceable(int prefabIndex, Gameplay.GridManager gm)
    {
        if (gm == null || shapePrefabs == null || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length) return false;
        var offs = GetOffsets(shapePrefabs[prefabIndex]);
        if (offs == null || offs.Count == 0) return false;
        int W = gm.GridWidth, H = gm.GridHeight;
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                if (gm.CanPlaceShape(new Vector2Int(x, y), offs)) return true;
            }
        }
        return false;
    }

    private GameObject SpawnShapeByIndex(int prefabIndex, int spawnIndex)
    {
        if (shapePrefabs == null || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length) return null;
        var shapePrefab = shapePrefabs[prefabIndex];
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        GameObject spawnedShape = Instantiate(shapePrefab, spawnPosition, Quaternion.identity);

        // Apply base scale control
        Vector3 baseScale = GetBaseScaleForSlot(spawnIndex);
        if (baseScale == Vector3.zero) baseScale = Vector3.one;
        spawnedShape.transform.localScale = baseScale;

        // Apply an orientation variant first so centering/fitting uses final layout
        var shapeComponent = spawnedShape.GetComponent<Core.Shape>();
        if (enableOrientationVariants && shapeComponent != null)
        {
            TryApplyOrientationVariant(shapeComponent, prefabIndex, spawnIndex);
            shapeComponent.CacheTileRenderers();
        }

        // Center in gizmo if requested
        if (centerSpawnedShapesInGizmo)
        {
            TryCenterShapeToSpawn(spawnedShape, spawnPosition);
        }

        // Fit within preview box to avoid overlaps across neighbors
        if (fitShapesToPreviewBox)
        {
            FitShapeIntoPreviewBox(spawnedShape, spawnIndex);
            if (centerSpawnedShapesInGizmo)
            {
                TryCenterByRenderers(spawnedShape, spawnPosition);
            }
        }

        // Sorting orders per slot for clarity
        if (setSpawnSortingOrders)
        {
            ApplySortingForSpawn(spawnedShape, spawnIndex);
        }

        var dragHandler = spawnedShape.GetComponent<Gameplay.DragHandler>();
        if (shapeComponent != null && dragHandler != null)
        {
            spawnedShape.name = $"Shape_{spawnIndex}_{Random.Range(1000, 9999)}";
        }

        // Use the fitted final scale as the base for the spawn effect so it doesn't override fitting
        Vector3 fittedScale = spawnedShape.transform.localScale;
        StartCoroutine(SpawnEffect(spawnedShape, fittedScale));
        return spawnedShape;
    }

    private void TryApplyOrientationVariant(Core.Shape shape, int prefabIndex, int spawnIndex)
    {
        var baseOffsets = new List<Vector2Int>(shape.ShapeOffsets);
        if (baseOffsets == null || baseOffsets.Count == 0) return;

        // Build a deterministic pool of variants per prefab index and spawn slot
        var variants = BuildVariants(baseOffsets);
        if (variants.Count <= 1) return;

        // Deterministic pick by hashing prefabIndex, spawnIndex, and a simple counter from bagCursor
        int seed = prefabIndex * 73856093 ^ spawnIndex * 19349663 ^ bagCursor * 83492791;
        if (!useDeterministicSelection)
        {
            seed = (int)(Random.value * int.MaxValue);
        }
        int pick = Mathf.Abs(seed) % variants.Count;
        var chosen = variants[pick];
        // Avoid identity variant if possible
        if (AreOffsetsEqual(baseOffsets, chosen) && variants.Count > 1)
        {
            pick = (pick + 1) % variants.Count;
            chosen = variants[pick];
        }
        shape.ApplyOffsetsAndRealign(chosen);
    }

    private bool AreOffsetsEqual(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private List<List<Vector2Int>> BuildVariants(List<Vector2Int> baseOffsets)
    {
        var set = new List<List<Vector2Int>>();
        void AddUnique(List<Vector2Int> offs)
        {
            // Keep ordering stable by sorting by x then y to detect duplicates
            offs.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            foreach (var s in set)
            {
                if (AreOffsetsEqual(s, offs)) return;
            }
            set.Add(new List<Vector2Int>(offs));
        }

        // Identity
        AddUnique(new List<Vector2Int>(baseOffsets));

        // Rotations
        if (allowRotate90) AddUnique(Rotate(baseOffsets, 90));
        if (allowRotate180) AddUnique(Rotate(baseOffsets, 180));
        if (allowRotate270) AddUnique(Rotate(baseOffsets, 270));

        // Mirrors
        if (allowMirrorX) AddUnique(Mirror(baseOffsets, x: true));
        if (allowMirrorY) AddUnique(Mirror(baseOffsets, x: false));

        return set;
    }

    private List<Vector2Int> Rotate(List<Vector2Int> offs, int degrees)
    {
        var res = new List<Vector2Int>(offs.Count);
        foreach (var o in offs)
        {
            switch (degrees)
            {
                case 90: res.Add(new Vector2Int(-o.y, o.x)); break;
                case 180: res.Add(new Vector2Int(-o.x, -o.y)); break;
                case 270: res.Add(new Vector2Int(o.y, -o.x)); break;
                default: res.Add(o); break;
            }
        }
        Normalize(res);
        return res;
    }

    private List<Vector2Int> Mirror(List<Vector2Int> offs, bool x)
    {
        var res = new List<Vector2Int>(offs.Count);
        foreach (var o in offs)
        {
            res.Add(x ? new Vector2Int(-o.x, o.y) : new Vector2Int(o.x, -o.y));
        }
        Normalize(res);
        return res;
    }

    private void Normalize(List<Vector2Int> offs)
    {
        // Shift so the minimum x,y becomes 0,0 to keep origin alignment sensible
        int minX = int.MaxValue, minY = int.MaxValue;
        for (int i = 0; i < offs.Count; i++)
        {
            if (offs[i].x < minX) minX = offs[i].x;
            if (offs[i].y < minY) minY = offs[i].y;
        }
        for (int i = 0; i < offs.Count; i++)
        {
            offs[i] = new Vector2Int(offs[i].x - minX, offs[i].y - minY);
        }
    }


    private Gameplay.GridManager GetGridManager()
    {
        if (ColorBlast.Core.Architecture.Services.Has<Gameplay.GridManager>())
            return ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
        return Object.FindFirstObjectByType<Gameplay.GridManager>();
    }

    private List<Vector2Int> GetOffsets(GameObject prefab)
    {
        if (prefab == null) return null;
        var s = prefab.GetComponent<Core.Shape>();
        return s != null ? s.ShapeOffsets : null;
    }

    private int CountValidPlacements(Gameplay.GridManager gm, List<Vector2Int> offs)
    {
        if (gm == null || offs == null || offs.Count == 0) return 0;
        int W = gm.GridWidth;
        int H = gm.GridHeight;
        int count = 0;
        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                var start = new Vector2Int(x, y);
                if (gm.CanPlaceShape(start, offs)) count++;
            }
        }
        return count;
    }

    private Vector3 GetBaseScaleForSlot(int index)
    {
        if (perSlotScale != null && perSlotScale.Length == 3)
        {
            var slot = perSlotScale[index];
            if (slot != Vector3.zero) return Vector3.Scale(globalShapeScale, slot);
        }
        return globalShapeScale;
    }

    private System.Collections.IEnumerator SpawnEffect(GameObject shape, Vector3 baseScale)
    {
        if (shape == null) yield break;
        // If clamped, don't allow growth beyond baseScale; just ease from a smaller value back to base
        Vector3 startScale = spawnEffectFromBaseScale ? (baseScale * minSpawnScale) : (Vector3.one * minSpawnScale);
        Vector3 targetScale;
        if (clampSpawnEffectToPreview)
        {
            targetScale = baseScale; // no growth beyond fitted size
        }
        else
        {
            targetScale = spawnEffectFromBaseScale ? (baseScale * maxSpawnScale) : (baseScale * maxSpawnScale);
        }
        shape.transform.localScale = startScale;
        float elapsed = 0f;
        while (elapsed < spawnEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnEffectDuration;
            t = 1f - (1f - t) * (1f - t);
            shape.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        shape.transform.localScale = targetScale;
    }

    private void TryCenterShapeToSpawn(GameObject shapeGO, Vector3 targetCenter)
    {
        if (shapeGO == null) return;

        if (centerByRenderers)
        {
            if (TryCenterByRenderers(shapeGO, targetCenter)) return;
        }

        var shape = shapeGO.GetComponent<Core.Shape>();
        if (shape == null || shape.ShapeOffsets == null || shape.ShapeOffsets.Count == 0) return;

        float cell = 1f;
        Gameplay.GridManager gm = null;
        if (ColorBlast.Core.Architecture.Services.Has<Gameplay.GridManager>())
            gm = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
        else
            gm = Object.FindFirstObjectByType<Gameplay.GridManager>();
        if (gm != null) cell = gm.CellSize; else if (shape.GridSize > 0f) cell = shape.GridSize;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var o in shape.ShapeOffsets)
        {
            if (o.x < minX) minX = o.x;
            if (o.x > maxX) maxX = o.x;
            if (o.y < minY) minY = o.y;
            if (o.y > maxY) maxY = o.y;
        }
        float centerX = (minX + maxX) * 0.5f * cell;
        float centerY = (minY + maxY) * 0.5f * cell;
        Vector3 worldOffsetFromOrigin = new Vector3(centerX, centerY, 0f);
        Vector3 desired = targetCenter - worldOffsetFromOrigin;
        if (gm != null) desired = gm.SnapToPixel(desired);
        shapeGO.transform.position = desired;
    }

    private bool TryCenterByRenderers(GameObject go, Vector3 targetCenter)
    {
        var renderers = go.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) return false;
        Bounds b = new Bounds(renderers[0].bounds.center, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            b.Encapsulate(renderers[i].bounds);
        }
        Vector3 offset = b.center - go.transform.position;
        Vector3 desired = targetCenter - offset;
        Gameplay.GridManager gm = null;
        if (ColorBlast.Core.Architecture.Services.Has<Gameplay.GridManager>())
            gm = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
        else
            gm = Object.FindFirstObjectByType<Gameplay.GridManager>();
        if (gm != null) desired = gm.SnapToPixel(desired);
        go.transform.position = desired;
        return true;
    }

    private void OnLinesCleared(List<Vector2Int> clearedPositions)
    {
        if (!Application.isPlaying) return;
        CheckIfAllShapesPlaced();
    }

    // Public methods for manual control
    [ContextMenu("Force Spawn New Shapes")]
    public void ForceSpawnNewShapes()
    {
        if (!Application.isPlaying) return;
        SpawnNewShapes();
    }
    
    public void ClearCurrentShapes()
    {
        if (!Application.isPlaying) return;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] != null)
            {
                Destroy(currentShapes[i]);
                currentShapes[i] = null;
            }
        }
        allShapesPlaced = false;
    }

    // Optional: reset deterministic queues/bag, e.g., when restarting a level
    [ContextMenu("Reset Deterministic State")]
    public void ResetDeterministicState()
    {
        bagCursor = 0;
        bag.Clear();
        recentIndices.Clear();
        deferredIndices.Clear();
    }
    
    public bool AreAllShapesPlaced()
    {
        return allShapesPlaced;
    }
    
    public int GetPlacedShapeCount()
    {
        int count = 0;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] != null)
            {
                var shapeComponent = currentShapes[i].GetComponent<Core.Shape>();
                if (shapeComponent != null && shapeComponent.IsPlaced)
                {
                    count++;
                }
            }
            else
            {
                count++; // Destroyed shapes count as placed
            }
        }
        return count;
    }
    
    // Helper method to set spawn points programmatically
    public void SetSpawnPoints(Transform[] points)
    {
        if (points != null && points.Length == 3)
        {
            spawnPoints = points;
        }
        else
        {
            Debug.LogError("Must provide exactly 3 spawn points!");
        }
    }
    
    private void ApplyThemesToShapes(GameObject[] shapes)
    {
        if (!Application.isPlaying) return;
        if (!useRandomThemes) return;
        if (shapes == null || shapes.Length == 0) return;
        
        if (spriteManager != null)
        {
            spriteManager.ApplyRandomThemes(shapes);
        }
        else if (ShapeSpriteManager.Instance != null)
        {
            ShapeSpriteManager.Instance.ApplyRandomThemes(shapes);
        }
        else
        {
            Debug.LogWarning("No ShapeSpriteManager available! Shapes will use default sprites.");
        }
    }
    
    public void ApplyThemeToShape(GameObject shape, string themeName)
    {
        if (!Application.isPlaying) return;
        ShapeSpriteManager manager = spriteManager ?? ShapeSpriteManager.Instance;
        if (manager != null)
        {
            var theme = manager.GetThemeByName(themeName);
            if (theme != null)
            {
                manager.ApplyThemeToShape(shape, theme);
            }
        }
    }
    
    // Helper method to add shape prefabs
    public void SetShapePrefabs(GameObject[] prefabs)
    {
        shapePrefabs = prefabs;
    }

    // Random/adaptive spawn helper used when deterministic mode is off
    private GameObject SpawnRandomShape(int spawnIndex)
    {
        if (!Application.isPlaying) return null;
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("No shape prefabs assigned to ShapeSpawner!");
            return null;
        }

        int selectedIndex = -1;
        // Try adaptive helper if present
        var selectorType = System.Type.GetType("AdaptiveShapeSelector");
        if (selectorType != null)
        {
            var method = selectorType.GetMethod("SelectPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                try
                {
                    var prefab = (GameObject)method.Invoke(null, new object[] { shapePrefabs, assistLevel });
                    if (prefab != null)
                    {
                        selectedIndex = System.Array.IndexOf(shapePrefabs, prefab);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Adaptive selection failed: {ex.Message}. Falling back to random.");
                }
            }
        }

        if (selectedIndex < 0)
        {
            selectedIndex = Random.Range(0, shapePrefabs.Length);
        }

        return SpawnShapeByIndex(selectedIndex, spawnIndex);
    }

    private void FitShapeIntoPreviewBox(GameObject shapeGO, int spawnIndex)
    {
        if (shapeGO == null) return;
        var srs = shapeGO.GetComponentsInChildren<SpriteRenderer>();
        if (srs == null || srs.Length == 0) return;
        Bounds b = new Bounds(srs[0].bounds.center, Vector3.zero);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] != null) b.Encapsulate(srs[i].bounds);
        }
        // Current world size
        float bw = Mathf.Max(0.0001f, b.size.x);
        float bh = Mathf.Max(0.0001f, b.size.y);

        // Allowed box size (world units)
        float maxW = Mathf.Max(0.0001f, previewSize.x - Mathf.Abs(previewPadding));
        float maxH = Mathf.Max(0.0001f, previewSize.y - Mathf.Abs(previewPadding));
        float scale = Mathf.Min(maxW / bw, maxH / bh);
        if (scale < 1f)
        {
            shapeGO.transform.localScale *= scale;
        }
    }

    private void ApplySortingForSpawn(GameObject shapeGO, int spawnIndex)
    {
        if (shapeGO == null) return;
        var srs = shapeGO.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null) return;
        int add = spawnSortingOrderBase + spawnIndex * spawnSortingOrderStep;
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] == null) continue;
            srs[i].sortingOrder += add;
        }
    }
}
