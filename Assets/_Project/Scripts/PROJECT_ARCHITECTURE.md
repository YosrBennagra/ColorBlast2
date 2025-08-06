# ColorBlast2 - Project Architecture

## 📁 File Structure & Organization

```
Assets/_Project/Scripts/
├── Architecture/           # Infrastructure & Framework
│   ├── ServiceLocator.cs  # Dependency injection system
│   └── Core/
│       └── Services.cs    # Static service access layer
├── Core/                  # Core game systems
│   ├── GameManager.cs     # Main game coordinator
│   └── Shape.cs          # Shape data model
└── Gameplay/             # Game mechanics
    ├── DragHandler.cs    # Mouse/touch dragging
    ├── GridManager.cs    # Grid state management
    ├── PlacementSystem.cs # Shape placement validation
    ├── LineClearSystem.cs # Line clearing logic
    └── ShapeDestructionSystem.cs # Shape splitting on line clear
```

---

## 🏗️ Architecture Patterns

### Service Locator Pattern
The project uses a **Service Locator** pattern for dependency injection:

- **ServiceLocator.cs**: MonoBehaviour-based container with Unity lifecycle management
- **Services.cs**: Static access layer with simple API (`Services.Get<T>()`, `Services.Register<T>()`)

### Modular System Design
Each system is independent and communicates through:
- **Service registration**: Systems register themselves for others to find
- **Event system**: Systems emit events for loose coupling
- **Interface-based design**: Systems depend on interfaces, not concrete implementations

---

## 🎯 System Responsibilities

### Core Systems

#### GameManager
- **Purpose**: System initialization and coordination
- **Responsibilities**:
  - Initialize all game systems on startup
  - Register systems with ServiceLocator
  - Handle scene transitions and persistence
  - Coordinate system lifecycles

#### Shape
- **Purpose**: Data model for draggable game pieces
- **Responsibilities**:
  - Store shape configuration (offsets, size)
  - Manage shape state (placed, dragged)
  - Handle shape-specific logic

### Gameplay Systems

#### DragHandler
- **Purpose**: Input handling for dragging mechanics
- **Responsibilities**:
  - Mouse/touch input processing
  - Drag start/end detection
  - Visual feedback during drag
  - Invalid placement handling

#### GridManager
- **Purpose**: Grid state and position management
- **Responsibilities**:
  - Track occupied/free grid positions
  - Provide grid coordinate conversions
  - Maintain grid state consistency
  - Cache grid lookups for performance

#### PlacementSystem
- **Purpose**: Shape placement validation and execution
- **Responsibilities**:
  - Validate placement attempts
  - Check bounds and collisions
  - Execute valid placements
  - Maintain placement history

#### LineClearSystem
- **Purpose**: Tetris-style line clearing logic
- **Responsibilities**:
  - Detect complete rows/columns
  - Clear completed lines
  - Trigger cascade clearing
  - Emit clearing events

#### ShapeDestructionSystem
- **Purpose**: Handle shape modification after line clears
- **Responsibilities**:
  - Split shapes when parts are cleared
  - Remove completely cleared shapes
  - Update remaining shape geometry
  - Manage destruction animations

---

## 🔗 System Dependencies

```
GameManager (entry point)
    ↓ initializes
┌─────────────────────────────────────┐
│ GridManager ← PlacementSystem       │
│      ↓             ↓                │
│ LineClearSystem → ShapeDestructionSystem │
└─────────────────────────────────────┘
    ↑ all accessed by
DragHandler (user input)
```

### Dependency Flow:
1. **GameManager** creates and registers all systems
2. **DragHandler** uses PlacementSystem for validation
3. **PlacementSystem** updates GridManager state
4. **LineClearSystem** monitors GridManager for complete lines
5. **ShapeDestructionSystem** responds to LineClearSystem events

---

## 🚀 Initialization Sequence

1. **GameManager.Awake()**: Singleton setup, DontDestroyOnLoad
2. **GameManager.Start()**: Begin system initialization
3. **Service Registration**: Each system registers with ServiceLocator
4. **System Ready**: All systems initialized and ready for use
5. **Game Loop**: Systems respond to events and user input

---

## 📢 Event System

### Current Events:
- **LineClearSystem.OnLinesCleared**: Triggered when lines are cleared
- **LineClearSystem.OnShapesDestroyed**: Triggered when shapes are destroyed

### Event Flow:
```
User Input → DragHandler → PlacementSystem → GridManager
                                 ↓
LineClearSystem (monitors) → ShapeDestructionSystem → Events
```

---

## 🔧 Service Access Patterns

### Registration (typically in GameManager):
```csharp
Services.Register<GridManager>(gridManager);
Services.Register<PlacementSystem>(placementSystem);
```

### Access (in any system):
```csharp
var gridManager = Services.Get<GridManager>();
var placementSystem = Services.Get<PlacementSystem>();
```

### Checking availability:
```csharp
if (Services.IsRegistered<GridManager>())
{
    // Safe to use
}
```

---

## 📦 Component Architecture

### MonoBehaviour Components:
- **GameManager**: Scene-persistent manager
- **Shape**: Attached to shape GameObjects
- **DragHandler**: Attached to draggable objects
- **GridManager**: Grid state management
- **PlacementSystem**: Placement logic
- **LineClearSystem**: Line clearing logic
- **ShapeDestructionSystem**: Shape modification

### Static Utilities:
- **Services**: Service access layer
- **ServiceLocator**: Dependency container

---

## 🎮 Game Loop Integration

### Play Mode Flow:
1. **Initialization**: GameManager sets up all systems
2. **Input**: DragHandler processes user input
3. **Validation**: PlacementSystem validates moves
4. **State Update**: GridManager updates grid state
5. **Line Detection**: LineClearSystem checks for completions
6. **Cleanup**: ShapeDestructionSystem handles cleared shapes
7. **Repeat**: Loop continues with next user input

### Performance Considerations:
- **Grid Caching**: GridManager caches expensive lookups
- **Event-Driven**: Systems only work when needed
- **Modular Loading**: Systems initialize independently
- **Memory Management**: Proper cleanup on scene transitions

---

## 🔄 Migration & Compatibility

### Legacy Support:
The architecture maintains compatibility with older code patterns while providing new, cleaner interfaces.

### Future Extensions:
- New gameplay systems can be added by implementing the service pattern
- Additional events can be added to existing systems
- UI systems can easily integrate using the same service access
- Save/load systems can be plugged in without changing existing code

---

## 🧪 Testing Strategy

### Unit Testing:
- Each system can be tested independently
- Service mocking for isolated testing
- Event testing for system communication

### Integration Testing:
- GameManager initialization testing
- Cross-system interaction validation
- Performance testing with service access patterns

This architecture provides a solid foundation that's both flexible for future development and maintainable for ongoing work!
