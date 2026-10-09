# Display safety

Keeping a UGUI game inside display cutouts, alive through window resizes, and honouring
the system font size — the four things Google Play's technical quality guidance asks for
that a Unity canvas does not give you for free.

Most of the guidance maps to settings you probably already have right. This covers the
four that need code.

---

## Port steps

### 1. Copy

```
Assets/.../Scripts/Meowdoku/SafeArea.cs
Assets/.../Scripts/Meowdoku/SystemFont.cs
Assets/.../Scripts/Meowdoku/<Editor folder>/SafeAreaCheck.cs
```

`SafeArea` is a MonoBehaviour plus a static `Wrap` helper. `SystemFont` is a static.
Neither depends on anything else in Purrdoku, so both port as-is.

### 2. Player Settings

| Setting | Value | Why |
|---|---|---|
| *Render outside safe area* | **on** | Lets your background reach the physical screen edge |
| *Supported Aspect Ratio* | **Native** | A capped ratio letterboxes 20:9 phones, which is most of the install base |
| *Optimized Frame Pacing* | **on** | This is the Android Frame Pacing API (Swappy). Serialized as `androidUseSwappy` |

Verify the aspect ratio actually changed by reading `ProjectSettings/ProjectSettings.asset`:

```yaml
androidSupportedAspectRatio: 2   # 1 = Legacy, 2 = Native, 3 = Custom
androidRenderOutsideSafeArea: 1
androidUseSwappy: 1
```

### 3. Inset each canvas

Two shapes, depending on whether the UI is generated or hand-authored.

**Generated** — create the node yourself and parent the UI to it:

```csharp
var bg = NewImage("Background", canvas.transform, null, Cream);   // stays full-bleed
Anchored(bg.rectTransform, 0f, 0f, 1f, 1f);

_safeRoot = NewRT("SafeArea", canvas.transform);
Anchored(_safeRoot, 0f, 0f, 1f, 1f);
_safeRoot.gameObject.AddComponent<SafeArea>().Changed += RelayoutBoard;
```

**Hand-authored** — `Wrap` inserts the node and moves the existing children into it, so
the scene keeps its structure and no GUIDs move:

```csharp
var safe = SafeArea.Wrap(canvas.transform, "Background", "AmbientPaws");
```

The named layers are left where they are. `Wrap` is idempotent, so it is safe to call on
every level load.

### 4. Measure layout against the safe rect, not the canvas

The single highest-leverage line. Anything that reads the canvas rect to compute a size
should read the safe node's rect instead — everything downstream inherits the insets with
no further edits:

```csharp
// before
Rect layoutRect = ((RectTransform)_canvas.transform).rect;
// after
Rect layoutRect = Root.rect;    // Root => _safeRoot, falling back to the canvas
```

### 5. Handle resize

Anchored children follow the node on their own. Anything sized by a *computed number*
does not — subscribe it:

```csharp
safe.Changed -= RelayoutBoard;   // unsubscribe first, see gotchas
safe.Changed += RelayoutBoard;
```

### 6. Apply the system font scale

Route every runtime-created label through it:

```csharp
t.fontSize = SystemFont.Size(size);
```

Purrdoku has four text factories (`MeowdokuGame`, `ConsentGate`, `DailyChallenge`,
`RemoveAdsPanel`). Find all of yours — `grep -rn "\.fontSize"` — or the ones you miss stay
fixed while the rest grow, which looks worse than not scaling at all.

---

## Gotchas

**"Render outside safe area" does not inset anything.** It only says where the frame *may*
be drawn. With it on and no `Screen.safeArea` reader, your HUD sits under the notch. With
it off you get letterboxing. You want it on **plus** this component.

**The Editor cannot show you the bug.** Game view always reports a full-screen
`Screen.safeArea`, so a broken implementation looks perfect in Play mode. That is why
`SafeAreaCheck.cs` exists — it runs the pure anchor math against real device geometry,
including a landscape notch, which is the case that catches an x/y mix-up. A phone with a
cutout is the only real test.

**A "keep this out of the safe area" list is not a "keep this behind" list.** Backgrounds
belong behind the UI; modal panels belong *in front* of it. If you insert the safe node at
the end of the child list, every kept layer ends up behind it — and a modal that was
drawing on top of the menu suddenly has the whole menu drawn on top of *it*. `Wrap`
inserts the node at the position the moved children came from, which preserves the
authored order on both sides.

