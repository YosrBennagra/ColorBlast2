# Mobile Grid & Shape Layout Guide - REWORKED SYSTEM

## New Simple Grid System

The grid has been completely reworked for simplicity and reliability!

### Key Changes:
- ✅ **Simple positioning** - Just move the GridManager GameObject
- ✅ **Automatic centering** - Grid centers itself around the GameObject position  
- ✅ **Real-time preview** - Wireframe updates instantly when you move
- ✅ **No confusing origins** - Everything is based on GameObject position
- ✅ **Reliable visualization** - Preview matches runtime exactly

## Recommended Mobile Layout (Portrait 1080x1920)

### Camera Setup
```
Main Camera Position: (0, 0, -10)
Camera Size (Orthographic): 10 (adjust as needed)
```

### Grid Setup (New System)
```
GridManager Position: (0, -2, 0) - Move this to position the grid!
Grid Width: 8 (fewer columns for mobile)
Grid Height: 10 (good height for mobile)  
Cell Size: 0.8 (smaller cells for mobile)
Cell Spacing: 0.0 (no gaps by default - classic style)
Use Uniform Cell Size: ✅ True (or set custom width/height)
Use Uniform Spacing: ✅ True (or set custom X/Y spacing)
Show Visual Grid: ✅ True (to see wireframe in Scene view)
```

> ✅ **Simple Rule**: Move the GridManager GameObject to position your grid. That's it!

### Spawn Points Setup (Top Area)
```
SpawnPoint_1: (-2, 3, 0)   // Top-left
SpawnPoint_2: (0, 3, 0)    // Top-center  
SpawnPoint_3: (2, 3, 0)    // Top-right
```

### UI Layout (Top Area)
```
Score Text: Top-left (anchored)
High Score: Top-center (anchored)
Settings: Top-right (anchored)
```

## How The New Grid Works

### Visual Preview in Scene View
- **Blue wireframe squares** = Exact grid cell positions
- **Yellow boundary box** = Overall grid area  
- **Red cube** = Grid center (GameObject position)

### Positioning the Grid
1. **Select GridManager** in Hierarchy
2. **Move the Transform Position** - grid follows immediately!
3. **Grid auto-centers** around the GameObject position
4. **Preview updates in real-time** as you drag

### Grid Coordinate System
- **Bottom-left cell** = (0, 0)
- **Grid centers itself** around the GameObject position
- **All coordinates** calculated automatically from GameObject position

## Simple Mobile Setup Steps

1. **Create GridManager** in your scene
2. **Set Position** to (0, -2, 0) to center grid in lower screen area
3. **Set Cell Size** to 0.8 for mobile-friendly touch targets
4. **Set Grid Width** to 8 (good for mobile screens)
5. **Set Grid Height** to 10 (good mobile height)
6. **Position spawn points** above the grid at y=3
7. **Adjust camera size** to fit everything

## New Inspector Features

### Grid Information Display
- Shows total cell count (Width x Height)
- Shows actual grid size in Unity units
- Color-coded preview legend
- Dynamic cell size information (uniform or custom)

### Cell Sizing Options
- **Uniform Cell Size** - Single size for all cells (default)
- **Custom Cell Size** - Separate width and height for rectangular cells
- **Runtime Controls** - Change cell size during play mode
- **Auto-refresh** - Grid updates automatically when size changes

### Cell Spacing Options
- **Uniform Spacing** - Single spacing value for all gaps (default)
- **Custom Spacing** - Separate X/Y spacing for different gap sizes
- **Zero Spacing** - No gaps between cells (classic Tetris style)
- **Runtime Controls** - Adjust spacing during play mode

### Useful Buttons
- **"Refresh Grid Preview"** - Force Scene view update
- **"Refresh Visual Grid"** - Recreate runtime visual tiles (Play mode)
- **"Clear All Occupied Cells"** - Reset grid state (Play mode)

## Advanced Cell Sizing Features

### Uniform vs Custom Cell Sizing
- **Uniform Mode** (Default): Single cell size for perfect squares
- **Custom Mode**: Separate width/height for rectangular cells

### Runtime Cell Size Control
```csharp
// Change cell size at runtime
gridManager.SetCellSize(1.0f);

// Set custom width/height
gridManager.SetCellSize(0.8f, 1.2f); // width, height

// Toggle uniform mode
gridManager.SetUniformCellSize(false);

// With validation (min/max limits)
gridManager.SetCellSize(0.5f, 0.1f, 2.0f); // size, min, max
```

### Runtime Cell Spacing Control
```csharp
// Change spacing at runtime
gridManager.SetCellSpacing(0.1f);

// Set custom X/Y spacing
gridManager.SetCellSpacing(0.05f, 0.15f); // spacingX, spacingY

// Toggle uniform spacing
gridManager.SetUniformSpacing(false);

// No gaps (classic style)
gridManager.SetCellSpacing(0f);
```

### Mobile-Optimized Settings
- **Phone Portrait**: 
  - Cell Size: 0.6 - 0.8 (smaller for touch)
  - Cell Spacing: 0.05 - 0.1 (subtle gaps)
- **Tablet Portrait**: 
  - Cell Size: 0.8 - 1.0 (larger screen)
  - Cell Spacing: 0.1 - 0.15 (visible gaps)
- **Landscape**: 
  - Cell Size: 0.7 - 0.9 (adjust for wider screen)
  - Cell Spacing: 0.08 - 0.12 (balanced gaps)

### Spacing Styles
- **Classic (No gaps)**: Spacing = 0.0
- **Modern (Subtle)**: Spacing = 0.05 - 0.1
- **Spaced (Clear)**: Spacing = 0.1 - 0.2
- **Wide (Dramatic)**: Spacing = 0.2+

### Inspector Controls
- **Cell Size**: Primary size control
- **Cell Spacing**: Gap between cells control
- **Use Uniform Cell Size**: Toggle for rectangular cells
- **Use Uniform Spacing**: Toggle for custom X/Y spacing
- **Cell Width/Height**: Individual dimension controls
- **Cell Spacing X/Y**: Individual spacing controls
- **Runtime Controls**: Live adjustment during play mode

## Benefits of New System

✅ **Much simpler** - No confusing Grid Origin settings
✅ **Intuitive positioning** - Move GameObject = move grid
✅ **Real-time preview** - See changes immediately
✅ **Reliable visualization** - Preview exactly matches runtime
✅ **Better performance** - Optimized grid calculations
✅ **Cleaner code** - Simplified, well-documented methods
✅ **Accurate shape placement** - Fixed grid position calculations

## Troubleshooting Shape Placement

If shapes are not placing correctly on the grid:

1. **Check GridPositionDebugger**: Add the GridPositionDebugger component to test position conversions
2. **Verify Grid Settings**: Ensure cell size and spacing match your desired layout  
3. **Test Position Conversions**: Use the debugger's context menu "Test Grid Positioning"
4. **Check Console**: Look for position conversion warnings

### Grid Position Debugging
- Add `GridPositionDebugger` component to any GameObject for real-time position testing
- Enable "Log Position Conversions" to see detailed conversion data
- Use gizmos to visualize grid cells and position conversions
- Context menu "Test Grid Positioning" for manual testing

## Migration from Old System

If you have an existing GridManager:
1. **Note your current grid position** and settings
2. **Delete the old GridManager** component
3. **Add the new GridManager** component  
4. **Set the new simple settings** (no Grid Origin needed!)
5. **Position using GameObject Transform** only
```
