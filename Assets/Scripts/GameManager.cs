using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool _gameRunning = false;

    public Transform Camera = null;
    public Player Player;

    public bool Debug = false;
    public int StartLevel = 0;
    [Space]
    public int CurrentLevel = 1;
    public LevelData[] Levels = null;

    public int GetCurrentLevel => CurrentLevel;

    private bool _gameStarted = false;
    private int LevelToUse = 1;


    [Header("Enemy Prefabs")]
    public List<GameObject> enemyPrefabs;
    [Header("Assign Enemy Spawn Positions Here")]
    public List<Vector2Int> gridPositions;
    public Vector2 gridOrigin = Vector2.zero;
    public Vector2 cellSize = Vector2.one;

    private void Awake()
    {
        Instance = this;
        DOTween.Init();
    }
#if UNITY_EDITOR
    private int TotalLevels(int value, GUIContent label) => (int)UnityEditor.EditorGUILayout.Slider(label, value, 1, this.Levels.Length);
#endif
    private void SetLevels()
    {
        for (int i = 0; i < Levels.Length; i++)
            Levels[i].SetLevel($"Level {i + 1}");
    }

    private void Start()
    {
        if (!Application.isEditor)
            Debug = false;
        foreach (LevelData Level in Levels)
            Level.LevelObject.SetActive(false);

        if (LevelToUse > Levels.Length)
            LevelToUse = 1;

        GridManager.Instance.InitGrid(Levels[LevelToUse - 1].Columns, Levels[LevelToUse - 1].Rows);
        Levels[LevelToUse - 1].ResetGroups();
        Levels[LevelToUse - 1].LevelObject.SetActive(true);
        if (Levels[LevelToUse - 1].PlayerPos)
            Player.transform.position = Levels[LevelToUse - 1].PlayerPos.position;

        Haptics.Generate(HapticTypes.LightImpact);
        if (AudioManager.instance)
            AudioManager.instance?.PlayBGMusic(0);

        Camera.transform.position = new Vector3(-0.5f, 25f, 15f);
        Camera.transform.DOMoveZ(-5f, 0.5f);

        Player.Init();

        SpawnEnemies();

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !_gameStarted)
            StartGame();
    }

    private void StartGame()
    {
        _gameStarted = true;
        AudioManager.instance?.PlaySFXSound(2);
        UIManager.Instance?.StartGame();
        _gameRunning = true;
    }

    public void LevelComplete()
    {
        Player.enabled = _gameRunning = false;
        CurrentLevel++;
        LevelToUse++;
        UIManager.Instance.LevelComplete();
        AudioManager.instance?.PlaySFXSound(3);
    }

    public void SpawnEnemies()
    {
        int count = Mathf.Min(enemyPrefabs.Count, gridPositions.Count);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = enemyPrefabs[i];
            Vector2Int cell = gridPositions[i];

            Vector3 worldpos = new Vector3(gridOrigin.x + cell.x, 0f, gridOrigin.y + cell.y);
            Instantiate(prefab, worldpos, Quaternion.identity);
        }

        if (enemyPrefabs.Count == gridPositions.Count)
        {
            print($"Enemy Prefabs count {enemyPrefabs.Count} does not match Grid Positions count {gridPositions.Count}");
        }

    }

    public void LevelLose()
    {
        Player.enabled = _gameRunning = false;
        UIManager.Instance?.LevelLose();
        AudioManager.instance.BGAudioSource.Stop(); 
        AudioManager.instance?.PlaySFXSound(4);
    }

    public void Replay()
    {
        AudioManager.instance?.PlaySFXSound(5);
        CubeGrid.Instance.Restart();
        UIManager.Instance.Start();
        Start();
        Player.Restart();
    }
}

[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    public Transform PlayerPos = null;
    [HideInInspector] public int Columns = 10;
    [HideInInspector] public int Rows = 20;
    public EnemyCubeGroup[] Groups = null;

    private string LevelName = null;

    public void SetLevel(string levelName) => LevelName = levelName;

    public void ResetGroups()
    {
        for (int i = 0; i < Groups.Length; i++)
            Groups[i].Restart();
    }
}
