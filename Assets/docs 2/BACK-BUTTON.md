# Android back button

Android's hardware back arrives in Unity as `KeyCode.Escape`. This is a pattern for
routing it to whatever is actually on screen — and, just as importantly, for **swallowing
it** when nothing should happen.

**The rule that matters:** an unhandled back press can quit the app. Silently exiting
from the menu, or dumping the player out to the menu from underneath a modal, are both
worse than ignoring the press. Every handler here therefore consumes the key even when it
does nothing.

---

## Port steps

There is no file to copy — this is a pattern applied in each screen that owns the key.

### 1. Give every dismissible panel a static `IsOpen`

```csharp
public static bool IsOpen
{
    get { return _instance != null && _instance._root != null && _instance._root.activeSelf; }
}
```

This is what lets screens below stand down. Without it they have no way to know something
is layered above them.

### 2. Handle Escape in each screen that owns it

**Gameplay** (`MeowdokuGame.cs:1397`) — mirrors the on-screen Back Button exactly, calling
the same method it is wired to, so the two can never diverge:

```csharp
if (Input.GetKeyDown(KeyCode.Escape))
{
    if (BackKeyBelongsToGameplay()) { PlayButtonSound(); ReturnToMenu(); }
    return;   // swallowed either way
}
```

Placed **before** the input-lock guard, so it still works while a result overlay is up.

**A panel** (`RemoveAdsPanel.cs:278`) — takes the same path as its close button, so it
gets the sound and any cleanup too:

```csharp
void Update()
{
    if (!Input.GetKeyDown(KeyCode.Escape)) return;
    if (_root == null || !_root.activeSelf) return;
    OnCloseClicked();
}
```

Deliberately **not** swallowed when the panel is shut — the screens below own the key
then.

### 3. Write the ownership guard

```csharp
bool BackKeyBelongsToGameplay()
{
    if (_canvas == null || !_canvas.gameObject.activeInHierarchy) return false;
    if (TutorialActive) return false;
    if (SomeModal.IsOpen) return false;
    return true;
}
```

One entry per thing that can cover gameplay. **This list is the whole design** — every
modal you add later must be added here too.

---

## Gotchas

**Back quits the app from screens nobody handled.** The most common bug, and invisible
until a tester complains the game closed. Decide explicitly for every screen; swallow by
default.

**A modal's parent acts underneath it.** If a panel opens over gameplay and gameplay does
not check `IsOpen`, back returns to the menu *behind* the still-visible panel. Guard the
parent, not just the child.

**Update order is undefined.** Two components polling Escape in the same frame can both
act. Do not rely on ordering — make the conditions mutually exclusive, so whichever runs
first, only one can respond.

**A throttled `Update` eats the key.** `DailyChallenge.Update` returns early once a second
for its timer refresh. Escape is polled *before* that guard (`:295`); behind it the button
would respond roughly once per second and feel broken.

**Menu-level back does nothing here.** That is a deliberate choice, not an oversight: the
Android convention of "back exits from the home screen" would silently close a puzzle game
mid-session. If you want it, add it explicitly in the menu — ideally with a confirm.

**`Application.Quit()` is not the fallback you want.** On Android it terminates rather than
backgrounding, losing the session. Prefer swallowing.

---

## How it works in this project

```
Escape pressed
  │
  ├─ RemoveAdsPanel open?      → closes the offer            (RemoveAdsPanel.cs:278)
  ├─ Streak panel open?        → closes the panel            (DailyChallenge.cs:295)
  ├─ Gameplay canvas active?   → returns to the main menu    (MeowdokuGame.cs:1397)
  └─ anything else             → swallowed, nothing happens
```

Mutual exclusion comes from the canvases: the streak panel lives on the menu canvas, so
the gameplay canvas is inactive whenever it is up, and `BackKeyBelongsToGameplay` fails on
its first check. The offer panel *does* sit over gameplay, which is exactly why the guard
has an explicit `RemoveAdsPanel.IsOpen` line.

## Known gap

**`NoInternetPanel.IsOpen` exists but nothing consumes it.** The panel is blocking with no
close button, and `BackKeyBelongsToGameplay` does not check it — so pressing back while it
covers gameplay returns the player to the menu *underneath* a panel they cannot dismiss.
The fix is one line in the guard, and a decision about whether back should dismiss a panel
that deliberately has no dismiss.

---

## Testing

Only a device or an Android emulator produces a real back press; the Editor has no
equivalent. Send one over adb without touching the phone:

```
adb shell input keyevent KEYCODE_BACK
```

Walk every screen and confirm the intended owner responds and nothing else does:

| Screen | Expected |
|---|---|
| Gameplay | Returns to the main menu |
| Gameplay, result overlay up | Same — the handler sits before the input-lock guard |
| Offer panel over gameplay | Closes the panel only; the game stays put |
| Streak panel | Closes the panel |
| Main menu | Nothing at all, and **the app does not close** |
| Tutorial | Nothing |

The last two are the ones worth being strict about — they are where an unhandled key
quietly kills the session.
