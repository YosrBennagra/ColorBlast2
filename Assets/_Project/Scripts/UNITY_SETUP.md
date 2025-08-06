# ColorBlast2 - Unity Editor Setup Guide

## 🎯 Quick Setup Checklist

- [ ] Create GameManager GameObject
- [ ] Create GridManager GameObject and position it
- [ ] Setup Shape prefabs with new components
- [ ] Setup Mobile UI Canvas with GameUIManager
- [ ] Setup ShapeSpawner with spawn points (optional)
- [ ] Test initialization
- [ ] Verify functionality

---

## 📋 Step-by-Step Setup

### Step 1: Create GameManager
1. **Right-click in Hierarchy** → Create Empty
2. **Rename** to `GameManager`
3. **Add Component** → Search for `GameManager` (Core.GameManager)
4. **Configure Inspector Settings**:
   ```
   ✅ Auto Initialize: ✓
   ✅ Persist Across Scenes: ✓ 
   ✅ Enable Debug Logs: ✓
   ```
5. **Position**: Set Transform to (0, 0, 0)

> ⚠️ **Important**: The GameManager will move to "DontDestroyOnLoad" section during play - this is normal!

### Step 2: Create GridManager
1. **Right-click in Hierarchy** → Create Empty
2. **Rename** to `GridManager`
3. **Add Component** → Search for `GridManager` (Gameplay.GridManager)
4. **Position the GridManager** where you want your game grid to be located
5. **Configure Grid Settings** in the Inspector:
   ```
   Grid Size: 1.0 (size of each grid cell)
   Grid Origin: (0, 0, 0) (adjust to position grid)
   Grid Width: 10 (number of columns)
   Grid Height: 10 (number of rows)
   ```
6. **Configure Visual Settings** in the Inspector:
   ```
   ✅ Show Grid: ✓ (to make grid visible)
   Tile Prefab: [Optional - drag a prefab here for custom tiles]
   Grid Material: [Optional - drag a material for custom appearance]
   ```
7. **Press Play or use the RefreshVisualGrid() method** to see your grid!

> 💡 **Visual Grid Options**: 
> - **With Tile Prefab**: Drag any prefab to create custom grid tiles
> - **Without Tile Prefab**: Automatically creates simple transparent squares
> - **Grid Material**: Apply custom materials for different visual styles
> - **Show Grid**: Toggle to hide/show the grid anytime

> 💡 **Benefits of Manual Setup**: 
> - **Visual Control**: See exactly where your grid will be positioned
> - **Easy Adjustment**: Move the GridManager GameObject to reposition the entire grid
> - **Scene Organization**: Keep your scene hierarchy clean and organized
> - **Design Flexibility**: Position grid relative to UI elements, cameras, or other objects

> 💡 **Tip**: Position the GridManager GameObject to visually place your grid exactly where you want it in the game world!

### Step 3: Setup Shape Prefabs

#### Using the Shape Creator Tool (Recommended):
1. **Open Tools Menu** → ColorBlast2 → Shape Creator
2. **Configure Settings**:
   ```
   Tile Prefab: [Optional - drag a tile prefab for custom shapes]
   Grid Size: 1.0 (match your GridManager grid size)
   Tile Material: [Optional - for custom appearance]
   ```
3. **Click shape buttons** to auto-generate prefabs:
   - Single Square
   - L-Shape, T-Shape, I-Shape, O-Shape, Z-Shape, S-Shape
4. **Prefabs are saved** to `Assets/_Project/Prefabs/Shapes/`

#### Manual Setup (Alternative):
**For Each Shape Prefab:**
1. **Open the prefab** in Prefab Mode (double-click in Project)
2. **Add Components**:
   - Add `Shape` component (Core.Shape)
   - Add `DragHandler` component (Gameplay.DragHandler)
3. **Configure Shape Component**:
   - **Shape Offsets**: Set the relative positions of tiles
     ```
     Example for L-shape: (0,0), (1,0), (2,0), (0,1)
     ```
   - **Grid Size**: Usually 1.0 (match your grid tile size)
4. **Configure DragHandler Component**:
   ```
   ✅ Return To Spawn On Invalid Placement: ✓
   ✅ Use Return Animation: ✓
   ✅ Show Invalid Placement Feedback: ✓
   Return Animation Duration: 0.3
   ```
