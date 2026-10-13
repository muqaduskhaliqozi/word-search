namespace RuStore.PayClient {

    /// <summary>
    /// Токен для валидации покупки на сервере.
    /// </summary>
    public class SubscriptionToken : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public SubscriptionToken(string value) : base(value) { }
    }
}
