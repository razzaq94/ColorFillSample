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
        //gridManager = GridManager.Instance; 
        //Vector3 p = transform.position;
        //transform.position = new Vector3(
        //    Mathf.Round(p.x / gridSize) * gridSize,
        //    p.y,
        //    Mathf.Round(p.z / gridSize) * gridSize
        //);

        //currentDir = PickRandomDirection(excludeOpposite: false);
        //StartCoroutine(GridMove());
        //InvokeRepeating(nameof(ChangeDirection), 3f, 3f);
    }
    public void Init(float speedFromCfg)
    {
        this.speed = speedFromCfg; 
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
        while (true)
        {
            Vector3 start = transform.position;
            Vector3 target = start + currentDir * gridSize;

            target.x = Mathf.Round(target.x / gridSize) * gridSize;
            target.z = Mathf.Round(target.z / gridSize) * gridSize;

            // Use Physics check before moving
            Vector3 boxCenter = target + Vector3.up * 0.5f;
            Vector3 halfExtents = new Vector3(gridSize * 0.45f, 0.45f, gridSize * 0.45f);

            Collider[] hits = Physics.OverlapBox(
                boxCenter,
                halfExtents,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Collide
            );

            bool blocked = false;
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Boundary") || hit.CompareTag("Obstacle") || hit.CompareTag("Enemy") || hit.CompareTag("EnemyGroup"))
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
                    {
                        AudioManager.instance?.PlaySFXSound(3);
                        Haptics.Generate(HapticTypes.HeavyImpact);
                        GameManager.Instance.CameraShake(0.35f, 0.15f);
                        GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                        GameManager.Instance.LevelLose();
                    }
                }

                if (hit.CompareTag("Player"))
                {
                    Haptics.Generate(HapticTypes.HeavyImpact);
                    var renderer = hit.gameObject.GetComponent<Renderer>();
                    GameManager.Instance.SpawnDeathParticles(hit.transform.gameObject, renderer.material.color);
                    GameManager.Instance.CameraShake(0.35f, 0.15f);
                    hit.gameObject.SetActive(false);
                    GameManager.Instance.Player.ClearUnfilledTrail();
                    currentDir = PickRandomDirection(excludeOpposite: true);
                    GameManager.Instance.LevelLose();
                }
            }

            if (blocked)
            {
                yield return null;
                continue;
            }

            float elapsed = 0f;
            float duration = gridSize / speed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            // Snap to final position to prevent float drift
            transform.position = target;
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
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioManager.instance?.PlaySFXSound(3);
            Haptics.Generate(HapticTypes.HeavyImpact);
            collision.gameObject.SetActive(false);
            GameManager.Instance.LevelLose();
        }
        
    }
}
