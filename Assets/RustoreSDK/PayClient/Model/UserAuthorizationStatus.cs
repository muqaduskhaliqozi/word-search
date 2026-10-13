namespace RuStore.PayClient {

    /// <summary>
    /// Статус авторизации пользователя.
    /// </summary>
    public enum UserAuthorizationStatus {

        /// <summary>
        /// Пользователь авторизован в RuStore.
        /// </summary>
        AUTHORIZED,

        /// <summary>
        /// Пользователь неавторизован в RuStore.
        /// Данное значение также вернется если у пользователя нет установленного МП RuStore на девайсе.
        /// </summary>
        UNAUTHORIZED
    }
}
