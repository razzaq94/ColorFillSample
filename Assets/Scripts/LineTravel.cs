using UnityEngine;
using System.Collections;

public class LineTravel : MonoBehaviour
{

    private void Start()
    {
        StartTravel();
    }

    public void StartTravel()
    {
        transform.position = new Vector3(-900f, -500f, transform.position.z);
        StartCoroutine(TravelCoroutine(1f));
    }

    private IEnumerator TravelCoroutine(float duration)
    {
        Vector3 startPos = new Vector3(-900f, -500f, transform.position.z);
        Vector3 endPos = new Vector3(950f, 500f, transform.position.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, endPos, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
    }
}
