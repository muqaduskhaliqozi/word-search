using RuStore.Internal;
using System;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class ConfirmTwoStepPurchaseResponseListener : SimpleResponseListener {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.ConfirmTwoStepPurchaseListener";

        public ConfirmTwoStepPurchaseResponseListener(Action<RuStoreError> onFailure, Action onSuccess) : base(javaClassName, onFailure, onSuccess) {
        }

        protected override RuStoreError ConvertError(AndroidJavaObject errorObject) =>
            DataConverter.ConvertExceptionClasses(errorObject);
    }
}