5. **Save the prefab**

#### Required Components on Shape Prefabs:
- **Transform**: Position and rotation
- **Collider2D**: For mouse detection (BoxCollider2D recommended)
- **SpriteRenderer**: Visual representation
- **Shape**: Shape data and configuration
- **DragHandler**: Dragging behavior

## 📱 Setting Up Simple Main Game UI (Minimal Mobile-Friendly Interface)

If you prefer a clean, minimal UI for the main game scene with only essential elements:

### Step 1: Create Simple UI Canvas
1. Right-click in Hierarchy → UI → Canvas
2. Name it "SimpleGameCanvas"
3. Set Canvas component settings:
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: ✅ (for crisp text)

### Step 2: Configure Canvas Scaler for Mobile
1. Add Canvas Scaler component to the Canvas
2. Set UI Scale Mode: Scale With Screen Size
3. Set Reference Resolution: 1080 x 1920 (portrait mobile)
4. Set Screen Match Mode: Match Width Or Height
5. Set Match: 0.5 (balanced scaling)

### Step 3: Create UI Elements
Create the following UI elements as children of the Canvas:

#### Score Text (top-left)
1. Right-click Canvas → UI → Text - TextMeshPro
2. Name: "ScoreText"
3. Set text: "Score: 0"
4. Position: Top-left corner with padding
5. Anchor: Top-Left
6. Font Size: 36-48 for mobile readability

#### High Score Text (top-center)
1. Right-click Canvas → UI → Text - TextMeshPro
2. Name: "HighScoreText"
3. Set text: "Best: 0"
4. Position: Top-center
5. Anchor: Top-Center
6. Font Size: 36-48 for mobile readability

#### Settings Button (top-right)
1. Right-click Canvas → UI → Button - TextMeshPro
2. Name: "SettingsButton"
3. Set button text: "Settings" or "⚙️"
4. Position: Top-right corner with padding
5. Anchor: Top-Right
6. Size: 80x80 for mobile touch targets

### Step 4: Add Simple UI Manager
1. Add `SimpleGameUI` component to the Canvas
2. Drag the UI elements to their respective fields:
   - Score Text → scoreText field
   - High Score Text → highScoreText field
   - Settings Button → settingsButton field

### Step 5: Add Simple UI Integration
1. Add `SimpleUIIntegration` component to the Canvas
2. The Game UI field should auto-populate with the SimpleGameUI component
3. Configure scoring settings:
   - Points Per Line: 100
   - Bonus Per Level: 50
   - Shape Bonus: 10

### Step 6: Position Game Area
Position your GridManager and game elements to leave space for the minimal UI:
- Keep the top ~150 pixels clear for the UI elements
- Center the game grid in the remaining space
- Ensure touch targets don't overlap with the game area

#### Grid Scaling and Positioning Guide:

**Grid Position Control:**
1. **Select GridManager GameObject** in Hierarchy
2. **Adjust Transform Position** to move the entire grid:
   ```
   Mobile Portrait: (0, -3, 0) - positions grid in lower area
   Landscape: (0, -1, 0) - centers grid vertically
   ```

**Grid Scale Control:**
1. **In GridManager Inspector**, adjust **Grid Size**:
   ```
   Mobile: 0.6-0.8 (smaller for touch accuracy)
   Desktop: 1.0 (standard size)
   Tablet: 1.2 (larger for bigger screens)
   ```

**Shape Spawn Positioning:**
1. **Position your 3 Spawn Points** above the grid:
   ```
   SpawnPoint_1: (-2.5, 4, 0) - Left spawn
   SpawnPoint_2: (0, 4, 0) - Center spawn
   SpawnPoint_3: (2.5, 4, 0) - Right spawn
   ```

**Shape Scale Matching:**
- **Always match** `Shape.gridSize` to `GridManager.gridSize` in Inspector
- **If GridManager uses 0.8**, set all Shape prefabs to **Grid Size: 0.8**

> 💡 **Mobile Layout Tip**: Use **Grid Size: 0.8** and **Grid Width: 8** for optimal mobile touch experience!

