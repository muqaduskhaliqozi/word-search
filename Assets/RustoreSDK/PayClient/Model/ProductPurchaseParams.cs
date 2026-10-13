#nullable enable

namespace RuStore.PayClient {

    /// <summary>
    /// Параметры покупки продукта.
    /// </summary>
    public class ProductPurchaseParams : BaseFields {

        /// <summary>
        /// Адрес электронной почты пользователя (необязательный параметр).
        /// При использовании данного параметра поле email пользователя автоматически заполняется этим значением при отправке чека,
        /// как для платежей вне RuStore, так и для случаев, когда пользователь не авторизован в RuStore.
        /// </summary>
        public AppUserEmail? appUserEmail { get; }

        /// <summary>
        /// Внутренний ID пользователя в приложении (необязательный параметр).
        /// Максимальная длина 128 символов.
        /// </summary>
        public AppUserId? appUserId { get; }

        /// <summary>
        /// Строка с дополнительной информацией о заказе, которую вы можете установить при инициализации процесса покупки (необязательный параметр).
        /// </summary>
        public DeveloperPayload? developerPayload { get; }

        /// <summary>
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </summary>
        public OrderId? orderId { get; }

        /// <summary>
        /// Идентификатор продукта, который был присвоен продукту в консоли RuStore.
        /// </summary>
        public ProductId productId { get; }

        /// <summary>
        /// Количество продукта (необязательный параметр — если не указывать, будет подставлено значение 1).
        /// </summary>
        public Quantity? quantity { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore.</param>
        /// <param name="appUserEmail">
        /// Адрес электронной почты пользователя (необязательный параметр).
        /// При использовании данного параметра поле email пользователя автоматически заполняется этим значением при отправке чека,
        /// как для платежей вне RuStore, так и для случаев, когда пользователь не авторизован в RuStore.
        /// </param>
        /// <param name="appUserId">
        /// Внутренний ID пользователя в приложении (необязательный параметр).
        /// Максимальная длина 128 символов.
        /// </param>
        /// <param name="developerPayload">Строка с дополнительной информацией о заказе, которую вы можете установить при инициализации процесса покупки (необязательный параметр).</param>
        /// <param name="orderId">
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </param>
        /// <param name="quantity">Количество продукта (необязательный параметр — если не указывать, будет подставлено значение 1).</param>
        public ProductPurchaseParams(
            ProductId productId,
            AppUserEmail? appUserEmail = null,
            AppUserId? appUserId = null,
            DeveloperPayload? developerPayload = null,
            OrderId? orderId = null,
            Quantity? quantity = null) {

            this.appUserEmail = appUserEmail;
            this.appUserId = appUserId;
            this.productId = productId;
            this.quantity = quantity;
            this.orderId = orderId;
            this.developerPayload = developerPayload;
        }
    }
}
