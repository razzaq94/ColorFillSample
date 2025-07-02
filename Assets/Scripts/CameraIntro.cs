using UnityEngine;
using System.Collections;
using JetBrains.Annotations;


[System.Serializable]

public class CameraSettings
{
    public int columnSize;
    public Vector3 CameraAngle;
}

[ExecuteAlways]
public class CameraIntro : MonoBehaviour
{
    public static CameraIntro instance;
    public static Camera CAMERA { get; private set; }


    [Header("Zoom Settings")]
    public float zoomStep = 2f;
    public float minFOV = 25f;
    public float maxFOV = 80f;

    [Header("Transition Settings")]
    public float moveDuration = 1f;
    public float zoomDuringMove = 0.1f;

    //private void Awake()
    //{
    //    instance = this;

    //    if (CAMERA == null)
    //        CAMERA = GetComponent<Camera>();

    //    if (CAMERA == null)
    //        CAMERA = Camera.main;
    //}

    //public void Start()
    //{
    //    if (!Application.isPlaying || CAMERA == null || GameManager.Instance?.Level == null)
    //        return;

    //    // Center the camera at start (keep existing Y)
    //    Vector3 pos = transform.position;
    //    pos.x = GameManager.Instance.Level.Columns / 2f - 0.5f;
    //    pos.z = GameManager.Instance.Level.Rows / 3f;
    //    transform.position = pos;

    //    StartCoroutine(StartLevelEffect());
    //}

    //public IEnumerator StartLevelEffect()
    //{
    //    if (CAMERA == null || GameManager.Instance?.Level == null)
    //        yield break;

    //    Vector3 farLeftPoint = new Vector3(-2f, 0, 0);
    //    Vector3 farRightPoint = new Vector3(-2f, 0, GameManager.Instance.Level.Rows);

    //    float fov = CAMERA.fieldOfView;

    //    // Step 1: Zoom in until far left leaves screen
    //    while (PointVisible(farLeftPoint) && fov > minFOV)
    //    {
    //        fov -= zoomStep;
    //        CAMERA.fieldOfView = fov;
    //        yield return new WaitForSeconds(0.01f);
    //    }

    //    // Step 2: Zoom out until far right enters screen
    //    while (!PointVisible(farRightPoint) && fov < maxFOV)
    //    {
    //        fov += zoomStep;
    //        CAMERA.fieldOfView = fov;
    //        yield return new WaitForSeconds(0.01f);
    //    }
    //}

    //public void GoToNextPart()
    //{
    //    StartCoroutine(MoveAndAdjust());
    //}

    //IEnumerator MoveAndAdjust()
    //{
    //    if (GameManager.Instance?.Level == null || CAMERA == null)
    //        yield break;

    //    Vector3 startPos = transform.position;
    //    float shift = GameManager.Instance.Level.Rows + 3f;

    //    Vector3 endPos = new Vector3(startPos.x, startPos.y, startPos.z + shift);

    //    float t = 0;
    //    float fov = CAMERA.fieldOfView;

    //    while (t < 1)
    //    {
    //        t += Time.deltaTime / moveDuration;
    //        transform.position = Vector3.Lerp(startPos, endPos, t);
    //        fov += zoomDuringMove;
    //        fov = Mathf.Clamp(fov, minFOV, maxFOV);
    //        CAMERA.fieldOfView = fov;
    //        yield return null;
    //    }

    //    Vector3 farEndPoint = new Vector3(-2f, 0, GameManager.Instance.Level.Rows * 2);
    //    while (!PointVisible(farEndPoint) && fov < maxFOV)
    //    {
    //        fov += 0.2f;
    //        CAMERA.fieldOfView = fov;
    //        yield return new WaitForSeconds(0.01f);
    //    }

    //    while (PointVisible(farEndPoint) && fov > minFOV)
    //    {
    //        fov -= 0.2f;
    //        CAMERA.fieldOfView = fov;
    //        yield return new WaitForSeconds(0.01f);
    //    }
    //}

    //private bool PointVisible(Vector3 worldPoint)
    //{
    //    Vector3 vp = CAMERA.WorldToViewportPoint(worldPoint);
    //    return vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
    //}
}
