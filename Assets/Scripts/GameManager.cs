using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;
//[HideMonoScript]    
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

    [Header("Color Settings")]
    public Color WallColor = Color.gray;
    public Color PlayerColor = Color.green;
    public Color CubeFillColor = Color.red;
    public Color BackgroundColor = new Color(1f, 1f, 1f, 1f); // Assuming white by default

    public int StartLevel = 0;
    public int CurrentLevel = 1;
    public int firstLevelBuildIndex = 1;
    public int TotalLevels => SceneManager.sceneCountInBuildSettings - firstLevelBuildIndex;
    public int Diamonds;
    private int LevelToUse = 1;


    [Header("Fall Speed")]
    [Range(0f, 1f)]
    public float SpeedForFallingObjects = 2;

    public int GetCurrentLevel => CurrentLevel;
    [FoldoutGroup("Level Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public LevelData Level;

    [Header("Assign Enemy Spawn Positions Here")]
    private Vector2 gridOrigin = Vector2.zero;
    [HideInInspector] public Vector2 cellSize = Vector2.one;



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
        GridManager.Instance.InitGrid(Level.Columns, Level.Rows);
        //if (Level.PlayerPos)
        //    Player.transform.position = Level.PlayerPos.position;
        Haptics.Generate(HapticTypes.LightImpact);
        if (AudioManager.instance)
            AudioManager.instance?.PlayBGMusic(0);
        GridManager.Instance.InitGrid(Level.Columns, Level.Rows);
        Player.Init();
        GetCells();

        foreach (var wall in GameObject.FindGameObjectsWithTag("Boundary"))
        {
            var renderer = wall.GetComponent<Renderer>();
            if (renderer) renderer.sharedMaterial.color = WallColor;
        }

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
        MarkLevelCompleted(SceneManager.GetActiveScene().buildIndex);
    }

    [Button]
    //public void PlacePreplacedEnemies()
    //{
    //    var level = Level;
    //    for (int i = 0; i < level.PreplacedPrefabs.Count; i++)
    //    {
    //        var prefab = level.PreplacedPrefabs[i];
    //        var point = level.PreplacedSpawnPoints[i];

    //        if (prefab != null && point != null)
    //        {
    //            var enemy = Instantiate(prefab, point.position, Quaternion.identity);
    //            enemy.transform.SetParent(point); // Make the spawn point the parent
    //        }
    //    }
    //}

    private void ScheduleEnemySpawns()
    {
        var level = Level;

        // Get the list of exposed cells
        List<Vector2Int> exposedCells = GridManager.Instance.GetExposedGridCells();

        foreach (var config in level.SpwanablesConfigurations)
        {
            if (config.spawnCount <= 0)
                continue;

            StartCoroutine(SpawnEnemyRoutine(config, exposedCells));
        }
    }


    public IEnumerator SpawnEnemyRoutine(SpawnableConfig cfg, List<Vector2Int> exposedCells)
    {
        yield return new WaitUntil(() => GridManager.Instance._progress >= cfg.progressThreshold);

        List<Cube> available = new List<Cube>(GridManager.Instance.GetAllFilledCells());
        GameManager.Instance.GetCells(); 

        for (int i = 0; i < cfg.spawnCount; i++)
        {
            if (i > 0 && i - 1 < cfg.subsequentProgressThresholds.Count)
            {
                float threshold = cfg.subsequentProgressThresholds[i - 1];
                yield return new WaitUntil(() => GridManager.Instance._progress >= threshold);
            }

            Vector3 spawnBasePos;

            if (cfg.enemyType == SpawnablesType.SpikeBall)
            {
                int idx = Random.Range(0, available.Count);
                var cell = available[idx];
                available.RemoveAt(idx);
                spawnBasePos = cell.transform.position;
            }
            else
            {
                Vector2Int configCell = new Vector2Int(cfg.col, cfg.row);
                spawnBasePos = GridManager.Instance.GridToWorld(configCell);

            }

            Vector3 finalTargetPos = spawnBasePos + Vector3.up * cfg.yOffset;

            Vector3 skySpawnPos = finalTargetPos + Vector3.up * cfg.initialSpawnHeight;

            var enemy = Instantiate(cfg.prefab, skySpawnPos, Quaternion.identity);
            RandomizeColor(enemy);
            var r = enemy.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = new Material(r.sharedMaterial); // clone material
                r.sharedMaterial.color = r.material.color;
               
            }
            StartCoroutine(ManualDropWithYFreeze(enemy, finalTargetPos, cfg.yOffset, SpeedForFallingObjects));

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

    private IEnumerator ManualDropWithYFreeze(GameObject enemy, Vector3 targetPos, float finalY, float duration)
    {
        Vector3 start = enemy.transform.position;
        Vector3 end = new Vector3(targetPos.x, finalY, targetPos.z);
        float elapsed = 0f;

        // Drop down over time
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            enemy.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        enemy.transform.position = end;

        var rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
            rb = enemy.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;

    }

    public void LevelLose()
    {
        Player.enabled = _gameRunning = false;
        UIManager.Instance?.LevelLose();
        AudioManager.instance?.BGAudioSource.Stop();
    }
    public void RandomizeColor(GameObject go)
    {
        if (go.TryGetComponent<Renderer>(out var rootRenderer))
        {
            var sharedMats = rootRenderer.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                var mat = new Material(sharedMats[i]);
                mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                sharedMats[i] = mat;
            }
            rootRenderer.sharedMaterials = sharedMats;
        }

        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (renderer == rootRenderer) continue;

            var sharedMats = renderer.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                var mat = new Material(sharedMats[i]);
                mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                sharedMats[i] = mat;
            }
            renderer.sharedMaterials = sharedMats;
        }
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

}
[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    //public Transform PlayerPos = null;
    // add these:
    public int PlayerStartRow = -1;
    public int PlayerStartCol = -1;
    public float levelTime = 0f;
    public List<Cube> gridPositions;
    [FoldoutGroup("Spawnables-Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public List<SpawnableConfig> SpwanablesConfigurations;

    public List<PreplacedEnemy> PreplacedEnemies = new List<PreplacedEnemy>();
    public List<Transform> PreplacedSpawnPoints;
    public List<CubeCell> enemyCubeCells = new();
    public int Columns = 50;
    public int Rows = 50;
    public List<CubeCell> gridCellPositions;
    public GameObject wallPrefab;
    public GameObject obstacle;
    public GameObject enemyCubePrefab;
    public List<EnemyCubeGroup> EnemyGroups = new List<EnemyCubeGroup>();



}
[System.Serializable]
public class SpawnableConfig
{
    public SpawnablesType enemyType;
    public GameObject prefab;

    [Range(0f, 1f)]
    public float progressThreshold; 

    public int spawnCount = 1;
    public List<float> subsequentProgressThresholds = new(); 

    public float yOffset;
    public float initialSpawnHeight = 35f;

    [HideInInspector] public bool usePhysicsDrop;

    public int row;
    public int col;
}

public enum CellType { Wall, Obstacle }

[System.Serializable]
public class CubeCell
{
    public int row;
    public int col;
    public CellType type;
}

[System.Serializable]
public class PreplacedEnemy
{
    public GameObject prefab;
    public int row, col;
    public string prefabName;
}