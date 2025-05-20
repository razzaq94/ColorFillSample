using UnityEngine;
using System.Collections.Generic;

public class EnemyBehaviors : MonoBehaviour
{
    public float speed = 5f;
    public SpawnablesType enemyType;
    public float bounceAngle = 3f;   
    public float minInitial = 0.3f;  

    private GridManager gridManager;
    private Rigidbody rb;
    private Vector3 dir;

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
        if (enemyType == SpawnablesType.Killer)
            SpikedBallMovement();
        else
            rb.linearVelocity = dir * speed;
    }

    private void SpikedBallMovement()
    {
        Vector3 pos = transform.position;
        Vector2Int curr = gridManager.WorldToGrid(pos); 
        bool inCurrBounds = curr.x >= 0 && curr.x < gridManager._gridColumns && curr.y >= 0 && curr.y < gridManager._gridRows;
        bool currFilled = inCurrBounds && gridManager._grid[curr.x, curr.y]; 

        if (!currFilled)
        {
            Vector3 newDir = PickRandomFilledNeighbor(curr);
            if (newDir != Vector3.zero)
                dir = newDir;               
            else
                dir = -dir;                 

            dir = ApplyJitter(dir);
            dir.Normalize();
            rb.linearVelocity = dir * speed;
            return;
        }

        Vector3 nextPos = pos + dir * speed * Time.fixedDeltaTime;
        Vector2Int next = gridManager.WorldToGrid(nextPos);
        bool inNextBounds = next.x >= 0 && next.x < gridManager._gridColumns
                         && next.y >= 0 && next.y < gridManager._gridRows;
        bool nextFilled = inNextBounds && gridManager._grid[next.x, next.y];

        if (!nextFilled)
        {
            if (next.x != curr.x) dir.x = -dir.x;
            if (next.y != curr.y) dir.z = -dir.z;

            dir = ApplyJitter(dir);
            dir.Normalize();
        }

        rb.linearVelocity = dir * speed;
    }

    private Vector3 PickRandomFilledNeighbor(Vector2Int cell)
    {
        Vector2Int[] offsets = {
            new Vector2Int( 1,  0), 
            new Vector2Int(-1,  0), 
            new Vector2Int( 0,  1), 
            new Vector2Int( 0, -1)  
        };
        Vector3[] worldDirs = {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        var choices = new List<Vector3>();
        for (int i = 0; i < offsets.Length; i++)
        {
            var n = cell + offsets[i];
            if (n.x < 0 || n.x >= gridManager._gridColumns ||
                n.y < 0 || n.y >= gridManager._gridRows)
                continue;

            if (gridManager._grid[n.x, n.y])
                choices.Add(worldDirs[i]);
        }

        if (choices.Count > 0)
            return choices[Random.Range(0, choices.Count)];
        return Vector3.zero;
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
            if (cube.IsFilled && enemyType == SpawnablesType.CubeDestroyer)
            {
                gridManager.RemoveCubeAt(cube);
                BounceOffNormal(collision.contacts[0].normal);
            }
            else if (!cube.IsFilled && cube.CanHarm)
            {
                GameManager.Instance.LevelLose();
            }
        }
        else if (collision.transform.CompareTag("Player"))
        {
            //GameManager.Instance.LevelLose();
        }
        else if (collision.transform.CompareTag("Boundary"))
        {
            BounceOffNormal(collision.contacts[0].normal);
        }
    }

    private void BounceOffNormal(Vector3 normal)
    {
        dir = Vector3.Reflect(dir, normal).normalized;
        float jitter = UnityEngine.Random.Range(-bounceAngle, bounceAngle);
        dir = Quaternion.Euler(0f, jitter, 0f) * dir;
        dir.Normalize();
        rb.linearVelocity = dir * speed;
    }
}

public enum SpawnablesType
{
    Killer,
    FlyingHoop,
    CubeDestroyer,
    CubeEater,
    Diamond,
}
