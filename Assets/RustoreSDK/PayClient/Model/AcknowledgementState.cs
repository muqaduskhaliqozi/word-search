namespace RuStore.PayClient {

    /// <summary>
    /// Состояние подтверждения покупки.
    /// </summary>
    public enum AcknowledgementState {
		
        /// <summary>
        /// Покупка ожидает подтверждения.
        /// </summary>
        PENDING,
		
        /// <summary>
        /// Покупка подтверждена.
        /// </summary>
        ACKNOWLEDGED,
		
        /// <summary>
        /// Неизвестное состояние подтверждения.
        /// </summary>
        UNKNOWN
    }
}
