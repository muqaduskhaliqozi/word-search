#nullable enable

namespace RuStore.PayClient {

    /// <summary>
    /// Результат успешной оплаты цифрового товара (для одностадийной оплаты) или успешного холдирования средств (для двухстадийной оплаты).
    /// </summary>
    public sealed class ProductPurchaseResult : BaseFields {

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
        /// Тип покупки.
        /// </summary>
        public PurchaseType purchaseType { get; }

        /// <summary>
        /// Количество купленного продукта.
        /// </summary>
        public Quantity quantity { get; }

        /// <summary>
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </summary>
        public bool sandbox { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="invoiceId">Идентификатор счёта.</param>
        /// <param name="orderId">
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </param>
        /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore.</param>
        /// <param name="productType">Тип продукта.</param>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="purchaseType">Тип покупки.</param>
        /// <param name="quantity">Количество купленного продукта.</param>
        /// <param name="sandbox">
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </param>
        public ProductPurchaseResult(
            InvoiceId invoiceId,
            OrderId? orderId,
            ProductId productId,
            ProductType productType,
            PurchaseId purchaseId,
            PurchaseType purchaseType,
            Quantity quantity,
            bool sandbox) {

            this.invoiceId = invoiceId;
            this.orderId = orderId;
            this.productId = productId;
            this.productType = productType;
            this.purchaseId = purchaseId;
            this.purchaseType = purchaseType;
            this.quantity = quantity;
            this.sandbox = sandbox;
        }
    }
}
