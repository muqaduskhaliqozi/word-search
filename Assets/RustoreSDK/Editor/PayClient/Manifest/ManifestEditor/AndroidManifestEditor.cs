#if UNITY_EDITOR
using System;
using System.IO;
using System.Xml.Linq;

namespace RuStore.Editor.Manifest.ManifestEditor {

    public sealed partial class AndroidManifestEditor {

        private static XNamespace androidNs = "http://schemas.android.com/apk/res/android";
        private static XNamespace toolsNs = "http://schemas.android.com/tools";

        private readonly XDocument document;

        public XElement manifest => document?.Element("manifest")
            ?? throw new InvalidDataException("AndroidManifest.xml: <manifest> not found.");

        public XElement application => manifest.Element("application")
            ?? throw new InvalidDataException("AndroidManifest.xml: <application> not found.");

        public string manifestPath { get; }

        public AndroidManifestEditor(string manifestPath) {
            this.manifestPath = manifestPath
                ?? throw new ArgumentNullException(nameof(manifestPath));

            if (!File.Exists(this.manifestPath))
                throw new FileNotFoundException("AndroidManifest.xml not found.", this.manifestPath);

            document = XDocument.Load(this.manifestPath, LoadOptions.None);
        }

        public void Save() {
            var settings = new System.Xml.XmlWriterSettings {
                Encoding = new System.Text.UTF8Encoding(false),
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                NewLineHandling = System.Xml.NewLineHandling.Replace,
                OmitXmlDeclaration = false
            };

            using var writer = System.Xml.XmlWriter.Create(manifestPath, settings);
            document.Save(writer);
        }

        public static XName AndroidNs(string name) => androidNs + name;
        public static XName ToolsNs(string name) => toolsNs + name;
    }
}
#endif
