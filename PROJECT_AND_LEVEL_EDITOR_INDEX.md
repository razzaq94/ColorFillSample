# ColorFillSample - Project & Level Editor Index

## 📋 Project Overview

**ColorFillSample** is a Unity-based mobile puzzle game where players control a character that fills a grid by moving around, creating filled areas while avoiding enemies and obstacles. The game features a sophisticated level editor system for creating custom levels.

## 🏗️ Project Architecture

### Core Systems
```
Assets/
├── Scripts/           # Core game logic (2,500+ lines)
├── Editor/            # Level editor tools (1,803 lines)
├── Scenes/            # Game levels and menus
├── Prefabs/           # Reusable game objects
├── UI/                # User interface components
├── Resources/         # Runtime-loaded assets
└── BackUpLevel/       # Level templates and backups
```

### Key Components
- **GameManager**: Central game controller (789 lines)
- **GridManager**: Grid system and flood fill (1,142 lines)
- **Player**: Player movement and interaction (438 lines)
- **LevelEditorWindow**: Visual level editor (1,803 lines)
- **Enemy System**: 7 enemy types with AI behaviors
- **UI System**: Complete UI management (453 lines)

## 🎮 Level Editor System

### Accessing the Level Editor
```
Unity Editor → Window → Level Editor Window
```

### Level Editor Features

#### 1. **General Tab**
**Purpose**: Configure basic level settings and properties

**Settings Available:**
- **Level Object**: Root GameObject reference
- **Timeless Level**: Toggle for unlimited time levels
- **Level Time**: Timer duration (seconds)
- **Preplaced Prefabs**: Static enemy placements
- **Player Settings**: Movement speed configuration
- **Camera Settings**: Position and zoom controls
- **Color Settings**: Visual theme customization

**Color Configuration:**
```csharp
WallColor: {r: 0.67, g: 0.34, b: 0.34, a: 1}      // Boundary walls
PlayerColor: {r: 1, g: 1, b: 1, a: 1}             // Player character
CubeFillColor: {r: 0.84, g: 0.79, b: 0.28, a: 1}  // Filled grid cells
BackgroundColor: {r: 0.33, g: 0.32, b: 0.32, a: 1} // Background
EnemyCubeColor: {r: 1, g: 0.59, b: 0.80, a: 1}    // Enemy cubes
```

#### 2. **Grid Tab**
**Purpose**: Visual grid editing and enemy placement

**Grid Configuration:**
- **Columns**: 8-50 (even numbers only)
- **Rows**: 8-80 (even numbers only)
- **Cube Size**: 0-10 units
- **Side Padding**: 0-10 units

**Drawing Modes:**
- **Wall**: Boundary walls (blue)
- **Obstacle**: Level obstacles (red)
- **Empty**: Remove placed objects
- **Enemy**: Enemy placement mode

**Grid Operations:**
- **Fill All Cubes**: Create complete wall grid
- **Clear All**: Remove all placed objects
- **Refresh**: Rebuild grid visualization
- **Create Enemy Group**: Group selected enemies

### Enemy Placement System

#### Enemy Types Available
```csharp
public enum SpawnablesType
{
    SpikeBall,        // Bouncing spike balls
    FlyingHoop,       // Flying obstacles  
    MultiColoredBall, // Color-changing balls
    CubeEater,        // Destructive enemies
    SolidBall,        // Basic enemy balls
    Pickups,          // Power-ups and collectibles
    RotatingMine      // Rotating mine obstacles
}
```

#### Enemy Configuration (SpawnableCellPopup)
**Access**: Right-click grid cell → Spawnable → Enemy Type

**Configuration Options:**
- **Type**: Enemy type selection
- **Prefab**: Specific enemy variant
- **Progress (%)**: When to spawn (0-100%)
- **Move Speed**: Enemy movement speed
- **Count**: Number of enemies to spawn
- **Subsequent Progress**: Additional spawn triggers

**Default Settings by Type:**
```csharp
FlyingHoop:  yOffset = 2.4f, useFall = true
SpikeBall:   yOffset = 1.7f, useFall = true
CubeEater:   yOffset = 0.0f, useFall = false
Pickups:     yOffset = 1.4f, useFall = true
```

#### Pickup Types
- **Timer**: Extends level time
- **SlowDown**: Slows enemy movement
- **DIAMOND**: Currency collection
- **Heart**: Restores player lives

