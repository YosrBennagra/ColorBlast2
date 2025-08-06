using UnityEngine;
using System.Collections.Generic;

namespace ColorBlast.Core.Data
{
    /// <summary>
    /// ScriptableObject defining shape data
    /// </summary>
    [CreateAssetMenu(fileName = "ShapeData", menuName = "ColorBlast/Shape Data")]
    public class ShapeData : ScriptableObject
    {
        [Header("Shape Configuration")]
        public string shapeName;
        [SerializeField] private List<Vector2Int> tileOffsets = new List<Vector2Int>();
        
        [Header("Visual")]
        public Sprite tileSprite;
        public Color shapeColor = Color.white;
        
        [Header("Gameplay")]
        [Range(1, 5)] public int rarity = 1; // 1 = common, 5 = rare
        public int pointValue = 10;
        
        [Header("Audio")]
        public AudioClip placementSound;
        
        public List<Vector2Int> TileOffsets => new List<Vector2Int>(tileOffsets);
        
        /// <summary>
        /// Get the bounding box of this shape
        /// </summary>
        public Vector2Int GetBounds()
        {
            if (tileOffsets.Count == 0) return Vector2Int.one;
            
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            foreach (Vector2Int offset in tileOffsets)
            {
                minX = Mathf.Min(minX, offset.x);
                maxX = Mathf.Max(maxX, offset.x);
                minY = Mathf.Min(minY, offset.y);
                maxY = Mathf.Max(maxY, offset.y);
            }
            
            return new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        }
        
        /// <summary>
        /// Get the center offset of this shape
        /// </summary>
        public Vector2 GetCenter()
        {
            if (tileOffsets.Count == 0) return Vector2.zero;
            
            Vector2 sum = Vector2.zero;
            foreach (Vector2Int offset in tileOffsets)
            {
                sum += new Vector2(offset.x, offset.y);
            }
            
            return sum / tileOffsets.Count;
        }
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(shapeName))
            {
                shapeName = name;
            }
            
            rarity = Mathf.Clamp(rarity, 1, 5);
            pointValue = Mathf.Max(0, pointValue);
        }
    }
}
