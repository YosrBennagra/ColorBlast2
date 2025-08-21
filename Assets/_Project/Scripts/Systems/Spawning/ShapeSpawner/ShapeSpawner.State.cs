using UnityEngine;
using System.Collections.Generic;
using ColorBlast.Game;

public partial class ShapeSpawner
{
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
                var shapeComponent = currentShapes[i].GetComponent<Shape>();
                if (shapeComponent != null && shapeComponent.IsPlaced)
                {
                    currentStatus = true; placedCount++;
                }
                else { allPlaced = false; }
            }
            else { currentStatus = true; placedCount++; }
            if (shapeStatusCache[i] != currentStatus)
            {
                shapeStatusCache[i] = currentStatus; statusChanged = true;
            }
        }
        if (allPlaced && placedCount >= 3)
        {
            allShapesPlaced = true;
            setsClearedStreak++;
            if (setsClearedStreak > bestStreak) bestStreak = setsClearedStreak;
            Invoke(nameof(SpawnNewShapes), 0.5f);
        }
        else if (statusChanged)
        {
            // no-op; reserved for UI updates
        }
    }

    private void OnLinesCleared(List<Vector2Int> cleared)
    {
        if (!Application.isPlaying) return;
        CheckIfAllShapesPlaced();
    }

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

    [ContextMenu("Reset Deterministic State")]
    public void ResetDeterministicState()
    {
        bagCursor = 0; bag.Clear(); recentIndices.Clear(); deferredIndices.Clear();
        setsClearedStreak = 0; rerollsUsed = 0; noMoveTimer = 0f;
    }

    public bool AreAllShapesPlaced() => allShapesPlaced;

    public int GetPlacedShapeCount()
    {
        int count = 0;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            if (currentShapes[i] != null)
            {
                var shape = currentShapes[i].GetComponent<Shape>();
                if (shape != null && shape.IsPlaced) count++;
            }
            else count++;
        }
        return count;
    }

    public bool HasAnyValidMove()
    {
        if (!Application.isPlaying) return true;
        var gm = GetGridManager();
        if (gm == null) return true;
        bool foundUnplaced = false;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            var go = currentShapes[i];
            if (go == null) continue;
            var s = go.GetComponent<Shape>();
            if (s == null || s.IsPlaced) continue;
            foundUnplaced = true;
            var offs = s.ShapeOffsets; if (offs == null || offs.Count == 0) continue;
            for (int x = 0; x < gm.GridWidth; x++)
                for (int y = 0; y < gm.GridHeight; y++)
                    if (gm.CanPlaceShape(new Vector2Int(x, y), offs)) return true;
        }
        if (!foundUnplaced) return true;
        return false;
    }

    public void DestroyUnplacedTrayShapes()
    {
        if (!Application.isPlaying) return;
        for (int i = 0; i < currentShapes.Length; i++)
        {
            var go = currentShapes[i];
            if (go == null) continue;
            var s = go.GetComponent<Shape>();
            if (s != null && !s.IsPlaced)
            {
                Destroy(go);
                currentShapes[i] = null;
                shapeStatusCache[i] = false;
            }
        }
        allShapesPlaced = false;
    }
}
