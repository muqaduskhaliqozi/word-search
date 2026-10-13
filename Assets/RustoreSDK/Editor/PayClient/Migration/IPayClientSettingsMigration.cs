#if UNITY_EDITOR
namespace RuStore.Editor {

    /// <summary>
    /// Интерфейс для миграции настроек PayClientSettings с одной версии на другую.
    /// Каждая миграция знает только свой шаг (например, 0 → 1, 1 → 2).
    /// </summary>
    internal interface IPayClientSettingsMigration {
        int FromVersion { get; }
        int ToVersion { get; }
        void Migrate(PayClientSettingsMigrationData data);
    }
}
#endif
