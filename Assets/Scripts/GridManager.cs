using UnityEngine;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine.InputSystem.Haptics;
using System.Linq;
using Sirenix.OdinInspector;

[HideMonoScript]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Title("GRID-MANAGER", null, titleAlignment: TitleAlignments.Centered)]

    [SerializeField, DisplayAsString] int _gridColumns = 10;
    [SerializeField, DisplayAsString] int _gridRows = 20;
    [SerializeField, DisplayAsString] int _totalCount = 0;
    [SerializeField, DisplayAsString] int _trueCount = 0;
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
        int col = Mathf.RoundToInt(x + (_gridColumns / 2));
        int row = Mathf.Abs(Mathf.RoundToInt(z - (_gridRows / 2)));
        _grid[col, row] = true;
        _trueCount++;
    }

    public void PerformFloodFill()
    {
        Haptics.Generate(HapticTypes.LightImpact);

        bool[,] oldGrid = (bool[,])_grid.Clone();

        bool[,] obstacleMap = new bool[_gridColumns, _gridRows];
        float half = cellSize * 0.45f;
        for (int x = 0; x < _gridColumns; x++)
            for (int y = 0; y < _gridRows; y++)
            {
                Vector3 ctr = GridToWorld(new Vector2Int(x, y)) + Vector3.up * 0.1f;
                var hits = Physics.OverlapBox(ctr, new Vector3(half, 0.1f, half), Quaternion.identity);
                foreach (var h in hits)
                    if (h.CompareTag("Obstacle"))
                    {
                        obstacleMap[x, y] = true;
                        break;
                    }
            }

        bool[,] cubeSnapshot = (bool[,])_grid.Clone();

        bool[,] boundary = new bool[_gridColumns, _gridRows];
        for (int x = 0; x < _gridColumns; x++)
            for (int y = 0; y < _gridRows; y++)
                boundary[x, y] = cubeSnapshot[x, y] || obstacleMap[x, y];

        bool[,] afterFill = FloodFillAlgo(boundary, cubeSnapshot);

        DestroyEnemiesInNewlyFilledCells(oldGrid, afterFill);
        SetProgressBar(afterFill);

        // 7) commit back to _grid
        _grid = (bool[,])afterFill.Clone();

        if (_progress >= 1f)
            GameManager.Instance.LevelComplete();
    }



    void DestroyEnemiesInNewlyFilledCells(bool[,] oldGrid, bool[,] newGrid)
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        // Find all enemies in the scene
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Vector3 pos = enemy.transform.position;
            int col = Mathf.RoundToInt(pos.x + cols / 2f);
            int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));

            if (col < 0 || col >= cols || row < 0 || row >= rows)
                continue;

            if (!oldGrid[col, row] && newGrid[col, row])
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private void SetProgressBar(bool[,] _grid)
    {
        _trueCount = GetTrueGridCount(_grid);
        _progress = (float)_trueCount / _totalCount;
        UIManager.Instance.FillAmount(_progress);
    }

    private int GetTrueGridCount(bool[,] _grid)
    {
        int trueCount = 0;
        for (int col = 0; col < _gridColumns; col++)
        {
            for (int row = 0; row < _gridRows; row++)
            {
                if (_grid[col, row])
                    trueCount++;
            }
        }
        return trueCount;
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

    private void MakeCubes(List<Point> pointList)
    {
        foreach (Point point in pointList)
        {
            Vector3 repCubePos = FindTransformFromPoint(point);
            Cube cube = CubeGrid.Instance.GetCube();
            cube.Initalize(repCubePos, true);
        }
    }
    private bool[,] FloodFillAlgo(bool[,] boundaryGrid, bool[,] originalGrid)
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        // Track visited empty cells
        bool[,] visited = new bool[cols, rows];
        var regions = new List<List<Point>>();

        // Offsets for 4-way neighbor checks
        var dirs = new (int dx, int dy)[]
        {
        ( 1,  0),
        (-1,  0),
        ( 0,  1),
        ( 0, -1),
        };

        // 1) Discover every connected region of “false” cells
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!boundaryGrid[x, y] && !visited[x, y])
                {
                    var queue = new Queue<Point>();
                    var region = new List<Point>();

                    visited[x, y] = true;
                    queue.Enqueue(new Point(x, y));
                    region.Add(new Point(x, y));

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        foreach (var (dx, dy) in dirs)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;
                            if (nx >= 0 && nx < cols && ny >= 0 && ny < rows
                                && !visited[nx, ny]
                                && !boundaryGrid[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue(new Point(nx, ny));
                                region.Add(new Point(nx, ny));
                            }
                        }
                    }
                    regions.Add(region);
                }
            }

        // **NEW**: if there's only one region (i.e. everything outside your line),
        // skip filling completely and return the grid as it was—
        // your drawn cubes have already been FillCube()'d.
        if (regions.Count <= 1)
            return originalGrid;

        // 2) Pick the *smallest* region by cell count (your true “pocket”)
        var pocket = regions.OrderBy(r => r.Count).First();

        // 3) Spawn cubes in that pocket
        MakeCubes(pocket);

        // 4) Build and return the updated grid
        var newGrid = (bool[,])originalGrid.Clone();
        foreach (var p in pocket)
            newGrid[p.X, p.Y] = true;
        return newGrid;
    }


    private void UpdateProgressAndEnemies(bool[,] oldGrid, bool[,] newGrid)
    {
        int cols = _gridColumns, rows = _gridRows;
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Vector3 pos = enemy.transform.position;
            int col = Mathf.RoundToInt(pos.x + cols / 2f);
            int row = Mathf.Abs(Mathf.RoundToInt(pos.z - rows / 2f));
            if (col < 0 || col >= cols || row < 0 || row >= rows) continue;

            if (!oldGrid[col, row] && newGrid[col, row])
                Destroy(enemy);
        }

        _trueCount = GetTrueGridCount(newGrid);
        _progress = (float)_trueCount / _totalCount;
        UIManager.Instance.FillAmount(_progress);
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
