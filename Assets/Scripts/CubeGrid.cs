using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class CubeGrid : MonoBehaviour
{
    public static CubeGrid Instance;
    [Title("CUBE-GRID", null, titleAlignment: TitleAlignments.Centered)]
    [SerializeField] private Cube _cubePrefab = null;

    private Queue<Cube> _cubeQueue = new Queue<Cube>();
    public List<Cube> TakenCubes = new List<Cube>();
    public List<Cube> AllCubes;
    public GridManager _gridManager;
    private void Awake()
    {
        Instance = this;
        MakePool();
    }
    private void MakePool()
    {
        int cols = _gridManager.Columns;
        int rows = _gridManager.Rows;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 worldPos = _gridManager.GridToWorld(new Vector2Int(x, y));
                worldPos.y = 0f;              
                Cube cube = Instantiate(_cubePrefab, worldPos, Quaternion.identity).GetComponent<Cube>();
                cube.gameObject.SetActive(false);
                cube.transform.SetParent(transform);    
                AllCubes.Add(cube);
                _cubeQueue.Enqueue(cube);
            }
        }
    }

    public Cube GetCube()
    {
        if (_cubeQueue.Count == 0)
            MakePool();

        TakenCubes.Add(_cubeQueue.Dequeue());
        return TakenCubes[TakenCubes.Count - 1];
    }
    public Cube GetCubeAtPosition(Vector3 position)
    {
        foreach (Cube cube in AllCubes)
        {
            if (cube.gameObject.activeInHierarchy && cube.transform.position == position)
            {
                return cube; // Return the cube if it's active
            }
        }
        return null; // No active cube found
    }

    public void PutBackInQueue(Cube cube)
    {
        cube.ResetCube();
        _cubeQueue.Enqueue(cube);
    }

    public void Restart()
    {
        for (int i = 0; i < TakenCubes.Count; i++)
            PutBackInQueue(TakenCubes[i]);

        TakenCubes.Clear();
    }
}
