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
        // After alignment, enforce tray outside grid if requested
        KeepTrayOutsideGrid();
    }

    private void KeepTrayOutsideGrid()
    {
        if (!autoKeepTrayOutsideGrid) return;
        var gm = GetGridManager();
        if (gm == null || spawnPoints == null) return;
        int W = gm.GridWidth, H = gm.GridHeight;
        if (W <= 0 || H <= 0) return;
        float cell = gm.CellSize;
        // Grid world AABB (expand half-cell to cover full bounds)
        Vector3 c00 = gm.GridToWorldPosition(new Vector2Int(0, 0));
        Vector3 c11 = gm.GridToWorldPosition(new Vector2Int(W - 1, H - 1));
        float minX = Mathf.Min(c00.x, c11.x) - cell * 0.5f;
        float maxX = Mathf.Max(c00.x, c11.x) + cell * 0.5f;
        float minY = Mathf.Min(c00.y, c11.y) - cell * 0.5f;
        float maxY = Mathf.Max(c00.y, c11.y) + cell * 0.5f;

        float halfW = previewSize.x * 0.5f;
        float halfH = previewSize.y * 0.5f;
        float margin = Mathf.Abs(trayMarginFromGrid);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var t = spawnPoints[i]; if (t == null) continue;
            Vector3 p = t.position;
            bool overlaps = (p.x + halfW > minX && p.x - halfW < maxX && p.y + halfH > minY && p.y - halfH < maxY);
            if (!overlaps) continue;
            // Push out based on alignment mode: left of grid if horizontal, below grid if vertical
            if (alignSpawnPointsHorizontally)
            {
                p.x = minX - halfW - margin;
            }
            else // vertical (default)
            {
                p.y = minY - halfH - margin;
            }
            t.position = p;
        }
    }
}
