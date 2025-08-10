using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class RotatingMine : MonoBehaviour
{
    [Title("RotatingMine", null, titleAlignment: TitleAlignments.Centered)]

    public SpawnablesType SpawnablesType;

    bool gotHit = false;
    private void Start()
    {
        SpawnablesType = SpawnablesType.RotatingMine;   
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !gotHit)
        {
            gotHit = true;
            //AudioManager.instance?.PlaySound(0);
            GameManager.Instance.Player.IsMoving  = false; 
            GameManager.Instance.Player.gameObject.SetActive(false);
            GameManager.Instance.CameraShake(0.15f, 0.15f);
            GameManager.Instance.SpawnDeathParticles(collision.gameObject, collision.gameObject.GetComponent<Renderer>().material.color);
            Invoke(nameof(HandelLose), 0.3f); 
        }
    }

    public void HandelLose()
    {
        UIManager.Instance.LevelLoseCrash();
        gotHit = false; // Reset the hit state for future collisions
    }
}
