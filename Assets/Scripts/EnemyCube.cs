using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class EnemyCube : MonoBehaviour
{
    [Title("ENEMY-CUBE", null, titleAlignment: TitleAlignments.Centered)]
    public EnemyCubeGroup enemyCubeGroup;
    [FoldoutGroup("Particle Refernce")]
    public ParticleSystem particle;

    private void Start()
    {
        enemyCubeGroup = GetComponentInParent<EnemyCubeGroup>();
    }

    private void OnCollisionEnter(Collision collision)
    {
       
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                Destroy(gameObject);
                //particle.Play();
            }
            else
            {
                AudioManager.instance.PlaySFXSound(3);
                Haptics.Generate(HapticTypes.HeavyImpact);
                GameManager.Instance.LevelLose();
            }
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioManager.instance.PlaySFXSound(3);
            GameManager.Instance.LevelLose();
        }
    }
}
