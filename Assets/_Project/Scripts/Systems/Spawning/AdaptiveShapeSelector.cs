using System.Collections.Generic;
using UnityEngine;
using Gameplay;
using Core;
using ColorBlast.Core.Architecture;

/// <summary>
/// Picks helpful shapes based on current board state.
/// Keeps it simple: ensures at least one placeable shape, favors placements that complete lines,
/// hug existing tiles, and stay central. Blends with randomness via assistLevel.
/// </summary>
public static class AdaptiveShapeSelector
{
    public static GameObject SelectPrefab(GameObject[] candidates, float assistLevel)
    {
        if (candidates == null || candidates.Length == 0)
            return null;

        var gm = TryGetGridManager();
        if (gm == null)
        {
            // Fallback to random if no grid
            return candidates[Random.Range(0, candidates.Length)];
        }

        // Build scoring for all candidates
        float bestScore = float.NegativeInfinity;
        GameObject bestPrefab = null;
        var validPrefabs = new List<GameObject>();
        foreach (var prefab in candidates)
        {
            var offsets = GetOffsets(prefab);
            if (offsets == null || offsets.Count == 0) continue;

            bool hasValid = false;
            float prefabBest = EvaluateBestPlacementScore(gm, offsets, ref hasValid);
            if (hasValid)
            {
                validPrefabs.Add(prefab);
                if (prefabBest > bestScore)
                {
                    bestScore = prefabBest;
                    bestPrefab = prefab;
                }
            }
        }

        if (validPrefabs.Count == 0)
        {
            // Lifeline: pick the smallest shape by tile count (likely to fit)
            GameObject lifeline = PickSmallestByTileCount(candidates);
            return lifeline != null ? lifeline : candidates[Random.Range(0, candidates.Length)];
        }

        // Blend random and helpful
        if (assistLevel <= 0f)
        {
            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }
        if (assistLevel >= 1f || bestPrefab == null)
        {
            return bestPrefab ?? validPrefabs[0];
        }

        // Weighted pick between best and random valid using assistLevel
        if (Random.value < assistLevel)
            return bestPrefab;
        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    private static GridManager TryGetGridManager()
    {
        if (Services.Has<GridManager>()) return Services.Get<GridManager>();
        return Object.FindFirstObjectByType<GridManager>();
    }

    private static List<Vector2Int> GetOffsets(GameObject prefab)
    {
        if (prefab == null) return null;
        var shape = prefab.GetComponent<Shape>();
        if (shape != null && shape.ShapeOffsets != null && shape.ShapeOffsets.Count > 0)
        {
            return shape.ShapeOffsets;
        }
        return null;
    }

    private static GameObject PickSmallestByTileCount(GameObject[] candidates)
    {
        GameObject best = null;
        int bestCount = int.MaxValue;
        foreach (var p in candidates)
        {
            var offs = GetOffsets(p);
            if (offs == null) continue;
            int c = offs.Count;
            if (c < bestCount)
            {
                bestCount = c;
                best = p;
            }
        }
        return best ?? (candidates.Length > 0 ? candidates[0] : null);
    }

    // Evaluate the best achievable score for given shape offsets across the board
    private static float EvaluateBestPlacementScore(GridManager gm, List<Vector2Int> offsets, ref bool hasValid)
    {
        hasValid = false;
        float best = float.NegativeInfinity;
        int W = gm.GridWidth;
        int H = gm.GridHeight;
        var occupied = gm.GetOccupiedPositions();

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                var start = new Vector2Int(x, y);
                if (!gm.CanPlaceShape(start, offsets)) continue;
                hasValid = true;
                float s = ScorePlacement(gm, occupied, offsets, start);
                if (s > best) best = s;
            }
        }
        return best;
    }

    private static float ScorePlacement(GridManager gm, HashSet<Vector2Int> occupied, List<Vector2Int> offsets, Vector2Int start)
    {
        int W = gm.GridWidth;
        int H = gm.GridHeight;

        // Simulate occupancy
        var sim = new HashSet<Vector2Int>(occupied);
        var placed = new List<Vector2Int>(offsets.Count);
        foreach (var o in offsets)
        {
            var p = start + o;
            sim.Add(p);
            placed.Add(p);
        }

        // Heuristics
        int linesCompleted = 0;
        // count full rows
        for (int y = 0; y < H; y++)
        {
            bool full = true;
            for (int x = 0; x < W; x++)
            {
                if (!sim.Contains(new Vector2Int(x, y))) { full = false; break; }
            }
            if (full) linesCompleted++;
        }
        // count full columns (optional, smaller weight)
        int colsCompleted = 0;
        for (int x = 0; x < W; x++)
        {
            bool full = true;
            for (int y = 0; y < H; y++)
            {
                if (!sim.Contains(new Vector2Int(x, y))) { full = false; break; }
            }
            if (full) colsCompleted++;
        }

        // Adjacency to existing blocks (touching existing helps reduce holes)
        int adjacency = 0;
        foreach (var p in placed)
        {
            if (occupied.Contains(new Vector2Int(p.x + 1, p.y))) adjacency++;
            if (occupied.Contains(new Vector2Int(p.x - 1, p.y))) adjacency++;
            if (occupied.Contains(new Vector2Int(p.x, p.y + 1))) adjacency++;
            if (occupied.Contains(new Vector2Int(p.x, p.y - 1))) adjacency++;
        }

        // Centrality (closer to center is slightly better)
        Vector2 center = new Vector2((W - 1) * 0.5f, (H - 1) * 0.5f);
        float avgDist = 0f;
        foreach (var p in placed)
        {
            avgDist += Vector2.Distance(center, new Vector2(p.x, p.y));
        }
        avgDist /= Mathf.Max(1, placed.Count);
        float maxDist = Vector2.Distance(Vector2.zero, new Vector2(center.x, center.y));
        float centrality = (maxDist - avgDist); // higher is better

        // Final score (tunable weights)
        float score = 100f * linesCompleted + 40f * colsCompleted + 3f * adjacency + 0.5f * centrality;
        return score;
    }
}
