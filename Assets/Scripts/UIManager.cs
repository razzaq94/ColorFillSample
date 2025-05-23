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


    private void Awake()
    {
        Instance = this;
    }
    public void Start()
    {
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
        GameWinScreen.ShowUI(); 
    }

    public void LevelLose()
    {
        GameLoseScreen.ShowUI();
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
        SettingsPanel.ShowUI();
    }

    public void ShopButton()
    {
        //ShopPanel.ShowUI();
    }

}
