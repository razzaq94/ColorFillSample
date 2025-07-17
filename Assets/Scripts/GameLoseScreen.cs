using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoseScreen : MonoBehaviour
{
    public static GameLoseScreen instance;
    [SerializeField] private TextMeshProUGUI countdownText;
    public GameObject TimeOutOptions;
    public GameObject CrashOptions;


    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {

        StartCoroutine(CountdownRoutine());
    }
    public static GameLoseScreen ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("GameLoseScreen")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<GameLoseScreen>();
        }
        return instance;
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 9; i >= 0; i--)
        {
            countdownText.text = i.ToString("00");
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "00";
        RestartButton();    
        Destroy(gameObject);
    }

    public void ShowCrashOptions()
    {
        TimeOutOptions?.SetActive(false);
        CrashOptions?.SetActive(true);
    }

    public void ShowTimeOutOptions()
    {
        TimeOutOptions?.SetActive(true);
        CrashOptions?.SetActive(false);
    }

    public void RestartButton()
    {
        AudioManager.instance.PlayUISound(0);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;

    }
    public void OnClick_AddExtraMinute()
    {
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.AddTime(60); 
            GameManager.Instance.ResumeAfterAd();
        });
    }

    public void OnClick_ReplayLevel()
    {
        AdManager_Admob.instance.ShowInterstitialAd();
        GameManager.Instance.Replay();
    }

    public void OnClick_SkipLevel()
    {
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.LevelComplete();
        });
    }
    public void OnClick_Revive()
    {
        UIManager.Instance.currentLives++;
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.ReviveFromCollision();
        });
        GameManager.Instance.Player.gameObject.SetActive(true);
        GameManager.Instance.Player.enabled = true;
    }

    public void OnClick_CrashRestart()
    {
        AdManager_Admob.instance.ShowInterstitialAd();
        GameManager.Instance.Replay();
    }

    public void OnClick_CrashSkip()
    {
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.LevelComplete();
        });
    }

    public void ClosePanael()
    {
        Destroy(gameObject);
    }
}
