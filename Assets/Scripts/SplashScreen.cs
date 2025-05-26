using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[HideMonoScript]
public class SplashScreen : MonoBehaviour
{
    [Title("MAIN-MENU", null, titleAlignment: TitleAlignments.Centered)]
    public Image fillBar;
    public TextMeshProUGUI loadingText;
    public Image startPanel;
    public float fillTime = 3f;
    public float fadeTime = 1.5f;

    private static bool _hasShown = false;

   
        private void Awake()
    {
        if (_hasShown)
        {
            gameObject.SetActive(false);

            return;
        }

        _hasShown = true;
        StartCoroutine(FillBarRoutine());
    }

    private IEnumerator FillBarRoutine()
    {
        float elapsedTime = 0f;
        fillBar.fillAmount = 0f;
        string baseLoadingText = "Loading";

        while (elapsedTime < fillTime)
        {
            elapsedTime += Time.deltaTime;
            fillBar.fillAmount = Mathf.Clamp01(elapsedTime / fillTime);

            int dotCount = (int)((elapsedTime % 1f) * 4);
            loadingText.text = baseLoadingText + new string('.', dotCount);

            yield return null;
        }

        fillBar.fillAmount = 1f;
        loadingText.text = "Complete!";
        yield return StartCoroutine(FadeOutPanel());
    }
    private IEnumerator FadeOutPanel()
    {
        Graphic[] graphics = startPanel.GetComponentsInChildren<Graphic>(true);
        List<Color> originals = new List<Color>(graphics.Length);
        foreach (var g in graphics)
            originals.Add(g.color);

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeTime);

            for (int i = 0; i < graphics.Length; i++)
            {
                Color c = originals[i];
                c.a = alpha;
                graphics[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            Color c = originals[i];
            c.a = 0f;
            graphics[i].color = c;
        }

        Destroy(gameObject);
    }


}
