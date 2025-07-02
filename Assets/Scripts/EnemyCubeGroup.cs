using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using NUnit.Framework.Internal;

[HideMonoScript]
public class EnemyCubeGroup : MonoBehaviour
{
    [Title("ENEMY-CUBE-GROUP", null, titleAlignment: TitleAlignments.Centered)]
    public EnemyCube[] Cubes = null;
    [DisplayAsString]
    public int Detected = 0;
    //public bool Static = false;
    public bool moveHorizontal = true;
    public bool moveVertical = false;
    public bool isStatic = false;
    public int moveCells = 5;
    [HideInInspector] public float cellSize = 1f;
    public float moveSpeed = 2f;

    Rigidbody _rb;
    int _direction = 1;
    int _cellsMoved = 0;
    Vector3 _target;

    public float HitResetTime = 0.5f;

    void Start()
    {
        Cubes = GetComponentsInChildren<EnemyCube>();
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeAll;
        if (!moveHorizontal && !moveVertical)
        {
            enabled = false;
            Debug.LogWarning("No axis selected; disabling mover.");
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
    //public void Restart()
    //{
    //    Detected = 0;
    //    gameObject.SetActive(true);
    //    for (int i = 0; i < Cubes.Length; i++)
    //        Cubes[i].gameObject.SetActive(true);
    //}

    //public void CubeDestroyed()
    //{
    //    Detected++;
    //    if (Detected == Cubes.Length)
    //    {
    //        AudioManager.instance?.PlaySFXSound(3);
    //        gameObject.SetActive(false);
    //    }
    //}

    public void SetNextTarget()
    {
        Vector3 dir = moveHorizontal
            ? Vector3.right * _direction
            : Vector3.forward * _direction;

        _target = transform.position + dir * cellSize;
    }


    public void ReverseDirection()
    {
        _direction *= -1;
        _cellsMoved = 0;

        Vector3 offset = (moveHorizontal ? Vector3.right : Vector3.forward) * _direction * cellSize * 0.5f;
        StartCoroutine(SmoothReverseOffset(offset));
    }


    private IEnumerator SmoothReverseOffset(Vector3 offset)
    {
        Vector3 start = transform.position;
        Vector3 end = start + offset;
        float duration = 0.1f; // you can tweak this
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.position = end;
    }

    public bool hit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") ||
            other.CompareTag("Boundary") ||
            other.CompareTag("EnemyGroup"))
        {
            if(hit)
            {
                return;
            }
            hit = true;
            Invoke(nameof(HitReset), HitResetTime);
            print("Obstacle hit: " + other.name);
            ReverseDirection(); 
            SetNextTarget();
        }
        if (other.GetComponent<Cube>() == null)
            return;
    }
    private void OnTriggerStay(Collider other)
    {
        if (hit)
            return;

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