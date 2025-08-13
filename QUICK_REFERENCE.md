# ColorFillSample - Quick Reference Guide

## Common Development Tasks

### Adding a New Enemy Type

1. **Create the enemy prefab** in `Assets/Prefabs/Spawnables/`
2. **Add to SpawnablesType enum** in `EnemyBehaviors.cs`:
   ```csharp
   public enum SpawnablesType
   {
       // ... existing types
       YourNewEnemy
   }
   ```
3. **Create enemy behavior script** extending `AEnemy`
4. **Update LevelEditorWindow.cs** to include the new type in the spawn menu
5. **Add to enemy variant groups** in GameManager if needed

### Creating a New Level

1. **Duplicate existing level scene** in `Assets/Scenes/`
2. **Set up GameManager** with new LevelData configuration
3. **Configure grid dimensions** (Columns/Rows)
4. **Set player start position** (PlayerStartRow/PlayerStartCol)
5. **Add enemy spawns** using SpawnableConfig
6. **Configure camera settings** for the grid size
7. **Add to build settings** with correct build index

### Modifying Grid Size

1. **Update LevelData** in GameManager:
   ```csharp
   Level.Columns = newColumnCount;
   Level.Rows = newRowCount;
   ```
2. **Adjust camera settings** in LevelData:
   ```csharp
   Level.cameraZPosition = newZPosition;
   Level.zoomSize = newZoomSize;
   ```
3. **Update camera positioning** in GameManager's ColumnToCamOrtho dictionary if needed

### Adding a New Power-up

1. **Create power-up prefab** in `Assets/Prefabs/Spawnables/`
2. **Add behavior script** for the power-up
3. **Update pickup variants** in LevelEditorWindow.cs:
   ```csharp
   _enemyTypeToPrefabsMap[SpawnablesType.Pickups] = new List<GameObject>
   {
       // ... existing pickups
       AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/YourPowerup.prefab"),
   };
   ```

## Troubleshooting

### Common Issues

#### GameManager Instance is Null
```csharp
// Check if GameManager exists in scene
if (GameManager.Instance == null)
{
    Debug.LogError("GameManager not found in scene!");
    return;
}
```

#### Grid Not Initializing
```csharp
// Ensure GridManager is initialized before use
if (GridManager.Instance == null)
{
    Debug.LogError("GridManager not found!");
    return;
}
GridManager.Instance.InitGrid(columns, rows);
```

#### Enemy Not Spawning
1. **Check SpawnableConfig**:
   - Verify `progressThreshold` is between 0-1
   - Ensure `spawnCount` > 0
   - Check `prefab` is assigned
2. **Verify scene name** matches active scene
3. **Check enemy variant groups** in GameManager

#### Player Movement Issues
```csharp
// Check if player is enabled and game is running
if (!Player.Instance.IsMoving || !GameManager.Instance._gameRunning)
{
    return;
}
```

#### UI Not Updating
```csharp
// Ensure UIManager exists
if (UIManager.Instance == null)
{
    Debug.LogError("UIManager not found!");
    return;
}
```

### Debug Commands

#### Enable Debug Mode
```csharp
GameManager.Instance.Debug = true;
```

#### Force Level Complete
```csharp
GameManager.Instance.LevelComplete("Debug Complete");
```

#### Reset Player Lives
```csharp
GameHandler.Instance.ResetLives();
```

#### Clear All Enemies
```csharp
foreach (var enemy in GameManager.Instance.levelEnemies.ToList())
{
    if (enemy != null)
        Destroy(enemy.gameObject);
}
GameManager.Instance.levelEnemies.Clear();
```

## Performance Optimization

### Grid Optimization
```csharp
// Use efficient flood fill
GridManager.Instance.PerformFloodFill();

// Check grid bounds before operations
if (index.x < 0 || index.x >= _gridColumns || index.y < 0 || index.y >= _gridRows)
    return;
```

### Enemy Spawning Optimization
```csharp
// Use object pooling for frequently spawned objects
// Limit concurrent enemies
if (GameManager.Instance.levelEnemies.Count > maxEnemies)
    return;
```

### Memory Management
```csharp
// Clean up destroyed objects
private void OnDestroy()
{
    GameManager.Instance.levelEnemies.Remove(this);
}
```

## Level Editor Tips

### Opening Level Editor
```
Window → Level Editor Window
```

### Adding Enemies
1. **Right-click** on grid cell
2. **Select Spawnable** → Enemy type
3. **Configure** in popup window:
   - Progress threshold (0-100%)
   - Move speed
   - Spawn count
   - Y offset

### Setting Player Start
1. **Right-click** on desired cell
2. **Select "Set Player Start"**

### Adding Walls/Obstacles
1. **Use toolbar** to select draw mode
2. **Click/drag** to place walls/obstacles

### Camera Settings
```csharp
// Auto camera positioning based on grid size
Level.useAutoCameraPositioning = true;

// Manual camera settings
Level.cameraZPosition = -1.5f;
Level.cameraYPosition = 10f;
Level.zoomSize = 9.36f;
```

## Build Configuration

### Scene Order
1. `_MainMenuScene` (Build Index 0)
2. `Level_1` (Build Index 1)
3. `Level_2` (Build Index 2)
4. ... etc.

### Platform Settings
- **Target Platform**: Android/iOS
- **Graphics API**: OpenGL ES 3.0
- **Scripting Backend**: IL2CPP
- **Architecture**: ARM64

### Quality Settings
- **Anti Aliasing**: 2x Multi Sampling
- **Texture Quality**: Full Res
- **Anisotropic Textures**: Per Texture
- **Soft Particles**: Enabled

## Testing Checklist

### Before Build
- [ ] All levels load correctly
- [ ] Enemy spawning works on all levels
- [ ] UI elements scale properly on different screen sizes
- [ ] Audio plays correctly
- [ ] Ads integrate properly
- [ ] Save/load functionality works
- [ ] Performance is acceptable on target devices

### Level Testing
- [ ] Player can move and fill grid
- [ ] Enemies spawn at correct progress thresholds
- [ ] Level completion triggers correctly
- [ ] Timer works (if applicable)
- [ ] Lives system functions
- [ ] Power-ups work as expected

### UI Testing
- [ ] All buttons respond to touch
- [ ] Progress bar updates correctly
- [ ] Level selection works
- [ ] Settings save properly
- [ ] Pause/resume functionality
- [ ] Win/lose screens display correctly

## Version Control

### Files to Ignore
```
# Unity generated
Library/
Temp/
Logs/
UserSettings/
.vs/
.utmp/

# Build outputs
Builds/
*.apk
*.aab
*.ipa

# OS generated
.DS_Store
Thumbs.db
```

### Important Files to Track
- All `.cs` scripts
- All `.unity` scenes
- All `.prefab` files
- `Packages/manifest.json`
- `ProjectSettings/`
- Custom editor tools

## Useful Scripts

### Quick Level Reset
```csharp
public void ResetLevel()
{
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

### Force Game Over
```csharp
public void ForceGameOver()
{
    GameManager.Instance.LevelLose();
}
```

### Skip to Next Level
```csharp
public void SkipToNextLevel()
{
    GameManager.Instance.LevelComplete("Skip");
}
```

### Toggle Slow Motion
```csharp
public void ToggleSlowMotion()
{
    Time.timeScale = Time.timeScale == 1f ? 0.5f : 1f;
}
```

This quick reference guide provides essential information for common development tasks and troubleshooting in the ColorFillSample project.
