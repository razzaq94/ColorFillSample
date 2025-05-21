using UnityEngine;
using System.Collections;

public class SlowDown : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var enemy = FindAnyObjectByType<EnemyBehaviors>();
            var cubeEater = FindAnyObjectByType<CubeEater>();
            if (enemy != null)
            {
                enemy.speed = enemy.speed / 2;
            }
            if (cubeEater != null)
            {
                cubeEater.speed = cubeEater.speed / 2;
            }
            var player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.moveSpeed = player.moveSpeed / 2;
            }
            StartCoroutine(ResetTimeScaleAfterDelay(15f));
            Destroy(gameObject);
        }
    }

    private IEnumerator ResetTimeScaleAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        var enemy = FindAnyObjectByType<EnemyBehaviors>();
        if (enemy != null)
        {
            enemy.speed *= 2;
        }
        var cubeEater = FindAnyObjectByType<CubeEater>();
        if (cubeEater != null)
        {
            cubeEater.speed *= 2;
        }
        var player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.moveSpeed *= 2;
        }
    }
}

