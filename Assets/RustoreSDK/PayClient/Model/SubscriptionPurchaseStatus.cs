namespace RuStore.PayClient {

    /// <summary>
    /// Статус покупки.
    /// </summary>
    public enum SubscriptionPurchaseStatus {

        /// <summary>
        /// Создан счет на оплату, покупка ожидает оплаты.
        /// </summary>
        INVOICE_CREATED,

        /// <summary>
        /// Подписка отменена покупателем.
        /// </summary>
        CANCELLED,

        /// <summary>
        /// Срок действия подписки истек.
        /// </summary>
        EXPIRED,

        /// <summary>
        /// Платеж в обработке.
        /// </summary>
        PROCESSING,

        /// <summary>
        /// Платеж отклонен (например, ввиду недостатка средств).
        /// </summary>
        REJECTED,

        /// <summary>
        /// Подписка активна.
        /// </summary>
        ACTIVE,

        /// <summary>
        /// Подписка приостановлена из-за проблем с оплатой.
        /// </summary>
        PAUSED,

        /// <summary>
        /// Закончились попытки списания по подписке (все были неуспешными). Подписка закрыта автоматически из-за проблем с оплатой.
        /// </summary>
        TERMINATED,

        /// <summary>
        /// Подписка была отменена пользователем или разработчиком. Истек срок оплаченного периода, подписка закрыта.
        /// </summary>
        CLOSED
    }
}
