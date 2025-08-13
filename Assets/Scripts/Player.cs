using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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
    public Vector3 lastSafeFilledPosition;



    [SerializeField] List<Cube> spawnedCubes = new List<Cube>();
    [SerializeField] private Color playerColor = Color.white;
    public Material material;
    private Rigidbody _rigidbody = null;
    public Color GetPlayerColor() => playerColor;
    [SerializeField] Direction _direction = Direction.None;



    private Coroutine invincibilityRoutine;
    private BoxCollider triggerCollider;
    public  BoxCollider collisionCollider;
    private Material _flashMaterial; // clone of material






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
        lastSafeFilledPosition = RoundPos();
    }
    public bool SpawnCubes{get => _spawnCubes; set => _spawnCubes = value; }

    public void Init()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (material != null)
            material.color = GameManager.Instance.PlayerColor;
        if (material != null)
        {
            _flashMaterial = new Material(material); 
            GetComponent<Renderer>().material = _flashMaterial;
            _flashMaterial.color = GameManager.Instance.PlayerColor;
        }
        spawnedCubes = new List<Cube>();
        MakeCube();
        var colliders = GetComponents<BoxCollider>();
        foreach (var col in colliders)
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                collisionCollider = col;
        }
    }
    private void Update()
    {
        DetectInput();
        DecideMovement();
    }

    private void FixedUpdate()
    {
        if (!_isMoving)
        {
            lastSafeFilledPosition = transform.position;
            return;
        }
        if (Vector3.Distance(_startPos3, transform.position) >= DistanceThreshold)
        {
            transform.position = _targetPos;
            _startPos3 = transform.position;
            _targetPos = transform.position + _moveVector;
            SpawnCube();
        }
        transform.position += (_targetPos - _startPos3) * moveSpeed * Time.fixedDeltaTime;
    }


    public  Vector3 RoundPos() => new Vector3((float)Mathf.Round(transform.position.x), transform.position.y, (float)Mathf.Round(transform.position.z));

    public void SpawnCube()
    {
        if(!_spawnCubes)
            return;
        MakeCube();
    }

    private void MakeCube()
    {
        spawnedCubes.Add(CubeGrid.Instance.GetCube());
        
        spawnedCubes[spawnedCubes.Count - 1].SetTrail(new Vector3((float)Mathf.Round(transform.position.x), transform.position.y - 0.7f, (float)Mathf.Round(transform.position.z)));
        spawnedCubes[spawnedCubes.Count - 1].ApplyTrailColorFromLevel();
    }

    private void FillCubes()
    {
        for (int i = 0; i < spawnedCubes.Count; i++)
        {
            
            spawnedCubes[i].FillCube();
            spawnedCubes[i].Illuminate(0.5f); 
            GridManager.Instance.ChangeValue(spawnedCubes[i].transform.position.x, spawnedCubes[i].transform.position.z);
        }
        spawnedCubes.Clear();
        lastSafeFilledPosition = RoundPos();
    }
    public void ClearUnfilledTrail()
    {
        for (int i = 0; i < spawnedCubes.Count; i++)
        {
            if (spawnedCubes[i] != null)
            {
                CubeGrid.Instance.PutBackInQueue(spawnedCubes[i]);
                spawnedCubes[i].CanHarm = false;
            }
        }
        spawnedCubes.Clear();
    }
    public void ResetMovement()
    {
        _moveVector = Vector3.zero; 
        _startPos2 = Vector2.zero;
        _startTime = 0f;           
        _rigidbody.linearVelocity = Vector3.zero; 
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Boundary") || collision.gameObject.CompareTag("Obstacle"))
        {
            IsMoving = false;
            AudioManager.instance?.PlaySFXSound(3);
            transform.DOMove(RoundPos(), 0.1f);
            if (_spawnCubes)
            {
                SpawnCube();
                _spawnCubes = false;
                FillCubes();
                GridManager.Instance.PerformFloodFill();
            }
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
                {
                    print("lllll");
                    GridManager.Instance.PerformFloodFill();
                }
            }
            else
            {
                if (cube.CanHarm)
                {
                    Haptics.Generate(HapticTypes.HeavyImpact);
                    AudioManager.instance?.PlaySFXSound(3);
                    GameManager.Instance.LevelLose();
                }
            }
        }
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                cube.onPlayer = true;
                _spawnCubes = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                _spawnCubes = true;
                cube.onPlayer = false;
            }
            else
                cube.CanHarm = true;
        }
    }




    [SerializeField] private int swipeThreshold = 35; 
    [SerializeField] private float maxSwipeTime = 0.5f; 
    [SerializeField] private bool useKeyboard = true;

    private bool _swipeDetected;

    private void DetectInput()
    {
        _direction = Direction.None;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                _startPos2 = t.position;
                _startTime = Time.time;
                _swipeDetected = false;
            }
            else if (t.phase == TouchPhase.Moved && !_swipeDetected)
            {
                Vector2 swipe = t.position - _startPos2;

                if (swipe.magnitude >= swipeThreshold)
                {
                    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                        _direction = swipe.x > 0 ? Direction.Right : Direction.Left;
                    else
                        _direction = swipe.y > 0 ? Direction.Up : Direction.Down;

                    _swipeDetected = true; 
                }
            }
            else if (t.phase == TouchPhase.Ended)
            {
                // Optional: reset or handle quick taps
            }
        }

        if (useKeyboard)
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

    private Tween moveTween;

    private void SetTargetPos()
    {
        if (_isMoving)
            return;

        lastSafeFilledPosition = transform.position; // ✅ capture true last grounded position

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

        foreach (var cube in spawnedCubes)
        {
            CubeGrid.Instance.PutBackInQueue(cube);
        }
        spawnedCubes.Clear();

        this.enabled = true;
    }
    public void ForceInitialCube()
    {
        if (CubeGrid.Instance == null) return;

        Cube existingCube = CubeGrid.Instance.GetCubeAtPosition(lastSafeFilledPosition);
        if (existingCube != null)
        {
            return;
        }

        Cube cube = CubeGrid.Instance.GetCube();
        Vector3 spawnPos = new Vector3(
            Mathf.Round(transform.position.x),
            transform.position.y - 0.7f,
            Mathf.Round(transform.position.z)
        );

        cube.SetTrail(spawnPos, false);
        cube._renderer.material.color = GameManager.Instance.CubeFillColor;
        spawnedCubes.Add(cube);
    }
    public void InvincibleForSeconds(float seconds = 5f)
    {
        if (invincibilityRoutine != null)
            StopCoroutine(invincibilityRoutine);

        invincibilityRoutine = StartCoroutine(InvincibilityRoutine(seconds));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        if (triggerCollider != null) triggerCollider.enabled = false;
        if (collisionCollider != null) collisionCollider.enabled = false;
        this.enabled = false;

        Color originalColor = _flashMaterial.color;
        Color lightColor = originalColor + new Color(0.3f, 0.3f, 0.3f); 

        float elapsed = 0f;
        float pulseSpeed = 1f;

        UIManager.Instance.countDown.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            UIManager.Instance.countDown.text = i.ToString("00");

            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f); 
            Color pulsingColor = Color.Lerp(originalColor, lightColor, t);
            _flashMaterial.color = pulsingColor;

            yield return new WaitForSecondsRealtime(1f);
        }

        UIManager.Instance.countDown.text = "00";
        UIManager.Instance.countDown.gameObject.SetActive(false);

        while (elapsed < duration - 3f) 
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            Color pulsingColor = Color.Lerp(originalColor, lightColor, t);
            _flashMaterial.color = pulsingColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _flashMaterial.color = originalColor;

        if (triggerCollider != null) triggerCollider.enabled = true;
        if (collisionCollider != null) collisionCollider.enabled = true;
        enabled = true;

        invincibilityRoutine = null;
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