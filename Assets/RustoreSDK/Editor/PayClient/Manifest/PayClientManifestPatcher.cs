#if UNITY_EDITOR
using RuStore.Editor.Manifest.ManifestEditor;
using RuStore.Editor.Manifest.Rule;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace RuStore.Editor.Manifest {

    public class PayClientManifestPatcher {

        internal sealed class ManifestRuleCheckResult {
            public IAndroidManifestRule rule { get; }
            public bool isVerified { get; }
            public string[] messages { get; }

            public ManifestRuleCheckResult(IAndroidManifestRule rule, bool isVerified, string[] messages) {
                this.rule = rule;
                this.isVerified = isVerified;
                this.messages = messages ?? Array.Empty<string>();
            }
        }

        private readonly string manifestPath =
            System.IO.Path.Combine("Assets", "Plugins", "Android", "AndroidManifest.xml");

        private readonly IReadOnlyList<IAndroidManifestRule> rules =
            new IAndroidManifestRule[]
            {
                new AddRuStoreDeeplinkActivityRule(),
                new AddPayClientMetaDataRule(),
            };

        private static PayClientManifestPatcher instance = null;

        public static PayClientManifestPatcher Instance {
            get {
                if (instance == null) instance = new PayClientManifestPatcher();
                return instance;
            }
        }

        private PayClientManifestPatcher() { }

        public bool VerifyProjectManifest(PayClientSettings settings) {
            var manifestEditor = new AndroidManifestEditor(manifestPath);

            var failedRules = GetFailedRules(manifestEditor, settings);
            
            if (failedRules.Count == 0) {
                Debug.Log($"[RuStore] AndroidManifest.xml verified.");

                return true;
            }

            Debug.LogWarning($"[RuStore] AndroidManifest.xml not verified. See details below.");
            PrintFailedRulesMessages(failedRules);

            return false;
        }

        public void PatchProjectManifest(PayClientSettings settings) {
            var manifestEditor = new AndroidManifestEditor(manifestPath);

            // Apply
            var failedRules = GetFailedRules(manifestEditor, settings);
            foreach (var result in failedRules) {
                result.rule.Apply(manifestEditor, settings);
            }
            manifestEditor.Save();

            // Verify
            var stillFailedRules = GetFailedRules(manifestEditor, settings);
            if (stillFailedRules.Count == 0) {
                Debug.Log("[RuStore] AndroidManifest.xml patched and verified.");
            }
            else {
                Debug.LogError("[RuStore] AndroidManifest.xml patch failed. See details below.");
                PrintFailedRulesMessages(stillFailedRules, Debug.LogError);
            }
        }

        private List<ManifestRuleCheckResult> GetFailedRules(AndroidManifestEditor manifestEditor, PayClientSettings settings) =>
            rules
                .Select(rule => {
                    var isVerified = rule.Verify(manifestEditor, settings, out var msgs);
                    return new ManifestRuleCheckResult(rule, isVerified, msgs);
                })
                .Where(result => !result.isVerified)
                .ToList();

        private void PrintFailedRulesMessages(List<ManifestRuleCheckResult> failedRules, Action<object> DebugLog = null) {
            if (failedRules.Count == 0) return;

            DebugLog ??= Debug.LogWarning;

            foreach (var result in failedRules) {
                if (result.messages.Length == 0) continue;

                DebugLog($"[RuStore] AndroidManifest.xml: {result.rule.GetType().Name} not verified.");

                foreach (var message in result.messages) {
                    DebugLog($"[RuStore] {result.rule.GetType().Name}: {message}");
                }
            }
        }
    }
}
#endif
