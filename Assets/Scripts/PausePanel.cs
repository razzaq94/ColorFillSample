using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public static PausePanel instance;

    private void Awake()
    {
        instance = this;
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
        AudioManager.instance.PlayUISound(0);
        Time.timeScale = 1.0f;
        Destroy(gameObject);
    }
    public void ReturnToMainMenu()
    {
        AudioManager.instance.PlayUISound(0);
        SceneManager.LoadScene(0);
        SplashScreen.Destroy(gameObject);
        Menu.Destroy(gameObject);
    }
    public void RestartButton()
    {
        AudioManager.instance.PlayUISound(0);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if(Time.timeScale == 0)
            Time.timeScale = 1.0f; 

    }
    public void QuitGame()
    {
        AudioManager.instance.PlayUISound(0);
        Application.Quit();
    }
    public void CloseButton()
    {
        AudioManager.instance.PlayUISound(0);
        Destroy(gameObject);
    }
}
