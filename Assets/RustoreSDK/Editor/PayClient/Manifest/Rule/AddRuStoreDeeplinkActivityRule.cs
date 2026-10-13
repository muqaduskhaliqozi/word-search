#nullable enable

#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.Rule {

    public sealed class AddRuStoreDeeplinkActivityRule : IAndroidManifestRule {
        private const string deeplinkSchemeValue = "@string/rustore_PayClientSettings_deeplinkScheme";
        private const string noDisplayTheme = "@android:style/Theme.NoDisplay";

        public static class IntentAction {
            public const string Main = "android.intent.action.MAIN";
            public const string View = "android.intent.action.VIEW";
        }

        public static class IntentCategory {
            public const string Launcher = "android.intent.category.LAUNCHER";
            public const string Default = "android.intent.category.DEFAULT";
            public const string Browsable = "android.intent.category.BROWSABLE";
        }

        public void Apply(AndroidManifestEditor manifestEditor, PayClientSettings settings) {
            var a = manifestEditor.GetOrCreateActivity(settings.rustoreDeeplinkActivityClass);
            a.SetAttributeValue(AndroidManifestEditor.AndroidNs("theme"), noDisplayTheme);
            a.SetAttributeValue(AndroidManifestEditor.AndroidNs("exported"), "true");

            // var main = manifestEditor.GetOrCreateIntentFilter(a, AndroidManifestEditor.IsMainLauncherIntentFilter);
            //manifestEditor.GetOrCreateAction(main, IntentAction.Main);
            //manifestEditor.GetOrCreateCategory(main, IntentCategory.Launcher);

            var view = manifestEditor.GetOrCreateIntentFilter(a, AndroidManifestEditor.IsViewDeeplinkIntentFilter);
            manifestEditor.GetOrCreateAction(view, IntentAction.View);
            manifestEditor.GetOrCreateCategory(view, IntentCategory.Default);
            manifestEditor.GetOrCreateCategory(view, IntentCategory.Browsable);

            var data = view.Elements("data").FirstOrDefault();
            if (data == null) {
                data = new XElement("data");
                view.Add(data);
            }

            data.SetAttributeValue(AndroidManifestEditor.AndroidNs("scheme"), deeplinkSchemeValue);
        }

        public bool Verify(AndroidManifestEditor manifestEditor, PayClientSettings settings, out string[] verifyMessages) {
            var messages = new List<string>();

            var activityName = settings.rustoreDeeplinkActivityClass;

            var a = manifestEditor.FindActivity(activityName);
            if (a == null) {
                messages.Add($"Missing <activity android:name=\"{activityName}\">.");
                verifyMessages = messages.ToArray();
                return false;
            }

            // Атрибуты activity
            var theme = (string?)a.Attribute(AndroidManifestEditor.AndroidNs("theme"));
            if (theme != noDisplayTheme) {
                messages.Add($"Activity '{activityName}': invalid android:theme. Expected '{noDisplayTheme}', actual '{theme ?? "<null>"}'.");
            }

            var exported = (string?)a.Attribute(AndroidManifestEditor.AndroidNs("exported"));
            if (exported != "true") {
                messages.Add($"Activity '{activityName}': invalid android:exported. Expected 'true', actual '{exported ?? "<null>"}'.");
            }

            // MAIN/LAUNCHER intent-filter
            /*var main = manifestEditor.GetIntentFilter(a, AndroidManifestEditor.IsMainLauncherIntentFilter);
            if (main == null) {
                messages.Add($"Activity '{activityName}': missing MAIN/LAUNCHER <intent-filter>.");
            }
            else {
                bool hasMainAction = main.Elements("action")
                    .Any(x => (string?)x.Attribute(AndroidManifestEditor.AndroidNs("name")) == IntentAction.Main);
                if (!hasMainAction) {
                    messages.Add($"Activity '{activityName}': MAIN/LAUNCHER intent-filter missing <action android:name=\"{IntentAction.Main}\">.");
                }

                bool hasLauncherCategory = main.Elements("category")
                    .Any(x => (string?)x.Attribute(AndroidManifestEditor.AndroidNs("name")) == IntentCategory.Launcher);
                if (!hasLauncherCategory) {
                    messages.Add($"Activity '{activityName}': MAIN/LAUNCHER intent-filter missing <category android:name=\"{IntentCategory.Launcher}\">.");
                }
            }*/

            // VIEW deeplink intent-filter + data scheme
            var view = manifestEditor.GetIntentFilter(a, AndroidManifestEditor.IsViewDeeplinkIntentFilter);
            if (view == null) {
                messages.Add($"Activity '{activityName}': missing VIEW deeplink <intent-filter>.");
            }
            else {
                bool hasViewAction = view.Elements("action")
                    .Any(x => (string?)x.Attribute(AndroidManifestEditor.AndroidNs("name")) == IntentAction.View);
                if (!hasViewAction) {
                    messages.Add($"Activity '{activityName}': VIEW intent-filter missing <action android:name=\"{IntentAction.View}\">.");
                }

                bool hasDefaultCategory = view.Elements("category")
                    .Any(x => (string?)x.Attribute(AndroidManifestEditor.AndroidNs("name")) == IntentCategory.Default);
                if (!hasDefaultCategory) {
                    messages.Add($"Activity '{activityName}': VIEW intent-filter missing <category android:name=\"{IntentCategory.Default}\">.");
                }

                bool hasBrowsableCategory = view.Elements("category")
                    .Any(x => (string?)x.Attribute(AndroidManifestEditor.AndroidNs("name")) == IntentCategory.Browsable);
                if (!hasBrowsableCategory) {
                    messages.Add($"Activity '{activityName}': VIEW intent-filter missing <category android:name=\"{IntentCategory.Browsable}\">.");
                }

                bool hasScheme = view.Elements("data")
                    .Any(d => (string?)d.Attribute(AndroidManifestEditor.AndroidNs("scheme")) == deeplinkSchemeValue);
                if (!hasScheme) {
                    messages.Add($"Activity '{activityName}': VIEW intent-filter missing <data android:scheme=\"{deeplinkSchemeValue}\">.");
                }
            }

            verifyMessages = messages.ToArray();
            return verifyMessages.Length == 0;
        }
    }
}
#endif
