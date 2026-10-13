#nullable enable

using System;

namespace RuStore.PayClient {

    /// <summary>
    /// Информация о покупке.
    /// </summary>
    public class SubscriptionPurchase : BaseFields, IPurchaseStatus<SubscriptionPurchaseStatus> {

        /// <summary>
        /// Идентификатор покупки.
        /// </summary>
        public PurchaseId purchaseId { get; }

        /// <summary>
        /// Идентификатор счёта.
        /// </summary>
        public InvoiceId invoiceId { get; }

        /// <summary>
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </summary>
        public OrderId? orderId { get; }

        /// <summary>
        /// Тип покупки.
        /// </summary>
        public PurchaseType purchaseType { get; }

        /// <summary>
        /// Состояние покупки.
        /// </summary>
        public SubscriptionPurchaseStatus status { get; }
        Enum IPurchase.status => status;

        /// <summary>
        /// Описание на языке language.
        /// </summary>
        public Description description { get; }

        /// <summary>
        /// Время покупки (необязательный параметр).
        /// </summary>
        public DateTime? purchaseTime { get; }

        /// <summary>
        /// Цена в минимальных единицах (например в копейках).
        /// </summary>
        public Price price { get; }

        /// <summary>
        /// Отформатированная цена покупки, включая валютный знак.
        /// </summary>
        public AmountLabel amountLabel { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public Currency currency { get; }

        /// <summary>
        /// Строка с дополнительной информацией о заказе,
        /// которую вы можете установить при инициализации процесса покупки (необязательный параметр).
        /// </summary>
        public DeveloperPayload? developerPayload { get; }

        /// <summary>
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </summary>
        public bool sandbox { get; }

        /// <summary>
        /// Идентификатор продукта, который был присвоен продукту в консоли RuStore.
        /// </summary>
        public ProductId productId { get; }

        /// <summary>
        /// Дата окончания срока действия подписки.
        /// </summary>
        public DateTime? expirationDate { get; }

        /// <summary>
        /// Флаг, указывающий, активен ли льготный период для подписки.
        /// </summary>
        public bool gracePeriodEnabled { get; }

        /// <summary>
        /// Состояние подтверждения покупки.
        /// </summary>
        public AcknowledgementState acknowledgementState { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="invoiceId">Идентификатор счёта.</param>
        /// <param name="orderId">
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </param>
        /// <param name="purchaseType">Тип покупки.</param>
        /// <param name="status">Состояние покупки.</param>
        /// <param name="description">Описание на языке language.</param>
        /// <param name="purchaseTime">Время покупки (необязательный параметр).</param>
        /// <param name="price">Цена в минимальных единицах (например в копейках).</param>
        /// <param name="amountLabel">Отформатированная цена покупки, включая валютный знак.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="developerPayload">Строка с дополнительной информацией о заказе, которую вы можете установить при инициализации процесса покупки (необязательный параметр).</param>
        /// <param name="sandbox">
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </param>
        /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore.</param>
        /// <param name="expirationDate">Дата окончания срока действия подписки.</param>
        /// <param name="gracePeriodEnabled">Флаг, указывающий, активен ли льготный период для подписки.</param>
        /// <param name="acknowledgementState">Состояние подтверждения покупки.</param>
        public SubscriptionPurchase(
            PurchaseId purchaseId,
            InvoiceId invoiceId,
            OrderId? orderId,
            PurchaseType purchaseType,
            SubscriptionPurchaseStatus status,
            Description description,
            DateTime? purchaseTime,
            Price price,
            AmountLabel amountLabel,
            Currency currency,
            DeveloperPayload? developerPayload,
            bool sandbox,
            ProductId productId,
            DateTime? expirationDate,
            bool gracePeriodEnabled,
            AcknowledgementState acknowledgementState) {

            this.purchaseId = purchaseId;
            this.invoiceId = invoiceId;
            this.orderId = orderId;
            this.purchaseType = purchaseType;
            this.status = status;
            this.description = description;
            this.purchaseTime = purchaseTime;
            this.price = price;
            this.amountLabel = amountLabel;
            this.currency = currency;
            this.developerPayload = developerPayload;
            this.sandbox = sandbox;
            this.productId = productId;
            this.expirationDate = expirationDate;
            this.gracePeriodEnabled = gracePeriodEnabled;
            this.acknowledgementState = acknowledgementState;
        }
    }
}
