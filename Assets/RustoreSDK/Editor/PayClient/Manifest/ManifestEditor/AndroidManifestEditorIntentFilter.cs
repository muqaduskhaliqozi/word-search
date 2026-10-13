#nullable enable

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        public XElement? GetIntentFilter(XElement activity, Func<XElement, bool> match) =>
            activity.GetXElement(
                "intent-filter",
                match
            );

        public XElement GetOrCreateIntentFilter(XElement activity, Func<XElement, bool> match) =>
            activity.GetOrCreateXElement(
                "intent-filter",
                match
            );

        public void RemoveIntentFilters(XElement activity, Func<XElement, bool> predicate) =>
            activity.RemoveXElement(
                "intent-filter",
                predicate
            );

        public static bool IsMainLauncherIntentFilter(XElement intentFilter) {
            bool hasMain = intentFilter.Elements("action")
                .Any(a => (string?)a.Attribute(AndroidNs("name")) == "android.intent.action.MAIN");

            bool hasLauncher = intentFilter.Elements("category")
                .Any(c => (string?)c.Attribute(AndroidNs("name")) == "android.intent.category.LAUNCHER");

            return hasMain || hasLauncher;
        }

        public static bool IsViewDeeplinkIntentFilter(XElement intentFilter) =>
            intentFilter.Elements("action")
                .Any(a => (string?)a.Attribute(AndroidNs("name")) == "android.intent.action.VIEW");
    }
}
#endif
