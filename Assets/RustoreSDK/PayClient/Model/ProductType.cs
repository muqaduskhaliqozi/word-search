namespace RuStore.PayClient {

    /// <summary>
    /// Тип продукта.
    /// </summary>
    public enum ProductType {

        /// <summary>
        /// Непотребляемй товар.
        /// Можно купить один раз.
        /// </summary>
        NON_CONSUMABLE_PRODUCT,

        /// <summary>
        /// Потребляемый товар.
        /// Можно купить много раз.
        /// </summary>
        CONSUMABLE_PRODUCT,

        /// <summary>
        /// Подписка.
        /// </summary>
        SUBSCRIPTION
    }
}
