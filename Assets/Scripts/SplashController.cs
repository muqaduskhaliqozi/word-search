using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplashController : MonoBehaviour
{
    [Header("Loading Bar")]
    [SerializeField] private Slider loadingBar;

    [Header("Optional Visuals")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private string loadingWord = "Loading";
    [SerializeField] private RectTransform barShine; // little glow that rides on the bar's fill edge

    [Header("Settings")]
    [SerializeField] private float loadingDuration = 6f;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "GamePlay";
    public const string FirstSessionKey = "WS_FirstSessionStarted";

    private void Start()
    {
        if (loadingBar != null) loadingBar.value = 0f;
        SceneFader.FadeInFromBlack(0.6f);
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        // first launch: hold the whole splash until Terms/Privacy are accepted
        // (the SDKs only start at Accept, so running the timer underneath would waste it)
        while (!ConsentGate.Accepted) yield return null;

        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            // no internet: the bar stops where it is until the connection is back (NoInternetPanel covers the screen)
            if (!NoInternetPanel.Online) { yield return null; continue; }
            elapsed += Time.deltaTime;

            float k = Mathf.Clamp01(elapsed / loadingDuration);
            // ease with a couple of natural "hiccups" so it feels like real loadingi
            float progress = Tween.InOutSine(k) * 0.85f + k * 0.15f;

            if (loadingBar != null) loadingBar.value = progress;

            if (loadingText != null)
            {
                int dots = (int)(elapsed * 3f) % 4;
                loadingText.text = loadingWord + new string('.', dots) + "<alpha=#00>" + new string('.', 3 - dots);
            }

            if (barShine != null && loadingBar != null && loadingBar.fillRect != null)
            {
                RectTransform fill = loadingBar.fillRect;
                barShine.position = fill.TransformPoint(new Vector3(fill.rect.xMax, fill.rect.center.y, 0f));
            }

            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;

        yield return new WaitForSecondsRealtime(0.2f);
        while (!NoInternetPanel.Online) yield return null; // never leave the splash offline

        // First session ever: straight into level 1 (tutorial). Every later session: the main menu.
        string target = nextSceneName;
        if (PlayerPrefs.GetInt(FirstSessionKey, 0) == 0)
        {
            PlayerPrefs.SetInt(FirstSessionKey, 1);
            PlayerPrefs.Save();
            LevelProgress.RequestLevel(0);
            target = gameSceneName;
        }
        SceneFader.LoadScene(target);
    }
}
