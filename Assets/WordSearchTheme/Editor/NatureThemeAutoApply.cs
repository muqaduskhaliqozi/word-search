using System.IO;
using UnityEditor;

/// <summary>
/// Runs "Apply Nature Theme (All Scenes)" once, automatically, after scripts compile,
/// when the marker file Assets/WordSearchTheme/Editor/.apply_pending exists.
/// (The marker is deleted before running, so this never loops.)
/// </summary>
[InitializeOnLoad]
public static class NatureThemeAutoApply
{
    private const string Marker = "Assets/WordSearchTheme/Editor/.apply_pending";

    static NatureThemeAutoApply()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (!File.Exists(Marker)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }
        File.Delete(Marker);
        NatureThemeBuilder.ApplyAll();
    }
}
