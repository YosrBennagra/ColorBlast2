using UnityEngine;
using System.Collections.Generic;

public partial class ShapeSpawner
{
    private void OnEnable()
    {
        EnsureShapesParent();
        AlignSpawnPointsIfNeeded();
    }

    private void OnValidate()
    {
        AlignSpawnPointsIfNeeded();
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

    private void Start()
    {
        if (!Application.isPlaying) return;
        EnsureShapesParent();
        if (spawnPoints.Length != 3)
        {
            Debug.LogError("ShapeSpawner requires exactly 3 spawn points!");
            return;
        }
        Gameplay.LineClearSystem.OnLinesCleared += OnLinesCleared;
        if (autoSpawnOnStart) SpawnNewShapes();
    }

    private void EnsureShapesParent()
    {
        if (!autoCreateShapesParent) return;
        if (shapesParent != null) return;
        // Try to find by name first (supports both exact and common typos)
        var found = GameObject.Find(shapesParentName)
                    ?? GameObject.Find("Shape Partial")
                    ?? GameObject.Find("Shapes Partial")
                    ?? GameObject.Find("Shapes");
        if (found == null)
        {
            found = new GameObject(string.IsNullOrWhiteSpace(shapesParentName) ? "Shap Partial" : shapesParentName);
        }
        shapesParent = found.transform;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        Gameplay.LineClearSystem.OnLinesCleared -= OnLinesCleared;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        // periodic placement check
        if (!allShapesPlaced && Time.time - lastCheckTime >= spawnCheckInterval)
        {
            lastCheckTime = Time.time;
            CheckIfAllShapesPlaced();
        }

        // adaptive assist tick (optional) + difficulty ramp
        if (adaptiveAssistMode)
        {
            TickAdaptiveAssist();
        }
        if (rampDifficultyOverTime)
        {
            float ramp = Mathf.Clamp01(setsSpawnedCount / Mathf.Max(1f, (float)setsToReachMaxDifficulty));
            float targetDiff = Mathf.Clamp01(Mathf.Max(difficulty, ramp));
            // tighten assist window as difficulty rises
            float baseFloor = 0.15f;
            float baseCeil = 0.9f;
            float hardFloor = 0.05f;
            float hardCeil = 0.6f;
            minAssist = Mathf.Lerp(baseFloor, hardFloor, targetDiff);
            maxAssist = Mathf.Lerp(baseCeil, hardCeil, targetDiff);
        }

        // dead-end reroll timer
        if (enableAutoRerollOnDeadEnd)
        {
            if (!HasAnyValidMove())
            {
                noMoveTimer += Time.deltaTime;
                if (noMoveTimer >= deadEndRerollDelay && rerollsUsed < maxRerollsPerSession)
                {
                    rerollsUsed++;
                    noMoveTimer = 0f;
                    DestroyUnplacedTrayShapes();
                    SpawnNewShapes();
                }
            }
            else
            {
                noMoveTimer = 0f;
            }
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
}
