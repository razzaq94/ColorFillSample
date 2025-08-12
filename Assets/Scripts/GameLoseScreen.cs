using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLoseScreen : MonoBehaviour
{
    public static GameLoseScreen instance;
    [SerializeField] private TextMeshProUGUI countdownText;
    public GameObject TimeOutOptions;
    public GameObject CrashOptions;
    public Button reviveBTN;
    Coroutine countdownCoroutine;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        Time.timeScale = 0;
        if (GameManager.Instance.reviveUsed)
        {
            //reviveBTN.interactable = false;
        }
        if (!AdManager_Admob.instance.IsRewardedAdLoaded())
        {
            reviveBTN.gameObject.SetActive(false);
        }
        countdownCoroutine = StartCoroutine(CountdownRoutine());

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
        if (!UIManager.Instance.isReviving)
        {
            AdManager_Admob.instance.ShowInterstitialAd();
            GameManager.Instance.Replay();
        }

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
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        AudioManager.instance?.PlayUISound(0);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;

    }
    public void OnClick_AddExtraMinute()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.AddTime(60);
            GameManager.Instance.ResumeAfterAd();
        });
        //if (Time.timeScale == 0)
        //    Time.timeScale = 1.0f;
    }

    public void OnClick_ReplayLevel()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);
        
        GameManager.Instance.Replay();
    }



    public void OnClick_CrashRestart()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        if (GameHandler.Instance.adCount == 0)
        {
            AdManager_Admob.instance.ShowInterstitialAd();
            GameHandler.Instance.adCount = 1;
        }

        GameManager.Instance.Replay();

        if (Time.timeScale == 0)
            Time.timeScale = 1f;
    }

    public void OnClick_SkipLevel()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.LevelComplete();
        });
    }
    public void OnClick_Revive()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            GameManager.Instance.reviveUsed = true;

            reviveBTN.interactable = false;
            ClosePanael();
            //print("AdLoaded ");
            GameManager.Instance.ReviveFromLife();
            //UIManager.Instance.GainLife();
        });
        //if(Time.timeScale == 0)
        //    Time.timeScale = 1.0f;  
    }

    public void OnClick_CrashSkip()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
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
