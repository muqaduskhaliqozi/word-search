#nullable enable

#if UNITY_EDITOR
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        public XElement GetOrCreateCategory(XElement intentFilter, string categoryName) =>
            intentFilter.GetOrCreateXElement(
                "category",
                c => (string?)c.Attribute(AndroidNs("name")) == categoryName,
                c => c.SetAttributeValue(AndroidNs("name"), categoryName)
            );
    }
}
#endif
