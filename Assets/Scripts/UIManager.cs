using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

[HideMonoScript]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Title("UI-MANAGER", null, titleAlignment: TitleAlignments.Centered)]

    public Image StartScreen;
    public Image Fill;

    public TextMeshProUGUI CurrentLevelText;
    public TextMeshProUGUI Diamonds;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI NextLevelText;
    public TextMeshProUGUI SwipeToStart;

    public GameObject LinePrefab;
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
        Invoke(nameof(StartTextDelay), 0.7f);
    }

    public void StartTextDelay()
    {
        SwipeToStart.gameObject.SetActive(true);
    }

    public void FillAmount(float amount) => Fill.DOFillAmount(amount, 0.25f);

    public void StartGame() => StartScreen.DOFade(0.0f, 0.5f).OnComplete(() => StartScreen.gameObject.SetActive(false));

    public void LevelComplete()
    {
        AudioManager.instance.PlaySFXSound(0);
        GameObject line = Instantiate(LinePrefab, GameObject.FindGameObjectWithTag("MainCanvas").transform.position, LinePrefab.transform.rotation);
        line.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);
        Invoke(nameof(ShowGamWinUI), 1f);
        Destroy(line, 1.6f);
    }

    public void LevelLose()
    {
        AudioManager.instance?.PlaySFXSound(1);
        //GameObject line = Instantiate(LinePrefab, GameObject.FindGameObjectWithTag("MainCanvas").transform.position, LinePrefab.transform.rotation);
        //line.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);
        Invoke(nameof(ShowGameLoseUI), 0.1f);
        //Destroy(line, 1.6f);
    }

    private void ShowGameLoseUI()
    {
        GameLoseScreen.ShowUI();
    }private void ShowGamWinUI()
    {
        GameWinScreen.ShowUI();
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

}
