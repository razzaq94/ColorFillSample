using UnityEngine;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine.InputSystem.Haptics;
using Unity.Collections;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int _gridColumns = 10;
    public int _gridRows = 20;
    public int _totalCount = 0;
    public int _trueCount = 0;
    public float _progress = 0f;
    public float cellSize = 1f;
    [Space]
    public bool[,] _grid;

    private const float PercentFalseCount = 0.1f;
    private float _limitToFill = 0f;
    public int Columns => _gridColumns;
    public int Rows => _gridRows;
    private void Awake()
    {
        Instance = this;
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
        //if (AudioManager.instance)
        //    AudioManager.instance?.PlaySFXSound(1);
        Haptics.Generate(HapticTypes.LightImpact);

        // 1) keep old grid snapshot
        bool[,] oldGrid = (bool[,])_grid.Clone();

        for (int i = 0; i < 2; i++)
        {
            bool[,] gridCopy = (bool[,])_grid.Clone();
            bool[,] gridCopySecond = (bool[,])_grid.Clone();
            bool[,] newGrid = FloodFill(gridCopy, gridCopySecond);

            DestroyEnemiesInNewlyFilledCells(oldGrid, newGrid);

            SetProgressBar(newGrid);
            _grid = (bool[,])newGrid.Clone();

            oldGrid = (bool[,])_grid.Clone();
        }

        if (_progress >= 1f)
            GameManager.Instance.LevelComplete();
    }
    
 void DestroyEnemiesInNewlyFilledCells(bool[,] oldGrid, bool[,] newGrid)
    {
        int cols = _gridColumns;
        int rows = _gridRows;

        // Find all enemies in the scene
        var enemies = GameObject.FindObjectsOfType<EnemyBehaviors>();
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
}
