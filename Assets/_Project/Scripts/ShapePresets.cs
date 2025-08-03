using UnityEngine;
using System.Collections.Generic;

public static class ShapePresets
{
    // Single square
    public static List<Vector2Int> SingleSquare => new List<Vector2Int> { Vector2Int.zero };
    
    // L-shape (4 tiles)
    public static List<Vector2Int> LShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(0, 1),      // (0,1) 
        new Vector2Int(0, 2),      // (0,2)
        new Vector2Int(1, 0)       // (1,0)
    };
    
    // T-shape (4 tiles)
    public static List<Vector2Int> TShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(-1, 0),     // (-1,0)
        new Vector2Int(1, 0),      // (1,0)
        new Vector2Int(0, 1)       // (0,1)
    };
    
    // I-shape (4 tiles vertical)
    public static List<Vector2Int> IShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(0, 1),      // (0,1)
        new Vector2Int(0, 2),      // (0,2)
        new Vector2Int(0, 3)       // (0,3)
    };
    
    // O-shape (2x2 square)
    public static List<Vector2Int> OShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(1, 0),      // (1,0)
        new Vector2Int(0, 1),      // (0,1)
        new Vector2Int(1, 1)       // (1,1)
    };
    
    // Z-shape
    public static List<Vector2Int> ZShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(-1, 0),     // (-1,0)
        new Vector2Int(0, 1),      // (0,1)
        new Vector2Int(1, 1)       // (1,1)
    };
    
    // S-shape
    public static List<Vector2Int> SShape => new List<Vector2Int> 
    { 
        Vector2Int.zero,           // (0,0)
        new Vector2Int(1, 0),      // (1,0)
        new Vector2Int(0, 1),      // (0,1)
        new Vector2Int(-1, 1)      // (-1,1)
    };
}
