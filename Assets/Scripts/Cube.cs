using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

[HideMonoScript]
public class Cube : MonoBehaviour
{
    [Title("CUBE", null, titleAlignment: TitleAlignments.Centered)]
    [DisplayAsString] public bool IsFilled = false;
    [DisplayAsString] public bool CanHarm = false;

    private Renderer _renderer;
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
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
        if (renderer)
            renderer.material.color = GameManager.Instance.CubeFillColor;

        IsFilled = false; // always start clean

        if (isFilled)
            FillCube();
    }

    public void FillCube(bool force = false)
    {
        if (!gameObject.activeSelf)
            return;

        Vector2Int index = GridManager.Instance.WorldToGrid(transform.position);

        // 🧠 Check if already filled unless forcing
        if (!force && GridManager.Instance.IsFilled(index))
        {
            Debug.LogWarning($"Duplicate cube fill attempt at {index} from {name}");
            gameObject.SetActive(false); // disable this cube immediately
            return;
        }

        IsFilled = true;

        // ✅ Update grid + progress
        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);

        // ✅ Visuals
        transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
        transform.DOMoveY(0.5f, 0.15f);
    }

    public void UnfillCube()
    {
        IsFilled = false;
        ApplyUnfilledColor();
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
