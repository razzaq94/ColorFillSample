using System.Collections.Generic;
using UnityEngine;

public class CubeGrid : MonoBehaviour
{
    public static CubeGrid Instance;
    [SerializeField] private int _poolSizeX = 10;  // Width (X axis)
    [SerializeField] private int _poolSizeY = 10;  // Height (Y axis)
    [SerializeField] private Cube _cubePrefab = null;

    private Queue<Cube> _cubeQueue = new Queue<Cube>();
    private List<Cube> TakenCubes = new List<Cube>();
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
                Vector3 position = new Vector3(x, 0f, y);

                Cube cube = Instantiate(_cubePrefab.gameObject, position, Quaternion.identity).GetComponent<Cube>();
                cube.gameObject.SetActive(false);
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
