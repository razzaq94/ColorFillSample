using System.Collections;
using TMPro;
using UnityEngine;

public class GameLoseScreen : MonoBehaviour
{
    public static GameLoseScreen instance;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject noThanksButton;
    private void Awake()
    {
        instance = this;
    }
    public static GameLoseScreen ShowUI()
    {
        if (instance == null)
        {
            GameObject obj = Instantiate(Resources.Load("GameLoseScreen")) as GameObject;

            obj.gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("MainCanvas").transform, false);

            instance = obj.GetComponent<GameLoseScreen>();
        }
        instance.StartCountdown();
        return instance;
    }

    private void StartCountdown()
    {
        noThanksButton.SetActive(false);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = 9; i >= 0; i--)
        {
            countdownText.text = i.ToString("00");

            if (i == 7)
                noThanksButton.SetActive(true);

            yield return new WaitForSeconds(1f);
        }

        //Destroy(gameObject);
    }

    public void WatchAdd()
    {

    }

}
