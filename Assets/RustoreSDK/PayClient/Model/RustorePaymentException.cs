#nullable enable

namespace RuStore.PayClient {

    /// <summary>
    /// Информация об ошибках платежного клиента.
    /// </summary>
    public class RuStorePaymentException : RuStoreError {

        /// <summary>
        /// Информация об ошибке.
        /// </summary>
        public virtual RuStoreError? cause { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="name">Название ошибки.</param>
        /// <param name="description">Сообщение ошибки.</param>
        /// <param name="cause">Информация об ошибке.</param>
        public RuStorePaymentException(string name, string description, RuStoreError? cause) {
            this.name = name;
            this.description = description;
            this.cause = cause;
        }

        /// <summary>
        /// Схема приложения не задана.
        /// </summary>
        public sealed class ApplicationSchemeWasNotProvided : RuStorePaymentException {
            public ApplicationSchemeWasNotProvided(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Ошибка получения платежного токена.
        /// </summary>
        public sealed class EmptyPaymentTokenException : RuStorePaymentException {
            public EmptyPaymentTokenException(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Произошла отмена покупки продукта (пользователь закрыл платежную шторку).
        /// </summary>
        public sealed class ProductPurchaseCancelled : RuStorePaymentException {

            /// <summary>
            /// Тип продукта.
            /// </summary>
            public ProductType? productType { get; }

            /// <summary>
            /// Идентификатор покупки (необязательный параметр).
            /// </summary>
            public PurchaseId? purchaseId { get; }

            /// <summary>
            /// Тип покупки (необязательный параметр).
            /// </summary>
            public PurchaseType? purchaseType { get; }

            /// <summary>
            /// Конструктор.
            /// </summary>
            /// <param name="name">Название ошибки.</param>
            /// <param name="description">Сообщение ошибки.</param>
            /// <param name="cause">Информация об ошибке.</param>
            /// <param name="productType">Тип продукта.</param>
            /// <param name="purchaseId">Идентификатор покупки (необязательный параметр).</param>
            /// <param name="purchaseType">Тип покупки (необязательный параметр).</param>
            public ProductPurchaseCancelled(string name, string description, RuStoreError? cause, ProductType? productType, PurchaseId? purchaseId, PurchaseType? purchaseType)
                : base(name, description, cause) {

                this.productType = productType;
                this.purchaseId = purchaseId;
                this.purchaseType = purchaseType;
            }
        }

        /// <summary>
        /// Ошибка покупки продукта (невозможно установить статус покупки).
        /// </summary>
        public sealed class ProductPurchaseException : RuStorePaymentException {

            /// <summary>
            /// Идентификатор счёта (необязательный параметр).
            /// </summary>
            public InvoiceId? invoiceId { get; }

            /// <summary>
            /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
            /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
            /// Если не укажете, он будет сгенерирован автоматически (uuid).
            /// Максимальная длина 150 символов.
            /// </summary>
            public OrderId? orderId { get; }

            /// <summary>
            /// Идентификатор продукта, который был присвоен продукту в консоли RuStore (необязательный параметр).
            /// </summary>
            public ProductId? productId { get; }

            /// <summary>
            /// Тип продукта.
            /// </summary>
            public ProductType? productType { get; }

            /// <summary>
            /// Идентификатор покупки (необязательный параметр).
            /// </summary>
            public PurchaseId? purchaseId { get; }

            /// <summary>
            /// Тип покупки (необязательный параметр).
            /// </summary>
            public PurchaseType? purchaseType { get; }

            /// <summary>
            /// Количество продукта (необязательный параметр).
            /// </summary>
            public Quantity? quantity { get; }

            /// <summary>
            /// Флаг, указывающий признак тестового платежа в песочнице.
            /// Если true — покупка совершена в режиме тестирования.
            /// </summary>
            public bool? sandbox { get; }

            /// <summary>
            /// Конструктор.
            /// </summary>
            /// <param name="name">Название ошибки.</param>
            /// <param name="description">Сообщение ошибки.</param>
            /// <param name="cause">Информация об ошибке.</param>
            /// <param name="invoiceId">Идентификатор счёта (необязательный параметр).</param>
            /// <param name="orderId">
            /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
            /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
            /// Если не укажете, он будет сгенерирован автоматически (uuid).
            /// Максимальная длина 150 символов.
            /// </param>
            /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore (необязательный параметр).</param>
            /// <param name="productType">Тип продукта.</param>
            /// <param name="purchaseId">Идентификатор покупки (необязательный параметр).</param>
            /// <param name="purchaseType">Тип покупки (необязательный параметр).</param>
            /// <param name="quantity">Количество продукта (необязательный параметр).</param>
            public ProductPurchaseException(
                string name,
                string description,
                RuStoreError cause,
                InvoiceId? invoiceId,
                OrderId? orderId,
                ProductId? productId,
                ProductType? productType,
                PurchaseId? purchaseId,
                PurchaseType? purchaseType,
                Quantity? quantity,
                bool? sandbox)
                : base(name, description, cause) {

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

        /// <summary>
        /// Ошибка повторной инициализации SDK.
        /// </summary>
        public class RuStorePayClientAlreadyExist : RuStorePaymentException {
            public RuStorePayClientAlreadyExist(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Попытка обратиться к публичным интерфейсам SDK до момента её инициализации.
        /// </summary>
        public sealed class RuStorePayClientNotCreated : RuStorePaymentException {
            public RuStorePayClientNotCreated(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Запущен процесс оплаты неизвестного типа продукта.
        /// </summary>
        public sealed class RuStorePayInvalidActivePurchase : RuStorePaymentException {
            public RuStorePayInvalidActivePurchase(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Не задан обязательный параметр console_app_id_value для инициализации SDK.
        /// </summary>
        public sealed class RuStorePayInvalidConsoleAppId : RuStorePaymentException {
            public RuStorePayInvalidConsoleAppId(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Неверная сигнатура ответа (возникает при попытке совершить мошеннические действия).
        /// </summary>
        public sealed class RuStorePaySignatureException : RuStorePaymentException {
            public RuStorePaySignatureException(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Общая ошибка SDK.
        /// </summary>
        public sealed class RuStorePaymentCommonException : RuStorePaymentException {
            public RuStorePaymentCommonException(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }

        /// <summary>
        /// Ошибка сетевого взаимодействия SDK.
        /// </summary>
        public class RuStorePaymentNetworkException : RuStorePaymentException {

            /// <summary>
            /// Код сетевой ошибки.
            /// </summary>
            public string? code { get; }

            /// <summary>
            /// Идентификатор.
            /// </summary>
            public string id { get; }

            public RuStorePaymentNetworkException(string? code, string id, string name, string description, RuStoreError? cause)
                : base (name, description, cause) {

                this.code = code;
                this.id = id;
            }
        }

        /// <summary>
        /// Ошибка оплаты сохраненной картой.
        /// </summary>
        public class InvalidCardBindingIdException : RuStorePaymentException {
            public InvalidCardBindingIdException(string name, string description, RuStoreError? cause)
                : base(name, description, cause) { }
        }
    }
}
