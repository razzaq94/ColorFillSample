using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Image StartScreen;
    public Image Fill;

    public TextMeshProUGUI CurrentLevelText;
    public TextMeshProUGUI Diamonds;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI NextLevelText;
    public TextMeshProUGUI LevelWinText;
    public TextMeshProUGUI LevelFailText;

    public GameObject LevelWinPanel;
    public GameObject LevelLosePanel;
    public Button RetryLevelBtn;
    public Button NextLevelBtn;

    private void Awake()
    {
        Instance = this;
    }
    public void Start()
    {
        LevelWinPanel.SetActive(false);
        LevelLosePanel.SetActive(false);
        
        Fill.fillAmount = 0f;

        NextLevelBtn.onClick.RemoveAllListeners();
        NextLevelBtn.onClick.AddListener(GameManager.Instance.Replay);
        
        RetryLevelBtn.onClick.RemoveAllListeners();
        RetryLevelBtn.onClick.AddListener(GameManager.Instance.Replay);
        
        NextLevelText.text = (GameManager.Instance.GetCurrentLevel + 1).ToString();
        CurrentLevelText.text = GameManager.Instance.GetCurrentLevel.ToString();
    }

    public void FillAmount(float amount) => Fill.DOFillAmount(amount, 0.25f);

    public void StartGame() => StartScreen.DOFade(0.0f, 0.5f).OnComplete(() => StartScreen.gameObject.SetActive(false));

    public void LevelComplete()
    {
        LevelWinText.text = "LEVEL\n<size=190>COMPLETED!";
        LevelWinPanel.SetActive(true);
        LevelWinPanel.GetComponent<Image>().DOFade(1f, 0.5f);
    }

    public void LevelLose()
    {
        LevelFailText.text = "LEVEL\n<size=200>FAILED";
        LevelLosePanel.SetActive(true);
        LevelLosePanel.GetComponent<Image>().DOFade(1f, 0.5f);
    }
}
