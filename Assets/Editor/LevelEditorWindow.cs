
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;

public class LevelDataEditorWindow : EditorWindow
{
    private GameManager _gameManager;
    private LevelData _level;          

    private Vector2 _scrollSpawnables = Vector2.zero;
    private List<bool> _spawnableFoldouts = new List<bool>();
    private int _selectedTab = 0;
    private string[] _tabs = new string[] { "General", "Grid" };
    private Dictionary<SpawnablesType, List<GameObject>> _enemyTypeToPrefabsMap = new Dictionary<SpawnablesType, List<GameObject>>();
    // assign your enemy prefabs in the Inspector of the LevelEditorWindow
    // at top of your LevelEditorWindow class:
    private Dictionary<GameObject, Color> _enemyTypeColors = new Dictionary<GameObject, Color>();

    private const int MinGridSize = 1;
    private const int MaxGridSize = 50;

    [MenuItem("Window/Level Data Editor (Friendly)")]
    public static void OpenWindow()
    {
        var wnd = GetWindow<LevelDataEditorWindow>();
        wnd.titleContent = new GUIContent("Level Editor");
        wnd.Show();
    }

    private void OnEnable()
    {
        _gameManager = Object.FindFirstObjectByType<GameManager>();
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
        if (_gameManager != null)
            _level = _gameManager.Level;
    }

