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
            var enemies = FindObjectsOfType<EnemyBehaviors>();
            var cubeEaters = FindObjectsOfType<CubeEater>();
            var rigidbodies = FindObjectsOfType<Rigidbody>(); 

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

            //Time.timeScale = 0.5f;

            StartCoroutine(ResetSlowDownEffect(15f));

            Destroy(gameObject);
        }
    }

    private IEnumerator ResetSlowDownEffect(float delay)
    {
        // Wait for the specified delay time
        yield return new WaitForSecondsRealtime(delay);

        // Find and reset all affected objects
        var enemies = FindObjectsOfType<EnemyBehaviors>();
        var cubeEaters = FindObjectsOfType<CubeEater>();
        var rigidbodies = FindObjectsOfType<Rigidbody>();

        // Reset enemy and cube eater speeds
        foreach (var enemy in enemies)
        {
            enemy.speed /= slowdownFactor; // Reset enemy speed
        }

        foreach (var cubeEater in cubeEaters)
        {
            cubeEater.speed /= slowdownFactor; // Reset cube eater speed
        }

        // Reset the velocity of all objects except the player
        foreach (var rb in rigidbodies)
        {
            if (rb.CompareTag("Player"))
            {
                continue; // Skip the player
            }

            rb.linearVelocity /= slowdownFactor;  // Reset the velocity
        }

        // Reset time scale to normal
        Time.timeScale = 1f;
    }
}
