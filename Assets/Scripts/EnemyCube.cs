using UnityEngine;

public class EnemyCube : MonoBehaviour
{
    private EnemyCubeGroup enemyCubeGroup;
    public ParticleSystem particle;
    private void Start()
    {
        enemyCubeGroup = GetComponentInParent<EnemyCubeGroup>();
        //particleManager = FindFirstObjectByType<ParticleManager>();  
    }
    public void OnCubeHit(Cube cube)
    {
        Destroy(gameObject);
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                particle.Play();
                Destroy(gameObject);
            }
            else
            {
                GameManager.Instance.LevelLose();
            }
        }
    }
}