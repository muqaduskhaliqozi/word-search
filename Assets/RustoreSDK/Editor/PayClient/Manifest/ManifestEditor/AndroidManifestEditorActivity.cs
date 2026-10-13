#nullable enable

#if UNITY_EDITOR
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        public XElement GetOrCreateActivity(string androidClassName) =>
            application.GetOrCreateXElement(
                "activity",
                a => (string?)a.Attribute(AndroidNs("name")) == androidClassName,
                a => a.SetAttributeValue(AndroidNs("name"), androidClassName)
            );

        public XElement? FindActivity(string androidClassName) =>
            application.FindXElementByAttribute(
                "activity",
                AndroidNs("name"),
                androidClassName
            );
    }
}
#endif
