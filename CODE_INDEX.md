# ColorFillSample - Code Index

## Core Scripts

### Game Management
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `GameManager.cs` | 789 | Main game controller | `Awake()`, `Start()`, `LevelComplete()`, `LevelLose()`, `SpawnEnemyRoutine()` |
| `GameHandler.cs` | 85 | Persistent data manager | `AddDiamond()`, `LoseLife()`, `GainLife()`, `SaveGameData()`, `LoadGameData()` |

### Player System
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `Player.cs` | 438 | Player controller | `Init()`, `DetectInput()`, `DecideMovement()`, `MakeCube()`, `FillCube()` |

### Grid System
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `GridManager.cs` | 1142 | Grid management | `InitGrid()`, `ChangeValue()`, `PerformFloodFill()`, `WorldToGrid()`, `GridToWorld()` |
| `Cube.cs` | 261 | Individual cube behavior | `SetTrail()`, `FillCube()`, `ResetCube()`, `Illuminate()` |
| `CubeGrid.cs` | 75 | Grid visualization | Grid rendering and management |

### Enemy System
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `AEnemy.cs` | 48 | Base enemy class | `Start()`, `SlowDown()`, `NormalSpeed()` |
| `EnemyBehaviors.cs` | 230 | Enemy AI behavior | `Start()`, `FixedUpdate()`, `SpikedBallMovement()`, `BounceOffNormal()` |
| `EnemyCube.cs` | 89 | Enemy cube behavior | Enemy cube specific logic |
| `EnemyCubeGroup.cs` | 164 | Grouped enemies | Group management and coordination |
| `CubeEater.cs` | 154 | Cube eater enemy | Destructive enemy behavior |

### UI System
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `UIManager.cs` | 453 | UI controller | `Start()`, `LevelComplete()`, `FillAmount()`, `ResetLives()` |
| `GameWinScreen.cs` | 63 | Victory screen | Victory screen management |
| `GameLoseScreen.cs` | 185 | Defeat screen | Defeat screen and retry logic |
| `PausePanel.cs` | 74 | Pause menu | Pause functionality |
| `SettingsPanel.cs` | 105 | Settings menu | Settings management |
| `LevelSelection.cs` | 66 | Level selection | Level selection UI |
| `LevelButton.cs` | 36 | Level button | Individual level button behavior |
| `Menu.cs` | 38 | Main menu | Menu navigation |

### Audio System
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `AudioManager.cs` | 116 | Audio controller | `PlayBGMusic()`, `PlaySFXSound()`, `PlayUISound()` |

### Utility Scripts
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `SplashScreen.cs` | 91 | Splash screen | Initial loading screen |
| `PrivacyPolicy.cs` | 38 | Privacy policy | Privacy policy display |
| `InternetChecker.cs` | 55 | Internet connectivity | Network connectivity checking |
| `AdManager_Admob.cs` | 451 | Ad management | Google Mobile Ads integration |

### Specialized Components
| File | Lines | Purpose | Key Functions |
|------|-------|---------|---------------|
| `RotatingMine.cs` | 36 | Rotating mine enemy | Mine rotation and collision |
| `Rotatary.cs` | 20 | Rotary component | Rotation behavior |
| `Heart.cs` | 18 | Heart pickup | Life restoration pickup |
| `Diamond.cs` | 20 | Diamond pickup | Currency collection |
| `SlowDown.cs` | 25 | Slow down power-up | Enemy slowdown effect |
| `AddTime.cs` | 14 | Time extension | Timer extension pickup |
| `LineTravel.cs` | 37 | Line movement | Line-based movement |
| `InsideBoundary.cs` | 17 | Boundary detection | Boundary collision detection |
| `ConfettiScaler.cs` | 24 | Confetti scaling | Celebration effect scaling |
| `RandomTextSpawner.cs` | 52 | Text spawning | Random text display |
| `GridBackground.cs` | 66 | Grid background | Background grid rendering |
| `CameraIntro.cs` | 37 | Camera introduction | Camera animation |
| `TestCube.cs` | 54 | Test cube | Development testing |

## Data Structures

### Level Configuration
```csharp
public class LevelData
{
    public GameObject LevelObject;
    public int PlayerStartRow, PlayerStartCol;
    public bool isTimeless;
    public float levelTime;
    public List<Cube> gridPositions;
    public List<SpawnableConfig> SpwanablesConfigurations;
    public List<PreplacedEnemy> PreplacedEnemies;
    public int Columns, Rows;
    // Camera settings, color schemes, etc.
}
```

