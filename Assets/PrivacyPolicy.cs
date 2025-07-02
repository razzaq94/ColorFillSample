using UnityEngine;

public class PrivacyPolicy : MonoBehaviour
{
    public static PrivacyPolicy instance;

    public bool ShowOnce = false;
    private void Awake()
    {
        instance = this;
    }

    public static PrivacyPolicy ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("PrivacyPanel")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<PrivacyPolicy>();
        }
        return instance;
    }

    public void PrivacyPolicyLink()
    {
        Application.OpenURL("https://sites.google.com/view/colorin3d?usp=sharing");
    }
    public void Accept()
    {

    }



}
