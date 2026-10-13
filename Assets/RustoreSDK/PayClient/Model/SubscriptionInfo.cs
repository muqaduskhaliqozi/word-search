using System.Collections.Generic;

namespace RuStore.PayClient {

    /// <summary>
    /// Информация о подписке.
    /// </summary>
    public class SubscriptionInfo {

        /// <summary>
        /// Список периодов подписки.
        /// </summary>
        public List<SubscriptionPeriod> periods { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="periods">Список периодов подписки.</param>
        internal SubscriptionInfo(List<SubscriptionPeriod> periods) {
            this.periods = periods;
        }
    }
}
