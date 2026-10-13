using UnityEngine;
using System;
using System.Reflection;

namespace RuStore.Internal {

    internal static class UnityActivityProvider {

        public static AndroidJavaObject GetCurrentActivity() {
#if UNITY_ANDROID
            // Unity 6+ API
            var androidApplicationType = Type.GetType("UnityEngine.AndroidApplication, UnityEngine");
            if (androidApplicationType != null) {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

                var prop = androidApplicationType.GetProperty("currentActivity", flags);
                if (prop != null && prop.GetValue(null) is AndroidJavaObject activityProp && activityProp != null)
                    return activityProp;

                var field = androidApplicationType.GetField("currentActivity", flags);
                if (field != null && field.GetValue(null) is AndroidJavaObject activityField && activityField != null)
                    return activityField;
            }

            // Legacy API
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#else
            return null;
#endif
        }
    }
}
