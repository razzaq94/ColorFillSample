using UnityEngine;
using DG.Tweening;

public class CameraIntro : MonoBehaviour
{
    [Header("Start pos")]
    [SerializeField] private Vector3 startPosition = new Vector3(-0.5f, 25f, 15f);

    [Header("Tween settings")]
    [SerializeField] private float targetZ = -5f;
    [SerializeField] private float tweenDuration = 0.5f;

    void Start()
    {
        transform.position = startPosition;
        transform.DOMoveZ(targetZ, tweenDuration);
    }
}
