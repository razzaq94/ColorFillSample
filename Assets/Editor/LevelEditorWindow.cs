
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

    private int _selectedTab = 0;
    private string[] _tabs = new string[] { "General", "Grid" };
    private Dictionary<SpawnablesType, List<GameObject>> _enemyTypeToPrefabsMap = new Dictionary<SpawnablesType, List<GameObject>>();
    private Dictionary<GameObject, Color> _enemyTypeColors = new Dictionary<GameObject, Color>();
    private const int MinGridSize = 1;
    private const int MaxGridSize = 50;
    private Dictionary<Vector2Int, GameObject> _placedEnemyMap = new Dictionary<Vector2Int, GameObject>();
    private enum DrawMode { Wall, Empty, Enemy }
    private DrawMode _drawMode = DrawMode.Wall;
   

    public Dictionary<SpawnablesType, List<GameObject>> EnemyTypeToPrefabsMap => _enemyTypeToPrefabsMap;

    // Maps grid‐cells → their placed EnemyCube instance
    private Dictionary<Vector2Int, EnemyCube> _placedEnemyCubeMap
        = new Dictionary<Vector2Int, EnemyCube>();
    private bool _cubeMapDirty = true;

    private int _selectedSelectionIndex = 0;

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
        RefreshEnemyCubeMap();
        RefreshSpawnableMap();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        _cubeMapDirty = true;

        _enemyTypeToPrefabsMap[SpawnablesType.Pickups] = new List<GameObject>
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/DIAMOND.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/SlowDown.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/Timer.prefab"),
        };
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

            

            if (GUI.changed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(_gameManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
#endif
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Type Colors", EditorStyles.miniBoldLabel);

            var uniquePrefabs = _level.PreplacedEnemies
                                     .Where(e => e.prefab != null)
                                     .Select(e => e.prefab)
                                     .Distinct()
                                     .ToList();

            foreach (var key in _enemyTypeColors.Keys.ToList())
                if (!uniquePrefabs.Contains(key))
                    _enemyTypeColors.Remove(key);

            foreach (var prefab in uniquePrefabs)
            {
                if (!_enemyTypeColors.TryGetValue(prefab, out var col))
                    _enemyTypeColors[prefab] = Color.HSVToRGB(
                        Random.value, 0.5f, 0.8f
                    );

                _enemyTypeColors[prefab] = EditorGUILayout.ColorField(
                    prefab.name, _enemyTypeColors[prefab]
                );
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

        DrawGridSizeControls();
        DrawFillClearButtons();
        DrawEnemyGroupButton();
        DrawSelectionTypePopup();
        DrawGridCells();

        EditorGUILayout.EndVertical();
    }

    private void DrawGridSizeControls()
    {
        int cols = EditorGUILayout.IntField("Columns", _level.Columns);
        _level.Columns = Mathf.Clamp(cols, MinGridSize, MaxGridSize);
        int rows = EditorGUILayout.IntField("Rows", _level.Rows);
        _level.Rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize);
        SyncSceneGridAndBackground();
    }

    private void DrawFillClearButtons()
    {
        if (GUILayout.Button("Fill All Cubes")) FillAllCubes();
        if (GUILayout.Button("Clear All")) ClearAllCubes();
        GUILayout.Space(5);
    }

    private void DrawEnemyGroupButton()
    {
        if (_drawMode == DrawMode.Enemy)
        {
            EditorGUI.BeginDisabledGroup(_placedEnemyCubeMap.Count == 0);
            if (GUILayout.Button("Create Enemy Group"))
                CreateEnemyGroup();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(8);
        }
    }

    private void DrawSelectionTypePopup()
    {
        EditorGUILayout.LabelField("Selection Type", EditorStyles.boldLabel);
        string[] modes = { "Wall", "Empty", "Enemy" };
        _drawMode = (DrawMode)EditorGUILayout.Popup("Select Type", (int)_drawMode, modes);
    }

    private void DrawGridCells()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
            GUILayout.Width(700), GUILayout.Height(700));
        GUILayout.Space(10);

        var e = Event.current;
        bool hasPlayer = _level.PlayerStartRow >= 0 && _level.PlayerStartCol >= 0;
        EnsureEnemyTypeColors();

        for (int row = 0; row < _level.Rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < _level.Columns; col++)
                DrawCell(row, col, e, hasPlayer);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
    private void EnsureEnemyTypeColors()
    {
        if (_enemyTypeColors == null)
            _enemyTypeColors = new Dictionary<GameObject, Color>();

        // For each distinct pre‐placed enemy prefab, assign it a repeatable color
        foreach (var go in _placedEnemyMap.Values.Distinct())
        {
            if (!_enemyTypeColors.ContainsKey(go))
                _enemyTypeColors[go] = Color.HSVToRGB(
                    (_enemyTypeColors.Count * .618f) % 1f,  // golden-ratio spacing
                    .6f,
                    .8f
                );
        }
    }

    private void DrawCell(int row, int col, Event e, bool hasPlayer)
    {
        var key = new Vector2Int(row, col);
        bool isWall = _level.gridCellPositions.Any(c => c.row == row && c.col == col && c.isObstacle);
        bool isPre = _placedEnemyMap.ContainsKey(key);
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);
        bool isPlayer = hasPlayer && row == _level.PlayerStartRow && col == _level.PlayerStartCol;
        bool isSpawnable = _cellSpawnMap != null && _cellSpawnMap.ContainsKey(key);

        bool occupied = isWall || isPre || isCube || isSpawnable;

        bool shouldPaint =
               (_drawMode == DrawMode.Wall && !occupied)
            || (_drawMode == DrawMode.Enemy && !occupied)
            || (_drawMode == DrawMode.Empty && occupied);

        // 1) draw box
        Rect cellRect = GUILayoutUtility.GetRect(30, 30);
        DrawCellBackground(cellRect, key, isWall, isPre, isCube, isPlayer);
        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black);

        if (isCube) DrawMovementArrow(cellRect, key);

        if (e.type == EventType.MouseDown && e.button == 1 && cellRect.Contains(e.mousePosition) && CanRightClickCell(row, col))
        {
            ShowCellContextMenu(cellRect, key, isWall, isPre, occupied, row, col);
            e.Use();
        }

        if (shouldPaint
            && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            && e.button == 0
            && cellRect.Contains(e.mousePosition))
        {
            if (_drawMode == DrawMode.Wall) HandleCellClick(row, col);

            if (_drawMode == DrawMode.Empty)
            {
                var cellKey = new Vector2Int(row, col);

                if (_cellSpawnMap != null && _cellSpawnMap.TryGetValue(cellKey, out var cfg))
                {
                    _level.SpwanablesConfigurations.Remove(cfg);
                    RefreshSpawnableMap();

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(_gameManager);
                    UnityEditor.SceneManagement.EditorSceneManager
                        .MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
                    Repaint();
                }
                else
                {
                    DestroyAt(row, col);
                    Repaint();
                }

                e.Use(); 
                return;
            }

            else if (_drawMode == DrawMode.Enemy)
                PlaceEnemyCube(row, col, key);

            Repaint();
            e.Use();
        }
    }

    private void DrawCellBackground(Rect r, Vector2Int key, bool wall, bool pre,bool cube, bool player)
    {
        Color baseColor;
        if (player)
            baseColor = Color.green;
        else if (cube)
            baseColor = Color.yellow;
        else if (pre && _placedEnemyMap.ContainsKey(key))
            baseColor = _enemyTypeColors[_placedEnemyMap[key]];
        else if (wall)
            baseColor = Color.red;
        else
            baseColor = Color.white;

        EditorGUI.DrawRect(r, baseColor);

        if (_cellSpawnMap != null
            && _cellSpawnMap.TryGetValue(key, out var cfg))
        {
            Color overlay = cfg.enemyType == SpawnablesType.SpikeBall
                ? new Color(1f, 1f, 0f, 0.3f)
                : new Color(0f, 1f, 1f, 0.5f);

            EditorGUI.DrawRect(r, overlay);
        }
    }


    private void DrawMovementArrow(Rect r, Vector2Int key)
    {
        // 1) guard against missing/destroyed cubes
        if (!_placedEnemyCubeMap.TryGetValue(key, out var ec) || ec == null)
            return;

        // 2) guard against missing transform (just in case)
        var t = ec.transform;
        if (t == null)
            return;

        // 3) grab the group
        var group = t.parent?.GetComponent<EnemyCubeGroup>();
        if (group == null)
            return;

        // 4) finally draw the arrow
        GUI.Label(r,
            group.moveHorizontal && !group.moveVertical ? "→" :
            group.moveVertical && !group.moveHorizontal ? "↑" : "",
            EditorStyles.boldLabel);
    }


    private void ShowCellContextMenu(Rect r, Vector2Int key,
        bool isWall, bool isPre, bool occupied, int row, int col)
    {
        var menu = new GenericMenu();
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);
        if (isPre)
        {
            menu.AddItem(new GUIContent("Remove Preplaced Enemy"), false, () =>
            {
                var go = _placedEnemyMap[key];
#if UNITY_EDITOR
                DestroyImmediate(go);
#else
            Destroy(go);
#endif
                _placedEnemyMap.Remove(key);
                Repaint();
            });
        }
        else if (isCube)
        {
            AddCubeContextItems(menu, key);
        }
        else
        {
            menu.AddItem(new GUIContent("Set Player Start"), false, () => SetPlayerStart(row, col));
            menu.AddSeparator("");
            AddPreplacedSpawnItems(menu, row, col);
        }
        var screenMouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        var rect = new Rect(screenMouse.x, screenMouse.y, 1, 1);

        menu.AddSeparator("Spawnable/");
        if (_cellSpawnMap != null&& _cellSpawnMap.TryGetValue(key, out var cellCfg))
        {
            // already has a config → edit or remove
            menu.AddItem(new GUIContent("Spawnable/Edit Config"), false, () =>
            {
                SpawnableCellPopup.Open(cellCfg, _enemyTypeToPrefabsMap, new Rect(screenMouse.x, screenMouse.y, 1, 1));
            });

            menu.AddItem(new GUIContent("Spawnable/Remove Config"), false, () => {
                EditorApplication.delayCall += () =>
                {
                    _level.SpwanablesConfigurations.Remove(cellCfg);
                    RefreshSpawnableMap();
                    Repaint();
                    EditorUtility.SetDirty(_gameManager);
                };
            });
        }
        else
        {
            menu.AddSeparator("Spawnable/");
            // loop over all types…
            foreach (SpawnablesType t in System.Enum.GetValues(typeof(SpawnablesType)))
            {
                // capture the current value so each lambda gets its own copy
                var spawnType = t;
                menu.AddItem(new GUIContent($"Spawnable/{spawnType}"), false, () =>
                {
                    // decide yOffset (and useFallDrop if you like) *before* creating cfg
                    float yOffset;
                    bool useFall = false;
                    switch (spawnType)
                    {
                        case SpawnablesType.FlyingHoop:
                            yOffset = 2f;
                            useFall = false;
                            break;
                        case SpawnablesType.Pickups:
                            yOffset = 1.4f;
                            useFall = true;
                            break;
                        case SpawnablesType.SpikeBall:
                            yOffset = 1.6f;
                            useFall = false;
                            break;
                        default:
                            yOffset = 0f;
                            break;
                    }

                    var cfg = new SpawnableConfig
                    {
                        enemyType = spawnType,
                        prefab = null,
                        row = row,
                        col = col,
                        spawnCount = 1,
                        progressThreshold = 0.5f,
                        subsequentProgressThresholds = new List<float>(),
                        usePhysicsDrop = useFall,
                        yOffset = yOffset
                    };

                    _level.SpwanablesConfigurations.Add(cfg);
                    RefreshSpawnableMap();
                    EditorUtility.SetDirty(_gameManager);

                    var screenMouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                    SpawnableCellPopup.Open(
                        cfg,
                        _enemyTypeToPrefabsMap,
                        new Rect(screenMouse.x, screenMouse.y, 1, 1)
                    );
                });
            }

        }
        menu.ShowAsContext();
    }

    private void AddCubeContextItems(GenericMenu menu, Vector2Int key)
    {
        var ec = _placedEnemyCubeMap[key];
        var group = ec.transform.parent?.GetComponent<EnemyCubeGroup>()
                  ?? CreateTempGroupFor(ec);

        // movement mode
        menu.AddItem(new GUIContent("Movement/Static"),
            !group.moveHorizontal && !group.moveVertical,
            () => SetEnemyGroupMovement(group, false, false, group.moveCells));
        menu.AddItem(new GUIContent("Movement/Horizontal"),
            group.moveHorizontal && !group.moveVertical,
            () => SetEnemyGroupMovement(group, true, false, group.moveCells));
        menu.AddItem(new GUIContent("Movement/Vertical"),
            !group.moveHorizontal && group.moveVertical,
            () => SetEnemyGroupMovement(group, false, true, group.moveCells));

        var screenMouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

        menu.AddSeparator("Movement/");
        menu.AddItem(
            new GUIContent("Movement/Set Distance…"),
            false,
            () =>
            {
                var dropRect = new Rect(screenMouse.x, screenMouse.y, 1, 1);
                MoveDistancePopup.Open(group, dropRect);
            }
        );

        menu.AddSeparator("Group/");
        menu.AddItem(new GUIContent("Group/Remove Entire Group"), false, () => {
            var grp = ec.transform.parent.GetComponent<EnemyCubeGroup>();
            EditorApplication.delayCall += () => {
                if (grp != null)
                    Undo.DestroyObjectImmediate(grp.gameObject);
                _cubeMapDirty = true;
                RefreshEnemyCubeMap();
                Repaint();
            };
        });

    }

    private void AddPreplacedSpawnItems(GenericMenu menu, int row, int col)
    {
        if (_level.PreplacedEnemies?.Count > 0)
        {
            foreach (var entry in _level.PreplacedEnemies)
            {
                if (entry.prefab == null) continue;
                int r = row, c = col;
                GameObject prefab = entry.prefab;
                menu.AddItem(new GUIContent($"Preplaced Enemy/{prefab.name}"), false, () => {
                    var gm = FindFirstObjectByType<GridManager>();
                    var wp = gm.GridToWorld(new Vector2Int(c, r));
                    wp.y = prefab.transform.position.y;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    _gameManager.RandomizeColor(go);
                    go.transform.position = wp;
                    _placedEnemyMap[new Vector2Int(r, c)] = go;
                    Repaint();
                });
            }
        }
        else
            menu.AddDisabledItem(new GUIContent("Preplaced Enemy/— none assigned —"));
    }

    private void PlaceEnemyCube(int row, int col, Vector2Int key)
    {
        var prefab = _level.enemyCubePrefab;
        var gm = FindFirstObjectByType<GridManager>();
        var wp = gm.GridToWorld(new Vector2Int(col, row));
        wp.y = prefab.transform.position.y;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = wp;

        _placedEnemyCubeMap[key] = go.GetComponent<EnemyCube>();
    }



    // ─────────────────────────────────────────────────────────
    // Draw Grid Tab Ends Here 
    // ─────────────────────────────────────────────────────────



    private void OnInspectorUpdate()
    {
        if (_gameManager != null)
            _level = _gameManager.Level;
        if (!_cubeMapDirty) return;

        SyncSceneGridAndBackground();

        RefreshEnemyCubeMap();
        _cubeMapDirty = false;
        Repaint();
    }
    // ─────────────────────────────────────────────────────────
    // Rebuilds the in-memory map of all EnemyCube instances
    // so DrawGridTab() can paint them yellow.
    // ─────────────────────────────────────────────────────────
    private void RefreshEnemyCubeMap()
    {
        if (_placedEnemyCubeMap == null)
            _placedEnemyCubeMap = new Dictionary<Vector2Int, EnemyCube>();
        else
            _placedEnemyCubeMap.Clear();

        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        foreach (var ec in Object.FindObjectsByType<EnemyCube>(FindObjectsSortMode.None))
        {
            Vector2Int gridCR = gm.WorldToGrid(ec.transform.position);

            var key = new Vector2Int(gridCR.y, gridCR.x);
            _placedEnemyCubeMap[key] = ec;
        }

        Repaint();
    }


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
                DestroyAt(row, col);
            }
        }

        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
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

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            DestroyImmediate(enemy);
        }
        foreach (var grp in Object.FindObjectsOfType<EnemyCubeGroup>())
            DestroyImmediate(grp.gameObject);

        var enemyGroups = GameObject.FindGameObjectsWithTag("EnemyGroup");
        foreach (var grp in enemyGroups)
        {
            DestroyImmediate(grp);
        }

        _level.SpwanablesConfigurations.Clear();
        RefreshSpawnableMap();

        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }

    private void CreateEnemyGroup()
    {
        int idx = Object.FindObjectsOfType<EnemyCubeGroup>().Length;
        var parent = new GameObject($"EnemyGroup{idx}");
        parent.tag = "EnemyGroup";
        Undo.RegisterCreatedObjectUndo(parent, "Create EnemyGroup");

        var group = Undo.AddComponent<EnemyCubeGroup>(parent);
        group.Cubes = new EnemyCube[0];

        var rb = Undo.AddComponent<Rigidbody>(parent);
        rb.useGravity = false;
        rb.freezeRotation = true;
        var cubesToGroup = _placedEnemyCubeMap.Values
            .Where(ec => ec != null && ec.transform.parent == null)
            .ToArray();
        if (cubesToGroup.Length > 0)
        {
            var b = new Bounds(cubesToGroup[0].transform.position, Vector3.zero);
            foreach (var ec in cubesToGroup) b.Encapsulate(ec.transform.position);
            b.Expand(new Vector3(group.cellSize, 0f, group.cellSize));

            var bc = Undo.AddComponent<BoxCollider>(parent);
            bc.center = parent.transform.InverseTransformPoint(b.center);
            bc.size = new Vector3(b.size.x, 1f, b.size.z);
            bc.isTrigger = true;

            foreach (var ec in cubesToGroup)
            {
                Undo.SetTransformParent(ec.transform, parent.transform, "Group Enemy Cubes");
                ArrayUtility.Add(ref group.Cubes, ec);
            }
        }

        _cubeMapDirty = true;
        RefreshEnemyCubeMap();
        Repaint();

        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void SetEnemyGroupMovement(EnemyCubeGroup group, bool horiz, bool vert, int dist)
    {
        if (group == null) return;

        // Record for undo
        Undo.RecordObject(group, "Set Enemy Group Movement");

        group.moveHorizontal = horiz;
        group.moveVertical = vert;
        group.moveCells = dist;

        EditorUtility.SetDirty(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Repaint();
    }

    private EnemyCubeGroup CreateTempGroupFor(EnemyCube ec)
    {
        var parentGO = new GameObject("EnemyCubeGroup");
        Undo.RegisterCreatedObjectUndo(parentGO, "Create Temp EnemyCubeGroup");
        var grp = Undo.AddComponent<EnemyCubeGroup>(parentGO);
        Undo.SetTransformParent(ec.transform, parentGO.transform, "Reparent to Temp Group");
        grp.Cubes = new[] { ec };
        return grp;
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

        var bg = Object.FindFirstObjectByType<GridBackground>();
        if (bg != null)
        {
            bg.UpdateVisuals();
            EditorUtility.SetDirty(bg);
        }
        else
        {
            Debug.LogWarning("[LevelEditor] No GridBackground component found!");
        }

        SceneView.RepaintAll();
#endif
    }

    private void DestroyAt(int row, int col)
    {
        var key = new Vector2Int(row, col);

        // ── 1) Remove any spawnable-config on this cell ─────────────
        var cfg = _level.SpwanablesConfigurations
            .FirstOrDefault(c => c.row == row && c.col == col);
        if (cfg != null)
        {
            _level.SpwanablesConfigurations.Remove(cfg);
            RefreshSpawnableMap();

#if UNITY_EDITOR
            // Mark dirty & repaint so the colored-cell overlay disappears
            UnityEditor.EditorUtility.SetDirty(_gameManager);
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
            Repaint();
            // continue on to also clear walls/enemies/etc.
        }

        // ── 2) Your existing wall/obstacle cleanup ──────────────────
        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;
        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;
        int flippedRow = _level.Rows - row - 1;
        Vector3 worldPos = new Vector3(col + offsetX, 0.5f, flippedRow + offsetZ);

        _level.gridCellPositions.RemoveAll(c => c.row == row && c.col == col && c.isObstacle);

        foreach (var obstacle in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            if (Vector3.Distance(obstacle.transform.position, worldPos) < 0.1f)
            {
#if UNITY_EDITOR
                DestroyImmediate(obstacle);
#else
            Destroy(obstacle);
#endif
                break;
            }
        }

        // ── 3) Preplaced-enemy cleanup ─────────────────────────────
        if (_placedEnemyMap.TryGetValue(key, out var preGo))
        {
#if UNITY_EDITOR
            DestroyImmediate(preGo);
#else
        Destroy(preGo);
#endif
            _placedEnemyMap.Remove(key);
        }

        // ── 4) Enemy-cube cleanup ──────────────────────────────────
        if (_placedEnemyCubeMap.TryGetValue(key, out var ec) && ec != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(ec.gameObject);
#else
        Destroy(ec.gameObject);
#endif
            _placedEnemyCubeMap.Remove(key);
        }
        else
        {
            foreach (var other in Object.FindObjectsOfType<EnemyCube>())
            {
                if (Vector3.Distance(other.transform.position, worldPos) < 0.1f)
                {
#if UNITY_EDITOR
                    DestroyImmediate(other.gameObject);
#else
                Destroy(other.gameObject);
#endif
                    _placedEnemyCubeMap.Remove(key);
                    break;
                }
            }
        }

        // ── 5) Final dirty + scene-dirty if desired ──────────────
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager
            .MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }


    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnFocus()
    {
        _cubeMapDirty = true;
        RefreshEnemyCubeMap();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
        _cubeMapDirty = true;
            RefreshEnemyCubeMap();
        }
    }

    private bool CanRightClickCell(int row, int col)
    {
        var key = new Vector2Int(row, col);

        bool isWall = _level.gridCellPositions
            .Any(c => c.row == row && c.col == col && c.isObstacle);

        bool isPreplaced = _placedEnemyMap.ContainsKey(key);
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);
        bool isPlayer = row == _level.PlayerStartRow && col == _level.PlayerStartCol;

        // guard against _cellSpawnMap being null
        bool isSpawnableCell = _cellSpawnMap != null
            && _cellSpawnMap.ContainsKey(key);

        return isCube
            || isSpawnableCell
            || (!isWall && !isPreplaced && !isPlayer);
    }


    // cache cell→config for fast lookups in DrawCell, right-click, etc.
    private Dictionary<Vector2Int, SpawnableConfig> _cellSpawnMap;

    private void RefreshSpawnableMap()
    {
        _cellSpawnMap = _level.SpwanablesConfigurations
            .Where(cfg => cfg != null)
            .ToDictionary(
                cfg => new Vector2Int(cfg.row, cfg.col),
                cfg => cfg
            );
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




}




