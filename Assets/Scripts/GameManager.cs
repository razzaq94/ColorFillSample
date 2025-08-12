using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
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
    
    public int firstLevelBuildIndex = 1;
    public int TotalLevels => SceneManager.sceneCountInBuildSettings - firstLevelBuildIndex;
    public int LevelToUse = 1;

    [Header("Fall Speed")]
    [Tooltip("If false will spawn from mid camera position")]
    public bool FallStraight = true;

    [Range(0f, 2f)] public float ObjectFallingTime = 1;

    [ShowIf("FallStraight")]
    public float YFallOffset = 3;
    [ShowIf("FallStraight")]
    public float ZFallOffset = 0;

    [FoldoutGroup("Level Data")]
    [ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true, DraggableItems = true)]
    public LevelData Level;

    [Header("Enemy Variants")]
    public List<EnemyVariantGroup> enemyVariantGroups;
    private Dictionary<SpawnablesType, List<GameObject>> enemyPrefabVariants;

    private bool isGameOver = false;
    public bool loosed = false;
    public bool reviveUsed = false;
    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1.0f;

        DOTween.Init();
        FillEnemyVariants();
        SetCamera();
        if (SceneManager.GetActiveScene().name != "_MainMenuScene")
        {
            GameHandler.Instance.CurrentLevel = SceneManager.GetActiveScene().buildIndex;
        }
    }

    private void Start()
    {
        if (!Application.isEditor) Debug = false;

        GridManager.Instance.InitGrid(Level.Columns, Level.Rows);
        Player.Init();
        GetCells();
        Player.gameObject?.SetActive(true);
        foreach (var wall in GameObject.FindGameObjectsWithTag("Boundary"))
        {
            if (wall.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial.color = WallColor;
        }
        foreach (var wall in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            if (wall.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial.color = WallColor;
        }
        if (!isGameOver && !Level.isTimeless && Level.levelTime > 0)
        {
            UIManager.Instance.ShowClockAndTime(Level.levelTime, UIManager.Instance.iconTransform);

        }
        UIManager.Instance.Diamonds.text = GameHandler.Instance.TotalDiamonds.ToString();

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

            CheckForHaptics(Level.levelTime);
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
        {
            StartGame();
            AdManager_Admob.instance?.ShowBannerAd();
        }

    }

    private bool isRedActive = false;   
    private bool isAnimating = false;   

    private void CheckForHaptics(float currentTime)
    {
        if (currentTime > 10)
        {
            if (isRedActive)
            {
                UIManager.Instance.timerText.color = Color.white; 
                UIManager.Instance.timerText.transform.DOKill();  
                isRedActive = false;  
                isAnimating = false;  
            }

            return;  
        }

        if (Mathf.CeilToInt(currentTime) == 15)
        {
            Haptics.Generate(HapticTypes.LightImpact);
            print("Haptic at 15 seconds");
        }
        else if (Mathf.CeilToInt(currentTime) == 10)
        {
            Haptics.Generate(HapticTypes.MediumImpact);
            print("Haptic at 10 seconds");

            if (!isRedActive)  
            {
                UIManager.Instance.timerText.color = Color.red;
                isRedActive = true; 
            }

            if (!isAnimating)
            {
                AnimateTimerText();
            }
        }
        else if (Mathf.CeilToInt(currentTime) == 5)
        {
            Haptics.Generate(HapticTypes.HeavyImpact);
            print("Haptic at 5 seconds");
        }
    }

    private void AnimateTimerText()
    {
        UIManager.Instance.timerText.transform.DOScale(Vector3.one * 0.5f, 0.3f) 
            .SetEase(Ease.InOutSine)  
            .SetLoops(-1, LoopType.Yoyo);  
        isAnimating = true; 
    }


    private void StartGame()
    {
        _gameStarted = true;
        AudioManager.instance?.PlayUISound(0);
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
        AudioManager.instance?.BGAudioSource.Stop();
    }

    
    

    public void AddTime(int time)
    {
        Level.levelTime += time;
    }

   
    public void ReviveFromLife()
    {
        hasTriggeredLose = false;
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
        if (reviveUsed)
        {
            Player.InvincibleForSeconds(3);
        }
        shake = false; // Reset camera shake flag
        //print(Player.lastSafeFilledPosition + " after revive");
    }




    public void LevelComplete(string msg)
    {
        //if (isGameOver) return; 

        Player.enabled = _gameRunning = false;
        GameHandler.Instance.CurrentLevel++;
        LevelToUse++;
        UIManager.Instance.LevelComplete(msg);
        MarkLevelCompleted(SceneManager.GetActiveScene().buildIndex);
    }
    public void ResumeAfterAd()
    {
        isGameOver = false;
        Player.enabled = true;
        GameLoseScreen.instance.ClosePanael();
        AudioManager.instance?.PlayBGMusic(0);
    }

    public bool hasTriggeredLose = false;
    public void LevelLose()
    {
        hasTriggeredLose = true; 
        loosed = true;
        Player.enabled = _gameRunning = false;
        print("Level Lose Triggered");
        Invoke(nameof(HandleLevelLoseCrash), 0.5f);
        AudioManager.instance?.BGAudioSource.Stop();
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

    bool shake = false;
    public void CameraShake(float duration = 0.5f, float magnitude = 0.2f)
    {
        loosed = true;
        if (!shake)
        {
            shake = true; // Set the flag to true to prevent multiple shakes
           StartCoroutine(DoShake(duration, magnitude));
        }
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        print("Camera Shake started with duration: " + duration + " and magnitude: " + magnitude);
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

        List<Vector2Int> exposedCells = GridManager.Instance.GetExposedGridCells();

        foreach (var config in level.SpwanablesConfigurations)
        {
            if (config.spawnCount <= 0)
                continue;

            StartCoroutine(SpawnEnemyRoutine(config, exposedCells));
        }
    }
    private List<Vector3> _usedPickupCells = new(); 


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

            Vector3 spawnBasePos = Vector3.zero; 


            bool isSmartPickup = cfg.prefab.name.Contains("Timer") ||
                     cfg.prefab.name.Contains("SlowDown") ||
                     cfg.prefab.name.Contains("Heart");

            if (cfg.enemyType == SpawnablesType.SpikeBall || isSmartPickup)

            {
                Cube cell = null;

                do
                {
                    if (available.Count == 0)
                    {
                        break;
                    }

                    int idx = Random.Range(0, available.Count);
                    cell = available[idx];
                    available.RemoveAt(idx);

                } while (GameManager.Instance._usedPickupCells.Contains(cell.transform.position)); 

                if (cell != null)
                {
                    spawnBasePos = cell.transform.position;
                    GameManager.Instance._usedPickupCells.Add(spawnBasePos); 
                }
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
            StartCoroutine(ManualDropWithYFreeze(enemy, finalTargetPos, cfg.yOffset, ObjectFallingTime, cfg.moveSpeed));

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

    private IEnumerator ManualDropWithYFreeze(GameObject enemy, Vector3 targetPos, float finalY, float duration, float speedToApply)
    {
        Vector3 start = Camera.position;

        if (FallStraight)
        {
            start = targetPos;
            start.y = Camera.position.y + YFallOffset;
            start.z = Camera.position.z + ZFallOffset;
        }
        enemy.transform.position = start;
        Vector3 end = new Vector3(targetPos.x, finalY, targetPos.z);
        float elapsed = 0f;

        while (elapsed < duration && enemy != null)
        {
            elapsed += Time.deltaTime;
            enemy.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        if (enemy == null)
        {
            yield break; 
        }
        enemy.transform.position = end;

        if (enemy.TryGetComponent<CubeEater>(out var eater))
        {
            eater.Init(speedToApply);
        }

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
        print("a");
        //Time.timeScale = 1.0f;
        StartCoroutine(WaitNPerform(0.2f,()=>
        {
            print("b");

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }));
    }

    //private IEnumerator RestartWithDelay()
    //{
    //    GameLoseScreen.instance?.ClosePanael();
    //    if (Time.timeScale == 0)
    //        Time.timeScale = 1.0f;
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //    if (Time.timeScale == 0)
    //        Time.timeScale = 1.0f;
    //}

    IEnumerator WaitNPerform(float time,System.Action act)
    {
        yield return new WaitForSeconds(time);
        act?.Invoke();
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

    
    public GameObject playerParticlePrefab;

    public void SpawnDeathParticles(GameObject sourceObject, Color color)
    {

        Vector3 position = sourceObject.transform.position + new Vector3(0, 1.5f, 0);
        GameObject prefabToUse = sourceObject.CompareTag("Player") ? playerParticlePrefab : deathParticlePrefab;

        GameObject fx = Instantiate(prefabToUse, position, Quaternion.identity);

        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }

        //print(fx.name + " spawned at " + position);
        Destroy(fx, 2f);
    }
    public void SetCamera()
    {
        if (Level.useAutoCameraPositioning)
        {
            var cam = Camera.GetComponent<Camera>();
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;



            float aspect = Screen.safeArea.width / Screen.safeArea.height;
            float halfWorldWidth = (Level.Columns * Level.cubeSize * 0.5f)  // half grid width
                                 + (Level.sidePaddingCubes * Level.cubeSize);

            cam.orthographicSize = halfWorldWidth / aspect;

            //Y / Z offsets remain table - driven(ignore X completely)
            if (ColumnToCamOrtho.TryGetValue(Level.Columns, out var posData))
            {
                Vector3 pos = cam.transform.position;
                pos.y = posData.y;
                pos.z = posData.z;
                cam.transform.position = pos;

                Level.cameraYPosition = pos.y;
                Level.cameraZPosition = pos.z;
            }
        }

    }
    private float slowdownFactor = 0.5f;



    public void ResetSlowDown()
    {
        StartCoroutine(ResetSlowDownEffect(10f));
    }
    public IEnumerator ResetSlowDownEffect(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        var enemies = FindObjectsOfType<EnemyBehaviors>();
        var cubeEaters = FindObjectsOfType<CubeEater>();
        var rigidbodies = FindObjectsOfType<Rigidbody>();

        foreach (var enemy in enemies)
        {
            enemy.speed /= slowdownFactor;
        }

        foreach (var cubeEater in cubeEaters)
        {
            cubeEater.speed /= slowdownFactor;
        }

        foreach (var rb in rigidbodies)
        {
            if (rb.CompareTag("Player"))
            {
                continue;
            }

            rb.linearVelocity /= slowdownFactor;
        }
    }



    private static readonly Dictionary<int, Vector3> ColumnToCamOrtho = new()
{
    { 8,  new Vector3(7f, 10f, -1.5f) },
    { 10, new Vector3(9.36f, 10f, -1.5f) },
    { 12, new Vector3(11.67f, 10f, -1f) },
    { 14, new Vector3(14.04f, 10f, -0.8f) },
    { 16, new Vector3(16.4f, 10f, -0.5f) },
    { 18, new Vector3(18.78f, 10f, 0f) },
    { 20, new Vector3(21.2f, 10f, .5f) },
    { 22, new Vector3(23.5f, 10f, .5f) },
    { 24, new Vector3(25.8f, 10f, .5f) },
    { 26, new Vector3(28.3f, 10f, 1f) },
    { 28, new Vector3(30.6f, 10f, 1f) },
    { 30, new Vector3(33f, 10f, 1f) },
    { 32, new Vector3(35.4f, 10f, 1f) },
    { 34, new Vector3(37.8f, 15f, 1f) },
    { 36, new Vector3(40f, 15f, 1f) },
    { 38, new Vector3(42.5f, 15f, 1f) },
    { 40, new Vector3(44.8f, 15f, 1f) },
    { 42, new Vector3(47f, 20f, 1f) },
    { 44, new Vector3(49.5f, 20f, 1f) },
    { 46, new Vector3(51.7f, 20f, 1f) },
    { 48, new Vector3(54f, 20f, 1f) },
    { 50, new Vector3(56.5f, 20f, 1f) },
};
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

    public float cubeSize = 1f;   // Unity default cube
    public float sidePaddingCubes = 1f;

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
    public string sceneName;
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
