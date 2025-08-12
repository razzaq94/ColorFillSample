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
    public GameObject[] lifeIcons;
    public int currentLives = 3;
    public TextMeshProUGUI countDown;
    public bool isReviving = false;

    [Header("Life Animation")]
    public GameObject lifeFlyerPrefab;
    public GameObject diamondFlyerPrefab;
    public Transform lifeFlyerParent;

    public Image clockImage;
    public TextMeshProUGUI levelTimeText;

    public Transform iconTransform;

    public Button settings;
    public Button pause;
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
        NextLevelText.text = (GameHandler.Instance.CurrentLevel + 1).ToString();
        CurrentLevelText.text = GameHandler.Instance.CurrentLevel.ToString();
        ResetLives();
        Invoke(nameof(StartTextDelay), 0.7f);
    }

    public void StartTextDelay()
    {
        //SwipeToStart.gameObject.SetActive(true);
    }

    public void FillAmount(float amount) => Fill.DOFillAmount(amount, 0.25f);

    //public void StartGame() => StartScreen.DOFade(0.0f, 0.5f).OnComplete(() => StartScreen.gameObject.SetActive(false));

    public void LevelComplete(string msg)
    {
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;
        AudioManager.instance?.PlaySFXSound(0);
        deleteenemies();

        if (msg.Contains("Congrat"))
        {
            GameObject line = Instantiate(LinePrefab, GameObject.FindGameObjectWithTag("MainCanvas").transform.position, LinePrefab.transform.rotation);
            GameObject Confetti = Instantiate(confetti, confettiHolder.position, Quaternion.identity);
            RandomTextSpawner.SpawnRandomText();
            line.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);
            Confetti.transform.SetParent(confettiHolder);
            Destroy(line, 1.6f);
            Destroy(Confetti, 1.6f);
        }
        NextLevelText.text = (GameHandler.Instance.CurrentLevel + 1).ToString();
        StartCoroutine(WaitNPerform(1, () =>
        {
            ShowGamWinUI(msg);
        }));
    }
    IEnumerator WaitNPerform(float time, System.Action act)
    {
        yield return new WaitForSeconds(time);
        act?.Invoke();
    }

    public void deleteenemies()
    {
        var enemyBehaviors = FindObjectsByType<EnemyBehaviors>(FindObjectsSortMode.None);
        var cubeEaters = FindObjectsByType<CubeEater>(FindObjectsSortMode.None);
        var enemyCubes = FindObjectsByType<EnemyCube>(FindObjectsSortMode.None);
        var mine = FindObjectsByType<RotatingMine>(FindObjectsSortMode.None);
        foreach (var enemy in enemyBehaviors)
        {
            Destroy(enemy.gameObject);
        }
        foreach (var enemy in cubeEaters)
        {
            Destroy(enemy.gameObject);
        }
        foreach (var enemy in enemyCubes)
        {
            Destroy(enemy.gameObject);
        }
        foreach (var enemy in mine)
        {
            Destroy(enemy.gameObject);
        }
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
        Player.Instance.collisionCollider.enabled = false;

        AudioManager.instance?.PlaySFXSound(1);
        print("Player Crashed");
        if (currentLives > 0)
        {
            LoseLife();
        }
        else
        {
            StartCoroutine(ShowGameLoseUICrashAfterDelay());
        }
    }

    private IEnumerator ShowGameLoseUICrashAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if (currentLives <= 0 && !isReviving)
        {
            ShowGameLoseUICrash();
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
    private void ShowGamWinUI(string msg)
    {
        GameWinScreen.ShowUI(msg);
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
        GameHandler.Instance.CurrentLives++;
        currentLives = GameHandler.Instance.CurrentLives;
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
        currentLives = GameHandler.Instance.CurrentLives;
        UpdateLifeIcons();
    }


    private float lastLifeLostTime = -5f;
    private bool isLosingLife = false;

    public void LoseLife()
    {
        if (currentLives <= 0 || isReviving || isLosingLife)
            return;

        if (Time.unscaledTime - lastLifeLostTime < 1f)
            return;

        lastLifeLostTime = Time.unscaledTime;
        isLosingLife = true;
        Player.Instance.gameObject.SetActive(false);
        currentLives--;
        GameHandler.Instance.LoseLife();
        if (currentLives < 0)
        {
            GameManager.Instance.loosed = false;
            Time.timeScale = 1f;
            GameManager.Instance.Player.ClearUnfilledTrail();
            Invoke(nameof(ShowGameLoseUICrash), 0.1f);
        }
        else
        {
            StartCoroutine(ReviveCountdownRoutine());
        }


        StartCoroutine(ResetLifeLossLock());
    }

    private IEnumerator ResetLifeLossLock()
    {
        yield return new WaitForSecondsRealtime(1f); // same as cooldown
        isLosingLife = false;
    }

    public void GainLife()
    {
        if (currentLives >= lifeIcons.Length)
            return;

        currentLives++;
        GameHandler.Instance.GainLife();
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
        settings.interactable = false;
        pause.interactable = false;
        countDown.gameObject.SetActive(true);
        GameManager.Instance.Player.ClearUnfilledTrail();

        Time.timeScale = 0f;

        GameManager.Instance.ReviveFromLife();

        // Countdown loop
        for (int i = 3; i > 0; i--)
        {
            countDown.text = i.ToString("00");
            yield return new WaitForSecondsRealtime(1f);
        }

        countDown.text = "00";
        countDown.gameObject.SetActive(false);
        if (!GameManager.Instance.Player.gameObject.activeInHierarchy) 
        {
            GameManager.Instance.Player.gameObject.SetActive(true);
        }
        AnimateLifeFromUIToPlayer(GameManager.Instance.Player.transform.position);

        //GameManager.Instance.Player.enabled = true;

        Time.timeScale = 1f;
        if(!GameManager.Instance.Player.gameObject.activeInHierarchy)
            GameManager.Instance.Player.gameObject.SetActive(true);
        AudioManager.instance?.PlayBGMusic(0);
        settings.interactable = true;
        pause.interactable = true;
        Player.Instance.collisionCollider.enabled = true;
        
        isReviving = false;
    }


    public void AnimateLifeGainFromWorld(Vector3 worldPosition)
    {
        int nextLifeIndex = Mathf.Clamp(currentLives, 0, lifeIcons.Length - 1);
        if (lifeIcons[nextLifeIndex].activeSelf)
        {
            Debug.LogWarning("All lives are already full. Animation skipped.");
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        GameObject flyer = Instantiate(lifeFlyerPrefab, screenPos, Quaternion.identity, lifeFlyerParent);
        RectTransform flyerRect = flyer.GetComponent<RectTransform>();

        flyerRect.SetParent(lifeFlyerParent);
        flyerRect.localScale = Vector3.one;

        Vector3 targetPos = lifeIcons[nextLifeIndex].GetComponent<RectTransform>().position;

        flyerRect.DOMove(targetPos, 0.65f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                Destroy(flyer);
                GainLife();
            });
    }

    public void AnimateLifeFromUIToPlayer(Vector3 playerPosition)
    {
        int nextLifeIndex = Mathf.Clamp(currentLives - 1, 0, lifeIcons.Length - 1);
        if (!lifeIcons[nextLifeIndex].activeSelf)
        {
            Debug.LogWarning("Life icon is not active, animation skipped.");
            return;
        }

        Vector3 uiPosition = lifeIcons[nextLifeIndex + 1].GetComponent<RectTransform>().position;

        Vector3 playerUIPosition = Camera.main.WorldToScreenPoint(playerPosition);

        GameObject flyer = Instantiate(lifeFlyerPrefab, uiPosition, Quaternion.identity, lifeFlyerParent);
        RectTransform flyerRect = flyer.GetComponent<RectTransform>();

        flyerRect.SetParent(lifeFlyerParent);
        flyerRect.localScale = Vector3.one;
        UpdateLifeIcons();
        flyerRect.DOMove(playerUIPosition, 0.65f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                Destroy(flyer);

                //LoseLife();      
            });
    }

    public Transform diamondPos;
    public void AnimateDiamondGainFromWorld(Vector3 worldPosition)
    {

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        GameObject flyer = Instantiate(diamondFlyerPrefab, screenPos, Quaternion.identity, lifeFlyerParent);
        RectTransform flyerRect = flyer.GetComponent<RectTransform>();

        flyerRect.SetParent(lifeFlyerParent);
        flyerRect.localScale = Vector3.one;

        Vector3 targetPos = diamondPos.transform.position;

        flyerRect.DOMove(targetPos, 0.65f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                Destroy(flyer);
                GameHandler.Instance.TotalDiamonds++;
                Diamonds.text = GameHandler.Instance.TotalDiamonds.ToString();
            });
    }



    public void ShowClockAndTime(float levelTime, Transform iconTransform)
    {

        levelTimeText.gameObject.transform.parent.gameObject.SetActive(true);
        levelTimeText.text = levelTime.ToString("F2");

        GameObject clock = Instantiate(clockImage.gameObject, lifeFlyerParent);
        clock.SetActive(true);
        clock.transform.position = lifeFlyerParent.position;

        VibrateClock(clock.transform);

        StartCoroutine(AnimateClockToIcon(clock.transform, iconTransform));
    }

    private void VibrateClock(Transform clockTransform)
    {
        clockTransform.DOShakeScale(1f, 0.1f, 10, 90f, false, DG.Tweening.ShakeRandomnessMode.Harmonic)
            .SetEase(Ease.Linear);
    }

    private IEnumerator AnimateClockToIcon(Transform clockTransform, Transform iconTransform)
    {
        yield return new WaitForSecondsRealtime(1f);

        GameManager.Instance._gameRunning = true;


        levelTimeText.gameObject.transform.parent.gameObject.SetActive(false);

        Vector3 targetPos = iconTransform.position;
        Vector3 targetScale = new Vector3(0.18f, 0.18f, 0.18f);

        clockTransform.DOMove(targetPos, 1f) 
            .SetEase(Ease.InOutQuad)  
            .OnStart(() =>
            {
                clockTransform.DOScale(targetScale, 1f)  
                    .SetEase(Ease.InOutQuad); 
            })
            .OnComplete(() =>
            {
                clockTransform.localScale = targetScale;
            });
    }




}
