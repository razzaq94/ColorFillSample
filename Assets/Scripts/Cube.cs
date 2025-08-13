using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[HideMonoScript]
public class Cube : MonoBehaviour
{
    [Title("CUBE", null, titleAlignment: TitleAlignments.Centered)]
    [DisplayAsString] public bool IsFilled = false;
    [DisplayAsString] public bool CanHarm = false;
    [DisplayAsString] public bool onPlayer = false;
    
    [Header("Visual Settings")]
    public Renderer _renderer;
    public List<Collider> colliders = new List<Collider>();
    
    [Header("Animation Settings")]
    public float stuckCheckInterval = 0.5f;
    public float fillAnimationDuration = 0.15f;
    public float illuminateDuration = 0.5f;
    
    private float stuckTime = 0f;
    private const float FILLED_Y_POSITION = 0.5f;
    private const float FILL_START_Y_POSITION = 0.3f;
    private const float ENEMY_DETECTION_DISTANCE = 6f;
    private const float COLOR_BRIGHTENING_FACTOR = 0.3f;
    private const float EMISSION_MULTIPLIER = 1.5f;

    private void Awake()
    {
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
    }
    
    private void Update()
    {
        UpdateStuckTime();
        UpdateFilledPosition();
        UpdateFilledColor();
    }
    
    private void UpdateStuckTime()
    {
        stuckTime += Time.deltaTime;
    }
    
    private void UpdateFilledPosition()
    {
        if (IsFilled && transform.position.y != FILLED_Y_POSITION)
        {
            transform.DOMoveY(FILLED_Y_POSITION, fillAnimationDuration);
        }
    }
    
    private void UpdateFilledColor()
    {
        if (stuckTime >= stuckCheckInterval && IsFilled)
        {
            ApplyFilledColor();
        }
    }

    public void ResetCube()
    {
        IsFilled = false;
        CanHarm = false;
        transform.localPosition = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void SetTrail(Vector3 pos, bool isFilled = false)
    {
        transform.position = pos;
        gameObject.SetActive(true);
        ApplyTrailColor();

        IsFilled = false; // always start clean

        if (isFilled)
        {
            FillCube();
        }
        else
        {
            EnableColliders();
        }
    }

    public void FillCube(bool force = false)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Vector2Int index = GridManager.Instance.WorldToGrid(transform.position);

        if (!force && GridManager.Instance.IsFilled(index))
        {
            IsFilled = true;
            return;
        }
        
        DetectAndDestroyEnemies();
        PerformFill();
        EnableColliders();
    }
    
    private void PerformFill()
    {
        IsFilled = true;
        CanHarm = false;
        
        ApplyFilledColor();
        Illuminate();
        
        // Update grid and animate
        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);
        GridManager.Instance._trueCount++;
        
