using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

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

    public void NextButton() 
    {
        AudioManager.instance.PlayUISound(0);
        int current = SceneManager.GetActiveScene().buildIndex;
        int first = GameManager.Instance.firstLevelBuildIndex;
        int total = GameManager.Instance.TotalLevels;
        int last = first + total - 1;

        if (current < last)
        {
            SceneManager.LoadScene(current + 1);
        }
        else
        {
        SceneManager.LoadScene(0);
        }
    }
    public void RestartButton() 
    {
        AudioManager.instance.PlayUISound(0);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;
    }
    public void MenuButton()
    {
        AudioManager.instance.PlayUISound(0);

        SceneManager.LoadScene(0);
    }

}