### Level Data Structure

#### LevelData Class
```csharp
[System.Serializable]
public class LevelData
{
    // Basic Settings
    public GameObject LevelObject;
    public int PlayerStartRow, PlayerStartCol;
    public bool isTimeless;
    public float levelTime;
    
    // Grid Configuration
    public int Columns = 50;
    public int Rows = 50;
    public float cubeSize = 1f;
    public float sidePaddingCubes = 1f;
    
    // Content
    public List<Cube> gridPositions;
    public List<SpawnableConfig> SpwanablesConfigurations;
    public List<PreplacedEnemy> PreplacedEnemies;
    public List<CubeCell> gridCellPositions;
    public List<EnemyCubeGroup> EnemyGroups;
    
    // Camera Settings
    public float cameraZPosition = 2.5f;
    public float cameraYPosition = 10f;
    public float zoomSize = 5f;
    public bool useAutoCameraPositioning = true;
}
```

#### SpawnableConfig Class
```csharp
[System.Serializable]
public class SpawnableConfig
{
    public string sceneName;
    public SpawnablesType enemyType;
    public GameObject prefab;
    
    [Range(0f, 1f)]
    public float progressThreshold;  // When to spawn (0-1)
    
    public int spawnCount = 1;
    public List<float> subsequentProgressThresholds;
    
    public float yOffset;
    public float initialSpawnHeight = 35f;
    public bool usePhysicsDrop;
    public float moveSpeed = 2f;
    public int row, col;
}
```

### Camera System

#### Auto Camera Positioning
The level editor automatically positions the camera based on grid size:

```csharp
private static readonly Dictionary<int, Vector3> ColumnToCamOrtho = new()
{
    { 8,  new Vector3(7f, 10f, -1.5f) },
    { 10, new Vector3(9.36f, 10f, -1.5f) },
    { 12, new Vector3(11.67f, 10f, -1f) },
    { 14, new Vector3(14.04f, 10f, -0.8f) },
    { 16, new Vector3(16.4f, 10f, -0.5f) },
    { 18, new Vector3(18.78f, 10f, 0f) },
    { 20, new Vector3(21.2f, 10f, .5f) },
    // ... continues for larger grids
};
```

#### Manual Camera Settings
- **Camera Z Position**: Distance from grid
- **Camera Y Position**: Height above grid
- **Zoom Size**: Orthographic camera size
- **Camera FOV**: Field of view (30-70 degrees)

## 🎯 Level Creation Workflow

### 1. **Setup New Level**
1. Open Unity Editor
2. Create new scene or duplicate existing level
3. Add GameManager GameObject
4. Open Level Editor Window
5. Create new LevelData if needed

### 2. **Configure Basic Settings**
1. **General Tab**:
   - Set level time or make timeless
   - Configure player start position
   - Set color scheme
   - Adjust camera settings

### 3. **Design Grid Layout**
1. **Grid Tab**:
   - Set grid dimensions (columns/rows)
   - Use drawing tools to place walls/obstacles
   - Set player start position (right-click → "Set Player Start")

### 4. **Add Enemies and Pickups**
1. **Right-click** on grid cells
2. **Select Spawnable** → Enemy type
3. **Configure** in popup:
   - Progress threshold (when to spawn)
   - Movement speed
   - Spawn count
   - Y offset position

### 5. **Test and Iterate**
1. **Save scene** (Ctrl+S)
2. **Play test** level
3. **Adjust** enemy placement and timing
4. **Fine-tune** difficulty

## 📁 Scene Structure

### Current Levels
```
Assets/Scenes/
├── _MainMenuScene.unity     # Main menu (Build Index 0)
├── Level_1.unity           # Tutorial level (Build Index 1)
├── Level_2.unity           # Basic gameplay (Build Index 2)
├── Level_3.unity           # Intermediate (Build Index 3)
├── Level_4.unity           # Advanced (Build Index 4)
├── Level_5.unity           # Expert (Build Index 5)
├── Level_6.unity           # Master (Build Index 6)
└── TestLevel.unity         # Development testing
```

### Level Templates
```
Assets/BackUpLevel/
└── Level_1 Template.unity  # Template for new levels
```

## 🔧 Editor Tools and Utilities

