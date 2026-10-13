using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using RuStore;
using RuStore.PayClient;

/// <summary>
/// RuStore Pay SDK billing core.
/// Every reward goes through exactly one path: DeliverPurchase -> OnProductPurchaseProcessed.
/// Purchases interrupted mid-payment, and non-consumables bought on another install,
/// are picked up by RecoverPurchases() on start and on every resume.
/// </summary>
public class RuStoreBillingManager : MonoBehaviour
{
    public static RuStoreBillingManager Instance { get; private set; }

    [Tooltip("Product catalog asset - ids must match the RuStore Console.")]
    [SerializeField] private RuStoreProductConfig productConfig;

    public bool IsBillingInitialized { get; private set; }

    // UI delegates
    public UnityAction<bool> OnBillingInitCompleted;
    public UnityAction<string, bool> OnProductPurchaseProcessed;
    public UnityAction OnPricesLoaded;

    private readonly Dictionary<string, string> _prices = new Dictionary<string, string>();
    private readonly HashSet<string> _deliveredPurchaseIds = new HashSet<string>();

    // The recovery sweep runs in Splash, but ShopManager (the only subscriber) lives in GamePlay.
    // Deliveries queue here until something is listening, so a recovered purchase is never
    // acknowledged before the game has actually granted it.
    private readonly List<Delivery> _pendingDeliveries = new List<Delivery>();

    private struct Delivery
    {
        public string productId;
        public PurchaseId purchaseId;
        public bool acknowledge;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (productConfig == null)
        {
            Debug.LogError("RuStoreBillingManager: No RuStoreProductConfig assigned. " +
                           "Create one via Assets > Create > RuStore > Product Config.");
        }

        RuStorePayClient.Instance.GetPurchaseAvailability(
            onFailure: error =>
            {
                Debug.LogError($"[RuStore] Availability check failed [{error.name}]: {error.description}");
                IsBillingInitialized = false;
                OnBillingInitCompleted?.Invoke(false);
            },
            onSuccess: result =>
            {
                IsBillingInitialized = result.isAvailable;

                if (!result.isAvailable)
                {
                    Debug.LogWarning($"[RuStore] Payments unavailable: {result.cause?.description}");
                    OnBillingInitCompleted?.Invoke(false);
                    return;
                }

                Debug.Log("[RuStore] Payments available.");
                OnBillingInitCompleted?.Invoke(true);
                LoadPrices();
                RecoverPurchases();
            });
    }

    // Payment happens in the RuStore app and returns via deeplink; if the game was killed
    // meanwhile, onSuccess never fired, so re-check on every resume.
    private void OnApplicationPause(bool paused)
    {
        if (!paused && IsBillingInitialized) RecoverPurchases();
    }

    public void PurchaseProduct(string productId)
    {
        if (!IsBillingInitialized)
        {
            Debug.LogWarning("[RuStore] Billing is not initialized yet.");
            OnProductPurchaseProcessed?.Invoke(productId, false);
            return;
        }

        RuStorePayClient.Instance.Purchase(
            parameters: new ProductPurchaseParams(new ProductId(productId)),
            preferredPurchaseType: PreferredPurchaseType.ONE_STEP,
            onFailure: error => OnPurchaseFailure(productId, error),
            onSuccess: result =>
            {
                // ONE_STEP is only *preferred* - RuStore may still return a TWO_STEP purchase,
                // where funds are merely held and auto-reverse after 6h unless we confirm them.
                if (result.purchaseType == PurchaseType.TWO_STEP)
                {
                    ConfirmThenDeliver(result.productId.value, result.purchaseId);
                    return;
                }

                DeliverPurchase(result.productId.value, result.purchaseId, acknowledge: true);
            });
    }

    public string GetPrice(string productId) =>
        _prices.TryGetValue(productId, out var price) ? price : null;

    private void LoadPrices()
    {
        if (productConfig == null) return;

        var ids = productConfig.AllProductIds();
        if (ids.Count == 0) return;

        var productIds = new ProductId[ids.Count];
        for (int i = 0; i < ids.Count; i++) productIds[i] = new ProductId(ids[i]);

        RuStorePayClient.Instance.GetProducts(
            productIds,
            onFailure: error => Debug.LogError($"[RuStore] GetProducts failed [{error.name}]: {error.description}"),
            onSuccess: products =>
            {
                foreach (var product in products)
                    _prices[product.productId.value] = product.amountLabel?.value;

                Debug.Log($"[RuStore] Loaded prices for {products.Count} products.");
                OnPricesLoaded?.Invoke();
            });
    }

