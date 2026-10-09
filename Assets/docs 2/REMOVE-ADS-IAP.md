# Remove Ads (Unity IAP 5.x)

A non-consumable "remove ads" purchase plus an offer panel showing the localized price.
The panel opens from a button and automatically after every Nth interstitial.

Uses the **current** IAP 5 API — `UnityIAPServices.StoreController` and its order events.
The `IStoreListener` / `UnityPurchasing.Initialize` flow in most tutorials is `[Obsolete]`
in IAP 5 and compiles with warnings.

---

## Port steps

### 1. Package

```json
"com.unity.purchasing": "5.0.4"
```

Then **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
This is not optional — see Gotchas.

### 2. Copy

```
Assets/.../Scripts/IAP/RemoveAdsStore.cs     # store logic, no UI
Assets/.../Scripts/IAP/RemoveAdsPanel.cs     # the offer panel
Assets/.../Scripts/<Editor>/RemoveAdsPanelBuilder.cs
```

`RemoveAdsStore` has no UI dependency and ports as-is apart from the product ID.
`RemoveAdsPanel` builds its UI at runtime from sprite fields — you will re-do its layout
constants for your own art.

### 3. Set the product ID

```csharp
public const string ProductId = "YOUR_PRODUCT_ID";   // Purrdoku: com.dss.purr.doku.removeads
```

Create it in Play Console as a **non-consumable**, Active, ID character-identical.

### 4. Pick the "ads are off" flag

`RemoveAdsStore` writes `PlayerPrefs["RemoveAds"] = 1`. Purrdoku's ad scripts already
checked that key in four places, so granting is one line and there is exactly one
definition of "ads are off". **Point the store at whatever key your ad code already
reads** rather than inventing a new one:

```csharp
const string RemoveAdsKey = "RemoveAds";   // must match your ad scripts
```

### 5. Tear down live ads on purchase

Your ad scripts check the flag before *loading or showing*, which does nothing about a
banner already on screen. Add a teardown and call it from the grant path:

```csharp
public void RemoveAdsPurchased()
{
    HideBanner(); HideBanner2(); /* MRec, etc. */
}
```

### 6. Interstitial counter

Call from wherever an interstitial *closes* — both paths if you have a fallback:

```csharp
RemoveAdsPanel.NotifyInterstitialClosed();
```

Purrdoku: `MediationHandler.OnInterstitialDismissedEvent` (MAX) and
`InterstitialAdManager.OnAdFullScreenContentClosed` (AdMob).

### 7. Tunables

| Constant | Purrdoku | Meaning |
|---|---|---|
| `ShowEveryNthInterstitial` | 5 | Panel cadence |
| `closeDelaySeconds` | 5 | Delay before the X appears |
| `FrameAspect` / `VisibleFraction` | 1351/1070, 0.851 | Panel art metrics |
| `PriceRowOnButton` / `PriceBandFraction` | 0.488, 0.32 | Price label on the button |

---

## Gotchas

**No price on device, app published, product Active.** The Play Billing library is not in
your build. IAP 5.x declares `com.android.billingclient:billing` **programmatically** in
`IAPAndroidDependencies.cs`, not in a `Dependencies.xml` like every other SDK — so it is
absent until an External Dependency Manager resolve. Worse, resolving leaves **no tracked
file change**, so a fresh clone hits it again with nothing in git to explain why. This one
cost hours; check it first.

**It works in the Editor.** The Editor uses Unity IAP's fake store, which fabricates a
price (`$0.01`). Editor success proves your UI renders and nothing else. Device only.

**The purchase gets refunded after three days.** Google requires acknowledgement.
`OnPurchasePending` must call `ConfirmPurchase(order)`. Also confirm pending orders found
at startup, or a purchase interrupted mid-flow is lost.

**Restore does nothing on Android.** It does not need to — Google replays owned purchases
through `OnPurchasesFetched`, so calling `FetchPurchases()` after the product fetch *is*
the restore path. `RestoreTransactions` is for iOS, where it must be user-initiated.

**Player pays and the banner is still there.** Step 5.

**The price collides with the button text.** If your button art has "Buy Now" baked into
the pixels, text drawn on it overlaps. Either get blank art, or paint the text out — for a
flat/vertical-gradient button you can rebuild each affected row from its own horizontal
median and the shading stays continuous. Restrict the repaint to the glyph rows or you
will flatten the bevel too.

**Rewarded ads.** Deliberately *not* gated in Purrdoku: they are opt-in (revive,
power-ups), so removing ads should not take away the player's way of earning them. Decide
explicitly rather than by omission.

---

## How it works

```
Connect() ──► FetchProducts ──► OnProductsFetched ──► price + FetchPurchases()
                                                            │
                            OnPurchasePending ──► Grant() ──┴──► ConfirmPurchase()
                            OnPurchasesFetched ─► Grant()          (restore)
```

`Grant()` is the single choke point for "ads are now off" — it writes the flag, tears
down live ads and raises `OnStateChanged`. Both fresh purchases and restores route
through it, so there is one place to get right.

The panel reads `RemoveAdsStore.PriceWithCurrency` **live** rather than from an event
payload. That matters: products are often fetched during the splash, before the panel
exists to subscribe, and a live read makes the missed event harmless.

---

## Testing

| Menu item | Does |
|---|---|
| `Meowdoku > Remove Ads > Build Panel Into Scene` | Adds the panel, assigns sprites, places the button |
| `Meowdoku > Remove Ads > Reset Purchase + Interstitial Counter` | Editor PlayerPrefs only |
| `Meowdoku > Remove Ads > Grant Without Buying (debug)` | Sets the flag directly |

**The one line that diagnoses everything:**

```
adb logcat -s Unity | grep IAP
```

| Log | Means |
|---|---|
| `store returned 1 product(s)` with a price | Working |
| `store returned 0 product(s)` | Connected; Play does not recognise the ID |
| `store disconnected` | Billing never connected — resolver, signing, or track |
| nothing at all | `Initialize()` never ran — consent not accepted |

Real purchases need an Internal Testing build installed from Play (Play App Signing means
a locally-signed APK has a different signature) and a licence-tested account.
