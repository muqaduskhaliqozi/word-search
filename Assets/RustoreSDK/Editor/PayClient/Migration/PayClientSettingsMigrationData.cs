#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RuStore.Editor {

    /// <summary>
    /// Класс для хранения данных миграции PayClientSettings.
    /// Использует рефлексию для автоматического захвата всех публичных полей.
    /// </summary>
    internal class PayClientSettingsMigrationData {

        private readonly Dictionary<string, object> _data = new();

        public PayClientSettingsMigrationData(PayClientSettings settings) {
            var fields = typeof(PayClientSettings)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly);

            foreach (var field in fields) {
                _data[field.Name] = field.GetValue(settings);
            }
        }

        public void ApplyTo(PayClientSettings settings) {
            var fields = typeof(PayClientSettings)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && _data.ContainsKey(f.Name));

            foreach (var field in fields) {
                field.SetValue(settings, _data[field.Name]);
            }
        }

        /// <summary>
        /// Возвращает список имён сохранённых полей (для отображения в UI)
        /// </summary>
        public IEnumerable<string> GetSavedFieldNames() => _data.Keys;

        /// <summary>
        /// Возвращает значение сохранённого поля по имени
        /// </summary>
        public object GetFieldValue(string fieldName) {
            return _data.TryGetValue(fieldName, out var value) ? value : null;
        }

        #region Migration Methods

        /// <summary>
        /// Переименовывает поле при миграции.
        /// Использовать, если поле изменило имя между версиями.
        /// </summary>
        public void RenameField(string oldName, string newName) {
            if (_data.TryGetValue(oldName, out var value)) {
                _data[newName] = value;
                _data.Remove(oldName);
            }
        }

        /// <summary>
        /// Удаляет поле из данных миграции.
        /// Использовать, если поле было удалено между версиями.
        /// </summary>
        public void RemoveField(string fieldName) {
            _data.Remove(fieldName);
        }

        /// <summary>
        /// Устанавливает значение поля.
        /// Использовать, если поле было добавлено между версиями.
        /// </summary>
        public void SetField(string fieldName, object value) {
            _data[fieldName] = value;
        }

        /// <summary>
        /// Проверяет наличие поля в данных миграции.
        /// </summary>
        public bool HasField(string fieldName) {
            return _data.ContainsKey(fieldName);
        }

        #endregion
    }
}
#endif
