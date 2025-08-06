using UnityEngine;
using System.Collections.Generic;
using ColorBlast.Core.Data;

namespace ColorBlast.Core.Interfaces
{
    /// <summary>
    /// Interface for grid management operations
    /// </summary>
    public interface IGridManager
    {
        bool IsValidGridPosition(Vector2Int gridPosition);
        bool IsCellOccupied(Vector2Int gridPosition);
        void OccupyCell(Vector2Int gridPosition);
        void FreeCell(Vector2Int gridPosition);
        Vector3 GridToWorldPosition(Vector2Int gridPosition);
        Vector2Int WorldToGridPosition(Vector3 worldPosition);
        HashSet<Vector2Int> GetOccupiedPositions();
        void ClearGrid();
    }

    /// <summary>
    /// Interface for shape placement operations
    /// </summary>
    public interface IShapePlacer
    {
        bool CanPlaceShape(IShape shape, Vector2Int gridPosition);
        bool PlaceShape(IShape shape, Vector2Int gridPosition);
        void RemoveShape(IShape shape);
        ShapeVisualState GetPlacementFeedback(IShape shape, Vector2Int gridPosition);
    }

    /// <summary>
    /// Interface for line clearing operations
    /// </summary>
    public interface ILineClearer
    {
        List<Vector2Int> CheckForCompletedLines();
        List<Vector2Int> ClearLines(List<int> rows, List<int> columns);
        bool IsRowComplete(int row);
        bool IsColumnComplete(int column);
    }

    /// <summary>
    /// Interface for shape spawning
    /// </summary>
    public interface IShapeSpawner
    {
        void SpawnShapes(int count);
        IShape GetRandomShape();
        void SetSpawnPoints(List<Transform> spawnPoints);
        bool CanSpawn();
    }

    /// <summary>
    /// Interface for input handling
    /// </summary>
    public interface IInputHandler
    {
        bool IsDragging { get; }
        Vector3 GetWorldPosition();
        bool GetMouseDown();
        bool GetMouseUp();
        void EnableInput();
        void DisableInput();
    }

    /// <summary>
    /// Interface for shape objects
    /// </summary>
    public interface IShape
    {
        List<Vector2Int> ShapeOffsets { get; }
        Vector2Int GridPosition { get; set; }
        bool IsPlaced { get; set; }
        Transform Transform { get; }
        void SetVisualState(ShapeVisualState state);
        void DestroyShape();
    }

    /// <summary>
    /// Interface for game state management
    /// </summary>
    public interface IGameStateManager
    {
        GameState CurrentState { get; }
        void ChangeState(GameState newState);
        void PauseGame();
        void ResumeGame();
        void RestartGame();
    }
}

namespace ColorBlast.Core.Data
{
    /// <summary>
    /// Visual states for shapes
    /// </summary>
    public enum ShapeVisualState
    {
        Normal,
        Highlighted,
        Invalid,
        Placed
    }

    /// <summary>
    /// Game states
    /// </summary>
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Menu
    }
}
