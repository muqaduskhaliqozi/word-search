namespace RuStore.PayClient {

    /// <summary>
    /// Указанная разработчиком строка, содержащая дополнительную информацию о заказе.
    /// </summary>
    public class DeveloperPayload : BaseValue<string> {
        
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="value">Значение.</param>
        public DeveloperPayload(string value) : base(value) { }
    }
}
