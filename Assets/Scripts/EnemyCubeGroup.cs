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
        print("Reversing direction");
        _direction *= -1;
        _cellsMoved = 0;
    }
    bool hit = false;

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

    void HitReset()
    {
        hit = false;
    }
}