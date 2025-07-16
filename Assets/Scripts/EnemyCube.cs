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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
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
                //GameManager.Instance.CameraShake(0.35f, 0.15f);
                //GameManager.Instance.SpawnDeathParticles(GameManager.Instance.Player.transform.gameObject, GameManager.Instance.Player.material.color);
                GameManager.Instance.LevelLose();
            }
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioManager.instance.PlaySFXSound(3);
            Invoke(nameof(HandleLevelLose), 0.5f);
        }
    }

    public void HandleLevelLose() 
    {
        GameManager.Instance.LevelLose();

    }
}