### Step 7: Test on Mobile Preview
1. Set Game view to a mobile resolution (e.g., 1080x1920)
2. Verify all UI elements are clearly visible
3. Ensure text is readable at mobile sizes
4. Test that the Settings button is easily touchable

## 📱 Setting Up Full-Featured Mobile UI (Complete Interface)

For a more complete mobile experience with additional features:

1. **Right-click in Hierarchy** → UI → Canvas
2. **Rename** to `Mobile UI Canvas`
3. **Add Component** → Search for `GameUIManager` (Systems.UI.GameUIManager)
4. **Add Component** → Search for `UIIntegration` (Systems.UI.UIIntegration)
5. **Configure Canvas for Mobile**:
   ```
   Render Mode: Screen Space - Overlay
   Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1080 x 1920 (mobile portrait)
   - Screen Match Mode: Match Width Or Height
   - Match: 0.5
   ```

#### Create UI Panels:
1. **Game Panel** (Right-click Canvas → UI → Panel):
   ```
   - Score Text (UI → Text - TextMeshPro)
   - Lines Text (UI → Text - TextMeshPro)  
   - Level Text (UI → Text - TextMeshPro)
   - Pause Button (UI → Button - TextMeshPro)
   ```

2. **Game Over Panel** (Right-click Canvas → UI → Panel):
   ```
   - Final Score Text (UI → Text - TextMeshPro)
   - High Score Text (UI → Text - TextMeshPro)
   - Play Again Button (UI → Button - TextMeshPro)
   - Main Menu Button (UI → Button - TextMeshPro)
   ```

3. **Game Area** (Right-click Canvas → UI → Panel):
   ```
   - This panel will contain your grid and shapes
   - Position it in the center, leaving space for UI elements
   ```

> 💡 **Mobile UI Features**: 
> - **Auto-scaling**: Adapts to different screen sizes and orientations
> - **Touch-friendly**: Large buttons optimized for finger taps
> - **Centered gameplay**: Game area automatically positioned in screen center
> - **Score tracking**: Automatic scoring system with high score persistence

### Step 5: Setup Shape Spawner (Optional but Recommended)
1. **Right-click in Hierarchy** → Create Empty
2. **Rename** to `ShapeSpawner`
3. **Add Component** → Search for `ShapeSpawner`
4. **Create 3 Spawn Points**:
   - Right-click in Hierarchy → Create Empty (3 times)
   - Rename them: `SpawnPoint_1`, `SpawnPoint_2`, `SpawnPoint_3`
   - Position them where you want shapes to appear
5. **Configure ShapeSpawner Settings**:
   ```
   Shape Prefabs: [Drag your shape prefabs from Project to this array]
   Spawn Points: [Drag the 3 spawn point GameObjects here]
   ✅ Auto Spawn On Start: ✓
   Spawn Effect Duration: 0.3
   Min Spawn Scale: 0.1
   Max Spawn Scale: 1.0
   ```

> 💡 **ShapeSpawner Features**: 
> - **Automatic Spawning**: Spawns 3 new shapes when all current shapes are placed
> - **Spawn Animation**: Scales shapes in with a smooth effect
> - **Smart Detection**: Tracks shape placement and line clearing
> - **Manual Control**: Force spawn or clear shapes via public methods

### Step 6: Scene Hierarchy Organization
```
📁 Main Scene
├── 🎮 GameManager (GameManager component)
├── 🟦 GridManager (GridManager component)
│   └── 📁 Grid_Visual (auto-generated)
│       ├── GridTile_0_0
│       ├── GridTile_0_1
│       ├── GridTile_1_0
│       ├── GridTile_1_1
│       └── ... (all grid tiles)
├── 🚀 ShapeSpawner (ShapeSpawner component)
├── 📍 SpawnPoint_1 (Empty GameObject)
├── 📍 SpawnPoint_2 (Empty GameObject)  
├── 📍 SpawnPoint_3 (Empty GameObject)
├──  Main Camera
├── 📁 Shapes (Empty GameObject - organizer for spawned shapes)
│   ├── 🟦 Shape_0_1234 (dynamically spawned)
│   ├── 🟨 Shape_1_5678 (dynamically spawned)
│   ├── 🟪 Shape_2_9012 (dynamically spawned)
│   └── ... (other dynamically spawned shapes)
├── 📱 Mobile UI Canvas (Canvas + GameUIManager + UIIntegration)
│   ├── 🎮 Game Panel
│   │   ├── Score Text
│   │   ├── Lines Text
│   │   ├── Level Text
│   │   └── Pause Button
│   ├── 🏁 Game Over Panel
│   │   ├── Final Score Text
│   │   ├── High Score Text
│   │   ├── Play Again Button
│   │   └── Main Menu Button
│   ├── ⏸️ Pause Panel
│   │   ├── Resume Button
│   │   ├── Restart Button
│   │   └── Main Menu Button
│   └── 🎯 Game Area (Panel for game positioning)
└── 🔊 Audio Manager (if you have one)
```

