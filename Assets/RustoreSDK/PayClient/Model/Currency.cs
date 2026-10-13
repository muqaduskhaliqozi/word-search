namespace RuStore.PayClient {

    /// <summary>
    /// Код валюты ISO 4217.
    /// </summary>
    public class Currency : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public Currency(string value) : base(value) { }
    }
}
