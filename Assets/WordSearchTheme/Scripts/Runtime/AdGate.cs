using System;
using UnityEngine;

/// <summary>
/// When the post-level interstitial is allowed: from level N on, and at least D seconds after the
/// previous interstitial closed. N and D come from remote config (ByteBrew) so they can be retuned
/// without a build; the defaults below are used until/unless the dashboard answers.
/// adb logcat -s Unity | grep AdGate
/// </summary>
public static class AdGate
{
    public const string StartLevelKey = "inter_ad_start_level";
    public const string DelayKey = "inter_ad_delay_seconds";
    public const int DefaultStartLevel = 3;
    public const int DefaultDelaySeconds = 60;
    private const string LastClosedKey = "WS_LastInterstitialUtc";

    /// <param name="completedLevel">1-based number of the level just completed.</param>
    public static bool AllowInterstitial(int completedLevel)
    {
        int startLevel = GameEvents.RemoteConfigInt(StartLevelKey, DefaultStartLevel);
        int delay = GameEvents.RemoteConfigInt(DelayKey, DefaultDelaySeconds);

        if (completedLevel < startLevel)
        {
            Debug.Log($"[AdGate] level {completedLevel} < start level {startLevel} - no interstitial");
            return false;
        }
        if (delay <= 0) return true;

        DateTime last;
        if (!TryReadLastClosed(out last)) return true;
        DateTime now = DateTime.UtcNow;
        if (last > now)
        {
            Debug.Log("[AdGate] stored timestamp is in the future (clock moved) - clearing");
            PlayerPrefs.DeleteKey(LastClosedKey);
            return true;
        }
        double since = (now - last).TotalSeconds;
        if (since < delay)
        {
            Debug.Log($"[AdGate] only {since:0}s since the last interstitial closed (need {delay}s)");
            return false;
        }
        return true;
    }

    /// <summary>Call from the interstitial hidden/dismissed callback (not on show).</summary>
    public static void NotifyInterstitialClosed()
    {
        PlayerPrefs.SetString(LastClosedKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private static bool TryReadLastClosed(out DateTime value)
    {
        value = default(DateTime);
        string s = PlayerPrefs.GetString(LastClosedKey, "");
        long ticks;
        if (string.IsNullOrEmpty(s) || !long.TryParse(s, out ticks)) return false;
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) { PlayerPrefs.DeleteKey(LastClosedKey); return false; }
        value = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }
}
