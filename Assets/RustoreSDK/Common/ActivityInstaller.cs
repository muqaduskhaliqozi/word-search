using RuStore.Internal;
using UnityEngine;

namespace RuStore {

    public sealed class ActivityInstaller {

        private static ActivityInstaller instance;

        private AndroidJavaObject activityProviderJavaInstance;
        private ActivityProviderListener activityProviderListener;
        private bool isInstalled;

        public static ActivityInstaller Instance {
            get {
                return instance ??= new ActivityInstaller();
            }
        }

        private ActivityInstaller() { }

        public void Install() {
#if UNITY_ANDROID
            if (isInstalled) return;

            using (var cls = new AndroidJavaClass("ru.rustore.unitysdk.core.PlayerProvider")) {
                activityProviderJavaInstance = cls.GetStatic<AndroidJavaObject>("INSTANCE");
            }

            activityProviderListener = new ActivityProviderListener(UnityActivityProvider.GetCurrentActivity);
            activityProviderJavaInstance.Call("setExternalProvider", activityProviderListener);

            isInstalled = true;
#endif
        }

        public void Uninstall() {
#if UNITY_ANDROID
            if (!isInstalled) return;

            try
            {
                activityProviderJavaInstance?.Call("setExternalProvider", null);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    $"[RuStore] PlayerProviderInstaller: failed to clear external provider. " +
                    $"Exception: {ex.GetType().Name}: {ex.Message}"
                );
            }

            activityProviderListener = null;
            activityProviderJavaInstance = null;
            isInstalled = false;
#endif
        }
    }
}
