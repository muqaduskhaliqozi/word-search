using RuStore.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class BillingSubscriptionsResponseListener : ResponseListener<List<BillingSubscription>> {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.BillingSubscriptionsResponseListener";

        public BillingSubscriptionsResponseListener(Action<RuStoreError> onFailure, Action<List<BillingSubscription>> onSuccess) : base(javaClassName, onFailure, onSuccess) {
        }

        protected override List<BillingSubscription> ConvertResponse(AndroidJavaObject responseObject) {
            var response = new List<BillingSubscription>();

            if (responseObject != null) {
                var size = responseObject.Call<int>("size");
                for (var i = 0; i < size; i++) {
                    using (var p = responseObject.Call<AndroidJavaObject>("get", i)) {
                        if (p != null) {
                            response.Add(DataConverter.ConvertBillingSubscription(p));
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
