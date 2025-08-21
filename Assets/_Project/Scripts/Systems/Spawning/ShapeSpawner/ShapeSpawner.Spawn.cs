using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColorBlast.Game;

public partial class ShapeSpawner
{
    private void SpawnNewShapes()
    {
        if (!Application.isPlaying) return;
        allShapesPlaced = false;
        for (int i = 0; i < currentShapes.Length; i++) { currentShapes[i] = null; shapeStatusCache[i] = false; }

        int[] indices;
        bool doChallenge = enableChallengeRounds && (challengeEveryNSets > 0) && (setsSpawnedCount > 0) && (setsSpawnedCount % challengeEveryNSets == 0);
        if (doChallenge)
        {
            indices = DetermineChallengeSetIndices();
        }
        else
        {
            indices = useDeterministicSelection ? DetermineNextSetIndices() : DetermineRandomAdaptiveIndices();
        }
        var newly = new List<GameObject>(3);
        for (int i = 0; i < 3; i++)
        {
            if (spawnPoints[i] == null) continue;
            GameObject go = SpawnShapeByIndex(indices[i], i);
            currentShapes[i] = go;
            if (go != null) newly.Add(go);
            if (useDeterministicSelection && noRepeatWindow > 0)
            {
                recentIndices.Enqueue(indices[i]);
                while (recentIndices.Count > noRepeatWindow) recentIndices.Dequeue();
            }
        }
        ApplyThemesToShapes(newly.ToArray());
        lastSpawnTime = Time.time;
    setsSpawnedCount++;
    }

    private int[] DetermineRandomAdaptiveIndices()
    {
        int n = shapePrefabs != null ? shapePrefabs.Length : 0;
        var result = new int[3] { 0, 0, 0 };
        for (int i = 0; i < 3; i++)
        {
            result[i] = PickRandomAdaptiveIndex();
        }
        return result;
    }

    private int PickRandomAdaptiveIndex()
    {
        if (shapePrefabs == null || shapePrefabs.Length == 0) return 0;
        int selectedIndex = -1;
        var selectorType = System.Type.GetType("AdaptiveShapeSelector");
        if (selectorType != null)
        {
            var method = selectorType.GetMethod("SelectPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                try
                {
                    float level = assistLevel;
                    var prefab = (GameObject)method.Invoke(null, new object[] { shapePrefabs, level });
                    if (prefab != null) selectedIndex = System.Array.IndexOf(shapePrefabs, prefab);
                }
                catch { }
            }
        }
        if (selectedIndex < 0) selectedIndex = Random.Range(0, shapePrefabs.Length);
        return selectedIndex;
    }

    private GameObject SpawnShapeByIndex(int prefabIndex, int spawnIndex)
    {
        if (shapePrefabs == null || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length) return null;
        var shapePrefab = shapePrefabs[prefabIndex];
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        GameObject spawnedShape = Instantiate(shapePrefab, spawnPosition, Quaternion.identity);

        Vector3 baseScale = GetBaseScaleForSlot(spawnIndex);
        if (baseScale == Vector3.zero) baseScale = Vector3.one;
        spawnedShape.transform.localScale = baseScale;

        var shapeComponent = spawnedShape.GetComponent<Shape>();
        if (enableOrientationVariants && shapeComponent != null)
        {
            TryApplyOrientationVariant(shapeComponent, prefabIndex, spawnIndex);
            shapeComponent.CacheTileRenderers();
        }
        if (centerSpawnedShapesInGizmo) TryCenterShapeToSpawn(spawnedShape, spawnPosition);
        if (fitShapesToPreviewBox)
        {
            FitShapeIntoPreviewBox(spawnedShape, spawnIndex);
            if (centerSpawnedShapesInGizmo) TryCenterByRenderers(spawnedShape, spawnPosition);
        }
        if (setSpawnSortingOrders) ApplySortingForSpawn(spawnedShape, spawnIndex);

        var dragHandler = spawnedShape.GetComponent<Gameplay.DragHandler>();
        if (shapeComponent != null && dragHandler != null)
        {
            spawnedShape.name = $"Shape_{spawnIndex}_{Random.Range(1000, 9999)}";
        }

        Vector3 fittedScale = spawnedShape.transform.localScale;
        StartCoroutine(SpawnEffect(spawnedShape, fittedScale));
        return spawnedShape;
    }

    private void ApplyThemesToShapes(GameObject[] shapes)
    {
        if (!Application.isPlaying || !useRandomThemes || shapes == null || shapes.Length == 0) return;
        var manager = spriteManager ?? ShapeSpriteManager.Instance;
        if (manager != null) manager.ApplyRandomThemes(shapes);
    }

    public void ApplyThemeToShape(GameObject shape, string themeName)
    {
        if (!Application.isPlaying) return;
        var manager = spriteManager ?? ShapeSpriteManager.Instance;
        if (manager == null) return;
        var theme = manager.GetThemeByName(themeName);
        if (theme != null) manager.ApplyThemeToShape(shape, theme);
    }
}
