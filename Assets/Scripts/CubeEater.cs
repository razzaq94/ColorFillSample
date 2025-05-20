using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CubeEater : MonoBehaviour
{
    [Tooltip("Cells per second")]
    public float speed = 2f;
    [Tooltip("Distance between grid cells")]
    public float gridSize = 1f;
    GridManager gridManager;

    private static readonly Vector3[] directions = {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    private Vector3 currentDir;

    void Start()
    {
        gridManager = GridManager.Instance; 
        Vector3 p = transform.position;
        transform.position = new Vector3(
            Mathf.Round(p.x / gridSize) * gridSize,
            p.y,
            Mathf.Round(p.z / gridSize) * gridSize
        );

        currentDir = PickRandomDirection(excludeOpposite: false);
        StartCoroutine(GridMove());
        InvokeRepeating(nameof(ChangeDirection), 3f, 3f);
    }

    private void ChangeDirection()
    {
        currentDir = PickRandomDirection(excludeOpposite: true);
    }

    private IEnumerator GridMove()
    {
        {
            while (true)
            {
                Vector3 start = transform.position;
                Vector3 target = start + currentDir * gridSize;

                Vector3 boxCenter = target + Vector3.up * 0.5f;
                Vector3 halfExtents = new Vector3(gridSize * 0.45f, 0.45f, gridSize * 0.45f);

                Collider[] hits = Physics.OverlapBox(
                    boxCenter,
                    halfExtents,
                    Quaternion.identity,
                    /*layerMask:*/ ~0,
                    QueryTriggerInteraction.Collide
                );

                bool blocked = false;
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Boundary"))
                    {
                        currentDir = PickRandomDirection(excludeOpposite: true);
                        blocked = true;
                        break;
                    }

                    if (hit.TryGetComponent<Cube>(out Cube cube))
                    {
                        if (cube.IsFilled)
                            gridManager.RemoveCubeAt(cube);
                        else if (cube.CanHarm)
                            GameManager.Instance.LevelLose();
                    }

                    if (hit.CompareTag("Player"))
                    {
                        GameManager.Instance.LevelLose();
                    }
                }

                if (blocked)
                {
                    yield return null;
                    continue;
                }

                float t = 0f;
                float duration = gridSize / speed;
                while (t < duration)
                {
                    transform.position = Vector3.Lerp(start, target, t / duration);
                    t += Time.deltaTime;
                    yield return null;
                }
                transform.position = target;
            }
        }
    }


    private Vector3 PickRandomDirection(bool excludeOpposite)
    {
        Vector3 opposite = -currentDir;
        Vector3 d;
        int tries = 0;
        do
        {
            d = directions[Random.Range(0, directions.Length)];
            if (++tries > 8) break;
        }
        while (excludeOpposite && Vector3.Dot(d, opposite) > 0.9f);
        return d;
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
    //    {
    //        if (cube.IsFilled)
    //        {
    //            gridManager.RemoveCubeAt(cube);
    //        }
    //        else
    //        {
    //            if (cube.CanHarm)
    //            {
    //                GameManager.Instance.LevelLose();
    //            }
    //        }
    //    }
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        GameManager.Instance.LevelLose();
    //    }
    //    if (collision.gameObject.CompareTag("Boundary"))
    //    {
    //        StartCoroutine(GridMove());
    //        ChangeDirection();
    //    }
    //}
}
