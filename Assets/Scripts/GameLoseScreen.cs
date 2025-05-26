using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoseScreen : MonoBehaviour
{
    public static GameLoseScreen instance;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject noThanksButton;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        noThanksButton.SetActive(false);

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
            if (i == 7)
                noThanksButton.SetActive(true);

            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "00";
        RestartButton();    //will call watch add after implementation;
        Destroy(gameObject);
    }

    public void RestartButton()
    {
        AudioManager.instance.PlayUISound(0);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;

    }

    public void WatchAdd()
    {

    }
}
