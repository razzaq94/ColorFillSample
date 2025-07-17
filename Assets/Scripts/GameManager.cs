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
    [DisplayAsString] public bool _gameRunning = false;
    public bool Debug = false;
    private bool _gameStarted = false;

    public GameObject _wallParent;
    public Player Player;
    public Transform Camera;
    public Vector3 Offset = new Vector3(0, 1, 0);
    public List<CameraSettings> CameraValues;
    public GameObject deathParticlePrefab; 

    [Header("Color Settings")]
    public Color WallColor = Color.gray;
    public Color PlayerColor = Color.green;
    public Color CubeFillColor = Color.yellow;
    public Color BackgroundColor = Color.white;
    public Color EnemyCubeColor = Color.red;

    [Header("Level Info")]
    public int StartLevel = 0;
    public int CurrentLevel = 1;
    public int firstLevelBuildIndex = 1;
    public int TotalLevels => SceneManager.sceneCountInBuildSettings - firstLevelBuildIndex;
    public int Diamonds;
    public int Life;
    private int LevelToUse = 1;
    public int GetCurrentLevel => CurrentLevel;

    [Header("Fall Speed")]
    [Range(0f, 1f)] public float SpeedForFallingObjects = 2;

    [FoldoutGroup("Level Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public LevelData Level;

    [Header("Enemy Variants")]
    public List<EnemyVariantGroup> enemyVariantGroups;
    private Dictionary<SpawnablesType, List<GameObject>> enemyPrefabVariants;

    private bool isGameOver = false;
    public bool loosed = false;

    private void Awake()
    {
        Instance = this;
        DOTween.Init();
        LoadPlayerPrefs();
        FillEnemyVariants();
    }

    private void Start()
    {
        if (!Application.isEditor) Debug = false;

        GridManager.Instance.InitGrid(Level.Columns, Level.Rows);
        Player.Init();
        GetCells();

        foreach (var wall in GameObject.FindGameObjectsWithTag("Boundary"))
        {
            if (wall.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial.color = WallColor;
        }

        Haptics.Generate(HapticTypes.LightImpact);
        AudioManager.instance?.PlayBGMusic(0);
    }
    private void Update()
    {
        if (!isGameOver && !Level.isTimeless && Level.levelTime > 0)
        {
            Level.levelTime -= Time.deltaTime;
            Level.levelTime = Mathf.Clamp(Level.levelTime, 0, 9999);
            UpdateTimerDisplay();
        }

        if (!isGameOver && Level.levelTime <= 0)
        {
            isGameOver = true;
            ShowTimeOutAdOptions();
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
        //UIManager.Instance.StartGame();
        _gameRunning = true;
        ScheduleEnemySpawns();
    }



    public void GetCells()
    {
        var availableEmpty = new List<Cube>(GridManager.Instance.GetAnyCells());
        for (int i = 0; i < Level.gridPositions.Count; i++)
        {
            Level.gridPositions[i] = availableEmpty[Random.Range(0, availableEmpty.Count)];
        }
    }

    private void ShowTimeOutAdOptions()
    {
        Player.enabled = _gameRunning = false;
        UIManager.Instance?.LevelLoseTimeOut();
        AudioManager.instance.BGAudioSource.Stop();
    }

    public void ShowRewardedForExtraTime()
    {
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            AddTime(60); 
            isGameOver = false;
        });
    }
    public void ShowInterstitialAndReplay()
    {
        AdManager_Admob.instance.ShowInterstitialAd();
        Replay();
    }

    public void ShowRewardedAndSkipLevel()
    {
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            LevelComplete();
        });
    }

    public void AddTime(int time)
    {
        Level.levelTime += time;
    }

    public void ReviveFromCollision()
    {
        Player.ClearUnfilledTrail();
        Player.ResetMovement();
        Player.transform.position = Player.lastSafeFilledPosition;
        GameLoseScreen.instance?.ClosePanael();
        Player.ForceInitialCube();
        AdManager_Admob.instance.ShowRewardedVideoAd(() =>
        {
            Player.gameObject.SetActive(true);
            Player.enabled = true;
            isGameOver = false;
            Player.IsMoving = true;
            loosed = false; 
        });

    }
    public void ReviveFromLife()
    {
        print(Player.lastSafeFilledPosition + " before revive");
        Player.ClearUnfilledTrail();

        Player.gameObject.SetActive(true);
        Player.enabled = true;

        Player.IsMoving = false; 
        Player.SpawnCubes = false; 

        Player.ResetMovement();

        Player.transform.position = Player.lastSafeFilledPosition;


        isGameOver = false;
        loosed = false;
        Player.ForceInitialCube();

        print(Player.lastSafeFilledPosition + " after revive");
    }




    public void LevelComplete()
    {
        if (isGameOver) return; 

        Player.enabled = _gameRunning = false;
        CurrentLevel++;
        LevelToUse++;
        UIManager.Instance.LevelComplete();
        MarkLevelCompleted(SceneManager.GetActiveScene().buildIndex);
    }
    public void ResumeAfterAd()
    {
        isGameOver = false;
        Player.enabled = true;
        GameLoseScreen.instance.ClosePanael();
        AudioManager.instance?.PlayBGMusic(0);
    }
    public void LevelLose()
    {
        if(loosed)
        {
            return;
        }
        loosed = true;
        print("Lose called");   
        Player.enabled = _gameRunning = false;
        Invoke(nameof(HandleLevelLoseCrash), 0.5f);
        AudioManager.instance.BGAudioSource.Stop();
    }

    public void  HandleLevelLoseCrash()
    {
        UIManager.Instance?.LevelLoseCrash();

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
    public void CameraShake(float duration = 0.5f, float magnitude = 0.2f)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        Transform cam = Camera.transform;
        Vector3 originalPos = cam.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = originalPos;
    }


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

            GameObject prefabToUse = cfg.prefab;

            if (enemyPrefabVariants.TryGetValue(cfg.enemyType, out var variants) && variants.Count > 0)
            {
                prefabToUse = variants[Random.Range(0, variants.Count)];
            }

            var enemy = Instantiate(prefabToUse, skySpawnPos, Quaternion.identity);
            StartCoroutine(ManualDropWithYFreeze(enemy, finalTargetPos, cfg.yOffset, SpeedForFallingObjects));
            if (cfg.enemyType == SpawnablesType.FlyingHoop || cfg.enemyType == SpawnablesType.CubeEater)
            {
                RandomizeColor(enemy);
            }
            if (enemy.TryGetComponent<EnemyBehaviors>(out var eb))
            {
                eb.speed = cfg.moveSpeed;
                
            }
            else if (enemy.TryGetComponent<CubeEater>(out var ce))
            {
                ce.speed = cfg.moveSpeed;
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

        var eater = enemy.GetComponent<CubeEater>();
        if (eater != null)
            eater.Init();

        var rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
            rb = enemy.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;

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
            string[] excludedNames = { "pCone1", "pCone2", "pCone3", "pCone4" };

            if (renderer == rootRenderer || excludedNames.Contains(renderer.gameObject.name))
                continue;

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
        StartCoroutine(RestartWithDelay());
    }

    private IEnumerator RestartWithDelay()
    {

        GameLoseScreen.instance?.ClosePanael();
        yield return new WaitForSeconds(0.2f); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    private void FillEnemyVariants()
    {
        enemyPrefabVariants = new Dictionary<SpawnablesType, List<GameObject>>();
        foreach (var group in enemyVariantGroups)
        {
            if (!enemyPrefabVariants.ContainsKey(group.type))
                enemyPrefabVariants[group.type] = new List<GameObject>();

            foreach (var prefab in group.variants)
            {
                if (prefab != null)
                    enemyPrefabVariants[group.type].Add(prefab);
            }
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
    public GameObject playerParticlePrefab;

    public void SpawnDeathParticles(GameObject sourceObject, Color color)
    {
        print("funcCalled");

        Vector3 position = sourceObject.transform.position + new Vector3(0, 1.5f, 0);
        GameObject prefabToUse = sourceObject.CompareTag("Player") ? playerParticlePrefab : deathParticlePrefab;

        GameObject fx = Instantiate(prefabToUse, position, Quaternion.identity);

        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }

        print(fx.name + " spawned at " + position);
        Destroy(fx, 2f);
    }


}
[System.Serializable]
public class LevelData
{
    public GameObject LevelObject = null;
    public int PlayerStartRow = -1;
    public int PlayerStartCol = -1;
    public bool isTimeless = false;
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

    [FoldoutGroup("Camera Settings")]
    public float cameraZPosition = 2.5f;

    public float cameraYPosition = 10f;
    [FoldoutGroup("Camera Settings")]
    public float zoomSize = 5f;
    public float cameraFOVMin = 30f;
    public float cameraFOVMax = 70f;
    public bool useAutoCameraPositioning = true;  

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
    public float moveSpeed = 2f;
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

[System.Serializable]
public class EnemyVariantGroup
{
    public SpawnablesType type;
    public List<GameObject> variants;
}
