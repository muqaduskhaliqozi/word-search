#if UNITY_EDITOR
using UnityEditor;

namespace RuStore.Editor {

    internal static class PayClientSettingsRepository {

        public static PayClientSettings Load() {
            return RuStoreModuleSettings.LoadAsset<PayClientSettings>();
        }

        public static PayClientSettings LoadFromInspector(SerializedObject serializedObject, UnityEngine.Object target) {
            if (target is PayClientSettings fromInspector) {
                serializedObject?.ApplyModifiedProperties();
                EditorUtility.SetDirty(fromInspector);
                AssetDatabase.SaveAssetIfDirty(fromInspector);

                return fromInspector;
            }

            return Load();
        }
    }
}
#endif
