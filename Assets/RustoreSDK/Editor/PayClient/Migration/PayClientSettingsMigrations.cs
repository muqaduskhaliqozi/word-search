#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuStore.Editor {

    /// <summary>
    /// Реестр миграций PayClientSettings.
    /// Управляет последовательным применением миграций от одной версии к другой.
    /// </summary>
    internal static class PayClientSettingsMigrations {

        private static readonly List<IPayClientSettingsMigration> _migrations = new() {
            new PayClientSettingsMigration_0_to_1(),
            // new PayClientSettingsMigration_1_to_2(),
            // new PayClientSettingsMigration_2_to_3(),
        };

        /// <summary>
        /// Применяет все миграции от fromVersion до toVersion последовательно.
        /// Например, от 2 до 6: 2→3, 3→4, 4→5, 5→6
        /// </summary>
        public static void RunMigrations(PayClientSettingsMigrationData data, int fromVersion, int toVersion) {
            int currentVersion = fromVersion;

            while (currentVersion < toVersion) {
                var migration = _migrations.FirstOrDefault(m => m.FromVersion == currentVersion);

                if (migration == null) {
                    Debug.LogError($"[PayClientSettings] Migration from version {currentVersion} not found! " +
                                   $"Available migrations: {string.Join(", ", _migrations.Select(m => $"{m.FromVersion}→{m.ToVersion}"))}");
                    break;
                }
                
                migration.Migrate(data);
                currentVersion = migration.ToVersion;
            }
        }
    }
}
#endif
