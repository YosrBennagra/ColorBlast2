using UnityEngine;
using System.Collections.Generic;

public partial class ShapeSpawner
{
    private void OnEnable()
    {
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
        if (spawnPoints.Length != 3)
        {
            Debug.LogError("ShapeSpawner requires exactly 3 spawn points!");
            return;
        }
        Gameplay.LineClearSystem.OnLinesCleared += OnLinesCleared;
        if (autoSpawnOnStart) SpawnNewShapes();
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

        // adaptive assist tick (optional)
        if (adaptiveAssistMode)
        {
            TickAdaptiveAssist();
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