internal class MoveDistancePopup : EditorWindow
{
    int _distance;
    EnemyCubeGroup _group;

    public static void Open(EnemyCubeGroup group, Rect buttonRect)
    {
        var wnd = CreateInstance<MoveDistancePopup>();
        wnd._group = group;
        wnd._distance = group.moveCells;
        wnd.minSize = new Vector2(180, 60);
        wnd.ShowAsDropDown(buttonRect, wnd.minSize);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Cells to move:", EditorStyles.boldLabel);
        _distance = EditorGUILayout.IntField(_distance);

        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OK"))
        {
            Undo.RecordObject(_group, "Set Move Distance");
            _group.moveCells = Mathf.Max(1, _distance);
            EditorUtility.SetDirty(_group);
            Close();
        }
        if (GUILayout.Button("Cancel"))
            Close();
        EditorGUILayout.EndHorizontal();
    }
}



internal class SpawnableCellPopup : EditorWindow
{
    private SpawnableConfig cfg;
    private Dictionary<SpawnablesType, List<GameObject>> _prefabMap;
    private Vector2 scroll;
    private readonly Vector2 size = new Vector2(280, 300);
    public static void Open(SpawnableConfig cfg,Dictionary<SpawnablesType, List<GameObject>> prefabMap,Rect buttonRect)
    {
        var wnd = CreateInstance<SpawnableCellPopup>();
        wnd.cfg = cfg;
        wnd._prefabMap = prefabMap;
        wnd.minSize = wnd.size;
        wnd.ShowAsDropDown(buttonRect, wnd.size);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(
            $"Cell ({cfg.row},{cfg.col}) → {cfg.enemyType}",
            EditorStyles.boldLabel
        );
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // ── Enemy Type & Prefab ────────────────────────────────────────
        cfg.enemyType = (SpawnablesType)EditorGUILayout.EnumPopup("Type", cfg.enemyType);
        if (_prefabMap != null
            && _prefabMap.TryGetValue(cfg.enemyType, out var variants)
            && variants.Count > 0)
        {
            var names = variants.ConvertAll(g => g.name).ToArray();
            int idx = Mathf.Max(0, variants.IndexOf(cfg.prefab));
            idx = EditorGUILayout.Popup("Prefab", idx, names);
            cfg.prefab = variants[idx];
        }
        else
        {
            cfg.prefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab", cfg.prefab, typeof(GameObject), false
            );
        }