### Enemy Configuration
```csharp
public class SpawnableConfig
{
    public string sceneName;
    public SpawnablesType enemyType;
    public GameObject prefab;
    public float progressThreshold;
    public int spawnCount;
    public float moveSpeed;
    public int row, col;
    // Spawn timing and positioning
}
```

### Grid Cell
```csharp
public class CubeCell
{
    public int row, col;
    public CellType type; // Wall, Obstacle
}
```

## Enums

### Enemy Types
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

### Cell Types
```csharp
public enum CellType
{
    Wall,     // Boundary walls
    Obstacle  // Level obstacles
}
```

### Movement Directions
```csharp
public enum Direction
{
    None, North, South, East, West
}
```

## Key Functions Reference

### Game Management
- `GameManager.Instance` - Global game manager access
- `GameManager.LevelComplete(string msg)` - Handle level completion
- `GameManager.LevelLose()` - Handle level failure
- `GameManager.SpawnEnemyRoutine()` - Enemy spawning logic
- `GameManager.CameraShake()` - Camera shake effect

### Grid Operations
- `GridManager.InitGrid(int col, int row)` - Initialize grid
- `GridManager.ChangeValue(float x, float z)` - Mark grid cell as filled
- `GridManager.PerformFloodFill()` - Execute flood fill algorithm
- `GridManager.WorldToGrid(Vector3 worldPos)` - Convert world to grid coordinates
- `GridManager.GridToWorld(Vector2Int gridPos)` - Convert grid to world coordinates

### Player Control
- `Player.Init()` - Initialize player
- `Player.DetectInput()` - Handle input detection
- `Player.DecideMovement()` - Determine movement direction
- `Player.MakeCube()` - Create cube trail
- `Player.FillCube()` - Fill cube at position

### Enemy Behavior
- `EnemyBehaviors.SpikedBallMovement()` - Spike ball AI
- `EnemyBehaviors.BounceOffNormal(Vector3 normal)` - Bounce physics
- `AEnemy.SlowDown(float factor)` - Apply slowdown effect
- `AEnemy.NormalSpeed()` - Restore normal speed

### UI Management
- `UIManager.FillAmount(float amount)` - Update progress bar
- `UIManager.LevelComplete(string msg)` - Show completion screen
- `UIManager.ResetLives()` - Reset life display
- `UIManager.ShowClockAndTime()` - Display timer

### Audio Control
- `AudioManager.PlayBGMusic(int index)` - Play background music
- `AudioManager.PlaySFXSound(int index)` - Play sound effect
- `AudioManager.PlayUISound(int index)` - Play UI sound

## Editor Tools

### Level Editor
- `LevelDataEditorWindow` - Main level editor window
- `SpawnableCellPopup` - Enemy configuration popup
- Grid visualization and editing tools
- Level backup and restore functionality

### Debug Tools
- Debug mode toggle in GameManager
- Visual grid debugging
- Enemy behavior debugging
- Performance monitoring

## Integration Points

### Third-Party Integrations
- **DOTween**: Animation system integration
- **Odin Inspector**: Enhanced editor tools
- **Google Mobile Ads**: Monetization system
- **Cinemachine**: Camera system integration

### Unity Systems
- **Input System**: Touch and keyboard input
- **Physics System**: Collision detection and physics
- **Particle System**: Visual effects
- **Audio System**: Sound management
- **UI System**: User interface components

## Performance Considerations

### Optimization Techniques
- Object pooling for cubes and effects
- Efficient grid algorithms (O(n) flood fill)
- Optimized collision detection
- Smart enemy spawning based on progress
- Memory management for large grids

### Mobile Optimizations
- Touch input optimization
- Haptic feedback integration
- Mobile-optimized UI scaling
- Efficient rendering with URP
- Battery-friendly update cycles

## Error Handling

### Common Error Patterns
- Null reference checks for singleton instances
- Grid boundary validation
- Collision detection safety
- Memory cleanup in OnDestroy
- Exception handling in coroutines

### Debug Features
- Debug mode with additional logging
- Visual debugging tools
- Performance profiling hooks
- Error reporting system

This code index provides a comprehensive reference for navigating and understanding the ColorFillSample Unity project structure and implementation details.
