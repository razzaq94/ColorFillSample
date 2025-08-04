using UnityEngine;

public class InternetChecker : MonoBehaviour
{
    public static InternetChecker Instance;
    public GameObject noInternetImage;

    private bool isPausedByNoInternet = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (noInternetImage != null)
                noInternetImage.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartCoroutine(CheckInternetRoutine());
    }

    private System.Collections.IEnumerator CheckInternetRoutine()
    {
        while (true)
        {
            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

            if (!hasInternet && !isPausedByNoInternet)
            {
                if (noInternetImage != null)
                    noInternetImage.SetActive(true);
                isPausedByNoInternet = true;
                Time.timeScale = 0f;
            }
            else if (hasInternet && isPausedByNoInternet)
            {
                if (noInternetImage != null)
                    noInternetImage.SetActive(false);
                isPausedByNoInternet = false;
                Time.timeScale = 1f;
            }
            yield return new WaitForSecondsRealtime(1f); // Unaffected by timescale
        }
    }
}
