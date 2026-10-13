using RuStore.Internal;
using System;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class PurchaseResponseListener : ResponseListener<IPurchase> {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.PurchaseResponseListener";

        public PurchaseResponseListener(Action<RuStoreError> onFailure, Action<IPurchase> onSuccess) : base(javaClassName, onFailure, onSuccess) {
        }

        protected override IPurchase ConvertResponse(AndroidJavaObject responseObject) =>
            responseObject != null ? DataConverter.ConvertPurchase(responseObject) : null;

        protected override RuStoreError ConvertError(AndroidJavaObject errorObject) =>
            DataConverter.ConvertExceptionClasses(errorObject);
    }
}
