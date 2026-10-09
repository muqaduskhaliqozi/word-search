# ByteBrew events and remote config

Analytics events **and remote config reads** behind a single wrapper and a scripting
define, so the project compiles and runs with or without the SDK imported.

The pattern generalises: swap the `#if BYTEBREW` body for GameAnalytics, Adjust, AppsFlyer
or anything else and every call site stays untouched.

---

## Port steps

### 1. Import the SDK, configure IDs

Import the ByteBrew `.unitypackage`, then fill the settings inspector. Verify it actually
saved by reading `Assets/ByteBrewSDK/Resources/ByteBrewSettings.asset`:

```yaml
androidEnabled: 1        # must be 1
androidGameID: <set>
androidSDKKey: <set>
```

Typing into the panel without saving leaves these blank, and `InitializeByteBrew()` bails
silently on an unconfigured asset.

### 2. Copy the wrapper

```
Assets/.../Scripts/Analytics/GameEvents.cs
```

The **only** file that names the ByteBrew API. Everything else calls plain methods on it.

### 3. Add the scripting define

*Player Settings → Other Settings → Scripting Define Symbols* → `BYTEBREW`, per platform
(Android, iOS — not WebGL unless you ship it).

Without the define the wrapper compiles to no-ops and logs a warning on startup, so a
forgotten define announces itself instead of silently dropping every event.

### 4. Initialize behind the consent gate

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Bootstrap() => ConsentGate.RunWhenAccepted(Initialize);
```

### 5. Add your events

Purrdoku tracks five. Two carry a parameter, three are bare:

| Event | Fires from | Parameter |
|---|---|---|
| `LevelComplete` | win routine, normal levels only | `Level_number` |
| `LevelFail` | game-over | `Level_number` |
| `InterAdsShown` | interstitial **displayed** callback (both ad paths) | — |
| `RewardedAdsShown` | rewarded impression callback | — |
| `BannerAdsShown` | banner impression callback (each unit) | — |

### 6. Optional: remote config

See [Remote config](#remote-config) below. Purrdoku uses it for ad placement —
[AD-GATE.md](AD-GATE.md).

---

## Remote config

Same wrapper, same define. `GameEvents.RemoteConfigInt(key, fallback)` is the only public
surface; callers never see the SDK.

### The fetch needs a delay after init

**This is the one that will cost you an afternoon.** `InitializeByteBrew()` returns before
the native SDK can serve a fetch, and a request made inside that window is dropped in
silence — the callback never fires, and every key keeps handing back its fallback. It looks
exactly like a dashboard that is ignoring you.

```csharp
const float RemoteConfigFetchDelay = 10f;   // measured, not documented

static IEnumerator FetchRemoteConfigs()
{
    for (int attempt = 1; attempt <= 3 && !ByteBrew.HasRemoteConfigsBeenSet(); attempt++)
    {
        yield return new WaitForSecondsRealtime(RemoteConfigFetchDelay);
        ByteBrew.RemoteConfigsUpdated(OnRemoteConfigsLoaded);
    }
}
```

Gating on `IsByteBrewInitialized()` instead does **not** work — that flag flips true well
before a fetch will succeed. It has to be wall-clock time. Retune the constant against a
device, never in the Editor.

Three details that matter:

- **`WaitForSecondsRealtime`**, not `WaitForSeconds`. A fullscreen ad or a pause menu that
  zeroes `Time.timeScale` would otherwise stall the fetch indefinitely.
- **The coroutine host must be `DontDestroyOnLoad`.** The delay usually spans a scene load
  (splash → game). On a normal GameObject the coroutine is cancelled mid-wait and the
  configs never arrive.
- **The clock starts at consent acceptance**, not app launch, because the SDK initialises
  inside `ConsentGate.RunWhenAccepted`.

### You cannot tell a fetched value from a fallback

`GetRemoteConfigForKey(key, default)` returns **the default you passed** when the key does
not exist. So a key configured as `3` and a key that was never created both come back as
`"3"`. There is no error, no null, no distinguishing signal.

`HasRemoteConfigsBeenSet()` is the only thing that separates "the dashboard says 3" from
"nothing was ever fetched". Check it before every read and log the result, or a broken
integration is indistinguishable from a working one — the same reasoning that makes the
wrapper log every event send.

### Parse leniently

Dashboards routinely store `60` as `"60.0"`. Accept an int, then a float you round, then
warn and fall back. Silently reverting to a default over a trailing zero is miserable to
debug.

---

## Gotchas

**Events vanish with no error.** `ByteBrew.NewCustomEvent` returns early when the SDK is
not initialized — no log, no return value, nothing on the dashboard. That makes "never
fired", "dropped before init" and "fired, dashboard is behind" indistinguishable. The
wrapper logs every attempt for exactly this reason; keep that if you swap SDKs.

**It will not compile after importing.** `ByteBrew` lives in namespace `ByteBrewSDK`,
which their docs do not mention. Put `using ByteBrewSDK;` inside the `#if` guard.

**Nothing happens in the Editor.** Both `InitializeByteBrew` and `NewCustomEvent` are
`#if UNITY_EDITOR → return`. Device only. Expect
*"ByteBrew is in Editor Mode, not sending events"*.

Remote config is worse: `GetRemoteConfigForKey` returns the default for **every** key in
the Editor, and `RemoteConfigsUpdated` auto-invokes its callback without fetching. So an
Editor run cannot prove a dashboard value is live no matter what it prints. Note that
events genuinely do reach the dashboard from a device while configs stay stuck on
fallbacks — "events are working" does not imply configs are.

**Ad events belong on callbacks, not requests.** Firing from `ShowInterstitial()` counts
attempts, including ones that never rendered. Use the displayed/impression callbacks.

**Hook both interstitial paths.** If you have a fallback network for low-end devices
(Purrdoku: AdMob below 2 GB RAM), hooking only the primary means your cheapest devices
report zero interstitials. AdMob needs `OnAdFullScreenContentOpened` added — only
`Closed` and `Failed` were wired.

**Banner impressions are high volume.** Banners auto-refresh every 30–60s, so an
impression-triggered event is ~60–120 per user-hour and will dominate your quota. Decide
whether you want "a banner was displayed" (guard to first per session) or every
impression.

---

## How it works

```csharp
#if BYTEBREW
    const bool Enabled = true;
#else
    const bool Enabled = false;
#endif
```

Public methods are always present and always compile; only the SDK call sits inside the
guard. Call sites never need `#if`, so gameplay and ad code stay free of preprocessor
noise and the SDK can be removed without touching them.

---

## Testing

```
adb logcat -s Unity | grep ByteBrew
```

Expect `[ByteBrew] initialized.` then lines like:

```
[ByteBrew] LevelComplete {Level_number=3}
[ByteBrew] InterAdsShown
```

| Log | Means |
|---|---|
| `BYTEBREW define is not set` | Step 3 missing — every event is a no-op |
| `fired before Initialize()` | Consent ordering problem |
| Events logged, dashboard empty | Genuinely just dashboard delay |
| `remote configs have NOT loaded` | Fetch never landed — network, or the delay is too short |
| `came back equal to the fallback` | Key missing or misspelled on the dashboard |
| `remote config inter_ad_start_level = 5` | Working |

The ad events need real fills — no fill, no impression callback, no event. On a fresh app
with no ad history that can look like a broken integration when it is only inventory.
