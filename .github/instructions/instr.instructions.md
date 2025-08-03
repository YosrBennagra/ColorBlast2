# 🎮 Unity 2D Grid Puzzle Game – Core Logic Instructions (Updated)

## 📌 Game Overview
Implement the core logic for a 2D Unity mobile puzzle game with the following mechanics:
- A fixed **8x8 grid**
- Players can **drag and drop shapes** (L-shape, T-shape, and 1x1 block) onto the grid
- Once placed, shapes become fixed and **cannot be moved**
- If a **row or column is completely filled**, it is **cleared from the grid**
- The player is shown **3 shapes at a time**
  - When all 3 have been placed, **automatically spawn 3 new shapes**
- Focus only on:
  - Grid logic
  - Drag-and-drop mechanics
  - Line clearing
  - Shape cycling (3 at a time)
- Do **not** implement full UI, scoring, or animations

---

## 🧠 Game Logic Requirements

### 1. Grid System
- Represent the grid as a `Cell[,]` 2D array (8x8)
- Each `Cell` holds:
  - `isFilled`
  - Optional reference to the occupying block GameObject
- `GridManager.cs` handles grid setup, cell state, and lookup helpers

### 2. Shapes
- Define shape types:
  - L-shape
  - T-shape
  - 1x1 block
- Shapes can be represented by:
  - A list of Vector2Int offsets (relative to a pivot)
  - A parent GameObject with child blocks (`ShapeBlock.cs`)
- Shapes are stored as prefabs and instantiated when needed

### 3. Drag and Drop
- Shapes can be dragged from the **"current 3 shapes" area**
- While dragging:
  - Show a preview on the grid (optional)
- On drop:
  - Validate shape fits on the grid and doesn't overlap filled cells
  - If valid, place the shape and lock it to the grid
  - Mark affected cells as filled
  - Remove the used shape from the current set

### 4. Line Clearing
- After each placement:
  - Check all rows and columns
  - If any are completely filled, clear them (set unfilled, destroy blocks)
  - Update grid state accordingly

### 5. Shape Set Cycling (New!)
- Maintain a list of 3 active shapes in the scene
- After placing all 3 shapes:
  - Automatically instantiate 3 new random shapes from available prefabs
- Only allow placement of active shapes
- Shapes can be placed in any order
- If none of the current 3 can be placed, game over logic may be added later (not required now)

---

## 🗂 Folder & Script Structure

/Scripts/
│
├── Grid/
│ ├── GridManager.cs // Manages grid state and utility functions
│ └── Cell.cs // Individual cell data
│
├── Shapes/
│ ├── Shape.cs // Represents a shape instance
│ ├── ShapeBlock.cs // Single block in a shape
│ └── ShapeData.cs // (Optional) stores shape layout info
│
├── Gameplay/
│ ├── ShapePlacer.cs // Handles drag-and-drop and placement
│ ├── LineClearer.cs // Clears full rows/columns
│ └── ShapeSpawner.cs // Spawns 3 new shapes when needed

---

## 🧼 Code Organization Rules

- Use **PascalCase** for public classes/methods, **camelCase** for variables
- Keep each file short and focused (single-responsibility)
- Recommended method breakdowns:
  - `IsValidPlacement()`
  - `PlaceShape()`
  - `ClearFullLines()`
  - `SpawnNewShapes()`
- Document key methods using `///` summary tags

---

## 🛑 Do NOT Implement
- UI or drag visuals
- Score system
- Game over logic (optional later)
- Shape randomization fairness/balance logic (simple random selection is fine)

---

## ✅ Goal
Build modular, readable code for a shape-dropping puzzle game on an 8x8 grid where:
- You can drag-and-drop 3 shapes at a time
- Full rows/columns are cleared
- New shapes are automatically spawned when 3 are used