### Level Editor Window Features
- **Visual Grid Editor**: Click/drag to place objects
- **Real-time Preview**: See changes immediately
- **Context Menus**: Right-click for options
- **Auto-save Integration**: Changes saved to scene
- **Undo Support**: Full undo/redo functionality

### Debug Tools
- **Grid Visualization**: See grid boundaries
- **Enemy Path Preview**: Visualize enemy movement
- **Progress Tracking**: Monitor level completion
- **Performance Monitoring**: Track frame rates

### Backup and Export
- **Scene Backup**: Automatic scene saving
- **Level Templates**: Reusable level structures
- **JSON Export**: Level data serialization (commented out)

## 🎮 Game Integration

### Level Loading
```csharp
// Level selection and loading
SceneManager.LoadScene(buildIndex);

// Level data access
GameManager.Instance.Level

// Grid initialization
GridManager.Instance.InitGrid(columns, rows);
```

### Enemy Spawning
```csharp
// Automatic spawning based on progress
GameManager.Instance.SpawnEnemyRoutine(config, exposedCells);

// Progress-based triggers
yield return new WaitUntil(() => 
    GridManager.Instance._progress >= cfg.progressThreshold);
```

### Progress Tracking
```csharp
// Grid completion percentage
GridManager.Instance._progress

// Level completion check
if (progress >= requiredPercentage)
    GameManager.Instance.LevelComplete("Level Complete!");
```

## 📊 Level Statistics

### Current Level Count
- **Total Levels**: 6 main levels + 1 test level
- **Main Menu**: 1 scene
- **Build Indices**: 0-6 (7 total scenes)

### Level Complexity
- **Grid Sizes**: 8x8 to 50x50
- **Enemy Types**: 7 different enemy types
- **Pickup Types**: 4 different power-ups
- **Camera Configurations**: 15 preset camera positions

### Performance Metrics
- **Editor Performance**: Real-time grid updates
- **Runtime Performance**: Optimized enemy spawning
- **Memory Usage**: Efficient object pooling
- **Load Times**: Fast scene transitions

## 🚀 Best Practices

### Level Design Tips
1. **Start Simple**: Begin with basic layouts
2. **Test Frequently**: Play test during creation
3. **Balance Difficulty**: Gradual difficulty progression
4. **Use Templates**: Leverage existing level structures
5. **Optimize Performance**: Limit complex enemy patterns

### Editor Usage Tips
1. **Save Often**: Use Ctrl+S frequently
2. **Use Undo**: Leverage undo/redo for experimentation
3. **Grid Alignment**: Ensure proper grid positioning
4. **Camera Testing**: Test camera positioning early
5. **Enemy Timing**: Balance enemy spawn timing

### Code Organization
1. **Modular Design**: Separate concerns in scripts
2. **Clear Naming**: Use descriptive variable names
3. **Documentation**: Comment complex logic
4. **Error Handling**: Validate user inputs
5. **Performance**: Optimize for mobile devices

## 🔮 Future Enhancements

### Planned Features
- **Level Templates**: Pre-built level structures
- **Advanced Enemy AI**: More sophisticated behaviors
- **Visual Effects**: Enhanced particle systems
- **Sound Integration**: Dynamic audio system
- **Analytics**: Level completion tracking

### Editor Improvements
- **Bulk Operations**: Multi-select and edit
- **Level Validation**: Automatic error checking
- **Performance Profiling**: Built-in performance tools
- **Export Tools**: Level sharing capabilities
- **Visual Scripting**: Node-based level creation

## 📚 Additional Resources

### Documentation
- `PROJECT_ANALYSIS.md`: Comprehensive project overview
- `CODE_INDEX.md`: Detailed code reference
- `QUICK_REFERENCE.md`: Development quick reference

### Key Scripts
- `LevelEditorWindow.cs`: Main editor interface
- `GameManager.cs`: Core game logic
- `GridManager.cs`: Grid system management
- `Player.cs`: Player behavior and input

### Data Structures
- `LevelData`: Complete level configuration
- `SpawnableConfig`: Enemy spawn settings
- `CubeCell`: Grid cell information
- `PreplacedEnemy`: Static enemy placement

---

This index provides a comprehensive overview of the ColorFillSample project and its sophisticated level editor system, enabling efficient level creation and game development workflows.
