using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
#if BYTEBREW
using ByteBrewSDK;
#endif

/// <summary>
/// The only file that names the ByteBrew API. Analytics events + remote config reads behind the
/// BYTEBREW scripting define, so the project still builds (and logs a warning) without the SDK.
/// Starts after the consent gate. Fill in Game ID / SDK key in Assets/ByteBrewSDK/Resources/ByteBrewSettings.
/// adb logcat -s Unity | grep ByteBrew
/// </summary>
public class GameEvents : MonoBehaviour
{
#if BYTEBREW
    private const bool Enabled = true;
#else
    private const bool Enabled = false;
#endif
    private const float RemoteConfigFetchDelay = 10f; // native SDK drops fetches made right after init

    private static GameEvents host;
    private static bool initialized;
    private static bool bannerReportedThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (host != null) return;
        GameObject go = new GameObject("GameEvents");
        DontDestroyOnLoad(go);
        host = go.AddComponent<GameEvents>();
        if (!Enabled) Debug.LogWarning("[ByteBrew] BYTEBREW define is not set - every event is a no-op");
        ConsentGate.RunWhenAccepted(Initialize);
    }

    private static void Initialize()
    {
        if (initialized) return;
#if BYTEBREW
        try
        {
            if (ByteBrew.Instance == null)
            {
                // the native SDK sends its callbacks to a GameObject called "ByteBrew"
                GameObject bb = new GameObject("ByteBrew");
                bb.AddComponent<ByteBrew>();
            }
            ByteBrew.InitializeByteBrew();
            initialized = true;
            Debug.Log("[ByteBrew] initialized.");
            if (host != null) host.StartCoroutine(FetchRemoteConfigs());
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ByteBrew] init failed: " + e.Message);
        }
#endif
    }

#if BYTEBREW
    private static IEnumerator FetchRemoteConfigs()
    {
        for (int attempt = 1; attempt <= 3 && !ByteBrew.HasRemoteConfigsBeenSet(); attempt++)
        {
            yield return new WaitForSecondsRealtime(RemoteConfigFetchDelay); // realtime: ads may zero timeScale
            ByteBrew.RemoteConfigsUpdated(OnRemoteConfigsLoaded);
        }
        if (ByteBrew.HasRemoteConfigsBeenSet()) OnRemoteConfigsLoaded();
    }

    private static void OnRemoteConfigsLoaded()
    {
        Debug.Log("[ByteBrew] remote configs loaded: inter_ad_start_level=" + RemoteConfigInt(AdGate.StartLevelKey, AdGate.DefaultStartLevel)
                  + " inter_ad_delay_seconds=" + RemoteConfigInt(AdGate.DelayKey, AdGate.DefaultDelaySeconds));
    }
#endif

    // ------------------------------------------------------------------ events
    private static void Send(string name, Dictionary<string, string> parameters = null)
    {
        string p = "";
        if (parameters != null)
        {
            var parts = new List<string>();
            foreach (var kv in parameters) parts.Add(kv.Key + "=" + kv.Value);
            p = " {" + string.Join(", ", parts) + "}";
        }
#if BYTEBREW
        if (!initialized) { Debug.Log($"[ByteBrew] {name}{p} fired before Initialize() (consent not accepted yet?) - dropped"); return; }
        Debug.Log($"[ByteBrew] {name}{p}");
        try
        {
            if (parameters != null) ByteBrew.NewCustomEvent(name, parameters);
            else ByteBrew.NewCustomEvent(name);
        }
        catch (Exception e) { Debug.LogWarning("[ByteBrew] send failed: " + e.Message); }
#else
        Debug.Log($"[ByteBrew] (disabled) {name}{p}");
#endif
    }

    public static void LevelComplete(int levelNumber) =>
        Send("LevelComplete", new Dictionary<string, string> { { "Level_number", levelNumber.ToString(CultureInfo.InvariantCulture) } });

    public static void LevelStart(int levelNumber) =>
        Send("LevelStart", new Dictionary<string, string> { { "Level_number", levelNumber.ToString(CultureInfo.InvariantCulture) } });

    public static void InterAdsShown() => Send("InterAdsShown");
    public static void RewardedAdsShown() => Send("RewardedAdsShown");

    /// <summary>Banners refresh every 30-60 s, so this is reported once per session to protect the event quota.</summary>
    public static void BannerAdsShown()
    {
        if (bannerReportedThisSession) return;
        bannerReportedThisSession = true;
        Send("BannerAdsShown");
    }

    public static void RemoveAdsPurchased() => Send("RemoveAdsPurchased");
    public static void PowerUpUsed(string power) => Send("PowerUpUsed", new Dictionary<string, string> { { "type", power } });
    public static void ReviewRequested(int levelNumber) =>
        Send("ReviewRequested", new Dictionary<string, string> { { "Level_number", levelNumber.ToString(CultureInfo.InvariantCulture) } });

    // ------------------------------------------------------------------ remote config
    /// <summary>
    /// Integer remote config value. Never throws or blocks: returns the fallback when the SDK is off,
    /// configs have not arrived, or the value does not parse. Accepts "60", "60.0".
    /// </summary>
    public static int RemoteConfigInt(string key, int fallback)
    {
#if BYTEBREW
        try
        {
            if (!initialized || !ByteBrew.HasRemoteConfigsBeenSet())
            {
                Debug.Log($"[ByteBrew] remote configs have NOT loaded - {key} uses fallback {fallback}");
                return fallback;
            }
            string fb = fallback.ToString(CultureInfo.InvariantCulture);
            string raw = ByteBrew.GetRemoteConfigForKey(key, fb);
            if (raw == fb) Debug.Log($"[ByteBrew] remote config {key} came back equal to the fallback ({fb}) - missing on the dashboard?");
            int i;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) { Debug.Log($"[ByteBrew] remote config {key} = {i}"); return i; }
            float f;
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) { i = Mathf.RoundToInt(f); Debug.Log($"[ByteBrew] remote config {key} = {i}"); return i; }
            Debug.LogWarning($"[ByteBrew] remote config {key} = '{raw}' is not a number - using {fallback}");
        }
        catch (Exception e) { Debug.LogWarning("[ByteBrew] remote config read failed: " + e.Message); }
#endif
        return fallback;
    }
}