### Step 7: Test the Setup

#### Initial Test:
1. **Press Play**
2. **Check Console** for these messages (in order):
   ```
   🎮 GameManager created and ready
   🎮 GameManager created and set to persist across scenes
   🟢 GameManager OnEnable called
   🎮 Initializing ColorBlast2 Game Systems...
   ✅ GridManager found and registered
   ✅ PlacementSystem registered  
   ✅ LineClearSystem registered
   ✅ ShapeDestructionSystem registered
   🎉 Game systems initialized successfully!
   ```

#### Verify Hierarchy During Play:
- Look for **"DontDestroyOnLoad"** section at bottom of Hierarchy
- GameManager should be there (this is correct behavior!)

#### Functionality Test:
1. **Drag a shape** - should follow mouse smoothly
2. **Drop on grid** - should snap to grid positions
3. **Invalid placement** - should return to spawn with animation
4. **Complete a line** - should clear automatically
5. **No console errors** - should see only success messages

---

## ⚙️ Configuration Options

### GameManager Settings
| Setting | Description | Recommended |
|---------|-------------|-------------|
| Auto Initialize | Automatically start all systems | ✅ True |
| Persist Across Scenes | Keep GameManager when changing scenes | ✅ True |
| Enable Debug Logs | Show initialization messages | ✅ True (for setup) |

### GridManager Settings (manually created)
| Setting | Description | Default |
|---------|-------------|---------|
| Grid Size | Size of each grid cell | 1.0 |
| Grid Origin | World position of grid center | (0,0,0) |
| Grid Width | Number of columns | 10 |
| Grid Height | Number of rows | 10 |
| Show Grid | Display visual grid tiles | ✅ True |
| Tile Prefab | Custom prefab for grid tiles | None (auto-generates) |
| Grid Material | Material for grid appearance | None (default) |

### Shape Component Settings
| Setting | Description | Example |
|---------|-------------|---------|
| Shape Offsets | Relative tile positions | (0,0), (1,0), (0,1) |
| Grid Size | Size of each grid cell | 1.0 |

### DragHandler Settings
| Setting | Description | Recommended |
|---------|-------------|-------------|
| Return To Spawn On Invalid Placement | Return shape if placement fails | ✅ True |
| Use Return Animation | Smooth vs instant return | ✅ True |
| Show Invalid Placement Feedback | Red flash on invalid drop | ✅ True |
| Return Animation Duration | Animation speed | 0.3 seconds |

### ShapeSpawner Settings (optional)
| Setting | Description | Recommended |
|---------|-------------|-------------|
| Shape Prefabs | Array of shape prefabs to spawn | All your shape prefabs |
| Spawn Points | 3 Transform positions for spawning | 3 positioned GameObjects |
| Auto Spawn On Start | Spawn shapes immediately when game starts | ✅ True |
| Spawn Effect Duration | Animation time for spawn effect | 0.3 seconds |
| Min Spawn Scale | Starting scale for spawn animation | 0.1 |
| Max Spawn Scale | Final scale for spawn animation | 1.0 |

### Mobile UI Settings (recommended for mobile)
| Setting | Description | Recommended |
|---------|-------------|-------------|
| Auto Configure Mobile | Automatically setup mobile-optimized settings | ✅ True |
| Mobile Reference Resolution | Base resolution for scaling | 1080 x 1920 (portrait) |
| Mobile Match Value | Width/Height match preference | 0.5 (balanced) |
| Game Area Padding | Space around game area | 50 pixels |
| Points Per Line | Score for clearing one line | 100 points |
| Bonus Per Level | Additional points per level | 50 points |
| Shape Bonus | Points for placing a shape | 10 points |

