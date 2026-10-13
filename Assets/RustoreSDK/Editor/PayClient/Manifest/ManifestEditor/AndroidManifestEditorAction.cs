#nullable enable

#if UNITY_EDITOR
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        public XElement GetOrCreateAction(XElement intentFilter, string actionName) =>
            intentFilter.GetOrCreateXElement(
                "action",
                a => (string?)a.Attribute(AndroidNs("name")) == actionName,
                a => a.SetAttributeValue(AndroidNs("name"), actionName)
            );
    }
}
#endif
