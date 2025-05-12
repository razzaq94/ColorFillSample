using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiColoredBall : MonoBehaviour
{
    public float speed = 5f;

    public float spinFactor = 360f;

    Rigidbody _rb;
    GridManager _gm;
    int _cols, _rows;

    Vector2Int _gridPos;
    Vector2Int _dir;
    Vector3 _targetWorld;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = true;             

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;               

        _gm = GridManager.Instance;
        _cols = _gm.Columns;
        _rows = _gm.Rows;
    }

    void Start()
    {
        _gridPos = _gm.WorldToGrid(transform.position);
        transform.position = _gm.GridToWorld(_gridPos);

        PickRandomDir();
        SetNextTarget();
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 nextPos = Vector3.MoveTowards(transform.position, _targetWorld, step);

        float dist = Vector3.Distance(transform.position, nextPos);
        if (transform && dist > 0f)
        {
            Vector3 moveDir = new Vector3(_dir.x, 0, _dir.y).normalized;
            Vector3 spinAxis = Vector3.Cross(moveDir, Vector3.up);
            transform.Rotate(spinAxis, dist * spinFactor, Space.World);
        }

        transform.position = nextPos;

        if (Vector3.SqrMagnitude(transform.position - _targetWorld) < 0.0001f)
        {
            var nextCell = _gridPos + _dir;
            if (InBounds(nextCell))
            {
                _gridPos = nextCell;
                _targetWorld = _gm.GridToWorld(_gridPos + _dir);
            }
            else
            {
                _dir = -_dir;
                nextCell = _gridPos + _dir;
                if (!InBounds(nextCell))

                _gridPos = _gridPos; 
                _targetWorld = _gm.GridToWorld(_gridPos + _dir);
                _gridPos += _dir;
            }
        }
    }

    bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < _cols && c.y < _rows;
    }

    void PickRandomDir()
    {
        var choices = new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };
        _dir = choices[Random.Range(0, choices.Count)];
    }

    void SetNextTarget()
    {
        _targetWorld = _gm.GridToWorld(_gridPos + _dir);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            _gm.RemoveCubeAt(cube);
            PickRandomDir();

        }
        if (other.gameObject.CompareTag("Boundary"))
        {
            PickRandomDir();
        }
    }
    
}