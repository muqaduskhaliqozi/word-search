namespace RuStore.PayClient {

    /// <summary>
    /// Внутренний ID пользователя в приложении.
    /// </summary>
    public class AppUserId : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public AppUserId(string value) : base(value) { }
    }
}
