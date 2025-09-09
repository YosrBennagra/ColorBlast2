using System.Collections.Generic;
using UnityEngine;
using Gameplay;
using ColorBlast.Game;
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

        // Build a snapshot once to reuse across all evaluations
        var snap = BoardScoring.CreateSnapshot(gm);

        // Build scoring for all candidates
        float bestScore = float.NegativeInfinity;
        GameObject bestPrefab = null;
        var validPrefabs = new List<GameObject>();
        foreach (var prefab in candidates)
        {
            var offsets = GetOffsets(prefab);
            if (offsets == null || offsets.Count == 0) continue;

            bool hasValid;
            float prefabBest = BoardScoring.EvaluateBestPlacementScore(snap, offsets, out hasValid);
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

    // Scoring moved to BoardScoring
}
