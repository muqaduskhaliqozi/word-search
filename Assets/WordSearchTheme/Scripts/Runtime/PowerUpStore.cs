using System;
using UnityEngine;

/// <summary>
/// Hint / Shuffle charges and the RuStore consumables that refill them.
///
/// Each power-up starts with <see cref="FreeUses"/> free charges. At zero the HUD opens
/// <see cref="PowerUpOfferPanel"/> instead of the rewarded video it used to play; buying adds
/// <see cref="RefillAmount"/> charges.
///
/// Purchases are granted straight into PlayerPrefs (never into a callback closed over HUD state),
/// because a payment can clear while the game is closed and is then delivered at the next launch
/// by RuStoreBillingManager's recovery sweep - in the Splash scene, before any HUD exists.
/// <see cref="ChargesChanged"/> only lets UI that happens to be open catch up.
///
/// Store side: RuStoreBillingManager (Splash scene) owns connect / price fetch / purchase /
/// recovery / acknowledge. Product ids live in Assets/RuStoreProductConfig.asset and must exist in
/// the RuStore Console as consumables.
/// adb logcat Unity:V "*:S" | grep -E "\[RuStore\]|\[PowerUps\]"
/// </summary>
public static class PowerUpStore
{
    public enum PowerUp { Hint, Shuffle }

    public const int FreeUses = 3;
    public const int RefillAmount = 3;

    // Same keys the rewarded-ad version used, so players keep the charges they already have.
    public const string HintKey = "WS_Power_Hint";
    public const string ShuffleKey = "WS_Power_Shuffle";

    public const string HintProductId = "com.dss.word.puzzle.game.hints3";
    public const string ShuffleProductId = "com.dss.word.puzzle.game.shuffles3";

    /// <summary>Charges changed (spent or granted). Courtesy for open UI - the PlayerPrefs value is the truth.</summary>
    public static event Action<PowerUp, int> ChargesChanged;
    /// <summary>A purchase started from <see cref="Buy"/> finished: success, or failed / cancelled.</summary>
    public static event Action<PowerUp, bool> PurchaseFinished;
    /// <summary>Localized prices arrived from the store.</summary>
    public static event Action PricesChanged;

    private static RuStoreBillingManager attached;

    // ------------------------------------------------------------------ mappings
    public static string CounterKeyFor(PowerUp which) => which == PowerUp.Hint ? HintKey : ShuffleKey;
    public static string ProductIdFor(PowerUp which) => which == PowerUp.Hint ? HintProductId : ShuffleProductId;
    public static string NameFor(PowerUp which) => which == PowerUp.Hint ? "Hints" : "Shuffles";

    public static bool TryFromProductId(string productId, out PowerUp which)
    {
        which = PowerUp.Hint;
        if (productId == HintProductId) { which = PowerUp.Hint; return true; }
        if (productId == ShuffleProductId) { which = PowerUp.Shuffle; return true; }
        return false;
    }

    // ------------------------------------------------------------------ charges
    public static int Charges(PowerUp which) => Mathf.Max(0, PlayerPrefs.GetInt(CounterKeyFor(which), FreeUses));

    /// <summary>Spends one charge. False (and nothing changes) when there is none left.</summary>
    public static bool TrySpend(PowerUp which)
    {
        int now = Charges(which);
        if (now <= 0) return false;
        Set(which, now - 1);
        return true;
    }

    private static void Set(PowerUp which, int value)
    {
        value = Mathf.Max(0, value);
        PlayerPrefs.SetInt(CounterKeyFor(which), value);
        PlayerPrefs.Save();
        ChargesChanged?.Invoke(which, value);
    }

    private static void Grant(PowerUp which, string why)
    {
        int now = Charges(which) + RefillAmount;
        Debug.Log($"[PowerUps] +{RefillAmount} {NameFor(which)} ({why}) -> {now}");
        Set(which, now);
    }

    // ------------------------------------------------------------------ store
    public static bool StoreReady => RuStoreBillingManager.Instance != null && RuStoreBillingManager.Instance.IsBillingInitialized;

    /// <summary>Localized store price, or null until RuStore has answered (never on non-Android).</summary>
    public static string PriceFor(PowerUp which)
    {
        RuStoreBillingManager m = RuStoreBillingManager.Instance;
        string p = m != null ? m.GetPrice(ProductIdFor(which)) : null;
        return string.IsNullOrEmpty(p) ? null : p;
    }

