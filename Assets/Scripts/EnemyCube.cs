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
        //if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Boundary"))
        //{
        //        print("wrt");

        //    if (enemyCubeGroup != null)
        //    {
        //        print("Reversing Direction");
        //        enemyCubeGroup.ReverseDirection();
        //        enemyCubeGroup.SetNextTarget();
        //    }

        //}
        if (collision.gameObject.CompareTag("Player"))
        {
            AudioManager.instance.PlaySFXSound(3);
            GameManager.Instance.LevelLose();
        }
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                particle.Play();
                Destroy(gameObject);
            }
            else
            {
                AudioManager.instance.PlaySFXSound(3);
                Haptics.Generate(HapticTypes.HeavyImpact);
                GameManager.Instance.LevelLose();
            }
        }
    }
}
