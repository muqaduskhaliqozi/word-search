#nullable enable

using System;
using RuStore.CoreClient;
using RuStore.PayClient.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace RuStore.PayClient {

    /// <summary>
    /// Класс реализует API для интегрирации платежей в мобильное приложение.
    /// </summary>
    public class RuStorePayClient {

        /// <summary>
        /// Версия плагина.
        /// </summary>
        public static string PluginVersion = "11.1.0";

        private static RuStorePayClient? instance;
        private AndroidJavaObject? clientWrapper { get; }

        /// <summary>
        /// Возвращает единственный экземпляр RuStorePayClient (реализация паттерна Singleton).
        /// Если экземпляр еще не создан, создает его.
        /// </summary>
        /// <example>@include public_static_RuStorePayClient_Instance.cs</example>
        public static RuStorePayClient Instance {
            get {
                if (instance == null)
                    instance = new RuStorePayClient();

                return instance;
            }
        }

        private RuStorePayClient() {
            if (!IsPlatformSupported()) return;

            CallbackHandler.InitInstance();
            using (var clientJavaClass = new AndroidJavaClass("ru.rustore.unitysdk.payclient.RuStoreUnityPayClient")) {
                clientWrapper = clientJavaClass.GetStatic<AndroidJavaObject>("INSTANCE");
            }
        }

        /// <summary>
        /// Проверка статуса авторизации пользователя.
        /// </summary>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает значение перечисленяи RuStore.PayClient.UserAuthorizationStatus с информцаией о статусе авторизации пользователя.
        /// </param>
        /// <example>@include public_void_GetUserAuthorizationStatus.cs</example>
        public void GetUserAuthorizationStatus(Action<RuStoreError> onFailure, Action<UserAuthorizationStatus> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new UserAuthorizationStatusListener(onFailure, onSuccess);
            clientWrapper?.Call("getUserAuthorizationStatus", listener);
        }

        /// <summary>
        /// Проверка доступности платежей.
        /// Если все условия выполняются, возвращается RuStore.PayClient.PurchaseAvailabilityResult.isAvailable == true.
        /// В противном случае возвращается RuStore.PayClient.PurchaseAvailabilityResult.isAvailable == false.
        /// </summary>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект RuStore.PayClient.PurchaseAvailabilityResult с информцаией о доступности оплаты.
        /// </param>
        /// <example>@include public_void_GetPurchaseAvailability.cs</example>
        public void GetPurchaseAvailability(Action<RuStoreError> onFailure, Action<PurchaseAvailabilityResult> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new PurchaseAvailabilityListener(onFailure, onSuccess);
            clientWrapper?.Call("getPurchaseAvailability", listener);
        }

        /// <summary>
        /// Проверка установлен ли на устройстве пользователя RuStore.
        /// </summary>
        /// <returns>Возвращает true, если RuStore установлен, в противном случае — false.</returns>
        /// <example>@include public_bool_IsRuStoreInstalled.cs</example>
        [Obsolete("Deprecated. Use RuStoreCore.Instance.IsRuStoreInstalled instead.")]
        public bool IsRuStoreInstalled() {
            if (!IsPlatformSupported()) return false;

            return RuStoreCoreClient.Instance.IsRuStoreInstalled();
        }

        /// <summary>
        /// Получение списка продуктов, добавленных в ваше приложение через RuStore консоль.
        /// </summary>
        /// <param name="productsId">Список идентификаторов продуктов (задаются при создании продукта в консоли разработчика).
        /// Список продуктов имеет ограничение в размере 1000 элементов.</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает список объектов RuStore.PayClient.Product с информцаией о продуктах.
        /// </param>
        /// <example>@include public_void_GetProducts.cs</example>
        public void GetProducts(ProductId[] productsId, Action<RuStoreError> onFailure, Action<List<Product>> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var ids = Array.ConvertAll(productsId, p => p.value);
            var listener = new ProductsResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("getProducts", ids, listener);
        }

        /// <summary>
        /// Получение списка покупок пользователя.
        /// </summary>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает список объектов RuStore.PayClient.Purchase с информцаией о покупках.
        /// </param>
        /// <example>@include public_void_GetPurchases.cs</example>
        public void GetPurchases(Action<RuStoreError> onFailure, Action<List<IPurchase>> onSuccess) =>
            GetPurchases(null, null, null, onFailure, onSuccess);

        /// <summary>
        /// Получение списка покупок пользователя.
        /// </summary>
        /// <param name="productType">Тип продукта (необязательный параметр).</param>
        /// <param name="purchaseStatus">Статус покупки (необязательный параметр).</param>
        /// <param name="acknowledgementState">Состояние подтверждения покупки (необязательный параметр).</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает список объектов RuStore.PayClient.Purchase с информцаией о покупках.
        /// </param>
        /// <example>@include public_void_GetPurchases_with_params.cs</example>
        public void GetPurchases(ProductType? productType, Enum? purchaseStatus, AcknowledgementState? acknowledgementState, Action<RuStoreError> onFailure, Action<List<IPurchase>> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var _purchaseStatus = purchaseStatus != null
                ? $"{purchaseStatus.GetType().Name}.{purchaseStatus}"
                : null;

            var listener = new PurchasesResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("getPurchases", productType?.ToString(), _purchaseStatus, acknowledgementState?.ToString(), listener);
        }

        /// <summary>
        /// Получение информации о покупке.
        /// </summary>
        /// <param name="purchaseId">
        /// Идентификатор продукта, который был присвоен продукту в RuStore Консоли.
        /// </param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект RuStore.PayClient.Purchase с информцаией о покупке.
        /// </param>
        /// <example>@include public_void_GetPurchase.cs</example>
        public void GetPurchase(PurchaseId purchaseId, Action<RuStoreError> onFailure, Action<IPurchase> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new PurchaseResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("getPurchase", purchaseId.value, listener);
        }

        /// <summary>
        /// Получение списка подписок, оформленных в SDK Billing Client.
        /// </summary>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает список объектов RuStore.PayClient.BillingSubscription с информацией о подписках.
        /// </param>
        /// <example>@include public_void_GetBillingSubscriptions.cs</example>
        public void GetBillingSubscriptions(Action<RuStoreError> onFailure, Action<List<BillingSubscription>> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new BillingSubscriptionsResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("getBillingSubscriptions", listener);
        }

        /// <summary>
        /// Покупка продукта.
        /// </summary>
        /// <param name="parameters">Параметры покупки продукта.</param>
        /// <param name="preferredPurchaseType">Предпочитаемый тип покупки – одностадийная (ONE_STEP) или двухстадийная (TWO_STEP).</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект ProductPurchaseResult с информцаией о результате покупки.
        /// </param>
        /// <param name="purchaseEventListener">Набор callback функций, позволяющий получать данные об invoiceId и purchaseId на разных этапах покупки – опционально.</param>
        /// <example>@include public_void_Purchase.cs</example>
        public void Purchase(ProductPurchaseParams parameters, PreferredPurchaseType preferredPurchaseType, Action<RuStoreError> onFailure, Action<ProductPurchaseResult> onSuccess, PurchaseEventListener? purchaseEventListener = null)
            => Purchase(parameters, preferredPurchaseType, SdkTheme.LIGHT, onFailure, onSuccess, purchaseEventListener);

        /// <summary>
        /// Покупка продукта.
        /// </summary>
        /// <param name="parameters">Параметры покупки продукта.</param>
        /// <param name="preferredPurchaseType">Предпочитаемый тип покупки – одностадийная (ONE_STEP) или двухстадийная (TWO_STEP).</param>
        /// <param name="sdkTheme">Цветовая тема платежной шторки.</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект ProductPurchaseResult с информцаией о результате покупки.
        /// </param>
        /// <param name="purchaseEventListener">Набор callback функций, позволяющий получать данные об invoiceId и purchaseId на разных этапах покупки – опционально.</param>
        /// <example>@include public_void_Purchase_with_theme.cs</example>
        public void Purchase(ProductPurchaseParams parameters, PreferredPurchaseType preferredPurchaseType, SdkTheme sdkTheme, Action<RuStoreError> onFailure, Action<ProductPurchaseResult> onSuccess, PurchaseEventListener? purchaseEventListener = null) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new ProductPurchaseResultListener(onFailure, onSuccess);
            clientWrapper?.Call(
                "purchase",
                parameters.productId.value,
                parameters.appUserEmail?.value,
                parameters.appUserId?.value,
                parameters.orderId?.value,
                parameters.quantity != null ? parameters.quantity.value : 1,
                parameters.developerPayload?.value,
                preferredPurchaseType.ToString(),
                sdkTheme.ToString(),
                listener,
                purchaseEventListener);
        }

        /// <summary>
        /// Покупки продукта с двустадийной оплатой.
        /// </summary>
        /// <param name="parameters">Параметры покупки продукта.</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект ProductPurchaseResult с информацией о результате покупки.
        /// </param>
        /// <param name="purchaseEventListener">Набор callback функций, позволяющий получать данные об invoiceId и purchaseId на разных этапах покупки – опционально.</param>
        /// <example>@include public_void_PurchaseTwoStep.cs</example>
        public void PurchaseTwoStep(ProductPurchaseParams parameters, Action<RuStoreError> onFailure, Action<ProductPurchaseResult> onSuccess, PurchaseEventListener? purchaseEventListener = null)
            => PurchaseTwoStep(parameters, SdkTheme.LIGHT, onFailure, onSuccess, purchaseEventListener);

        /// <summary>
        /// Покупки продукта с двустадийной оплатой.
        /// </summary>
        /// <param name="parameters">Параметры покупки продукта.</param>
        /// <param name="sdkTheme">Цветовая тема платежной шторки.</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект ProductPurchaseResult с информацией о результате покупки.
        /// </param>
        /// <param name="purchaseEventListener">Набор callback функций, позволяющий получать данные об invoiceId и purchaseId на разных этапах покупки – опционально.</param>
        /// <example>@include public_void_PurchaseTwoStep_with_theme.cs</example>
        public void PurchaseTwoStep(ProductPurchaseParams parameters, SdkTheme sdkTheme, Action<RuStoreError> onFailure, Action<ProductPurchaseResult> onSuccess, PurchaseEventListener? purchaseEventListener = null) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new ProductPurchaseResultListener(onFailure, onSuccess);
            clientWrapper?.Call(
                "purchaseTwoStep",
                parameters.productId.value,
                parameters.appUserEmail?.value,
                parameters.appUserId?.value,
                parameters.orderId?.value,
                parameters.quantity != null ? parameters.quantity.value : 1,
                parameters.developerPayload?.value,
                sdkTheme.ToString(),
                listener,
                purchaseEventListener);
        }

        /// <summary>
        /// Потребление (подтверждение) покупки. После вызова подтверждения покупка перейдёт в статус CONFIRMED.
        /// Запрос на потребление (подтверждение) покупки должен сопровождаться выдачей товара.
        /// </summary>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="developerPayload">Строка, содержащая дополнительную информацию о заказе (необязательный параметр).</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">
        /// Действие, выполняемое при успешном завершении операции.
        /// Возвращает объект наследник RuStore.PayClient.IProductPurchaseResult с информацией о результате покупки.
        /// </param>
        /// <example>@include public_void_ConfirmTwoStepPurchase.cs</example>
        public void ConfirmTwoStepPurchase(PurchaseId purchaseId, DeveloperPayload? developerPayload, Action<RuStoreError> onFailure, Action onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new ConfirmTwoStepPurchaseResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("confirmTwoStepPurchase", purchaseId.value, developerPayload?.value, listener);
        }

        /// <summary>
        /// Отмена покупки.
        /// Запрос на потребление (подтверждение) покупки должен сопровождаться выдачей товара.
        /// </summary>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">Действие, выполняемое при успешном завершении операции.</param>
        /// <example>@include public_void_CancelTwoStepPurchase.cs</example>
        public void CancelTwoStepPurchase(PurchaseId purchaseId, Action<RuStoreError> onFailure, Action onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new CancelTwoStepPurchaseResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("cancelTwoStepPurchase", purchaseId.value, listener);
        }

        /// <summary>
        /// Обновление статуса подтверждения покупки.
        /// </summary>
        /// <param name="purchaseId">Идентификатор покупки.</param>
        /// <param name="acknowledgementState">Новый статус подтверждения покупки.</param>
        /// <param name="developerPayload">Строка, содержащая дополнительную информацию о заказе (необязательный параметр).</param>
        /// <param name="onFailure">
        /// Действие, выполняемое в случае ошибки.
        /// Возвращает объект RuStore.RuStoreError с информацией об ошибке.
        /// </param>
        /// <param name="onSuccess">Действие, выполняемое при успешном завершении операции.</param>
        /// <example>@include public_void_UpdateAcknowledgementState.cs</example>
        public void UpdateAcknowledgementState(PurchaseId purchaseId, AcknowledgementState acknowledgementState, DeveloperPayload? developerPayload, Action<RuStoreError> onFailure, Action<AcknowledgementState> onSuccess) {
            if (!IsPlatformSupported(onFailure)) return;

            var listener = new UpdateAcknowledgementStateResponseListener(onFailure, onSuccess);
            clientWrapper?.Call("updateAcknowledgementState", purchaseId.value, acknowledgementState.ToString(), developerPayload?.value, listener);
        }

        private bool IsPlatformSupported(Action<RuStoreError>? onFailure = null) {
            if(Application.platform != RuntimePlatform.Android) {
                onFailure?.Invoke(new RuStoreError() {
                    name = "RuStorePayClientError",
                    description = "Unsupported platform"
                });
                return false;
            }

            return true;
        }
    }
}