    /// <summary>Starts the purchase. Returns false when it could not start (the player was told why).</summary>
    public static bool Buy(PowerUp which)
    {
        EnsureAttached();
#if UNITY_EDITOR
        // RuStorePayClient refuses every non-Android platform, so the Editor can only test the UI flow.
        if (!StoreReady)
        {
            Debug.Log($"[PowerUps] (Editor) simulated purchase of {ProductIdFor(which)}");
            Grant(which, "editor simulated purchase");
            PurchaseFinished?.Invoke(which, true);
            return true;
        }
#endif
        if (!StoreReady)
        {
            Debug.LogWarning("[PowerUps] RuStore billing is not available (RuStore app missing / signed out / no network?)");
            Toast.Show("Store is not available right now. Make sure RuStore is installed and you are signed in.", true);
            return false;
        }
        RuStoreBillingManager.Instance.PurchaseProduct(ProductIdFor(which));
        return true;
    }

    private static void OnPurchaseProcessed(string productId, bool success)
    {
        if (!TryFromProductId(productId, out PowerUp which))
        {
            Debug.LogWarning($"[PowerUps] unknown product '{productId}' - not in PowerUpStore's mapping, nothing granted");
            return;
        }
        // Fresh purchases and recovered ones (paid while the game was closed) both arrive here; the
        // manager acknowledges right after this returns, which is what lets a consumable be bought again.
        if (success) Grant(which, "purchase");
        PurchaseFinished?.Invoke(which, success);
    }

    private static void OnPricesLoaded() => PricesChanged?.Invoke();

    // ------------------------------------------------------------------ hook-up
    // RuStoreBillingManager lives in the Splash scene and runs its recovery sweep there, so the grant hook
    // must be attached from the first frame - not from the gameplay HUD.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStatics() { attached = null; } // editor domain-reload safety

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("PowerUpStore");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Host>();
    }

    private static void EnsureAttached()
    {
        RuStoreBillingManager m = RuStoreBillingManager.Instance;
        if (m == null || m == attached) return;
        m.OnProductPurchaseProcessed += OnPurchaseProcessed;
        m.OnPricesLoaded += OnPricesLoaded;
        attached = m;
        Debug.Log("[PowerUps] attached to RuStoreBillingManager");
    }

    /// <summary>Attaches as soon as the billing manager exists (and re-attaches if it is ever replaced).</summary>
    private class Host : MonoBehaviour
    {
        private void Awake() => EnsureAttached();
        private void Update() { if (attached != RuStoreBillingManager.Instance) EnsureAttached(); }
    }

#if UNITY_EDITOR
    // ------------------------------------------------------------------ debug + mapping check
    [UnityEditor.MenuItem("Tools/Word Search/Power-Ups/Empty All Charges")]
    private static void EmptyAll() { Set(PowerUp.Hint, 0); Set(PowerUp.Shuffle, 0); Debug.Log("[PowerUps] all charges emptied"); }

    [UnityEditor.MenuItem("Tools/Word Search/Power-Ups/Restore Free Charges")]
    private static void RestoreAll()
    {
        PlayerPrefs.DeleteKey(HintKey); PlayerPrefs.DeleteKey(ShuffleKey); PlayerPrefs.Save();
        Debug.Log($"[PowerUps] counters reset to {FreeUses} free uses");
    }

    [UnityEditor.MenuItem("Tools/Word Search/Power-Ups/Verify Store Mapping")]
    private static void Verify()
    {
        int bad = 0;

        Check(ref bad, HintProductId != ShuffleProductId, "product ids are not distinct");
        Check(ref bad, HintKey != ShuffleKey, "counter keys are not distinct");
        Check(ref bad, RefillAmount > 0, "RefillAmount must be > 0");
        foreach (PowerUp p in Enum.GetValues(typeof(PowerUp)))
        {
            Check(ref bad, TryFromProductId(ProductIdFor(p), out PowerUp back) && back == p, $"{p}: product id does not map back");
            Check(ref bad, CounterKeyFor(p).StartsWith("WS_Power_"), $"{p}: counter key has the wrong prefix");
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RuStoreProductConfig");
        var listed = new System.Collections.Generic.HashSet<string>();
        foreach (string g in guids)
        {
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<RuStoreProductConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            if (cfg == null) continue;
            foreach (var e in cfg.products)
            {
                if (e == null) continue;
                listed.Add(e.productId);
                if (TryFromProductId(e.productId, out _))
                    Check(ref bad, e.type == RuStoreProductConfig.ProductType.Consumable, $"{e.productId} must be Consumable in the product config");
            }
        }
        foreach (PowerUp p in Enum.GetValues(typeof(PowerUp)))
            Check(ref bad, listed.Contains(ProductIdFor(p)), $"{ProductIdFor(p)} is missing from RuStoreProductConfig - its price will never load");

        Debug.Log(bad == 0 ? "[PowerUps] store mapping OK" : $"[PowerUps] {bad} problem(s)");
    }

    private static void Check(ref int bad, bool ok, string what)
    {
        if (ok) return;
        bad++;
        Debug.LogError("[PowerUps] " + what);
    }
#endif
}
