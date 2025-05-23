using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

[HideMonoScript]
public class EnemyCubeGroup : MonoBehaviour
{
    [Title("ENEMY-CUBE-GROUP", null, titleAlignment: TitleAlignments.Centered)]
    public EnemyCube[] Cubes = null;
    [DisplayAsString]
    public int Detected = 0;

    public bool moveHorizontal = true;
    public bool moveVertical = false;
    public int moveCells = 5;
    public float cellSize = 1f;
    public float moveSpeed = 2f; 

    Rigidbody _rb;
    int _direction = 1;
    int _cellsMoved = 0;
    Vector3 _target;

   
    void Start()
    {
        Cubes = GetComponentsInChildren<EnemyCube>();
        _rb = GetComponent<Rigidbody>();

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
        Vector3 newPos = Vector3.MoveTowards(
            _rb.position,
            _target,
            moveSpeed * Time.fixedDeltaTime
        );
        _rb.MovePosition(newPos);

        if ((newPos - _target).sqrMagnitude < 0.0001f)
        {
            _cellsMoved++;
            if (_cellsMoved >= moveCells)
                ReverseDirection();
            SetNextTarget();
        }
    }
    public void Restart()
    {
        Detected = 0;
        gameObject.SetActive(true);
        for (int i = 0; i < Cubes.Length; i++)
            Cubes[i].gameObject.SetActive(true);
    }

    public void CubeDestroyed()
    {
        Detected++;
        if (Detected == Cubes.Length)
        {
            AudioManager.instance?.PlaySFXSound(3);
            gameObject.SetActive(false);
        }
    }

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
    }

    //void OnCollisionEnter(Collision col)
    //{
    //    if (col.collider.CompareTag("Obstacle") ||
    //        col.collider.CompareTag("Boundary"))
    //    {
    //        ReverseDirection();
    //        SetNextTarget();
    //    }
    //    if (col.collider.GetComponent<Cube>() == null)
    //        return;

    //    // Dispatch the hit to whichever child-collider actually made contact:
    //    foreach (var contact in col.contacts)
    //    {
    //        // contact.thisCollider is the child‐Collider (or parent’s) that hit the Cube
    //        var hitGO = contact.thisCollider.gameObject;
    //        var childDetector = hitGO.GetComponent<EnemyCube>();
    //        if (childDetector != null)
    //        {
    //            childDetector.OnCubeHit(col.collider.GetComponent<Cube>());
    //        }
    //    }
    //}
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") ||
            other.CompareTag("Boundary"))
        {
            ReverseDirection();
            SetNextTarget();
        }
        if (other.GetComponent<Cube>() == null)
            return;

        //// Dispatch the hit to whichever child-collider actually made contact:
        //foreach (var contact in other.contacts)
        //{
        //    // contact.thisCollider is the child‐Collider (or parent’s) that hit the Cube
        //    var hitGO = contact.thisCollider.gameObject;
        //    var childDetector = hitGO.GetComponent<EnemyCube>();
        //    if (childDetector != null)
        //    {
        //        childDetector.OnCubeHit(col.collider.GetComponent<Cube>());
        //    }
        //}
    }
}
