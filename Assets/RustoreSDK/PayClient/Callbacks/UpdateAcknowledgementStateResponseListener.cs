using RuStore.Internal;
using System;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class UpdateAcknowledgementStateResponseListener : ResponseListener<AcknowledgementState> {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.UpdateAcknowledgementStateListener";

        public UpdateAcknowledgementStateResponseListener(Action<RuStoreError> onFailure, Action<AcknowledgementState> onSuccess) : base(javaClassName, onFailure, onSuccess) {
        }

        protected override AcknowledgementState ConvertResponse(AndroidJavaObject responseObject) =>
            (AcknowledgementState)DataConverter.ConvertEnum<AcknowledgementState>(responseObject);

        protected override RuStoreError ConvertError(AndroidJavaObject errorObject) =>
            DataConverter.ConvertExceptionClasses(errorObject);
    }
}
