using UnityEngine;

[ExecuteAlways]  // run in edit‐mode and play‐mode
public class GridBackground : MonoBehaviour
{
    [Tooltip("The singleton or your scene's GridManager that holds Columns/Rows")]
    public GridManager gridManager;

    [Tooltip("The Renderer on your plane (for material tiling)")]
    public Renderer bgRenderer;

    private void Start()
    {
        bgRenderer = GetComponent<Renderer>();
        var gameManager = FindAnyObjectByType<GameManager>();


        if (Application.isPlaying)
            bgRenderer.material.color = GameManager.Instance.BackgroundColor;
        else
            bgRenderer.sharedMaterial.color = gameManager.BackgroundColor;
    }


    void OnEnable()
    {
        if (gridManager == null && GridManager.Instance != null)
            gridManager = GridManager.Instance;
        if (bgRenderer == null)
            bgRenderer = GetComponent<Renderer>();

        UpdateVisuals();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (gridManager == null && GridManager.Instance != null)
            gridManager = GridManager.Instance;
        if (bgRenderer == null)
            bgRenderer = GetComponent<Renderer>();

        UpdateVisuals();
    }
#endif

    public void UpdateVisuals()
    {
        if (gridManager == null || bgRenderer == null)
            return;

        int cols = gridManager.Columns;
        int rows = gridManager.Rows;

        float sx = cols / 10f;
        float sz = rows / 10f;
        transform.localScale = new Vector3(sx, transform.localScale.y, sz);

        var mat = bgRenderer.sharedMaterial;
        mat.mainTextureScale = new Vector2(cols / 2f, rows / 2f);
    }

    

}
