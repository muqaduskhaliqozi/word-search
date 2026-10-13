#nullable enable

namespace RuStore.PayClient {

    /// <summary>
    /// Информация о продукте.
    /// </summary>
    public class Product : BaseFields {

        /// <summary>
        /// Отформатированная цена покупки, включая валютный знак.
        /// </summary>
        public AmountLabel amountLabel { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public Currency currency { get; }

        /// <summary>
        /// Описание на языке language (необязательный параметр).
        /// </summary>
        public Description? description { get; }

        /// <summary>
        /// Ссылка на картинку.
        /// </summary>
        public Url imageUrl { get; }

        /// <summary>
        /// Цена в минимальных единицах (например в копейках) (необязательный параметр).
        /// </summary>
        public Price? price { get; }

        /// <summary>
        /// Идентификатор продукта, который был присвоен продукту в консоли RuStore.
        /// </summary>
        public ProductId productId { get; }
        
        public Url? promoImageUrl { get; }

        /// <summary>
        /// Информация о подписке, будет не null, если тип продукта SUBSCRIPTION.
        /// </summary>
        public SubscriptionInfo? subscriptionInfo { get; }

        /// <summary>
        /// Название продукта на языке language.
        /// </summary>
        public Title title { get; }

        /// <summary>
        /// Тип продукта.
        /// </summary>
        public ProductType type { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="amountLabel">Отформатированная цена покупки, включая валютный знак.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="description">Описание на языке language (необязательный параметр).</param>
        /// <param name="imageUrl">Ссылка на картинку.</param>
        /// <param name="price">Цена в минимальных единицах (например в копейках) (необязательный параметр).</param>
        /// <param name="productId">Идентификатор продукта, который был присвоен продукту в консоли RuStore.</param>
        /// <param name="promoImageUrl"></param>
        /// <param name="subscriptionInfo">Информация о подписке.</param>
        /// <param name="title">Название продукта на языке language.</param>
        /// <param name="type">Тип продукта.</param>
        public Product(
            AmountLabel amountLabel,
            Currency currency,
            Description? description,
            Url imageUrl,
            Price? price,
            ProductId productId,
            Url? promoImageUrl,
            SubscriptionInfo? subscriptionInfo,
            Title title,
            ProductType type) {

            this.amountLabel = amountLabel;
            this.currency = currency;
            this.description = description;
            this.imageUrl = imageUrl;
            this.price = price;
            this.productId = productId;
            this.promoImageUrl = promoImageUrl;
            this.subscriptionInfo = subscriptionInfo;
            this.title = title;
            this.type = type;
        }
    }
}
