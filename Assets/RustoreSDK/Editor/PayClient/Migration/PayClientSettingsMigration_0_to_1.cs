#if UNITY_EDITOR
namespace RuStore.Editor {
    
    /// <summary>
    /// Миграция PayClientSettings с версии 0 (legacy) на версию 1.
    /// Первая версия схемы — никаких преобразований данных не требуется.
    /// </summary>
    internal class PayClientSettingsMigration_0_to_1 : IPayClientSettingsMigration {

        public int FromVersion => 0;
        public int ToVersion => 1;

        public void Migrate(PayClientSettingsMigrationData data) {
            // Первая миграция — данные уже сохранены через рефлексию.
            // Здесь можно добавить преобразования, если в версии 0 поля имели другие имена.
            // Например:
            // data.RenameField("oldFieldName", "newFieldName");
        }
    }
}
#endif
