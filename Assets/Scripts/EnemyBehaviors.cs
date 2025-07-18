using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[HideMonoScript]
public class EnemyBehaviors : MonoBehaviour
{
    [Title("ENEMY-BEHAVIORS", null, titleAlignment: TitleAlignments.Centered)]

    [DisplayAsString]
    public  float speed = 5f;
    [SerializeField, DisplayAsString] SpawnablesType enemyType;
    [SerializeField, DisplayAsString] float bounceAngle = 3f;
    [SerializeField, DisplayAsString] float minInitial = 0.3f;  

    private GridManager gridManager;
    private Rigidbody rb;
    private Vector3 dir;

    private int _lastDestroyFrame = -1;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gridManager = GridManager.Instance;

        rb.useGravity = false;
        rb.angularDamping = 0f;
        rb.linearDamping = 0f;

        dir = PickRandomXZDirection(minInitial);
        rb.linearVelocity = dir * speed;
    }

    void FixedUpdate()
    {
        if (enemyType == SpawnablesType.SpikeBall)
            SpikedBallMovement();
        else
            rb.linearVelocity = dir * speed;
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
    //private bool IsCubeFilledAt(Vector3 worldPos)
    //{
    //    float r = gridManager.cellSize * 0.45f;  
    //    Collider[] hits = Physics.OverlapBox(worldPos + Vector3.up * 0.1f,
    //                                        new Vector3(r, 0.1f, r),
    //                                        Quaternion.identity);
    //    foreach (var hit in hits)
    //    {
    //        if (hit.TryGetComponent<Cube>(out Cube c)
    //            && c.IsFilled
    //            && c.gameObject.activeInHierarchy)  
    //        {
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    //private Vector3 PickRandomFilledNeighbor(Vector2Int cell)
    //{
    //    Vector2Int[] offsets = {
    //        new Vector2Int( 1,  0), 
    //        new Vector2Int(-1,  0), 
    //        new Vector2Int( 0,  1), 
    //        new Vector2Int( 0, -1)  
    //    };
    //    Vector3[] worldDirs = {
    //        Vector3.right,
    //        Vector3.left,
    //        Vector3.forward,
    //        Vector3.back
    //    };

    //    var choices = new List<Vector3>();
    //    for (int i = 0; i < offsets.Length; i++)
    //    {
    //        var n = cell + offsets[i];
    //        if (n.x < 0 || n.x >= gridManager._gridColumns ||
    //            n.y < 0 || n.y >= gridManager._gridRows)
    //            continue;

    //        if (gridManager._grid[n.x, n.y])
    //            choices.Add(worldDirs[i]);
    //    }

    //    if (choices.Count > 0)
    //        return choices[Random.Range(0, choices.Count)];
    //    return Vector3.zero;
    //}

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
            if (enemyType == SpawnablesType.SolidBall)
            {
                BounceOffNormal(collision.contacts[0].normal);
            }
            if (enemyType == SpawnablesType.MultiColoredBall && cube.IsFilled && Time.frameCount != _lastDestroyFrame)
            {
                gridManager.RemoveCubeAt(cube);
                gridManager.RemoveCubeAt(cube);

                BounceOffNormal(collision.contacts[0].normal);

                _lastDestroyFrame = Time.frameCount;
            }
            else if (!cube.IsFilled && cube.CanHarm)
            {
                AudioManager.instance.PlaySFXSound(3);
                Haptics.Generate(HapticTypes.HeavyImpact);
                GameManager.Instance.CameraShake(0.35f, 0.15f);
                GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                GameManager.Instance.LevelLose();
            }
        }
        else if (collision.transform.CompareTag("Player"))
        {
            AudioManager.instance.PlaySFXSound(3);
            collision.gameObject.SetActive(false);
            var renderer = collision.gameObject.GetComponent<Renderer>();
            GameManager.Instance.SpawnDeathParticles(collision.transform.gameObject, renderer.material.color);
            GameManager.Instance.CameraShake(0.35f, 0.15f);
            GameManager.Instance.LevelLose();
        }
        else if (collision.transform.CompareTag("Boundary")
            || collision.transform.CompareTag("Obstacle")
            || collision.transform.CompareTag("EnemyGroup")
            || collision.transform.CompareTag("Enemy")
            && Time.frameCount != _lastDestroyFrame)
        {
            BounceOffNormal(collision.contacts[0].normal);
            _lastDestroyFrame = Time.frameCount;

        }
    }


    private void BounceOffNormal(Vector3 normal)
    {
        if (normal == Vector3.zero)
        {
            dir = PickRandomXZDirection(0.4f);
        }
        else
        {
            dir = Vector3.Reflect(dir, normal).normalized;
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
