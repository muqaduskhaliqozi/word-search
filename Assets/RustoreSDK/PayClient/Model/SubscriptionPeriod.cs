namespace RuStore.PayClient {

    /// <summary>
    /// Интерфейс, представляющий период подписки.
    /// </summary>
    public interface SubscriptionPeriod { }

    /// <summary>
    /// Период бесплатного тестового использования подписки.
    /// </summary>
    public sealed class TrialPeriod : SubscriptionPeriod {

        /// <summary>
        /// Длительность периода в формате ISO 8601.
        /// </summary>
        public string duration { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public string currency { get; }

        /// <summary>
        /// Цена в минимальных единицах валюты.
        /// </summary>
        public int price { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="duration">Длительность периода в формате ISO 8601.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="price">Цена в минимальных единицах валюты.</param>
        internal TrialPeriod(string duration, string currency, int price) {
            this.duration = duration;
            this.currency = currency;
            this.price = price;
        }
    }

    /// <summary>
    /// Период подписки с действием промо-акции.
    /// </summary>
    public sealed class PromoPeriod : SubscriptionPeriod {

        /// <summary>
        /// Длительность периода в формате ISO 8601.
        /// </summary>
        public string duration { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public string currency { get; }

        /// <summary>
        /// Цена в минимальных единицах валюты.
        /// </summary>
        public int price { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="duration">Длительность периода в формате ISO 8601.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="price">Цена в минимальных единицах валюты.</param>
        internal PromoPeriod(string duration, string currency, int price) {
            this.duration = duration;
            this.currency = currency;
            this.price = price;
        }
    }

    /// <summary>
    /// Основной период оплачиваемой подписки.
    /// </summary>
    public sealed class MainPeriod : SubscriptionPeriod {

        /// <summary>
        /// Длительность периода в формате ISO 8601.
        /// </summary>
        public string duration { get; }

        /// <summary>
        /// Код валюты ISO 4217.
        /// </summary>
        public string currency { get; }

        /// <summary>
        /// Цена в минимальных единицах валюты.
        /// </summary>
        public int price { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="duration">Длительность периода в формате ISO 8601.</param>
        /// <param name="currency">Код валюты ISO 4217.</param>
        /// <param name="price">Цена в минимальных единицах валюты.</param>
        internal MainPeriod(string duration, string currency, int price) {
            this.duration = duration;
            this.currency = currency;
            this.price = price;
        }
    }

    /// <summary>
    /// Грейс период.
    /// </summary>
    public sealed class GracePeriod : SubscriptionPeriod {

        /// <summary>
        /// Длительность периода в формате ISO 8601.
        /// </summary>
        public string duration { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="duration">Длительность периода в формате ISO 8601.</param>
        internal GracePeriod(string duration) {
            this.duration = duration;
        }
    }

    /// <summary>
    /// Период ожидания или временной приостановки подписки.
    /// </summary>
    public sealed class HoldPeriod : SubscriptionPeriod {

        /// <summary>
        /// Длительность периода в формате ISO 8601.
        /// </summary>
        public string duration { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="duration">Длительность периода в формате ISO 8601.</param>
        internal HoldPeriod(string duration) {
            this.duration = duration;
        }
    }
}
