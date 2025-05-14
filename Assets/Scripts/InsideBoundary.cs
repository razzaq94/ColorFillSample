using UnityEngine;
using static UnityEngine.ParticleSystem;

public class InsideBoundary : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Cube>(out Cube cube))
        {
            if (cube.IsFilled)
            {
                cube.gameObject.SetActive(false);
            }
        }
    }
}
