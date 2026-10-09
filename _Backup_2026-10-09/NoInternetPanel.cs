using System.Collections;
using UnityEngine;

/// <summary>
/// Blocking "No internet" panel with Retry. Watches connectivity everywhere (it lives across scenes),
/// covers the screen when it drops and hides itself when the signal returns.
/// NOTE: Application.internetReachability only says a network interface exists - captive portals
/// read as online, and a VPN can keep it "online" with wifi off. Use the Simulate Offline menu to test.
/// Sorting 31000: above the game and the remove-ads offer, below the consent gate (32000).
/// adb logcat -s Unity | grep NoInternet
/// </summary>
public class NoInternetPanel : MonoBehaviour
{
    public const int SortingOrder = 31000;
    [SerializeField] private float pollSeconds = 1f;

    private static NoInternetPanel instance;
    private RuntimeUI.Panel panel;
    private bool lastOnline = true;

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.root != null && instance.panel.root.activeSelf;

#if UNITY_EDITOR
    private const string SimKey = "WS_SimulateOffline";
    private static bool SimulateOffline => UnityEditor.EditorPrefs.GetBool(SimKey, false); // survives entering Play

    [UnityEditor.MenuItem("Tools/Word Search/No Internet/Simulate Offline (toggle)")]
    private static void ToggleSim() { UnityEditor.EditorPrefs.SetBool(SimKey, !SimulateOffline); Debug.Log("[NoInternet] Simulate offline = " + SimulateOffline); }

    [UnityEditor.MenuItem("Tools/Word Search/No Internet/Simulate Offline (toggle)", true)]
    private static bool ToggleSimCheck() { UnityEditor.Menu.SetChecked("Tools/Word Search/No Internet/Simulate Offline (toggle)", SimulateOffline); return true; }

    [UnityEditor.MenuItem("Tools/Word Search/No Internet/Log Reachability")]
    private static void LogReach() { Debug.Log("[NoInternet] Application.internetReachability = " + Application.internetReachability); }
#else
    private static bool SimulateOffline => false;
#endif

    public static bool Online => !SimulateOffline && Application.internetReachability != NetworkReachability.NotReachable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("NoInternetPanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<NoInternetPanel>();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            bool online = Online;
            if (online != lastOnline)
            {
                Debug.Log("[NoInternet] connectivity " + (online ? "restored" : "lost") + " (" + Application.internetReachability + ")");
                lastOnline = online;
            }
            // one branch for both directions, so recovery can't be forgotten
            if (online) { if (IsOpen) Hide(); }
            else if (!IsOpen) ShowPanel();
            yield return new WaitForSecondsRealtime(pollSeconds);
        }
    }

    private void ShowPanel()
    {
        if (panel == null)
        {
            panel = RuntimeUI.Create("NoInternet UI", SortingOrder, new Vector2(860, 640), true, true);
            RuntimeUI.Label(RuntimeUI.OnCard(panel, "Title", 0, 40, new Vector2(700, 76)), "No Internet", 52, Color.white);
            RuntimeUI.Label(RuntimeUI.OnCard(panel, "Body", 0, 230, new Vector2(740, 220)),
                "Please check your Wi-Fi or mobile data connection and try again.",
                40, RuntimeUI.TextDark, false, TMPro.TextAlignmentOptions.Center, true);
            RuntimeUI.PillButton(panel, "Retry", 470, 480, true, "Retry", Retry);
        }
        panel.root.SetActive(true);
        StartCoroutine(RuntimeUI.Pop(panel.card));
    }

    private void Hide()
    {
        if (panel != null && panel.root != null) panel.root.SetActive(false);
    }

    private void Retry()
    {
        if (Online) Hide();
        else StartCoroutine(RuntimeUI.Shake(panel.card)); // a silent Retry reads as broken
    }

    private void Update()
    {
        // back while blocked: swallowed (the panel has no dismiss on purpose)
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) { }
    }
}
