#nullable enable

using System;

namespace RuStore.PayClient {

    /// <summary>
    /// Общий интерфейс IPurchase.
    /// </summary>
    public interface IPurchase {

        /// <summary>
        /// Идентификатор покупки.
        /// Используется для получения информации о покупке в SDK.
        /// </summary>
        PurchaseId purchaseId { get; }

        /// <summary>
        /// Идентификатор счета.
        /// Используется для серверной валидации платежа,
        /// поиска платежей в консоли разработчика,
        /// а также отображается покупателю в истории платежей.
        /// </summary>
        InvoiceId invoiceId { get; }

        /// <summary>
        /// Уникальный идентификатор оплаты, сформированный приложением (необязательный параметр).
        /// Если вы укажете этот параметр в вашей системе, вы получите его в ответе при работе с API.
        /// Если не укажете, он будет сгенерирован автоматически (uuid).
        /// Максимальная длина 150 символов.
        /// </summary>
        OrderId? orderId { get; }

        /// <summary>
        /// Тип покупки.
        /// </summary>
        PurchaseType purchaseType { get; }

        /// <summary>
        /// Статус покупки.
        /// </summary>
        Enum status { get; }

        /// <summary>
        /// Описание на языке language.
        /// </summary>
        Description description { get; }

        /// <summary>
        /// Время покупки (необязательный параметр).
        /// </summary>
        DateTime? purchaseTime { get; }

        /// <summary>
        /// Цена в минимальных единицах (например в копейках).
        /// </summary>
        Price price { get; }

        /// <summary>
        /// Отформатированная цена покупки, включая валютный знак.
        /// </summary>
        AmountLabel amountLabel { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        Currency currency { get; }

        /// <summary>
        /// Строка с дополнительной информацией о заказе,
        /// которую вы можете установить при инициализации процесса покупки (необязательный параметр).
        /// </summary>
        DeveloperPayload? developerPayload { get; }

        /// <summary>
        /// Определяет, является ли платёж тестовым.
        /// Значения могут быть true или false, где true обозначает тестовый платёж, а false – реальный.
        /// </summary>
        bool sandbox { get; }
    }
}
