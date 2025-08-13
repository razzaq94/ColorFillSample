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
    public virtual void Start()
    {
        defaultSpeed = speed;
        GameManager.Instance.levelEnemies.Add(this);
        if (GameManager.Instance.enemiesAreSlowed)
        {
            SlowDown(GameManager.Instance.slowdownFactor);
        }
        if (defaultRenderer == null)
        {
            defaultRenderer = GetComponent<Renderer>();
        }
        if (defaultRenderer == null)
        {
            defaultRenderer = GetComponentInChildren<Renderer>();
        }
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

    private void OnDestroy()
    {
        GameManager.Instance.levelEnemies.Remove(this);
    }
}