### Simple Main Game UI Settings (minimal UI for focused gameplay)
| Setting | Description | Recommended |
|---------|-------------|-------------|
| Score Text | Current score display | TextMeshPro component |
| High Score Text | Best score display | TextMeshPro component |
| Settings Button | Access to settings scene | Button component |
| UI Scale Mode | Canvas scaler setting | Scale With Screen Size |
| Reference Resolution | Mobile-friendly resolution | 1080 x 1920 |
| Match Width/Height | Scaling balance | 0.5 (balanced) |

---

## 🐛 Troubleshooting

### ❌ "GameManager disappears when playing"
**Solution**: This is normal! Check the "DontDestroyOnLoad" section at the bottom of the Hierarchy.

### ❌ "GridManager not found! Please create a GridManager GameObject manually in the scene."
**Solution**: You need to manually create a GridManager GameObject:
1. Right-click in Hierarchy → Create Empty
2. Rename to "GridManager"
3. Add Component → Search for "GridManager" (Gameplay.GridManager)
4. Position and configure as desired

### ❌ "Grid is invisible/not showing"
**Causes & Solutions**:
- Show Grid unchecked → Check "Show Grid" in GridManager inspector
- Grid positioned off-camera → Adjust GridManager position or camera position
- Grid behind other objects → Check sorting layers or Z-positions
- Grid too transparent → Adjust Grid Material alpha or use a custom Tile Prefab

### ❌ "Service [SystemName] not found" errors
**Causes & Solutions**:
- GameManager not in scene → Add GameManager GameObject
- GridManager not in scene → Add GridManager GameObject manually
- Auto Initialize unchecked → Check "Auto Initialize" in GameManager
- GameManager inactive → Ensure GameManager GameObject is active
- GridManager inactive → Ensure GridManager GameObject is active
- Initialization failed → Check Console for error messages

### ❌ Shapes won't drag
**Causes & Solutions**:
- Missing Collider2D → Add BoxCollider2D to shape
- Missing DragHandler → Add DragHandler component
- Camera.main not set → Tag your camera as "MainCamera"
- Shape prefab not configured → Follow Step 3 setup

### ❌ Shapes don't snap to grid
**Causes & Solutions**:
- Grid configuration incorrect → Adjust GridManager's Grid Width/Height and Grid Size in inspector
- Grid size mismatch → Match Shape.gridSize to GridManager.gridSize
- PlacementSystem not initialized → Check GameManager initialization

### ❌ Line clearing not working
**Causes & Solutions**:
- LineClearSystem not registered → Check GameManager initialization messages
- Grid state not updating → Verify PlacementSystem is working
- Complete lines not detected → Check grid dimensions and shape placement

### ❌ ShapeSpawner not spawning shapes
**Causes & Solutions**:
- Shape Prefabs array empty → Drag shape prefabs to Shape Prefabs array
- Spawn Points not set → Assign 3 Transform references to Spawn Points
- Auto Spawn On Start disabled → Check "Auto Spawn On Start" or call ForceSpawnNewShapes()
- Shape prefabs missing components → Ensure each prefab has Shape + DragHandler components

### ❌ Shapes spawn but disappear immediately
**Causes & Solutions**:
- Spawn points positioned off-camera → Move spawn point GameObjects to visible positions
- Shapes spawning inside colliders → Check spawn point positions for overlaps
- Shape prefabs corrupted → Verify shape prefabs have all required components

### ❌ UI not scaling properly on mobile
**Causes & Solutions**:
- Canvas Scaler not configured → Set UI Scale Mode to "Scale With Screen Size"
- Wrong reference resolution → Use 1080x1920 for portrait mobile games
- Match value incorrect → Set Match Width Or Height to 0.5 for balanced scaling
- Auto Configure Mobile disabled → Check "Auto Configure Mobile" in GameUIManager

### ❌ "Play Again" button not working
**Causes & Solutions**:
- Button not assigned → Drag Play Again button to GameUIManager's playAgainButton field
- UIIntegration missing → Add UIIntegration component to the Canvas
- Event handlers not set → Check that GameUIManager and UIIntegration are on same GameObject

