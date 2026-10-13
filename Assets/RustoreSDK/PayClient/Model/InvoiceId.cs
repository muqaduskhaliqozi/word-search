namespace RuStore.PayClient {

    /// <summary>
    /// Идентификатор счета.
    /// Используется для серверной валидации платежа, поиска платежей в консоли разработчика,
    /// а также отображается покупателю в истории платежей в мобильном приложении RuStore.
    /// </summary>
    public class InvoiceId : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public InvoiceId(string value) : base(value) { }
    }
}
