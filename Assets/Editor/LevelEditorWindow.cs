
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDataEditorWindow : EditorWindow
{
    private GameManager _gameManager;
    private LevelData _level;

    private int _selectedTab = 0;
    private int _prevRows;
    private int _prevColumns;
    private const int MinGridSize = 8;
    private const int MaxGridSize = 50;

    private Dictionary<SpawnablesType, List<GameObject>> _enemyTypeToPrefabsMap = new Dictionary<SpawnablesType, List<GameObject>>();
    private Dictionary<GameObject, Color> _enemyTypeColors = new Dictionary<GameObject, Color>();
    private Dictionary<Vector2Int, SpawnableConfig> _cellSpawnMap;
    private Dictionary<Vector2Int, GameObject> _placedEnemyMap = new Dictionary<Vector2Int, GameObject>();
    private DrawMode _drawMode = DrawMode.Wall;
    private bool _cubeMapDirty = true;

    private string[] _tabs = new string[] { "General", "Grid" };
    private enum DrawMode { Wall, Obstacle, Empty, Enemy }
    public Dictionary<SpawnablesType, List<GameObject>> EnemyTypeToPrefabsMap => _enemyTypeToPrefabsMap;

    private Vector2 scrollPosition = Vector2.zero;

    private Dictionary<Vector2Int, EnemyCube> _placedEnemyCubeMap = new Dictionary<Vector2Int, EnemyCube>();

    [MenuItem("Window/Level Editor Window")]
    public static void OpenWindow()
    {
        var wnd = GetWindow<LevelDataEditorWindow>();
        wnd.titleContent = new GUIContent("Level Editor");
        wnd.Show();
    }
    private double _refreshDelayTime = 1.5f;
    private void OnEnable()
    {
        _gameManager = Object.FindFirstObjectByType<GameManager>();
        if (_gameManager != null)
            _level = _gameManager.Level;

        if (_level != null)
        {
            if (_level.SpwanablesConfigurations == null)
                _level.SpwanablesConfigurations = new List<SpawnableConfig>();

            EnsureSpawnableSceneNames();
        }

        RefreshSpawnableMap();


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
                    //Debug.LogWarning($"Prefab name '{prefab.name}' does not match any SpawnablesType enum.");
                }
            }
        }

        _enemyTypeToPrefabsMap[SpawnablesType.Pickups] = new List<GameObject>
    {
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/DIAMOND.prefab"),
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/SlowDown.prefab"),
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/Timer.prefab"),
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/Heart.prefab"),
    };

        if (_level != null)
        {
            // 🧠 Only place boundary walls if the grid is actually empty
            if (_level.gridCellPositions == null)
                _level.gridCellPositions = new List<CubeCell>();

            if (_level.gridCellPositions.Count == 0)
            {
                //Debug.Log("Grid was empty. Placing boundary walls.");
                PlaceBoundaryWalls();
            }
            else
            {
                //Debug.Log("Grid already populated. Skipping boundary wall placement.");
            }
            RefreshEnemyCubeMap();
            _drawMode = DrawMode.Empty;
            RefreshPreplacedEnemyMap();

            // ✅ Set previous size after everything is loaded
            _prevColumns = _level.Columns;
            _prevRows = _level.Rows;
        }
        else
        {
            //Debug.LogWarning("⚠️ GameManager.Level is null. Level editor not initialized.");
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        _cubeMapDirty = true;

        
    }

    private void WaitAndRefreshAll()
    {
        if (EditorApplication.timeSinceStartup >= _refreshDelayTime)
        {
            EditorApplication.update -= WaitAndRefreshAll;
            RefreshAll();
        }
    }
    private void EnsureSpawnableSceneNames()
    {
        if (_level.SpwanablesConfigurations == null)
            _level.SpwanablesConfigurations = new List<SpawnableConfig>();

        string active = SceneManager.GetActiveScene().name;
        bool changed = false;

        foreach (var cfg in _level.SpwanablesConfigurations)
        {
            if (cfg == null) continue;

            // Backward-compat: if missing, stamp with current scene
            if (string.IsNullOrEmpty(cfg.sceneName))
            {
                cfg.sceneName = active;
                changed = true;
            }
        }

#if UNITY_EDITOR
        if (changed)
        {
            EditorUtility.SetDirty(_gameManager);
            EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
        }
#endif
    }


    private void OnGUI()
    {
        if (_gameManager == null)
        {
            EditorGUILayout.HelpBox("There is no GameManager in the scene.\nPlease add a GameManager GameObject with a public LevelData Level field.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
            {
                _gameManager = FindFirstObjectByType<GameManager>();
                RefreshAll();
                _refreshDelayTime = EditorApplication.timeSinceStartup + 1f; // 1 second later
                EditorApplication.update += WaitAndRefreshAll;
                //RefreshSpawnableMap();
            }

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
        }

        //if (GUILayout.Button("💾 Backup Level to JSON File"))
        //{
        //    string path = EditorUtility.SaveFilePanel("Save Level JSON", "", "LevelBackup.json", "json");
        //    if (!string.IsNullOrEmpty(path))
        //    {
        //        string json = JsonUtility.ToJson(_level, true);
        //        System.IO.File.WriteAllText(path, json);
        //        Debug.Log($"Level backed up to: {path}");
        //    }
        //}


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

            _level.isTimeless = EditorGUILayout.ToggleLeft("Timeless Level (no timer)", _level.isTimeless);

            EditorGUI.BeginDisabledGroup(_level.isTimeless);
            _level.levelTime = EditorGUILayout.FloatField(
                new GUIContent("Level Time (seconds)"),
                _level.levelTime
            );
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("Preplaced Prefabs:", EditorStyles.miniBoldLabel);
            if (_level.PreplacedEnemies == null)
                _level.PreplacedEnemies = new List<PreplacedEnemy>();

            for (int i = 0; i < _level.PreplacedEnemies.Count; i++)
            {
                var entry = _level.PreplacedEnemies[i];
                EditorGUILayout.BeginHorizontal();

                entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Prefab #{i + 1}",
                    entry.prefab,
                    typeof(GameObject),
                    false
                );

                entry.row = EditorGUILayout.IntField(entry.row, GUILayout.Width(40));
                entry.col = EditorGUILayout.IntField(entry.col, GUILayout.Width(40));

                if (GUILayout.Button("✕", GUILayout.Width(20)))
                {
                    _level.PreplacedEnemies.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Settings", EditorStyles.boldLabel);

            if (_gameManager != null && _gameManager.Player != null)
            {
                _gameManager.Player.moveSpeed = EditorGUILayout.FloatField("Player Move Speed", _gameManager.Player.moveSpeed);
                EditorUtility.SetDirty(_gameManager.Player);
            }
            else
            {
                EditorGUILayout.HelpBox("Player reference is missing in GameManager.", MessageType.Warning);
            }

            if (_gameManager != null && _gameManager.Camera != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);

                _level.useAutoCameraPositioning = EditorGUILayout.ToggleLeft("Auto Camera", _level.useAutoCameraPositioning);

                Camera cam = _gameManager.Camera.GetComponent<Camera>();
                if (cam != null)
                    cam.orthographic = true; // ✅ Use orthographic mode

                EditorGUI.BeginDisabledGroup(_level.useAutoCameraPositioning);

                float newY = EditorGUILayout.FloatField("Camera Y Position", _level.cameraYPosition);
                float newZ = EditorGUILayout.FloatField("Camera Z Position", _level.cameraZPosition);
                float newSize = EditorGUILayout.FloatField("Orthographic Size", _level.zoomSize);

                EditorGUI.EndDisabledGroup();

                if (!_level.useAutoCameraPositioning)
                {
                    if (!Mathf.Approximately(newY, _level.cameraYPosition))
                    {
                        _level.cameraYPosition = newY;
                        Vector3 pos = _gameManager.Camera.position;
                        pos.y = newY;
                        _gameManager.Camera.position = pos;
                        EditorUtility.SetDirty(_gameManager.Camera.gameObject);
                    }

                    if (!Mathf.Approximately(newZ, _level.cameraZPosition))
                    {
                        _level.cameraZPosition = newZ;
                        Vector3 pos = _gameManager.Camera.position;
                        pos.z = newZ;
                        _gameManager.Camera.position = pos;
                        EditorUtility.SetDirty(_gameManager.Camera.gameObject);
                    }

                    if (!Mathf.Approximately(newSize, _level.zoomSize) && cam != null)
                    {
                        _level.zoomSize = newSize;
                        cam.orthographicSize = newSize;
                        EditorUtility.SetDirty(cam);
                    }
                }
                else
                {
//                    var cam1 = Camera.main ? Camera.main : _gameManager.Camera.GetComponent<Camera>();
//                    if (cam1 == null)
//                    {
//                        Debug.LogWarning("No camera found for auto-positioning.");
//                        return;
//                    }

                    //                    cam1.orthographic = true;

                    //                    const float cubeSize = 1f;   // default Unity cube
                    //                    const float sidePaddingCubes = 0.5f; // tweak for more/less gap

                    //                    float aspect = (float)Screen.safeArea.width / Screen.safeArea.height;
                    //                    float halfWorldWide = (_level.Columns * cubeSize * 0.5f) + (sidePaddingCubes * cubeSize);
                    //                    float autoSize = halfWorldWide / aspect;

                    //                    _level.zoomSize = autoSize;
                    //                    cam1.orthographicSize = autoSize;
                    //                    Debug.Log("a");
                    //                    // Y / Z offsets (optional)
                    //                    float yPos =cam1.transform.position.y;
                    //                    float zPos =cam1.transform.position.z;
                    //                    if (ColumnToCamOrtho.TryGetValue(_level.Columns, out var p))
                    //                    {
                    //                        yPos = p.y;
                    //                        zPos = p.z;
                    //                    }
                    //                    else
                    //                    {
                    //                        Debug.LogWarning($"No Y/Z camera offsets defined for column count {_level.Columns}");
                    //                    }

                    //                    Vector3 camPos = _gameManager.Camera.position;
                    //                    camPos.y = yPos;
                    //                    camPos.z = zPos;
                    //                    _gameManager.Camera.position = camPos;

                    //#if UNITY_EDITOR
                    //                    EditorUtility.SetDirty(cam);
                    //                    EditorUtility.SetDirty(_gameManager.Camera.gameObject);
                    //#endif
                }

            }


            if (GUI.changed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(_gameManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
#endif
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);

            _gameManager.WallColor = EditorGUILayout.ColorField("Wall Color", _gameManager.WallColor);
            _gameManager.PlayerColor = EditorGUILayout.ColorField("Player Color", _gameManager.PlayerColor);
            _gameManager.CubeFillColor = EditorGUILayout.ColorField("Filler Cube Color", _gameManager.CubeFillColor);
            _gameManager.BackgroundColor = EditorGUILayout.ColorField("Background Color", _gameManager.BackgroundColor);
            _gameManager.EnemyCubeColor = EditorGUILayout.ColorField("Enemy Cube Color", _gameManager.EnemyCubeColor);

            if (GUI.changed)
            {
                ApplyEditorColors();
                EditorUtility.SetDirty(_gameManager);
                EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
            }

            EditorGUILayout.EndVertical();
        }
    }

    // ─────────────────────────────────────────────────────────
    // Tab 1: Grid (Columns, Rows, simple grid info)
    // ─────────────────────────────────────────────────────────

    void RefreshAll()
    {
        // Force a full resync/redraw right now
        RefreshEnemyCubeMap();
        RefreshPreplacedEnemyMap();
        SyncSceneGridAndBackground();
        ApplyEditorColors();
        RefreshSpawnableMap();
        SetCamera();
        Repaint();
    }
    private void DrawGridTab()
    {
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // ── Quick Actions ────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(90)))
        {
            RefreshAll();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        // ─────────────────────────────────────────────────────────────────

        DrawGridSizeControls();
        DrawFillClearButtons();
        DrawEnemyGroupButton();
        DrawSelectionTypePopup();
        DrawGridCells();
        SetCamera();

        EditorGUILayout.EndVertical();
    }


    public void SetCamera()
    {
        if (_level.useAutoCameraPositioning)
        {
            var cam = Camera.main ? Camera.main : _gameManager.Camera.GetComponent<Camera>();
            if (cam == null)
            {
                //Debug.LogWarning("No camera found for auto-positioning.");
                return;
            }

            //Debug.Log("Camera Updated");
            cam.orthographic = true;


            //Debug.Log(Screen.safeArea.width + " X" + Screen.safeArea.height);

            float aspect = Screen.safeArea.width / Screen.safeArea.height;
            //Debug.Log(aspect);
            float halfWorldWidth = (_level.Columns * _level.cubeSize * 0.5f)  // half grid width
                                 + (_level.sidePaddingCubes * _level.cubeSize);

            cam.orthographicSize = halfWorldWidth / aspect;

            //Y / Z offsets remain table - driven(ignore X completely)
            if (ColumnToCamOrtho.TryGetValue(_level.Columns, out var posData))
            {
                Vector3 pos = cam.transform.position;
                pos.y = posData.y;
                pos.z = posData.z;
                cam.transform.position = pos;

                _level.cameraYPosition = pos.y;
                _level.cameraZPosition = pos.z;
            }
            else
            {
                //Debug.LogWarning($"No Y/Z camera offsets defined for column count {_level.Columns}");
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(cam);
            if (_gameManager.Camera) UnityEditor.EditorUtility.SetDirty(_gameManager.Camera.gameObject);
#endif
        }
        
    }

    private void DrawGridSizeControls()
    {
        int cols = EditorGUILayout.IntField("Columns", _level.Columns);
        int rows = EditorGUILayout.IntField("Rows", _level.Rows);
        _level.cubeSize = EditorGUILayout.FloatField("cubeSize", _level.cubeSize);
        _level.sidePaddingCubes = EditorGUILayout.FloatField("sidePadding", _level.sidePaddingCubes);


        _level.cubeSize = Mathf.Clamp(_level.cubeSize, 0, 10);
        _level.sidePaddingCubes = Mathf.Clamp(_level.sidePaddingCubes, 0, 10);

        cols = Mathf.Clamp(cols, MinGridSize, MaxGridSize);
        rows = Mathf.Clamp(rows, MinGridSize, MaxGridSize + 30);

        if (cols % 2 != 0) cols++;
        if (rows % 2 != 0) rows++;

        bool sizeChanged = (cols != _level.Columns || rows != _level.Rows);

        if (sizeChanged)
        {
            _level.Columns = cols;
            _level.Rows = rows;

            ClearAllCubes();
            PlaceBoundaryWalls();
            RefreshEnemyCubeMap();
            RefreshSpawnableMap();
            UpdateCubeMaterialTiling(cols, rows);
            SyncSceneGridAndBackground();

            _prevColumns = cols;
            _prevRows = rows;

            EditorUtility.SetDirty(_gameManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        //SetCamera();

        SyncSceneGridAndBackground();
    }




    private void UpdateCubeMaterialTiling(int cols, int rows)
    {
        foreach (var cube in FindObjectsOfType<Cube>())
        {
            cube.SetTiling(cols, rows);
        }
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
        string[] modes = { "Wall", "Obstacle", "Empty", "Enemy" };
        _drawMode = (DrawMode)EditorGUILayout.Popup("Select Type", (int)_drawMode, modes);
    }

    private void DrawGridCells()
    {
        const float viewSize = 650f;
        float cellSize = Mathf.Min(
            viewSize / _level.Columns,
            viewSize / _level.Rows
        );

        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Width(viewSize),
            GUILayout.Height(viewSize)
        );
        GUILayout.Space(10);

        var e = Event.current;
        bool hasPlayer = _level.PlayerStartRow >= 0 && _level.PlayerStartCol >= 0;
        EnsureEnemyTypeColors();

        for (int row = 0; row < _level.Rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < _level.Columns; col++)
                DrawCell(row, col, e, hasPlayer, cellSize);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(50);
    }

    private void EnsureEnemyTypeColors()
    {
        if (_enemyTypeColors == null)
            _enemyTypeColors = new Dictionary<GameObject, Color>();

        foreach (var go in _placedEnemyMap.Values.Distinct())
        {
            if (!_enemyTypeColors.ContainsKey(go))
                _enemyTypeColors[go] = Color.HSVToRGB(
                    (_enemyTypeColors.Count * .618f) % 1f,
                    .6f,
                    .8f
                );
        }
    }

    private void DrawCell(int row, int col, Event e, bool hasPlayer, float cellSize)
    {
        var key = new Vector2Int(row, col);
        bool isWall = _level.gridCellPositions.Any(c => c.row == row && c.col == col && c.type == CellType.Wall);
        bool isObstacleType = _level.gridCellPositions.Any(c => c.row == row && c.col == col && c.type == CellType.Obstacle);
        bool isPre = _placedEnemyMap.ContainsKey(key);
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);
        bool isPlayer = hasPlayer && row == _level.PlayerStartRow && col == _level.PlayerStartCol;
        bool isSpawn = _cellSpawnMap != null && _cellSpawnMap.ContainsKey(key);
        bool occupied = isWall || isObstacleType || isPre || isCube || isSpawn;

        bool shouldPaint = (_drawMode == DrawMode.Wall && !occupied) || (_drawMode == DrawMode.Obstacle && !occupied) || (_drawMode == DrawMode.Enemy && !occupied) || (_drawMode == DrawMode.Empty && occupied);


        // 1) draw box
        Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize, GUILayout.Width(cellSize), GUILayout.Height(cellSize));

        DrawCellBackground(cellRect, key, isPre, isCube, isPlayer);
        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black);

        if (isCube) DrawMovementArrow(cellRect, key);

        if (cellRect.Contains(e.mousePosition) &&
         (e.type == EventType.ContextClick || (e.type == EventType.MouseDown && e.button == 1)))
        {
            if (CanRightClickCell(row, col))
            {
                ShowCellContextMenu(cellRect, key, isWall, isPre, occupied, row, col);
                e.Use();
            }
        }

        if (shouldPaint
            && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            && e.button == 0
            && cellRect.Contains(e.mousePosition))
        {
            if (_drawMode == DrawMode.Wall)
                HandleCellClick(row, col, _level.wallPrefab);
            else if (_drawMode == DrawMode.Obstacle)
                HandleCellClick(row, col, _level.obstacle);

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
            {
                PlaceEnemyCube(row, col, key);
                ApplyEditorColors();
            }


            Repaint();
            e.Use();
        }
    }

    private void DrawCellBackground(Rect r, Vector2Int key, bool pre, bool cube, bool player)
    {
        var cellEntry = _level.gridCellPositions
            .FirstOrDefault(c => c.row == key.x && c.col == key.y);

        Color baseColor;

        if (player)
            baseColor = Color.green;

        else if (cube)
            baseColor = Color.red;

        else if (pre && _placedEnemyMap.TryGetValue(key, out var obj))
        {
            if (!_enemyTypeColors.TryGetValue(obj, out baseColor))
            {
                baseColor = Color.HSVToRGB(
                    (_enemyTypeColors.Count * .618f) % 1f,
                    0.6f,
                    0.8f
                );
                _enemyTypeColors[obj] = baseColor;
            }
        }
        else if (cellEntry != null)
        {
            baseColor = cellEntry.type == CellType.Wall
                ? Color.gray
                : new Color(1f, 0.5f, 0f);
        }
        else
            baseColor = Color.white;

        EditorGUI.DrawRect(r, baseColor);

        if (_cellSpawnMap != null && _cellSpawnMap.TryGetValue(key, out var cfg))
        {
            bool drawForThisScene =
                string.IsNullOrEmpty(cfg.sceneName) ||
                cfg.sceneName == SceneManager.GetActiveScene().name;

            if (drawForThisScene)
            {
                var overlay = cfg.enemyType == SpawnablesType.SpikeBall
                    ? new Color(1f, 1f, 0f, 0.3f)
                    : new Color(0f, 1f, 1f, 0.5f);

                EditorGUI.DrawRect(r, overlay);
            }
        }
    }

    private void DrawMovementArrow(Rect r, Vector2Int key)
    {
        if (!_placedEnemyCubeMap.TryGetValue(key, out var ec) || ec == null)
            return;

        var t = ec.transform;
        if (t == null)
            return;

        var group = t.parent?.GetComponent<EnemyCubeGroup>();
        if (group == null)
            return;

        string arrow = group.moveDirection switch
        {
            MoveDirection.Up => "↑",
            MoveDirection.Down => "↓",
            MoveDirection.Left => "←",
            MoveDirection.Right => "→",
            _ => ""
        };

        GUI.Label(r, arrow, EditorStyles.boldLabel);

    }

    private void ShowCellContextMenu(Rect r, Vector2Int key, bool isWall, bool isPre, bool occupied, int row, int col)
    {
        var menu = new GenericMenu();
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);

        var screenMouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        var popupRect = new Rect(screenMouse.x, screenMouse.y, 1, 1);

        if (isPre)
        {
            var go = _placedEnemyMap[key];
            var screenMouse1 = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            var popupRect1 = new Rect(screenMouse1.x, screenMouse1.y, 1, 1);
            PreplacedSpeedPopup.Open(go, popupRect1);
            return;
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
            menu.AddSeparator("Spawnable/");
            if (_cellSpawnMap != null && _cellSpawnMap.TryGetValue(key, out var cellCfg))
            {
                SpawnableCellPopup.Open(cellCfg, _enemyTypeToPrefabsMap, new Rect(screenMouse.x, screenMouse.y, 1, 1));
                return;
            }
            else
            {
                menu.AddSeparator("Spawnable/");

                string[] pickupVariants = { "Timer", "SlowDown", "DIAMOND", "Heart" };
                menu.AddSeparator("Spawnable/Pickups/");
                foreach (var variant in pickupVariants)
                {
                    string path = $"Assets/Prefabs/Spawnables/{variant}.prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    menu.AddItem(
                        new GUIContent($"Spawnable/Pickups/{variant}"),
                        false,
                        () =>
                        {
                            var cfg = new SpawnableConfig
                            {
                                enemyType = SpawnablesType.Pickups,
                                prefab = GetRandomPrefabVariant(prefab),
                                row = row,
                                col = col,
                                spawnCount = 1,
                                progressThreshold = 0.5f,
                                subsequentProgressThresholds = new List<float>(),
                                usePhysicsDrop = true,
                                yOffset = 1.4f,
                                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                            };
                            _level.SpwanablesConfigurations.Add(cfg);
                            RefreshSpawnableMap();
                            EditorUtility.SetDirty(_gameManager);

                            SpawnableCellPopup.Open(
                                cfg,
                                _enemyTypeToPrefabsMap,
                                new Rect(screenMouse.x, screenMouse.y, 1, 1)
                            );
                        }
                    );

                }

                foreach (SpawnablesType t in System.Enum.GetValues(typeof(SpawnablesType)))
                {
                    if (t == SpawnablesType.Pickups)
                        continue;

                    string name = t.ToString();
                    string path = $"Assets/Prefabs/Spawnables/{name}.prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    float yOffset;
                    bool useFall = false;
                    switch (t)
                    {
                        case SpawnablesType.FlyingHoop:
                            yOffset = 2.4f; useFall = true; break;
                        case SpawnablesType.SpikeBall:
                            yOffset = 1.7f; useFall = true; break;
                        default:
                            yOffset = 0f; useFall = false; break;
                    }

                    menu.AddItem(
                        new GUIContent($"Spawnable/{name}"),
                        false,
                        () =>
                        {
                            var cfg = new SpawnableConfig
                            {
                                enemyType = t,
                                prefab = (t == SpawnablesType.CubeEater) ? prefab : GetRandomPrefabVariant(prefab),
                                row = row,
                                col = col,
                                spawnCount = 1,
                                progressThreshold = 0.5f,
                                subsequentProgressThresholds = new List<float>(),
                                usePhysicsDrop = useFall,
                                yOffset = yOffset,
                                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                            };
                            _level.SpwanablesConfigurations.Add(cfg);
                            RefreshSpawnableMap();
                            EditorUtility.SetDirty(_gameManager);

                            SpawnableCellPopup.Open(
                                cfg,
                                _enemyTypeToPrefabsMap,
                                new Rect(screenMouse.x, screenMouse.y, 1, 1)
                            );
                        }
                    );
                    menu.AddItem(
    new GUIContent($"Spawnable/{name}"),
    false,
    () =>
    {
        // Load the prefab (ensure the path is correct)
        GameObject newSpawnablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spawnables/NewSpawnablePrefab.prefab");

        // Create the SpawnableConfig
        var cfg = new SpawnableConfig
        {
            enemyType = SpawnablesType.RotatingMine,
            prefab = newSpawnablePrefab,
            row = row,
            col = col,
            spawnCount = 1,
            progressThreshold = 0.5f,
            subsequentProgressThresholds = new List<float>(),
            usePhysicsDrop = true,
            yOffset = 1.4f,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        _level.SpwanablesConfigurations.Add(cfg);

        RefreshSpawnableMap();
        EditorUtility.SetDirty(_gameManager);

        SpawnableCellPopup.Open(
            cfg,
            _enemyTypeToPrefabsMap,
            new Rect(screenMouse.x, screenMouse.y, 1, 1)
        );
    }
);

                }
            }
        }

        menu.ShowAsContext();
    }

    private GameObject GetRandomPrefabVariant(GameObject originalPrefab)
    {

        if (_gameManager == null || _gameManager.enemyVariantGroups == null || originalPrefab == null)
            return originalPrefab;

        string baseName = originalPrefab.name.Split('_')[0];

        if (System.Enum.TryParse<SpawnablesType>(baseName, true, out var spawnType))
        {
            var match = _gameManager.enemyVariantGroups.FirstOrDefault(g => g.type == spawnType);
            if (match != null && match.variants.Count > 0)
                return match.variants[Random.Range(0, match.variants.Count)];
        }

        return originalPrefab;
    }

    private void AddCubeContextItems(GenericMenu menu, Vector2Int key)
    {
        var ec = _placedEnemyCubeMap[key];
        var group = ec.transform.parent?.GetComponent<EnemyCubeGroup>()
                  ?? CreateTempGroupFor(ec);

        var dir = group.moveDirection;

        menu.AddItem(new GUIContent("Movement/Static"), dir == MoveDirection.None, () =>
    SetEnemyGroupDirection(group, MoveDirection.None));
        menu.AddSeparator("Movement/");
        menu.AddItem(new GUIContent("Movement/Right"), dir == MoveDirection.Right, () =>
            SetEnemyGroupDirection(group, MoveDirection.Right));
        menu.AddItem(new GUIContent("Movement/Left"), dir == MoveDirection.Left, () =>
            SetEnemyGroupDirection(group, MoveDirection.Left));
        menu.AddItem(new GUIContent("Movement/Up"), dir == MoveDirection.Up, () =>
            SetEnemyGroupDirection(group, MoveDirection.Up));
        menu.AddItem(new GUIContent("Movement/Down"), dir == MoveDirection.Down, () =>
            SetEnemyGroupDirection(group, MoveDirection.Down));



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
    }
    private void SetEnemyGroupDirection(EnemyCubeGroup group, MoveDirection dir)
    {
        Undo.RecordObject(group, "Change Movement Direction");
        group.moveDirection = dir;
        EditorUtility.SetDirty(group);
        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
        Repaint();
    }

    private void AddPreplacedSpawnItems(GenericMenu menu, int row, int col)
    {
        if (_level.PreplacedEnemies?.Count > 0)
        {
            foreach (var entry in _level.PreplacedEnemies)
            {
                if (entry.prefab == null) continue;
                int r1 = row, c = col;
                GameObject prefab = entry.prefab;
                menu.AddItem(new GUIContent($"Preplaced Enemy/{prefab.name}"), false, () =>
                {
                    var gm = FindFirstObjectByType<GridManager>();
                    var wp = gm.GridToWorld(new Vector2Int(c, r1));
                    wp.y = prefab.transform.position.y;
                    var variant = GetRandomPrefabVariant(prefab);
                    var go = Object.Instantiate(variant, wp, Quaternion.identity);
                    go.name = prefab.name;
                    go.transform.position = wp;
                    go.AddComponent<PreplacedMarker>();
                    Vector2Int key = new(r1, c);
                    if (!_placedEnemyMap.ContainsKey(key))
                        _placedEnemyMap[key] = go;

                    RefreshPreplacedEnemyMap(); // force update
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

        var go = Object.Instantiate(prefab, wp, prefab.transform.rotation);
        go.transform.position = wp;

        _placedEnemyCubeMap[key] = go.GetComponent<EnemyCube>();
    }

    // ─────────────────────────────────────────────────────────
    // Draw Grid Tab Ends Here 
    // ─────────────────────────────────────────────────────────

    private void PlaceBoundaryWalls()
    {
        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;
        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;

        for (int row = 0; row < _level.Rows; row++)
        {
            for (int col = 0; col < _level.Columns; col++)
            {
                bool isEdge = row == 0 || row == _level.Rows - 1 || col == 0 || col == _level.Columns - 1;
                if (!isEdge) continue;

                // Avoid duplicates
                if (_level.gridCellPositions.Any(c => c.row == row && c.col == col && c.type == CellType.Wall))
                    continue;

                _level.gridCellPositions.Add(new CubeCell { row = row, col = col, type = CellType.Wall });

                Vector3 spawnPosition = new Vector3(col + offsetX, 0.5f, (_level.Rows - row - 1) + offsetZ);
                var go = (GameObject)Object.Instantiate(_level.wallPrefab, spawnPosition, Quaternion.identity);
                if (_gameManager._wallParent != null) go.transform.SetParent(_gameManager._wallParent.transform);

                go.tag = "Boundary";
            }
        }
    }

    private void OnInspectorUpdate()
    {
        if (_cubeMapDirty)
        {
            RefreshEnemyCubeMap();
            _cubeMapDirty = false;
            Repaint();
        }
    }

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
    private void RefreshPreplacedEnemyMap()
    {
        if (_placedEnemyMap == null)
            _placedEnemyMap = new Dictionary<Vector2Int, GameObject>();
        else
            _placedEnemyMap.Clear();

        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        foreach (var marker in Object.FindObjectsByType<PreplacedMarker>(FindObjectsSortMode.None))
        {
            if (marker == null || marker.gameObject == null) continue;

            Vector3 pos = marker.transform.position;
            Vector2Int grid = gm.WorldToGrid(pos);
            var key = new Vector2Int(grid.y, grid.x);

            if (!_placedEnemyMap.ContainsKey(key))
                _placedEnemyMap[key] = marker.gameObject;

            if (!_enemyTypeColors.ContainsKey(marker.gameObject))
            {
                _enemyTypeColors[marker.gameObject] = Color.HSVToRGB(
                    (_enemyTypeColors.Count * .618f) % 1f,
                    0.6f,
                    0.8f
                );
            }
        }

        Repaint();
    }

    private void HandleCellClick(int row, int col, GameObject prefab)
    {
        float baseOffsetX = -(_level.Columns - 1) * 0.5f;
        float baseOffsetZ = -(_level.Rows - 1) * 0.5f;
        float offsetX = baseOffsetX - 0.5f;
        float offsetZ = baseOffsetZ + 0.5f;
        int flippedRow = _level.Rows - row - 1;
        Vector3 spawnPosition = new Vector3(col + offsetX, 0.5f, flippedRow + offsetZ);

        var cellType = _drawMode == DrawMode.Wall ? CellType.Wall : CellType.Obstacle;
        _level.gridCellPositions.Add(new CubeCell { row = row, col = col, type = cellType });

        var go = (GameObject)Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (_gameManager._wallParent != null) go.transform.SetParent(_gameManager._wallParent.transform);

        //go.transform.position = spawnPosition;
        go.tag = (cellType == CellType.Wall) ? "Boundary" : "Obstacle";
        Undo.RegisterCreatedObjectUndo(go, $"Place {cellType}");
        EditorUtility.SetDirty(_gameManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
                if (!_level.gridCellPositions.Exists(c => c.row == row && c.col == col && c.type == CellType.Wall))
                {
                    _level.gridCellPositions.Add(new CubeCell { row = row, col = col, type = CellType.Wall });

                    Vector3 spawnPosition = new Vector3(col + offsetX, 0.5f, (_level.Rows - row - 1) + offsetZ);
                    var go = Instantiate(_level.wallPrefab, spawnPosition, Quaternion.identity);
                    if (_gameManager._wallParent != null) go.transform.SetParent(_gameManager._wallParent.transform);

                }
            }
        }
        EditorUtility.SetDirty(_gameManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_gameManager.gameObject.scene);
    }

    private void ClearAllCubes()
    {
        string json = JsonUtility.ToJson(_level);
        System.IO.File.WriteAllText("LastLevelBackup.json", json);
        // 1) Clear grid data
        _level.gridCellPositions.Clear();

        // 2) Destroy all walls & obstacles
        foreach (var tag in new[] { "Boundary", "Obstacle" })
            foreach (var go in GameObject.FindGameObjectsWithTag(tag))
                DestroyImmediate(go);

        foreach (var kvp in _placedEnemyMap.ToList())
            DestroyImmediate(kvp.Value);
        _placedEnemyMap.Clear();

        foreach (var kvp in _placedEnemyCubeMap.ToList())
            DestroyImmediate(kvp.Value.gameObject);
        _placedEnemyCubeMap.Clear();

        foreach (var grp in Object.FindObjectsOfType<EnemyCubeGroup>())
            DestroyImmediate(grp.gameObject);

        _level.SpwanablesConfigurations.Clear();
        RefreshSpawnableMap();

        _cubeMapDirty = true;
        Repaint();
#if UNITY_EDITOR
        EditorUtility.SetDirty(_gameManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
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
            bc.size = new Vector3(b.size.x-0.2f, 1f, b.size.z - 0.2f);
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
    

    //private void SetEnemyGroupMovement(EnemyCubeGroup group, bool horiz, bool vert, int dist)
    //{
    //    if (group == null) return;

    //    // Record for undo
    //    Undo.RecordObject(group, "Set Enemy Group Movement");

    //    group.moveHorizontal = horiz;
    //    group.moveVertical = vert;
    //    group.moveCells = dist;
    //    group.isStatic = !horiz && !vert;
    //    EditorUtility.SetDirty(group);
    //    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    //    Repaint();
    //}

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
            //Debug.LogWarning("[LevelEditor] No GridManager found in scene to sync with!");
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
            //Debug.LogWarning("[LevelEditor] No GridBackground component found!");
        }

        SceneView.RepaintAll();
#endif
    }

    private void DestroyAt(int row, int col)
    {
        var key = new Vector2Int(row, col);

        var cfg = _level.SpwanablesConfigurations
            .FirstOrDefault(c => c.row == row && c.col == col);
        if (cfg != null)
        {
            _level.SpwanablesConfigurations.Remove(cfg);
            RefreshSpawnableMap();
        }

        var gm = Object.FindFirstObjectByType<GridManager>();
        Vector3 worldPos = Vector3.zero;
        if (gm != null)
        {
            worldPos = gm.GridToWorld(new Vector2Int(col, row));
            worldPos.y = 0.5f; // match your spawn‐height
        }
        else
        {
            float baseX = -(_level.Columns - 1) * .5f - .5f;
            float baseZ = -(_level.Rows - 1) * .5f + .5f;
            int flip = _level.Rows - row - 1;
            worldPos = new Vector3(col + baseX, 0.5f, flip + baseZ);
        }

        _level.gridCellPositions.RemoveAll(c =>
            c.row == row && c.col == col &&
            (c.type == CellType.Wall || c.type == CellType.Obstacle)
        );

        foreach (var obj in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            if (Vector3.Distance(obj.transform.position, worldPos) < 0.1f)
            {
                DestroyImmediate(obj);
                break;
            }
        }

        foreach (var obj in GameObject.FindGameObjectsWithTag("Boundary"))
        {
            if (Vector3.Distance(obj.transform.position, worldPos) < 0.1f)
            {
                DestroyImmediate(obj);
                break;
            }
        }

        if (_placedEnemyMap.TryGetValue(key, out var preGo) && preGo != null)
        {
            DestroyImmediate(preGo);
            _placedEnemyMap.Remove(key);
        }

        if (_placedEnemyCubeMap.TryGetValue(key, out var cube))
        {
            Transform parent = cube.transform.parent;
            DestroyImmediate(cube.gameObject);
            _placedEnemyCubeMap.Remove(key);

            if (parent != null && parent.GetComponentsInChildren<EnemyCube>().Length == 0)
            {
                _level.EnemyGroups.RemoveAll(g => g != null && g.gameObject == parent.gameObject);
                DestroyImmediate(parent.gameObject);
            }
        }
        else
        {
            foreach (var other in Object.FindObjectsOfType<EnemyCube>())
            {
                if (Vector3.Distance(other.transform.position, worldPos) < 0.1f)
                {
                    DestroyImmediate(other.gameObject);
                    _placedEnemyCubeMap.Remove(key);
                    break;
                }
            }
        }

        RefreshSpawnableMap();
        _cubeMapDirty = true;
        Repaint();
#if UNITY_EDITOR
        EditorUtility.SetDirty(_gameManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
        RefreshPreplacedEnemyMap();
        RefreshSpawnableMap();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            _cubeMapDirty = true;
            RefreshEnemyCubeMap();
            RefreshPreplacedEnemyMap();
            RefreshSpawnableMap();
        }
    }

    private bool CanRightClickCell(int row, int col)
    {
        Vector2Int key = new(row, col);

        bool isWall = _level.gridCellPositions.Any(c => c.row == row && c.col == col && c.type == CellType.Wall);
        bool isSpawnableCell = _cellSpawnMap.ContainsKey(key);
        bool isPre = _placedEnemyMap.ContainsKey(key);
        bool isCube = _placedEnemyCubeMap.ContainsKey(key);
        bool isPlayer = row == _level.PlayerStartRow && col == _level.PlayerStartCol;

        if (isCube)
        {
            var ec = _placedEnemyCubeMap[key];
            if (ec.transform.parent == null || ec.transform.parent.GetComponent<EnemyCubeGroup>() == null)
                return false; 
        }

        if (isPre || isCube || isSpawnableCell)
            return true;

        if (isWall || isPlayer)
            return false;

        return true;
    }


    private void RefreshSpawnableMap()
    {
        _cellSpawnMap = new Dictionary<Vector2Int, SpawnableConfig>();

        if (_level?.SpwanablesConfigurations == null) return;

        string active = SceneManager.GetActiveScene().name;

        foreach (var cfg in _level.SpwanablesConfigurations)
        {
            if (cfg == null) continue;

            // Only draw configs for this scene. (Empty or null are treated as this scene for safety.)
            if (!string.IsNullOrEmpty(cfg.sceneName) && cfg.sceneName != active)
                continue;

            var key = new Vector2Int(cfg.row, cfg.col);

            // If multiples exist for same cell, prefer the last one in the list
            _cellSpawnMap[key] = cfg;
        }

        Repaint();
    }


    private void ApplyEditorColors()
    {
        var player = GameObject.FindWithTag("Player");
        if (player && player.TryGetComponent<Renderer>(out var pr))
            SetColor(pr, _gameManager.PlayerColor);
        
        foreach (var wall in GameObject.FindGameObjectsWithTag("Boundary"))
            if (wall.TryGetComponent<Renderer>(out var r))
                SetColor(r, _gameManager.WallColor);

        foreach (var wall in GameObject.FindGameObjectsWithTag("Obstacle"))
            if (wall.TryGetComponent<Renderer>(out var r))
                SetColor(r, _gameManager.WallColor);

        foreach (var cube in Object.FindObjectsOfType<Cube>())
            if (cube.IsFilled && cube.TryGetComponent<Renderer>(out var r))
                SetColor(r, _gameManager.CubeFillColor);

        foreach (var enemy in Object.FindObjectsByType<EnemyCube>(FindObjectsSortMode.None))
            if (enemy.TryGetComponent<Renderer>(out var r))
                SetColor(r, _gameManager.EnemyCubeColor);

        var bg = GameObject.FindWithTag("GridBackground");
        if (bg && bg.TryGetComponent<Renderer>(out var br))
            SetColor(br, _gameManager.BackgroundColor);
    }
    private void SetColor(Renderer r, Color color)
    {
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);

        if (r.sharedMaterial.HasProperty("_BaseColor"))
            block.SetColor("_BaseColor", color);
        else if (r.sharedMaterial.HasProperty("_Color"))
            block.SetColor("_Color", color);
        //else
            //Debug.LogWarning($"{r.gameObject.name} has no _Color or _BaseColor");
        r.SetPropertyBlock(block);
    }



    private static readonly Dictionary<int, Vector3> ColumnToCamOrtho = new()
{
    { 8,  new Vector3(7f, 10f, -1.5f) },
    { 10, new Vector3(9.36f, 10f, -1.5f) },
    { 12, new Vector3(11.67f, 10f, -1f) },
    { 14, new Vector3(14.04f, 10f, -0.8f) },
    { 16, new Vector3(16.4f, 10f, -0.5f) },
    { 18, new Vector3(18.78f, 10f, 0f) },
    { 20, new Vector3(21.2f, 10f, .5f) },
    { 22, new Vector3(23.5f, 10f, .5f) },
    { 24, new Vector3(25.8f, 10f, .5f) },
    { 26, new Vector3(28.3f, 10f, 1f) },
    { 28, new Vector3(30.6f, 10f, 1f) },
    { 30, new Vector3(33f, 10f, 1f) },
    { 32, new Vector3(35.4f, 10f, 1f) },
    { 34, new Vector3(37.8f, 15f, 1f) },
    { 36, new Vector3(40f, 15f, 1f) },
    { 38, new Vector3(42.5f, 15f, 1f) },
    { 40, new Vector3(44.8f, 15f, 1f) },
    { 42, new Vector3(47f, 20f, 1f) },
    { 44, new Vector3(49.5f, 20f, 1f) },
    { 46, new Vector3(51.7f, 20f, 1f) },
    { 48, new Vector3(54f, 20f, 1f) },
    { 50, new Vector3(56.5f, 20f, 1f) },
};

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

        float initialPct = cfg.progressThreshold * 100f;
        initialPct = Mathf.Round(EditorGUILayout.Slider("Progress (%)", initialPct, 0f, 100f));
        cfg.progressThreshold = initialPct * 0.01f;

        EditorGUILayout.Space();

        float newSpeed = EditorGUILayout.FloatField("Move Speed", cfg.moveSpeed);
        cfg.moveSpeed = newSpeed;

        if (cfg.prefab != null && cfg.enemyType == SpawnablesType.CubeEater)
        {
            var cubeEater = cfg.prefab.GetComponent<CubeEater>();
            if (cubeEater != null)
            {
                cubeEater.speed = newSpeed; 
            }
        }

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
                subPct = Mathf.Round(EditorGUILayout.Slider($"After spawn #{i + 1}", subPct, minPct, 100f));
                cfg.subsequentProgressThresholds[i] = subPct * 0.01f;
            }
        }
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
            EditorUtility.SetDirty(FindFirstObjectByType<GameManager>());
    }

}
internal class PreplacedSpeedPopup : EditorWindow
{
    private GameObject _target;
    private SerializedObject _serializedObject;
    private SerializedProperty _speedProperty;

