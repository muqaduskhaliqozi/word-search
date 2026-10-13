#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;
using System.Collections.Generic;

namespace RuStore.Editor.Manifest.Rule {

    public sealed class RemoveMainLauncherRule : IAndroidManifestRule {

        public void Apply(AndroidManifestEditor manifestEditor, PayClientSettings settings) {
            var gameActivityName = GetEntryActivityClassName(settings);
            if (gameActivityName != null) {
                RemoveMainLauncher(manifestEditor, gameActivityName);
            }
        }

        public bool Verify(AndroidManifestEditor manifestEditor, PayClientSettings settings, out string[] verifyMessages) {
            var messages = new List<string>();

            var gameActivityName = GetEntryActivityClassName(settings);

            var activity = manifestEditor.application
                .FindXElementByAttribute(
                    "activity",
                    AndroidManifestEditor.AndroidNs("name"),
                    gameActivityName
                );

            if (activity == null) {
                messages.Add($"GameActivity '{gameActivityName}' not found in project AndroidManifest.xml. Skip MAIN/LAUNCHER removal.");
                verifyMessages = messages.ToArray();

                return false;
            }

            var mainLauncherFilter = activity.GetXElement("intent-filter", AndroidManifestEditor.IsMainLauncherIntentFilter);
            if (mainLauncherFilter != null) {
                messages.Add($"Activity '{gameActivityName}' contains MAIN/LAUNCHER intent-filter. It should be removed when RuStore deeplink activity is launcher.");
            }

            verifyMessages = messages.ToArray();

            return verifyMessages.Length == 0;
        }

        private static string GetEntryActivityClassName(PayClientSettings settings) {
            var cls = settings?.unityMainActivityClass?.Trim();
            return string.IsNullOrEmpty(cls) ? null : cls;
        }

        private static void RemoveMainLauncher(AndroidManifestEditor manifestEditor, string activityName) {
            var activity = manifestEditor.application
                .FindXElementByAttribute(
                    "activity",
                    AndroidManifestEditor.AndroidNs("name"),
                    activityName
                );

            activity?.RemoveXElement("intent-filter", AndroidManifestEditor.IsMainLauncherIntentFilter);
        }
    }
}
#endif
