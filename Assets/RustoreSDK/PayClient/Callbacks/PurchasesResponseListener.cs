using RuStore.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class PurchasesResponseListener : ResponseListener<List<IPurchase>> {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.PurchasesResponseListener";

        public PurchasesResponseListener(Action<RuStoreError> onFailure, Action<List<IPurchase>> onSuccess) : base(javaClassName, onFailure, onSuccess) {
        }

        protected override List<IPurchase> ConvertResponse(AndroidJavaObject responseObject)  {
            var response = new List<IPurchase>();

            if (responseObject != null) {
                var size = responseObject.Call<int>("size");
                for (var i = 0; i < size; i++) {
                    using (var p = responseObject.Call<AndroidJavaObject>("get", i)) {
                        if (p != null) {
                            response.Add(DataConverter.ConvertPurchase(p));
                        }
                    }
                }
            }

            return response;
        }

        protected override RuStoreError ConvertError(AndroidJavaObject errorObject) =>
            DataConverter.ConvertExceptionClasses(errorObject);
    }
}
