using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;


[HideMonoScript]
public class Player : MonoBehaviour
{
    public static Player Instance;
    [Title("PLAYER", null, titleAlignment: TitleAlignments.Centered)]
    [DisplayAsString]
    [SerializeField] bool _spawnCubes = false;
    [DisplayAsString]
    [SerializeField] bool _isMoving = false;
    [SerializeField] bool _useKeyboard = true;

    private const float DistanceThreshold = 1f;
    private const float MaxSwipeTime = 0.5f;
    private const float MinSwipeDistance = 0.10f;
    private float _startTime = 0f;

    [SerializeField] public float moveSpeed = 10f;

    private Vector3 _startPos3 = Vector3.zero;
    [DisplayAsString]
    private Vector3 _moveVector = Vector3.zero;
    private Vector3 _targetPos = Vector3.zero;
    private Vector2 _startPos2 = Vector2.zero;

    [SerializeField] List<Cube> spawnedCubes = new List<Cube>();
    [SerializeField] private Color playerColor = Color.white;
    public Material material;
    private Rigidbody _rigidbody = null;
    public Color GetPlayerColor() => playerColor;
    [SerializeField] Direction _direction = Direction.None;
    public bool IsMoving
    {
        get => _isMoving;
        set
        {
            _isMoving = value;
            if (!_isMoving)
                _moveVector = _rigidbody.linearVelocity = Vector3.zero;
        }
    }
    private void Awake()
    {
        Instance = this;
    }
    public bool SpawnCubes{get => _spawnCubes; set => _spawnCubes = value; }

    public void Init()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (material != null)
            material.color = GameManager.Instance.PlayerColor;
        spawnedCubes = new List<Cube>();
        MakeCube();
    }
    private void Update()
    {
        DetectInput();
        DecideMovement();
    }

    private void FixedUpdate()
    {
        if (!_isMoving)
            return;
        if (Vector3.Distance(_startPos3, transform.position) >= DistanceThreshold)
        {
            transform.position = _targetPos;
            _startPos3 = transform.position;
            _targetPos = transform.position + _moveVector;
            SpawnCube();
        }
        transform.position += (_targetPos - _startPos3) * moveSpeed * Time.fixedDeltaTime;
    }
    private Vector3 RoundPos() => new Vector3((float)Mathf.Round(transform.position.x), transform.position.y, (float)Mathf.Round(transform.position.z));

    public void SpawnCube()
    {
        if(!_spawnCubes)
            return;
        MakeCube();
    }

    private void MakeCube()
    {
        spawnedCubes.Add(CubeGrid.Instance.GetCube());
        spawnedCubes[spawnedCubes.Count - 1].Initalize(new Vector3((float)Mathf.Round(transform.position.x), transform.position.y - 0.7f, (float)Mathf.Round(transform.position.z)));
    }

    private void FillCubes()
    {
        foreach (var cube in spawnedCubes)
        {
            cube.FillCube();
            // Safety update to grid just in case
            GridManager.Instance.ChangeValue(cube.transform.position.x, cube.transform.position.z);
        }
        spawnedCubes.Clear();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Boundary") || collision.gameObject.CompareTag("Obstacle"))
        {
            IsMoving = false;
            transform.DOMove(RoundPos(), 0.1f);
            if (_spawnCubes)
            {
                SpawnCube();
                _spawnCubes = false;
                FillCubes();
                GridManager.Instance.PerformFloodFill();
            }
        }
        if (collision.gameObject.TryGetComponent<EnemyCube>(out EnemyCube enemyCube))
        {
            IsMoving = false;
            transform.position = RoundPos();
            AudioManager.instance.PlaySFXSound(3);
            Haptics.Generate(HapticTypes.HeavyImpact);
            GameManager.Instance.LevelLose();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                FillCubes();
                if (_spawnCubes)
                    GridManager.Instance.PerformFloodFill();
            }
            else
            {
                if (cube.CanHarm)
                {
                    Haptics.Generate(HapticTypes.HeavyImpact);
                    AudioManager.instance.PlaySFXSound(3);
                    GameManager.Instance.LevelLose();
                }
            }
        }
        //else if (other.TryGetComponent<EnemyCube>(out EnemyCube enemyCube))
        //{
        //    IsMoving = false;
        //    transform.position = RoundPos();
        //    GameManager.Instance.LevelLose();
        //}
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
                _spawnCubes = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
                _spawnCubes = true;
            else
                cube.CanHarm = true;
        }
    }
    private void DetectInput()
    {
        _direction = Direction.None;

        if (Input.touches.Length > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _startPos2 = new Vector2(t.position.x / (float)Screen.width, t.position.y / (float)Screen.width); // normalize position according to screen width.
                _startTime = Time.time;
            }
            if (t.phase == TouchPhase.Ended)
            {
                if (Time.time - _startTime > MaxSwipeTime) 
                    return;

                Vector2 endPos = new Vector2(t.position.x / (float)Screen.width, t.position.y / (float)Screen.width);

                Vector2 swipe = new Vector2(endPos.x - _startPos2.x, endPos.y - _startPos2.y);

                if (swipe.magnitude < MinSwipeDistance) 
                    return;

                if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                {
                    if (swipe.x > 0)
                        _direction = Direction.Right;
                    // swipeRight = true;
                    else
                        _direction = Direction.Left;
                    // swipeLeft = true;
                }
                else
                {
                    if (swipe.y > 0)
                        _direction = Direction.Up;
                    // swipeUp = true;
                    else
                        _direction = Direction.Down;
                    // swipeDown = true;
                }
            }
        }

        if (_useKeyboard)
        {
            if (Input.GetKeyDown(KeyCode.W))
                _direction = Direction.Up;
            if (Input.GetKeyDown(KeyCode.S))
                _direction = Direction.Down;
            if (Input.GetKeyDown(KeyCode.A))
                _direction = Direction.Left;
            if (Input.GetKeyDown(KeyCode.D))
                _direction = Direction.Right;
        }
    }

    private void DecideMovement()
    {
        switch (_direction)
        {
            case Direction.None:
                return;
            case Direction.Up:
                if (SpawnCubes && _moveVector.Equals(Vector3.back))
                    return;
                _moveVector = Vector3.forward;
                break;
            case Direction.Down:
                if (SpawnCubes && _moveVector.Equals(Vector3.forward))
                    return;
                _moveVector = Vector3.back;
                break;
            case Direction.Left:
                if (SpawnCubes && _moveVector.Equals(Vector3.right))
                    return;
                _moveVector = Vector3.left;
                break;
            case Direction.Right:
                if (SpawnCubes && _moveVector.Equals(Vector3.left))
                    return;
                _moveVector = Vector3.right;
                break;
        }
        SetTargetPos();
    }
    private void SetTargetPos()
    {
        if (_isMoving)
            return;
        SpawnCubes = _isMoving = true;
        _targetPos = transform.position + _moveVector;
        _startPos3 = transform.position;
    }
    
    public void Restart()
    {
        SpawnCubes = _isMoving = false;
        _rigidbody.linearVelocity = _startPos3 = _targetPos = _moveVector = Vector3.zero;
        _startPos2 = Vector2.zero;
        _startTime = 0f;
        this.enabled = true;
    }
}
public enum Direction
{
    None,
    Up,
    Down,
    Left,
    Right
}