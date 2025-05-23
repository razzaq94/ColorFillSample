using UnityEngine;

public class GameWinScreen : MonoBehaviour
{
    public static GameWinScreen instance;

    private void Awake()
    {
        instance = this;
    }
    public static GameWinScreen ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("GameWinScreen")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<GameWinScreen>();
        }

        return instance;
    }
}
