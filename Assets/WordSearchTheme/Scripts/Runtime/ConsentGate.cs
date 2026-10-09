using System;
using UnityEngine;

/// <summary>
/// First-launch Welcome panel with Terms of Service / Privacy Policy links and an Accept button.
/// Nothing that tracks the player (ads mediation, SolarEngine, Firebase, ByteBrew, IAP) starts until Accept;
/// those initializers call ConsentGate.RunWhenAccepted(...) instead of starting directly.
/// After the first Accept the panel never appears again and startup is unchanged.
/// Note: a Terms acknowledgement, not a full GDPR consent flow (there is no decline path).
/// </summary>
public class ConsentGate : MonoBehaviour
{
    public const string AcceptedKey = "WS_ConsentAccepted";
    public const int SortingOrder = 32000; // above no-internet (31000) so a first launch offline cannot deadlock

    private static Action waiting;
    private static ConsentGate instance;
    private RuntimeUI.Panel panel;

    public static bool Accepted => PlayerPrefs.GetInt(AcceptedKey, 0) == 1;
    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.root != null && instance.panel.root.activeSelf;

    /// <summary>Runs now if the player already accepted, otherwise queues it until Accept.</summary>
    public static void RunWhenAccepted(Action action)
    {
        if (action == null) return;
        if (Accepted) { action(); return; }
        waiting += action;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetQueue() { waiting = null; instance = null; } // editor domain-reload safety

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Accepted || instance != null) return;
        GameObject go = new GameObject("ConsentGate");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ConsentGate>();
    }

    private void Start()
    {
        panel = RuntimeUI.Create("ConsentGate UI", SortingOrder, new Vector2(900, 900), true, true);
        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Title", 0, 40, new Vector2(760, 76)), "Welcome!", 52, Color.white);

        SkyUIResources r = SkyUIResources.Get();
        if (r != null && r.logo != null)
        {
            var logo = RuntimeUI.NewImage("Logo", panel.card, r.logo);
            logo.preserveAspect = true;
            RectTransform lr = logo.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 1f);
            lr.anchoredPosition = new Vector2(0, -(24 + 250));
            lr.sizeDelta = new Vector2(520, 300);
        }

        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Body", 0, 470, new Vector2(780, 150)),
            "Before you play, please read and accept our Terms of Service and Privacy Policy.",
            38, RuntimeUI.TextDark, false, TMPro.TextAlignmentOptions.Center, true);

        // links: two separate tappable labels, each measured on its own (no fixed-width row to overlap)
        LinkButton("Terms", "Terms of Service", -200, 590, () => GameLinks.Open(GameLinks.TermsOfService, "Terms of Service"));
        LinkButton("Privacy", "Privacy Policy", 200, 590, () => GameLinks.Open(GameLinks.PrivacyPolicy, "Privacy Policy"));

        RuntimeUI.PillButton(panel, "Accept", 760, 560, true, "Accept & Play", Accept);
        StartCoroutine(RuntimeUI.Pop(panel.card));
    }

    private void LinkButton(string name, string text, float x, float y, Action onClick)
    {
        RectTransform rt = RuntimeUI.OnCard(panel, name, x, y, new Vector2(360, 70));
        var hit = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(1, 1, 1, 0);
        var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = hit;
        b.onClick.AddListener(() => onClick());
        // the text needs its own object: a GameObject can hold only one Graphic (the Image above)
        var textGo = new GameObject("Text", typeof(RectTransform));
        var tr = (RectTransform)textGo.transform;
        tr.SetParent(rt, false);
        RuntimeUI.Stretch(tr);
        var t = RuntimeUI.Label(tr, "<u>" + text + "</u>", 36, RuntimeUI.TextBlue);
        t.raycastTarget = false;
    }

    private void Update()
    {
        // Android back on the consent screen: swallowed (never quits, never skips consent)
        if (Input.GetKeyDown(KeyCode.Escape)) { }
    }

    private void Accept()
    {
        PlayerPrefs.SetInt(AcceptedKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[ConsentGate] accepted - starting SDKs");
        Action run = waiting;
        waiting = null;
        if (panel != null && panel.root != null) Destroy(panel.root);
        try { run?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
        Destroy(gameObject);
        instance = null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Word Search/Consent Gate/Reset Consent (replay first launch)")]
    private static void ResetConsent() { PlayerPrefs.DeleteKey(AcceptedKey); PlayerPrefs.Save(); Debug.Log("[ConsentGate] reset"); }

    [UnityEditor.MenuItem("Tools/Word Search/Consent Gate/Grant Consent (skip the panel)")]
    private static void GrantConsent() { PlayerPrefs.SetInt(AcceptedKey, 1); PlayerPrefs.Save(); Debug.Log("[ConsentGate] granted"); }
#endif
}