        AnimateFill();
    }
    
    private void AnimateFill()
    {
        transform.position = new Vector3(transform.position.x, FILL_START_Y_POSITION, transform.position.z);
        transform.DOMoveY(FILLED_Y_POSITION, fillAnimationDuration);
        transform.DOScale(Vector3.one, 0.1f);
    }
    
    private void DetectAndDestroyEnemies()
    {
        if (CanHarm)
        {
            return;
        }
        var enemiesInColumn = DetectEnemiesInColumn();
        //var enemiesInSweep = DetectEnemiesInSweep();
        
        DestroyEnemies(enemiesInColumn);
        //DestroyEnemies(enemiesInSweep);
    }
    
    private Collider[] DetectEnemiesInColumn()
    {
        Bounds bounds = GetBounds();
        Vector3 columnCenter = bounds.center;
        Vector3 columnHalfExtents = new Vector3(bounds.extents.x * 0.98f, ENEMY_DETECTION_DISTANCE * 0.5f, bounds.extents.z * 0.98f);
        
        return Physics.OverlapBox(
            columnCenter,
            columnHalfExtents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide
        );
    }
    
    //private RaycastHit? DetectEnemiesInSweep()
    //{
    //    Bounds bounds = GetBounds();
    //    Vector3 origin = bounds.center - Vector3.up * ENEMY_DETECTION_DISTANCE * 0.5f;
    //    Vector3 halfExtents = new Vector3(bounds.extents.x * 0.98f, 0.05f, bounds.extents.z * 0.98f);
        
    //    if (Physics.BoxCast(origin, halfExtents, Vector3.up, out RaycastHit hit, Quaternion.identity, ENEMY_DETECTION_DISTANCE, ~0, QueryTriggerInteraction.Collide))
    //    {
    //        return hit;
    //    }
        
    //    return null;
    //}
    
    private Bounds GetBounds()
    {
        if (_renderer != null)
            return _renderer.bounds;
        if (colliders.Count > 0 && colliders[0] != null)
            return colliders[0].bounds;
        return new Bounds(transform.position, Vector3.one);
    }
    
    private void DestroyEnemies(Collider[] colliders)
    {
        foreach (var collider in colliders)
        {
            DestroyEnemy(collider.transform.root);
        }
    }
    
    private void DestroyEnemies(RaycastHit? hit)
    {
        if (hit.HasValue)
        {
            DestroyEnemy(hit.Value.collider.transform.root);
        }
    }
    
    private void DestroyEnemy(Transform enemyRoot)
    {
        if (!enemyRoot.CompareTag("Enemy"))
            return;
            
        if (enemyRoot.TryGetComponent<AEnemy>(out var enemy) && enemy.enemyType != SpawnablesType.FlyingHoop)
        {
            PlayEnemyDestructionEffects(enemy);
            Destroy(enemyRoot.gameObject);
        }
    }
    
    private void PlayEnemyDestructionEffects(AEnemy enemy)
    {
        AudioManager.instance?.PlaySFXSound(2);
        
        if (enemy.defaultRenderer != null)
        {
            GameManager.Instance.SpawnDeathParticles(enemy.gameObject, enemy.defaultRenderer.material.color);
        }
    }

    public void SetTiling(int gridCols, int gridRows)
    {
        if (_renderer != null)
        {
            Material mat = _renderer.material; 
            mat.mainTextureScale = new Vector2(gridCols / 5f, gridRows / 5f);
        }
    }

    public void Illuminate(float duration = 0.5f)
    {
        if (!TryGetComponent<Renderer>(out Renderer renderer)) 
            return;

        Material mat = renderer.material;
        if (!mat.HasProperty("_EmissionColor")) 
            return;

        Color baseColor = GameManager.Instance.CubeFillColor; 
        Color glow = baseColor * EMISSION_MULTIPLIER; 

        if (!mat.IsKeywordEnabled("_EMISSION"))
            mat.EnableKeyword("_EMISSION");

        mat.SetColor("_EmissionColor", glow);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        DOTween.To(() => mat.GetColor("_EmissionColor"),
                   c => mat.SetColor("_EmissionColor", c),
                   Color.black,
                   duration);
    }

    public void ApplyTrailColor()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        Color baseColor = GameManager.Instance.CubeFillColor;
        Color lighterColor = CreateLighterColor(baseColor);
        _renderer.material.color = lighterColor;
    }

    private void ApplyFilledColor()
    {
        if (_renderer == null) 
            return;

        _renderer.material.color = GameManager.Instance.CubeFillColor;
    }

    private Color CreateLighterColor(Color baseColor)
    {
        return new Color(
            Mathf.Clamp01(baseColor.r + COLOR_BRIGHTENING_FACTOR),
            Mathf.Clamp01(baseColor.g + COLOR_BRIGHTENING_FACTOR),
            Mathf.Clamp01(baseColor.b + COLOR_BRIGHTENING_FACTOR)
        );
    }
    
    private void EnableColliders()
    {
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }
}
