using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShapeBlaster.Adventure
{
    public enum GoalMode { All, Any }

    public enum ObjectiveType
    {
        Score,        // Reach target score
        ClearTheme    // Clear N tiles of a given theme (e.g., Water, Volcano)
    }

    [Serializable]
    public class AdventureGoal
    {
        public ObjectiveType type = ObjectiveType.Score;
        public int targetScore = 0;        // when type == Score
        public string themeName;           // when type == ClearTheme
        public int targetCount = 0;        // when type == ClearTheme
    }

    [Serializable]
    public class AdventureLevel
    {
        public string id;
        public ObjectiveType type = ObjectiveType.Score;
        public int targetScore = 0;
        public string themeName; // Used when type == ClearTheme
        public int targetCount = 0; // Used when type == ClearTheme
        [TextArea]
        public string description;

        [Serializable]
        public class PrePlacedTile
        {
            public Vector2Int position; // 0..7 for 8x8
            public string themeName;    // e.g., "Water", "Volcano"
            [Tooltip("Optional sprite to use for this pre-placed tile (overrides theme sprite if set)")]
            public Sprite spriteOverride;
        }

        [Header("Optional Pre-Placed Tiles")]
        public List<PrePlacedTile> prePlacedTiles = new List<PrePlacedTile>();

        [Header("Advanced Goals (optional)")]
        [Tooltip("If set, these goals define the level objective. If empty, uses legacy single-objective fields above.")]
        public GoalMode mode = GoalMode.All;
        public List<AdventureGoal> goals = new List<AdventureGoal>();
    }

    [CreateAssetMenu(fileName = "AdventureLevelLibrary", menuName = "ColorBlast/Adventure/Level Library")]
    public class AdventureLevelLibrary : ScriptableObject
    {
        public List<AdventureLevel> allLevels = new List<AdventureLevel>();
    }
}
