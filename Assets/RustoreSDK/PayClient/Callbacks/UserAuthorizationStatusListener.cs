using RuStore.Internal;
using System;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public class UserAuthorizationStatusListener : ErrorListener {

        private const string javaClassName = "ru.rustore.unitysdk.payclient.callbacks.UserAuthorizationStatusListener";

        private Action<UserAuthorizationStatus> onSuccessAction;

        public UserAuthorizationStatusListener(Action<RuStoreError> onFailure, Action<UserAuthorizationStatus> onSuccess) : base(javaClassName, onFailure) {
            onSuccessAction = onSuccess;
        }

        public void OnSuccess(AndroidJavaObject responseObject) {

            try {
                var response = DataConverter.ConvertEnumStrict<UserAuthorizationStatus>(responseObject);
                CallbackHandler.AddCallback(() => {
                    onSuccessAction?.Invoke(response);
                });
            }
            catch (Exception e) {
                var javaError = new AndroidJavaObject("java.lang.Exception", e.Message);
                OnFailure(javaError);
            }
        }

        protected override RuStoreError ConvertError(AndroidJavaObject errorObject) =>
            DataConverter.ConvertExceptionClasses(errorObject);
    }
}
