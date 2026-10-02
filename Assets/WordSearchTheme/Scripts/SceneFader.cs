using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent full-screen fade used for every scene change.
/// Usage: SceneFader.LoadScene("MainMenu");
/// </summary>
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    private CanvasGroup group;
    private bool busy;

    public static bool IsBusy => instance != null && instance.busy;

    private static SceneFader Instance
    {
        get
        {
            if (instance == null) Create();
            return instance;
        }
    }

    private static void Create()
    {
        GameObject go = new GameObject("[SceneFader]");
        DontDestroyOnLoad(go);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        go.AddComponent<GraphicRaycaster>();

        GameObject img = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        img.transform.SetParent(go.transform, false);
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.10f, 1f);

        instance = go.AddComponent<SceneFader>();
        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
    }

    public static void LoadScene(string sceneName, float fadeTime = 0.35f)
    {
        if (Instance.busy) return;
        Instance.StartCoroutine(Instance.LoadRoutine(sceneName, fadeTime));
    }

    /// <summary>Starts fully covered and fades away (use on the very first scene).</summary>
    public static void FadeInFromBlack(float fadeTime = 0.5f)
    {
        Instance.StopAllCoroutines();
        Instance.group.alpha = 1f;
        Instance.StartCoroutine(Instance.FadeRoutine(1f, 0f, fadeTime));
    }

    private IEnumerator LoadRoutine(string sceneName, float fadeTime)
    {
        busy = true;
        group.blocksRaycasts = true;
        yield return FadeRoutine(group.alpha, 1f, fadeTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op != null)
        {
            while (!op.isDone) yield return null;
        }
        else
        {
            Debug.LogError($"[SceneFader] Scene '{sceneName}' could not be loaded. Is it in Build Settings?");
        }

        yield return null; // let the new scene run its first Start()
        yield return FadeRoutine(1f, 0f, fadeTime);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator FadeRoutine(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Tween.InOutSine(Mathf.Clamp01(t / time)));
            yield return null;
        }
        group.alpha = to;
    }
}
