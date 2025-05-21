using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [Header("Core Settings")]
    public bool _gameRunning = false;
    public bool Debug = false;
    private bool _gameStarted = false;
    public Player Player;
    public Transform Camera = null;

    public int StartLevel = 0;
    public int CurrentLevel = 1;
    public int Diamonds;
    private int LevelToUse = 1;
    public int GetCurrentLevel => CurrentLevel;
    [Space]
    public LevelData[] Levels = null;

    [Header("Assign Enemy Spawn Positions Here")]
    public Vector2 gridOrigin = Vector2.zero;
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
        AudioManager.instance?.PlayUISound(0);
        UIManager.Instance?.StartGame();
        _gameRunning = true;
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
        AudioManager.instance?.PlaySFXSound(0);
    }

    public void StartSpawningEnemies()
    {
        foreach (var Config in Levels[LevelToUse-1].SpwanablesConfigurations)
        {
            if (Config.spawnCount <= 0)
                continue;
            StartCoroutine(SpawnEnemyRoutine(Config));
        }
    }

    public IEnumerator SpawnEnemyRoutine(SpawnableConfig Config)
    {
        List<Cube> availableFilled = new List<Cube>(GridManager.Instance.GetAllFilledCells());
        GetCells();

        for (int i = 0; i < Config.spawnCount; i++)
        {
            Cube cell;

            if (Config.enemyType == SpawnablesType.Killer)
            {
                int rnd = Random.Range(0, availableFilled.Count);
                cell = availableFilled[rnd];
                availableFilled.RemoveAt(rnd);
            }
            else
            {
                List<Cube> positions = Levels[CurrentLevel].gridPositions;
                int rndIndex = Random.Range(0, positions.Count);
                cell = positions[rndIndex];
            }

            Vector3 groundPos = cell.transform.position + new Vector3(0f, Config.yOffset, 0f);
            Vector3 spawnPos = new Vector3(groundPos.x, Config.initialSpawnHeight, groundPos.z);
            yield return new WaitForSeconds(Config.delayRange.x);
            GameObject enemy = Instantiate(Config.prefab, spawnPos, Quaternion.identity);

            if (Config.usePhysicsDrop)
            {
                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = enemy.AddComponent<Rigidbody>();
                }

                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                StartCoroutine(ManualDrop(enemy.transform, groundPos, 0.5f));
            }

            if (i < Config.spawnCount - 1)
            {
                float delay = Random.Range(Config.delayRange.x, Config.delayRange.y);
                yield return new WaitForSeconds(delay);
            }
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
        AudioManager.instance.BGAudioSource.Stop();
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
}

[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    public Transform PlayerPos = null;
    public float levelTime = 0f;
    [HideInInspector] public int Columns = 10;
    [HideInInspector] public int Rows = 20;
    public List<Cube> gridPositions;
    public List<SpawnableConfig> SpwanablesConfigurations;
    

    private string LevelName = null;
    public void SetLevel(string levelName) => LevelName = levelName;


}
[System.Serializable]
public class SpawnableConfig
{
    public SpawnablesType enemyType;
    public GameObject prefab;
    public int spawnCount;
    public Vector2 delayRange = new Vector2(3f, 10f);
    public float yOffset = 0f;
    public float initialSpawnHeight = 35f;
    public bool usePhysicsDrop = true;
}
