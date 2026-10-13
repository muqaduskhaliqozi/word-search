#if UNITY_EDITOR
using RuStore.Editor.EntryPoint;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RuStore.Editor {
    public sealed class RuStorePayClientPreBuild : IPreprocessBuildWithReport {
        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report) {
            if (report.summary.platform != BuildTarget.Android) return;

            var settings = PayClientSettingsRepository.Load();
            if (settings == null) {
                Debug.LogWarning("[RuStore] PayClientSettings not found, skip entry point override generation.");
                return;
            }

            RuStoreEntryPointOverrideGenerator.Instance.Overwrite(settings.unityMainActivityClass);
        }
    }
}
#endif
