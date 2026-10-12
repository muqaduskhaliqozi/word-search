using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Connecting to store" panel shown from the moment a Remove Ads button is tapped until the store answers:
/// bought, cancelled, failed, or not reachable. Blocks taps underneath so the button can't be spammed.
/// Safety nets: closes a few seconds after returning from the Google Play sheet even if no callback arrived,
/// and after <see cref="TimeoutSeconds"/> of waiting in the app with no store response.
/// </summary>
public class IAPLoading : MonoBehaviour
{
    public const int SortingOrder = 30500;     // above the Remove Ads offer (30000), below no-internet (31000)
    public const float TimeoutSeconds = 25f;   // in-app time without any store response
    public const float AfterReturnSeconds = 4f; // back from the payment sheet without a callback

    private static IAPLoading instance;
    private static Sprite spinnerSprite;

    private RuntimeUI.Panel panel;
    private RectTransform spinner;
    private float waited;
    private bool leftApp;
    private Coroutine returnWatch;

    public static bool IsOpen => instance != null;

    public static void Show()
    {
        if (instance != null) { instance.waited = 0f; return; }
        GameObject go = new GameObject("IAPLoading");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<IAPLoading>();
        instance.Build();
    }

    /// <summary>Closes the panel; optional toast explains why (null for cancel / success).</summary>
    public static void Hide(string toast = null)
    {
        if (instance != null)
        {
            if (instance.panel != null && instance.panel.root != null) Destroy(instance.panel.root);
            Destroy(instance.gameObject);
            instance = null;
        }
        if (!string.IsNullOrEmpty(toast)) Toast.Show(toast);
    }

    private void Build()
    {
        panel = RuntimeUI.Create("IAPLoading UI", SortingOrder, new Vector2(620, 430), false, true);

        Image sp = RuntimeUI.NewImage("Spinner", panel.card, SpinnerSprite());
        spinner = sp.rectTransform;
        spinner.anchorMin = spinner.anchorMax = new Vector2(0.5f, 1f);
        spinner.anchoredPosition = new Vector2(0, -(24 + 140));
        spinner.sizeDelta = new Vector2(130, 130);

        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Title", 0, 250, new Vector2(540, 64)),
            "Connecting to store...", 42, RuntimeUI.TextDark);
        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Sub", 0, 320, new Vector2(540, 50)),
            "Please wait", 32, RuntimeUI.TextBlue);

        StartCoroutine(RuntimeUI.Pop(panel.card));
    }

    private void Update()
    {
        if (spinner != null) spinner.Rotate(0f, 0f, 360f * Time.unscaledDeltaTime) /* counter-clockwise: the bright head leads */;
        if (leftApp) return; // the Google Play sheet is up: wait for it, however long the player takes
        waited += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (waited >= TimeoutSeconds)
        {
            Debug.LogWarning("[IAP] no store response in " + TimeoutSeconds + "s - closing the loading panel");
            Hide("Store is not responding. Please try again.");
        }
    }

    // The payment sheet is a separate activity: the game loses focus while it is open.
    private void OnApplicationFocus(bool focus) { OnAway(!focus); }
    private void OnApplicationPause(bool paused) { OnAway(paused); }

    private void OnAway(bool away)
    {
        if (away) { leftApp = true; if (returnWatch != null) { StopCoroutine(returnWatch); returnWatch = null; } return; }
        if (leftApp && returnWatch == null) returnWatch = StartCoroutine(ReturnWatch());
    }

    private IEnumerator ReturnWatch()
    {
        // normally the purchase callback (bought / cancelled) arrives right after the sheet closes
        yield return new WaitForSecondsRealtime(AfterReturnSeconds);
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (panel != null && panel.root != null) Destroy(panel.root);
    }

    /// <summary>Procedural ring with a fading tail (no texture asset needed).</summary>
    private static Sprite SpinnerSprite()
    {
        if (spinnerSprite != null) return spinnerSprite;
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32 c = new Color32(0x1E, 0x8C, 0xF0, 0xFF);
        Color32[] px = new Color32[n * n];
        float outer = n * 0.5f - 2f, inner = outer - 16f, mid = (outer + inner) * 0.5f, half = (outer - inner) * 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = x - n * 0.5f + 0.5f, dy = y - n * 0.5f + 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float edge = Mathf.Clamp01(half + 0.75f - Mathf.Abs(r - mid)); // anti-aliased ring
            float ang = (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f) % 360f; // 0..360, tail fades behind the head
            float tail = ang <= 300f ? ang / 300f : 0f; // bright head at 300deg, 60deg gap after it
            px[y * n + x] = new Color32(c.r, c.g, c.b, (byte)(255f * edge * (0.12f + 0.88f * tail * tail)));
        }
        tex.SetPixels32(px);
        tex.Apply();
        spinnerSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return spinnerSprite;
    }
}
