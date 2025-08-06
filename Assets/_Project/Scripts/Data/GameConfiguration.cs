using UnityEngine;
using System.Collections.Generic;

namespace ColorBlast.Core.Data
{
    /// <summary>
    /// Game configuration ScriptableObject for data-driven design
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ColorBlast/Game Configuration")]
    public class GameConfiguration : ScriptableObject
    {
        [Header("Grid Settings")]
        public int gridRows = 8;
        public int gridColumns = 8;
        public float cellSize = 1f;
        public Vector3 gridOffset = Vector3.zero;

        [Header("Gameplay Settings")]
        public int shapesPerWave = 3;
        public bool allowPartialPlacement = false;
        public bool strictBoundsChecking = true;
        public float placementAnimationDuration = 0.3f;

        [Header("Scoring")]
        public int pointsPerTile = 10;
        public int lineCompletionBonus = 50;
        public int cascadeMultiplier = 2;
        public bool enableComboSystem = true;

        [Header("Visual Settings")]
        public Color normalShapeColor = Color.white;
        public Color highlightedShapeColor = Color.yellow;
        public Color invalidShapeColor = Color.red;
        public Color placedShapeColor = Color.gray;
        public Color gridLineColor = Color.white;
        public float gridLineAlpha = 0.3f;

        [Header("Audio Settings")]
        public bool enableAudio = true;
        public float masterVolume = 1f;
        public float sfxVolume = 0.8f;
        public float musicVolume = 0.6f;

        [Header("Performance Settings")]
        public bool useObjectPooling = true;
        public int maxPoolSize = 50;
        public bool enablePerformanceMonitoring = false;
        public int targetFrameRate = 60;

        [Header("Debug Settings")]
        public bool enableDebugMode = false;
        public bool showGridGizmos = true;
        public bool showPerformanceStats = false;
        public KeyCode restartKey = KeyCode.R;
        public KeyCode pauseKey = KeyCode.Space;

        /// <summary>
        /// Validate the configuration values
        /// </summary>
        private void OnValidate()
        {
            // Ensure valid grid dimensions
            gridRows = Mathf.Clamp(gridRows, 4, 20);
            gridColumns = Mathf.Clamp(gridColumns, 4, 20);
            cellSize = Mathf.Max(0.1f, cellSize);

            // Ensure valid gameplay settings
            shapesPerWave = Mathf.Clamp(shapesPerWave, 1, 5);
            placementAnimationDuration = Mathf.Max(0f, placementAnimationDuration);

            // Ensure valid scoring
            pointsPerTile = Mathf.Max(0, pointsPerTile);
            lineCompletionBonus = Mathf.Max(0, lineCompletionBonus);
            cascadeMultiplier = Mathf.Max(1, cascadeMultiplier);

            // Ensure valid audio volumes
            masterVolume = Mathf.Clamp01(masterVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            musicVolume = Mathf.Clamp01(musicVolume);

            // Ensure valid performance settings
            maxPoolSize = Mathf.Max(10, maxPoolSize);
            targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);

            // Ensure valid visual settings
            gridLineAlpha = Mathf.Clamp01(gridLineAlpha);
        }

        /// <summary>
        /// Create a default configuration
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            gridRows = 8;
            gridColumns = 8;
            cellSize = 1f;
            gridOffset = Vector3.zero;

            shapesPerWave = 3;
            allowPartialPlacement = false;
            strictBoundsChecking = true;
            placementAnimationDuration = 0.3f;

            pointsPerTile = 10;
            lineCompletionBonus = 50;
            cascadeMultiplier = 2;
            enableComboSystem = true;

            normalShapeColor = Color.white;
            highlightedShapeColor = Color.yellow;
            invalidShapeColor = Color.red;
            placedShapeColor = Color.gray;
            gridLineColor = Color.white;
            gridLineAlpha = 0.3f;

            enableAudio = true;
            masterVolume = 1f;
            sfxVolume = 0.8f;
            musicVolume = 0.6f;

            useObjectPooling = true;
            maxPoolSize = 50;
            enablePerformanceMonitoring = false;
            targetFrameRate = 60;

            enableDebugMode = false;
            showGridGizmos = true;
            showPerformanceStats = false;
            restartKey = KeyCode.R;
            pauseKey = KeyCode.Space;
        }

        /// <summary>
        /// Get a formatted summary of this configuration
        /// </summary>
        public string GetConfigSummary()
        {
            return $"Grid: {gridColumns}x{gridRows} (Cell: {cellSize})\n" +
                   $"Shapes per wave: {shapesPerWave}\n" +
                   $"Points per tile: {pointsPerTile}\n" +
                   $"Audio: {(enableAudio ? "Enabled" : "Disabled")}\n" +
                   $"Debug: {(enableDebugMode ? "Enabled" : "Disabled")}";
        }
    }
}
