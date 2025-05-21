using UnityEngine;
using System.Collections;

public class AddTime : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.AddTime(15); 
            Destroy(gameObject);
        }
    }

    
}