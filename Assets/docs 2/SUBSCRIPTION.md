# Weekly subscription — daily power-ups

> **Not built yet.** This is the specification and the store homework, written
> before the UI design lands. Everything under *Design* is a decision already
> made; everything under *Port steps* is what implementation will do. Update this
> file as it gets built — do not assume the code matches it yet.

A weekly auto-renewing subscription that grants a daily bundle of power-ups and
removes ads while active. Sits alongside the existing one-off Remove Ads
purchase ([REMOVE-ADS-IAP.md](REMOVE-ADS-IAP.md)) rather than replacing it.

---

## Design

| | |
|---|---|
| Product | Weekly, auto-renewing, **no free trial** |
| Grant | 3 auto-cats + 3 hints + 3 undos, once per calendar day |
| Offer appears | 3 days after install |
| Ads | Removed while the subscription is active, restored when it lapses |

**Missed days do not accumulate.** One grant per calendar day, no back-pay — a
player returning after a week gets one bundle, not seven.

---

## Part 1 — Store setup and policy

Play rejects subscription screens over *disclosure* far more often than over
code, so this half matters as much as the implementation.

### Play Console

1. **Monetize → Subscriptions → Create subscription.** Match your existing IAP
   naming — Purrdoku uses `com.dss.purr.doku.removeads`, so
   `com.dss.purr.doku.subscription.weekly`.
2. **One base plan**, period **weekly**, **auto-renewing**.
3. **Create no offer.** Offers are where free trials and introductory pricing
   live. No offer means the card is charged immediately at the full price.
4. Set regional prices, then **activate both the subscription and the base plan**.
   An inactive base plan still resolves as a product but cannot be purchased —
   which looks like a code bug and is not one.
5. Publish to at least a **closed testing track** and add the tester account, or
   the product will not resolve on device at all.

### What the offer screen must say

Before the purchase button, visible without scrolling or tapping:

- **Price and billing period** — "$X.XX per week"
- That it **renews automatically** until cancelled
- **How to cancel**, and cancelling must not be harder than subscribing
- **What is included**

Also required:

- **No trial language.** With no offer configured, any "free", "try", "trial" or
  "$0 today" wording is a false claim.
- **A manage/cancel route** from inside the app:
  `https://play.google.com/store/account/subscriptions?sku=<PRODUCT_ID>&package=<PACKAGE>`
- **A restore path.** Reinstalling must restore an active subscription with no
  second charge.
- **A visible dismiss control.** Subscribing must not be the only way off the
  screen.

### Do not

Sell or advertise it outside Play Billing, imply the price is one-off, or show
the offer to someone who already subscribes.

---

## Part 2 — Port steps

### 1. Reuse what exists

| What | Where |
|---|---|
| Power-up grant | `MeowdokuGame.GrantPowerUps(int, int, int)` — public, already drives the weekly streak reward |
| Day arithmetic | `DailyChallenge` — `Epoch` (2024-01-01) + `TodayNumber`. **Currently private**; make it public or lift it into a shared helper |
| IAP shape | `IAP/RemoveAdsStore.cs` — Unity IAP 5.x *new* API (`StoreController`, `ProductDefinition`, `Orders`). Do **not** copy legacy `IStoreListener` samples off the web |
| Offer panel | `IAP/RemoveAdsPanel.cs` — runtime-built, price pulled from the store |
| Consent gating | `ConsentGate.RunWhenAccepted` — mandatory, IAP must not start before Accept |
| Display safety | `SafeArea.Wrap`, backdrop-outside / card-inside ([DISPLAY-SAFE-AREA.md](DISPLAY-SAFE-AREA.md)) |

### 2. `SubscriptionStore.cs`

Modelled on `RemoveAdsStore`, differing in three ways:

- `new ProductDefinition(ProductId, ProductType.Subscription)`
- **Entitlement is re-checked every launch**, never trusted from a stored bool.
  Unity IAP 5.0.4 gives you `EntitlementStatus` (`Unknown`, `NotEntitled`,
  `EntitledUntilConsumed`, `EntitledButNotFinished`, `FullyEntitled`) plus
  `SubscriptionInfo` — `IsSubscribed()`, `IsExpired()`, `IsCancelled()`,
  `IsAutoRenewing()`, `GetRemainingTime()`.
- Exposes `IsActive`, a status-changed event, and the localized price.

