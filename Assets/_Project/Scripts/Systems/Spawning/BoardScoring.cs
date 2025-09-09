using System.Collections.Generic;
using UnityEngine;
using Gameplay;

/// <summary>
/// Centralized board evaluation utilities to score potential placements efficiently.
/// Reduces per-evaluation allocations and eliminates duplicate logic across systems.
/// </summary>
public static class BoardScoring
{
    public sealed class Snapshot
    {
        public GridManager gm;
        public int W;
        public int H;
        public bool[,] occ; // current occupancy grid for fast neighbor checks
        public int[] rowCounts; // occupied count per row
        public int[] colCounts; // occupied count per col
        public Vector2 center;  // board center in cell coords
        public float maxDist;   // max distance to center for normalization
    }

    public static Snapshot CreateSnapshot(GridManager gm)
    {
        if (gm == null) return null;
        var snap = new Snapshot
        {
            gm = gm,
            W = gm.GridWidth,
            H = gm.GridHeight
        };

        snap.occ = new bool[snap.W, snap.H];
        snap.rowCounts = new int[snap.H];
        snap.colCounts = new int[snap.W];

        var occupied = gm.GetOccupiedPositions();
        if (occupied != null)
        {
            foreach (var p in occupied)
            {
                if (p.x >= 0 && p.x < snap.W && p.y >= 0 && p.y < snap.H && !snap.occ[p.x, p.y])
                {
                    snap.occ[p.x, p.y] = true;
                    snap.rowCounts[p.y]++;
                    snap.colCounts[p.x]++;
                }
            }
        }

        snap.center = new Vector2((snap.W - 1) * 0.5f, (snap.H - 1) * 0.5f);
        snap.maxDist = Vector2.Distance(Vector2.zero, new Vector2(snap.center.x, snap.center.y));
        return snap;
    }

    /// <summary>
    /// Evaluate the best achievable score for given shape offsets across the board.
    /// Returns best score; outputs whether any valid placement exists.
    /// </summary>
    public static float EvaluateBestPlacementScore(Snapshot snap, List<Vector2Int> offsets, out bool hasValid)
    {
        hasValid = false;
        if (snap == null || snap.gm == null || offsets == null || offsets.Count == 0) return float.NegativeInfinity;
        float best = float.NegativeInfinity;
        for (int x = 0; x < snap.W; x++)
        {
            for (int y = 0; y < snap.H; y++)
            {
                var start = new Vector2Int(x, y);
                if (!snap.gm.CanPlaceShape(start, offsets)) continue;
                hasValid = true;
                float s = ScorePlacementFast(snap, offsets, start);
                if (s > best) best = s;
            }
        }
        return best;
    }

    /// <summary>
    /// Same as EvaluateBestPlacementScore but also returns the best placement footprint.
    /// </summary>
    public static float EvaluateBestPlacementScoreAndFootprint(Snapshot snap, List<Vector2Int> offsets, out List<Vector2Int> bestFootprint, out bool hasValid)
    {
        bestFootprint = null; hasValid = false;
        if (snap == null || snap.gm == null || offsets == null || offsets.Count == 0) return float.NegativeInfinity;
        float best = float.NegativeInfinity;
        for (int x = 0; x < snap.W; x++)
        {
            for (int y = 0; y < snap.H; y++)
            {
                var start = new Vector2Int(x, y);
                if (!snap.gm.CanPlaceShape(start, offsets)) continue;
                hasValid = true;
                float s = ScorePlacementFast(snap, offsets, start);
                if (s > best)
                {
                    best = s;
                    // build footprint
                    if (bestFootprint == null) bestFootprint = new List<Vector2Int>(offsets.Count);
                    else bestFootprint.Clear();
                    foreach (var o in offsets) bestFootprint.Add(start + o);
                }
            }
        }
        return best;
    }

    /// <summary>
    /// Fast scoring that avoids building a simulated HashSet for the whole board.
    /// Uses precomputed row/col counts and occupancy for adjacency.
    /// </summary>
    public static float ScorePlacementFast(Snapshot snap, List<Vector2Int> offsets, Vector2Int start)
    {
        int placedCount = offsets.Count;
        // Accumulate added counts per row/col for this placement
        // Using arrays sized to board dimensions (small) per call could allocate; instead use temporary dictionaries
        // but since we keep it small, use pooled arrays via stackalloc-like pattern is not available in Unity/IL2CPP.
        // So we use small dictionaries to count per affected row/col only.
        var rowsAdd = new Dictionary<int, int>(4);
        var colsAdd = new Dictionary<int, int>(4);

        // Also accumulate positions for adjacency/centrality
        float avgDist = 0f;
        int adjacency = 0;
        for (int i = 0; i < offsets.Count; i++)
        {
            int px = start.x + offsets[i].x;
            int py = start.y + offsets[i].y;

            // row/col increments
            rowsAdd.TryGetValue(py, out int r); rowsAdd[py] = r + 1;
            colsAdd.TryGetValue(px, out int c); colsAdd[px] = c + 1;

            // adjacency to existing cells (not to other placed cells)
            if (px + 1 < snap.W && snap.occ[px + 1, py]) adjacency++;
            if (px - 1 >= 0 && snap.occ[px - 1, py]) adjacency++;
            if (py + 1 < snap.H && snap.occ[px, py + 1]) adjacency++;
            if (py - 1 >= 0 && snap.occ[px, py - 1]) adjacency++;

            avgDist += Vector2.Distance(snap.center, new Vector2(px, py));
        }
        avgDist /= Mathf.Max(1, placedCount);
        float centrality = (snap.maxDist - avgDist);

        // Count completed rows/cols
        int linesCompleted = 0;
        foreach (var kv in rowsAdd)
        {
            int y = kv.Key; int add = kv.Value;
            if (y >= 0 && y < snap.H && (snap.rowCounts[y] + add) >= snap.W) linesCompleted++;
        }
        int colsCompleted = 0;
        foreach (var kv in colsAdd)
        {
            int x = kv.Key; int add = kv.Value;
            if (x >= 0 && x < snap.W && (snap.colCounts[x] + add) >= snap.H) colsCompleted++;
        }

        // Tunable weights (kept identical to previous logic for parity)
        float score = 100f * linesCompleted + 40f * colsCompleted + 3f * adjacency + 0.5f * centrality;
        return score;
    }
}
