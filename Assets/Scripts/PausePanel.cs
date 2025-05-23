using UnityEngine;

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

        GameManager.Instance.Replay();
        Destroy(gameObject);
    }
    public void OpenSettings()
    {
        AudioManager.instance.PlayUISound(0);

        SettingsPanel.ShowUI();
        Destroy(gameObject);
    }
    public void QuitGame()
    {
        AudioManager.instance.PlayUISound(0);
        Application.Quit();
    }

}
