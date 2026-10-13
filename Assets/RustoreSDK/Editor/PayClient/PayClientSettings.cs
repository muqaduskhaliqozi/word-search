using UnityEditor;
using UnityEngine;

namespace RuStore.Editor
{

    public class PayClientSettings : RuStoreModuleSettings
    {

        public const int CURRENT_SCHEME_VERSION = 1;

        [HideInInspector]
        public int schemeVersion = 0;

        public string consoleApplicationId;
        public readonly string internalConfigKey = "unity";
        public string deeplinkScheme;
        public string customMainActivityClass = "com.unity3d.player.UnityPlayerActivity";
        public string unityMainActivityClass = "com.unity3d.player.UnityPlayerActivity";
        public string customDeeplinkActivityClass = "ru.rustore.unitysdk.RuStoreDeeplinkActivityDefault";
        public string rustoreDeeplinkActivityClass = "ru.rustore.unitysdk.RuStoreDeeplinkActivityDefault";

        [MenuItem("Window/RuStoreSDK/Settings/PayClient")]
        public static void EditPayClientSettings()
        {
            EditSettings<PayClientSettings>(settings =>
            {
                settings.schemeVersion = CURRENT_SCHEME_VERSION;
            });
        }
    }
}
