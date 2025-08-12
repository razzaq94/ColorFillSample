using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

[HideMonoScript]
public class SlowDown : MonoBehaviour
{
    [Title("SLOWDOWN", null, titleAlignment: TitleAlignments.Centered)]

    private float slowdownFactor = 0.5f; 

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var enemies = FindObjectsByType<EnemyBehaviors>(FindObjectsSortMode.None);
            var cubeEaters = FindObjectsByType<CubeEater>(FindObjectsSortMode.None);
            var rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                enemy.speed *= slowdownFactor;
            }

            foreach (var cubeEater in cubeEaters)
            {
                cubeEater.speed *= slowdownFactor;  
            }

            foreach (var rb in rigidbodies)
            {
                if (rb.CompareTag("Player"))
                {
                    continue;  
                }

                rb.linearVelocity *= slowdownFactor;
            }

            GameManager.Instance.ResetSlowDown();

            Destroy(gameObject);
        }
    }

    
}
