# Portable features

Features built for Purrdoku that every mobile game tends to need. Each file is a
**porting guide**, not a description of this project — generic steps with placeholders,
with the Purrdoku wiring shown as the worked example.

| Feature | What it gives you |
|---|---|
| [CONSENT-GATE.md](CONSENT-GATE.md) | First-run Terms/Privacy panel that holds every tracking SDK until the player accepts |
| [REMOVE-ADS-IAP.md](REMOVE-ADS-IAP.md) | Unity IAP 5.x non-consumable purchase + an offer panel with the localized price |
| [BYTEBREW-EVENTS.md](BYTEBREW-EVENTS.md) | Analytics events and remote config behind a scripting define, so the project builds with or without the SDK |
| [AD-GATE.md](AD-GATE.md) | Interstitial start level and minimum spacing driven from a remote config dashboard |
| [REWARDED-UNAVAILABLE.md](REWARDED-UNAVAILABLE.md) | Native toast when a rewarded ad cannot play, so the button is never silently dead |
| [SUBSCRIPTION.md](SUBSCRIPTION.md) | Weekly auto-renewing subscription with a daily grant — **spec only, not built yet** |
| [LOCAL-NOTIFICATIONS.md](LOCAL-NOTIFICATIONS.md) | Re-engagement reminders scheduled on-device, no server or FCM |
| [STORE-REVIEW.md](STORE-REVIEW.md) | Native Play / App Store review prompt on milestone levels, replacing the interstitial |
| [NO-INTERNET.md](NO-INTERNET.md) | Blocking connectivity panel with Retry |
| [BACK-BUTTON.md](BACK-BUTTON.md) | Android hardware back handled per screen, without a manager |
| [DISPLAY-SAFE-AREA.md](DISPLAY-SAFE-AREA.md) | Display cutouts, window resize, system font scale — Play's technical quality items |

## Port order

**Do the consent gate first.** The other three register with it — they call
`ConsentGate.RunWhenAccepted(...)` instead of initializing directly, and porting them
first means going back to rewire.

```
ConsentGate ──┬── RemoveAdsStore
              ├── GameEvents (ByteBrew) ── AdGate
              ├── GameNotifications
              └── your ad SDKs

StoreReview   ── independent, but shares the post-level ad slot with RemoveAds and AdGate
NoInternet    ── fully independent
BackButton    ── fully independent
SafeArea      ── fully independent, but touches every canvas the others create
```

After that they are independent. Overlaps worth knowing:

- **Remove-Ads and ByteBrew both touch the ad callbacks.** If you are porting both, do
  them together and edit each callback once.
- **Remove-Ads and Notifications both hook the level-win path.** Same advice.
- **Store Review, Remove-Ads and the Ad Gate all contend for the post-level interstitial.**
  Review replaces the ad on milestone levels; the offer panel appears after every Nth
  interstitial; the gate decides whether an ad is allowed at all. Port them together so the
  ordering is decided once, in one method — and keep the review check *outside* the gate,
  or an ad cooldown silently swallows a review milestone.
- **The Ad Gate needs remote config, which is in the ByteBrew guide.** It is one method
  (`RemoteConfigInt`); any config backend works.
- **Safe area touches every canvas the other features build.** Each one that creates its
  own canvas — consent gate, no-internet, offer panel — needs its card inset and its
  backdrop left full-bleed. Cheapest done as you port each panel, not in one pass at the
  end.

If you only want one feature, none of them require the others *technically* — but
without the consent gate you must replace every `ConsentGate.RunWhenAccepted(X)` with a
direct call to `X`, and decide for yourself when tracking is allowed to start.

## What these files assume

- Unity 2022.3+, Android as the primary target
- An existing ad setup you are gating (Purrdoku uses AppLovin MAX with an AdMob
  fallback); the guides name the integration points rather than assuming your class names
- External Dependency Manager present — it ships with most ad SDKs

## Reading them

Placeholders are in caps (`YOUR_PRODUCT_ID`), with the Purrdoku value beside them.
Line numbers are hints and will drift; the method names are the reliable anchor.

Project-specific docs live at the repo root: [../README.md](../README.md),
[../CLAUDE.md](../CLAUDE.md), [../BUILD.md](../BUILD.md), [../LEVELS.md](../LEVELS.md).
