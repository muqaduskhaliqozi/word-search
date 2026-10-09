# Rewarded "not available" feedback

A native Android toast when a rewarded ad cannot play, so a revive or refill button never
just sits there doing nothing.

The interesting part is not the toast — it is the audit. A rewarded call has more failure
paths than most people wire up, and every one of them tends to `return` in silence.

---

## Port steps

### 1. Copy the toast helper

`ShowToast` in `Assets/.../Scripts/AD Scripts/MediationHandler.cs`. It has no dependencies
beyond `UnityEngine`, so lift it into its own static if you want it project-wide.

### 2. Audit every path out of your show method

This is the actual work. Purrdoku's rewarded call had **four** ways to do nothing, three
inside the show method and one before it:

| Path | Why it happens | Was |
|---|---|---|
| Low-RAM device | Mediation SDK deliberately never initialised there | `return` |
| No connectivity | Body wrapped in `if (reachable)`, so it fell through | nothing |
| Ad not loaded | No fill, or still caching after launch | logged only |
| No mediation instance | Ad stack absent from the scene | `return` in the *caller* |

The fourth is the one that gets missed, because it never reaches the method you are
editing. Grep for it:

```bash
grep -rn "ShowRewardedVideo\|MediationHandler.Instance == null" Assets/YourScripts
```

### 3. Report from each

Prefer early returns that all read the same way over a wrapping `if`:

```csharp
if (_isLowRamDevice)                { ShowToast(Unavailable); return; }
if (!Reachable)                     { ShowToast(Unavailable); return; }
if (!IsRewardedAdReady())           { ShowToast(Unavailable); LoadRewardedVideo(); return; }
```

The "not loaded" case should also kick off a load, so the *next* tap has a chance of
working. A toast with no reload means the button is broken until something else happens to
trigger a cache.

### 4. Do not toast where the reward is granted anyway

Purrdoku's revive button grants the revive directly when there is no mediation instance, so
the player gets their hearts. A "video not available" toast on top of a granted reward is
worse than silence. Toast the paths that do nothing, not the paths that do something else.

---

## Gotchas

**`runOnUiThread` is asynchronous, so the activity must outlive your method.** The obvious
`using` block disposes the `AndroidJavaObject` when the block exits — which can be *before*
the runnable executes, crashing on the Java side. Cache the activity in a static and do not
dispose it:

```csharp
static AndroidJavaObject _unityActivity;   // never disposed, on purpose
```

This is the opposite of the advice for synchronous JNI calls, where `using` is correct.

**Box the message as a `java.lang.String`.** `Toast.makeText` has two overloads —
`(Context, CharSequence, int)` and `(Context, int resId, int)`. A bare C# string can bind
to the resource-id form, which reads your text as an integer resource and throws
`Resources$NotFoundException`:

```csharp
using (var text = new AndroidJavaObject("java.lang.String", message))
    toastClass.CallStatic<AndroidJavaObject>("makeText", activity, text, 0);
```

**Wrap both layers in try/catch.** The outer call and the runnable body each need one. A
cosmetic message must never be able to take down an ad path — and a JNI exception thrown
inside a UI-thread runnable will not surface anywhere you would think to look.

**One message for every cause.** "No fill", "offline" and "your device is unsupported" are
the same event to a player, and naming the cause invites them to try to fix something they
cannot. Purrdoku uses a single
`"Video not available right now. Please try again."`

**Low-RAM devices may never have rewarded ads at all.** If you skip your mediation SDK
below a memory threshold — Purrdoku skips MAX under 2 GB — then rewarded is permanently
unavailable there, not temporarily. A toast is honest, but consider hiding the button
instead so those players are not offered something that can never work.

**Nothing shows in the Editor.** There is no Android runtime, so the helper falls back to
`Debug.Log("[Toast] …")`. The message text is testable in the Editor; the toast itself is
device-only.

**A toast is fire-and-forget.** It cannot be styled, positioned reliably across OEM skins,
or guaranteed visible if the app is backgrounding. It is right for "that didn't work"; it
is not right for anything the player must act on. If the message matters, use in-game UI.

---

## How it works

```csharp
activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
{
    using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
    using (var text = new AndroidJavaObject("java.lang.String", message))
    {
        var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, text, 0);
        toast.Call("show");
    }
}));
```

`0` is `Toast.LENGTH_SHORT`, `1` is `LENGTH_LONG`. Android's own constants are not readable
through JNI without another class lookup, so the literal with a named local is clearer than
it looks.

---

## Testing

Each case has to be forced — they do not occur on demand:

| Case | How to force |
|---|---|
| No connectivity | Airplane mode, then tap |
| Ad not loaded | Tap within a second or two of launch, before the cache fills |
| Low-RAM | Change the threshold constant to something above your test device's RAM |
| No instance | Play the game scene directly, without the scene that owns the ad stack |

```
adb logcat -s Unity | grep -E "Rewarded|Toast"
```

Expect a matching `AdLog` line beside every toast — `Rewarded skipped — no connectivity`
and so on — so the logs tell you *which* path fired, while the player only ever sees the
one neutral message.
