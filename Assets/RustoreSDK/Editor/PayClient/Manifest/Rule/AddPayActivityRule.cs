#nullable enable

#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.Rule {

    public sealed class AddPayActivityRule : IAndroidManifestRule {
        private const string payActivityClass = "ru.rustore.sdk.pay.internal.presentation.ui.PayActivity";
        private const string toolsNsUri = "http://schemas.android.com/tools";

        public void Apply(AndroidManifestEditor manifestEditor, PayClientSettings settings) {
            // Ensure xmlns:tools namespace is declared on <manifest>
            var m = manifestEditor.manifest;
            var existingToolsNs = m.Attribute(XNamespace.Xmlns + "tools");
            if (existingToolsNs == null) {
                m.SetAttributeValue(XNamespace.Xmlns + "tools", toolsNsUri);
            }

            var a = manifestEditor.GetOrCreateActivity(payActivityClass);
            a.SetAttributeValue(AndroidManifestEditor.AndroidNs("exported"), "false");
            a.SetAttributeValue(AndroidManifestEditor.AndroidNs("launchMode"), "singleTask");
            a.SetAttributeValue(AndroidManifestEditor.ToolsNs("replace"), "android:launchMode");
        }

        public bool Verify(AndroidManifestEditor manifestEditor, PayClientSettings settings, out string[] verifyMessages) {
            var messages = new List<string>();

            var a = manifestEditor.FindActivity(payActivityClass);
            if (a == null) {
                messages.Add($"Missing <activity android:name=\"{payActivityClass}\">.");
                verifyMessages = messages.ToArray();
                return false;
            }

            var exported = (string?)a.Attribute(AndroidManifestEditor.AndroidNs("exported"));
            if (exported != "false") {
                messages.Add($"Activity '{payActivityClass}': invalid android:exported. Expected 'false', actual '{exported ?? "<null>"}'.");
            }

            var launchMode = (string?)a.Attribute(AndroidManifestEditor.AndroidNs("launchMode"));
            if (launchMode != "singleTask") {
                messages.Add($"Activity '{payActivityClass}': invalid android:launchMode. Expected 'singleTask', actual '{launchMode ?? "<null>"}'.");
            }

            var toolsReplace = (string?)a.Attribute(AndroidManifestEditor.ToolsNs("replace"));
            if (toolsReplace != "android:launchMode") {
                messages.Add($"Activity '{payActivityClass}': invalid tools:replace. Expected 'android:launchMode', actual '{toolsReplace ?? "<null>"}'.");
            }

            verifyMessages = messages.ToArray();
            return verifyMessages.Length == 0;
        }
    }
}
#endif
