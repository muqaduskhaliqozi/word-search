#nullable enable

#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;
using System.Collections.Generic;

namespace RuStore.Editor.Manifest.Rule {

    public sealed class AddPayClientMetaDataRule : IAndroidManifestRule {

        private const string consoleAppIdName = "console_app_id_value";
        private const string consoleAppIdValue = "@string/rustore_PayClientSettings_consoleApplicationId";

        private const string internalConfigKeyName = "internal_config_key";
        private const string internalConfigKeyValue = "@string/rustore_PayClientSettings_internalConfigKey";

        private const string paySchemeName = "sdk_pay_scheme_value";
        private const string paySchemeValue = "@string/rustore_PayClientSettings_deeplinkScheme";

        public void Apply(AndroidManifestEditor manifestEditor, PayClientSettings settings) {
            manifestEditor.UpdateMetaData(consoleAppIdName, consoleAppIdValue);
            manifestEditor.UpdateMetaData(internalConfigKeyName, internalConfigKeyValue);
            manifestEditor.UpdateMetaData(paySchemeName, paySchemeValue);
        }

        public bool Verify(AndroidManifestEditor manifestEditor, PayClientSettings settings, out string[] verifyMessages) {
            var messages = new List<string>();

            ValidateMeta(manifestEditor, consoleAppIdName, consoleAppIdValue, messages);
            ValidateMeta(manifestEditor, internalConfigKeyName, internalConfigKeyValue, messages);
            ValidateMeta(manifestEditor, paySchemeName, paySchemeValue, messages);

            verifyMessages = messages.ToArray();

            return verifyMessages.Length == 0;
        }

        private static void ValidateMeta(AndroidManifestEditor manifestEditor, string metaName, string expectedValue, List<string> messages) {

            var metaData = manifestEditor.FindMetaData(metaName);
            if (metaData == null) {
                messages.Add($"Missing <meta-data android:name=\"{metaName}\">.");
                return;
            }

            var value = (string?)metaData.Attribute(AndroidManifestEditor.AndroidNs("value"));
            if (value != expectedValue) {
                messages.Add(
                    $"Invalid <meta-data android:name=\"{metaName}\"> value. Expected '{expectedValue}', actual '{value ?? "<null>"}'.");
            }
        }
    }
}
#endif
