namespace RuStore.PayClient {

    /// <summary>
    /// Цена в минимальных единицах валюты.
    /// </summary>
    public class Price : BaseValue<int> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public Price(int value) : base(value) { }
    }
}
