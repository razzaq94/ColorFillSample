using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public static PausePanel instance;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        Time.timeScale = 0f;  
    }
    public static PausePanel ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("PausePanel")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<PausePanel>();
        }

        return instance;
    }
    public void ResumeGame()
    {
        AudioManager.instance?.PlayUISound(0);
        Time.timeScale = 1.0f;
        Destroy(gameObject);
    }
    public void ReturnToMainMenu()
    {
        AudioManager.instance?.PlayUISound(0);
        SceneManager.LoadScene(0);
        SplashScreen.Destroy(gameObject);
        Menu.Destroy(gameObject);
    }
    public void RestartButton()
    {
        AudioManager.instance?.PlayUISound(0);

        const int MaxAdsPerLevel = 1;
        var scene = SceneManager.GetActiveScene();
        string key = $"CrashRestartAds_{scene.buildIndex}_{scene.name}";

        int shown = PlayerPrefs.GetInt(key, 0);
        if (shown < MaxAdsPerLevel)
        {
            AdManager_Admob.instance?.ShowInterstitialAd();
            PlayerPrefs.SetInt(key, shown + 1);
            PlayerPrefs.Save();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f; 

    }
    public void QuitGame()
    {
        AudioManager.instance?.PlayUISound(0);
        Application.Quit();
    }
    public void CloseButton()
    {
        AudioManager.instance?.PlayUISound(0);
        if (Time.timeScale == 0)
            Time.timeScale = 1.0f;
        Destroy(gameObject);
    }
}
