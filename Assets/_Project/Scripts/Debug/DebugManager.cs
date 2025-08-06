using UnityEngine;
using UnityEngine.InputSystem;

namespace DebugTools
{
    /// <summary>
    /// Centralized debug manager for all ColorBlast2 debug functionality
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private Key debugToggleKey = Key.F1;
        [SerializeField] private Key spawnKey = Key.Space;
        [SerializeField] private Key clearKey = Key.C;
        [SerializeField] private Key lineClearKey = Key.L;
        [SerializeField] private Key resetKey = Key.R;
        
        [Header("Debug UI")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool showPerformanceInfo = true;
        
        private bool debugUIVisible = false;
        private ShapeSpawner shapeSpawner;
        private Rect debugWindowRect = new Rect(10, 10, 300, 400);
        
        private void Start()
        {
            shapeSpawner = FindFirstObjectByType<ShapeSpawner>();
            debugUIVisible = showDebugUI;
        }
        
        private void Update()
        {
            HandleDebugInput();
        }
        
        private void HandleDebugInput()
        {
            // Toggle debug UI
            if (Keyboard.current[debugToggleKey].wasPressedThisFrame)
            {
                debugUIVisible = !debugUIVisible;
                Debug.Log($"Debug UI: {(debugUIVisible ? "Enabled" : "Disabled")}");
            }
            
            if (!debugUIVisible) return;
            
            // Shape spawning controls
            if (shapeSpawner != null)
            {
                if (Keyboard.current[spawnKey].wasPressedThisFrame)
                {
                    shapeSpawner.ForceSpawnNewShapes();
                    Debug.Log("🚀 Debug: Forced spawn new shapes!");
                }
                
                if (Keyboard.current[clearKey].wasPressedThisFrame)
                {
                    shapeSpawner.ClearCurrentShapes();
                    Debug.Log("🧹 Debug: Cleared all shapes!");
                }
            }
            
            // Line clearing
            if (Keyboard.current[lineClearKey].wasPressedThisFrame)
            {
                if (ColorBlast.Core.Architecture.Services.IsRegistered<Gameplay.LineClearSystem>())
                {
                    var lineClearSystem = ColorBlast.Core.Architecture.Services.Get<Gameplay.LineClearSystem>();
                    var clearedPositions = lineClearSystem.CheckAndClearLines();
                    Debug.Log($"🔥 Debug: Manual line clear - {clearedPositions.Count} tiles cleared!");
                }
                else
                {
                    Debug.LogWarning("🔥 Debug: LineClearSystem not available yet!");
                }
            }
            
            // Reset all shapes
            if (Keyboard.current[resetKey].wasPressedThisFrame)
            {
                // Reset functionality would need to be implemented
                Debug.Log("🔄 Debug: Reset functionality not yet implemented");
            }
        }
        
        private void OnGUI()
        {
            if (!debugUIVisible) return;
            
            debugWindowRect = GUILayout.Window(0, debugWindowRect, DrawDebugWindow, "🎮 ColorBlast2 Debug Panel");
        }
        
        private void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // Header
            GUILayout.Label("Debug Controls", GUI.skin.label);
            GUILayout.Space(10);
            
            // Key bindings
            GUILayout.Label("Key Bindings:");
            GUILayout.Label($"F1 - Toggle Debug UI");
            GUILayout.Label($"Space - Force Spawn Shapes");
            GUILayout.Label($"C - Clear Current Shapes");
            GUILayout.Label($"L - Trigger Line Clear");
            GUILayout.Label($"R - Reset All Shapes");
            GUILayout.Space(10);
            
            // Shape spawner info
            if (shapeSpawner != null)
            {
                GUILayout.Label("Shape Spawner Status:", GUI.skin.label);
                GUILayout.Label($"Placed Shapes: {shapeSpawner.GetPlacedShapeCount()}/3");
                GUILayout.Label("Auto Spawn: Check spawner settings");
                
                GUILayout.Space(5);
                if (GUILayout.Button("Force Spawn"))
                {
                    shapeSpawner.ForceSpawnNewShapes();
                }
                if (GUILayout.Button("Clear Shapes"))
                {
                    shapeSpawner.ClearCurrentShapes();
                }
            }
            else
            {
                GUILayout.Label("⚠️ ShapeSpawner not found!");
            }
            
            GUILayout.Space(10);
            
            // Game state info
            GUILayout.Label("Game State:", GUI.skin.label);
            
            if (ColorBlast.Core.Architecture.Services.IsRegistered<Gameplay.GridManager>())
            {
                var gridManager = ColorBlast.Core.Architecture.Services.Get<Gameplay.GridManager>();
                var occupiedPositions = gridManager.GetOccupiedPositions();
                GUILayout.Label($"Total Placed Shapes: {FindObjectsByType<Core.Shape>(FindObjectsSortMode.None).Length}");
                GUILayout.Label($"Occupied Positions: {occupiedPositions.Count}");
            }
            else
            {
                GUILayout.Label("Total Placed Shapes: N/A (GridManager not ready)");
                GUILayout.Label("Occupied Positions: N/A (GridManager not ready)");
            }
            
            if (GUILayout.Button("Trigger Line Clear"))
            {
                if (ColorBlast.Core.Architecture.Services.IsRegistered<Gameplay.LineClearSystem>())
                {
                    var lineClearSystem = ColorBlast.Core.Architecture.Services.Get<Gameplay.LineClearSystem>();
                    var clearedPositions = lineClearSystem.CheckAndClearLines();
                    Debug.Log($"Manual line clear - {clearedPositions.Count} tiles cleared!");
                }
                else
                {
                    Debug.LogWarning("LineClearSystem not available yet!");
                }
            }
            
            if (GUILayout.Button("Reset All"))
            {
                // Reset functionality would need to be implemented
                Debug.Log("Reset functionality not yet implemented");
            }
            
            GUILayout.Space(10);
            
            // Performance info
            if (showPerformanceInfo)
            {
                GUILayout.Label("Performance:", GUI.skin.label);
                GUILayout.Label($"FPS: {Mathf.Round(1f / Time.unscaledDeltaTime)}");
                GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB");
            }
            
            GUILayout.EndVertical();
            
            // Make window draggable
            GUI.DragWindow();
        }
        
        // Helper methods for other systems to use
        public static void LogDebug(string message, string category = "DEBUG")
        {
            Debug.Log($"[{category}] {message}");
        }
        
        public static void LogWarning(string message, string category = "DEBUG")
        {
            Debug.LogWarning($"[{category}] {message}");
        }
        
        public static void LogError(string message, string category = "DEBUG")
        {
            Debug.LogError($"[{category}] {message}");
        }
    }
}
