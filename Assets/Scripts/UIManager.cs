using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public CanvasGroup StartScreen = null;
    public TMP_Text CurrentLevelText = null;
    public TMP_Text NextLevelText = null;
    public GameObject LevelWinPanel = null;
    public Button NextLevelBtn = null;
    public TMP_Text LevelWinText = null;
    public GameObject LevelLosePanel = null;
    public Button RetryLevelBtn = null;
    public TMP_Text LevelFailText = null;
    public Image Fill = null;

    private void Awake()
    {
        Instance = this;
    }
    public void Start()
    {
        //LevelWinPanel.SetActive(false);
        //LevelLosePanel.SetActive(false);
        //Fill.fillAmount = 0f;
        //NextLevelBtn.onClick.RemoveAllListeners();
        //NextLevelBtn.onClick.AddListener(GameManager.Instance.Replay);
        //RetryLevelBtn.onClick.RemoveAllListeners();
        //RetryLevelBtn.onClick.AddListener(GameManager.Instance.Replay);
        //NextLevelText.text = (GameManager.Instance.GetCurrentLevel + 1).ToString();
        //CurrentLevelText.text = GameManager.Instance.GetCurrentLevel.ToString();
    }//Start() end

    public void FillAmount(float amount) => Fill.DOFillAmount(amount, 0.25f);

    public void StartGame() => StartScreen.DOFade(0.0f, 0.5f).OnComplete(() => StartScreen.gameObject.SetActive(false));

    public void LevelComplete()
    {
        //LevelWinText.text = "LEVEL\n<size=190>COMPLETED!";
        LevelWinPanel.SetActive(true);
        LevelWinPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
    }//LevelComplete() end

    public void LevelLose()
    {
        LevelFailText.text = "LEVEL\n<size=200>FAILED";
        LevelLosePanel.SetActive(true);
        LevelLosePanel.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
    }//LevelLose() end

}
