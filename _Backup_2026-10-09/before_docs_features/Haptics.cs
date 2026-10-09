using UnityEngine;

/// <summary>
/// Short haptic taps of different strengths. Everything is silent when the player turns
/// "Vibrate" off in Settings (GameSettings.VibrateOn).
/// Android: uses the system Vibrator with VibrationEffect (API 26+) for crisp, light ticks.
/// iOS: falls back to Handheld.Vibrate for the stronger events only.
/// </summary>
public static class Haptics
{
    private static float lastTick;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdk = -1;
    private static bool hasAmplitude;

    private static bool Init()
    {
        if (sdk >= 0) return vibrator != null;
        try
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdk = version.GetStatic<int>("SDK_INT");
            using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator != null && !vibrator.Call<bool>("hasVibrator")) vibrator = null;
            hasAmplitude = vibrator != null && sdk >= 26 && vibrator.Call<bool>("hasAmplitudeControl");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Haptics] " + e.Message);
            vibrator = null;
            if (sdk < 0) sdk = 0;
        }
        return vibrator != null;
    }
#endif

    private static void Pulse(long ms, int amplitude, bool iosFallback)
    {
        if (!GameSettings.VibrateOn || !Application.isPlaying) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Init()) return;
        try
        {
            if (sdk >= 26)
            {
                using (AndroidJavaClass effect = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject e = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, hasAmplitude ? Mathf.Clamp(amplitude, 1, 255) : -1))
                    vibrator.Call("vibrate", e);
            }
            else
            {
                vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Haptics] " + ex.Message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        if (iosFallback) Handheld.Vibrate();
#endif
    }

    /// <summary>Tiny tick while dragging across letters (rate-limited).</summary>
    public static void Selection()
    {
        if (Time.unscaledTime - lastTick < 0.045f) return;
        lastTick = Time.unscaledTime;
        Pulse(12, 60, false);
    }

    /// <summary>Button taps.</summary>
    public static void Light() => Pulse(15, 80, false);

    /// <summary>Hint / shuffle / reward.</summary>
    public static void Medium() => Pulse(30, 150, false);

    /// <summary>Word found.</summary>
    public static void Success() => Pulse(45, 200, true);

    /// <summary>Wrong word.</summary>
    public static void Error() => Pulse(70, 120, false);

    /// <summary>Level complete.</summary>
    public static void Heavy() => Pulse(120, 255, true);

    // keeps Unity's automatic VIBRATE permission in the Android manifest
    private static void KeepPermission() { Handheld.Vibrate(); }
}
