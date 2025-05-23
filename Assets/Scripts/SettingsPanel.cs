using UnityEngine;

public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel instance;

    private void Awake()
    {
        instance = this;
    }
    public static SettingsPanel ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("Settings")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<SettingsPanel>();
        }

        return instance;
    }
}
