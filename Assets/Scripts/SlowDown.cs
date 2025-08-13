using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

[HideMonoScript]
public class SlowDown : MonoBehaviour
{
    [Title("SLOWDOWN", null, titleAlignment: TitleAlignments.Centered)]

    private float slowdownFactor = 0.5f;
    private float effectTime = 10;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.SlowEnemies(slowdownFactor, effectTime);

            Destroy(gameObject);
        }
    }

    
}
