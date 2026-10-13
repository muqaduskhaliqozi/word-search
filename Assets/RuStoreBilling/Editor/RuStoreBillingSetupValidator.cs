using System.Collections.Generic;
using System.IO;
using System.Text;
using RuStore.Editor;
using RuStore.Editor.Manifest;
using UnityEditor;
using UnityEngine;

namespace RuStoreBilling.EditorTools
{
    /// <summary>
    /// One-click check of everything that has to be set per game before RuStore payments work.
    /// Window > RuStoreSDK > Validate Billing Setup. Read-only: it reports, it never edits.
    /// </summary>
    public static class RuStoreBillingSetupValidator
    {
        private const string MainTemplatePath = "Assets/Plugins/Android/mainTemplate.gradle";
        private const int RequiredMinSdk = 24;

        [MenuItem("Window/RuStoreSDK/Validate Billing Setup")]
        public static void Validate()
        {
            var report = new StringBuilder("[RuStore] Billing setup check\n");
            bool failed = false;

            var settings = RuStoreModuleSettings.LoadAsset<PayClientSettings>();
            if (settings == null)
            {
                Fail(report, ref failed, "PayClientSettings asset missing - open Window > RuStoreSDK > Settings > PayClient.");
            }
            else
            {
                CheckConsoleApplicationId(settings, report, ref failed);
                CheckDeeplinkScheme(settings, report, ref failed);

                // The SDK owns the manifest rules; reuse its verifier instead of restating them.
                if (PayClientManifestPatcher.Instance.VerifyProjectManifest(settings))
                    Pass(report, "AndroidManifest.xml patched (deeplink activity + meta-data).");
                else
                    Fail(report, ref failed, "AndroidManifest.xml not patched - hit Patch in the PayClient settings window (details logged above).");
            }

            CheckProductConfigs(report, ref failed);
            CheckResolvedDependencies(report, ref failed);
            CheckPlayerSettings(report, ref failed);

            report.Append("\nStill to confirm by hand (nothing local can prove these):\n")
                  .Append("  - RuStore Console app id matches this build's package name: ")
                  .Append(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)).Append('\n')
                  .Append("  - build signed with the same keystore as the published version\n")
                  .Append("  - every product id above exists in the console, with matching consumable/non-consumable types\n")
                  .Append("  - test account added as a tester in the console\n");

            if (failed) Debug.LogError(report.ToString());
            else Debug.Log(report.ToString());
        }

        private static void CheckConsoleApplicationId(PayClientSettings settings, StringBuilder report, ref bool failed)
        {
            var id = settings.consoleApplicationId;

            if (string.IsNullOrWhiteSpace(id))
            {
                Fail(report, ref failed, "consoleApplicationId is empty - copy it from the RuStore Console.");
                return;
            }

            foreach (var c in id)
            {
                if (char.IsDigit(c)) continue;
                Fail(report, ref failed, $"consoleApplicationId '{id}' is not numeric - it is the console's app id, not the package name.");
                return;
            }

            Pass(report, $"consoleApplicationId = {id}");
        }

        private static void CheckDeeplinkScheme(PayClientSettings settings, StringBuilder report, ref bool failed)
        {
            var scheme = settings.deeplinkScheme;

            if (string.IsNullOrWhiteSpace(scheme))
            {
                Fail(report, ref failed, "deeplinkScheme is empty - payments cannot return to the game without it.");
                return;
            }

            foreach (var c in scheme)
            {
                if ((c >= 'a' && c <= 'z') || char.IsDigit(c)) continue;
                Fail(report, ref failed, $"deeplinkScheme '{scheme}' must be lowercase letters and digits only.");
                return;
            }

            Pass(report, $"deeplinkScheme = {scheme} (must be unique to this game - never reuse another title's)");
        }

        private static void CheckProductConfigs(StringBuilder report, ref bool failed)
        {
            var guids = AssetDatabase.FindAssets("t:RuStoreProductConfig");

            if (guids.Length == 0)
            {
                Fail(report, ref failed, "No RuStoreProductConfig asset - create one via Assets > Create > RuStore > Product Config.");
                return;
            }

            if (guids.Length > 1)
                Warn(report, $"{guids.Length} RuStoreProductConfig assets found - make sure the scene points at the right one.");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<RuStoreProductConfig>(path);
                if (config == null) continue;

                var seen = new HashSet<string>();
                int blanks = 0, duplicates = 0;

                foreach (var entry in config.products)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.productId)) blanks++;
                    else if (!seen.Add(entry.productId)) duplicates++;
                }

                if (config.products.Count == 0)
                    Fail(report, ref failed, $"{path} lists no products - prices will never load.");
                else if (blanks > 0 || duplicates > 0)
                    Fail(report, ref failed, $"{path}: {blanks} blank id(s), {duplicates} duplicate id(s).");
                else
                    Pass(report, $"{path}: {seen.Count} product id(s).");
            }
        }

        private static void CheckResolvedDependencies(StringBuilder report, ref bool failed)
        {
            if (!File.Exists(MainTemplatePath))
            {
                Warn(report, $"{MainTemplatePath} not found - if this project has no custom main gradle template, check the RuStore deps land some other way.");
                return;
            }

            var gradle = File.ReadAllText(MainTemplatePath);
            bool pay = gradle.Contains("ru.rustore.sdk:pay");
            bool core = gradle.Contains("ru.rustore.sdk:core");

            if (pay && core)
                Pass(report, "ru.rustore.sdk pay + core resolved into mainTemplate.gradle.");
            else
                Fail(report, ref failed, $"mainTemplate.gradle missing {(pay ? "" : "pay ")}{(core ? "" : "core ")}- run Assets > External Dependency Manager > Android Resolver > Force Resolve.");
        }

        private static void CheckPlayerSettings(StringBuilder report, ref bool failed)
        {
            int minSdk = (int)PlayerSettings.Android.minSdkVersion;
            if (minSdk < RequiredMinSdk)
                Fail(report, ref failed, $"minSdkVersion is {minSdk}, RuStore Pay needs {RequiredMinSdk}+.");
            else
                Pass(report, $"minSdkVersion = {minSdk}");

            if (PlayerSettings.Android.useCustomKeystore)
                Pass(report, "Custom keystore set (must be the one the published build was signed with).");
            else
                Fail(report, ref failed, "No custom keystore - a debug-signed build fails RuStore's signature check.");
        }

        private static void Pass(StringBuilder report, string message) => report.Append("  OK   ").Append(message).Append('\n');

        private static void Warn(StringBuilder report, string message) => report.Append("  WARN ").Append(message).Append('\n');

        private static void Fail(StringBuilder report, ref bool failed, string message)
        {
            failed = true;
            report.Append("  FAIL ").Append(message).Append('\n');
        }
    }
}
