using System.Collections.Generic;
using UnityEngine;

public class SpikeBall : MonoBehaviour
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
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 nextPos = Vector3.MoveTowards(transform.position, _targetWorld, step);
        float dist = Vector3.Distance(transform.position, nextPos);
        if (dist > 0f)
        {
            Vector3 moveDir = new Vector3(_dir.x, 0, _dir.y).normalized;
            Vector3 spinAxis = Vector3.Cross(moveDir, Vector3.up);
            transform.Rotate(spinAxis, dist * spinFactor, Space.World);
        }
        transform.position = nextPos;

        if ((transform.position - _targetWorld).sqrMagnitude < 0.0001f)
        {
            Vector2Int nextCell = _gridPos + _dir;
            if (!InBounds(nextCell))
            {
                if (nextCell.x < 0 || nextCell.x >= _cols) _dir.x = -_dir.x;
                if (nextCell.y < 0 || nextCell.y >= _rows) _dir.y = -_dir.y;
                nextCell = _gridPos + _dir;
            }

            _gridPos = nextCell;
            _targetWorld = _gm.GridToWorld(_gridPos + _dir);
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

    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.LevelLose();
        }
        if (other.CompareTag("Boundary"))
        {
            PickRandomDir();
        }
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                gameObject.SetActive(false);
            }
            PickRandomDir();
        }
    }
}
