using UnityEngine;

public class EnemyCube : MonoBehaviour
{
    private EnemyCubeGroup enemyCubeGroup;
    //public ParticleManager particleManager;
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
                //particleManager?.PlayParticle("Destroy", transform.position);
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