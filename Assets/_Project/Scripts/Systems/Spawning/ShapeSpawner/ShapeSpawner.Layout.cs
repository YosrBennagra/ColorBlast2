using UnityEngine;

public partial class ShapeSpawner
{
    private void AlignSpawnPointsIfNeeded()
    {
        if (spawnPoints == null || spawnPoints.Length < 3) return;
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
        if (alignSpawnPointsHorizontally)
        {
            float baseY = Mathf.Abs(alignAtY) > Mathf.Epsilon ? alignAtY : transform.position.y;
            int leftIndex = 0;
            for (int i = 0; i < spawnPoints.Length; i++) { if (spawnPoints[i] != null) { leftIndex = i; break; } }
            if (spawnPoints[leftIndex] == null) return;
            float leftX = spawnPoints[leftIndex].position.x;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;
                Vector3 p = spawnPoints[i].position;
                p.y = baseY; p.x = leftX + i * Mathf.Abs(horizontalSpacing);
                spawnPoints[i].position = p;
            }
            return;
        }
        if (!alignSpawnPointsVertically) return;
        float baseX = Mathf.Abs(alignAtX) > Mathf.Epsilon ? alignAtX : transform.position.x;
        int topIndex = 0;
        for (int i = 0; i < spawnPoints.Length; i++) { if (spawnPoints[i] != null) { topIndex = i; break; } }
        if (spawnPoints[topIndex] == null) return;
        float topY = spawnPoints[topIndex].position.y;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            Vector3 p = spawnPoints[i].position;
            p.x = baseX; p.y = topY - i * Mathf.Abs(verticalSpacing);
            spawnPoints[i].position = p;
        }
    }
}
