using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public int rows = 8;
    public int columns = 8;
    public float spacing = 0.1f;
    public Vector2 tileSize = new Vector2(1f, 1f);

    public static List<Vector3> gridPositions = new List<Vector3>();

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        gridPositions.Clear(); // Ensure no duplicates
        Vector2 startPos = transform.position;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2 position = startPos + new Vector2(
                    x * (tileSize.x + spacing),
                    -y * (tileSize.y + spacing)
                );

                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";

                // Save tile center position
                gridPositions.Add(tile.transform.position);
            }
        }
    }
}
