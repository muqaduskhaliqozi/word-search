namespace RuStore.PayClient {

    /// <summary>
    /// Описание продукта/покупки.
    /// </summary>
    public class Description : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public Description(string value) : base(value) { }
    }
}
