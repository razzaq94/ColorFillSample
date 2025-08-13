using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CubeEater : AEnemy
{
    [Tooltip("Distance between grid cells")]
    public float gridSize = 1f;

    private static readonly Vector3[] directions = {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    private Vector3 currentDir;

    protected override void Start()
    {
        base.Start();
        enemyType = SpawnablesType.CubeEater;
    }
    
    public void Init(float speedFromCfg)
    {
        this.speed = speedFromCfg; 
        SnapToGrid();
        currentDir = PickRandomDirection(directions, excludeOpposite: false, currentDir);
        StartCoroutine(GridMove());
        InvokeRepeating(nameof(ChangeDirection), 3f, 3f);
    }
    
    private void SnapToGrid()
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(
            Mathf.Round(p.x / gridSize) * gridSize,
            p.y,
            Mathf.Round(p.z / gridSize) * gridSize
        );
    }
    
    private void ChangeDirection()
    {
        currentDir = PickRandomDirection(directions, excludeOpposite: true, currentDir);
    }

    private IEnumerator GridMove()
    {
        while (true)
        {
            Vector3 start = transform.position;
            Vector3 target = CalculateTargetPosition(start);

            if (IsPathBlocked(target))
            {
                currentDir = PickRandomDirection(directions, excludeOpposite: true, currentDir);
                yield return null;
                continue;
            }

            yield return StartCoroutine(MoveToTarget(start, target));
        }
    }
    
    private Vector3 CalculateTargetPosition(Vector3 start)
    {
        Vector3 target = start + currentDir * gridSize;
        target.x = Mathf.Round(target.x / gridSize) * gridSize;
        target.z = Mathf.Round(target.z / gridSize) * gridSize;
        return target;
    }
    
    private bool IsPathBlocked(Vector3 target)
    {
        Vector3 boxCenter = target + Vector3.up * 0.5f;
        Vector3 halfExtents = new Vector3(gridSize * 0.45f, 0.45f, gridSize * 0.45f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            halfExtents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            if (IsBlockingObject(hit))
            {
                currentDir = PickRandomDirection(directions, excludeOpposite: true, currentDir);
                return true;
            }

            if (hit.TryGetComponent<Cube>(out Cube cube))
            {
                HandleCubeInteraction(cube);
            }

            if (hit.CompareTag("Player"))
            {
                HandlePlayerInteraction(hit);
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsBlockingObject(Collider hit)
    {
        string tag = hit.tag;
        return tag == "Boundary" || tag == "Obstacle" || tag == "Enemy" || 
               tag == "EnemyGroup" || tag == "Diamond";
    }
    
    private void HandleCubeInteraction(Cube cube)
    {
        if (cube.IsFilled)
        {
            gridManager.RemoveCubeAt(cube);
        }
        else if (cube.CanHarm)
        {
            HandleHarmfulCollision();
        }
    }
    
    private void HandlePlayerInteraction(Collider player)
    {
        Haptics.Generate(HapticTypes.HeavyImpact);
        var renderer = player.gameObject.GetComponent<Renderer>();
        GameManager.Instance.SpawnDeathParticles(player.transform.gameObject, renderer.material.color);
        GameManager.Instance.CameraShake(0.35f, 0.15f);
        player.gameObject.SetActive(false);
        GameManager.Instance.Player.ClearUnfilledTrail();
        currentDir = PickRandomDirection(directions, excludeOpposite: true, currentDir);
        GameManager.Instance.LevelLose();
    }
    
    private IEnumerator MoveToTarget(Vector3 start, Vector3 target)
    {
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
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsValidCollision(collision))
            return;
            
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioManager.instance?.PlaySFXSound(3);
            Haptics.Generate(HapticTypes.HeavyImpact);
            collision.gameObject.SetActive(false);
            GameManager.Instance.LevelLose();
        }
    }
}
