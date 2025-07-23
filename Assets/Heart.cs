using Sirenix.OdinInspector;
using UnityEngine;

public class Heart : MonoBehaviour
{
    [Title("Heart", null, titleAlignment: TitleAlignments.Centered)]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //AudioManager.instance?.PlaySound(0);
            Destroy(gameObject);
            UIManager.Instance.AnimateLifeGainFromWorld(transform.position);

        }
    }
}