    private void OnGUI()
    {
        if (_gameManager == null)
        {
            EditorGUILayout.HelpBox("There is no GameManager in the scene.\nPlease add a GameManager GameObject with a public LevelData Level field.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
                _gameManager = FindFirstObjectByType<GameManager>();
            return;
        }

        if (_gameManager.Level == null)
        {
            EditorGUILayout.HelpBox("GameManager.Level is currently null.\nAssign or create a LevelData instance in GameManager's Inspector first.", MessageType.Warning);
            if (GUILayout.Button("Create & Assign New LevelData"))
            {
                _gameManager.Level = new LevelData
                {
                    LevelObject = null,
                    levelTime = 0f,
                    PreplacedEnemies = new List<PreplacedEnemy>(),
                    gridPositions = new List<Cube>(),
                    SpwanablesConfigurations = new List<SpawnableConfig>(),
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

        _level = _gameManager.Level;

        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);

        GUILayout.Space(8);


        switch (_selectedTab)
        {
            case 0:
                DrawGeneralTab();
                break;
            case 1:
                DrawGridTab();  
                break;
            //case 2:
            //    //DrawSpawnablesTab();
            //    break;
            //case 3:
            //    //DrawPreplacedTab();
            //    break;
            //case 4:
            //    DrawAdvancedTab();
            //    break;
        }

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
            _level.LevelObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Level Root Object"),
                _level.LevelObject,
                typeof(GameObject),
                true
            );

            _level.levelTime = EditorGUILayout.FloatField(
                new GUIContent("Level Time (seconds)"),
                _level.levelTime
            );

            EditorGUILayout.LabelField("Preplaced Prefabs:", EditorStyles.miniBoldLabel);
            // lazy‐init
            if (_level.PreplacedEnemies == null)
                _level.PreplacedEnemies = new List<PreplacedEnemy>();

            for (int i = 0; i < _level.PreplacedEnemies.Count; i++)
            {
                var entry = _level.PreplacedEnemies[i];
                EditorGUILayout.BeginHorizontal();

                // draw the prefab selector
                entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Prefab #{i + 1}",
                    entry.prefab,
                    typeof(GameObject),
                    false
                );

                // draw row/col
                entry.row = EditorGUILayout.IntField(entry.row, GUILayout.Width(40));
                entry.col = EditorGUILayout.IntField(entry.col, GUILayout.Width(40));

                // remove button
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                {
                    _level.PreplacedEnemies.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            // add new entry
            if (GUILayout.Button("+ Add Preplaced Enemy"))
            {
                _level.PreplacedEnemies.Add(new PreplacedEnemy
                {
                    prefab = null,
                    row = 0,
                    col = 0
                });
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Type Colors", EditorStyles.miniBoldLabel);

            // Gather all unique, non-null prefabs in your level
            var uniquePrefabs = _level.PreplacedEnemies
                                     .Where(e => e.prefab != null)
                                     .Select(e => e.prefab)
                                     .Distinct()
                                     .ToList();

            // Remove stale entries
            foreach (var key in _enemyTypeColors.Keys.ToList())
                if (!uniquePrefabs.Contains(key))
                    _enemyTypeColors.Remove(key);

            // For each prefab, show a color picker
            foreach (var prefab in uniquePrefabs)
            {
                // give it a default if needed
                if (!_enemyTypeColors.TryGetValue(prefab, out var col))
                    _enemyTypeColors[prefab] = Color.HSVToRGB(
                        Random.value, 0.5f, 0.8f
                    );

                _enemyTypeColors[prefab] = EditorGUILayout.ColorField(
                    prefab.name, _enemyTypeColors[prefab]
                );
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_gameManager);
                EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
            }

            EditorGUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_gameManager);
                EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // Tab 1: Grid (Columns, Rows, simple grid info)
    // ─────────────────────────────────────────────────────────

    private Vector2 scrollPosition = Vector2.zero;

    private void DrawGridTab()
    {
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        int cols = EditorGUILayout.IntField("Columns", _level.Columns);
        _level.Columns = Mathf.Clamp(cols, MinGridSize, MaxGridSize);

        int rows = EditorGUILayout.IntField("Rows", _level.Rows);
        _level.Rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize);

        SyncSceneGridAndBackground();

        if (GUILayout.Button("Fill All Cubes"))
            FillAllCubes();
        if (GUILayout.Button("Clear All"))
            ClearAllCubes();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Selection Type", EditorStyles.boldLabel);
        string[] selectionOptions = new string[] { "Wall", "Empty" };
        _selectedSelectionIndex = EditorGUILayout.Popup("Select Type", _selectedSelectionIndex, selectionOptions);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                                   GUILayout.Width(700), GUILayout.Height(700));
        GUILayout.Space(10);

        Event e = Event.current;
        bool hasPlayerStart = _level.PlayerStartRow >= 0 && _level.PlayerStartCol >= 0;

        for (int row = 0; row < _level.Rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < _level.Columns; col++)
            {
                bool isPlayerStart = row == _level.PlayerStartRow && col == _level.PlayerStartCol;
                bool isOccupied = _level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.isObstacle);
                bool shouldFire = (_selectedSelectionIndex == 0 && !isOccupied)|| (_selectedSelectionIndex == 1 && isOccupied);
                Rect cellRect = GUILayoutUtility.GetRect(30, 30);

                Color fill;
                if (isPlayerStart)
                {
                    fill = Color.green;
                }
                else
                {
                    // check for a preplaced enemy at this cell
                    var pe = _level.PreplacedEnemies
                                  .FirstOrDefault(e => e.row == row && e.col == col);
                    if (pe != null && pe.prefab != null && _enemyTypeColors.TryGetValue(pe.prefab, out var c))
                    {
                        fill = c;              // use the user‐picked color
                    }
                    else if (isOccupied)
                    {
                        fill = Color.red;      // still your obstacle color
                    }
                    else
                    {
                        fill = Color.white;    // empty
                    }
                }
                EditorGUI.DrawRect(cellRect, fill);

                Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black);

                // ── Right-click down on the cell → context menu ─────────────────
                if (e.type == EventType.MouseDown && e.button == 1 && cellRect.Contains(e.mousePosition))
                {
                    int r = row, c = col;
                    var menu = new GenericMenu();

                    // 1) Keep your “Set Player Start” option
                    menu.AddItem(
                        new GUIContent("Set Player Start"),
                        isPlayerStart,
                        () => SetPlayerStart(r, c)
                    );

                    // 2) Add a separator so “Preplaced Enemy” becomes its own submenu
                    menu.AddSeparator("");

                    // 3) Populate the Preplaced Enemy submenu
                    if (_level.PreplacedEnemies == null || _level.PreplacedEnemies.Count == 0)
                    {
                        menu.AddDisabledItem(new GUIContent("Preplaced Enemy/— no prefabs assigned —"));
                    }
                    else
                    {
                        foreach (var entry in _level.PreplacedEnemies)
                        {
                            if (entry.prefab == null) continue;
                            menu.AddItem(
                                new GUIContent($"Preplaced Enemy/{entry.prefab.name}"),
                                false,
                                () => PlacePreplacedEnemy(r, c, entry.prefab)
                            );
                        }
                    }

                    menu.ShowAsContext();
                    e.Use();
                }

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                    && e.button == 0
                    && cellRect.Contains(e.mousePosition))
                    {
                    

                    if (shouldFire)
                    {
                        HandleCellClick(row, col);

                        if (_selectedSelectionIndex == 1)
                        {
                            DestroyObstacleAt(row, col);
                        }
                    }

                    e.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void PlacePreplacedEnemy(int row, int col, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is null. Cannot place preplaced enemy.");
            return;
        }

        _level.PreplacedEnemies.Add(new PreplacedEnemy
        {
            prefab = prefab,
            row = row,
            col = col
   });

#if UNITY_EDITOR
        UnityEditor.Undo.IncrementCurrentGroup();
        UnityEditor.Undo.SetCurrentGroupName("Place Preplaced Enemy");
#endif

        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;
        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;
        int flippedRow = _level.Rows - row - 1;
        Vector3 worldPos = new Vector3(
            col + offsetX,
            prefab.transform.position.y,
            flippedRow + offsetZ
        );

#if UNITY_EDITOR
        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = worldPos;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Place Preplaced Enemy");
        UnityEditor.EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
#else
        Instantiate(prefab, worldPos, Quaternion.identity);
#endif

        Repaint();
    }

