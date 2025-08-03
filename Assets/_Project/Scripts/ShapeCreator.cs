using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Shape", menuName = "Puzzle Game/Shape")]
public class ShapeData : ScriptableObject
{
    [Header("Shape Configuration")]
    public string shapeName;
    public List<Vector2Int> tileOffsets = new List<Vector2Int>();
    public Color shapeColor = Color.white;
    
    [Header("Visual")]
    public Sprite tileSprite;
}

public class ShapeCreator : MonoBehaviour
{
    [Header("Shape Setup")]
    [SerializeField] private ShapeData shapeData;
    [SerializeField] private GameObject tilePrefab; // Prefab with sprite renderer for each tile
    [SerializeField] private float tileSize = 1f;
    
    private List<GameObject> shapeTiles = new List<GameObject>();
    
    void Start()
    {
        CreateShapeVisual();
        SetupDragComponent();
    }
    
    private void CreateShapeVisual()
    {
        if (shapeData == null || tilePrefab == null) return;
        
        // Clear existing tiles
        foreach (GameObject tile in shapeTiles)
        {
            if (tile != null) DestroyImmediate(tile);
        }
        shapeTiles.Clear();
        
        // Create tiles for each offset
        foreach (Vector2Int offset in shapeData.tileOffsets)
        {
            GameObject tile = Instantiate(tilePrefab, transform);
            tile.transform.localPosition = new Vector3(offset.x * tileSize, offset.y * tileSize, 0);
            
            // Set color
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = shapeData.shapeColor;
                if (shapeData.tileSprite != null)
                    renderer.sprite = shapeData.tileSprite;
            }
            
            shapeTiles.Add(tile);
        }
    }
    
    private void SetupDragComponent()
    {
        Drag2D dragComponent = GetComponent<Drag2D>();
        if (dragComponent == null)
        {
            dragComponent = gameObject.AddComponent<Drag2D>();
        }
        
        // Set shape offsets in drag component
        if (shapeData != null)
        {
            dragComponent.SetShapeOffsets(shapeData.tileOffsets);
        }
        
        // Add collider for the entire shape
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            CalculateColliderBounds(collider);
        }
    }
    
    private void CalculateColliderBounds(BoxCollider2D collider)
    {
        if (shapeData == null || shapeData.tileOffsets.Count == 0) return;
        
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        
        foreach (Vector2Int offset in shapeData.tileOffsets)
        {
            Vector2 pos = new Vector2(offset.x * tileSize, offset.y * tileSize);
            min = Vector2.Min(min, pos);
            max = Vector2.Max(max, pos);
        }
        
        collider.size = (max - min) + Vector2.one * tileSize;
        collider.offset = (max + min) * 0.5f;
    }
    
    // Editor helper
    [ContextMenu("Refresh Shape")]
    public void RefreshShape()
    {
        CreateShapeVisual();
        SetupDragComponent();
    }
}
