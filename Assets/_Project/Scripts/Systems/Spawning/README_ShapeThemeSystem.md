# Simple Shape Sprite Manager System

This system allows you to assign different sprites (like water, land, etc.) to shapes when they spawn in your ColorBlast2 game.

## Components

### 1. ShapeSpriteManager
The main manager that handles sprite themes and applies them to shapes.

### 2. SpriteTheme (Class)
A simple class that defines a visual theme with:
- Theme name
- Tile sprite
- Spawn weight (probability)
- Placement sound (optional)

### 3. ShapeThemeStorage
A component automatically added to shapes to store their current theme.

## Setup Instructions

### Step 1: Create the Manager
1. Go to `Tools > ColorBlast2 > Shape Theme Setup`
2. Click "Create Shape Sprite Manager"
3. This creates a ShapeSpriteManager GameObject in your scene

### Step 2: Configure Themes
1. Select the ShapeSpriteManager in the hierarchy
2. In the inspector, expand "Editor Helpers"
3. Assign your water and land sprites
4. Right-click on the component and select "Setup Basic Water/Land Themes"

OR manually configure themes:
1. Expand "Sprite Themes" in the inspector
2. Set the size to 2 (for water and land)
3. Configure each theme:
   - **Theme Name**: "Water" or "Land"
   - **Tile Sprite**: Your water/land sprite
   - **Spawn Weight**: 0.5 for equal probability
   - **Placement Sound**: Optional audio clip for when shape is placed

### Step 3: Configure the ShapeSpawner
1. Find your ShapeSpawner in the scene
2. In the inspector, enable "Use Random Themes"
3. Assign the ShapeSpriteManager to the "Sprite Manager" field (optional - it will find it automatically)

## Features

### Spawning Rules
- **Randomize Themes**: Whether to use random themes or default
- **Allow Same Theme For All Shapes**: Can all shapes have the same theme?
- **Min/Max Different Themes**: Control variety in spawned shapes

## Usage Examples

### Basic Setup (Water/Land)
```csharp
// The system automatically handles theme assignment
// Just configure the themes in the inspector
```

### Programmatic Theme Assignment
```csharp
// Apply a specific theme to a shape
ShapeSpriteManager.Instance.ApplyThemeToShape(shapeGameObject, "Water");

// Get the current theme of a shape
SpriteTheme currentTheme = ShapeSpriteManager.Instance.GetShapeTheme(shapeGameObject);

// Apply random themes to multiple shapes
GameObject[] shapes = GetSpawnedShapes();
ShapeSpriteManager.Instance.ApplyRandomThemes(shapes);
```

### Adding New Themes
1. Increase the "Sprite Themes" size in the inspector
2. Configure the new theme:
   - Name (e.g., "Fire", "Ice", "Stone")
   - Sprite
   - Spawn weight
   - Placement sound (optional)

### Custom Theme Behavior
```csharp
// Check what theme a shape has
ShapeThemeStorage themeStorage = shape.GetComponent<ShapeThemeStorage>();
if (themeStorage != null)
{
    string themeName = themeStorage.GetThemeName();
    if (themeName == "Water")
    {
        // Do water-specific behavior
    }
    else if (themeName == "Land")
    {
        // Do land-specific behavior
    }
}
```

### Playing Placement Sounds
```csharp
// In your placement system, when a shape is placed:
ShapeThemeStorage themeStorage = placedShape.GetComponent<ShapeThemeStorage>();
if (themeStorage != null)
{
    themeStorage.PlayPlacementSound(); // Plays the theme's placement sound
}
```

## Integration with Existing Systems

### ShapeSpawner Integration
The system automatically integrates with your existing ShapeSpawner. When shapes are spawned, themes are automatically applied based on your configuration.

### PlacementSystem Integration
When shapes are placed, you can play theme-specific sounds:
```csharp
// In your placement code
ShapeThemeStorage themeStorage = placedShape.GetComponent<ShapeThemeStorage>();
if (themeStorage != null)
{
    themeStorage.PlayPlacementSound();
}
```

### ShapeCreatorTool Integration
New shapes created with the ShapeCreatorTool automatically include the ShapeThemeStorage component for theme support.

## Tips

1. **Balanced Spawn Weights**: Use equal weights (0.5 each) for water and land for 50/50 distribution
2. **Visual Consistency**: Use similar art styles for all theme sprites
3. **Performance**: The system is optimized and uses caching for better performance
4. **Debugging**: Enable "Show Debug Info" in the manager to see theme applications in the console
5. **Audio**: Keep audio clips short for better performance

## Troubleshooting

**Q: Themes aren't being applied to shapes**
A: Check that "Use Random Themes" is enabled in the ShapeSpawner and that you have configured at least one theme.

**Q: All shapes have the same theme**
A: Set "Allow Same Theme For All Shapes" to false and adjust min/max different themes.

**Q: Custom sprites aren't showing**
A: Make sure your sprites are properly imported and set to "Sprite" texture type.

**Q: No placement sounds**
A: Ensure AudioSource components can be added to GameObjects and that audio clips are assigned to themes.

This simplified system provides an easy way to add visual variety to your shapes with different sprites.
