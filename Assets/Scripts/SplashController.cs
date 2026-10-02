using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashController : MonoBehaviour
{
    [Header("Loading Bar")]
    [SerializeField] private Slider loadingBar;

    [Header("Settings")]
    [SerializeField] private float loadingDuration = 6f;
    [SerializeField] private string nextSceneName = "MainMenu";

    private void Start()
    {
        loadingBar.value = 0f;

        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / loadingDuration);

            loadingBar.value = progress;

            yield return null;
        }

        loadingBar.value = 1f;

        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(nextSceneName);
    }
}