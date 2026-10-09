using UnityEngine;

/// <summary>
/// Haptic feedback. Silent when the player turns "Vibrate" off in Settings (GameSettings.VibrateOn).
/// Android 10+ (API 29): the system's predefined haptic effects (tick / click / heavy click / double click),
/// which every manufacturer tunes for its own motor - these are what you actually feel on button taps.
/// Older Android: one-shot pulses long enough for cheap motors to spin up.
/// iOS: Handheld.Vibrate for the strong events only.
/// adb logcat -s Unity | grep Haptics  -> one init line telling you what the device supports.
/// </summary>
public static class Haptics
{
    // android.os.VibrationEffect predefined ids
    private const int EffectClick = 0, EffectDoubleClick = 1, EffectTick = 2, EffectHeavyClick = 5;

    private static float lastTick;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdk = -1;
    private static bool hasAmplitude;
    private static bool loggedError;

    private static bool Init()
    {
        if (sdk >= 0) return vibrator != null;
        try
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdk = version.GetStatic<int>("SDK_INT");
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (sdk >= 31)
                {
                    using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                        vibrator = manager != null ? manager.Call<AndroidJavaObject>("getDefaultVibrator") : null;
                }
                if (vibrator == null) vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            bool has = vibrator != null && vibrator.Call<bool>("hasVibrator");
            if (!has) vibrator = null;
            hasAmplitude = vibrator != null && sdk >= 26 && vibrator.Call<bool>("hasAmplitudeControl");
            Debug.Log($"[Haptics] init: api={sdk} hasVibrator={has} amplitudeControl={hasAmplitude}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Haptics] init failed: " + e.Message);
            vibrator = null;
            if (sdk < 0) sdk = 0;
        }
        return vibrator != null;
    }

    private static void Play(int predefined, long fallbackMs, int amplitude)
    {
        if (!Init()) return;
        try
        {
            if (sdk >= 29 && predefined >= 0)
            {
                using (AndroidJavaClass effect = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject e = effect.CallStatic<AndroidJavaObject>("createPredefined", predefined))
                    vibrator.Call("vibrate", e);
            }
            else if (sdk >= 26)
            {
                using (AndroidJavaClass effect = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject e = effect.CallStatic<AndroidJavaObject>("createOneShot", fallbackMs, hasAmplitude ? Mathf.Clamp(amplitude, 1, 255) : -1))
                    vibrator.Call("vibrate", e);
            }
            else
            {
                vibrator.Call("vibrate", fallbackMs);
            }
        }
        catch (System.Exception ex)
        {
            if (!loggedError) { loggedError = true; Debug.LogWarning("[Haptics] vibrate failed: " + ex.Message); }
        }
    }
#endif

    private static void Pulse(int predefined, long fallbackMs, int amplitude, bool iosToo)
    {
        if (!GameSettings.VibrateOn || !Application.isPlaying) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        Play(predefined, fallbackMs, amplitude);
#elif UNITY_IOS && !UNITY_EDITOR
        if (iosToo) Handheld.Vibrate();
#endif
    }

    /// <summary>Tick while dragging across letters (rate-limited).</summary>
    public static void Selection()
    {
        if (Time.unscaledTime - lastTick < 0.05f) return;
        lastTick = Time.unscaledTime;
        Pulse(EffectTick, 25, 140, false);
    }

    /// <summary>Button taps.</summary>
    public static void Light() => Pulse(EffectClick, 30, 170, false);

    /// <summary>Hint / shuffle / reward.</summary>
    public static void Medium() => Pulse(EffectClick, 40, 210, false);

    /// <summary>Word found.</summary>
    public static void Success() => Pulse(EffectHeavyClick, 55, 255, true);

    /// <summary>Wrong word.</summary>
    public static void Error() => Pulse(EffectDoubleClick, 90, 200, false);

    /// <summary>Level complete: a long strong buzz everywhere.</summary>
    public static void Heavy() => Pulse(-1, 160, 255, true);

    // keeps Unity's automatic VIBRATE permission in the Android manifest
    private static void KeepPermission() { Handheld.Vibrate(); }
}