### ❌ Game area not centered
**Causes & Solutions**:
- Game Area not assigned → Create a Panel and assign it to GameUIManager's gameArea field
- Position Game Area incorrect → Set anchors to center (0.5, 0.5) for both min and max
- Camera position wrong → Position GridManager relative to Game Area center

### ❌ Grid or shapes wrong size/position
**Causes & Solutions**:
- **Grid too big for mobile** → Reduce GridManager's Grid Size to 0.6-0.8
- **Grid off-screen** → Adjust GridManager GameObject Transform position
- **Shapes don't match grid** → Ensure Shape.gridSize = GridManager.gridSize
- **Spawn points wrong** → Position spawn point GameObjects above the grid area
- **Grid not visible** → Check camera size, grid might be outside camera view
- **Shapes too small/big** → Match all Shape prefab Grid Size to GridManager setting

### ❌ Mobile layout issues
**Causes & Solutions**:
- **Grid overlaps UI** → Move GridManager position down (e.g., y = -3)
- **Touch targets too small** → Increase Grid Size or reduce Grid Width/Height
- **Shapes spawn off-screen** → Position spawn points within camera bounds
- **UI overlaps game** → Increase padding between UI elements and game area

---

## 🔧 Advanced Configuration

### Custom Grid Sizes
If your grid tiles are not 1x1 Unity units:
1. Set `Shape.gridSize` to match your tile size  
2. Set `GridManager.gridSize` to the same value
3. Update any hardcoded position calculations

### Multiple Scenes
The GameManager persists across scenes automatically:
- First scene: GameManager initializes all systems
- Subsequent scenes: GameManager and systems remain active
- No need to add GameManager to every scene

### Performance Optimization
For better performance in large grids:
- Enable object pooling for shapes (custom implementation)
- Use batch operations for multiple line clears
- Consider using Unity's Job System for complex grid operations

---

## ✅ Validation Checklist

After setup, verify:

### Scene Setup:
- [ ] GameManager GameObject exists and is active
- [ ] GameManager has Core.GameManager component
- [ ] GridManager GameObject exists and is active
- [ ] GridManager has Gameplay.GridManager component
- [ ] GridManager is positioned where you want the grid
- [ ] Canvas exists with either SimpleGameUI or GameUIManager component
- [ ] UI components are properly assigned (score text, buttons, etc.)
- [ ] SimpleUIIntegration or UIIntegration component added to Canvas
- [ ] Canvas Scaler configured for mobile (1080x1920, Match 0.5)
- [ ] UI elements positioned with proper anchors
- [ ] ShapeSpawner GameObject exists and is active (optional)
- [ ] ShapeSpawner has 3 spawn points assigned
- [ ] ShapeSpawner has shape prefabs assigned
- [ ] Auto Initialize is checked
- [ ] All shape prefabs have Shape + DragHandler components

### Runtime Verification:
- [ ] Console shows successful initialization messages
- [ ] Console shows "GridManager found and registered"
- [ ] GameManager appears in DontDestroyOnLoad during play
- [ ] UI scales properly on different screen sizes
- [ ] Score and high score display correctly
- [ ] Settings button responds to clicks (for SimpleGameUI)
- [ ] UI elements are positioned correctly and readable
- [ ] Touch targets are appropriately sized for mobile
- [ ] ShapeSpawner spawns 3 shapes on start (if enabled)
- [ ] Shapes can be dragged smoothly
- [ ] Shapes snap to grid on valid placement
- [ ] Invalid placements return to spawn
- [ ] New shapes spawn when all 3 are placed
- [ ] Line clearing works and updates score
- [ ] High score is saved and displayed
- [ ] No error messages in Console

### Component Verification:
- [ ] Each shape prefab has Collider2D for mouse detection
- [ ] Each shape prefab has SpriteRenderer for visuals
- [ ] Shape offsets are correctly configured  
- [ ] Grid sizes match between Shape and GridManager components

---

## 🎉 Success!

If all checks pass, your ColorBlast2 game is properly set up with the new modular architecture! 

The game will now have:
- ✅ Clean, maintainable code structure
- ✅ Proper dependency injection
- ✅ Modular systems that can be easily extended
- ✅ Automatic system initialization
- ✅ Robust error handling
- ✅ Performance optimizations

Happy coding! 🚀
