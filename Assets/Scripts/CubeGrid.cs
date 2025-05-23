using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class CubeGrid : MonoBehaviour
{
    public static CubeGrid Instance;
    [Title("CUBE-GRID", null, titleAlignment: TitleAlignments.Centered)]
    [SerializeField] private int _poolSizeX = 10;  // Width (X axis)
    [SerializeField] private int _poolSizeY = 10;  // Height (Y axis)
    [SerializeField] private Cube _cubePrefab = null;

    private Queue<Cube> _cubeQueue = new Queue<Cube>();
    private List<Cube> TakenCubes = new List<Cube>();
    public List<Cube> AllCubes;
    public GridManager _gridManager;
    private void Awake()
    {
        Instance = this;
        MakePool();
    }
    private void MakePool()
    {
        for (int x = 0; x < _poolSizeX; x++)
        {
            for (int y = 0; y < _poolSizeY; y++)
            {
                Vector3 worldPos = _gridManager.GridToWorld(new Vector2Int(x, y));
                worldPos.y = 0f;              
                Cube cube = Instantiate(_cubePrefab, worldPos, Quaternion.identity)
                                .GetComponent<Cube>();
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
