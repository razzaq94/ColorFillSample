using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Sirenix.OdinInspector;
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
    public int Diamonds;
    private int LevelToUse = 1;
    public int GetCurrentLevel => CurrentLevel;
    [FoldoutGroup("Level Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public LevelData[] Levels = null;

    [Header("Assign Enemy Spawn Positions Here")]
    private Vector2 gridOrigin = Vector2.zero;
    public Vector2 cellSize = Vector2.one;


    private bool isGameOver = false;

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
        GetCells();
    }

    public void GetCells()
    {
        var availableEmpty = new List<Cube>(GridManager.Instance.GetAnyCells());
        for (int i = 0; i < Levels[LevelToUse - 1].gridPositions.Count; i++)
        {
            Levels[LevelToUse - 1].gridPositions[i] = availableEmpty[Random.Range(0, availableEmpty.Count)];
        }
    }

    private void Update()
    {
        if (!isGameOver && Levels[LevelToUse-1].levelTime > 0)
        {
            Levels[LevelToUse-1].levelTime -= Time.deltaTime;
            Levels[LevelToUse - 1].levelTime = Mathf.Clamp(Levels[LevelToUse - 1].levelTime, 0, 9999); 
            UpdateTimerDisplay();
        }

        if (!isGameOver && Levels[LevelToUse - 1].levelTime <= 0)
        {
            LevelLose();
        }
        void UpdateTimerDisplay()
        {
            UIManager.Instance.timerText.text = Mathf.CeilToInt(Levels[LevelToUse - 1].levelTime).ToString();
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
        PlacePreplacedEnemies();
        ScheduleEnemySpawns();
    }
    public void AddTime(int time)
    {
        Levels[LevelToUse - 1].levelTime += time;
    }


    public void LevelComplete()
    {
        Player.enabled = _gameRunning = false;
        CurrentLevel++;
        LevelToUse++;
        UIManager.Instance.LevelComplete();
        AudioManager.instance.PlaySFXSound(0);
    }

    private void PlacePreplacedEnemies()
    {
        var level = Levels[LevelToUse - 1];
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
        var level = Levels[LevelToUse - 1];

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
                var positions = Levels[CurrentLevel].gridPositions;
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
                StartCoroutine(ManualDrop(enemy.transform, ground, 0.5f));
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

    public void DeleteEnemies()
    {
        EnemyBehaviors[] enemies = FindObjectsByType<EnemyBehaviors>(FindObjectsSortMode.None);
        foreach (EnemyBehaviors enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var lvl in Levels)
            foreach (var cfg in lvl.SpwanablesConfigurations)
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
    public bool usePhysicsDrop;
}
