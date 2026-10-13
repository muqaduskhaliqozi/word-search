#nullable enable

#if UNITY_EDITOR
using System.Linq;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        public XElement? FindMetaData(string metaName) =>
            application.Elements("meta-data")
                .FirstOrDefault(e => (string?)e.Attribute(AndroidNs("name")) == metaName);

        public XElement UpdateMetaData(string metaName, string metaValue) {
            var md = FindMetaData(metaName);
            if (md == null) {
                md = new XElement("meta-data");
                application.Add(md);
            }

            md.SetAttributeValue(AndroidNs("name"), metaName);
            md.SetAttributeValue(AndroidNs("value"), metaValue);

            return md;
        }
    }
}
#endif
