using UnityEngine;

public class EnemyCubeGroup : MonoBehaviour
{
    public EnemyCube[] Cubes = null;
    public int Detected = 0;

    private void Start() => Cubes = GetComponentsInChildren<EnemyCube>();

    public void Restart()
    {
        Detected = 0;
        gameObject.SetActive(true);
        for (int i = 0; i < Cubes.Length; i++)
            Cubes[i].gameObject.SetActive(true);
    }

    public void CubeDestroyed()
    {
        Detected++;
        if (Detected == Cubes.Length)
        {
            AudioManager.instance?.PlaySFXSound(3);
            gameObject.SetActive(false);
        }
    }
}
