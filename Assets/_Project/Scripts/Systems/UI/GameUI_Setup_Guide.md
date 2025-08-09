# GameUI Setup Guide

## Overview
The `GameUI` component is a simple script that displays only the current score. It automatically sets up Screen Space - Camera mode for better 3D integration.

## Setup Instructions

### Step 1: Create UI Canvas
1. In your scene, create a new Canvas: `Right-click in Hierarchy > UI > Canvas`
2. Name it "GameUI"
3. Add the `GameUI` script to this Canvas GameObject

### Step 2: Create Score Text
1. **Right-click on Canvas > UI > Text - TextMeshPro**
2. **Name it "ScoreText"**
3. **Position it where you want (usually top of screen)**
4. **Configure the text:**
   - Text: "Score: 0"
   - Font Size: 36 (or whatever looks good)
   - Color: White (or any color you prefer)
5. **Drag this to the "Score Text" field in GameUI**

### Step 3: Assign Camera (Optional)
- **UI Camera**: Drag your main camera here (auto-assigns to Camera.main if left empty)

That's it! The script automatically handles the rest.

## How to Use in Code

### Update Score:
```csharp
// Get reference to GameUI
GameUI gameUI = FindObjectOfType<GameUI>();

// Update the score
gameUI.UpdateScore(newScore);

// Get current score
int currentScore = gameUI.GetCurrentScore();

// Reset score (for new game)
gameUI.ResetScore();
```

## What Was Removed

❌ **Removed unnecessary features:**
- Game area positioning (not needed for simple score display)
- Mobile-specific configurations (Unity handles this automatically)
- Background image creation (not essential)
- Complex canvas scaler setup (Unity defaults work fine)
- Game area center calculations (not needed for score-only UI)
- Multiple configuration options (keep it simple)

✅ **Kept only essentials:**
- Score text display
- Basic Screen Space - Camera setup
- Score update methods

## Troubleshooting

### Score Not Showing:
- **Problem**: Score text doesn't appear
- **Solution**: Make sure TextMeshPro is assigned to "Score Text" field
- **Check**: Verify the text element exists and is visible

### Canvas Issues:
- **Problem**: Canvas doesn't display correctly
- **Solution**: The script automatically sets up Screen Space - Camera mode
- **Manual Fix**: If needed, assign your main camera to "UI Camera" field

## Key Features

✅ **Super simple - only displays score**
✅ **Automatic Screen Space - Camera setup**
✅ **Automatic camera assignment**
✅ **Clean, minimal code**
✅ **Easy to use and understand**

The GameUI is now as simple as possible - just add it to a Canvas, assign a TextMeshPro for the score, and you're done!
