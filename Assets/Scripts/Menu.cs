using UnityEngine;

public class Menu : MonoBehaviour
{
    public static Menu instance;
    public Animator animator;
    private void Awake()
    {
        instance = this;
    }
    public static Menu ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("MainMenu")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<Menu>();
        }

        return instance;
    }


    public void StartGame()
    {
        animator.Play("PanelsAnim");
        Destroy(gameObject, 0.3f);
    }   
}
