namespace RuStore.PayClient {

    /// <summary>
    /// Уникальный идентификатор оплаты, указанный разработчиком или сформированный автоматически (uuid). 
    /// </summary>
    public class OrderId : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public OrderId(string value) : base(value) { }
    }
}
