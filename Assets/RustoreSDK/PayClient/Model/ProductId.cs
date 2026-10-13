namespace RuStore.PayClient {

    /// <summary>
    /// Идентификатор продукта, указанный при создании продукта в консоли разработчика.
    /// </summary>
    public class ProductId : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public ProductId(string value) : base(value) { }
    }
}
