#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RuStore.Editor.EntryPoint {

    internal class RuStoreEntryPointOverrideGenerator {

        private readonly static string javaDir =
            Path.Combine("Assets", "RustoreSDK", "Java");

        private readonly static string javaFilePath =
            Path.Combine(javaDir, "RuStoreEntryPointOverride.java");

        private static RuStoreEntryPointOverrideGenerator instance = null;
        public static RuStoreEntryPointOverrideGenerator Instance {
            get {
                if (instance == null) instance = new RuStoreEntryPointOverrideGenerator();
                return instance;
            }
        }

        private RuStoreEntryPointOverrideGenerator() { }

        public void Overwrite(string activityClass) {
            if (string.IsNullOrWhiteSpace(activityClass)) {
                DeleteOverrideIfExists();
                Debug.Log("[RuStore] RuStoreEntryPointOverride.java removed (fallback to default entry activity).");
                return;
            }

            var contents = BuildJava(activityClass);

            Directory.CreateDirectory(javaDir);

            if (File.Exists(javaFilePath)) {
                var old = File.ReadAllText(javaFilePath);
                if (old == contents)
                    return;
            }

            File.WriteAllText(javaFilePath, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(javaFilePath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[RuStore] RuStoreEntryPointOverride.java overwritten. Entry game activity: {activityClass}");
        }

        private void DeleteOverrideIfExists() {
            if (!File.Exists(javaFilePath))
                return;

            File.Delete(javaFilePath);

            var metaPath = javaFilePath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            AssetDatabase.Refresh();
        }

        private string BuildJava(string activityClass) {
            var escaped = activityClass.Replace("\\", "\\\\").Replace("\"", "\\\"");

            return
$@"package ru.rustore.unitysdk;

public final class RuStoreEntryPointOverride {{
    private RuStoreEntryPointOverride() {{}}

    public static final String UNITY_PLAYER_ACTIVITY_CLASS = ""{escaped}"";
}}
";
        }
    }
}
#endif
