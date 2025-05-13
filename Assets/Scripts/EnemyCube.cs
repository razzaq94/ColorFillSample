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

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Cube>(out Cube cube))
        {
            if(cube.IsFilled)
            {
                particle.Play();
                enemyCubeGroup.CubeDestroyed();
                gameObject.SetActive(false);
            }
            else
            {
                GameManager.Instance.LevelLose();
            }
        }
    }
}