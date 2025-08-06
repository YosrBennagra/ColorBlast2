using UnityEngine;
using ColorBlast.Core.Architecture;
using Gameplay;

namespace Core
{
    /// <summary>
    /// Main game manager that initializes and coordinates all game systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Initialization")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool persistAcrossScenes = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private static GameManager instance;
        private bool isInitialized = false;
        
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameManager>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            // Singleton pattern - ensure only one GameManager exists
            if (instance == null)
            {
                instance = this;
                
                if (persistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                    if (enableDebugLogs)
                        Debug.Log("🎮 GameManager created and set to persist across scenes");
                }
                
                if (enableDebugLogs)
                    Debug.Log("🎮 GameManager created and ready");
            }
            else if (instance != this)
            {
                if (enableDebugLogs)
                    Debug.Log("🔄 Duplicate GameManager found - destroying duplicate");
                Destroy(gameObject);
                return;
            }
        }
        
        private void OnEnable()
        {
            if (enableDebugLogs)
                Debug.Log("🟢 GameManager OnEnable called");
        }
        
        private void OnDisable()
        {
            if (enableDebugLogs)
                Debug.Log("🔴 GameManager OnDisable called");
        }
        
        private void Start()
        {
            if (autoInitialize && !isInitialized)
            {
                InitializeGameSystems();
            }
        }
        
        public void InitializeGameSystems()
        {
            if (isInitialized)
            {
                if (enableDebugLogs)
                    Debug.Log("🎮 Game systems already initialized - skipping");
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log("🎮 Initializing ColorBlast2 Game Systems...");
            
            try
            {
                // ServiceLocator is automatically initialized when first accessed
                // No need to manually create or register it
                
                // Find and register existing GridManager
                var gridManager = FindFirstObjectByType<GridManager>();
                if (gridManager == null)
                {
                    Debug.LogError("❌ GridManager not found! Please create a GridManager GameObject manually in the scene.");
                    return;
                }
                if (!Services.Has<GridManager>())
                {
                    Services.Register(gridManager);
                }
                if (enableDebugLogs)
                    Debug.Log("✅ GridManager found and registered");
                
                // Create and register PlacementSystem
                var placementSystem = FindFirstObjectByType<PlacementSystem>();
                if (placementSystem == null)
                {
                    var placementSystemGO = new GameObject("PlacementSystem");
                    placementSystem = placementSystemGO.AddComponent<PlacementSystem>();
                }
                if (!Services.Has<PlacementSystem>())
                {
                    Services.Register(placementSystem);
                }
                if (enableDebugLogs)
                    Debug.Log("✅ PlacementSystem registered");
                
                // Create and register LineClearSystem
                var lineClearSystem = FindFirstObjectByType<LineClearSystem>();
                if (lineClearSystem == null)
                {
                    var lineClearSystemGO = new GameObject("LineClearSystem");
                    lineClearSystem = lineClearSystemGO.AddComponent<LineClearSystem>();
                }
                if (!Services.Has<LineClearSystem>())
                {
                    Services.Register(lineClearSystem);
                }
                if (enableDebugLogs)
                    Debug.Log("✅ LineClearSystem registered");
                
                // Create and register ShapeDestructionSystem
                var shapeDestructionSystem = FindFirstObjectByType<ShapeDestructionSystem>();
                if (shapeDestructionSystem == null)
                {
                    var shapeDestructionSystemGO = new GameObject("ShapeDestructionSystem");
                    shapeDestructionSystem = shapeDestructionSystemGO.AddComponent<ShapeDestructionSystem>();
                }
                if (!Services.Has<ShapeDestructionSystem>())
                {
                    Services.Register(shapeDestructionSystem);
                }
                if (enableDebugLogs)
                    Debug.Log("✅ ShapeDestructionSystem registered");
                
                if (enableDebugLogs)
                    Debug.Log("🎉 Game systems initialized successfully!");
                
                isInitialized = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize game systems: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            // Clear static reference if this instance is being destroyed
            if (instance == this)
            {
                instance = null;
                Services.Clear();
            }
        }
        
        // Public methods for debugging
        public bool IsInitialized() => isInitialized;
        
        public void ForceReinitialize()
        {
            isInitialized = false;
            InitializeGameSystems();
        }
    }
}