**Full-screen dims must stay outside.** A modal's backdrop has to reach the screen edge;
inset, it leaves a bright band at the notch. The pattern throughout Purrdoku is *backdrop
outside the safe node, card inside* — consent gate, no-internet panel, streak panel. A
centred card clears a cutout anyway, so when insetting the content would cost you the
backdrop, skip the whole overlay.

**Check which UI path actually runs before you believe it works.** Purrdoku has a
generated canvas and a hand-authored one, and the first version of this feature wired only
the generated path — which was dead code on the live build. The check that produced that
mistake was grepping the scene for the component's *class name*; Unity scenes reference
scripts by **GUID**, so that grep can never match. Do this instead:

```bash
G=$(grep -m1 "guid:" YourComponent.cs.meta | awk '{print $2}')
grep -c "$G" YourScene.unity
```

**`Changed` handlers stack.** If you subscribe where the UI is built and that runs per
level, you get one handler per level and a single resize fires all of them. Unsubscribe
first, every time.

**A zero-sized screen produces NaN anchors.** Unity reports `Screen.width == 0` for a frame
or two while an app is restored from the background. Dividing by it sets the anchors to
NaN and the UI blanks permanently — there is no later frame that repairs a NaN. Guard the
divide and skip the frame.

**targetSdk 36 makes resize mandatory.** Android 16 ignores `resizeableActivity` on large
screens, so a fold, a split-screen drag or a rotation resizes your game whether it opted in
or not. If you bumped the target SDK you inherited this whether you wanted it or not.

**Fixed-width text plus font scale equals overlapping text.** A `HorizontalLayoutGroup`
pinned to a fixed width squeezes its children when the content no longer fits, and `Text`
components set to `HorizontalWrapMode.Overflow` keep drawing at full width inside those
narrower slots — so the words render on top of each other. Once you scale fonts by the
system preference, every fixed-width row becomes a candidate. Use a `ContentSizeFitter`
and scale the row down as a unit if it overruns.

**Never reference another `static readonly` from a different partial-class file.** Static
initializers run in declaration order *within* a file, but the order of files is a compiler
detail. `static readonly Color X = Y;` where `Y` lives in another partial can silently
initialise to `default` — transparent black for a `Color`. Repeat the literal.

**Clamp the font scale.** Android's largest setting is 2.0, which overflows any layout
built from fixed-size chips and buttons. Text clipped out of its own button is worse for
the player than text that did not grow. Purrdoku clamps to 1.3.

**48 dp touch targets may be geometrically impossible.** For an *n×n* grid filling the
screen width, cell size is roughly `screenWidth × 0.8 / n`. A 10×10 grid needs a 595 dp
board to reach 48 dp cells; a typical phone is 411 dp wide. Do not burn a day tuning
margins — measure the ceiling first, take what is available, and rely on tap tolerance
(routing presses in the gaps to the nearest cell) for the rest.

---

## How it works

The component sets its own anchors from `Screen.safeArea` as fractions of the screen, so
every anchored child re-lays-out against the inset rect automatically:

```csharp
min = new Vector2(area.xMin / screenWidth, area.yMin / screenHeight);
max = new Vector2(area.xMax / screenWidth, area.yMax / screenHeight);
```

It re-applies whenever the safe area *or* the resolution changes, comparing against the
last applied values — one `Rect` comparison per frame. That same change detection is what
raises `Changed`, so cutout handling and resize handling are one mechanism, not two.

`SystemFont` reads Android's `fontScale` over JNI once and caches it:

```
currentActivity → getResources → getConfiguration → fontScale
```

UGUI has no equivalent of `sp`; without this, a player who turns system text up sees no
change anywhere in your game.

---

## Testing

| Check | Where | Proves |
|---|---|---|
| `Meowdoku > Verify Safe Area Math` | Editor | The anchor math, including a landscape notch |
| 20:9 and 22:9 Game view profiles | Editor | No letterboxing, no clipped HUD |
| Notched device | Device | The actual insets — nothing else does |
| Split-screen drag mid-level | Device | Relayout, and that taps still hit the right cell after |
| System font size → Large | Device | Text grows, nothing overflows its container |

**The tap-accuracy check after a resize is the one people skip.** If your relayout moves
the visuals but leaves a cached cell size behind, everything looks correct and presses land
on the wrong element — a failure that is invisible in a screenshot.
