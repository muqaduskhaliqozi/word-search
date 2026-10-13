namespace RuStore.PayClient {

    /// <summary>
    /// Адрес электронной почты пользователя.
    /// </summary>
    public class AppUserEmail : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public AppUserEmail(string value) : base(value) { }
    }
}
