using UnityEngine;
using System.Collections.Generic;

public partial class ShapeSpawner
{
    private int[] DetermineNextSetIndices()
    {
        int n = shapePrefabs != null ? shapePrefabs.Length : 0;
        var result = new int[3] { 0, 0, 0 };
        if (n == 0) return result;
        RefillBagIfNeeded(n);

        if (useAdaptiveSetComposition)
        {
            return ComposeAdaptiveSet(n);
        }

        var disallow = new HashSet<int>();
        var usedFamilies = new HashSet<string>();
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
                // replace last with a placeable candidate, defer the replaced
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
        if (useDeterministicSelection && randomizeInitialDeterministicBag && !initialBagRandomized && Application.isPlaying)
        {
            ShuffleBag();
            if (randomizeInitialCursor && bag.Count > 0)
            {
                bagCursor = Random.Range(0, bag.Count);
            }
            initialBagRandomized = true;
        }
        if (bagCursor >= bag.Count) bagCursor = 0;
    }

    private void ShuffleBag()
    {
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = bag[i];
            bag[i] = bag[j];
            bag[j] = tmp;
        }
    }

    private int NextIndexFromBag(int n, HashSet<int> disallow, bool allowRepeatIfNeeded = false, HashSet<string> familyBlock = null)
    {
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
            allowRepeatIfNeeded = true;
        }
        int fallback = bag[bagCursor];
        bagCursor = (bagCursor + 1) % n;
        return fallback;
    }

    private int NextIndexPreferDeferred(int n, HashSet<int> disallow, bool allowRepeatIfNeeded = false, HashSet<string> familyBlock = null)
    {
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
            return idx;
        }
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
        // If variants are enabled, consider all allowed orientations
        if (enableOrientationVariants)
        {
            var variants = BuildVariants(new List<Vector2Int>(offs));
            foreach (var v in variants)
            {
                int W = gm.GridWidth, H = gm.GridHeight;
                for (int x = 0; x < W; x++)
                    for (int y = 0; y < H; y++)
                        if (gm.CanPlaceShape(new Vector2Int(x, y), v)) return true;
            }
            return false;
        }
        else
        {
            int W = gm.GridWidth, H = gm.GridHeight;
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    if (gm.CanPlaceShape(new Vector2Int(x, y), offs)) return true;
            return false;
        }
    }

    // Adaptive set: one high-score shape, one variety, one challenge (if safe)
    private int[] ComposeAdaptiveSet(int n)
    {
        var gm = GetGridManager();
        var result = new int[3] { 0, 0, 0 };
        if (gm == null || shapePrefabs == null || shapePrefabs.Length == 0)
        {
            // fallback to bag logic
            return new int[3]
            {
                NextIndexPreferDeferred(n, null),
                NextIndexPreferDeferred(n, null),
                NextIndexPreferDeferred(n, null)
            };
        }

        // Score each prefab by best placement; collect valid ones (variant-aware)
        var valid = new List<(int idx, float score, int tiles)>();
        for (int i = 0; i < shapePrefabs.Length; i++)
        {
            var offs = GetOffsets(shapePrefabs[i]);
            if (offs == null || offs.Count == 0) continue;
            int tiles = offs.Count;
            int count = CountValidPlacementsConsideringVariants(gm, offs);
            if (count == 0) continue;
            bool hv = false;
            // Evaluate using the best variant's placement score
            float best = float.NegativeInfinity;
            if (enableOrientationVariants)
            {
                var variants = BuildVariants(new List<Vector2Int>(offs));
                var snap = BoardScoring.CreateSnapshot(gm);
                foreach (var v in variants)
                {
                    bool h;
                    float s = BoardScoring.EvaluateBestPlacementScore(snap, v, out h);
                    if (h) { hv = true; if (s > best) best = s; }
                }
            }
            else
            {
                best = EvaluateBestPlacementScoreForOffsets(gm, offs, ref hv);
            }
            if (hv) valid.Add((i, best, tiles));
        }
        if (valid.Count == 0)
        {
            return new int[3]
            {
                NextIndexPreferDeferred(n, null),
                NextIndexPreferDeferred(n, null),
                NextIndexPreferDeferred(n, null)
            };
        }

    // 1) Best helper - as difficulty rises, slightly bias toward picks that don't trivially create clears
    valid.Sort((a, b) => b.score.CompareTo(a.score));
        result[0] = valid[0].idx;

        // 2) Variety: avoid same family and size; pick mid-ranked
        string fam0 = GetFamilyLabel(result[0]);
        var variety = new List<(int idx, float score, int tiles)>();
        foreach (var v in valid)
        {
            if (v.idx == result[0]) continue;
            if (preventSameFamilyInSet && GetFamilyLabel(v.idx) == fam0) continue;
            if (v.tiles == valid[0].tiles) continue;
            variety.Add(v);
        }
        if (variety.Count == 0) variety = valid;
        int mid = Mathf.Clamp(variety.Count / 2, 0, variety.Count - 1);
        result[1] = variety[mid].idx;

    // 3) Challenge: at higher difficulty, prefer larger or mid-large piece to increase commitment
    var bySize = new List<(int idx, float score, int tiles)>(valid);
    bySize.Sort((a, b) => a.tiles.CompareTo(b.tiles));
    int pickIndex = 0; // smallest by default
    if (Mathf.Clamp01(difficulty) > 0.4f) pickIndex = Mathf.Min(bySize.Count - 1, Mathf.RoundToInt(Mathf.Lerp(0, bySize.Count - 1, Mathf.Clamp01(difficulty))));
    result[2] = bySize[pickIndex].idx;

        // Ensure at least one placeable in set
        if (ensurePlaceableInSet)
        {
            var anyOffs = GetOffsets(shapePrefabs[result[0]]);
            if (anyOffs == null || anyOffs.Count == 0 || CountValidPlacements(gm, anyOffs) == 0)
            {
                result[0] = valid[0].idx;
            }
        }
        // As difficulty increases, try to introduce overlap between first two best placements to force a choice
        if (Mathf.Clamp01(difficulty) > 0.5f)
        {
            var gm2 = GetGridManager();
            if (gm2 != null)
            {
                var aOff = GetOffsets(shapePrefabs[result[0]]);
                var bOff = GetOffsets(shapePrefabs[result[1]]);
                if (aOff != null && bOff != null)
                {
                    int overlapBest = 0;
                    for (int ax = 0; ax < gm2.GridWidth; ax++)
                    for (int ay = 0; ay < gm2.GridHeight; ay++)
                    {
                        if (!gm2.CanPlaceShape(new Vector2Int(ax, ay), aOff)) continue;
                        var aFoot = new HashSet<Vector2Int>(); foreach (var o in aOff) aFoot.Add(new Vector2Int(ax + o.x, ay + o.y));
                        for (int bx = 0; bx < gm2.GridWidth; bx++)
                        for (int by = 0; by < gm2.GridHeight; by++)
                        {
                            if (!gm2.CanPlaceShape(new Vector2Int(bx, by), bOff)) continue;
                            int ov = 0; foreach (var o in bOff) { var p = new Vector2Int(bx + o.x, by + o.y); if (aFoot.Contains(p)) ov++; }
                            if (ov > overlapBest) overlapBest = ov;
                        }
                    }
                    // If no overlap found, try swapping second with a closer-by-score candidate to increase odds
                    if (overlapBest == 0 && valid.Count > 2)
                    {
                        for (int i = 2; i < Mathf.Min(valid.Count, 6); i++)
                        {
                            var bAltOff = GetOffsets(shapePrefabs[valid[i].idx]);
                            if (bAltOff == null) continue;
                            bool anyValid = false;
                            for (int bx = 0; bx < gm2.GridWidth && !anyValid; bx++)
                            for (int by = 0; by < gm2.GridHeight && !anyValid; by++)
                            {
                                if (!gm2.CanPlaceShape(new Vector2Int(bx, by), bAltOff)) continue;
                                anyValid = true;
                            }
                            // lightweight heuristic: prefer different family + similar size when swapping
                            if (!preventSameFamilyInSet || GetFamilyLabel(valid[i].idx) != GetFamilyLabel(result[0]))
                            { result[1] = valid[i].idx; break; }
                        }
                    }
                }
            }
        }
        return result;
    }

    // Check if a single shape can be placed such that all current occupied cells are cleared in one LineClear pass.
    // Returns true with the prefab index and preferred slot (0..2) to inject.
    private bool TryGetPerfectClearIndex(out int prefabIndex, out int slot)
    {
        prefabIndex = -1; slot = -1;
        if (!enablePerfectClearOpportunities) return false;
        var gm = GetGridManager();
        if (gm == null || shapePrefabs == null || shapePrefabs.Length == 0) return false;
        var occupied = gm.GetOccupiedPositions();
        if (occupied == null || occupied.Count < perfectClearMinOccupied) return false;
        if (Random.value > perfectClearChance) return false;

        // Heuristic: If placing a shape leads to every row and column that contains an occupied cell being full, we’ll treat as potential all-clear.
        // Simpler proxy: simulate placement, then count full rows/cols across entire board; if total cleared cells >= occupied count and no leftover occupied after clear, accept.
        for (int i = 0; i < shapePrefabs.Length; i++)
        {
            var offs = GetOffsets(shapePrefabs[i]);
            if (offs == null || offs.Count == 0) continue;
            for (int x = 0; x < gm.GridWidth; x++)
            {
                for (int y = 0; y < gm.GridHeight; y++)
                {
                    var start = new Vector2Int(x, y);
                    if (!gm.CanPlaceShape(start, offs)) continue;
                    int cleared = EstimateLinesClearedIfPlaced(gm, occupied, offs, start, out int remainingAfter);
                    if (remainingAfter == 0 && cleared >= occupied.Count)
                    {
                        prefabIndex = i;
                        // choose a random slot to keep variety
                        slot = Random.Range(0, 3);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private int EstimateLinesClearedIfPlaced(Gameplay.GridManager gm, HashSet<Vector2Int> occupied, List<Vector2Int> offsets, Vector2Int start, out int remainingAfter)
    {
        int W = gm.GridWidth, H = gm.GridHeight;
        var sim = new HashSet<Vector2Int>(occupied);
        foreach (var o in offsets) sim.Add(start + o);
        // determine full rows/cols
        var clearedCells = new HashSet<Vector2Int>();
        for (int y = 0; y < H; y++)
        {
            bool full = true;
            for (int x = 0; x < W; x++) { if (!sim.Contains(new Vector2Int(x, y))) { full = false; break; } }
            if (full) { for (int x = 0; x < W; x++) clearedCells.Add(new Vector2Int(x, y)); }
        }
        for (int x = 0; x < W; x++)
        {
            bool full = true;
            for (int y = 0; y < H; y++) { if (!sim.Contains(new Vector2Int(x, y))) { full = false; break; } }
            if (full) { for (int y = 0; y < H; y++) clearedCells.Add(new Vector2Int(x, y)); }
        }
        // remaining = sim - cleared
        int remaining = 0;
        foreach (var p in sim) if (!clearedCells.Contains(p)) remaining++;
        remainingAfter = remaining;
        return clearedCells.Count;
    }

    // Challenge set aims to present two shapes whose best placements overlap, forcing a decision.
    private int[] DetermineChallengeSetIndices()
    {
        int n = shapePrefabs != null ? shapePrefabs.Length : 0;
        var gm = GetGridManager();
        var result = new int[3] { 0, 0, 0 };
        if (gm == null || n == 0) return DetermineNextSetIndices();

        // Build valid candidates with their best placement footprints using a single snapshot
        var candidates = new List<(int idx, List<Vector2Int> bestPlacement, float score, int tiles)>();
        var snap = BoardScoring.CreateSnapshot(gm);
        for (int i = 0; i < n; i++)
        {
            var offs = GetOffsets(shapePrefabs[i]);
            if (offs == null || offs.Count == 0) continue;
            // Find best placement and footprint via BoardScoring
            bool hasValid;
            List<Vector2Int> bestFoot;
            float bestScore = BoardScoring.EvaluateBestPlacementScoreAndFootprint(snap, offs, out bestFoot, out hasValid);
            if (hasValid && bestFoot != null)
                candidates.Add((i, new List<Vector2Int>(bestFoot), bestScore, offs.Count));
        }
        if (candidates.Count < 2)
        {
            return DetermineNextSetIndices();
        }

        // Sort to prefer helpful but overlapping pairs
        candidates.Sort((a, b) => b.score.CompareTo(a.score));
        int chosenA = -1, chosenB = -1;
        int requiredOverlap = Mathf.Max(0, minChallengeOverlapCells);
        for (int i = 0; i < candidates.Count && chosenA < 0; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                int overlap = ComputeOverlap(candidates[i].bestPlacement, candidates[j].bestPlacement);
                if (overlap >= requiredOverlap)
                {
                    chosenA = candidates[i].idx; chosenB = candidates[j].idx; break;
                }
            }
        }
        if (chosenA < 0)
        {
            // fallback: just take two top valid with different families
            chosenA = candidates[0].idx;
            string famA = GetFamilyLabel(chosenA);
            for (int k = 1; k < candidates.Count; k++)
            {
                if (!preventSameFamilyInSet || GetFamilyLabel(candidates[k].idx) != famA)
                { chosenB = candidates[k].idx; break; }
            }
            if (chosenB < 0) chosenB = candidates[Mathf.Min(1, candidates.Count - 1)].idx;
        }

        result[0] = chosenA;
        result[1] = chosenB;

        // Third piece: encourage tension - prefer smallest or mid-tier
        int third = -1;
        if (challengePreferSmallThird)
        {
            int bestTiles = int.MaxValue; float tiebreak = float.NegativeInfinity;
            foreach (var c in candidates)
            {
                if (c.idx == chosenA || c.idx == chosenB) continue;
                if (c.tiles < bestTiles || (c.tiles == bestTiles && c.score > tiebreak))
                { bestTiles = c.tiles; tiebreak = c.score; third = c.idx; }
            }
        }
        if (third < 0)
        {
            // pick mid-ranked different family if possible
            string famA = GetFamilyLabel(chosenA), famB = GetFamilyLabel(chosenB);
            for (int i = candidates.Count / 2; i < candidates.Count; i++)
            {
                int idx = candidates[i].idx; if (idx == chosenA || idx == chosenB) continue;
                if (preventSameFamilyInSet && (GetFamilyLabel(idx) == famA || GetFamilyLabel(idx) == famB)) continue;
                third = idx; break;
            }
        }
        if (third < 0) third = candidates[candidates.Count - 1].idx;
        result[2] = third;

        // Optionally relax assist during challenge to let tension stand out
        if (challengeRelaxAssist)
        {
            assistLevel = Mathf.Max(assistLevel, challengeAssistFloor);
        }
        return result;
    }

    private int ComputeOverlap(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null) return 0;
        int count = 0;
        var set = new HashSet<Vector2Int>(a);
        foreach (var p in b) if (set.Contains(p)) count++;
        return count;
    }
}
