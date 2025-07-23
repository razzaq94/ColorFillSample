using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;  
    [SerializeField] private GameObject lockImage;
    [SerializeField] private Button button;

    private int buildIndex;
   
    public void Initialize(int buildIndex, bool isCompleted)
    {
        this.buildIndex = buildIndex;

        levelText.gameObject.SetActive(isCompleted);
        lockImage.SetActive(!isCompleted);
        button.interactable = isCompleted;

        if (isCompleted)
            levelText.text = buildIndex.ToString();  
    }

    public void OnClick_LoadLevel()
    {
        AudioManager.instance?.PlayUISound(0);

        if (!button.interactable) return;
        SceneManager.LoadScene(buildIndex);
    }
}
