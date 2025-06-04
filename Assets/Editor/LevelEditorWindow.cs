// Assets/Editor/LevelDataEditorWindow.cs
// ─────────────────────────────────────────────────────────────
// A user-friendly, tabbed Level Editor window for editing
// GameManager.Level (LevelData) offline (Edit Mode only).
// ─────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class LevelDataEditorWindow : EditorWindow
{
    private GameManager _gameManager;
    private LevelData _level;           // Shortcut to _gameManager.Level

    private Vector2 _scrollSpawnables = Vector2.zero;
    private List<bool> _spawnableFoldouts = new List<bool>();
    // Tab bar
    private int _selectedTab = 0;
    private string[] _tabs = new string[] { "General", "Grid", "Spawnables", "Preplaced", "Advanced" };
    private Dictionary<SpawnablesType, List<GameObject>> _enemyTypeToPrefabsMap = new Dictionary<SpawnablesType, List<GameObject>>();

    [MenuItem("Window/Level Data Editor (Friendly)")]
    public static void OpenWindow()
    {
        var wnd = GetWindow<LevelDataEditorWindow>();
        wnd.titleContent = new GUIContent("Level Editor");
        wnd.Show();
    }

    private void OnEnable()
    {
        // Find the single GameManager in the scene
        _gameManager = FindFirstObjectByType<GameManager>();
        if (_gameManager != null)
            _level = _gameManager.Level;
        _enemyTypeToPrefabsMap = new Dictionary<SpawnablesType, List<GameObject>>();

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Prefabs/Spawnables" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                // Use ignoreCase = true
                if (System.Enum.TryParse<SpawnablesType>(prefab.name, true, out var enemyType))
                {
                    if (!_enemyTypeToPrefabsMap.ContainsKey(enemyType))
                        _enemyTypeToPrefabsMap[enemyType] = new List<GameObject>();
                    _enemyTypeToPrefabsMap[enemyType].Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"Prefab name '{prefab.name}' does not match any SpawnablesType enum.");
                }
            }
        }

        _enemyTypeToPrefabsMap[SpawnablesType.Pickups] = new List<GameObject>
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/DIAMOND.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/SlowDown.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/Timer.prefab"),
        };



    }

    private void OnInspectorUpdate()
    {
        // In case GameManager.Level changes externally, keep our _level reference updated
        if (_gameManager != null)
            _level = _gameManager.Level;
    }

    private void OnGUI()
    {
        // If no GameManager, show warning
        if (_gameManager == null)
        {
            EditorGUILayout.HelpBox("There is no GameManager in the scene.\nPlease add a GameManager GameObject with a public LevelData Level field.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
                _gameManager = FindFirstObjectByType<GameManager>();
            return;
        }

        // If GameManager.Level is null, prompt the user to assign it
        if (_gameManager.Level == null)
        {
            EditorGUILayout.HelpBox("GameManager.Level is currently null.\nAssign or create a LevelData instance in GameManager's Inspector first.", MessageType.Warning);
            if (GUILayout.Button("Create & Assign New LevelData"))
            {
                // Create a new LevelData instance embedded in the scene:
                _gameManager.Level = new LevelData
                {
                    LevelObject = null,
                    PlayerPos = null,
                    levelTime = 0f,
                    gridPositions = new List<Cube>(),
                    SpwanablesConfigurations = new List<SpawnableConfig>(),
                    PreplacedPrefabs = new List<GameObject>(),
                    PreplacedSpawnPoints = new List<Transform>(),
                    Columns = 10,
                    Rows = 20
                };
                _level = _gameManager.Level;
                EditorUtility.SetDirty(_gameManager);
                EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
            }
            return;
        }

        // At this point, we have a valid _level to edit
        _level = _gameManager.Level;

        // Draw the tab bar
        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);

        GUILayout.Space(8);

        // Depending on selected tab, draw the corresponding UI
        switch (_selectedTab)
        {
            case 0: DrawGeneralTab(); break;
                // Check for mouse wheel scroll input (while hovering over the grid area)
                Event e = Event.current;
                if (e.type == EventType.ScrollWheel)
                {
                    zoomFactor -= e.delta.y * 0.1f;  // Scroll up = zoom in, down = zoom out
                    zoomFactor = Mathf.Clamp(zoomFactor, 0.5f, 3f);  // Clamp zoom between 50% and 300%
                    e.Use();  // Mark event as used
                }

                DrawGridTab();  // Draw the zoomable grid here
            case 1: DrawGridTab(); break;
            case 2: DrawSpawnablesTab(); break;
            case 3: DrawPreplacedTab(); break;
            case 4: DrawAdvancedTab(); break;
        }

        // At the very bottom, a Save prompt
        GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox("After making changes above, press Ctrl+S (or File→Save) to commit them to the scene.", MessageType.Info);
    }

    // ─────────────────────────────────────────────────────────
    // Tab 0: General (LevelObject, PlayerPos, levelTime)
    // ─────────────────────────────────────────────────────────
    private void DrawGeneralTab()
    {
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            // 1) LevelObject (a parent GameObject for all spawned cubes & items)
            _level.LevelObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Level Root Object"),
                _level.LevelObject,
                typeof(GameObject),
                true
            );

            // 2) PlayerPos (where the player starts)
            _level.PlayerPos = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Player Start Transform"),
                _level.PlayerPos,
                typeof(Transform),
                true
            );

            // 3) levelTime (float duration)
            _level.levelTime = EditorGUILayout.FloatField(
                new GUIContent("Level Time (seconds)"),
                _level.levelTime
            );
        }
        EditorGUILayout.EndVertical();

        // Mark scene dirty if any of these changed
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_gameManager);
            EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Tab 1: Grid (Columns, Rows, simple grid info)
    // ─────────────────────────────────────────────────────────
    private float zoomFactor = 1f;  // Default zoom factor

    private void DrawGridTab()
    {
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        // Add the "Fill All" button to fill all cells with cubes
        if (GUILayout.Button("Fill All Cubes"))
        {
            FillAllCubes();
        }

        // Add the "Clear All" button to clear all cubes (remove obstacles)
        if (GUILayout.Button("Clear All"))
        {
            ClearAllCubes();
        }

        GUILayout.Space(10);  // Space between buttons and the grid
        // Zoom slider
        zoomFactor = EditorGUILayout.Slider("Zoom", zoomFactor, 0.5f, 3f);  // Zoom from 50% to 300%
        GUILayout.Space(6);

        // Create the grid interactable area
        for (int row = 0; row < _level.Rows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < _level.Columns; col++)
            {
                string cellKey = $"{row},{col}";  // Unique key for each cell in the grid

                // Handle cell clicks and mouse interaction
                bool isOccupied = _level.gridCellPositions.Exists(c => c.row == row && c.col == col);

                // Calculate cell size based on zoom factor
                float cellSize = 30 * zoomFactor;  // Default size is 30, scale by zoomFactor

                // Handle cell color if occupied
                GUI.backgroundColor = isOccupied ? Color.green : Color.white;
                if (GUILayout.Button(" ", GUILayout.Width(30 * zoomFactor), GUILayout.Height(30 * zoomFactor)))
                {
                    // Handle the click and place/remove the obstacle
                    HandleCellClick(row, col);
                }
                GUI.backgroundColor = Color.white;  // Reset background color
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }


    // Handle mouse clicks on grid cells
    private void HandleCellClick(int row, int col)
    {

        // Add offset based on your grid origin (lower-left corner at (25, -24))
        float offsetX = -25f;  // Your grid's offset X (adjust based on your needs)
        float offsetZ = -24f;  // Your grid's offset Z (adjust based on your needs)

        // Fix row orientation (flip it)
        int flippedRow = _level.Rows - row - 1;  // Reverse the row index to flip the grid

        // Now calculate the spawn position with the offset
        Vector3 spawnPosition = new Vector3(col + offsetX, 1f, flippedRow + offsetZ);
        // Check if the cell is already occupied
        bool isOccupied = _level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.isObstacle);

        if (isOccupied)
        {
            // If occupied, remove the spawnable (or empty the cell)
            _level.gridCellPositions.RemoveAll(c => c.row == row && c.col == col);

            DestroyObstacleAt(row, col);
        }
        else
        {
            // If empty, add a spawnable (or mark it with something, like a spawn point)
            _level.gridCellPositions.Add(new CubeCell { row = row, col = col, isObstacle = true });
            // Instantiate the obstacle prefab at the correct position
            if (_level.obstaclePrefab == null)
            {
                Debug.LogError("Obstacle prefab is not assigned!");
                return;
            }
            Instantiate(_level.obstaclePrefab, spawnPosition, Quaternion.identity);
        }

        // Mark scene dirty so it saves changes
        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }


    private void DestroyObstacleAt(int row, int col)
    {
        // Apply the same offsets used for placement
        float offsetX = -25f;  // Your grid's offset X
        float offsetZ = -24f; // Your grid's offset Z

        // Calculate the position based on row, col, and offsets
        Vector3 obstaclePosition = new Vector3(col + offsetX, 1f, (_level.Rows - row - 1) + offsetZ);  // Use flipped row

        // Find obstacles by position
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");  // Assuming obstacles are tagged as "Obstacle"

        foreach (var obstacle in obstacles)
        {
            // Check if the position matches (adjust the comparison if needed)
            if (Vector3.Distance(obstacle.transform.position, obstaclePosition) < 0.1f)  // Small tolerance
            {
                DestroyImmediate(obstacle);  // Use DestroyImmediate for immediate effect in Edit mode
                Debug.Log($"Destroyed obstacle at: {obstaclePosition}");
                return;
            }
        }
        Debug.LogWarning($"No obstacle found at position: {obstaclePosition}");
    }
    private void FillAllCubes()
    {
        // Define the offsets (use your existing grid offsets)
        float offsetX = -25f;  // Your grid's offset X
        float offsetZ = -24f; // Your grid's offset Z

        // Loop through each cell in the grid
        for (int row = 0; row < _level.Rows; row++)
        {
            for (int col = 0; col < _level.Columns; col++)
            {
                // Check if this cell is not already filled with a cube (obstacle)
                if (!_level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.isObstacle))
                {
                    // Add a new obstacle (cube) to this cell
                    _level.gridCellPositions.Add(new CubeCell { row = row, col = col, isObstacle = true });

                    // Calculate spawn position with offsets
                    Vector3 spawnPosition = new Vector3(col + offsetX, 1f, (_level.Rows - row - 1) + offsetZ);  // Adjust position with offsets and flipped row
                    Instantiate(_level.obstaclePrefab, spawnPosition, Quaternion.identity);  // Spawn obstacle in scene
                }
            }
        }

        // Mark scene dirty to save the changes
        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }


    private void ClearAllCubes()
    {
        // Clear the grid positions by removing all obstacles
        _level.gridCellPositions.RemoveAll(c => c.isObstacle);

        // Optionally, destroy all existing obstacles in the scene
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");  // Assuming obstacles are tagged as "Obstacle"
        foreach (var obstacle in obstacles)
        {
            DestroyImmediate(obstacle);  // Use DestroyImmediate in Edit mode to immediately remove the obstacle
        }

        // Mark scene dirty to save the changes
        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }



    private void DrawSpawnablesTab()
    {
        EditorGUILayout.LabelField("Spawnable Configurations", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (_level.SpwanablesConfigurations == null)
            _level.SpwanablesConfigurations = new List<SpawnableConfig>();

        // Ensure foldouts list matches spawnables list
        while (_spawnableFoldouts.Count < _level.SpwanablesConfigurations.Count)
            _spawnableFoldouts.Add(true);
        while (_spawnableFoldouts.Count > _level.SpwanablesConfigurations.Count)
            _spawnableFoldouts.RemoveAt(_spawnableFoldouts.Count - 1);

        _scrollSpawnables = EditorGUILayout.BeginScrollView(_scrollSpawnables, GUILayout.Height(800));
        int? removeIndex = null;

        for (int i = 0; i < _level.SpwanablesConfigurations.Count; i++)
        {
            SpawnableConfig cfg = _level.SpwanablesConfigurations[i];
            if (cfg == null) continue;

            // Ensure prefab assignment from dictionary (if any)
            if (_enemyTypeToPrefabsMap.TryGetValue(cfg.enemyType, out var prefabList) && prefabList.Count > 0)
            {
                if (cfg.prefab == null || !prefabList.Contains(cfg.prefab))
                    cfg.prefab = prefabList[0];

                string[] prefabNames = prefabList.ConvertAll(p => p != null ? p.name : "Null").ToArray();

                int selectedIndex = prefabList.IndexOf(cfg.prefab);
                if (selectedIndex < 0) selectedIndex = 0;

                int newIndex = EditorGUILayout.Popup("Prefab Variant", selectedIndex, prefabNames);
                if (newIndex != selectedIndex)
                {
                    cfg.prefab = prefabList[newIndex];
                    EditorUtility.SetDirty(_gameManager);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
                }
            }
            else
            {
                cfg.prefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Prefab"),
                    cfg.prefab,
                    typeof(GameObject),
                    false
                );
            }

            _spawnableFoldouts[i] = EditorGUILayout.Foldout(_spawnableFoldouts[i], $"Spawnable #{i + 1}: {cfg.enemyType}", true);
            if (_spawnableFoldouts[i])
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUI.indentLevel++;

                cfg.enemyType = (SpawnablesType)EditorGUILayout.EnumPopup(new GUIContent("Enemy Type"), cfg.enemyType);

                // Show prefab field again for manual override if needed
                cfg.prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab"), cfg.prefab, typeof(GameObject), false);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Spawn Trigger Settings", EditorStyles.boldLabel);
                cfg.useTimeBasedSpawn = EditorGUILayout.ToggleLeft("Use Time-Based Spawn", cfg.useTimeBasedSpawn);

                if (cfg.useTimeBasedSpawn)
                    cfg.initialDelay = EditorGUILayout.FloatField(new GUIContent("Initial Delay (sec)"), cfg.initialDelay);
                else
                    cfg.progressThreshold = EditorGUILayout.Slider(new GUIContent("Progress (0–1)"), cfg.progressThreshold, 0f, 1f);

                // Show Spawn Count field
                cfg.spawnCount = EditorGUILayout.IntField(new GUIContent("Spawn Count"), cfg.spawnCount);
                if (cfg.spawnCount < 1) cfg.spawnCount = 1;

                // Only show Subsequent Spawn Delays if spawnCount > 1
                if (cfg.spawnCount > 1)
                {
                    if (cfg.subsequentSpawnDelays == null)
                        cfg.subsequentSpawnDelays = new List<float>();

                    EditorGUILayout.LabelField("Subsequent Spawn Delays (seconds):", EditorStyles.boldLabel);
                    for (int d = 0; d < cfg.subsequentSpawnDelays.Count; d++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        cfg.subsequentSpawnDelays[d] = EditorGUILayout.FloatField($"Delay #{d + 1}", cfg.subsequentSpawnDelays[d]);
                        if (GUILayout.Button("✕", GUILayout.Width(20)))
                        {
                            cfg.subsequentSpawnDelays.RemoveAt(d);
                            break; // safe to break here after ending horizontal group
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("+ Add Delay"))
                    {
                        cfg.subsequentSpawnDelays.Add(0f);
                    }
                }


                GUILayout.Space(6);

               

                //if (cfg.subsequentSpawnDelays == null)
                //    cfg.subsequentSpawnDelays = new List<float>();

                //for (int d = 0; d < cfg.subsequentSpawnDelays.Count; d++)
                //{
                //    EditorGUILayout.BeginHorizontal();
                //    cfg.subsequentSpawnDelays[d] = EditorGUILayout.FloatField($"Delay #{d + 1}", cfg.subsequentSpawnDelays[d]);
                //    if (GUILayout.Button("✕", GUILayout.Width(20)))
                //    {
                //        cfg.subsequentSpawnDelays.RemoveAt(d);
                //        break;  // break is safe here because we just ended horizontal layout
                //    }
                //    EditorGUILayout.EndHorizontal();
                //}
                //if (GUILayout.Button("+ Add Delay"))
                //    cfg.subsequentSpawnDelays.Add(0f);

                GUILayout.Space(6);

                // Fall from height toggle for specific enemy types
                if (cfg.enemyType == SpawnablesType.Pickups
                    || cfg.enemyType == SpawnablesType.FlyingHoop
                    || cfg.enemyType == SpawnablesType.SpikeBall)
                {
                    cfg.useFallDrop = EditorGUILayout.Toggle("Fall from height", cfg.useFallDrop);
                }
                else
                {
                    cfg.useFallDrop = false;
                }

                // Hide yOffset, assign internally
                switch (cfg.enemyType)
                {
                    case SpawnablesType.Pickups:
                        cfg.yOffset = 1.4f;
                        break;
                    case SpawnablesType.FlyingHoop:
                    case SpawnablesType.SpikeBall:
                        cfg.yOffset = 0.7f;
                        break;
                    default:
                        cfg.yOffset = 0f;
                        break;
                }

                GUILayout.Space(6);

                // Remove button in red, single button only
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Remove Spawnable", GUILayout.Width(140)))
                {
                    removeIndex = i; // defer removal
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
            }
        }

        if (removeIndex.HasValue)
        {
            _level.SpwanablesConfigurations.RemoveAt(removeIndex.Value);
            _spawnableFoldouts.RemoveAt(removeIndex.Value);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add New Spawnable Configuration"))
        {
            if (_level.SpwanablesConfigurations == null)
                _level.SpwanablesConfigurations = new List<SpawnableConfig>();

            _level.SpwanablesConfigurations.Add(new SpawnableConfig
            {
                enemyType = (SpawnablesType)0,
                prefab = null,
                useTimeBasedSpawn = false,
                initialDelay = 0f,
                progressThreshold = 0f,
                spawnCount = 1,
                subsequentSpawnDelays = new List<float>(),
                yOffset = 0f,
                initialSpawnHeight = 0f,
                usePhysicsDrop = false,
            });

            _spawnableFoldouts.Add(true);
        }

        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_gameManager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
        }
    }


    // ─────────────────────────────────────────────────────────
    // Tab 3: Preplaced (List of PreplacedPrefabs & PreplacedSpawnPoints)
    // ─────────────────────────────────────────────────────────
    private void DrawPreplacedTab()
    {
        EditorGUILayout.LabelField("Preplaced Prefabs & Spawn Points", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            // 1) Preplaced Prefabs (List<GameObject>)
            EditorGUILayout.LabelField("Preplaced Prefabs:", EditorStyles.miniBoldLabel);
            if (_level.PreplacedPrefabs == null)
                _level.PreplacedPrefabs = new List<GameObject>();

            for (int i = 0; i < _level.PreplacedPrefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _level.PreplacedPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                    $"Prefab #{i + 1}",
                    _level.PreplacedPrefabs[i],
                    typeof(GameObject),
                    false
                );
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                {
                    _level.PreplacedPrefabs.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Preplaced Prefab"))
            {
                _level.PreplacedPrefabs.Add(null);
            }

            GUILayout.Space(8);

            // 2) Preplaced Spawn Points (List<Transform>)
            EditorGUILayout.LabelField("Preplaced Spawn Points:", EditorStyles.miniBoldLabel);
            if (_level.PreplacedSpawnPoints == null)
                _level.PreplacedSpawnPoints = new List<Transform>();

            for (int i = 0; i < _level.PreplacedSpawnPoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _level.PreplacedSpawnPoints[i] = (Transform)EditorGUILayout.ObjectField(
                    $"SpawnPoint #{i + 1}",
                    _level.PreplacedSpawnPoints[i],
                    typeof(Transform),
                    true
                );
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                {
                    _level.PreplacedSpawnPoints.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Preplaced Spawn Point"))
            {
                _level.PreplacedSpawnPoints.Add(null);
            }


            GUILayout.Space(15);
            // 2) Button to call your GameManager’s “Spawn Preplaced Enemies” method
            if (GUILayout.Button("➤ Spawn Preplaced Enemies"))
            {
                _gameManager.PlacePreplacedEnemies(); // your existing method
                EditorUtility.SetDirty(_gameManager);
                EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
            }
        }
        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_gameManager);
            EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
        }
    }

    // ─────────────────────────────────────────────────────────
    // Tab 4: Advanced (Columns/Rows again, plus “Spawn Preplaced Enemies”)
    // ─────────────────────────────────────────────────────────
    private void DrawAdvancedTab()
    {
        EditorGUILayout.LabelField("Advanced / Utilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            // 1) Show Columns/Rows as read‐only labels:
            EditorGUILayout.LabelField("Grid Dimensions", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Columns", _level.Columns.ToString());
            EditorGUILayout.LabelField("Rows", _level.Rows.ToString());
        }
        EditorGUILayout.EndVertical();

    }

    private void OnDisable()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }
}