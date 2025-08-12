using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class GameWinScreen : MonoBehaviour
{
    public static GameWinScreen instance;
    public TextMeshProUGUI completText;
    private void Awake()
    {
        instance = this;
    }
    public static GameWinScreen ShowUI(string msg)
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("GameWinScreen")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<GameWinScreen>();
            instance.completText.text = msg;
        }

        return instance;
    }

    public void NextButton() 
    {
        AudioManager.instance?.PlayUISound(0);
        int current = SceneManager.GetActiveScene().buildIndex;
        int first = GameManager.Instance.firstLevelBuildIndex;
        int total = GameManager.Instance.TotalLevels;
        int last = first + total - 1;
        GameHandler.Instance.adCount = 0;

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
        AudioManager.instance?.PlayUISound(0);
        AdManager_Admob.instance.ShowInterstitialAd();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;
    }
    public void MenuButton()
    {
        AudioManager.instance?.PlayUISound(0);

        SceneManager.LoadScene(0);
    }

}
