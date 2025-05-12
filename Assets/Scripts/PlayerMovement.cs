using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    [SerializeField] bool _isMoving  = false;
    [SerializeField] float moveSpeed = 10f;

     Vector3 _moveVector = Vector3.zero;

    [SerializeField] bool _useKeyboard = true;
    [SerializeField] Direction _direction = Direction.None;

    private Player _player = null;
    private const float DistanceThreshold = 1f;
    private const float MaxSwipeTime      = 0.5f;
    private const float MinSwipeDistance  = 0.10f;
    private Rigidbody _rigidbody = null;
    private Vector3   _startPos3 = Vector3.zero;
    private Vector3   _targetPos = Vector3.zero;
    private Vector2   _startPos2 = Vector2.zero;
    private float     _startTime = 0f;

    //Properties
    public bool IsMoving
    {
        get => _isMoving; 
        set
        {
            _isMoving = value;
            if(!_isMoving)
                _moveVector = _rigidbody.linearVelocity = Vector3.zero;
        }//set end
    }//IsMoving end

    public void Init()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _player = GetComponent<Player>();
        _player.Init();
    }//Start() end

    public void Restart()
    {
        _player.SpawnCubes = _isMoving = false;
        _rigidbody.linearVelocity = _startPos3 = _targetPos = _moveVector= Vector3.zero;
        _startPos2 = Vector2.zero;
        _startTime = 0f;
        this.enabled = true;
    }//Restart() end

    private void Update()
    {
        DetectInput();
        DecideMovement();
    }

    private void FixedUpdate()
    {
        if(!_isMoving)
            return;
        if(Vector3.Distance(_startPos3, transform.position) >= DistanceThreshold)
        {
            transform.position = _targetPos;
            _startPos3 = transform.position;
            _targetPos = transform.position + _moveVector;
            _player.SpawnCube();
        }//if end
        transform.position += (_targetPos - _startPos3) * moveSpeed * Time.fixedDeltaTime;
    }//FixedUpdate() end

    private void DetectInput()
    {
        _direction = Direction.None;

        if(Input.touches.Length > 0)
        {
            Touch t = Input.GetTouch(0);
            if(t.phase == TouchPhase.Began)
            {
                _startPos2  = new Vector2(t.position.x / (float)Screen.width, t.position.y / (float)Screen.width); // normalize position according to screen width.
                _startTime = Time.time;
            }//if end
            if(t.phase == TouchPhase.Ended)
            {
                if(Time.time - _startTime > MaxSwipeTime) // swipe duration is too short.
                    return;

                Vector2 endPos = new Vector2(t.position.x / (float)Screen.width, t.position.y / (float)Screen.width);

                Vector2 swipe = new Vector2(endPos.x - _startPos2.x, endPos.y - _startPos2.y);

                if(swipe.magnitude < MinSwipeDistance) // swipe magnitude is below threshold.
                    return;

                if(Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                { 
                    if(swipe.x > 0)
                        _direction = Direction.Right;
                        // swipeRight = true;
                    else
                        _direction = Direction.Left;
                        // swipeLeft = true;
                }//if end
                else
                { 
                    if(swipe.y > 0)
                        _direction = Direction.Up;
                        // swipeUp = true;
                    else
                        _direction = Direction.Down;
                        // swipeDown = true;
                }//else end
            }//if end
        }//if end

        if(_useKeyboard)
        {
            if(Input.GetKeyDown(KeyCode.W))
                _direction = Direction.Up;
            if(Input.GetKeyDown(KeyCode.S))
                _direction = Direction.Down;
            if(Input.GetKeyDown(KeyCode.A))
                _direction = Direction.Left;
            if(Input.GetKeyDown(KeyCode.D))
                _direction = Direction.Right;
        }//if end
    }//DetectSwipes() end

    private void DecideMovement()
    {
        switch(_direction)
        {
            case Direction.None:
            return;
            case Direction.Up:
                if(_player.SpawnCubes && _moveVector.Equals(Vector3.back))
                    return;
                _moveVector = Vector3.forward;
            break;
            case Direction.Down:
                if(_player.SpawnCubes && _moveVector.Equals(Vector3.forward))
                    return;
                _moveVector = Vector3.back;
            break;
            case Direction.Left:
                if(_player.SpawnCubes && _moveVector.Equals(Vector3.right))
                    return;
                _moveVector = Vector3.left;
            break;
            case Direction.Right:
                if(_player.SpawnCubes && _moveVector.Equals(Vector3.left))
                    return;
                _moveVector = Vector3.right;
            break;
        }//switch end
        SetTargetPos();
    }//ApplyMovement() end

    private void SetTargetPos()
    {
        if(_isMoving)
            return;
        _player.SpawnCubes = _isMoving = true;
        _targetPos  = transform.position + _moveVector;
        _startPos3  = transform.position;
    }//SetTargetPos() end

}//class end