using System;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class ProductPurchaseResultListener : AndroidJavaProxy {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.ProductPurchaseResultListener";

        private Action<ProductPurchaseResult> onSuccessAction;
        private Action<RuStoreError> onFailureAction;

        public ProductPurchaseResultListener(Action<RuStoreError> onFailure, Action<ProductPurchaseResult> onSuccess)
            : base (javaClassName) {

            onSuccessAction = onSuccess;
            onFailureAction = onFailure;
        }

        public void OnSuccess(AndroidJavaObject responseObject) {
            var response = DataConverter.ConvertProductPurchaseResult(responseObject);
            CallbackHandler.AddCallback(() => {
                onSuccessAction(response);
            });
        }

        public void OnFailure(AndroidJavaObject errorObject) {
            var error = DataConverter.ConvertExceptionClasses(errorObject);
            CallbackHandler.AddCallback(() => {
                onFailureAction(error);
            });
        }
    }
}
