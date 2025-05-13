using UnityEngine;

public class EnemyBehaviors : MonoBehaviour
{
    public float speed = 5f;
    public EnemyType enemyType; 
    GridManager gridManager;
    Rigidbody rb;
    private Vector3 dir;

    public float bounceAngle = 3f;
    public float minInitial = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gridManager = GridManager.Instance;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.useGravity = false;

        dir = PickRandomXZDirection(minInitial);
        rb.linearVelocity = dir * speed;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = dir * speed;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
                    print("2");
            switch (enemyType)
            {
                case EnemyType.CubeDestroyer:
                    print(enemyType);
                    gridManager.RemoveCubeAt(cube);
                    Vector3 normal = collision.contacts[0].normal;
                    dir = Vector3.Reflect(dir, normal).normalized;

                    float jitter = Random.Range(-bounceAngle, bounceAngle);
                    dir = Quaternion.Euler(0f, jitter, 0f) * dir;
                    dir = dir.normalized;

                    rb.linearVelocity = dir * speed;
                    break;
                case EnemyType.CubeEater:
                    gridManager.RemoveCubeAt(cube);
                    break;
            }
        }
        if (collision.transform.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.LevelLose();
        }
        if (collision.transform.gameObject.CompareTag("Boundary"))
        {
            Vector3 normal = collision.contacts[0].normal;
            dir = Vector3.Reflect(dir, normal).normalized;

            float jitter = Random.Range(-bounceAngle, bounceAngle);
            dir = Quaternion.Euler(0f, jitter, 0f) * dir;
            dir = dir.normalized;

            rb.linearVelocity = dir * speed;
        }
    }
    private Vector3 PickRandomXZDirection(float minAxis)
    {
        Vector2 dir;
        do
        {
            dir = Random.insideUnitCircle.normalized;
        }
        while (Mathf.Abs(dir.x) < minAxis || Mathf.Abs(dir.y) < minAxis);

        return new Vector3(dir.x, 0f, dir.y).normalized;
    }
}
public enum EnemyType
{
    Killer,
    CubeDestroyer,
    CubeEater
}