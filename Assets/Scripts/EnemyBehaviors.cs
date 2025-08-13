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

    private GridManager gridManager;
    private Vector3 dir;
    public Vector3 lastPosition;
    private int _lastDestroyFrame = -1;
    public float stuckTime = 0f;
    private float stuckCheckInterval = 2f; 
    private float stuckDistanceThreshold = 1f;

    public override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        gridManager = GridManager.Instance;
        lastPosition = transform.position;
        rb.useGravity = false;
        rb.angularDamping = 0f;
        rb.linearDamping = 0f;

        dir = PickRandomXZDirection(minInitial);
        rb.linearVelocity = dir * speed;

    }
    private void Update()
    {
        stuckTime += Time.deltaTime;
        if (stuckTime >= stuckCheckInterval)
        {
            float dist = Vector3.Distance(transform.position, lastPosition);
            if (dist < stuckDistanceThreshold)
            {
            print(dist);
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
            if (rb.linearVelocity.magnitude < 0.1f)
            {
                dir = PickRandomXZDirection(minInitial);
            }

            rb.linearVelocity = dir.normalized * speed;
            

        }

        
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

    private Vector3 PickRandomXZDirection(float minAxis)
    {
        Vector2 r;
        do
        {
            r = Random.insideUnitCircle.normalized;
        } while (Mathf.Abs(r.x) < minAxis || Mathf.Abs(r.y) < minAxis);

        return new Vector3(r.x, 0f, r.y).normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            if (enemyType == SpawnablesType.SolidBall && cube.IsFilled)
            {
                BounceOffNormal(collision.contacts[0].normal);
            }
            else if (enemyType == SpawnablesType.MultiColoredBall && cube.IsFilled && Time.frameCount != _lastDestroyFrame)
            {
                gridManager.RemoveCubeAt(cube);  
                BounceOffNormal(collision.contacts[0].normal);
                _lastDestroyFrame = Time.frameCount;
            }
            else if (!cube.IsFilled && cube.CanHarm)
            {
                HandleHarmfulCollision();
            }
        }
        else if (collision.transform.CompareTag("Player"))
        {
            BounceOffNormal(collision.contacts[0].normal);
            rb.MovePosition(transform.position + dir * 0.1f);
            HandlePlayerCollision();
        }
        else if (collision.transform.CompareTag("Boundary")
            || collision.transform.CompareTag("Obstacle")
            || collision.transform.CompareTag("EnemyGroup")
            || collision.transform.CompareTag("Enemy")
            || collision.transform.CompareTag("Heart")
            || collision.transform.CompareTag("SlowDown")
            || collision.transform.CompareTag("Timer")
            || collision.transform.CompareTag("Diamond")
            && Time.frameCount != _lastDestroyFrame)
        {
            BounceOffNormal(collision.contacts[0].normal);
            rb.MovePosition(transform.position + dir * 0.1f);
            _lastDestroyFrame = Time.frameCount;
        }
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

    private void HandleHarmfulCollision()
    {
        if (GameManager.Instance.loosed || GameManager.Instance.hasTriggeredLose)
        {
            return;
        }

        AudioManager.instance?.PlaySFXSound(3);
        Haptics.Generate(HapticTypes.HeavyImpact);
        GameManager.Instance.CameraShake(0.35f, 0.15f);
        GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
        GameManager.Instance.LevelLose();
    }

    private void HandlePlayerCollision()
    {
        AudioManager.instance?.PlaySFXSound(3);
        GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
        GameManager.Instance.CameraShake(0.35f, 0.15f);
        GameManager.Instance.Player.ClearUnfilledTrail();
        GameManager.Instance.LevelLose();
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
