using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class RotatingMine : AEnemy
{
    [Title("RotatingMine", null, titleAlignment: TitleAlignments.Centered)]

    bool gotHit = false;
    
    protected override void Start()
    {
        base.Start();
        enemyType = SpawnablesType.RotatingMine;   
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsValidCollision(collision) || gotHit)
            return;
            
        if (collision.gameObject.CompareTag("Player"))
        {
            HandleMineCollision(collision);
        }
    }
    
    private void HandleMineCollision(Collision collision)
    {
        gotHit = true;
        GameManager.Instance.Player.IsMoving = false; 
        GameManager.Instance.Player.gameObject.SetActive(false);
        GameManager.Instance.CameraShake(0.15f, 0.15f);
        GameManager.Instance.SpawnDeathParticles(collision.gameObject, collision.gameObject.GetComponent<Renderer>().material.color);
        Invoke(nameof(HandleLose), 0.3f); 
    }

    public void HandleLose()
    {
        UIManager.Instance.LevelLoseCrash();
        gotHit = false; // Reset the hit state for future collisions
    }
}
