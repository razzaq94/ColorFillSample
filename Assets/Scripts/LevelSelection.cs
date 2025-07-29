using System.Linq;
using UnityEngine;

public class LevelSelection : MonoBehaviour
{
    public static LevelSelection instance;
    [Header("Prefab & Container")]
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Transform contentParent;



    public static LevelSelection ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("LevelSelection")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("SelectionParent").transform, false);

            instance = obj.GetComponent<LevelSelection>();
        }

        return instance;
    }

    private void Start()
    {
        int first = GameManager.Instance.firstLevelBuildIndex;
        int total = GameManager.Instance.TotalLevels;

        for (int i = 0; i < total; i++)
        {
            int buildIndex = first + i;
            bool isUnlocked;

            if (i == 0)
            {
                isUnlocked = true;
            }
            else
            {
                int prevBuildIndex = first + i - 1;
                isUnlocked = GameManager.Instance.IsLevelCompleted(prevBuildIndex);
            }

            var btnGO = Instantiate(levelButtonPrefab, contentParent);
            var lvlBtn = btnGO.GetComponent<LevelButton>();
            lvlBtn.Initialize(buildIndex, isUnlocked);
        }
    }

    public void CloseButton()
    {
        AudioManager.instance?.PlayUISound(0);
        if(Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        Menu.ShowUI();
        Menu.instance.animator.Play("Return");
        Destroy(gameObject);

    }
}
