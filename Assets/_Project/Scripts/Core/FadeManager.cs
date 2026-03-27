using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeTime = 0.5f;

    private void Awake()
    {
        Instance = this;

        fadeGroup.alpha = 0;
        fadeGroup.gameObject.SetActive(false);
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeRoutine(sceneName));
    }

    private IEnumerator FadeRoutine(string sceneName)
    {
        float time = 0;

        fadeGroup.gameObject.SetActive(true);

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = time / fadeTime;
            yield return null;
        }

        fadeGroup.alpha = 1;

        yield return SceneManager.LoadSceneAsync(sceneName);

        time = fadeTime;

        while (time > 0)
        {
            time -= Time.deltaTime;
            fadeGroup.alpha = time / fadeTime;
            yield return null;
        }

        fadeGroup.alpha = 0;

        fadeGroup.gameObject.SetActive(false);
    }
}