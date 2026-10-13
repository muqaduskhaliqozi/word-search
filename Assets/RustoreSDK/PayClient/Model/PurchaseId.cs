namespace RuStore.PayClient {

    /// <summary>
    /// Идентификатор покупки.
    /// </summary>
    public class PurchaseId : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public PurchaseId(string value) : base(value) { }
    }
}
