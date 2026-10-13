using System;

namespace RuStore.PayClient {

    public interface IPurchaseStatus<T> : IPurchase where T : Enum {
        new T status { get; }
    }
}
