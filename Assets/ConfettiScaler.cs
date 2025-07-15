using UnityEngine;

public class ConfettiScaler : MonoBehaviour
{
    public Camera targetCamera;
    public float baseOrthoSize = 10f; // The size where confetti looks normal
    public float baseScale = 1f;      // The normal scale



    private void Start()
    {
        targetCamera = Camera.main;
    }
    void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float scaleFactor = targetCamera.orthographicSize / baseOrthoSize;
        transform.localScale = Vector3.one * (baseScale * scaleFactor);
    }
}
