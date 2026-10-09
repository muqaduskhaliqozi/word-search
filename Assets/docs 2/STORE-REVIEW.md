# Store review prompt

Native store review — Google Play In-App Review on Android, `SKStoreReviewController`
on iOS — offered after chosen milestone levels **instead of** the usual interstitial.

**It does not** guarantee a dialog appears. Both stores rate-limit hard and neither
reports whether anything rendered. Treat it as a request, never an event.

---

## Port steps

### 1. Android package

Import Google's **Play Review** plugin (`com.google.play.review`, via their scoped
registry or the `.unitypackage`). It lands at `Assets/GooglePlayPlugins/` and brings
`com.google.play.common` with it.

Then add `PLAY_REVIEW` to *Player Settings → Other Settings → Scripting Define Symbols*
for **Android only**. Without it the Android path compiles to a no-op and the project
still builds.

### 2. iOS needs nothing

`UnityEngine.iOS.Device.RequestStoreReview()` is built into Unity's CoreModule. No
plugin, no define.

### 3. Copy

```
Assets/.../Scripts/Review/StoreReview.cs
Assets/.../Scripts/<Editor>/StoreReviewBuilder.cs
```

### 4. Hook the post-level ad

Find the single place your game shows an interstitial after a level and ask first:

```csharp
bool reviewTookTheSlot = afterWin && !isChallengeOrBonus
                         && StoreReview.TryRequestInsteadOfAd(levelNumber);

if (!reviewTookTheSlot)
    ShowInterstitial();
```

Purrdoku: `MeowdokuGame.ResultButtonClicked`, which serves both *Next Level* and
*Try Again* — hence the `afterWin` flag, passed `true` only from the two win paths.

### 5. Tunables

| Field | Purrdoku | Meaning |
|---|---|---|
| `reviewAfterLevels` | `{ 5, 15, 40 }` | 1-based levels that trigger a prompt |
| `delaySeconds` | 0.4 | Wait after the win panel settles |

`Meowdoku > Store Review > Add To Scene` gives an inspectable instance. The component
self-spawns with defaults when absent, so it works without one.

---

## Gotchas

**Never ask after a loss.** The obvious hook is "the result panel button", but that
serves both win and lose. A review request on a failure screen is the fastest way to
collect one-star ratings. Purrdoku passes an explicit `afterWin` flag rather than
inferring it.

**Nothing happens in the Editor.** Google's plugin stubs itself — `LaunchReviewFlow`
returns success immediately with no dialog. Same trap as Unity IAP's fake store: Editor
success proves the wiring, not the feature.

**Success does not mean shown.** `LaunchReviewFlow` completing with `NoError` means the
*flow* finished. Play may have shown nothing at all, and there is no API that tells you.
Do not treat the absence of a dialog on your test device as a broken integration — that
is the single most likely thing to send you debugging working code.

**Quota makes "replace the ad" a gamble.** Play allows roughly one prompt per user per
month. If you skip the interstitial every time you request a review, quota-blocked
attempts leave the player seeing *neither* — a donated impression for nothing. Purrdoku
skips the ad only on the first genuine attempt (`Meowdoku_ReviewAdSkipUsed`); later
milestones still request a review but let the ad through.

**Replays would re-trigger a milestone.** Levels can be replayed, so the check must be
"have we asked at this milestone" rather than "is this level a milestone".
`Meowdoku_ReviewLastLevel` holds the highest one already asked.

**`ReviewInfo` expires.** Request and launch back to back rather than pre-warming at
startup.

---

## How it works

```
level win ──► TryRequestInsteadOfAd(level)
                 │  not a milestone / already asked / mid-flight  ──► false ──► show ad
                 │
                 └─ milestone ──► start request coroutine
                                  first attempt ever?  ──► true  ──► ad skipped
                                  otherwise           ──► false ──► ad shows too
```

The method is deliberately *synchronous and predictive*: it returns whether the ad
should be skipped before the native flow has run, because the caller has to decide
immediately. It cannot wait for a result that never arrives.

---

## Testing

| Menu item | Does |
|---|---|
| `Meowdoku > Store Review > Add To Scene` | Adds an inspectable instance |
| `Meowdoku > Store Review > Reset Review State` | Clears both keys so milestones fire again |

Reset clears Editor PlayerPrefs only. On device, reinstall — and remember the OS
rate-limits independently of anything you clear.

```
adb logcat -s Unity | grep Review
```

Shows the milestone decision and whether the flow completed. It will never tell you a
dialog appeared, because the platform does not say.
