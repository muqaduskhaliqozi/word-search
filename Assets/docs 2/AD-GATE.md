# Remote interstitial gating

Ad *placement policy* — when the first interstitial appears, and the minimum gap between
two of them — driven from a remote config dashboard instead of hardcoded, so it can be
retuned without shipping a build.

Purrdoku reads it from ByteBrew, but the gate does not name any SDK. Point
`RemoteConfigInt` at Firebase Remote Config, GameAnalytics or your own endpoint and
nothing else changes.

---

## Port steps

### 1. Copy

```
Assets/.../Scripts/AD Scripts/AdGate.cs
```

Depends only on `PlayerPrefs` and on one method in your analytics wrapper (step 2).

### 2. Add an int getter to your analytics wrapper

`AdGate` calls exactly one thing:

```csharp
GameEvents.RemoteConfigInt(key, fallback)
```

See [BYTEBREW-EVENTS.md](BYTEBREW-EVENTS.md#remote-config) for the ByteBrew
implementation. Whatever backs it, the contract is: **return the fallback rather than
throwing or blocking, ever.** An ad decision must never wait on the network.

### 3. Replace your hardcoded trigger

```csharp
// before — a hardcoded level threshold
if (_levelIndex > 0)
    MediationHandler.Instance.ShowInterstitial();

// after
if (AdGate.AllowInterstitial(completedLevel))
    MediationHandler.Instance.ShowInterstitial();
```

Call it at the **decision point**, not at startup. Caching the values once at launch
freezes whatever was available then — usually the fallbacks — for the whole session.

### 4. Stamp the cooldown on dismiss, in every ad path

```csharp
AdGate.NotifyInterstitialClosed();
```

Goes in the ad **hidden/dismissed** callback. If you have a fallback network for low-end
devices, it needs the same line — Purrdoku hooks both `MediationHandler`
(`OnInterstitialDismissedEvent`, MAX) and `InterstitialAdManager`
(`OnAdFullScreenContentClosed`, AdMob).

Look for an existing "interstitial closed" hook before adding a new one; most projects
already have one for a remove-ads counter, and that is the correct place.

### 5. Create the dashboard keys

| Key | Default | Meaning |
|---|---|---|
| `inter_ad_start_level` | 3 | First level number whose completion may show an interstitial |
| `inter_ad_delay_seconds` | 60 | Minimum seconds between one ad closing and the next opening |

Set `inter_ad_delay_seconds` to 0 to turn the cooldown off entirely.

---

## Gotchas

**Reset on dismiss, never on show.** The gap that matters to a player runs from the end of
one ad to the start of the next. Stamping when the ad *opens* lets a 30-second ad eat half
of a 60-second cooldown, and the player experiences half the spacing you configured.

**Persist the timestamp.** Held in memory only, force-quitting after an ad hands the
player another one on the very next level, and relaunching becomes a reliable way to skip
the wait. Purrdoku stores UTC ticks in `Meowdoku_LastInterstitialUtc`.

**Handle a clock that moves backwards.** Travel, a manual change, or a device that boots
with a bad clock before NTP corrects it leaves a stored timestamp in the future — and a
naive `now - last` locks ads out for as long as the skew lasts. Treat a future stamp as "no
previous ad" and clear it.

**Guard the timestamp parse.** A corrupted PlayerPrefs value passed to `new DateTime(ticks)`
throws, and it throws inside your result-panel button handler — taking the button with it.
Range-check before constructing.

**A returned value and a fallback are the same bytes.** Most remote config SDKs return
*the default you passed* for a key they do not have. So `inter_ad_start_level` coming back
as `3` is identical whether the dashboard says 3 or the key was never created. Log the
difference, or you cannot tell a working integration from a broken one. See the ByteBrew
guide for the `HasRemoteConfigsBeenSet` check that separates them.

**Keep review prompts outside the gate.** If a store-review prompt replaces the ad slot on
milestone levels, and you nest that check inside the ad gate, an ad cooldown silently
swallows the review milestone too. Check the review first, gate only the ad:

```csharp
bool reviewTookTheSlot = afterWin && StoreReview.TryRequestInsteadOfAd(completedLevel);
if (!reviewTookTheSlot && AdGate.AllowInterstitial(completedLevel))
    ShowInterstitial();
```

**Early sessions get the defaults.** Remote config arrives seconds after init (see the
ByteBrew guide's fetch delay). A player who triggers an ad inside that window gets your
compiled-in defaults. That is the correct behaviour — it is also why the defaults should be
the values you actually want, not placeholders.

**Offline players run on defaults forever.** No fetch, no values. Another reason the
defaults matter.

---

## How it works

Two independent rules, cheapest first:

```csharp
if (levelNumber < startLevel) return false;          // not far enough in
if (delaySeconds <= 0) return true;                  // cooldown disabled
if (!TryReadLastClosed(out lastClosed)) return true; // no ad yet this install
if (lastClosed > now) { clear; return true; }        // clock moved backwards
return (now - lastClosed).TotalSeconds >= delaySeconds;
```

Placement policy deliberately lives outside the mediation handler. Which SDK serves the ad
changes rarely; *when* an ad is allowed changes constantly. Mixing the two is how "why did
an ad show there?" stops being answerable.

---

## Testing

```
adb logcat -s Unity | grep AdGate
```

| Log | Means |
|---|---|
| `level 2 < start level 3 — no interstitial` | Start-level rule working |
| `only 14s since the last interstitial closed (need 60s)` | Cooldown working |
| No `AdGate` lines at all | `AllowInterstitial` is not being reached — check the call site |

**The Editor cannot verify the remote half.** ByteBrew (and most SDKs) short-circuit
`GetRemoteConfigForKey` in the Editor and return the default for every key, so an Editor
run exercises the gate logic but proves nothing about the dashboard. Device build only.

To test the cooldown without waiting, set `inter_ad_delay_seconds` low on the dashboard, or
clear `Meowdoku_LastInterstitialUtc` to simulate a fresh install.
