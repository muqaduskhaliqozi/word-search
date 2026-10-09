# Local notifications

Re-engagement reminders scheduled on-device. No server, no FCM, no push infrastructure —
every notification is computed from state the game already keeps in `PlayerPrefs`.

Two kinds: an **idle ladder** at a fixed interval since the last session, and
**contextual** reminders tied to game state (a streak about to lapse, an unclaimed
reward).

---

## Port steps

### 1. Package + settings

```json
"com.unity.mobile.notifications": "2.4.3"
```

*Project Settings → Mobile Notifications*:

| Setting | Value | Why |
|---|---|---|
| Android → Reschedule on Device Restart | **ON** | A reboot silently drops every pending alarm otherwise |
| Android → Schedule Exact Alarms | **OFF** | Play restricts exact alarms to alarm/calendar apps and rejects games |
| iOS → Request Authorization on App Launch | **OFF** | Ask at a better moment (below) |

### 2. Notification icons

Add a **Small** and **Large** icon with IDs matching the constants in code
(`purrdoku_small` / `purrdoku_large`).

The small icon must be a **white-on-transparent silhouette**. Android tints the alpha
channel and discards colour — a normal launcher icon renders as a featureless grey square.
Requirements: square, ≥48px, **Read/Write enabled** in the texture importer, ideally
uncompressed. The package rejects icons without Read/Write.

### 3. Copy

```
Assets/.../Scripts/Notifications/GameNotifications.cs
Assets/.../Scripts/Notifications/Editor/NotificationDebugMenu.cs
```

Self-spawning via `[RuntimeInitializeOnLoadMethod]` — no scene wiring.

### 4. Rewrite `BuildSchedule`

The one method you must adapt. Keep it **pure** — no Unity calls, `DateTime` in, list
out — because that is what makes it verifiable without a device.

Replace Purrdoku's contextual reminders (daily challenge, streak, weekly gift) with your
own game's hooks. The idle ladder is generic and ports unchanged.

### 5. Ask for permission

```csharp
GameNotifications.RequestPermissionIfNeeded();
```

Purrdoku calls this from the first level win — Android 13+ offers the prompt only a
couple of times per install, so spend it where the player has just seen the game deliver,
not on a cold launch.

### 6. Tunables

| Constant | Purrdoku | Meaning |
|---|---|---|
| `IdleIntervalHours` | 6 | Nudge cadence since last session |
| `LadderSlots` | 12 | How far it runs (12 × 6h = 3 days) |
| `MinSpacingHours` | 3 | Reminders closer than this read as spam |
| `MaxScheduled` | 60 | iOS silently discards past 64 pending |

---

## Gotchas

**No permission dialog appears.** `POST_NOTIFICATIONS` is **Android 13+ (API 33)** only.
On Android 12 and below it is granted at install and no dialog exists — that is correct
OS behaviour, not a bug. Nor is there a dialog in the Editor. Check
`adb shell getprop ro.build.version.sdk` before debugging anything else.

**Keep no local "already asked" flag.** An obvious design — and a trap. Any run that
records the attempt without producing a dialog leaves the app permanently convinced it
asked, with no way to clear it on device short of uninstalling. The OS already tracks the
real state; `PermissionRequest` no-ops when granted and only prompts when needed.

**Notifications stop after a reboot.** Reschedule on Device Restart, step 1.

**Grey square in the status bar.** Small icon is not an alpha-only silhouette, step 2.

**Shrinking the interval for testing shows only one notification.** `MinSpacingHours`
also has to shrink, or the spacing rule discards every slot after the first. Change all
three tunables together, then put them back.

**Contextual reminders eat the cadence.** If a contextual reminder lands within
`MinSpacingHours` of a ladder slot and you drop one of them, you can lose a notification
*inside* the guaranteed window to gain one outside it. Purrdoku lays the ladder first and
has contextual reminders **replace a slot's copy** instead of displacing it — the count
holds and the better message wins.

**Quiet hours vs a guaranteed cadence — pick one.** Clamping fire times into daytime
collapses several slots onto one morning, and the spacing rule then throws them away, so
"away 12h means two notifications" quietly stops being true. Purrdoku chose wall-clock
with no quiet hours, so nudges do land overnight. Deliberate; document whichever you pick
so nobody "fixes" it.

---

## How it works

**Cancel-all-and-reschedule-every-session.**

```csharp
void OnApplicationPause(bool paused)
{
    if (paused) { CancelAll(); ScheduleAll(DateTime.Now); }
    else CancelAll();
}
```

Backgrounding wipes everything pending and lays down a fresh ladder from that moment;
foregrounding wipes it again. That is what makes "nudge every N hours since last play,
cancel if they return" fall out without repeat-interval alarms, and it guarantees a
returning player never sees a stale nudge.

`OnApplicationPause(true)` is the reliable Android signal; `OnApplicationQuit` is
belt-and-braces because Android often skips it.

Full-screen ads background the app, so this also runs on every interstitial — a cancel +
reschedule of the whole ladder each time. Correct but chatty; it happens while the app is
already backgrounded, so it costs no frames.

---

## Testing

| Menu item | Does |
|---|---|
| `Meowdoku > Notifications > Verify Schedule` | Sweeps 24 hours × 5 game states and asserts the invariants |
| `Meowdoku > Notifications > Dump Schedule` | Prints actual fire times |
| `Meowdoku > Notifications > Permission Notes` | The Android-version rules |
| `Meowdoku > Notifications > Fire Test Notification (+10s)` | Device only |

**Verify Schedule is the real check** — it asserts every reminder is in the future, at
least `MinSpacingHours` apart, at least two within the guaranteed window, the ladder on
exact interval multiples at every hour of the day, and the count under the iOS cap.
Run it after touching `BuildSchedule`.

**Device:**

```
adb shell dumpsys alarm | grep <your.package>
adb logcat -s Unity | grep Notifications
```

1. Shrink all three tunables (see Gotchas) so you are not waiting hours
2. Background the app → `dumpsys` lists the pending alarms
3. Wait → notification arrives with **your icon**, not a grey square
4. Foreground, background again → `dumpsys` shows *later* fire times. This proves the
   cancel-and-reschedule loop, which is the whole feature
5. Reboot with notifications pending → they survive
6. Restore the tunables