    /// <summary>
    /// Grants anything the player paid for but never received (app killed during payment,
    /// deeplink lost, acknowledge failed) and restores non-consumables after a reinstall.
    /// </summary>
    public void RecoverPurchases()
    {
        // Two-step purchases still holding funds: confirm them or they reverse after 6h.
        RuStorePayClient.Instance.GetPurchases(
            null, ProductPurchaseStatus.PAID, null,
            onFailure: error => Debug.LogError($"[RuStore] GetPurchases(PAID) failed: {error.description}"),
            onSuccess: purchases =>
            {
                foreach (var purchase in purchases)
                {
                    if (!(purchase is ProductPurchase product)) continue;
                    ConfirmThenDeliver(product.productId.value, product.purchaseId);
                }
            });

        RuStorePayClient.Instance.GetPurchases(
            null, ProductPurchaseStatus.CONFIRMED, null,
            onFailure: error => Debug.LogError($"[RuStore] GetPurchases(CONFIRMED) failed: {error.description}"),
            onSuccess: purchases =>
            {
                foreach (var purchase in purchases)
                {
                    if (!(purchase is ProductPurchase product)) continue;

                    // Paid but never acknowledged - the player may never have received it.
                    if (product.acknowledgementState == AcknowledgementState.PENDING)
                    {
                        DeliverPurchase(product.productId.value, product.purchaseId, acknowledge: true);
                    }
                    // Already settled: re-apply non-consumables so "remove ads" and friends
                    // survive a reinstall. Granting these must stay idempotent.
                    else if (product.productType == ProductType.NON_CONSUMABLE_PRODUCT)
                    {
                        DeliverPurchase(product.productId.value, product.purchaseId, acknowledge: false);
                    }
                }
            });
    }

    private void ConfirmThenDeliver(string productId, PurchaseId purchaseId)
    {
        RuStorePayClient.Instance.ConfirmTwoStepPurchase(
            purchaseId,
            developerPayload: null,
            onFailure: error => OnPurchaseFailure(productId, error),
            onSuccess: () => DeliverPurchase(productId, purchaseId, acknowledge: true));
    }

    private void DeliverPurchase(string productId, PurchaseId purchaseId, bool acknowledge)
    {
        // Acknowledging is what stops the sweep re-delivering, so guard the window between
        // granting and the acknowledge round-trip - and deliver each purchase once per session.
        if (!_deliveredPurchaseIds.Add(purchaseId.value)) return;

        _pendingDeliveries.Add(new Delivery
        {
            productId = productId,
            purchaseId = purchaseId,
            acknowledge = acknowledge
        });

        FlushDeliveries();
    }

    private void Update()
    {
        if (_pendingDeliveries.Count > 0) FlushDeliveries();
    }

    private void FlushDeliveries()
    {
        if (OnProductPurchaseProcessed == null) return;

        foreach (var delivery in _pendingDeliveries)
        {
            Debug.Log($"[RuStore] Delivering {delivery.productId} (purchase {delivery.purchaseId.value}).");
            OnProductPurchaseProcessed.Invoke(delivery.productId, true);

            if (delivery.acknowledge) Acknowledge(delivery.productId, delivery.purchaseId);
        }

        _pendingDeliveries.Clear();
    }

    private void Acknowledge(string productId, PurchaseId purchaseId)
    {
        RuStorePayClient.Instance.UpdateAcknowledgementState(
            purchaseId,
            AcknowledgementState.ACKNOWLEDGED,
            developerPayload: null,
            // Reward is already granted; a failure here means the next sweep offers it again,
            // which is what the delivered-id guard above is for.
            onFailure: error => Debug.LogError($"[RuStore] Acknowledge failed for {productId}: {error.description}"),
            onSuccess: _ => Debug.Log($"[RuStore] Acknowledged {productId}."));
    }

    private void OnPurchaseFailure(string productId, RuStoreError error)
    {
        if (error is RuStorePaymentException.ProductPurchaseCancelled)
            Debug.Log($"[RuStore] Purchase of {productId} cancelled by user.");
        else
            Debug.LogError($"[RuStore] Purchase failed for {productId} [{error.name}]: {error.description}");

        OnProductPurchaseProcessed?.Invoke(productId, false);
    }
}
