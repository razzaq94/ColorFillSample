using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AEnemy : MonoBehaviour
{
    [SerializeField, DisplayAsString] public SpawnablesType enemyType;
    public float speed = 2f;
    public Rigidbody rb;
    [ReadOnly]
    public float defaultSpeed = 2f;
    public Renderer defaultRenderer;
    
    // Common collision handling
    protected bool hasCollidedThisFrame = false;
    protected int lastCollisionFrame = -1;
    
    // Common movement properties
    protected Vector3 currentDirection;
    protected GridManager gridManager;
    
    protected virtual void Start()
    {
        defaultSpeed = speed;
        GameManager.Instance.levelEnemies.Add(this);
        
        // Initialize common components
        InitializeComponents();
        
        // Apply slowdown if active
        if (GameManager.Instance.enemiesAreSlowed)
        {
            SlowDown(GameManager.Instance.slowdownFactor);
        }
    }
    
    protected virtual void InitializeComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        if (defaultRenderer == null)
        {
            defaultRenderer = GetComponent<Renderer>();
            if (defaultRenderer == null)
                defaultRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (gridManager == null)
            gridManager = GridManager.Instance;
    }
    
    public void SlowDown(float slowFactor)
    {
        speed *= slowFactor;
        if (rb != null)
        {
            rb.linearVelocity = rb.linearVelocity * slowFactor;
        }
    }

    public void NormalSpeed()
    {
        speed = defaultSpeed;
    }
    
    // Common collision handling methods
    protected virtual void HandlePlayerCollision()
    {
        if (GameManager.Instance.loosed || GameManager.Instance.hasTriggeredLose)
            return;
            
        AudioManager.instance?.PlaySFXSound(3);
        Haptics.Generate(HapticTypes.HeavyImpact);
        GameManager.Instance.CameraShake(0.35f, 0.15f);
        GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
        GameManager.Instance.Player.ClearUnfilledTrail();
        GameManager.Instance.LevelLose();
    }
    
    protected virtual void HandleHarmfulCollision()
    {
        if (GameManager.Instance.loosed || GameManager.Instance.hasTriggeredLose)
            return;
            
        AudioManager.instance?.PlaySFXSound(3);
        Haptics.Generate(HapticTypes.HeavyImpact);
        GameManager.Instance.CameraShake(0.35f, 0.15f);
        GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
        GameManager.Instance.LevelLose();
    }
    
    // Common movement utilities
    protected Vector3 PickRandomXZDirection(float minAxis = 0.3f)
    {
        Vector2 r;
        do
        {
            r = Random.insideUnitCircle.normalized;
        } while (Mathf.Abs(r.x) < minAxis || Mathf.Abs(r.y) < minAxis);

        return new Vector3(r.x, 0f, r.y).normalized;
    }
    
    protected Vector3 PickRandomDirection(Vector3[] directions, bool excludeOpposite = false, Vector3 currentDir = default)
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
    
    protected bool IsCollisionThisFrame()
    {
        if (Time.frameCount == lastCollisionFrame)
            return true;
        lastCollisionFrame = Time.frameCount;
        return false;
    }
    
    protected bool IsValidCollision(Collision collision)
    {
        return collision != null && !IsCollisionThisFrame();
    }

    private void OnDestroy()
    {
        GameManager.Instance.levelEnemies.Remove(this);
    }
}
