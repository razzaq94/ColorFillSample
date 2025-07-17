using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class EnemyCube : MonoBehaviour
{
    [Title("ENEMY-CUBE", null, titleAlignment: TitleAlignments.Centered)]
    public EnemyCubeGroup enemyCubeGroup;
    [FoldoutGroup("Particle Refernce")]

    public Renderer _renderer;

    private void Start()
    {
        enemyCubeGroup = GetComponentInParent<EnemyCubeGroup>();
        _renderer = GetComponent<Renderer>();
        if (_renderer)
            _renderer.material.color = GameManager.Instance.EnemyCubeColor;
    }
    bool collided = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube) && !collided)
        {
            collided = true;
            if (cube.IsFilled)
            {
                gameObject.SetActive(false);
                GameManager.Instance.SpawnDeathParticles(transform.gameObject, _renderer.material.color);
                Destroy(gameObject, 1f);
            }
            else
            {
                AudioManager.instance.PlaySFXSound(3);
                Haptics.Generate(HapticTypes.HeavyImpact);
                GameManager.Instance.CameraShake(0.35f, 0.15f);
                GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                GameManager.Instance.LevelLose();
            }
            Invoke(nameof(enableAgain), 0.6f);
        }
        else if (collision.gameObject.CompareTag("Player") && !collided)
        {
            collided = true;
            AudioManager.instance.PlaySFXSound(3);
            Invoke(nameof(HandleLevelLoseCrash), 0.5f);
        }
    }

    void enableAgain()
    {
        collided = false;
    }

    public void HandleLevelLoseCrash() 
    {
        UIManager.Instance.LevelLoseCrash();
        collided = false;
    }
}
