# RuStore Billing — drop-in package

Game-agnostic RuStore Pay SDK wiring. Nothing in this folder knows about coins, bundles or ads —
per game you write exactly one method (the grant hook) and fill one asset (the product list).

```
RuStoreBilling/
├── RuStoreBillingManager.cs   purchase + recovery + acknowledge. Don't edit per game.
├── RuStoreProductConfig.cs    ScriptableObject: the product id list.
└── Editor/
    └── RuStoreBillingSetupValidator.cs   Window > RuStoreSDK > Validate Billing Setup
```

## Integration workflow

Same eight steps every time. Step 8 tells you if you got 1–7 right.

1. **Import the RuStore Pay SDK** unitypackage (built against **11.1.0**) → lands in `Assets/RustoreSDK`.
2. **Copy this folder** into `Assets/`.
3. **`Window > RuStoreSDK > Settings > PayClient`** — set `consoleApplicationId` (the numeric id from
   the RuStore Console, *not* the package name) and `deeplinkScheme` (lowercase, unique to this game,
   e.g. `arrowescapescheme`). Press the button that patches `AndroidManifest.xml`.
4. **`Assets > Create > RuStore > Product Config`** — add every product id, spelled exactly as in the
   console. Prices are fetched for these ids and nothing else.
5. **First scene in build settings**: empty GameObject → add `RuStoreBillingManager` → assign the
   config asset. It `DontDestroyOnLoad`s itself, so one instance covers the whole game.
6. **Write the grant hook** (below) in your shop code.
7. **`Assets > External Dependency Manager > Android Resolver > Force Resolve`**.
8. **`Window > RuStoreSDK > Validate Billing Setup`** — everything must read OK. It also prints the
   handful of things only the RuStore Console can confirm.

## The only code you write per game

```csharp
private void OnEnable()
{
    if (RuStoreBillingManager.Instance != null)
    {
        RuStoreBillingManager.Instance.OnProductPurchaseProcessed += OnPurchaseProcessed;
        RuStoreBillingManager.Instance.OnPricesLoaded += RefreshShopUI;
    }
}

private void OnDisable()
{
    if (RuStoreBillingManager.Instance != null)
    {
        RuStoreBillingManager.Instance.OnProductPurchaseProcessed -= OnPurchaseProcessed;
        RuStoreBillingManager.Instance.OnPricesLoaded -= RefreshShopUI;
    }
}

private void OnPurchaseProcessed(string productId, bool success)
{
    if (!success) return;               // failed, or the player cancelled
    switch (productId) { /* grant it */ }
}
```

**The grant must be idempotent.** It is called for fresh purchases *and* for recovered ones —
non-consumables are re-delivered on every launch after a reinstall, which is what restores them.
Setting a flag is fine; `coins += 5000` behind a flag is fine; unconditional `coins += 5000` is not,
for anything the console marks non-consumable.

Buy: `RuStoreBillingManager.Instance.PurchaseProduct(productId)`
Price: `RuStoreBillingManager.Instance.GetPrice(productId)` — `null` until `OnPricesLoaded` fires.
Gate the UI: `IsBillingInitialized`.

`ShopManager.cs` in this project is the reference implementation.

## What the manager already handles

- `ONE_STEP` purchases, plus the `TWO_STEP` fallback RuStore can return anyway (confirmed before
  delivery, so held funds never auto-reverse after 6h).
- Recovery on launch and on every resume: purchases paid for but not delivered (app killed
  mid-payment, deeplink lost) are granted, then acknowledged.
- Non-consumable restore after reinstall, via the same sweep.
- Delivery queueing — the sweep runs in the first scene, the shop usually lives in a later one, and
  nothing is acknowledged before the game has actually granted it.
- Localized prices via `GetProducts`.
- Player-cancelled purchases logged as info, not errors.

## Gotchas that cost the most time

- **None of this works in the editor.** `RuStorePayClient` refuses any non-Android platform. Test on
  a device with the RuStore app installed and signed in, watching
  `adb logcat Unity:V "*:S" | grep "\[RuStore\]"`.
- **Signature.** The build must be signed with the same keystore as the version in the console, or
  every call fails on a signature check. A debug-signed build always fails.
- **Deeplink scheme must be unique per app.** Two of your games sharing a scheme means payments can
  return to the wrong one.
- **Product ids are case- and character-exact** against the console. A typo shows up as a missing
  price, not an error.
- **RuStore-only builds: strip Unity IAP** (`com.unity.purchasing`) so Google Play Billing isn't
  bundled.
