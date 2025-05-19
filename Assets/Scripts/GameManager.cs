using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool _gameRunning = false;
    public bool Debug = false;
    private bool _gameStarted = false;
    public Transform Camera = null;
    public Player Player;

    public int StartLevel = 0;
    public int CurrentLevel = 1;
    private int LevelToUse = 1;
    public int GetCurrentLevel => CurrentLevel;
    [Space]
    public LevelData[] Levels = null;

    [Header("Enemy Prefabs")]
    public List<GameObject> enemyPrefabs;
    [Header("Assign Enemy Spawn Positions Here")]
    public Vector2 gridOrigin = Vector2.zero;
    public Vector2 cellSize = Vector2.one;

    private void Awake()
    {
        Instance = this;
        DOTween.Init();
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
        //Levels[LevelToUse - 1].ResetGroups();
        Levels[LevelToUse - 1].LevelObject.SetActive(true);
        if (Levels[LevelToUse - 1].PlayerPos)
            Player.transform.position = Levels[LevelToUse - 1].PlayerPos.position;

        Haptics.Generate(HapticTypes.LightImpact);
        if (AudioManager.instance)
            AudioManager.instance?.PlayBGMusic(0);

        Camera.transform.position = new Vector3(-0.5f, 25f, 15f);
        Camera.transform.DOMoveZ(-5f, 0.5f);

        Player.Init();


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
        Invoke(nameof(SpawnEnemies), 5f);
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
        var availableFilled = new List<Cube>(GridManager.Instance.GetAllFilledCells());
        var availableEmpty = new List<Cube>(Levels[CurrentLevel].gridPositions);   
        int killerCount = enemyPrefabs
            .Count(p => p.GetComponent<EnemyBehaviors>().enemyType == EnemyType.Killer);

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            var prefab = enemyPrefabs[i];
            var behav = prefab.GetComponent<EnemyBehaviors>();
            Cube cell;

            if (behav.enemyType == EnemyType.Killer)
            {
                int rnd = Random.Range(0, availableFilled.Count);
                cell = availableFilled[rnd];
                availableFilled.RemoveAt(rnd);
                print(cell);
            }
            else
            {
                int random = Random.Range(0, availableFilled.Count);
                cell = Levels[CurrentLevel].gridPositions[random];
            }

            float y = 0f;
            if (behav.enemyType == EnemyType.Killer || behav.enemyType == EnemyType.FlyingHoop)
                y = 0.7f;

            Vector3 worldPos = cell.transform.position + new Vector3(0, y, 0);
            print(worldPos);
            Instantiate(prefab, worldPos, Quaternion.identity);
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
        DeleteEnemies();
        SpawnEnemies();
    }

    public void DeleteEnemies()
    {
        EnemyBehaviors[] enemies = FindObjectsByType<EnemyBehaviors>(FindObjectsSortMode.None);
        foreach (EnemyBehaviors enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }
}

[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    public Transform PlayerPos = null;
    [HideInInspector] public int Columns = 10;
    [HideInInspector] public int Rows = 20;
    public List<Cube> gridPositions;

    private string LevelName = null;
    public void SetLevel(string levelName) => LevelName = levelName;

    
}
