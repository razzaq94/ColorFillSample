using Sirenix.OdinInspector;
using UnityEngine;


[HideMonoScript]
public class Diamond : MonoBehaviour
{
    [Title("DIAMOND", null, titleAlignment: TitleAlignments.Centered)]
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