    public static void Open(GameObject enemy, Rect buttonRect)
    {
        var wnd = CreateInstance<PreplacedSpeedPopup>();
        wnd._target = enemy;

        if (enemy.TryGetComponent<EnemyBehaviors>(out var eb))
        {
            wnd._serializedObject = new SerializedObject(eb);
            wnd._speedProperty = wnd._serializedObject.FindProperty("speed");
        }
        else if (enemy.TryGetComponent<CubeEater>(out var ce))
        {
            wnd._serializedObject = new SerializedObject(ce);
            wnd._speedProperty = wnd._serializedObject.FindProperty("speed");
        }

        wnd.minSize = new Vector2(200, 60);
        wnd.ShowAsDropDown(buttonRect, wnd.minSize);
    }

    void OnGUI()
    {
        if (_serializedObject == null || _speedProperty == null)
        {
            EditorGUILayout.HelpBox("Target is missing or unsupported.", MessageType.Error);
            return;
        }

        _serializedObject.Update();

        EditorGUILayout.LabelField("Set Preplaced Speed", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_speedProperty);

        _serializedObject.ApplyModifiedProperties();

        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OK"))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(_serializedObject.targetObject);
            EditorUtility.SetDirty(_target);
            Close();
        }

        if (GUILayout.Button("Cancel"))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}
