using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

[HideMonoScript]
public class Cube : MonoBehaviour
{
    [Title("CUBE", null, titleAlignment: TitleAlignments.Centered)]
    [DisplayAsString] public bool IsFilled = false;
    [DisplayAsString] public bool CanHarm = false;
    public float stuckTime = 0f;
    private float stuckCheckInterval = 0.5f;
    public Renderer _renderer;
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }
    private void Update()
    {
        if (IsFilled)
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

    public void Initalize(Vector3 pos, bool isFilled = false)
    {
        transform.position = pos;
        gameObject.SetActive(true);
        var renderer = GetComponent<Renderer>();
        ApplyTrailColorFromLevel();
        //if (renderer)
        //    renderer.material.color = GameManager.Instance.CubeFillColor;

        IsFilled = false; // always start clean

        if (isFilled)
            FillCube();
    }

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
        _renderer.material.color = GameManager.Instance.CubeFillColor;
        Illuminate(0.5f);

        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);
        GridManager.Instance._trueCount++;

        transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
        transform.DOMoveY(0.5f, 0.15f);
        transform.DOScale(Vector3.one, 0.1f);
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
        print("flash");
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
