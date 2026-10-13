namespace RuStore.PayClient {

    /// <summary>
    /// Отформатированная цена товара, включая валютный знак.
    /// </summary>
    public class AmountLabel : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public AmountLabel(string value) : base(value) { }
    }
}
