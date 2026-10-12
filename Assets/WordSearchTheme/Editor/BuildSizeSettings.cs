using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Android size settings (applied once automatically, re-apply / revert from the menu):
///  - IL2CPP code generation "Optimize for size" -> smaller libil2cpp.so
///  - Minify Release (R8) + Assets/Plugins/Android/proguard-user.txt keep rules -> much smaller dex
/// Applied through the PlayerSettings API so the open Editor does not overwrite it.
/// </summary>
[InitializeOnLoad]
public static class BuildSizeSettings
{
    private const string Marker = "Assets/WordSearchTheme/Editor/.size_apply_pending";

    static BuildSizeSettings()
    {
        if (!File.Exists(Marker)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Marker)) return;
            File.Delete(Marker);
            Apply();
        };
    }

    [MenuItem("Tools/Word Search/Build Size/Apply Size Settings")]
    public static void Apply()
    {
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.Android.minifyRelease = true;
        PlayerSettings.Android.minifyDebug = false;
        SetCustomProguard(true);
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildSize] Android: IL2CPP code generation = Optimize Size, Minify Release (R8) = ON, " +
                  "custom proguard-user.txt = ON. Test ads, IAP, review, notifications and ByteBrew on a device.");
    }

    [MenuItem("Tools/Word Search/Build Size/Revert Minify (if a release build misbehaves)")]
    public static void RevertMinify()
    {
        PlayerSettings.Android.minifyRelease = false;
        SetCustomProguard(false);
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildSize] Minify Release OFF (IL2CPP Optimize Size kept).");
    }

    private static void SetCustomProguard(bool on)
    {
        Object ps = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        if (ps == null) return;
        SerializedObject so = new SerializedObject(ps);
        SerializedProperty p = so.FindProperty("useCustomProguardFile");
        if (p == null) { Debug.LogWarning("[BuildSize] tick Publishing Settings > Custom Proguard File manually"); return; }
        p.boolValue = on;
        so.ApplyModifiedProperties();
    }
}
