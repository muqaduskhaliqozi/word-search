namespace RuStore.PayClient {

    /// <summary>
    /// Название продукта.
    /// </summary>
    public class Title : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public Title(string value) : base(value) { }
    }
}
