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
    public bool collided = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collided) return; // ☂️ central guard

        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            collided = true;

            if (cube.IsFilled)
            {
                gameObject.SetActive(false);
                AudioManager.instance?.PlaySFXSound(2);
                GameManager.Instance.SpawnDeathParticles(transform.gameObject, _renderer.material.color);
                Destroy(gameObject, 1f);
            }
            else if (cube.CanHarm)
            {
                if (!GameManager.Instance.loosed)
                {
                    AudioManager.instance?.PlaySFXSound(3);
                    Haptics.Generate(HapticTypes.HeavyImpact);
                    GameManager.Instance.CameraShake(0.35f, 0.15f);
                    GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                    GameManager.Instance.LevelLose();
                }
            }

            Invoke(nameof(enableAgain), 0.6f);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            collided = true;

            if (!GameManager.Instance.loosed)
            {
                GameManager.Instance.Player.IsMoving = false;
                GameManager.Instance.Player.transform.position = GameManager.Instance.Player.RoundPos();
                Haptics.Generate(HapticTypes.HeavyImpact);
                GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                GameManager.Instance.CameraShake(0.35f, 0.15f);
                GameManager.Instance.Player.gameObject.SetActive(false);
                GameManager.Instance.Player.collisionCollider.enabled = false;
                GameManager.Instance.Player.enabled = false;
                GameManager.Instance.Player.ClearUnfilledTrail();
                AudioManager.instance?.PlaySFXSound(3);

                Invoke(nameof(HandleLevelLoseCrash), 0.3f);
            }
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
