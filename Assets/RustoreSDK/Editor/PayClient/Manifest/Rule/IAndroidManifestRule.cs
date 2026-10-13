#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;

namespace RuStore.Editor.Manifest.Rule {

    public interface IAndroidManifestRule {

        bool Verify(AndroidManifestEditor manifestEditor, PayClientSettings settings, out string[] verifyMessages);
        void Apply(AndroidManifestEditor manifestEditor, PayClientSettings settings);
    }
}
#endif
