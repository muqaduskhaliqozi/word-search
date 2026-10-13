namespace RuStore.PayClient {

    /// <summary>
    /// Количество продукта.
    /// </summary>
    public class Quantity : BaseValue<int> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public Quantity(int value) : base(value) { }
    }
}
