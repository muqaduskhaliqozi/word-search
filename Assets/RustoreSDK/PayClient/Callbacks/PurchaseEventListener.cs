#nullable enable

using RuStore.PayClient.Internal;
using System;
using UnityEngine;

namespace RuStore.PayClient {

    public class PurchaseEventListener : AndroidJavaProxy {

        private const string javaClassName = "ru.rustore.sdk.pay.callback.PurchaseEventListener";

        private Action<PurchaseId,  InvoiceId>  onPaymentCompletedAction;
        private Action<PurchaseId?, InvoiceId?> onPaymentFailedAction;
        private Action<PurchaseId,  InvoiceId>  onPaymentStartedAction;
        private Action<PurchaseId?, InvoiceId?> onPurchaseCancelledAction;
        private Action<PurchaseId,  InvoiceId>  onPurchaseCreatedAction;

        public PurchaseEventListener(
            Action<PurchaseId,  InvoiceId>  onPaymentCompleted,
            Action<PurchaseId?, InvoiceId?> onPaymentFailed,
            Action<PurchaseId,  InvoiceId>  onPaymentStarted,
            Action<PurchaseId?, InvoiceId?> onPurchaseCancelled,
            Action<PurchaseId,  InvoiceId>  onPurchaseCreated
            ) : base(javaClassName) {

            onPaymentCompletedAction  = onPaymentCompleted;
            onPaymentFailedAction     = onPaymentFailed;
            onPaymentStartedAction    = onPaymentStarted;
            onPurchaseCancelledAction = onPurchaseCancelled;
            onPurchaseCreatedAction   = onPurchaseCreated;
        }

        public void onPaymentCompleted(AndroidJavaObject purchaseIdObject, AndroidJavaObject invoiceIdObject) {
            var (purchaseId, invoiceId) = DataConverter.ConvertPurchaseIdInvoiceIdPair(purchaseIdObject, invoiceIdObject);
            CallbackHandler.AddCallback(() => {
                onPaymentCompletedAction(purchaseId, invoiceId);
            });
        }

        public void onPaymentFailed(AndroidJavaObject? purchaseIdObject, AndroidJavaObject? invoiceIdObject) {
            var (purchaseId, invoiceId) = DataConverter.ConvertPurchaseIdInvoiceIdPair(purchaseIdObject, invoiceIdObject);
            CallbackHandler.AddCallback(() => {
                onPaymentFailedAction(purchaseId, invoiceId);
            });
        }

        public void onPaymentStarted(AndroidJavaObject purchaseIdObject, AndroidJavaObject invoiceIdObject) {
            var (purchaseId, invoiceId) = DataConverter.ConvertPurchaseIdInvoiceIdPair(purchaseIdObject, invoiceIdObject);
            CallbackHandler.AddCallback(() => {
                onPaymentStartedAction(purchaseId, invoiceId);
            });
        }

        public void onPurchaseCancelled(AndroidJavaObject? purchaseIdObject, AndroidJavaObject? invoiceIdObject) {
            var (purchaseId, invoiceId) = DataConverter.ConvertPurchaseIdInvoiceIdPair(purchaseIdObject, invoiceIdObject);
            CallbackHandler.AddCallback(() => {
                onPurchaseCancelledAction(purchaseId, invoiceId);
            });
        }

        public void onPurchaseCreated(AndroidJavaObject purchaseIdObject, AndroidJavaObject invoiceIdObject) {
            var (purchaseId, invoiceId) = DataConverter.ConvertPurchaseIdInvoiceIdPair(purchaseIdObject, invoiceIdObject);
            CallbackHandler.AddCallback(() => {
                onPurchaseCreatedAction(purchaseId, invoiceId);
            });
        }
    }
}
