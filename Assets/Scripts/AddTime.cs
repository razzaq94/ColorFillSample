using UnityEngine;
using Sirenix.OdinInspector;

[HideMonoScript]
public class AddTime : MonoBehaviour
{
    [Title("ADDTIME", null, titleAlignment: TitleAlignments.Centered)]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.AddTime(15); 
            Destroy(gameObject);
        }
    }
}