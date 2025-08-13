using UnityEngine;
using System;
using System.Collections.Generic;
using ColorBlast.Core.Data;

namespace ColorBlast.Core.Events
{
    /// <summary>
    /// Central event system for decoupled communication between systems
    /// </summary>
    public static class EventBus
    {
        private static Dictionary<Type, List<object>> eventHandlers = new Dictionary<Type, List<object>>();

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Type eventType = typeof(T);
            
            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<object>();
            }
            
            eventHandlers[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Type eventType = typeof(T);
            
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);
                
                if (eventHandlers[eventType].Count == 0)
                {
                    eventHandlers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            Type eventType = typeof(T);
            
            if (eventHandlers.ContainsKey(eventType))
            {
                foreach (var handler in eventHandlers[eventType])
                {
                    try
                    {
                        ((Action<T>)handler).Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error handling event {eventType.Name}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clear all event handlers (useful for scene transitions)
        /// </summary>
        public static void Clear()
        {
            eventHandlers.Clear();
        }

        /// <summary>
        /// Get number of handlers for a specific event type (for debugging)
        /// </summary>
        public static int GetHandlerCount<T>() where T : IGameEvent
        {
            Type eventType = typeof(T);
            return eventHandlers.ContainsKey(eventType) ? eventHandlers[eventType].Count : 0;
        }
    }

    /// <summary>
    /// Base interface for all game events
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Event fired when lines are cleared
    /// </summary>
    public struct LinesCleared : IGameEvent
    {
        public List<Vector2Int> ClearedPositions { get; }
        public int LinesCount { get; }
        public int Score { get; }

        public LinesCleared(List<Vector2Int> clearedPositions, int linesCount, int score)
        {
            ClearedPositions = clearedPositions;
            LinesCount = linesCount;
            Score = score;
        }
    }

    /// <summary>
    /// Event fired when a shape is placed
    /// </summary>
    public struct ShapePlaced : IGameEvent
    {
        public Vector2Int GridPosition { get; }
        public List<Vector2Int> ShapeOffsets { get; }

        public ShapePlaced(Vector2Int gridPosition, List<Vector2Int> shapeOffsets)
        {
            GridPosition = gridPosition;
            ShapeOffsets = shapeOffsets;
        }
    }

    /// <summary>
    /// Event fired when shapes are spawned
    /// </summary>
    public struct ShapesSpawned : IGameEvent
    {
        public int Count { get; }

        public ShapesSpawned(int count)
        {
            Count = count;
        }
    }

    /// <summary>
    /// Event fired when game state changes
    /// </summary>
    public struct GameStateChanged : IGameEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }

        public GameStateChanged(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event fired when shapes are destroyed
    /// </summary>
    public struct ShapesDestroyed : IGameEvent
    {
        public int Count { get; }
        public List<Vector2Int> AffectedPositions { get; }

        public ShapesDestroyed(int count, List<Vector2Int> affectedPositions)
        {
            Count = count;
            AffectedPositions = affectedPositions;
        }
    }

    /// <summary>
    /// Event fired for audio feedback
    /// </summary>
    public struct AudioEvent : IGameEvent
    {
        public AudioType Type { get; }
        public Vector3 Position { get; }

        public AudioEvent(AudioType type, Vector3 position = default)
        {
            Type = type;
            Position = position;
        }
    }

    /// <summary>
    /// Types of audio events
    /// </summary>
    public enum AudioType
    {
        ShapePlaced,
        LineCleared,
        ShapeDestroyed,
        InvalidPlacement
    }
}