        EditorGUILayout.Space();

        // ── Initial Progress Slider (0–100) ────────────────────────────
        float initialPct = cfg.progressThreshold * 100f;
        initialPct = EditorGUILayout.Slider("Progress (%)", initialPct, 0f, 100f);
        cfg.progressThreshold = initialPct * 0.01f;

        EditorGUILayout.Space();

        // ── Spawn Count ─────────────────────────────────────────────────
        cfg.spawnCount = Mathf.Max(1, EditorGUILayout.IntField("Count", cfg.spawnCount));

        // ── Subsequent Progress Sliders ────────────────────────────────
        if (cfg.spawnCount > 1)
        {
            int needed = cfg.spawnCount - 1;
            // ensure list length
            while (cfg.subsequentProgressThresholds.Count < needed)
                cfg.subsequentProgressThresholds.Add(cfg.progressThreshold);
            while (cfg.subsequentProgressThresholds.Count > needed)
                cfg.subsequentProgressThresholds.RemoveAt(cfg.subsequentProgressThresholds.Count - 1);

            EditorGUILayout.LabelField("Subsequent Progress (%)", EditorStyles.boldLabel);
            for (int i = 0; i < needed; i++)
            {
                float minPct = cfg.progressThreshold * 100f;
                float subPct = cfg.subsequentProgressThresholds[i] * 100f;
                subPct = EditorGUILayout.Slider($"After spawn #{i + 1}", subPct, minPct, 100f);
                cfg.subsequentProgressThresholds[i] = subPct * 0.01f;
            }
        }

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
            EditorUtility.SetDirty(FindFirstObjectByType<GameManager>());
    }

}
