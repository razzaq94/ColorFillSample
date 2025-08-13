using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[HideMonoScript]
public class EnemyBehaviors : AEnemy
{
    [Title("ENEMY-BEHAVIORS", null, titleAlignment: TitleAlignments.Centered)]

    [SerializeField, DisplayAsString] float bounceAngle = 3f;
    [SerializeField, DisplayAsString] float minInitial = 0.3f;

    private Vector3 dir;
    public Vector3 lastPosition;
    public float stuckTime = 0f;
    private float stuckCheckInterval = 2f; 
    private float stuckDistanceThreshold = 1f;

    protected override void Start()
    {
        base.Start();
        lastPosition = transform.position;
        rb.useGravity = false;
        rb.angularDamping = 0f;
        rb.linearDamping = 0f;

        dir = PickRandomXZDirection(minInitial);
        rb.linearVelocity = dir * speed;
    }
    
    private void Update()
    {
        CheckForStuck();
    }
    
    private void CheckForStuck()
    {
        stuckTime += Time.deltaTime;
        if (stuckTime >= stuckCheckInterval)
        {
            float dist = Vector3.Distance(transform.position, lastPosition);
            if (dist < stuckDistanceThreshold)
            {
                dir = PickRandomXZDirection(minInitial);
                rb.linearVelocity = dir * speed;
            }

            lastPosition = transform.position;
            stuckTime = 0f;
        }
    }
    
    void FixedUpdate()
    {
        if (enemyType == SpawnablesType.SpikeBall)
        {
            SpikedBallMovement();
        }
        else
        {
            StandardMovement();
        }
    }

    private void StandardMovement()
    {
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            dir = PickRandomXZDirection(minInitial);
        }
        rb.linearVelocity = dir.normalized * speed;
    }

    private void SpikedBallMovement()
    {
        var filled = gridManager.GetAllFilledCells()
                                .Where(c => c.gameObject.activeInHierarchy)
                                .ToList();

        Vector3 pos = transform.position;
        Vector2Int curr = gridManager.WorldToGrid(pos);
        Vector2Int next = gridManager.WorldToGrid(pos + dir * speed * Time.fixedDeltaTime);

        bool currValid = filled.Any(c => gridManager.WorldToGrid(c.transform.position) == curr);
        bool nextValid = filled.Any(c => gridManager.WorldToGrid(c.transform.position) == next);

        if (!currValid || !nextValid)
        {
            Vector3 newDir = PickRandomFilledNeighbor(curr, filled);
            dir = (newDir != Vector3.zero) ? newDir : -dir;  
            dir = ApplyJitter(dir).normalized;
        }

        rb.linearVelocity = dir * speed;
    }

    private Vector3 PickRandomFilledNeighbor(Vector2Int cell, List<Cube> filled)
    {
        var candidates = filled
            .Where(c => {
                var idx = gridManager.WorldToGrid(c.transform.position);
                return Mathf.Abs(idx.x - cell.x) + Mathf.Abs(idx.y - cell.y) == 1;
            })
            .ToList();

        if (candidates.Count == 0)
            return Vector3.zero;

        var chosen = candidates[Random.Range(0, candidates.Count)];
        Vector3 d = (chosen.transform.position - transform.position).normalized;
        d.y = 0;  
        return d;
    }

    private Vector3 ApplyJitter(Vector3 d)
    {
        float jitter = Random.Range(-bounceAngle, bounceAngle);
        return Quaternion.Euler(0f, jitter, 0f) * d;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsValidCollision(collision))
            return;
            
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            HandleCubeCollision(cube, collision);
        }
        else if (collision.transform.CompareTag("Player"))
        {
            HandlePlayerCollision(collision);
        }
        else if (IsBoundaryOrObstacle(collision.transform))
        {
            HandleBoundaryCollision(collision);
        }
    }
    
    private void HandleCubeCollision(Cube cube, Collision collision)
    {
        if (enemyType == SpawnablesType.SolidBall && cube.IsFilled)
        {
            BounceOffNormal(collision.contacts[0].normal);
        }
        else if (enemyType == SpawnablesType.MultiColoredBall && cube.IsFilled)
        {
            gridManager.RemoveCubeAt(cube);  
            BounceOffNormal(collision.contacts[0].normal);
        }
        else if (!cube.IsFilled && cube.CanHarm)
        {
            HandleHarmfulCollision();
        }
    }
    
    private void HandlePlayerCollision(Collision collision)
    {
        BounceOffNormal(collision.contacts[0].normal);
        rb.MovePosition(transform.position + dir * 0.1f);
        HandlePlayerCollision();
    }
    
    private void HandleBoundaryCollision(Collision collision)
    {
        BounceOffNormal(collision.contacts[0].normal);
        rb.MovePosition(transform.position + dir * 0.1f);
    }
    
    private bool IsBoundaryOrObstacle(Transform obj)
    {
        string tag = obj.tag;
        return tag == "Boundary" || tag == "Obstacle" || tag == "EnemyGroup" || 
               tag == "Enemy" || tag == "Heart" || tag == "SlowDown" || 
               tag == "Timer" || tag == "Diamond";
    }
   
    private void BounceOffNormal(Vector3 normal)
    {
        if (normal == Vector3.zero || dir.magnitude < 0.1f)
        {
            dir = PickRandomXZDirection(0.4f);
        }
        else
        {
            dir = Vector3.Reflect(dir, normal).normalized;
            dir = Quaternion.Euler(0f, Random.Range(-bounceAngle, bounceAngle), 0f) * dir;
        }

        float jitter = Random.Range(-bounceAngle, bounceAngle);
        dir = Quaternion.Euler(0f, jitter, 0f) * dir;
        dir = dir.normalized;

        if (dir.magnitude < 0.1f)
            dir = PickRandomXZDirection(0.4f);

        rb.linearVelocity = dir * speed;  
    }
}

public enum SpawnablesType
{
    SpikeBall,
    FlyingHoop,
    MultiColoredBall,
    CubeEater,
    SolidBall,
    Pickups,
    RotatingMine,
}
