using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

[HideMonoScript]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Title("UI-MANAGER", null, titleAlignment: TitleAlignments.Centered)]

    //public Image StartScreen;
    public Image Fill;

    public TextMeshProUGUI CurrentLevelText;
    public TextMeshProUGUI Diamonds;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI NextLevelText;

    public GameObject LinePrefab;
    public GameObject confetti;
    public Transform confettiHolder;
    public RandomTextSpawner RandomTextSpawner;

    [Header("Lives UI")]
    public GameObject[] lifeIcons; // Assign 3 GameObjects in Inspector representing life icons
    public int currentLives = 3;
    public TextMeshProUGUI countDown;
    public bool isReviving = false;


    private void Awake()
    {
        Instance = this;
    }
    public void Start()
    {
        if (GameManager.Instance.Level.isTimeless)
        {
            timerText.text = "Timelesss";
        }

        Fill.fillAmount = 0f;
        NextLevelText.text = (GameManager.Instance.GetCurrentLevel + 1).ToString();
        CurrentLevelText.text = GameManager.Instance.GetCurrentLevel.ToString();
        ResetLives();
        Invoke(nameof(StartTextDelay), 0.7f);
    }

    public void StartTextDelay()
    {
        //SwipeToStart.gameObject.SetActive(true);
    }

    public void FillAmount(float amount) => Fill.DOFillAmount(amount, 0.25f);

    //public void StartGame() => StartScreen.DOFade(0.0f, 0.5f).OnComplete(() => StartScreen.gameObject.SetActive(false));

    public void LevelComplete()
    {
        AudioManager.instance.PlaySFXSound(0);
        GameObject line = Instantiate(LinePrefab, GameObject.FindGameObjectWithTag("MainCanvas").transform.position, LinePrefab.transform.rotation);
        GameObject Confetti = Instantiate(confetti, confettiHolder.position, Quaternion.identity);
        RandomTextSpawner.SpawnRandomText();
        line.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);
        Confetti.transform.SetParent(confettiHolder);
        Invoke(nameof(ShowGamWinUI), 1f);
        Destroy(line, 1.6f);
        Destroy(Confetti, 1.6f);
    }

    public void LevelLoseTimeOut()
    {
        AudioManager.instance?.PlaySFXSound(1);
        //GameObject line = Instantiate(LinePrefab, GameObject.FindGameObjectWithTag("MainCanvas").transform.position, LinePrefab.transform.rotation);
        //line.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

        Invoke(nameof(ShowGameLoseUITimeOut), 0.1f);

        //Destroy(line, 1.6f);
    }
    public void LevelLoseCrash()
    {
        Player.Instance.gameObject.SetActive(false);
        AudioManager.instance?.PlaySFXSound(1);

        if (currentLives > 1)
        {
            LoseLife(); 
        }
        else
        {
            LoseLife();
            Invoke(nameof(ShowGameLoseUICrash), 0.1f);
        }
    }

    private void ShowGameLoseUITimeOut()
    {
        
        GameLoseScreen.ShowUI();
        GameLoseScreen.instance.ShowTimeOutOptions();
    }
    private void ShowGameLoseUICrash()
    {
        GameLoseScreen.ShowUI();
        GameLoseScreen.instance.ShowCrashOptions();
    }
    private void ShowGamWinUI()
    {
        GameWinScreen.ShowUI();
    }
   

    public void ShowDeathAdOptions()
    {


    }

    public void ReturnToMenu()
    {
        Menu.ShowUI();
    }
    public void ResetartLevel()
    {
        GameManager.Instance.Replay();
    }

    public void SettingsButton()
    {
        Time.timeScale = 0f;
        SettingsPanel.ShowUI();
    }

    public void PauseButton()
    {
        Time.timeScale = 0f;
        PausePanel.ShowUI();
    }

    public void ShopButton()
    {
        //ShopPanel.ShowUI();
    }
    public void ResetLives()
    {
        currentLives = 3;
        UpdateLifeIcons();
    }

    public void LoseLife()
    {
        if (currentLives <= 0 || isReviving)
            return;

        currentLives--;
        UpdateLifeIcons();

        if (currentLives <= 0)
        {
            GameManager.Instance.loosed = false;
            Time.timeScale = 1f; 
            GameManager.Instance.LevelLose(); // Final loss
        }
        else
        {
            StartCoroutine(ReviveCountdownRoutine());
        }
    }

    public void GainLife()
    {
        if (currentLives >= lifeIcons.Length)
            return;

        currentLives++;
        UpdateLifeIcons();
    }

    private void UpdateLifeIcons()
    {
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].SetActive(i < currentLives);
    }

    [ContextMenu("Revive Button")]
    public void reviveButton()
    {
        StartCoroutine(ReviveCountdownRoutine());
    }
    private IEnumerator ReviveCountdownRoutine()
    {
        isReviving = true;
        countDown.gameObject.SetActive(true);
        Time.timeScale = 0f;

        GameManager.Instance.ReviveFromLife();
        // Countdown loop
        for (int i = 3; i > 0; i--)
        {
            countDown.text = i.ToString("00");
            yield return new WaitForSecondsRealtime(1f); // Countdown waits in real time, not affected by Time.timeScale
        }

        countDown.text = "00";  // Optional: set to "00" after countdown
        countDown.gameObject.SetActive(false);

        //GameManager.Instance.Player.gameObject.SetActive(true);
        //GameManager.Instance.Player.enabled = true;

        Time.timeScale = 1f;
        isReviving = false;
    }
}
