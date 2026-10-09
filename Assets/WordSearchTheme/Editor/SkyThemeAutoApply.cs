using System.IO;
using UnityEditor;

/// <summary>
/// Runs "Apply Sky Menu Theme" once, automatically, after scripts compile,
/// when the marker file Assets/WordSearchTheme/Editor/.sky_apply_pending exists (deleted before running).
/// </summary>
[InitializeOnLoad]
public static class SkyThemeAutoApply
{
    private const string Marker = "Assets/WordSearchTheme/Editor/.sky_apply_pending";

    static SkyThemeAutoApply()
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
        SkyThemeBuilder.ApplyAll();
    }
}
