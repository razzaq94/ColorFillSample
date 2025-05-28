using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
[HideMonoScript]    
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [Title("GAME-MANAGER", null, titleAlignment: TitleAlignments.Centered)]
    [Header("Core Settings")]
    [DisplayAsString]
    public bool _gameRunning = false;
    public bool Debug = false;
    private bool _gameStarted = false;

    [FoldoutGroup("Player References")]
    public Player Player;
    [FoldoutGroup("Player References")]
    public Transform Camera = null;

    public int StartLevel = 0;
    public int CurrentLevel = 1;
    public int firstLevelBuildIndex = 1;
    public int TotalLevels => SceneManager.sceneCountInBuildSettings - firstLevelBuildIndex;
    public int Diamonds;
    private int LevelToUse = 1;


    [Header("Fall Speed")]
    [Range(0f, 1f)]
    public float SpeedForFallingObjects;

    public int GetCurrentLevel => CurrentLevel;
    [FoldoutGroup("Level Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public LevelData Level;

    [Header("Assign Enemy Spawn Positions Here")]
    private Vector2 gridOrigin = Vector2.zero;
    [HideInInspector]public Vector2 cellSize = Vector2.one;


    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
        DOTween.Init();
        LoadPlayerPrefs();
    }

    private void Start()
    {
        if (!Application.isEditor)
            Debug = false;
        //foreach (LevelData Level in Level)
        //    Level.LevelObject.SetActive(false);
        //if (LevelToUse > Level.Length)
        //    LevelToUse = 1;

        GridManager.Instance.InitGrid(Level.Columns, Level.Rows);
        //Levels[LevelToUse - 1].ResetGroups();
        //Level[LevelToUse - 1].LevelObject.SetActive(true);
        if (Level.PlayerPos)
            Player.transform.position = Level.PlayerPos.position;

        Haptics.Generate(HapticTypes.LightImpact);
        if (AudioManager.instance)
            AudioManager.instance?.PlayBGMusic(0);

        //Camera.transform.position = new Vector3(-0.5f, 25f, 15f);
        //Camera.transform.DOMoveZ(-5f, 0.5f);

        Player.Init();
        GetCells();
    }

    

    public void GetCells()
    {
        var availableEmpty = new List<Cube>(GridManager.Instance.GetAnyCells());
        for (int i = 0; i < Level.gridPositions.Count; i++)
        {
            Level.gridPositions[i] = availableEmpty[Random.Range(0, availableEmpty.Count)];
        }
    }

    private void Update()
    {
        if (!isGameOver && Level.levelTime > 0)
        {
            Level.levelTime -= Time.deltaTime;
            Level.levelTime = Mathf.Clamp(Level.levelTime, 0, 9999); 
            UpdateTimerDisplay();
        }

        if (!isGameOver && Level.levelTime <= 0)
        {
            LevelLose();
        }
        void UpdateTimerDisplay()
        {
            UIManager.Instance.timerText.text = Mathf.CeilToInt(Level.levelTime).ToString();
        }

        if (Input.GetMouseButtonDown(0) && !_gameStarted)
            StartGame();
    }

    private void StartGame()
    {
        _gameStarted = true;
        AudioManager.instance.PlayUISound(0);
        UIManager.Instance.StartGame();
        UIManager.Instance.SwipeToStart.gameObject.SetActive(false);
        _gameRunning = true;
        //PlacePreplacedEnemies();
        ScheduleEnemySpawns();
    }
    public void AddTime(int time)
    {
        Level.levelTime += time;
    }


    public void LevelComplete()
    {
        Player.enabled = _gameRunning = false;
        CurrentLevel++;
        LevelToUse++;
        UIManager.Instance.LevelComplete();
        AudioManager.instance.PlaySFXSound(0);
        MarkLevelCompleted(SceneManager.GetActiveScene().buildIndex); 
    }

    [Button]
    private void PlacePreplacedEnemies()
    {
        var level = Level;
        for (int i = 0; i < level.PreplacedPrefabs.Count; i++)
        {
            var prefab = level.PreplacedPrefabs[i];
            var point = level.PreplacedSpawnPoints[i];
            if (prefab != null && point != null)
                Instantiate(prefab, point.position, Quaternion.identity);
        }
    }
    private void ScheduleEnemySpawns()
    {
        var level = Level;

        foreach (var config in level.SpwanablesConfigurations)
        {
            if (config.spawnCount <= 0)
                continue;

            StartCoroutine(SpawnEnemyRoutine(config));
        }
    }

    public IEnumerator SpawnEnemyRoutine(SpawnableConfig cfg)
    {
        if (cfg.useTimeBasedSpawn)
        {
            yield return new WaitForSeconds(cfg.initialDelay);
        }
        else if (!cfg.useTimeBasedSpawn)
        {

            yield return new WaitUntil(
                () => GridManager.Instance._progress >= cfg.progressThreshold
            );
        }

        List<Cube> available = new List<Cube>(GridManager.Instance.GetAllFilledCells());
        GetCells();

        for (int i = 0; i < cfg.spawnCount; i++)
        {
            Cube cell;
            if (cfg.enemyType == SpawnablesType.SpikedBall)
            {
                int idx = Random.Range(0, available.Count);
                cell = available[idx];
                available.RemoveAt(idx);
            }
            else
            {
                var positions = Level.gridPositions;
                cell = positions[Random.Range(0, positions.Count)];
            }

            Vector3 ground = cell.transform.position + Vector3.up * cfg.yOffset;
            Vector3 spawnPos = new Vector3(ground.x, cfg.initialSpawnHeight, ground.z);

            var enemy = Instantiate(cfg.prefab, spawnPos, Quaternion.identity);
            if (cfg.usePhysicsDrop)
            {
                var rb = enemy.GetComponent<Rigidbody>()
                         ?? enemy.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotationX
                                 | RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                StartCoroutine(ManualDrop(enemy.transform, ground, SpeedForFallingObjects));
            }

            if (i < cfg.spawnCount - 1)
                yield return new WaitForSeconds(cfg.subsequentSpawnDelays[i]);
        }
    }
   

    public IEnumerator ManualDrop(Transform pos, Vector3 target, float duration)
    {
        Vector3 start = pos.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pos.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        pos.position = target;
    }
    public void LevelLose()
    {
        Player.enabled = _gameRunning = false;
        UIManager.Instance?.LevelLose();
        AudioManager.instance?.BGAudioSource.Stop();
        AudioManager.instance?.PlaySFXSound(0);
    }

    public void Replay()
    {
        AudioManager.instance?.PlayUISound(0);
        CubeGrid.Instance.Restart();
        UIManager.Instance.Start();
        Start();
        Player.Restart();
        DeleteEnemies();

    }
    public bool IsLevelCompleted(int buildIndex)
    {
        return PlayerPrefs.GetInt($"Level_{buildIndex}_Completed", 0) == 1;
    }

    public void MarkLevelCompleted(int buildIndex)
    {
        PlayerPrefs.SetInt($"Level_{buildIndex}_Completed", 1);
        PlayerPrefs.Save();
    }
    public void DeleteEnemies()
    {
        EnemyBehaviors[] enemies = FindObjectsByType<EnemyBehaviors>(FindObjectsSortMode.None);
        foreach (EnemyBehaviors enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }



    public void SavePlayerPrefs()
    {
        PlayerPrefs.SetInt("Diamonds", Diamonds);
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        PlayerPrefs.Save();
    }

    public void LoadPlayerPrefs()
    {
        Diamonds = PlayerPrefs.GetInt("Diamonds", 0);
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
    }

    private void OnApplicationQuit()
    {
        SavePlayerPrefs();
    }
   

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var cfg in Level.SpwanablesConfigurations)
        {
            int needed = Mathf.Max(0, cfg.spawnCount - 1);
            while (cfg.subsequentSpawnDelays.Count < needed)
                cfg.subsequentSpawnDelays.Add(0f);
            while (cfg.subsequentSpawnDelays.Count > needed)
                cfg.subsequentSpawnDelays.RemoveAt(cfg.subsequentSpawnDelays.Count - 1);
        }
    }
#endif
}

[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    public Transform PlayerPos = null;
    public float levelTime = 0f;
    public List<Cube> gridPositions;
    [FoldoutGroup("Spawnables-Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public List<SpawnableConfig> SpwanablesConfigurations;

    public List<GameObject> PreplacedPrefabs;
    public List<Transform> PreplacedSpawnPoints;
    [HideInInspector] public int Columns = 10;
    [HideInInspector] public int Rows = 20;
   
}
[System.Serializable]
public class SpawnableConfig
{
    public SpawnablesType enemyType;
    public GameObject prefab;
    [Header("Spawn Trigger")]
    [Space]
    public bool useTimeBasedSpawn;
    [Space]
    public float initialDelay;
    [Space]
    [Range(0f, 1f)]
    public float progressThreshold;

    [Space]
    public int spawnCount = 1;
    [Space]
    public List<float> subsequentSpawnDelays = new List<float>() { };

    public float yOffset;
    public float initialSpawnHeight;

    [HideInInspector] public bool usePhysicsDrop;
}
