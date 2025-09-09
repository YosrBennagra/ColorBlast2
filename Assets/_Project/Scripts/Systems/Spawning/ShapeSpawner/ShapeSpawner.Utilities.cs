using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColorBlast.Game;
using ColorBlast.Core.Architecture;

public partial class ShapeSpawner
{
    private Gameplay.GridManager GetGridManager()
    {
        if (Services.Has<Gameplay.GridManager>())
            return Services.Get<Gameplay.GridManager>();
        return Object.FindFirstObjectByType<Gameplay.GridManager>();
    }

    private List<Vector2Int> GetOffsets(GameObject prefab)
    {
        if (prefab == null) return null;
        var s = prefab.GetComponent<Shape>();
        return s != null ? s.ShapeOffsets : null;
    }

    private int CountValidPlacements(Gameplay.GridManager gm, List<Vector2Int> offs)
    {
        if (gm == null || offs == null || offs.Count == 0) return 0;
        int W = gm.GridWidth, H = gm.GridHeight;
        int count = 0;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                if (gm.CanPlaceShape(new Vector2Int(x, y), offs)) count++;
        return count;
    }

    // Variant-aware placement counting: tries all allowed rotations/mirrors and returns the max placements among variants
    private int CountValidPlacementsConsideringVariants(Gameplay.GridManager gm, List<Vector2Int> baseOffsets)
    {
        if (gm == null || baseOffsets == null || baseOffsets.Count == 0) return 0;
        if (!enableOrientationVariants)
        {
            return CountValidPlacements(gm, baseOffsets);
        }
        int best = 0;
        var variants = BuildVariants(baseOffsets);
        for (int i = 0; i < variants.Count; i++)
        {
            int c = CountValidPlacements(gm, variants[i]);
            if (c > best) best = c;
            if (best > 0 && variants.Count == 1) break; // micro short-circuit
        }
        return best;
    }

    // Pick a big shape by tile count threshold; optionally require at least one valid placement.
    private int FindBigShapeIndex(int minTiles, bool preferPlaceable)
    {
        if (shapePrefabs == null || shapePrefabs.Length == 0) return -1;
        var gm = GetGridManager();
        var candidates = new List<int>();
        for (int i = 0; i < shapePrefabs.Length; i++)
        {
            var offs = GetOffsets(shapePrefabs[i]);
            if (offs == null) continue;
            if (offs.Count >= Mathf.Max(1, minTiles))
            {
                if (!preferPlaceable || (gm != null && CountValidPlacementsConsideringVariants(gm, offs) > 0))
                {
                    candidates.Add(i);
                }
            }
        }
        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
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

    private IEnumerator SpawnEffect(GameObject shape, Vector3 baseScale)
    {
        if (shape == null) yield break;
        Vector3 startScale = spawnEffectFromBaseScale ? (baseScale * minSpawnScale) : (Vector3.one * minSpawnScale);
        Vector3 targetScale = clampSpawnEffectToPreview ? baseScale : (baseScale * maxSpawnScale);
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

    private void TryApplyOrientationVariant(Shape shape, int prefabIndex, int spawnIndex)
    {
        var baseOffsets = new List<Vector2Int>(shape.ShapeOffsets);
        if (baseOffsets == null || baseOffsets.Count == 0) return;
        var variants = BuildVariants(baseOffsets);
        if (variants.Count <= 1) return;

        // Prefer a placeable variant on the current board
        var gm = GetGridManager();
        List<Vector2Int> bestVar = null;
        if (gm != null)
        {
            bool hasValid = false;
            float bestScore = float.NegativeInfinity;
            var snap = BoardScoring.CreateSnapshot(gm);
            for (int i = 0; i < variants.Count; i++)
            {
                bool hv;
                float sc = BoardScoring.EvaluateBestPlacementScore(snap, variants[i], out hv);
                if (hv && sc > bestScore)
                {
                    bestScore = sc; bestVar = variants[i]; hasValid = true;
                }
            }
            if (hasValid && bestVar != null)
            {
                shape.ApplyOffsetsAndRealign(bestVar);
                return;
            }
        }

        // Fallback to deterministic/random variant if none are placeable
        int seed = prefabIndex * 73856093 ^ spawnIndex * 19349663 ^ bagCursor * 83492791;
        if (!useDeterministicSelection) seed = (int)(Random.value * int.MaxValue);
        int pick = Mathf.Abs(seed) % variants.Count;
        var fallback = variants[pick];
        if (AreOffsetsEqual(baseOffsets, fallback) && variants.Count > 1)
        { pick = (pick + 1) % variants.Count; fallback = variants[pick]; }
        shape.ApplyOffsetsAndRealign(fallback);
    }

    private bool AreOffsetsEqual(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }

    private List<List<Vector2Int>> BuildVariants(List<Vector2Int> baseOffsets)
    {
        var set = new List<List<Vector2Int>>();
        void AddUnique(List<Vector2Int> offs)
        {
            offs.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            foreach (var s in set) { if (AreOffsetsEqual(s, offs)) return; }
            set.Add(new List<Vector2Int>(offs));
        }
        AddUnique(new List<Vector2Int>(baseOffsets));
        if (allowRotate90) AddUnique(Rotate(baseOffsets, 90));
        if (allowRotate180) AddUnique(Rotate(baseOffsets, 180));
        if (allowRotate270) AddUnique(Rotate(baseOffsets, 270));
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
        Normalize(res); return res;
    }

    private List<Vector2Int> Mirror(List<Vector2Int> offs, bool x)
    {
        var res = new List<Vector2Int>(offs.Count);
        foreach (var o in offs) res.Add(x ? new Vector2Int(-o.x, o.y) : new Vector2Int(o.x, -o.y));
        Normalize(res); return res;
    }

    private void Normalize(List<Vector2Int> offs)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        for (int i = 0; i < offs.Count; i++) { if (offs[i].x < minX) minX = offs[i].x; if (offs[i].y < minY) minY = offs[i].y; }
        for (int i = 0; i < offs.Count; i++) offs[i] = new Vector2Int(offs[i].x - minX, offs[i].y - minY);
    }

    private void FitShapeIntoPreviewBox(GameObject shapeGO, int spawnIndex)
    {
        if (shapeGO == null) return;
        var srs = shapeGO.GetComponentsInChildren<SpriteRenderer>();
        if (srs == null || srs.Length == 0) return;
        Bounds b = new Bounds(srs[0].bounds.center, Vector3.zero);
        for (int i = 0; i < srs.Length; i++) if (srs[i] != null) b.Encapsulate(srs[i].bounds);
        float bw = Mathf.Max(0.0001f, b.size.x);
        float bh = Mathf.Max(0.0001f, b.size.y);
        float maxW = Mathf.Max(0.0001f, previewSize.x - Mathf.Abs(previewPadding));
        float maxH = Mathf.Max(0.0001f, previewSize.y - Mathf.Abs(previewPadding));
        float scale = Mathf.Min(maxW / bw, maxH / bh);
        if (scale < 1f) shapeGO.transform.localScale *= scale;
    }

    private void ApplySortingForSpawn(GameObject shapeGO, int spawnIndex)
    {
        if (shapeGO == null) return;
        var srs = shapeGO.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null) return;
        int add = spawnSortingOrderBase + spawnIndex * spawnSortingOrderStep;
        for (int i = 0; i < srs.Length; i++) { if (srs[i] == null) continue; srs[i].sortingOrder += add; }
    }