    private void SetPlayerStart(int row, int col)
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(_gameManager, "Set Player Start");
#endif

        _level.PlayerStartRow = row;
        _level.PlayerStartCol = col;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_gameManager);
#endif

        Repaint();  

        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        Vector3 world = gm.GridToWorld(new Vector2Int(col, row));
        world.y = playerGO.transform.position.y;
        playerGO.transform.position = world;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(playerGO);
#endif
    }


    private void SyncSceneGridAndBackground()
    {
#if UNITY_EDITOR
        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null)
        {
            Debug.LogWarning("[LevelEditor] No GridManager found in scene to sync with!");
            return;
        }

        gm.InitGrid(_level.Columns, _level.Rows);
        EditorUtility.SetDirty(gm);
        Debug.Log($"[LevelEditor] Synced GridManager to {_level.Columns}×{_level.Rows}");

        var bg = Object.FindFirstObjectByType<GridBackground>();
        if (bg != null)
        {
            bg.UpdateVisuals();
            EditorUtility.SetDirty(bg);
            Debug.Log("[LevelEditor] GridBackground visuals updated");
        }
        else
        {
            Debug.LogWarning("[LevelEditor] No GridBackground component found!");
        }

        SceneView.RepaintAll();
#endif
    }


    private int _selectedSelectionIndex = 0; 

    private void HandleCellClick(int row, int col)
    {
        bool isObstacle = _selectedSelectionIndex == 0;

        float baseOffsetX = -(_level.Columns - 1) * 0.5f; 
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f; 

        float offsetX = baseOffsetX - 0.5f;  
        float offsetZ = baseOffsetZ + 0.5f;
        int flippedRow = _level.Rows - row - 1;  
        Vector3 spawnPosition = new Vector3(col + offsetX, 0.5f, flippedRow + offsetZ);

        bool isOccupied = _level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.isObstacle);

        if (isObstacle)
        {
            if (!isOccupied)
            {
                _level.gridCellPositions.Add(new CubeCell { row = row, col = col, isObstacle = true });
                Instantiate(_level.obstaclePrefab, spawnPosition, Quaternion.identity);  
            }
        }
        else
        {
            if (isOccupied)
            {
                _level.gridCellPositions.RemoveAll(c => c.row == row && c.col == col && c.isObstacle);
                DestroyObstacleAt(row, col);  
            }
        }

        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }


    private void DestroyObstacleAt(int row, int col)
    {
        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;

        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;

        Vector3 obstaclePosition = new Vector3(col + offsetX, 0.5f, (_level.Rows - row - 1) + offsetZ);  

        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");  

        foreach (var obstacle in obstacles)
        {
            if (Vector3.Distance(obstacle.transform.position, obstaclePosition) < 0.1f)  
            {
                DestroyImmediate(obstacle); 
                Debug.Log($"Destroyed obstacle at: {obstaclePosition}");
                return;
            }
        }
        Debug.LogWarning($"No obstacle found at position: {obstaclePosition}");
    }
    private void FillAllCubes()
    {
        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;

        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;

        for (int row = 0; row < _level.Rows; row++)
        {
            for (int col = 0; col < _level.Columns; col++)
            {
                if (!_level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.isObstacle))
                {
                    _level.gridCellPositions.Add(new CubeCell { row = row, col = col, isObstacle = true });

                    Vector3 spawnPosition = new Vector3(col + offsetX, 0.5f, (_level.Rows - row - 1) + offsetZ);  
                    Instantiate(_level.obstaclePrefab, spawnPosition, Quaternion.identity);  
                }
            }
        }
        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }


    private void ClearAllCubes()
    {
        _level.gridCellPositions.RemoveAll(c => c.isObstacle);

        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");  
        foreach (var obstacle in obstacles)
        {
            DestroyImmediate(obstacle); 
        }

        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }



    //private void DrawSpawnablesTab()
    //{
    //    EditorGUILayout.LabelField("Spawnable Configurations", EditorStyles.boldLabel);
    //    EditorGUILayout.BeginVertical("box");

    //    if (_level.SpwanablesConfigurations == null)
    //        _level.SpwanablesConfigurations = new List<SpawnableConfig>();

    //    while (_spawnableFoldouts.Count < _level.SpwanablesConfigurations.Count)
    //        _spawnableFoldouts.Add(true);
    //    while (_spawnableFoldouts.Count > _level.SpwanablesConfigurations.Count)
    //        _spawnableFoldouts.RemoveAt(_spawnableFoldouts.Count - 1);

    //    _scrollSpawnables = EditorGUILayout.BeginScrollView(_scrollSpawnables, GUILayout.Height(800));
    //    int? removeIndex = null;

    //    for (int i = 0; i < _level.SpwanablesConfigurations.Count; i++)
    //    {
    //        SpawnableConfig cfg = _level.SpwanablesConfigurations[i];
    //        if (cfg == null) continue;

    //        if (_enemyTypeToPrefabsMap.TryGetValue(cfg.enemyType, out var prefabList) && prefabList.Count > 0)
    //        {
    //            if (cfg.prefab == null || !prefabList.Contains(cfg.prefab))
    //                cfg.prefab = prefabList[0];

    //            string[] prefabNames = prefabList.ConvertAll(p => p != null ? p.name : "Null").ToArray();

    //            int selectedIndex = prefabList.IndexOf(cfg.prefab);
    //            if (selectedIndex < 0) selectedIndex = 0;

    //            int newIndex = EditorGUILayout.Popup("Prefab Variant", selectedIndex, prefabNames);
    //            if (newIndex != selectedIndex)
    //            {
    //                cfg.prefab = prefabList[newIndex];
    //                EditorUtility.SetDirty(_gameManager);
    //                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    //            }
    //        }
    //        else
    //        {
    //            cfg.prefab = (GameObject)EditorGUILayout.ObjectField(
    //                new GUIContent("Prefab"),
    //                cfg.prefab,
    //                typeof(GameObject),
    //                false
    //            );
    //        }

    //        _spawnableFoldouts[i] = EditorGUILayout.Foldout(_spawnableFoldouts[i], $"Spawnable #{i + 1}: {cfg.enemyType}", true);
    //        if (_spawnableFoldouts[i])
    //        {
    //            EditorGUILayout.BeginVertical("box");
    //            EditorGUI.indentLevel++;

    //            cfg.enemyType = (SpawnablesType)EditorGUILayout.EnumPopup(new GUIContent("Enemy Type"), cfg.enemyType);

    //            cfg.prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab"), cfg.prefab, typeof(GameObject), false);

    //            EditorGUILayout.Space();

    //            EditorGUILayout.LabelField("Spawn Trigger Settings", EditorStyles.boldLabel);
    //            cfg.useTimeBasedSpawn = EditorGUILayout.ToggleLeft("Use Time-Based Spawn", cfg.useTimeBasedSpawn);

    //            if (cfg.useTimeBasedSpawn)
    //                cfg.initialDelay = EditorGUILayout.FloatField(new GUIContent("Initial Delay (sec)"), cfg.initialDelay);
    //            else
    //                cfg.progressThreshold = EditorGUILayout.Slider(new GUIContent("Progress (0–1)"), cfg.progressThreshold, 0f, 1f);

    //            cfg.spawnCount = EditorGUILayout.IntField(new GUIContent("Spawn Count"), cfg.spawnCount);
    //            if (cfg.spawnCount < 1) cfg.spawnCount = 1;

    //            if (cfg.spawnCount > 1)
    //            {
    //                if (cfg.subsequentSpawnDelays == null)
    //                    cfg.subsequentSpawnDelays = new List<float>();

    //                EditorGUILayout.LabelField("Subsequent Spawn Delays (seconds):", EditorStyles.boldLabel);
    //                for (int d = 0; d < cfg.subsequentSpawnDelays.Count; d++)
    //                {
    //                    EditorGUILayout.BeginHorizontal();
    //                    cfg.subsequentSpawnDelays[d] = EditorGUILayout.FloatField($"Delay #{d + 1}", cfg.subsequentSpawnDelays[d]);
    //                    if (GUILayout.Button("✕", GUILayout.Width(20)))
    //                    {
    //                        cfg.subsequentSpawnDelays.RemoveAt(d);
    //                        break; 
    //                    }
    //                    EditorGUILayout.EndHorizontal();
    //                }
    //                if (GUILayout.Button("+ Add Delay"))
    //                {
    //                    cfg.subsequentSpawnDelays.Add(0f);
    //                }
    //            }

    //            GUILayout.Space(6);

    //            if (cfg.enemyType == SpawnablesType.Pickups
    //                || cfg.enemyType == SpawnablesType.FlyingHoop
    //                || cfg.enemyType == SpawnablesType.SpikeBall)
    //            {
    //                cfg.useFallDrop = EditorGUILayout.Toggle("Fall from height", cfg.useFallDrop);
    //            }
    //            else
    //            {
    //                cfg.useFallDrop = false;
    //            }

    //            switch (cfg.enemyType)
    //            {
    //                case SpawnablesType.Pickups:
    //                    cfg.yOffset = 1.4f;
    //                    break;
    //                case SpawnablesType.FlyingHoop:
    //                case SpawnablesType.SpikeBall:
    //                    cfg.yOffset = 0.7f;
    //                    break;
    //                default:
    //                    cfg.yOffset = 0f;
    //                    break;
    //            }

    //            GUILayout.Space(6);

    //            EditorGUILayout.BeginHorizontal();
    //            GUILayout.FlexibleSpace();
    //            GUI.backgroundColor = Color.red;
    //            if (GUILayout.Button("Remove Spawnable", GUILayout.Width(140)))
    //            {
    //                removeIndex = i; 
    //            }
    //            GUI.backgroundColor = Color.white;
    //            EditorGUILayout.EndHorizontal();

    //            EditorGUI.indentLevel--;
    //            EditorGUILayout.EndVertical();

    //            GUILayout.Space(10);
    //        }
    //    }

    //    if (removeIndex.HasValue)
    //    {
    //        _level.SpwanablesConfigurations.RemoveAt(removeIndex.Value);
    //        _spawnableFoldouts.RemoveAt(removeIndex.Value);
    //    }

    //    EditorGUILayout.EndScrollView();

    //    if (GUILayout.Button("+ Add New Spawnable Configuration"))
    //    {
    //        if (_level.SpwanablesConfigurations == null)
    //            _level.SpwanablesConfigurations = new List<SpawnableConfig>();

    //        _level.SpwanablesConfigurations.Add(new SpawnableConfig
    //        {
    //            enemyType = (SpawnablesType)0,
    //            prefab = null,
    //            useTimeBasedSpawn = false,
    //            initialDelay = 0f,
    //            progressThreshold = 0f,
    //            spawnCount = 1,
    //            subsequentSpawnDelays = new List<float>(),
    //            yOffset = 0f,
    //            initialSpawnHeight = 0f,
    //            usePhysicsDrop = false,
    //        });

    //        _spawnableFoldouts.Add(true);
    //    }

    //    EditorGUILayout.EndVertical();

    //    if (GUI.changed)
    //    {
    //        EditorUtility.SetDirty(_gameManager);
    //        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    //    }
    //}


    // ─────────────────────────────────────────────────────────
    // Tab 3: Preplaced (List of PreplacedPrefabs & PreplacedSpawnPoints)
    // ─────────────────────────────────────────────────────────
    //private void DrawPreplacedTab()
    //{
    //    EditorGUILayout.LabelField("Preplaced Prefabs & Spawn Points", EditorStyles.boldLabel);
    //    EditorGUILayout.BeginVertical("box");
    //    {
    //        EditorGUILayout.LabelField("Preplaced Prefabs:", EditorStyles.miniBoldLabel);
    //        if (_level.PreplacedPrefabs == null)
    //            _level.PreplacedPrefabs = new List<GameObject>();

    //        for (int i = 0; i < _level.PreplacedPrefabs.Count; i++)
    //        {
    //            EditorGUILayout.BeginHorizontal();
    //            _level.PreplacedPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
    //                $"Prefab #{i + 1}",
    //                _level.PreplacedPrefabs[i],
    //                typeof(GameObject),
    //                false
    //            );
    //            if (GUILayout.Button("✕", GUILayout.Width(20)))
    //            {
    //                _level.PreplacedPrefabs.RemoveAt(i);
    //                break;
    //            }
    //            EditorGUILayout.EndHorizontal();
    //        }
    //        if (GUILayout.Button("+ Add Preplaced Prefab"))
    //        {
    //            _level.PreplacedPrefabs.Add(null);
    //        }

    //        GUILayout.Space(8);

    //        EditorGUILayout.LabelField("Preplaced Spawn Points:", EditorStyles.miniBoldLabel);
    //        if (_level.PreplacedSpawnPoints == null)
    //            _level.PreplacedSpawnPoints = new List<Transform>();

    //        for (int i = 0; i < _level.PreplacedSpawnPoints.Count; i++)
    //        {
    //            EditorGUILayout.BeginHorizontal();
    //            _level.PreplacedSpawnPoints[i] = (Transform)EditorGUILayout.ObjectField(
    //                $"SpawnPoint #{i + 1}",
    //                _level.PreplacedSpawnPoints[i],
    //                typeof(Transform),
    //                true
    //            );
    //            if (GUILayout.Button("✕", GUILayout.Width(20)))
    //            {
    //                _level.PreplacedSpawnPoints.RemoveAt(i);
    //                break;
    //            }
    //            EditorGUILayout.EndHorizontal();
    //        }
    //        if (GUILayout.Button("+ Add Preplaced Spawn Point"))
    //        {
    //            _level.PreplacedSpawnPoints.Add(null);
    //        }


    //        GUILayout.Space(15);
    //        if (GUILayout.Button("➤ Spawn Preplaced Enemies"))
    //        {
    //            _gameManager.PlacePreplacedEnemies(); 
    //            EditorUtility.SetDirty(_gameManager);
    //            EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    //        }
    //    }
    //    EditorGUILayout.EndVertical();

    //    if (GUI.changed)
    //    {
    //        EditorUtility.SetDirty(_gameManager);
    //        EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    //    }
    //}

    // ─────────────────────────────────────────────────────────
    // Tab 4: Advanced (Columns/Rows again, plus “Spawn Preplaced Enemies”)
    // ─────────────────────────────────────────────────────────
    private void DrawAdvancedTab()
    {
        EditorGUILayout.LabelField("Advanced / Utilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
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