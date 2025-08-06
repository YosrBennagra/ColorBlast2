# Mobile UI Quick Setup Guide

## 🎯 **Quick Mobile UI Setup (5 minutes)**

### 1. Create Canvas:
```
Right-click Hierarchy → UI → Canvas
Rename: "Mobile UI Canvas"
```

### 2. Auto-Configure Components:
```
Add Component: GameUIManager (Systems.UI.GameUIManager)
Add Component: UIIntegration (Systems.UI.UIIntegration)
```

### 3. Create UI Panels:
```
Right-click Canvas → UI → Panel → Rename: "Game Panel"
Right-click Canvas → UI → Panel → Rename: "Game Over Panel" 
Right-click Canvas → UI → Panel → Rename: "Game Area"
```

### 4. Add UI Elements to Game Panel:
```
Right-click Game Panel → UI → Text - TextMeshPro → Rename: "Score Text"
Right-click Game Panel → UI → Text - TextMeshPro → Rename: "Lines Text"
Right-click Game Panel → UI → Button - TextMeshPro → Rename: "Pause Button"
```

### 5. Add UI Elements to Game Over Panel:
```
Right-click Game Over Panel → UI → Text - TextMeshPro → Rename: "Final Score Text"
Right-click Game Over Panel → UI → Text - TextMeshPro → Rename: "High Score Text"
Right-click Game Over Panel → UI → Button - TextMeshPro → Rename: "Play Again Button"
```

### 6. Assign References in GameUIManager:
```
Drag panels and UI elements to corresponding fields in GameUIManager component
```

## 📱 **Mobile Positioning Tips**

### Game Area Setup:
1. **Select Game Area Panel**
2. **Set Anchors**: Min (0.5, 0.5), Max (0.5, 0.5) 
3. **Set Pivot**: (0.5, 0.5)
4. **Position**: (0, 0, 0)
5. **Size**: Will auto-calculate based on screen

### UI Layout (Portrait Mode):
```
┌─────────────────┐
│   Score: 1234   │ ← Top UI
│   Lines: 5      │
├─────────────────┤
│                 │
│   GAME AREA     │ ← Centered game
│   (Grid here)   │
│                 │
├─────────────────┤
│  [Pause] [Menu] │ ← Bottom UI
└─────────────────┘
```

### Position Your Grid:
1. **Move GridManager GameObject** to center of Game Area
2. **Use GameUIManager.GetGameAreaCenter()** for automatic positioning
3. **Grid will be perfectly centered** on all mobile devices

## 🎮 **Game Flow**

### Normal Play:
- Game Panel visible
- Score, Lines, Level update automatically
- Pause button available

### Game Over:
- Game Over Panel appears
- Shows final score and high score
- Play Again button restarts everything

### Features:
- ✅ **Auto-scaling** for all screen sizes
- ✅ **High score persistence** 
- ✅ **Touch-friendly buttons**
- ✅ **Pause/Resume functionality**
- ✅ **Score tracking with levels**

## 🔗 **Integration**

The UI automatically connects to:
- **LineClearSystem**: Updates score when lines clear
- **ShapeSpawner**: Resets on Play Again
- **GridManager**: Clears grid on restart
- **Game Systems**: Pause/resume functionality

**You're ready for mobile! 🚀**
