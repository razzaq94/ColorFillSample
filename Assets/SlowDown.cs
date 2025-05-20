using UnityEngine;

public class SlowDown : MonoBehaviour
{



    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
           Time.timeScale = 0.5f; 
            Destroy(gameObject);
        }
    }
}
