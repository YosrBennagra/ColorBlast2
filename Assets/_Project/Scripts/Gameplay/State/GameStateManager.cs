using UnityEngine;
using ColorBlast.Core.Interfaces;
using ColorBlast.Core.Data;
using ColorBlast.Core.Architecture;
using ColorBlast.Core.Events;

namespace ColorBlast.Gameplay.State
{
    /// <summary>
    /// Manages the overall game state
    /// </summary>
    public class GameStateManager : MonoBehaviour, IGameStateManager
    {
        [Header("Game State")]
        [SerializeField] private GameState initialState = GameState.Menu;
        [SerializeField] private bool debugMode = false;

        private GameState currentState;

        #region Unity Lifecycle

        private void Start()
        {
            // Register this service
            Services.Register<IGameStateManager>(this);
            
            // Set initial state
            ChangeState(initialState);
            
            if (debugMode)
            {
                Debug.Log($"GameStateManager initialized with state: {initialState}");
            }
        }

        private void OnDestroy()
        {
            // Unregister service when destroyed
            if (Services.IsRegistered<IGameStateManager>())
            {
                Services.Unregister<IGameStateManager>();
            }
        }

        #endregion

        #region IGameStateManager Implementation

        public GameState CurrentState => currentState;

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            // Publish state change event
            EventBus.Publish(new GameStateChanged(previousState, newState));

            OnStateChanged(previousState, newState);

            if (debugMode)
            {
                Debug.Log($"Game state changed from {previousState} to {newState}");
            }
        }

        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void RestartGame()
        {
            // Clear any existing game state
            ClearGameState();
            
            // Change to playing state
            ChangeState(GameState.Playing);
        }

        #endregion

        #region Private Methods

        private void OnStateChanged(GameState previousState, GameState newState)
        {
            // Handle state transitions
            switch (newState)
            {
                case GameState.Menu:
                    OnEnterMenu();
                    break;
                
                case GameState.Playing:
                    OnEnterPlaying();
                    break;
                
                case GameState.Paused:
                    OnEnterPaused();
                    break;
            }

            // Handle state exits
            switch (previousState)
            {
                case GameState.Menu:
                    OnExitMenu();
                    break;
                
                case GameState.Playing:
                    OnExitPlaying();
                    break;
                
                case GameState.Paused:
                    OnExitPaused();
                    break;
            }
        }

        private void OnEnterMenu()
        {
            // Enable input if needed
            var inputHandler = Services.Get<IInputHandler>();
            inputHandler?.EnableInput();
            
            if (debugMode)
            {
                Debug.Log("Entered Menu state");
            }
        }

        private void OnEnterPlaying()
        {
            // Enable input
            var inputHandler = Services.Get<IInputHandler>();
            inputHandler?.EnableInput();
            
            if (debugMode)
            {
                Debug.Log("Entered Playing state");
            }
        }

        private void OnEnterPaused()
        {
            // Disable input
            var inputHandler = Services.Get<IInputHandler>();
            inputHandler?.DisableInput();
            
            if (debugMode)
            {
                Debug.Log("Entered Paused state");
            }
        }

        private void OnExitMenu()
        {
            if (debugMode)
            {
                Debug.Log("Exited Menu state");
            }
        }

        private void OnExitPlaying()
        {
            if (debugMode)
            {
                Debug.Log("Exited Playing state");
            }
        }

        private void OnExitPaused()
        {
            if (debugMode)
            {
                Debug.Log("Exited Paused state");
            }
        }

        // State management methods

        private void ClearGameState()
        {
            // Clear grid positions
            var gridManager = Services.Get<IGridManager>();
            gridManager?.ClearGrid();
            
            // Clear event bus
            EventBus.Clear();
            
            if (debugMode)
            {
                Debug.Log("Game state cleared");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Check if the game is currently playable
        /// </summary>
        public bool IsPlayable()
        {
            return currentState == GameState.Playing;
        }

        /// <summary>
        /// Check if the game is currently paused
        /// </summary>
        public bool IsPaused()
        {
            return currentState == GameState.Paused;
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug State Info")]
        public void DebugStateInfo()
        {
            Debug.Log("=== Game State Manager Info ===");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Is Playable: {IsPlayable()}");
            Debug.Log($"Is Paused: {IsPaused()}");
        }

        [ContextMenu("Start Game")]
        public void DebugStartGame()
        {
            ChangeState(GameState.Playing);
        }

        [ContextMenu("Pause Game")]
        public void DebugPauseGame()
        {
            PauseGame();
        }

        [ContextMenu("Resume Game")]
        public void DebugResumeGame()
        {
            ResumeGame();
        }

        [ContextMenu("Restart Game")]
        public void DebugRestartGame()
        {
            RestartGame();
        }

        #endregion
    }
}
