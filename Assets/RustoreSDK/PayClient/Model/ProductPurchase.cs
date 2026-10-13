#nullable enable

using System;

namespace RuStore.PayClient {

    /// <summary>
    /// Информация о покупке.
    /// </summary>
    public class ProductPurchase : BaseFields, IPurchaseStatus<ProductPurchaseStatus> {

        /// <summary>
        /// Отформатированная цена покупки, включая валютный знак.
        /// </summary>
        public AmountLabel amountLabel { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public Currency currency { get; }

        /// <summary>
        /// Описание на языке language.
        /// </summary>
        public Description description { get; }

        /// <summary>
        /// Строка с дополнительной информацией о заказе,
        /// которую вы можете установить при инициализации процесса покупки (необязательный параметр).
        /// </summary>
        public DeveloperPayload? developerPayload { get; }

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
        /// Цена в минимальных единицах (например в копейках).
        /// </summary>
        public Price price { get; }

        /// <summary>
        /// Идентификатор продукта, который был присвоен продукту в консоли RuStore.
        /// </summary>
        public ProductId productId { get; }

        /// <summary>
        /// Тип продукта.
        /// </summary>
        public ProductType productType { get; }

        /// <summary>
        /// Идентификатор покупки.
        /// </summary>
        public PurchaseId purchaseId { get; }

        /// <summary>
        /// Время покупки (необязательный параметр).
        /// </summary>
        public DateTime? purchaseTime { get; }

        /// <summary>
        /// Тип покупки.
        /// </summary>
        public PurchaseType purchaseType { get; }

        /// <summary>
        /// Количество продукта.
        /// </summary>
        public Quantity quantity { get; }

        /// <summary>
        /// Состояние покупки.
        /// </summary>
        public ProductPurchaseStatus status { get; }
        Enum IPurchase.status => status;

        /// <summary>
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </summary>
        public bool sandbox { get; }

        /// <summary>
        /// Состояние подтверждения покупки.
        /// </summary>
        public AcknowledgementState acknowledgementState { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="amountLabel">Отформатированная цена покупки, включая валютный знак.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="description">Описание на языке language.</param>
        /// <param name="developerPayload">Строка с дополнительной информацией о заказе, которую вы можете установить при инициализации процесса покупки (необязательный параметр).</param>
        /// <param name="invoiceId">Идентификатор счёта.</param>
        /// <param name="orderId">
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </param>
        /// <param name="price">Цена в минимальных единицах (например в копейках).</param>
        /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore.</param>
        /// <param name="productType">Тип продукта.</param>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="purchaseTime">Время покупки (необязательный параметр).</param>
        /// <param name="purchaseType">Тип покупки.</param>
        /// <param name="quantity">Количество продукта.</param>
        /// <param name="status">Состояние покупки.</param>
        /// <param name="sandbox">
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </param>
        /// <param name="acknowledgementState">Состояние подтверждения покупки.</param>
        public ProductPurchase(
            AmountLabel amountLabel,
            Currency currency,
            Description description,
            DeveloperPayload? developerPayload,
            InvoiceId invoiceId,
            OrderId? orderId,
            Price price,
            ProductId productId,
            ProductType productType,
            PurchaseId purchaseId,
            DateTime? purchaseTime,
            PurchaseType purchaseType,
            Quantity quantity,
            ProductPurchaseStatus status,
            bool sandbox,
            AcknowledgementState acknowledgementState) {

            this.amountLabel = amountLabel;
            this.currency = currency;
            this.description = description;
            this.developerPayload = developerPayload;
            this.invoiceId = invoiceId;
            this.orderId = orderId;
            this.price = price;
            this.productId = productId;
            this.productType = productType;
            this.purchaseId = purchaseId;
            this.purchaseTime = purchaseTime;
            this.purchaseType = purchaseType;
            this.quantity = quantity;
            this.status = status;
            this.sandbox = sandbox;
            this.acknowledgementState = acknowledgementState;
        }
    }
}
