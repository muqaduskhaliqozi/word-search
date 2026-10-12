using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// In-app purchases (Unity IAP 5): the non-consumable "Remove Ads" product.
/// Created automatically on game start and kept alive across scenes.
/// Buying (or restoring) it sets GameSettings.AdsRemoved, which also sets the "RemoveAds" key
/// MediationHandler checks, and hides the banners that are already on screen.
/// Rewarded videos (power-ups, coins) keep working after Remove Ads.
/// </summary>
public class IAPManager : MonoBehaviour
{
    public const string RemoveAdsId = "com.word.search.remove.ads";

    public static IAPManager Instance { get; private set; }
    /// <summary>Localized price from the store, e.g. "Rs 2,990.00" (empty until the store answers).</summary>
    public static string RemoveAdsPrice { get; private set; } = "";
    public static event Action<string> PriceUpdated;
    public static event Action<bool, string> PurchaseFinished; // success, message

    private StoreController store;
    private bool connected;
    private bool connecting;
    private bool buyWhenReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("IAPManager");
        DontDestroyOnLoad(go);
        go.AddComponent<IAPManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SkyMenu.RemoveAdsRequested += BuyRemoveAds;
    }

    private void OnDestroy()
    {
        if (Instance == this) SkyMenu.RemoveAdsRequested -= BuyRemoveAds;
    }

    private void Start()
    {
        ConsentGate.RunWhenAccepted(Connect);
    }

    private async void Connect()
    {
        if (connected || connecting) return;
        connecting = true;
        try
        {
            store = UnityIAPServices.StoreController();
            store.OnProductsFetched += OnProductsFetched;
            store.OnProductsFetchFailed += f => Debug.LogWarning("[IAP] Product fetch failed: " + f.FailureReason);
            store.OnPurchasesFetched += OnPurchasesFetched;
            store.OnPurchasesFetchFailed += f => Debug.LogWarning("[IAP] Purchase fetch failed: " + f.Message);
            store.OnPurchasePending += OnPurchasePending;
            store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            store.OnPurchaseFailed += OnPurchaseFailed;
            store.OnStoreDisconnected += d => { connected = false; Debug.LogWarning("[IAP] Store disconnected: " + d.Message); };

            await store.Connect();
            connected = true;
            store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(RemoveAdsId, ProductType.NonConsumable) });
        }
        catch (Exception e)
        {
            connected = false;
            Debug.LogWarning("[IAP] Could not connect to the store: " + e.Message);
        }
        finally
        {
            connecting = false;
        }
    }

    // ------------------------------------------------------------------ store callbacks
    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] store returned {products.Count} product(s)");
        foreach (Product p in products)
        {
            if (p?.definition == null || p.definition.id != RemoveAdsId) continue;
            if (p.metadata != null && !string.IsNullOrEmpty(p.metadata.localizedPriceString))
            {
                RemoveAdsPrice = p.metadata.localizedPriceString;
                PriceUpdated?.Invoke(RemoveAdsPrice);
            }
        }
        // restores Remove Ads automatically (reinstall / new device)
        store.FetchPurchases();
        if (buyWhenReady) { buyWhenReady = false; BuyRemoveAds(); }
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (ConfirmedOrder o in orders.ConfirmedOrders)
            if (Contains(o, RemoveAdsId)) Grant(false);
        foreach (PendingOrder o in orders.PendingOrders)
            if (Contains(o, RemoveAdsId)) store.ConfirmPurchase(o);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        if (Contains(order, RemoveAdsId)) Grant(true);
        store.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (Contains(order, RemoveAdsId)) Grant(false);
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {order.FailureReason} {order.Details}");
        PurchaseFinished?.Invoke(false, order.FailureReason.ToString());
    }

    private static bool Contains(Order order, string id)
    {
        if (order?.CartOrdered == null) return false;
        foreach (CartItem item in order.CartOrdered.Items())
        {
            if (item?.Product?.definition != null && item.Product.definition.id == id) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ public API
    /// <summary>Starts the Remove Ads purchase (hooked to every Remove Ads / No-Ads button).</summary>
    public void BuyRemoveAds()
    {
        if (GameSettings.AdsRemoved) { PurchaseFinished?.Invoke(true, "Already purchased"); return; }
        if (!connected || store == null)
        {
            buyWhenReady = true;
            Connect();
            return;
        }
        Product p = store.GetProductById(RemoveAdsId);
        if (p == null || !p.availableToPurchase)
        {
            Debug.LogWarning("[IAP] Remove Ads product is not available yet. Is it active in Google Play Console?");
            buyWhenReady = true;
            store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(RemoveAdsId, ProductType.NonConsumable) });
            return;
        }
        store.PurchaseProduct(p);
    }

    /// <summary>Manual restore (needed on iOS; Google Play restores automatically on start).</summary>
    public void RestorePurchases()
    {
        if (store == null) return;
        store.RestoreTransactions((ok, msg) => Debug.Log($"[IAP] Restore finished: {ok} {msg}"));
    }

    private void Grant(bool justBought)
    {
        if (!GameSettings.AdsRemoved) GameSettings.AdsRemoved = true;
        if (MediationHandler.Instance != null) MediationHandler.Instance.RemoveAdsPurchased(); // tear down live banners
        if (justBought)
        {
            GameEvents.RemoveAdsPurchased();
            PurchaseFinished?.Invoke(true, "Ads removed");
        }
    }
}
