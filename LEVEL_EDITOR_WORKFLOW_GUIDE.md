# Level Editor Workflow Guide

## 🎯 Quick Start Guide

### Prerequisites
- Unity Editor (2022.3 LTS or newer)
- ColorFillSample project opened
- Basic understanding of Unity interface

### Accessing the Level Editor
1. **Open Unity Editor**
2. **Navigate to**: `Window → Level Editor Window`
3. **Window opens**: Level Editor interface appears

---

## 📋 Step-by-Step Level Creation

### Step 1: Scene Setup

#### 1.1 Create New Scene
```
File → New Scene → 2D
```
**OR**
```
Duplicate existing level scene:
- Right-click Level_1.unity in Project window
- Select "Duplicate"
- Rename to "Level_New"
```

#### 1.2 Add GameManager
```
1. Create Empty GameObject
2. Rename to "GameManager"
3. Add GameManager component
4. Configure basic settings in Inspector
```

#### 1.3 Open Level Editor
```
Window → Level Editor Window
```

### Step 2: Basic Configuration

#### 2.1 General Tab Settings
```
Level Object: Assign GameManager GameObject
Timeless Level: ✓ (for unlimited time) OR ☐ (for timed)
Level Time: 60 (if not timeless)
```

#### 2.2 Player Configuration
```
Player Start Position: Set in Grid Tab
Player Move Speed: 5.0 (default)
```

#### 2.3 Camera Settings
```
Use Auto Camera Positioning: ✓ (recommended)
OR Manual settings:
- Camera Z Position: -1.5
- Camera Y Position: 10
- Zoom Size: 9.36
```

#### 2.4 Color Scheme
```
Wall Color: {r: 0.67, g: 0.34, b: 0.34, a: 1}
Player Color: {r: 1, g: 1, b: 1, a: 1}
Cube Fill Color: {r: 0.84, g: 0.79, b: 0.28, a: 1}
Background Color: {r: 0.33, g: 0.32, b: 0.32, a: 1}
Enemy Cube Color: {r: 1, g: 0.59, b: 0.80, a: 1}
```

### Step 3: Grid Design

#### 3.1 Grid Tab Configuration
```
Columns: 10 (even number, 8-50)
Rows: 20 (even number, 8-80)
Cube Size: 1.0
Side Padding: 1.0
```

#### 3.2 Drawing Tools
**Select Drawing Mode:**
- **Wall**: Boundary walls (blue)
- **Obstacle**: Level obstacles (red)
- **Empty**: Remove objects
- **Enemy**: Enemy placement

**Place Objects:**
- **Click**: Place single object
- **Drag**: Place multiple objects
- **Right-click**: Context menu options

#### 3.3 Set Player Start
```
1. Right-click desired grid cell
2. Select "Set Player Start"
3. Verify position in General Tab
```

### Step 4: Enemy Placement

#### 4.1 Basic Enemy Placement
```
1. Right-click grid cell
2. Navigate: Spawnable → Enemy Type
3. Configure in popup window:
   - Progress Threshold: 25% (when to spawn)
   - Move Speed: 2.0
   - Count: 1
   - Y Offset: 1.4
```

#### 4.2 Enemy Types and Settings

**SpikeBall (Bouncing)**
```
Progress: 15-30%
Speed: 2.0-3.0
Y Offset: 1.7
Use Physics Drop: ✓
```

**FlyingHoop (Flying)**
```
Progress: 20-40%
Speed: 1.5-2.5
Y Offset: 2.4
Use Physics Drop: ✓
```

**CubeEater (Destructive)**
```
Progress: 35-60%
Speed: 1.0-2.0
Y Offset: 0.0
Use Physics Drop: ☐
```

**RotatingMine (Static)**
```
Progress: 25-45%
Speed: 0.0
Y Offset: 0.0
Use Physics Drop: ☐
```

#### 4.3 Pickup Placement
```
1. Right-click filled cell
2. Navigate: Spawnable → Pickups → Type
3. Types available:
   - Timer: Extends level time
   - SlowDown: Slows enemies
   - DIAMOND: Currency
   - Heart: Restores lives
```

### Step 5: Advanced Configuration

#### 5.1 Multiple Enemy Spawns
```
1. Set Count > 1 in enemy config
2. Configure Subsequent Progress:
   - Spawn #1: 25%
   - Spawn #2: 45%
   - Spawn #3: 65%
```

#### 5.2 Enemy Groups
```
1. Place multiple enemies in adjacent cells
2. Select "Create Enemy Group" button
3. Configure group behavior
```

#### 5.3 Preplaced Enemies
```
1. General Tab → Preplaced Prefabs
2. Add new entry
3. Assign prefab and position
```

### Step 6: Testing and Iteration

#### 6.1 Save Scene
```
Ctrl+S or File → Save
```

