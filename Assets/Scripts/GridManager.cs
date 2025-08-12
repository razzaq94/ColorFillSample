using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Haptics;

[HideMonoScript]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Title("GRID-MANAGER", null, titleAlignment: TitleAlignments.Centered)]

    public int _gridColumns = 50;
    public int _gridRows = 50;
    public int TotalExposedGridCount;
    [SerializeField, DisplayAsString] int _totalCount = 0;
    [SerializeField, DisplayAsString]public int _trueCount = 0;
    [ProgressBar(0f, 1f, Height = 20)]public float _progress = 0f;

    public float cellSize = 1f;
    
    [Space]
    [HideInEditorMode]
    [ShowInInspector, ReadOnly] bool[,] _grid;

    private const float PercentFalseCount = 0.1f;
    private float _limitToFill = 0f;
    public int Columns => _gridColumns;
    public int Rows => _gridRows;
    public Cube[] allCubes;

    private bool lastPocketFilled = false;

    private Dictionary<int, List<Vector2Int>> _areas;
    private int _regionCounter = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        allCubes = GameObject.FindObjectsByType<Cube>(FindObjectsSortMode.None);
    }
    public void InitGrid(int col, int row)
    {
        _gridColumns = col;
        _gridRows = row;
        _grid = new bool[_gridColumns, _gridRows];

        _totalCount = _gridColumns * _gridRows;
        _limitToFill = _totalCount * PercentFalseCount;
    }

    public void ChangeValue(float x, float z)
    {
        Vector2Int index = WorldToGrid(new Vector3(x, 0, z));

        if (index.x < 0 || index.x >= _gridColumns || index.y < 0 || index.y >= _gridRows)
            return;

        if (_grid[index.x, index.y])
            return;

        _grid[index.x, index.y] = true;
        CheckAndFillIfPocketFormed();

        //_trueCount++;
    }

    public bool IsFilled(Vector2Int index)
    {
        if (index.x < 0 || index.x >= _grid.GetLength(0) || index.y < 0 || index.y >= _grid.GetLength(1))
            return false;
        return _grid[index.x, index.y];
    }


    public void PerformFloodFill()
    {
        Haptics.Generate(HapticTypes.LightImpact);

        bool[,] oldGrid = (bool[,])_grid.Clone();

        _obstacleMap = new bool[_gridColumns, _gridRows];
        float half = cellSize * 0.45f;
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                Vector3 ctr = GridToWorld(new Vector2Int(x, y)) + Vector3.up * 0.1f;
                var hits = Physics.OverlapBox(ctr, new Vector3(half, 0.1f, half), Quaternion.identity);
                foreach (var h in hits)
                {
                    if (h.CompareTag("Obstacle") || h.CompareTag("Boundary"))
                    {
                        _obstacleMap[x, y] = true;
                        break;
                    }
                }
            }
        }

        bool[,] cubeSnapshot = (bool[,])_grid.Clone();
        bool[,] boundary = new bool[_gridColumns, _gridRows];
        for (int x = 0; x < _gridColumns; x++)
            for (int y = 0; y < _gridRows; y++)
                boundary[x, y] = cubeSnapshot[x, y] || _obstacleMap[x, y];



        FillHoles();
        float percent = GetCurrentFillPercentage();

        SetProgressBar(_grid);

        if (percent >= 0.9f)
        {
            StartCoroutine(FinalFullVisualFillThenWin());

        }
        TryForceFillLastPocket();
        SetProgressBar(_grid);

        ForceFillRemainingVisuals();

        if (_progress >= 1f)
            StartCoroutine(DelayedLevelWin());
    }


    private IEnumerator DelayedLevelWin()
    {
        yield return new WaitForEndOfFrame(); // let cube visuals render
        yield return new WaitForSeconds(0.1f); // optional buffer
        ForceFillEveryUnfilledCell();
        GameManager.Instance.LevelComplete();
    }


    //void DestroyEnemiesInNewlyFilledCells(bool[,] oldGrid, bool[,] newGrid)
    //{
    //    int cols = _gridColumns;
    //    int rows = _gridRows;

    //    // Find all enemies in the scene
    //    var enemies = GameObject.FindGameObjectsWithTag("Enemy");
    //    foreach (var enemy in enemies)
    //    {
    //        Vector3 pos = enemy.transform.position;
    //        int col = Mathf.RoundToInt(pos.x + cols / 2f);
    //        int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));

    //        if (col < 0 || col >= cols || row < 0 || row >= rows)
    //            continue;

    //        if (!oldGrid[col, row] && newGrid[col, row])
    //        {
    //            var renderer = enemy.GetComponent<EnemyCube>()?._renderer;
    //            if (renderer != null)
    //            {
    //                AudioManager.instance?.PlaySFXSound(2);
    //                GameManager.Instance.SpawnDeathParticles(enemy.transform.gameObject, renderer.material.color);
    //            }
    //            Destroy(enemy.gameObject);
    //        }
    //    }
    //    var diamonds = GameObject.FindGameObjectsWithTag("Diamond");
    //    foreach (var diamond in diamonds)
    //    {
    //        Vector3 pos = diamond.transform.position;
    //        int col = Mathf.RoundToInt(pos.x + cols / 2f);
    //        int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));
    //        if (col < 0 || col >= cols || row < 0 || row >= rows)
    //            continue;
    //        if (!oldGrid[col, row] && newGrid[col, row])
    //        {
    //            AudioManager.instance?.PlaySFXSound(1);
    //            Destroy(diamond.gameObject);
    //            UIManager.Instance.AnimateDiamondGainFromWorld(diamond.transform.position);
    //        }
    //    }

    //    var heart = GameObject.FindGameObjectWithTag("Heart");
    //    if (heart != null)
    //    {
    //        Vector3 pos = heart.transform.position;
    //        int col = Mathf.RoundToInt(pos.x + cols / 2f);
    //        int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));
    //        if (col < 0 || col >= cols || row < 0 || row >= rows)
    //            return;
    //        if (!oldGrid[col, row] && newGrid[col, row])
    //        {
    //            AudioManager.instance?.PlaySFXSound(1);
    //            Destroy(heart.gameObject);
    //            UIManager.Instance.AnimateLifeGainFromWorld(heart.transform.position);

    //        }
    //    }
    //    var timer = GameObject.FindGameObjectWithTag("Timer");
    //    if(timer != null)
    //    {
    //        Vector3 pos = timer.transform.position;
    //        int col = Mathf.RoundToInt(pos.x + cols / 2f);
    //        int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));
    //        if (col < 0 || col >= cols || row < 0 || row >= rows)
    //            return;
    //        if (!oldGrid[col, row] && newGrid[col, row])
    //        {
    //            AudioManager.instance?.PlaySFXSound(1);
    //            Destroy(timer.gameObject);
    //            GameManager.Instance.AddTime(15); 
    //        }
    //    }
    //}

    private void SetProgressBar(bool[,] _grid)
    {
        _trueCount = GetTrueGridCount(_grid);  // This counts the filled cells

        // Get the list of exposed cells instead of just counting
        List<Vector2Int> exposedCells = GetExposedGridCells();
        if (exposedCells.Count <= 0)
        {
            Debug.LogWarning("No exposed cells found — possible progress bar bug");
            return;
        }

        _progress = (float)_trueCount / exposedCells.Count;
        UIManager.Instance.FillAmount(_progress);
        if (_progress < 0.1f && _trueCount > 100)
        {
            Debug.LogWarning($"Progress dropped unexpectedly: TrueCount = {_trueCount}, Exposed = {exposedCells.Count}, GridSize = {_gridColumns}x{_gridRows}");
        }

    }

    #region new method

    private int FloodFillRegion(Vector2Int start, int[,] tempGrid, int marker, int gridCols, int gridRows)
    {
        if (tempGrid[start.x, start.y] != 0)
            return 0;

        Queue<Vector2Int> queue = new();
        queue.Enqueue(start);
        tempGrid[start.x, start.y] = marker;
        _areas[marker].Add(start);
        int count = 1;

        Vector2Int[] directions = {
        new(1, 0), new(-1, 0),
        new(0, 1), new(0, -1)
    };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                if (nx < 0 || ny < 0 || nx >= gridCols || ny >= gridRows)
                    continue;

                if (tempGrid[nx, ny] == 0)
                {
                    tempGrid[nx, ny] = marker;
                    queue.Enqueue(new Vector2Int(nx, ny));
                    _areas[marker].Add(new Vector2Int(nx, ny));
                    count++;
                }
            }
        }

        return count;
    }
    public void FillHoles()
    {
        print("clear0");

        _areas = new Dictionary<int, List<Vector2Int>>();
        int[,] regionMap = new int[_gridColumns, _gridRows];

        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                regionMap[x, y] = (_grid[x, y] || _obstacleMap[x, y]) ? 1 : 0;
            }
        }

        _regionCounter = 2; 
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (regionMap[x, y] == 0)
                {
                    _areas[_regionCounter] = new List<Vector2Int>();
                    FloodFillRegion(new Vector2Int(x, y), regionMap, _regionCounter, _gridColumns, _gridRows);
                    _regionCounter++;
                }
            }
        }


        var largestArea = _areas.OrderByDescending(a => a.Value.Count).FirstOrDefault();

        print("clear1");
        foreach (var region in _areas)
        {
            print("clear1.5");

            if (region.Key == largestArea.Key)
                continue;

            print("clear2");

            foreach (var pos in region.Value)
            {
                print("clear3");

                if (!_grid[pos.x, pos.y])
                {
                    Vector3 worldPos = GridToWorld(pos) + Vector3.up * 0.1f;
                    var hits = Physics.OverlapBox(worldPos, new Vector3(0.4f, 0.5f, 0.4f), Quaternion.identity);
                    print("clear4");

                    foreach (var hit in hits)
                    {
                        print("clear5");

                        if (hit.CompareTag("Enemy"))
                        {
                            print("clear6");

                            var renderer = hit.GetComponent<Renderer>();
                            AudioManager.instance?.PlaySFXSound(2);
                            if (renderer)
                                GameManager.Instance.SpawnDeathParticles(hit.gameObject, renderer.material.color);
                            Destroy(hit.gameObject);
                        }
                        if (hit.CompareTag("Diamond"))
                        {
                            var renderer = hit.GetComponent<Renderer>();
                            AudioManager.instance?.PlaySFXSound(1);
                            if (renderer)
                                GameManager.Instance.SpawnDeathParticles(hit.gameObject, renderer.material.color);
                            UIManager.Instance.AnimateDiamondGainFromWorld(hit.transform.position);
                            Destroy(hit.gameObject);
                        }

                    }

                    if (GetCubeAtPosition(pos) == null)
                    {
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(GridToWorld(pos), true);
                        cube.FillCube(); 
                    }

                    _grid[pos.x, pos.y] = true; 
                }
            }
        }

        ForceFillRemainingVisuals();
        SyncVisualsForExposedFilledCells();
    }

    
    private float GetCurrentFillPercentage()
    {
        int total = 0;
        int filled = 0;

        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (_obstacleMap[x, y])
                    continue;

                total++;
                if (_grid[x, y])
                    filled++;
            }
        }

        return (float)filled / total;
    }
   
 
    private IEnumerator FinalFullVisualFillThenWin()
    {
        // ⏳ Give enemies time to finish any destruction
        //yield return new WaitForSeconds(0.3f);

        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (_obstacleMap[x, y]) continue;

                Vector2Int pos = new(x, y);
                bool hasVisual = GetCubeAtPosition(pos) != null;

                if (!hasVisual)
                {
                    Cube cube = CubeGrid.Instance.GetCube();
                    cube.Initalize(GridToWorld(pos), true);
                    cube.FillCube();
                }

                _grid[x, y] = true;
            }
        }
        yield return new WaitForSeconds(0.2f);
        ForceFillRemainingVisuals();
        //StartCoroutine(DelayedLevelWin());
    }
    private void ForceFillEveryUnfilledCell()
    {
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (_obstacleMap[x, y])
                    continue;

                Vector2Int pos = new(x, y);
                if (GetCubeAtPosition(pos) == null)
                {
                    Cube cube = CubeGrid.Instance.GetCube();
                    cube.Initalize(GridToWorld(pos), true);
                    cube.FillCube();
                    _grid[x, y] = true;
                }
            }
        }
    }
    private void TryForceFillLastPocket()
    {
        _areas = new Dictionary<int, List<Vector2Int>>();
        int[,] regionMap = new int[_gridColumns, _gridRows];

        // 1. Mark filled or obstacle cells as 1, empty as 0
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                regionMap[x, y] = (_grid[x, y] || _obstacleMap[x, y]) ? 1 : 0;
            }
        }

        // 2. Find all empty regions (connected 0s)
        _regionCounter = 2;
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (regionMap[x, y] == 0)
                {
                    _areas[_regionCounter] = new List<Vector2Int>();
                    FloodFillRegion(new Vector2Int(x, y), regionMap, _regionCounter, _gridColumns, _gridRows);
                    _regionCounter++;
                }
            }
        }

        // 3. Exclude the largest (main) region — usually connected to the boundary
        int maxCount = 0;
        int largestRegionKey = -1;
        foreach (var region in _areas)
        {
            if (region.Value.Count > maxCount)
            {
                maxCount = region.Value.Count;
                largestRegionKey = region.Key;
            }
        }

        // 4. Fill the other smaller pockets
        foreach (var region in _areas)
        {
            if (region.Key == largestRegionKey)
                continue; // skip the main region

            foreach (var pos in region.Value)
            {
                if (!_grid[pos.x, pos.y])
                {
                    if (GetCubeAtPosition(pos) == null)
                    {
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(GridToWorld(pos), true);
                        cube.FillCube();
                    }
                    _grid[pos.x, pos.y] = true;
                }
            }
        }

        ForceFillRemainingVisuals();
    }
    private HashSet<string> _filledPocketHashes = new();
    private string GetPocketHash(List<Point> pocket)
    {
        pocket.Sort((a, b) =>
        {
            int cmp = a.X.CompareTo(b.X);
            return cmp != 0 ? cmp : a.Y.CompareTo(b.Y);
        });

        return string.Join("_", pocket.Select(p => $"{p.X}-{p.Y}"));
    }

    private void CheckAndFillIfPocketFormed()
    {
        bool[,] gridBefore = (bool[,])_grid.Clone();

        List<List<Point>> newPockets = DetectNewEnclosedPockets();

        if (newPockets.Count > 0)
        {
            //Debug.Log($"🟢 Found {newPockets.Count} new enclosed pocket(s).");
            FillOnlyNewPockets(newPockets);
        }

        int beforeFilled = GetTrueGridCount(gridBefore);
        int afterFilled = GetTrueGridCount(_grid);

        if (afterFilled > beforeFilled)
        {
            //Debug.Log("✅ Pocket formed and filled. Auto-filled " + (afterFilled - beforeFilled) + " cells.");
            lastPocketFilled = true;
        }
        else
        {
            //Debug.Log("No pocket formed. Skipping auto-fill.");
            lastPocketFilled = false;
        }
    }
    private List<List<Point>> DetectNewEnclosedPockets()
    {
        int cols = _gridColumns;
        int rows = _gridRows;
        bool[,] visited = new bool[cols, rows];
        var dirs = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

        List<List<Point>> newPockets = new();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y] && !visited[x, y])
                {
                    bool touchesEdge = false;
                    var queue = new Queue<Point>();
                    var pocket = new List<Point>();

                    queue.Enqueue(new Point(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        pocket.Add(p);

                        if (p.X == 0 || p.X == cols - 1 || p.Y == 0 || p.Y == rows - 1)
                            touchesEdge = true;

                        foreach (var (dx, dy) in dirs)
                        {
                            int nx = p.X + dx;
                            int ny = p.Y + dy;

                            if (nx >= 0 && nx < cols && ny >= 0 && ny < rows &&
                                !_grid[nx, ny] && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue(new Point(nx, ny));
                            }
                        }
                    }

                    if (!touchesEdge && pocket.Count > 0)
                    {
                        string hash = GetPocketHash(pocket);
                        if (!_filledPocketHashes.Contains(hash))
                        {
                            newPockets.Add(pocket);
                            _filledPocketHashes.Add(hash);
                        }
                    }
                }
            }
        }

        return newPockets;
    }
    private void FillOnlyNewPockets(List<List<Point>> pockets)
    {
        foreach (var pocket in pockets)
        {
            foreach (var p in pocket)
            {
                Vector2Int pos = new(p.X, p.Y);
                if (GetCubeAtPosition(pos) == null)
                {
                    Cube cube = CubeGrid.Instance.GetCube();
                    cube.Initalize(GridToWorld(pos), true);
                    cube.FillCube();
                }

                _grid[p.X, p.Y] = true;
            }
        }

    }





    #endregion
    private int GetTrueGridCount(bool[,] _grid)
    {
        int trueCount = 0;
        int startX = 0, startY = 0;
        int endX = _gridColumns, endY = _gridRows;

        for (int col = startX; col < endX; col++)
        {
            for (int row = startY; row < endY; row++)
            {
                if (_grid[col, row] && IsCellExposed(new Vector2Int(col, row)))  
                    trueCount++;
            }
        }

        return trueCount;
    }
    public List<Vector2Int> GetExposedGridCells()
    {
        List<Vector2Int> exposedCells = new List<Vector2Int>();

        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (IsCellExposed(new Vector2Int(x, y)))  
                {
                    exposedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return exposedCells;
    }
    private void MakeCubes(List<Point> pointList)
    {
        foreach (Point point in pointList)
        {
            Vector2Int pos = new Vector2Int(point.X, point.Y);

            if (GetCubeAtPosition(pos) != null)
                continue; 

            Vector3 repCubePos = FindTransformFromPoint(point);
            Cube cube = CubeGrid.Instance.GetCube();
            cube.Initalize(repCubePos, true);
            cube.FillCube(); 
        }
    }


    private bool[,] FloodFill(bool[,] _grid, bool[,] gridCopySecond)
    {
        Queue<Point> q = new Queue<Point>();
        int randIndex;
        List<Point> testPointList = new List<Point>();
        bool[,] gridCopy3 = (bool[,])_grid.Clone();
        int falseCountBeforeFill = FindFalseCountPositions(_grid, ref testPointList, gridCopy3);

        if (falseCountBeforeFill < _limitToFill)
        {
            MakeCubes(testPointList);
            return gridCopy3;
        }

        Point randPoint = new Point();
        do
        {
            randIndex = Random.Range(0, testPointList.Count);
            randPoint = testPointList[randIndex];
        } while (_grid[randPoint.X, randPoint.Y]);

        List<Point> firstTryPointList = new List<Point>();
        q.Enqueue(randPoint);
        int trueCount = 0;
        while (q.Count > 0)
        {
            Point n = q.Dequeue();
            if (_grid[n.X, n.Y])
                continue;
            Point w = n, e = new Point(n.X + 1, n.Y);
            while ((w.X >= 0) && !_grid[w.X, w.Y])
            {
                _grid[w.X, w.Y] = true;
                firstTryPointList.Add(new Point(w.X, w.Y));
                trueCount++;
                if ((w.Y > 0) && !_grid[w.X, w.Y - 1])
                    q.Enqueue(new Point(w.X, w.Y - 1));
                if ((w.Y < _gridRows - 1) && !_grid[w.X, w.Y + 1])
                    q.Enqueue(new Point(w.X, w.Y + 1));
                w.X--;
            }
            while ((e.X <= _gridColumns - 1) && !_grid[e.X, e.Y])
            {
                _grid[e.X, e.Y] = true;
                firstTryPointList.Add(new Point(e.X, e.Y));
                trueCount++;
                if ((e.Y > 0) && !_grid[e.X, e.Y - 1])
                    q.Enqueue(new Point(e.X, e.Y - 1));
                if ((e.Y < _gridRows - 1) && !_grid[e.X, e.Y + 1])
                    q.Enqueue(new Point(e.X, e.Y + 1));
                e.X++;
            }
        }
        List<Point> secondTryPointList = new List<Point>();
        int falseCount = FindFalseCountPositions(_grid, ref secondTryPointList, gridCopySecond);

        if (falseCount > trueCount)
        {
            MakeCubes(firstTryPointList);
            return _grid;
        }

        if (secondTryPointList.Count > 0)
            MakeCubes(secondTryPointList);
        return gridCopySecond;
    }


    private bool[,] FloodFillAlgo(bool[,] boundaryGrid, bool[,] originalGrid)
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        bool[,] visited = new bool[cols, rows];
        var regions = new List<List<Point>>();

        var dirs = new (int dx, int dy)[]
        {
        (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!boundaryGrid[x, y] && !visited[x, y])
                {
                    var queue = new Queue<Point>();
                    var region = new List<Point>();

                    queue.Enqueue(new Point(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        region.Add(p);

                        foreach (var (dx, dy) in dirs)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;
                            if (nx >= 0 && nx < cols && ny >= 0 && ny < rows
                                && !visited[nx, ny] && !boundaryGrid[nx, ny])
                            {
                                queue.Enqueue(new Point(nx, ny));
                                visited[nx, ny] = true;
                            }
                        }
                    }

                    regions.Add(region);
                }
            }
        }

        if (regions.Count <= 1)
            return originalGrid;

        List<Point> outerRegion = regions.FirstOrDefault(r =>
            r.Any(p => p.X == 0 || p.X == cols - 1 || p.Y == 0 || p.Y == rows - 1)
        );

        if (outerRegion == null)
            outerRegion = regions.OrderByDescending(r => r.Count).First();  // fallback


        foreach (var region in regions)
        {
            if (region == outerRegion)
                continue;

            foreach (var p in region)
                originalGrid[p.X, p.Y] = true;

            MakeCubes(region);
        }


        return originalGrid;
    }


    private void FillFullyEnclosedPockets()
    {

        //print("LastFill");
        int cols = _gridColumns;
        int rows = _gridRows;

        bool[,] visited = new bool[cols, rows];
        var dirs = new (int dx, int dy)[]
        {
        (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        List<List<Point>> newPockets = new();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y] && !visited[x, y])
                {
                    bool isTouchingEdge = false;
                    var queue = new Queue<Point>();
                    var pocket = new List<Point>();

                    queue.Enqueue(new Point(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        pocket.Add(p);

                        if (p.X == 0 || p.X == cols - 1 || p.Y == 0 || p.Y == rows - 1)
                            isTouchingEdge = true;

                        foreach (var (dx, dy) in dirs)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;

                            if (nx >= 0 && nx < cols && ny >= 0 && ny < rows &&
                                !_grid[nx, ny] && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue(new Point(nx, ny));
                            }
                        }
                    }

                    if (!isTouchingEdge)
                    {
                        string hash = GetPocketHash(pocket);
                        if (!_filledPocketHashes.Contains(hash))
                        {
                            newPockets.Add(pocket);
                            _filledPocketHashes.Add(hash);
                        }
                    }

                }
            }
        }

        if (newPockets.Count == 0)
            return;

        List<GameObject> enemiesToDestroy = new();

        foreach (var pocket in newPockets)
        {
            foreach (var p in pocket)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(p.X, p.Y)) + Vector3.up * 0.1f;
                var hits = Physics.OverlapBox(worldPos, new Vector3(0.4f, 0.5f, 0.4f), Quaternion.identity);

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Enemy") && !enemiesToDestroy.Contains(hit.gameObject))
                        enemiesToDestroy.Add(hit.gameObject);

                    if (hit.CompareTag("Diamond"))
                    {
                        var renderer = hit.GetComponent<Renderer>();
                        AudioManager.instance?.PlaySFXSound(1);
                        if (renderer)
                            GameManager.Instance.SpawnDeathParticles(hit.gameObject, renderer.material.color);
                        UIManager.Instance.AnimateDiamondGainFromWorld(hit.transform.position);
                        Destroy(hit.gameObject);
                    }
                }
            }
        }

        foreach (var enemy in enemiesToDestroy)
        {
            var renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
                GameManager.Instance.SpawnDeathParticles(enemy, renderer.material.color);
            Destroy(enemy);
        }

        foreach (var pocket in newPockets)
        {
            foreach (var p in pocket)
            {
                Vector2Int pos = new Vector2Int(p.X, p.Y);
                if (GetCubeAtPosition(pos) == null)
                {
                    Cube cube = CubeGrid.Instance.GetCube();
                    cube.Initalize(GridToWorld(pos), true);
                    cube.FillCube(); // grid mark + visuals
                }

                _grid[p.X, p.Y] = true;
            }

            lastPocketFilled = true;
        }

        SyncVisualsForExposedFilledCells();
    }


    private void FillRemainingUnfilledCells()
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y])
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (GetCubeAtPosition(pos) == null)
                    {
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(GridToWorld(pos), true);
                        cube.FillCube();
                    }
                    // handles grid mark + visual
                }
            }
        }
    }

    public void ForceFillRemainingVisuals()
    {
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (_grid[x, y])
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Cube existing = GetCubeAtPosition(pos);

                    if (existing == null)
                    {
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(GridToWorld(pos), true);
                        cube.FillCube(true);
                    }
                    else if (!existing.IsFilled || !existing.gameObject.activeInHierarchy)
                    {
                        existing.Initalize(GridToWorld(pos), true);
                        existing.FillCube(true);
                    }
                }
            }
        }
    }


    private Vector3 FindTransformFromPoint(Point point) => new Vector3((float)(point.X - (float)_gridColumns / 2f), 0.3f, (float)((float)_gridRows / 2f - point.Y));

    private int FindFalseCountPositions(bool[,] _grid, ref List<Point> pointList, bool[,] gridCopy2)
    {
        int falseCount = 0;
        for (int col = 0; col < _gridColumns; col++)
        {
            for (int row = 0; row < _gridRows; row++)
            {
                if (_grid[col, row])
                    continue;
                falseCount++;
                gridCopy2[col, row] = true;
                pointList.Add(new Point(col, row));
            }
        }
        return falseCount;
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int col = Mathf.RoundToInt(world.x + (_gridColumns / 2f));
        int row = Mathf.Abs(Mathf.RoundToInt(world.z - (_gridRows / 2f)));
        return new Vector2Int(col, row);
    }

    public Vector3 GridToWorld(Vector2Int grid)
    {
        float x = grid.x - _gridColumns / 2f;
        float z = (_gridRows / 2f) - grid.y;
        return new Vector3(x, transform.position.y, z);
    }

    public void RemoveCubeAt(Cube cube)
    {
        Vector2Int idx = WorldToGrid(cube.transform.position);


        if (_grid[idx.x, idx.y])
        {
            _grid[idx.x, idx.y] = false;
            _trueCount = Mathf.Max(0, _trueCount - 1);
        }

        //print(cube.gameObject.name);
        cube.IsFilled = false; // always reset
        cube.CanHarm = false;
        cube.gameObject.SetActive(false);

        CubeGrid.Instance.PutBackInQueue(cube);
    }



    public List<Cube> GetAllFilledCells()
    {
        var filled = new List<Cube>();

        foreach (var cube in CubeGrid.Instance.AllCubes)
        {
            if (cube.IsFilled)
            {
                filled.Add(cube);
            }
        }
        return filled;
    }
    private void SyncVisualsForExposedFilledCells()
    {
        for (int x = 0; x < _gridColumns; x++)
        {
            for (int y = 0; y < _gridRows; y++)
            {
                if (_grid[x, y] && IsCellExposed(new Vector2Int(x, y)))
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (GetCubeAtPosition(pos) == null)
                    {
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(GridToWorld(pos), true);
                        cube.FillCube();
                    }

                }
            }
        }
    }

    private bool[,] _obstacleMap; // Store this during flood fill to reuse

    public bool IsCellExposed(Vector2Int gridPos)
    {
        if (_obstacleMap == null)
            return true; 

        if (gridPos.x < 0 || gridPos.x >= _gridColumns || gridPos.y < 0 || gridPos.y >= _gridRows)
            return false;

        return !_obstacleMap[gridPos.x, gridPos.y];
    }

    public Cube GetCubeAtPosition(Vector2Int position)
    {
        foreach (Cube cube in CubeGrid.Instance.AllCubes)
        {
            if (!cube.gameObject.activeInHierarchy)
                continue;

            Vector2Int cubePos = WorldToGrid(cube.transform.position);
            if (cubePos == position)
                return cube;
        }

        return null;
    }




    public List<Cube> GetAnyCells()
    {
        var filled = new List<Cube>();
        foreach (var cube in CubeGrid.Instance.AllCubes)
        {
            if (!cube.isActiveAndEnabled && !cube.gameObject.activeInHierarchy)
            {
                filled.Add(cube);
            }
        }
        return filled;
    }
    
}
