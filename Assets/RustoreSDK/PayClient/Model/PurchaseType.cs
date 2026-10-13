namespace RuStore.PayClient {

    /// <summary>
    /// Тип покупки.
    /// </summary>
    public enum PurchaseType {

        /// <summary>
        /// Одностадийная оплата.
        /// </summary>
        ONE_STEP,

        /// <summary>
        /// Двухстадийная оплата.
        /// </summary>
        TWO_STEP,

        /// <summary>
        /// Значение не определено.
        /// </summary>
        UNDEFINED
    }
}
