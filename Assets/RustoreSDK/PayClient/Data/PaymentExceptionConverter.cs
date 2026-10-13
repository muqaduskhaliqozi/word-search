using RuStore.Internal;
using UnityEngine;

namespace RuStore.PayClient.Internal {

    public static partial class DataConverter {

        public static RuStorePaymentException ConvertExceptionClasses(AndroidJavaObject obj) {
            var resultType = "";
            if (obj != null) {
                using (var javaClass = obj.Call<AndroidJavaObject>("getClass")) {
                    var className = javaClass.Call<string>("getName").Split('$');
                    resultType = className[className.Length - 1];
                }
            }

            switch (resultType) {
                case "ApplicationSchemeWasNotProvided":
                    return ConvertApplicationSchemeWasNotProvided(obj);

                case "EmptyPaymentTokenException":
                    return ConvertEmptyPaymentTokenException(obj);

                case "ProductPurchaseCancelled":
                    return ConvertProductPurchaseCancelled(obj);

                case "ProductPurchaseException":
                    return ConvertProductPurchaseException(obj);

                case "RuStorePayClientAlreadyExist":
                    return ConvertRuStorePayClientAlreadyExist(obj);

                case "RuStorePayClientNotCreated":
                    return ConvertRuStorePayClientNotCreated(obj);

                case "RuStorePayInvalidActivePurchase":
                    return ConvertRuStorePayInvalidActivePurchase(obj);

                case "RuStorePayInvalidConsoleAppId":
                    return ConvertRuStorePayInvalidConsoleAppId(obj);

                case "RuStorePaySignatureException":
                    return ConvertRuStorePaySignatureException(obj);

                case "RuStorePaymentCommonException":
                    return ConvertRuStorePaymentCommonException(obj);

                case "RuStorePaymentNetworkException":
                    return ConvertRuStorePaymentNetworkException(obj);

                case "InvalidCardBindingIdException":
                    return ConvertInvalidCardBindingIdException(obj);

                default:
                    return ConvertRuStorePaymentException(obj);
            }
        }

        public static RuStorePaymentException ConvertRuStorePaymentException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);

            return new RuStorePaymentException(name, message, cause);
        }

        public static RuStorePaymentException.ApplicationSchemeWasNotProvided ConvertApplicationSchemeWasNotProvided(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.ApplicationSchemeWasNotProvided(name, message, cause);
        }

        public static RuStorePaymentException.EmptyPaymentTokenException ConvertEmptyPaymentTokenException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.EmptyPaymentTokenException(name, message, cause);
        }

        public static RuStorePaymentException.ProductPurchaseCancelled ConvertProductPurchaseCancelled(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            var productType = ConvertEnum<ProductType>(obj.Get<AndroidJavaObject>("productType"));
            var purchaseId = obj.Get<AndroidJavaObject>("purchaseId")?.Get<string>("value");
            var purchaseType = ConvertEnum<PurchaseType>(obj.Get<AndroidJavaObject>("purchaseType"));

            return new RuStorePaymentException.ProductPurchaseCancelled(
                name,
                message,
                cause,
                productType,
                purchaseId != null ? new PurchaseId(purchaseId) : null,
                purchaseType
            );
        }

        public static RuStorePaymentException.ProductPurchaseException ConvertProductPurchaseException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            var invoiceId = obj.Get<AndroidJavaObject>("invoiceId")?.Get<string>("value");
            var orderId = obj.Get<AndroidJavaObject>("orderId")?.Get<string>("value");
            var productId = obj.Get<AndroidJavaObject>("productId")?.Get<string>("value");
            var productType = ConvertEnum<ProductType>(obj.Get<AndroidJavaObject>("productType"));
            var purchaseId = obj.Get<AndroidJavaObject>("purchaseId")?.Get<string>("value");
            var purchaseType = ConvertEnum<PurchaseType>(obj.Get<AndroidJavaObject>("purchaseType"));
            var quantity = obj.Get<AndroidJavaObject>("quantity")?.Get<int>("value");
            var sandbox = obj.Get<AndroidJavaObject>("sandbox")?.Call<bool>("booleanValue");

            return new RuStorePaymentException.ProductPurchaseException(
                name,
                message,
                cause,
                invoiceId != null ? new InvoiceId(invoiceId) : null,
                orderId != null ? new OrderId(orderId) : null,
                productId != null ? new ProductId(productId) : null,
                productType,
                purchaseId != null ? new PurchaseId(purchaseId) : null,
                purchaseType,
                quantity != null ? new Quantity((int)quantity) : null,
                sandbox != null ? (bool)sandbox : null
            );
        }

        public static RuStorePaymentException.RuStorePayClientAlreadyExist ConvertRuStorePayClientAlreadyExist(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePayClientAlreadyExist(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePayClientNotCreated ConvertRuStorePayClientNotCreated(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePayClientNotCreated(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePayInvalidActivePurchase ConvertRuStorePayInvalidActivePurchase(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePayInvalidActivePurchase(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePayInvalidConsoleAppId ConvertRuStorePayInvalidConsoleAppId(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePayInvalidConsoleAppId(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePaySignatureException ConvertRuStorePaySignatureException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePaySignatureException(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePaymentCommonException ConvertRuStorePaymentCommonException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePaymentCommonException(name, message, cause);
        }

        public static RuStorePaymentException.RuStorePaymentNetworkException ConvertRuStorePaymentNetworkException(AndroidJavaObject obj) {
            var code = obj.Get<string>("code");
            var id = obj.Get<string>("id");
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.RuStorePaymentNetworkException(code, id, name, message, cause);
        }

        public static RuStorePaymentException.InvalidCardBindingIdException ConvertInvalidCardBindingIdException(AndroidJavaObject obj) {
            var (name, message, cause) = ExtractNameMessageCause(obj);
            
            return new RuStorePaymentException.InvalidCardBindingIdException(name, message, cause);
        }

        private static (string name, string message, RuStoreError cause) ExtractNameMessageCause(AndroidJavaObject obj) {

            var errorJavaClass = obj.Call<AndroidJavaObject>("getClass");
            var name = errorJavaClass.Call<string>("getSimpleName");
            var message = obj.Call<string>("getMessage");

            var causeObject = obj.Get<AndroidJavaObject>("cause");
            RuStoreError cause = causeObject != null
                ? ErrorDataConverter.ConvertError(causeObject)
                : null;

            return (name, message, cause);
        }
    }
}
