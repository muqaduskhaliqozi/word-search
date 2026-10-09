# No Internet panel

A blocking panel that watches connectivity and takes over the screen whenever it drops,
anywhere in the game. Retry re-checks; the panel also dismisses itself when the signal
returns on its own.

**Read the trade-off first.** If your game is playable offline — Purrdoku's puzzles are —
a blocking, undismissable panel locks players out on a plane or underground for a game
that needs no network. That is a real one-star driver. The alternative is to show it only
when a network action (rewarded ad, purchase) actually needs a connection.

---

## Port steps

### 1. Copy

```
Assets/.../Scripts/Connectivity/NoInternetPanel.cs
Assets/.../Scripts/<Editor>/NoInternetPanelBuilder.cs
```

### 2. Art

Two sprites, both with their text baked in — so the panel builds **no Text elements** and
nothing here is localisable in code. If you need translation, the art has to change.

| Slice | Purrdoku | What |
|---|---|---|
| `_0` | 1324×1465 | Whole card: banner ("No internet"), cat, divider |
| `_1` | 565×208 | Green Retry button |

### 3. Place it

`Meowdoku > No Internet > Build Panel Into Scene` assigns both sprites. Put it in the
**first scene** (Splash) if you want cover from launch — it is `DontDestroyOnLoad`, so
one instance covers the whole session. A scene instance is required: without the art the
panel would be a white box, so it does not self-spawn.

### 4. Tunables

| Field | Purrdoku | Meaning |
|---|---|---|
| `pollSeconds` | 1 | Interval between checks |
| `panelWidth` | 900 | In a 1080-wide reference; everything scales off it |
| `backdropColor` | `0x0A3E8C` | Opaque, raycast-blocking |
| `RetryGapFraction` | 0.35 | Card-to-button gap, as a fraction of button height |
| `CardBottomFraction` | 0.961 | Where the card's opaque area ends inside the sprite |

---

## Gotchas

**`Application.internetReachability` does not mean the internet works.** It reports
whether a network *interface* exists. Consequences, both real:

- **Turning off wifi on a dev machine often does not trigger it** — a VPN, a Docker
  bridge, or an interface still up without a route keeps it reporting reachable. This is
  why the Simulate Offline toggle exists, and why "I disabled wifi and nothing happened"
  is not evidence of a bug.
- **Captive portals defeat it on device.** Hotel and café wifi report reachable while
  nothing resolves, so the panel stays hidden exactly when the player needs it. A
  trustworthy check needs a real request to a known endpoint with a short timeout.

**A blocking panel on an unreliable signal is the dangerous combination.** If you watch
continuously and cannot be dismissed, one false "offline" reading locks out a player who
is online. Either make the signal trustworthy or make the panel dismissible.

**It must hide itself, not only on Retry.** Otherwise the player sits on a dead panel
after reconnecting, tapping a button to be told what the game already knew.

**Sorting order matters.** Purrdoku uses **31000** — above gameplay and the Remove Ads
offer (30000), deliberately *below* the consent gate (32000). Put it above the consent
gate and a first launch with no signal deadlocks: you cannot accept terms you cannot tap.

**A silent Retry reads as broken.** When still offline the tap must do something visible;
Purrdoku shakes the card.

**Editor-only flags must survive the domain reload.** A plain `static` simulation flag
resets when you enter Play mode, so it can only be set *during* Play — useless for
testing "offline at launch". Back it with `EditorPrefs`.

---

## How it works

A poll, not an event — Unity offers no connectivity callback:

```csharp
if (Online) { if (IsOpen) Hide(); return; }
if (!IsOpen) Show();
```

One branch handles both directions, so recovery cannot be forgotten. Transitions are
logged, which is what makes a stuck "reachable" reading visible instead of looking like
dead code.

The card and button are positioned as a **centred pair** using the card's opaque bottom
edge, not the sprite's, because the art carries shadow padding below the visible card.

---

## Testing

| Menu item | Does |
|---|---|
| `Meowdoku > No Internet > Build Panel Into Scene` | Adds the panel, assigns sprites |
| `Meowdoku > No Internet > Simulate Offline (toggle)` | Forces offline; ticked when active, survives entering Play |
| `Meowdoku > No Internet > Log Reachability` | Prints what Unity actually thinks |

**Editor:** toggle Simulate Offline, press Play — the panel appears on launch. Toggle it
off while running and it dismisses itself, which exercises the auto-recovery path. This
drives the same code path as the real check; only the signal is faked, so UI and
placement verify honestly here.

**Device:**

```
adb logcat -s Unity | grep NoInternet
```

Shows every reachability transition. Test in aeroplane mode, and separately on a captive
portal if you have one — that is the case the Editor cannot reproduce and the default
check does not catch.
