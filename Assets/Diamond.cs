using UnityEngine;

public class Diamond : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //AudioManager.instance?.PlaySound(0);
            Destroy(gameObject);
            GameManager.Instance.Diamonds++;
            UIManager.Instance.Diamonds.text = GameManager.Instance.Diamonds.ToString();    
        }
    }
}