### 3. `SubscriptionBenefits.cs`

The install-day record and the daily grant. Keep the date logic pure so it can be
checked without a store:

- `Meowdoku_InstallDay` — written on first run.
- `Meowdoku_SubGrantDay` — last day number granted, so a relaunch cannot
  double-grant.
- On launch and on resume: if subscribed and `SubGrantDay < TodayNumber`, call
  `GrantPowerUps(3, 3, 3)` and stamp the day.

### 4. Wire ads to a computed entitlement

See the gotcha below — this is the step to get right.

### 5. Trigger the offer

`TodayNumber - InstallDay >= 3`, not already subscribed, and only at a natural
break — result panel or menu, never mid-level. Rate-limit it the way
`RemoveAdsPanel` limits itself.

---

## Gotchas

**Do not write the subscription into the `RemoveAds` key.** It is the obvious
one-line approach and it is a data-loss bug. That key is read in four places,
three of them raw:

```
AD Scripts/MRecAdManager.cs:28          PlayerPrefs.GetInt("RemoveAds", 0)
AD Scripts/InterstitialAdManager.cs:79  PlayerPrefs.GetInt("RemoveAds", 0)
AD Scripts/MediationHandler.cs:770      IsRemoveAds()
IAP/RemoveAdsStore.cs:33                Purchased
```

When the subscription lapses you would clear that key — wiping the entitlement of
anyone who *also* bought Remove Ads outright, permanently, with no way to detect
it happened. Add one computed source of truth instead and point the readers at
it:

```csharp
public static bool AdsRemoved => Purchased || SubscriptionStore.IsActive;
```

This is also the only shape that lets ads come *back* correctly on a lapse.

**Client-side entitlement is spoofable and cannot see a refund.** PlayerPrefs is
editable on a rooted device, and a chargeback or a Play-side cancellation is
invisible until the app next reaches the store. The real fix is server-side
validation via Real-Time Developer Notifications. Without it, pick a deliberate
policy rather than discovering one: **re-check every launch, and if the store is
unreachable keep the last known state for a grace window (48h) instead of
revoking.** Revoking an offline paying subscriber earns a 1-star review; granting
9 power-ups to an offline lapsed one costs nothing.

**Grace period and account hold are "active".** Play keeps a subscription alive
while a renewal payment is failing. Treat `EntitledButNotFinished` with an
unexpired `SubscriptionInfo` as active, or paying users lose access over a bank
hiccup.

**There is no install date in this project.** Nothing records first launch, so
"day 3" needs a new key — and existing players have no record. If they should be
eligible immediately rather than waiting three days from the update, seed the
install day from evidence already stored (saved level progress,
`Meowdoku_StreakDay`). `DailyChallenge.LastStreakDay` already does exactly this
kind of legacy seeding and is the pattern to copy.

**The Editor proves nothing.** Unity's fake store fabricates both a price and an
entitlement, the same trap Remove Ads has. And Unity IAP 5.x self-declares its
billing dependency in code rather than a `Dependencies.xml`, so a fresh clone
silently gets no price until *Android Resolver → Force Resolve*.

**Two paid offers can collide.** `AdGate`, `StoreReview` and `RemoveAdsPanel`
already contend for the post-level slot. Decide the ordering in one method rather
than letting each feature decide independently.

**A daily notification is a separate change.** "Your power-ups are ready" fits
naturally, but `GameNotifications.BuildSchedule` enforces ≥3h spacing and a
6-hourly idle ladder, and a fixed-time daily reminder can silently displace
ladder slots. Add it separately and run
*Meowdoku > Notifications > Verify Schedule* afterwards.

---

## Testing

| Step | Where | Proves |
|---|---|---|
| Grant fires once, second launch same day does not re-grant | Editor | Date logic |
| Offer appears day 3, not day 2 (backdate `Meowdoku_InstallDay`) | Editor | Trigger |
| Subscribe on a licence-tester account | Device | Ads stop, 9 power-ups arrive, copy matches Console |
| Reinstall | Device | Restores with no second charge |
| Cancel and let it expire | Device | Ads return, grants stop, **a separately purchased Remove Ads survives** |

**The lapse test is the one that matters.** It is the only check that catches the
`RemoveAds` key bug, and the code looks correct right up until you run it. Do not
skip it because the diff looks obviously right.

Bump `AndroidBundleVersionCode` before uploading to the testing track.
