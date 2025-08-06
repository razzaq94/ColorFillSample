using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance;

    [Header("Persistent Stats")]
    public int TotalDiamonds = 0;
    public int CurrentLives = 3;
    private const int MaxLives = 3;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Sync lives with UIManager if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.currentLives = CurrentLives;
            UIManager.Instance.ResetLives(); // updates life icons
        }
    }

    public void AddDiamond()
    {
        TotalDiamonds++;
        SaveGameData();
    }

    public void LoseLife()
    {
        if (CurrentLives > 0)
        {
            CurrentLives--;
            SaveGameData();
        }
    }

    public void GainLife()
    {
        if (CurrentLives < MaxLives)
        {
            CurrentLives++;
            SaveGameData();
        }
    }

    public void ResetLives()
    {
        CurrentLives = MaxLives;
        SaveGameData();
    }

    public void SaveGameData()
    {
        PlayerPrefs.SetInt("Diamonds", TotalDiamonds);
        PlayerPrefs.SetInt("Lives", CurrentLives);
        PlayerPrefs.Save();
    }

    public void LoadGameData()
    {
        TotalDiamonds = PlayerPrefs.GetInt("Diamonds", 0);
        CurrentLives = PlayerPrefs.GetInt("Lives", MaxLives);
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}
