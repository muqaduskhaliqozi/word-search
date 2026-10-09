using UnityEngine;

/// <summary>
/// The Android system font-size preference (Settings > Display > Font size), clamped so layouts
/// built from fixed-size buttons do not overflow. UGUI has no "sp" unit, so text that should honour
/// the preference goes through Size().
/// </summary>
public static class SystemFont
{
    public const float MaxScale = 1.3f;
    private static float scale = -1f;

    public static float Scale
    {
        get
        {
            if (scale > 0f) return scale;
            scale = 1f;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var res = activity.Call<AndroidJavaObject>("getResources"))
                using (var cfg = res.Call<AndroidJavaObject>("getConfiguration"))
                    scale = cfg.Get<float>("fontScale");
            }
            catch (System.Exception e) { Debug.LogWarning("[SystemFont] " + e.Message); scale = 1f; }
#endif
            scale = Mathf.Clamp(scale, 1f, MaxScale);
            return scale;
        }
    }

    public static float Size(float designSize) => designSize * Scale;
}
