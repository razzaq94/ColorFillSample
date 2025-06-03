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
        IsFilled = isFilled;
        gameObject.SetActive(true);
         Color baseColor = Player.Instance.GetPlayerColor();
        if (IsFilled)
            ApplyFilledColor();
        else
            ApplyUnfilledColor();
    }

    public void FillCube()
    {
        if (!gameObject.activeSelf || IsFilled)
            return;
        IsFilled = true;

        ApplyFilledColor();

        GridManager.Instance.ChangeValue(transform.position.x, transform.position.z);
        transform.DOMoveY((transform.position.y + 0.5f), 0.15f);
        //transform.DOScale(new Vector3(0.5f, 0.5f, 0.5f), 0.15f);
       

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
