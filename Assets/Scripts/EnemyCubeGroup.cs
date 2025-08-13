using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

[HideMonoScript]
public class EnemyCubeGroup : MonoBehaviour
{
    [Title("ENEMY-CUBE-GROUP", null, titleAlignment: TitleAlignments.Centered)]
    public EnemyCube[] Cubes = null;
    [DisplayAsString, ReadOnly] public bool returnOnEnemyCollision = false;
    [DisplayAsString]
    public int Detected = 0;

    public MoveDirection moveDirection = MoveDirection.Right;

    public bool isStatic = false;
    public int moveCells = 5;
    [HideInInspector] public float cellSize = 1f;
    public float moveSpeed = 2f;

    public float HitResetTime = 0.5f;

    private Rigidbody _rb;
    private int _direction = 1;
    private int _cellsMoved = 0;
    private Vector3 _target;
    private bool hit = false;

    void Start()
    {
        Cubes = GetComponentsInChildren<EnemyCube>();
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeAll;

        if (isStatic || moveDirection == MoveDirection.None)
        {
            enabled = false;
            //Debug.LogWarning("Static group or no movement direction set; disabling mover.");
            return;
        }
        SetNextTarget();
    }

    void FixedUpdate()
    {
        if (isStatic) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _target,
            moveSpeed * Time.fixedDeltaTime
        );

        if ((transform.position - _target).sqrMagnitude < 0.0001f)
        {
            _cellsMoved++;
            if (_cellsMoved >= moveCells)
                ReverseDirection();

            SetNextTarget();
        }
    }

    public void SetNextTarget()
    {
        Vector3 dir = GetDirectionVector();
        _target = transform.position + dir * _direction * cellSize;
    }

    public void ReverseDirection()
    {
        _direction *= -1;
        _cellsMoved = 0;

        Vector3 dir = GetDirectionVector();
        Vector3 offset = dir * _direction * cellSize * 0.5f;
        StartCoroutine(SmoothReverseOffset(offset));
    }

    private Vector3 GetDirectionVector()
    {
        switch (moveDirection)
        {
            case MoveDirection.Right: return Vector3.right;
            case MoveDirection.Left: return Vector3.left;
            case MoveDirection.Up: return Vector3.forward;
            case MoveDirection.Down: return Vector3.back;
            case MoveDirection.None:
            default: return Vector3.zero;
        }
    }


    private IEnumerator SmoothReverseOffset(Vector3 offset)
    {
        Vector3 start = transform.position;
        Vector3 end = start + offset;
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.position = end;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Obstacle") ||
            other.CompareTag("Boundary") ||
            other.CompareTag("EnemyGroup"))||(returnOnEnemyCollision && other.CompareTag("Enemy")))
        {
            Return();
        }
    }

    void Return()
    {
        if (hit) return;
        hit = true;
        Invoke(nameof(HitReset), HitResetTime);
        ReverseDirection();
        SetNextTarget();
    }

    private void OnTriggerStay(Collider other)
    {
        if (hit) return;

        if (other.CompareTag("Obstacle") || other.CompareTag("Boundary") || other.CompareTag("EnemyGroup"))
        {
            hit = true;
            Invoke(nameof(HitReset), HitResetTime);
            ReverseDirection();
            SetNextTarget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Boundary") || other.CompareTag("EnemyGroup"))
            hit = false;
    }

    void HitReset()
    {
        hit = false;
    }
}

public enum MoveDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}
