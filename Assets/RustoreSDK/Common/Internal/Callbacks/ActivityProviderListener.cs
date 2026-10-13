using System;
using UnityEngine;

namespace RuStore.Internal {

    public sealed class ActivityProviderListener : AndroidJavaProxy {

        private const string javaClassName = "ru.rustore.unitysdk.core.IActivityProvider";

        private readonly Func<AndroidJavaObject> onGetActivityAction;

        public ActivityProviderListener(Func<AndroidJavaObject> onGetActivity) : base(javaClassName) {
            onGetActivityAction = onGetActivity ?? throw new ArgumentNullException(nameof(onGetActivity));
        }

        public AndroidJavaObject getCurrentActivity() {
            var activity = onGetActivityAction.Invoke();
            if (activity == null)
                throw new InvalidOperationException("[RuStore] ActivityProviderListener: getCurrentActivity returned null");

            return activity;
        }
    }
}
