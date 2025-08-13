using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[HideMonoScript]
public class Cube : MonoBehaviour
{
    [Title("CUBE", null, titleAlignment: TitleAlignments.Centered)]
    [DisplayAsString] public bool IsFilled = false;
    [DisplayAsString] public bool CanHarm = false;
    public float stuckTime = 0f;
    private float stuckCheckInterval = 0.5f;
    public Renderer _renderer;

    public List<Collider> colliders = new List<Collider>();
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }
    private void Update()
    {
        if (IsFilled && transform.position.y != 0.5f)
        {
            transform.DOMoveY(0.5f, 0.15f);
        }
        stuckTime += Time.deltaTime;
        if (stuckTime >= stuckCheckInterval)
        {
            if(IsFilled)
            {
                _renderer.material.color = GameManager.Instance.CubeFillColor;
            }
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
        var renderer = GetComponent<Renderer>();
        ApplyTrailColorFromLevel();

        IsFilled = false; // always start clean

        if (isFilled)
        {
            FillCube();
        }
        else
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                colliders[i].enabled = true;
            }
        }
    }

    public int collCount = 0;
    public Collider[] overlaps;
    public void FillCube(bool force = false)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Vector2Int index = GridManager.Instance.WorldToGrid(transform.position);

        if (!force && GridManager.Instance.IsFilled(index))
        {
            //Debug.LogWarning($"Duplicate cube fill attempt at {index} from {name}");
            IsFilled = true;
            return;
        }
        IsFilled = true;
        CanHarm = false;
        _renderer.material.color = GameManager.Instance.CubeFillColor;
        Illuminate(0.5f);

        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);
        GridManager.Instance._trueCount++;
        transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
        transform.DOMoveY(0.5f, 0.15f);
        transform.DOScale(Vector3.one, 0.1f);

        var rend = _renderer;
        var col = colliders[0];

        Bounds b = rend ? rend.bounds : (col ? col.bounds : new Bounds(transform.position, Vector3.one));

        float distance = 6f;
        float halfSweep = distance * 0.5f;
        Vector3 origin = b.center - Vector3.up * halfSweep;
        Vector3 dir = Vector3.up;

        Vector3 halfExtents = new Vector3(b.extents.x * 0.98f, 0.05f, b.extents.z * 0.98f);

        // assumes: origin, dir, distance, halfExtents already computed
        Debug.DrawRay(origin, dir * distance, Color.red, 5f);
        Physics.SyncTransforms();

        Quaternion rot = Quaternion.identity;

        // 1) Overlap at start position (catch contained enemies)
        overlaps = Physics.OverlapBox(
            origin,
            halfExtents,
            rot,
            ~0,                               // all layers
            QueryTriggerInteraction.Collide   // include triggers
        );

        collCount = overlaps.Length;

        for (int i = 0; i < overlaps.Length; i++)
        {
            var c = overlaps[i];
            // if tag might be on root instead of child, use: c.transform.root.CompareTag("Enemy")
            if (c.CompareTag("Enemy"))
            {
                // your same handling:
                if (c.TryGetComponent<AEnemy>(out var enemy) && enemy.enemyType != SpawnablesType.FlyingHoop)
                {
                    var renderer = enemy.defaultRenderer;
                    AudioManager.instance?.PlaySFXSound(2);
                    if (renderer) GameManager.Instance.SpawnDeathParticles(c.gameObject, renderer.material.color);
                    Destroy(c.gameObject);
                    // optionally return here if one hit is enough
                }
            }
        }

        // 2) Sweep to catch enemies along the path
        if (Physics.BoxCast(
                origin,
                halfExtents,
                dir,
                out var hit,
                rot,
                distance,
                ~0,
                QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                if (hit.collider.TryGetComponent<AEnemy>(out var enemy) && enemy.enemyType != SpawnablesType.FlyingHoop)
                {
                    Debug.Log($"Enemy in centered sweep (boxcast): {hit.collider.name} at {hit.point}");
                    var renderer = enemy.defaultRenderer;
                    AudioManager.instance?.PlaySFXSound(2);
                    if (renderer) GameManager.Instance.SpawnDeathParticles(hit.collider.gameObject, renderer.material.color);
                    Destroy(hit.collider.gameObject);
                }
            }
        }

        for (int i = 0; i < colliders.Count; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true; // Enable the collider
            }
        }
    }


    public void SetTiling(int gridCols, int gridRows)
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = r.material; 
            mat.mainTextureScale = new Vector2(gridCols / 5f, gridRows / 5f);
        }
    }


   
    public void Illuminate(float duration = 0.5f)
    {
        if (!TryGetComponent<Renderer>(out Renderer renderer)) return;

        Material mat = renderer.material;
        if (!mat.HasProperty("_EmissionColor")) return;

        Color baseColor = GameManager.Instance.CubeFillColor; 

        Color glow = baseColor * 1.5f; 

        if (!mat.IsKeywordEnabled("_EMISSION"))
            mat.EnableKeyword("_EMISSION");

        mat.SetColor("_EmissionColor", glow);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        DOTween.To(() => mat.GetColor("_EmissionColor"),
                   c => mat.SetColor("_EmissionColor", c),
                   Color.black,
                   duration);
    }



    public void ApplyTrailColorFromLevel()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        Color baseColor = GameManager.Instance.CubeFillColor;
        Color lighterColor = new Color(
            Mathf.Clamp01(baseColor.r + 0.3f),
            Mathf.Clamp01(baseColor.g + 0.3f),
            Mathf.Clamp01(baseColor.b + 0.3f)
        );

        _renderer.material.color = lighterColor;
    }




    private void ApplyFilledColor()
    {
        if (_renderer == null) return;

        Color baseColor = Player.Instance.GetPlayerColor();
        Color vividColor = ComputeVividVersion(baseColor);
        _renderer.material.color = vividColor;
    }

   
    private void ApplyUnfilledColor()
    {
        if (_renderer == null) return;

        Color baseColor = Player.Instance.GetPlayerColor();
        _renderer.material.color = baseColor;
    }

    
    private Color ComputeVividVersion(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float H, out float S, out float V);

        S = Mathf.Min(1f, S * 1.5f);
        V = 1f;

        return Color.HSVToRGB(H, S, V);
    }

}
