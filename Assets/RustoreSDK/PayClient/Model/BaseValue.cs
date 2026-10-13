using System.Collections.Generic;

namespace RuStore.PayClient {

    public abstract class BaseValue<T> {

        public T value { get; }

        protected BaseValue(T value) {
            this.value = value;
        }

        public override bool Equals(object obj) {
            if (obj is BaseValue<T> other) {
                return EqualityComparer<T>.Default.Equals(value, other.value);
            }

            return false;
        }

        public override int GetHashCode() => value?.GetHashCode() ?? 0;

        public override string ToString() => $"{GetType().Name}(value='{value}')";
    }
}