#### 6.2 Play Test
```
1. Press Play button
2. Test player movement
3. Verify enemy spawning
4. Check level completion
```

#### 6.3 Adjustments
```
1. Stop play mode
2. Modify settings in Level Editor
3. Save changes
4. Re-test
```

---

## 🎮 Level Design Patterns

### Beginner Level (Level 1-2)
```
Grid Size: 10x20
Enemy Count: 2-3
Progress Thresholds: 25%, 50%, 75%
Enemy Types: SpikeBall, SolidBall
Pickups: 2-3 Diamonds
Time Limit: 60 seconds
```

### Intermediate Level (Level 3-4)
```
Grid Size: 12x24
Enemy Count: 4-6
Progress Thresholds: 20%, 35%, 50%, 65%
Enemy Types: FlyingHoop, CubeEater
Pickups: Timer, SlowDown
Time Limit: 90 seconds
```

### Advanced Level (Level 5-6)
```
Grid Size: 14x28
Enemy Count: 6-8
Progress Thresholds: 15%, 30%, 45%, 60%, 75%
Enemy Types: All types
Pickups: All types
Time Limit: 120 seconds
```

---

## 🔧 Troubleshooting

### Common Issues

#### Level Editor Not Opening
```
Problem: "No GameManager in scene"
Solution: 
1. Add GameManager GameObject
2. Add GameManager component
3. Refresh Level Editor
```

#### Grid Not Displaying
```
Problem: Empty grid view
Solution:
1. Check grid dimensions (8-50)
2. Ensure even numbers
3. Click "Refresh" button
4. Verify LevelData exists
```

#### Enemies Not Spawning
```
Problem: Enemies don't appear during gameplay
Solution:
1. Check progress thresholds (0-100%)
2. Verify prefab assignments
3. Ensure scene name matches
4. Test with lower thresholds
```

#### Camera Issues
```
Problem: Camera positioning incorrect
Solution:
1. Enable "Use Auto Camera Positioning"
2. Check grid size compatibility
3. Adjust manual camera settings
4. Test different zoom levels
```

#### Performance Problems
```
Problem: Slow editor or gameplay
Solution:
1. Reduce grid size
2. Limit enemy count
3. Simplify enemy patterns
4. Use object pooling
```

---

## 📊 Level Validation Checklist

### Before Publishing
- [ ] **Grid Configuration**
  - [ ] Grid size is appropriate (8-50 columns/rows)
  - [ ] Player start position is set
  - [ ] Boundary walls are placed
  - [ ] Grid is playable (no impossible paths)

- [ ] **Enemy Configuration**
  - [ ] All enemies have valid prefabs
  - [ ] Progress thresholds are logical (ascending)
  - [ ] Spawn counts are reasonable
  - [ ] Movement speeds are balanced

- [ ] **Gameplay Testing**
  - [ ] Level is completable
  - [ ] Difficulty progression is smooth
  - [ ] Pickups are accessible
  - [ ] Time limit is appropriate

- [ ] **Technical Validation**
  - [ ] Scene saves without errors
  - [ ] Build includes scene
  - [ ] Performance is acceptable
  - [ ] No console errors

---

## 🚀 Advanced Techniques

### Dynamic Difficulty
```
Use progress-based enemy spawning:
- Early enemies: 15-25% (basic types)
- Mid enemies: 35-55% (intermediate types)
- Late enemies: 65-85% (advanced types)
```

### Strategic Pickup Placement
```
Place pickups on filled cells:
- Timer: When time is running low
- SlowDown: Before difficult enemy waves
- Heart: After challenging sections
- Diamond: Reward for exploration
```

### Camera Optimization
```
For different grid sizes:
- 8-12 columns: Auto positioning
- 14-20 columns: Manual adjustment
- 22+ columns: Custom camera setup
```

### Enemy Pattern Design
```
Create interesting patterns:
- Wave attacks (multiple spawns)
- Escalating difficulty
- Strategic positioning
- Timing-based challenges
```

---

## 📚 Reference Materials

### Quick Commands
```
Refresh Grid: Click "Refresh" button
Save Scene: Ctrl+S
Undo: Ctrl+Z
Redo: Ctrl+Y
Play Test: Ctrl+P
```

### Default Values
```
Player Speed: 5.0
Enemy Speed: 2.0
Spawn Height: 35.0
Y Offset: 1.4
Progress Threshold: 50%
```

### File Locations
```
Level Editor: Assets/Editor/LevelEditorWindow.cs
Enemy Prefabs: Assets/Prefabs/Spawnables/
Level Scenes: Assets/Scenes/
Templates: Assets/BackUpLevel/
```

---

This workflow guide provides comprehensive instructions for creating levels using the ColorFillSample level editor, from basic setup to advanced techniques and troubleshooting.
