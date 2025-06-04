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

    public int _gridColumns = 50;
    public int _gridRows = 50;
    public int TotalExposedGridCount;
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

    private bool lastPocketFilled = false;


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

        _grid = (bool[,])afterFill.Clone();

        FillFullyEnclosedPockets();
        SetProgressBar(_grid);
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
        _trueCount = GetTrueGridCount(_grid);  // This counts the filled cells

        // Get the list of exposed cells instead of just counting
        List<Vector2Int> exposedCells = GridManager.Instance.GetExposedGridCells();

        // Use the count of exposed cells for progress calculation
        _progress = (float)_trueCount / exposedCells.Count;  // Use exposed cells count dynamically
        UIManager.Instance.FillAmount(_progress);
    }


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
                if (IsCellExposed(new Vector2Int(x, y)))  // Check if the cell is exposed
                {
                    exposedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return exposedCells;
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

    private void FillFullyEnclosedPockets()
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        bool[,] visited = new bool[cols, rows];
        var dirs = new (int dx, int dy)[]
        {
        ( 1,  0),
        (-1,  0),
        ( 0,  1),
        ( 0, -1),
        };

        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y] && !visited[x, y])
                {
                    var queue = new Queue<Point>();
                    var pocket = new List<Point>();
                    bool isEnclosed = true; // assume enclosed unless found otherwise

                    visited[x, y] = true;
                    queue.Enqueue(new Point(x, y));
                    pocket.Add(new Point(x, y));

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();

                        // If this pocket touches grid edge, it’s NOT enclosed
                        if (p.X == 0 || p.X == cols - 1 || p.Y == 0 || p.Y == rows - 1)
                            isEnclosed = false;

                        foreach (var (dx, dy) in dirs)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;

                            if (nx < 0 || nx >= cols || ny < 0 || ny >= rows)
                            {
                                isEnclosed = false;
                                continue;
                            }

                            if (!_grid[nx, ny] && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue(new Point(nx, ny));
                                pocket.Add(new Point(nx, ny));
                            }
                        }
                    }

                    // If pocket is enclosed and not empty, fill it immediately
                    if (isEnclosed && pocket.Count > 0)
                    {
                        MakeCubes(pocket);

                        // Update grid to mark these cells filled
                        foreach (var pnt in pocket)
                        {
                            _grid[pnt.X, pnt.Y] = true;
                        }
                        lastPocketFilled = true;
                    }
                }
            }
        if (lastPocketFilled)
        {
            FillRemainingUnfilledCells(); 
        }
    }

    private void FillRemainingUnfilledCells()
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        bool allFilled = true;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y]) // unfilled cells
                {
                    allFilled = false;
                    break;
                }
            }
            if (!allFilled) break;
        }

        if (!allFilled)
        {
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                {
                    if (!_grid[x, y]) // if the cell is unfilled
                    {
                        _grid[x, y] = true;
                        Vector3 position = GridToWorld(new Vector2Int(x, y));
                        Cube cube = CubeGrid.Instance.GetCube();
                        cube.Initalize(position, true);
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
    public bool IsCellExposed(Vector2Int gridPos)
    {
        // Define the position of the cell in world space
        Vector3 worldPos = GridToWorld(gridPos);

        // Check if the cell is covered by an obstacle (can use layers or tags)
        Collider[] hitColliders = Physics.OverlapBox(worldPos, new Vector3(cellSize / 2, 0.1f, cellSize / 2), Quaternion.identity);

        // Check if there's an obstacle at this grid position
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Obstacle"))  // Assuming "Obstacle" is the tag for obstacles
            {
                return false;  // This cell is obstructed, not exposed
            }
        }

        return true;  // This cell is exposed
    }

    public Cube GetCubeAtPosition(Vector2Int position)
    {
        foreach (Cube cube in CubeGrid.Instance.AllCubes)
        {
            Vector2Int cubePos = GridManager.Instance.WorldToGrid(cube.transform.position);
            if (cubePos == position)
            {
                return cube;
            }
        }
        return null; // Return null if no cube is found at the position (shouldn't happen if logic is correct)
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
