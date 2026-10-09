# Consent gate

A first-launch Welcome panel with Terms of Service / Privacy Policy links and an Accept
button. Nothing that tracks the user starts until Accept is tapped; on every later launch
the panel never appears and startup is unchanged.

**It does not** give you a GDPR-valid consent flow — there is one Accept button and no
decline path. That is fine for a Terms acknowledgement. Real GDPR consent needs a genuine
reject option and per-purpose choices.

---

## Port steps

### 1. Copy

```
Assets/.../Scripts/ConsentGate.cs
Assets/.../Scripts/<Editor folder>/ConsentGateBuilder.cs
```

`ConsentGate` is in the global namespace and uses `Level.FromHex` plus `SpriteFactory`
from Purrdoku for its placeholder look. In a new project either copy those helpers or
replace the colour constants with literals and the sprites with your own.

### 2. Gate every tracking SDK

This is the whole port. Find each place an ad/analytics/attribution SDK initializes and
wrap it:

```csharp
// before
InitializeMaxSdk();

// after
ConsentGate.RunWhenAccepted(InitializeMaxSdk);
```

`RunWhenAccepted` runs the action immediately if consent already exists, otherwise queues
it until Accept. Every initializer goes through it so none can forget the pending case.

Purrdoku's hooks:

| File | Method | What was gated |
|---|---|---|
| `AD Scripts/MediationHandler.cs` | `Start()` | MAX → SolarEngine → Firebase chain |
| `AD Scripts/InterstitialAdManager.cs` | `Awake()` | `MobileAds.Initialize` |
| `Scripts/IAP/RemoveAdsStore.cs` | `Initialize()` | Unity IAP connect |
| `Scripts/Analytics/GameEvents.cs` | `Bootstrap()` | `ByteBrew.InitializeByteBrew` |
| `Scripts/Notifications/GameNotifications.cs` | — | not gated; local only, no tracking |

### 3. Hold the splash

If you have a splash that auto-advances, it must wait — otherwise the game loads and
starts playing behind the dialog. `SplashScene.cs`:

```csharp
while (!ConsentGate.Accepted)
    yield return null;
```

Purrdoku holds the **whole** splash (progress bar and logo animation), not just the scene
load, because the SDKs only begin at Accept — running the timer underneath would spend
the delay before initialization had even started.

### 4. Set the URLs

`termsUrl` and `privacyUrl` on the component. They ship as `https://example.com/...`
placeholders and log an error every time they are tapped until you change them.

### 5. Display safety

The panel builds its own canvas, so it needs its own cutout inset — see
[DISPLAY-SAFE-AREA.md](DISPLAY-SAFE-AREA.md). The split matters:

- **Backdrop stays on the canvas**, full-bleed, so the dim reaches the physical screen
  edge. Inset, it leaves a bright band at the notch.
- **Card goes inside a `SafeArea` node.** It is centred and would clear a cutout on any
  current phone, but it is the first screen every player sees and must never be the one
  that gets clipped.

Text also goes through `SystemFont.Size(size)` so the panel honours the system font
preference. That has a consequence — see the link-row gotcha below.

### 6. Optional: scene instance

`Meowdoku > Consent Gate > Add To Scene` puts an inspectable instance in the splash scene
so art, copy and URLs can be edited without touching code. The gate self-spawns if none
exists, so this is convenience, not a requirement.

---

## Gotchas

**Ads still initialize on first launch.** Your AdMob manager probably calls
`MobileAds.Initialize` in `Awake()` — and `Awake` runs *before* `Start`. Gating only the
mediation handler leaves AdMob running before the player has agreed to anything. Grep for
every `Initialize` across all ad scripts, not just the obvious one.

**The panel appears but the game is playing behind it.** The splash advanced on its
timer. See step 3.

**Your configured panel gets replaced by the placeholder.** The gate must spawn
`AfterSceneLoad`, not `BeforeSceneLoad` — a scene-authored instance has already run its
`Awake` by then, so the bootstrap sees it and stands down. Spawning earlier creates the
placeholder first and the configured one becomes the duplicate that destroys itself.

**Link buttons do nothing.** Empty URL strings make `OpenURL` a no-op. Ship real
`https` defaults, not empty strings, and log loudly — a tap that silently does nothing is
indistinguishable from a broken button.

**Attribution numbers look light.** Gating delays install attribution until Accept, which
on a first launch is exactly when it matters. That is the correct privacy trade, but know
you made it.

**"Terms of Service and Privacy Policy" renders on top of itself on some devices.** The
link row is a `HorizontalLayoutGroup`; if it is pinned to a fixed width and the content
needs more, the layout group squeezes the children while the `Text` components — set to
`HorizontalWrapMode.Overflow` — keep drawing at full width. The words overlap.

Three things push it over: a longer translation, a different font fallback, or the system
font scale from step 5. Do not size links by character count (`label.Length * 22f` assumes
every glyph is the same width, which is wrong per string, per font and per size). Let the
layout group measure the real text, add a `ContentSizeFitter`, and scale the row down as a
unit if it still overruns the card:

```csharp
hl.childControlWidth = true;                      // measure real glyph widths
fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
LayoutRebuilder.ForceRebuildLayoutImmediate(rowRT);
if (rowRT.rect.width > usableWidth)
    rowRT.localScale = Vector3.one * (usableWidth / rowRT.rect.width);
```

The body line and the Accept button label are fixed-width too — single lines, so they spill
past the card rather than merging, but the same class of bug.

---

## How it works

A static queue, not an event:

```csharp
static Action _waiting;

public static void RunWhenAccepted(Action action)
{
    if (Accepted) { action(); return; }
    _waiting += action;
}
```

`Accept()` records `Meowdoku_ConsentAccepted = 1`, invokes the queue once, clears it, and
destroys the panel. Because it is a queue rather than an event, callers that register
before *or* after the gate exists both work — registration order and component lifecycle
stop mattering.

A `BeforeSceneLoad` hook clears the queue so an Editor domain reload cannot leave stale
subscribers from the previous play session.

---

## Testing

| Menu item | Does |
|---|---|
| `Meowdoku > Consent Gate > Reset Consent (replay first launch)` | Clears the flag — next Play replays first launch |
| `Meowdoku > Consent Gate > Grant Consent (skip the panel)` | Skips the panel, behaves like a returning player |
| `Meowdoku > Consent Gate > Add To Scene` | Adds/selects an inspectable instance |

**Editor:** Reset Consent, Play from the splash. Panel appears, splash holds, no SDK logs
in the Console. Tap Accept → SDK logs appear and the game scene loads.

**Device:** `adb logcat -s Unity` and confirm no ad/analytics SDK logs precede the Accept
tap. That is the only check that actually proves the gate works — the Editor may not
initialize some SDKs at all.

PlayerPrefs key: `Meowdoku_ConsentAccepted`. On device it can only be cleared by
reinstalling.
