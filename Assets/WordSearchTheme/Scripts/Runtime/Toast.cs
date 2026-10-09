using UnityEngine;

/// <summary>Native Android toast ("that didn't work" messages). Editor/iOS: logs instead.</summary>
public static class Toast
{
    public const string VideoUnavailable = "Video not available right now. Please try again.";

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject activity; // never disposed on purpose: the runnable runs later on the UI thread
#endif

    public static void Show(string message, bool longDuration = false)
    {
        Debug.Log("[Toast] " + message);
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (activity == null)
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            int duration = longDuration ? 1 : 0; // Toast.LENGTH_LONG / LENGTH_SHORT
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
                    using (var text = new AndroidJavaObject("java.lang.String", message)) // boxed so it binds to the CharSequence overload
                    using (var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, text, duration))
                        toast.Call("show");
                }
                catch (System.Exception e) { Debug.LogWarning("[Toast] " + e.Message); }
            }));
        }
        catch (System.Exception e) { Debug.LogWarning("[Toast] " + e.Message); }
#endif
    }
}