    private void TryCenterShapeToSpawn(GameObject shapeGO, Vector3 targetCenter)
    {
        if (shapeGO == null) return;
        if (centerByRenderers && TryCenterByRenderers(shapeGO, targetCenter)) return;
        var shape = shapeGO.GetComponent<Shape>();
        if (shape == null || shape.ShapeOffsets == null || shape.ShapeOffsets.Count == 0) return;
        float cell = 1f;
        var gm = GetGridManager();
        if (gm != null) cell = gm.CellSize; else if (shape.GridSize > 0f) cell = shape.GridSize;
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var o in shape.ShapeOffsets)
        { if (o.x < minX) minX = o.x; if (o.x > maxX) maxX = o.x; if (o.y < minY) minY = o.y; if (o.y > maxY) maxY = o.y; }
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
        for (int i = 0; i < renderers.Length; i++) { if (renderers[i] == null) continue; b.Encapsulate(renderers[i].bounds); }
        Vector3 offset = b.center - go.transform.position;
        Vector3 desired = targetCenter - offset;
        var gm = GetGridManager();
        if (gm != null) desired = gm.SnapToPixel(desired);
        go.transform.position = desired;
        return true;
    }

    // Scoring heuristic similar to AdaptiveShapeSelector for internal use
    private float EvaluateBestPlacementScoreForOffsets(Gameplay.GridManager gm, List<Vector2Int> offsets, ref bool hasValid)
    {
        var snap = BoardScoring.CreateSnapshot(gm);
        return BoardScoring.EvaluateBestPlacementScore(snap, offsets, out hasValid);
    }

    private float ScorePlacement(Gameplay.GridManager gm, HashSet<Vector2Int> occupied, List<Vector2Int> offsets, Vector2Int start)
    {
        // Legacy signature kept for callers; internally use BoardScoring with a temp snapshot
        var snap = BoardScoring.CreateSnapshot(gm);
        return BoardScoring.ScorePlacementFast(snap, offsets, start);
    }

    // Public setter for spawn points (renamed to avoid any ambiguous resolution with older signatures)
    public void AssignSpawnPoints(Transform[] points)
    {
        if (points != null && points.Length == 3) spawnPoints = points; else Debug.LogError("Must provide exactly 3 spawn points!");
    }
}
