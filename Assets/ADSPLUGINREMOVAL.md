# Going ad-free: stripping the ad stack and selling the power-ups instead

How Purrdoku was taken from an ad-monetized Google Play build to an ad-free
RuStore build, and how to do the same to another Unity project.

Two things happen here, and they have to happen in this order:

1. **Remove every ad, mediation and analytics SDK** — AppLovin MAX, Google
   Mobile Ads, Firebase, AppsFlyer, ByteBrew — along with the scripts, scene
   components, manifest entries, Gradle dependencies and scripting defines that
   only existed to serve them.
2. **Replace the rewarded-video placements with an IAP.** Each power-up keeps
   its three free trials; when they run out, the button opens a buy panel
   instead of an ad.

Do them in that order and the compiler tells you exactly which gameplay paths
were tied to ads — every one becomes a build error you must answer. Do it the
other way round and the dead ad code quietly keeps compiling around you.

> **Read this before you start:** a full pass touches ~740 files and deletes
> ~56,000 lines. Do it on a branch, in one sitting, with the Editor closed for
> the bulk deletes. Reopening Unity halfway through a partial delete produces a
> wave of "missing script" dialogs that are much harder to read than a compiler
> error list.

---

## Contents

- [Part 0 — Survey before you delete](#part-0--survey-before-you-delete)
- [Part 1 — Remove the SDKs](#part-1--remove-the-sdks)
- [Part 2 — Refactor rewarded ads into an IAP](#part-2--refactor-rewarded-ads-into-an-iap)
- [Part 3 — Verify](#part-3--verify)
- [What was done to this project](#what-was-done-to-this-project)
- [Store setup checklist](#store-setup-checklist)
- [Traps](#traps)

---

## Part 0 — Survey before you delete

Deleting an SDK folder is easy. Finding the six places that quietly depended on
it is the work. Do this first, and keep the output — it is your checklist.

### 0.1 List every file that names an ad or analytics type

```bash
grep -rln "MediationHandler\|MaxSdk\|GoogleMobileAds\|Firebase\|AppsFlyer\|ByteBrew\|SolarEngine\|AdGate\|Interstitial\|Rewarded\|AppOpenAd" --include="*.cs" Assets/ | grep -v "^Assets/\(MaxSdk\|GoogleMobileAds\|Firebase\|AppsFlyer\|ByteBrewSDK\)/"
```

Excluding the vendor folders themselves is the point — what is left is *your*
code, and every one of those files needs a decision.

Sort the results into three piles:

| Pile | Meaning | Action |
|---|---|---|
| **Pure ad code** | The file exists only to show ads | Delete the file |
| **Ad call site** | Gameplay that calls into ad code | Rewrite the call |
| **Innocent bystander** | A utility that happens to live in an ad file | **Move it out before deleting** |

That third pile is the one that bites. In Purrdoku the native Android toast
helper lived inside `MediationHandler` and was used by the daily-challenge card
to explain a locked button — nothing to do with ads. Deleting the file would
have taken it out. It moved to `Scripts/Toast.cs` first.

### 0.2 Find the scene and prefab references

C# references break loudly. Scene references break silently — a `MonoBehaviour`
whose script GUID no longer resolves just shows up as "Missing (Mono Script)"
and the GameObject keeps existing.

Get the GUID of every script you are about to delete, then search the scenes:

```bash
for f in $(find Assets/YourAdScripts -name "*.cs"); do
  echo "$(grep -m1 '^guid:' "$f.meta" | awk '{print $2}')  $f"
done
```

```bash
grep -rn "<guid>" Assets/**/*.unity Assets/**/*.prefab
```

Write down the anchor id of each hit (`--- !u!114 &529862146`). You will need
them in step 1.4.

### 0.3 Note what the SDKs were feeding

Ad SDKs tend to smuggle in non-ad services. Before deleting, list what else was
reading them:

- **Remote config** — anything reading gameplay tunables from a dashboard loses
  its source. In Purrdoku the daily-challenge unlock level was a ByteBrew key;
  it went back to being a compile-time constant.
- **Attribution / install referrer** — usually nothing in gameplay reads it.
- **Crash reporting** — if Firebase Crashlytics was your only crash reporting,
  removing it leaves you blind. Decide consciously.
- **Consent gate** — likely gates the SDKs you are deleting. It does not
  necessarily go with them (see [trap 4](#4-dont-delete-the-consent-gate-by-reflex)).

---

## Part 1 — Remove the SDKs

Close the Unity Editor for 1.1–1.3.

### 1.1 Rescue the innocent bystanders

Move anything from pile three into its own file **first**, so the delete in the
next step cannot take it with it. Keep the original doc comment and add a line
saying where it came from — the next reader will wonder why a toast helper is
sitting at the root of `Scripts/`.

### 1.2 Delete your own ad scripts

```bash
git rm -r "Assets/YourProject/Scripts/AD Scripts" "Assets/YourProject/Scripts/AD Scripts.meta"
git rm -r "Assets/YourProject/Scripts/Analytics" "Assets/YourProject/Scripts/Analytics.meta"
```

Delete the `.meta` alongside every file and folder. A `.meta` with no asset is
harmless but noisy; an asset with no `.meta` gets a **new GUID** on the next
import, which breaks every scene reference to it.

### 1.3 Delete the vendored SDKs and their native payloads

Three separate places, and it is the second and third that get forgotten.

**The managed folders:**

```bash
git rm -r Assets/MaxSdk Assets/MaxSdk.meta \
          Assets/GoogleMobileAds Assets/GoogleMobileAds.meta \
          Assets/Firebase Assets/Firebase.meta \
          Assets/AppsFlyer Assets/AppsFlyer.meta \
          Assets/ByteBrewSDK Assets/ByteBrewSDK.meta \
          Assets/GeneratedLocalRepo Assets/GeneratedLocalRepo.meta \
          Assets/google-services.json Assets/google-services.json.meta
```

**The native plugin payloads under `Assets/Plugins/`** — `.aar` files,
`.androidlib` folders, iOS `.a` archives and headers. These are copied in by the
resolver and do **not** live in the SDK folder:

```bash
git rm -r Assets/Plugins/Android/googlemobileads-unity.aar \
          Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib \
          Assets/Plugins/Android/FirebaseApp.androidlib \
          Assets/Plugins/iOS Assets/Plugins/tvOS   # (plus their .meta files)
```

Check what remains — `Assets/Plugins/Android/` should be down to your Gradle
templates and manifest:

```bash
find Assets/Plugins -not -name "*.meta"
```

**The editor-only stragglers.** SDKs drop console icons, desktop configs and
settings assets in places that are not the SDK folder:

```bash
git rm -r "Assets/Editor Default Resources/Firebase"          # console icons
git rm Assets/StreamingAssets/google-services-desktop.json    # desktop config
```

Finish by checking for `.meta` files whose asset you just deleted, and for
folders you emptied — an orphaned `.meta` is noise, but an emptied folder whose
`.meta` survives comes back as a missing-folder warning on the next import:

```bash
python3 -c "
import os
print([os.path.join(r,f) for r,d,fs in os.walk('Assets') for f in fs
       if f.endswith('.meta') and not os.path.exists(os.path.join(r,f[:-5]))])"
```

**The resolver's own caches** in `ProjectSettings/`:

```bash
git rm ProjectSettings/AndroidResolverDependencies.xml \
       ProjectSettings/AppLovinInternalSettings.json
```

`AndroidResolverDependencies.xml` is a generated record of the last resolve. It
regenerates from scratch on the next *Force Resolve*; leaving a stale one in the
repo means the file claims you still depend on AppLovin.

> **Do not delete the External Dependency Manager** (`Assets/ExternalDependencyManager`,
> `Assets/External Dependency Manager`). It arrived with the ad SDKs, but Unity
> IAP needs it — see [trap 1](#1-unity-iap-needs-the-external-dependency-manager).

### 1.4 Clean the scenes

Now the anchors from step 0.2. A scene is YAML; each component is a document
with an anchor, and deleting one means **also** removing every reference to that
anchor — the parent `m_Component:` list, the parent `m_Children:` list, and any
field pointing at it. A dangling `{fileID: N}` is a broken reference, not a null
one.

Three shapes come up:

| What you found | What to remove |
|---|---|
| A GameObject that only hosted ad components | The GameObject, its Transform, all its components, its entry in the parent's `m_Children` |
| An ad component sharing a GameObject with something you keep | Just that `MonoBehaviour` document, and its line in the host's `m_Component` list |
| A `PrefabInstance` whose source prefab you deleted | The whole `PrefabInstance` document |

In Purrdoku's `Splash.unity` all three appeared, and the second one mattered:
the `MediationHandler` and `InterstitialAdManager` components shared their
GameObject with `SplashScene`, the script that runs the splash sequence. Deleting
the GameObject would have deleted the splash.

You can do this by hand for a handful of components. Past that, script it — and
whatever you use, finish by checking that **no GUID referenced by a scene points
at a file you deleted**:

```bash
# every guid the scenes reference, against every guid still present in Assets/
python3 - <<'PY'
import io, os, re
metas = set()
for root, _, files in os.walk("Assets"):
    for f in files:
        if f.endswith(".meta"):
            for line in io.open(os.path.join(root, f), encoding="utf-8", errors="ignore"):
                if line.startswith("guid: "):
                    metas.add(line.split()[1]); break
for root, _, files in os.walk("Assets"):
    for f in files:
        if not f.endswith((".unity", ".prefab", ".asset")): continue
        p = os.path.join(root, f)
        s = io.open(p, encoding="utf-8", errors="ignore").read()
        # package/built-in guids live outside Assets/, so compare against what
        # you actually deleted rather than flagging every miss
        for g in set(re.findall(r"guid: ([0-9a-f]{32})", s)) - metas:
            print("check:", p, g)
PY
```

### 1.5 Clean the Android manifest and Gradle templates

**`Assets/Plugins/Android/AndroidManifest.xml`** — remove the advertising ID
permission and any ad-SDK `<meta-data>` / `<activity>` / `<provider>` blocks:

```diff
-  <uses-permission android:name="com.google.android.gms.permission.AD_ID" />
```

This one matters beyond tidiness: RuStore, Google Play and the App Store all ask
whether your app collects an advertising identifier, and the permission in the
manifest is what the automated check reads. An ad-free build that still declares
`AD_ID` gets flagged.

**`Assets/Plugins/Android/settingsTemplate.gradle`** — the resolver writes a
*second* block here, listing a maven repository per SDK. It is easy to miss
because it is in a different file from the dependencies, and one entry is
actively dangerous:

```gradle
maven {
    url (unityProjectPath + "/Assets/GeneratedLocalRepo/Firebase/m2repository")
}
```

That is a `file://` repository pointing into `Assets/`. Delete the SDK folder
without deleting this line and Gradle is configured to resolve from a directory
that no longer exists. Cut every `maven {}` whose comment names a deleted
`Dependencies.xml`; `google()` and `mavenCentral()` above the block already
cover anything you kept.

**`Assets/Plugins/Android/mainTemplate.gradle`** — the resolver writes its
dependencies between two marker comments, and each line carries the
`Dependencies.xml` that produced it. Since you deleted those files, keep only
the lines whose comment names a package you still have:

```gradle
// Android Resolver Dependencies Start
    implementation 'com.google.android.play:core-common:2.0.4'  // GooglePlayPlugins/...
    implementation 'com.google.android.play:review:2.0.2'       // GooglePlayPlugins/...
// Android Resolver Dependencies End
```

Also check `gradleTemplate.properties` and `baseProjectTemplate.gradle` for
SDK-specific flags. `android.useAndroidX` / `android.enableJetifier` usually
stay — billing and Play Review need AndroidX too.

### 1.6 Clear the scripting defines

*Player Settings > Other Settings > Scripting Define Symbols*, **per platform** —
Android and iOS have separate lists and the second one is always the one that is
forgotten. Or edit `ProjectSettings/ProjectSettings.asset`:

```yaml
  scriptingDefineSymbols:
    Android: PLAY_REVIEW      # was: BYTEBREW;PLAY_REVIEW
    iPhone:                   # was: BYTEBREW
```

A define left set after its SDK is gone is worse than useless: code guarded by
`#if BYTEBREW` will try to compile against types that no longer exist.

### 1.7 Answer the compiler

Reopen Unity. Every remaining tie to the ad stack is now a build error. Work
through them; the interesting ones are the gameplay paths, which are Part 2.

The mechanical ones:

- **Analytics calls** — delete the call. If the surrounding comment explained
  the funnel it fed, delete that too.
- **Remote config reads** — replace with the constant that was the fallback, and
  say in the doc comment that the value is now compile-time.
- **Interstitial triggers** — delete. Watch for logic that was *sharing* the slot
  with the ad, such as a review prompt; that logic survives, and the boolean
  gymnastics around it usually simplify.
- **Comments naming deleted classes** — `grep` for the class names one more time
  at the end. A comment that describes machinery nobody can find is a trap for
  the next reader.

---

## Part 2 — Refactor rewarded ads into an IAP

### The model

Before: the power-up has N free uses, then a rewarded video refills it.
After: the power-up has N free uses, then a purchase refills it.

The free trials do not change, and that is the point — the free tier stays
exactly as generous as it was, and only the *refill* is priced.

```
tap power-up button
      │
      ├── charges > 0 ──▶ spend one, do the thing
      │
      └── charges == 0 ──▶ open the offer panel
                                  │
                                  └── Buy ──▶ store sheet ──▶ purchase clears
                                                                    │
                                              PlayerPrefs counter += N
                                                                    │
                                              event ──▶ HUD badges refresh
                                                    └──▶ panel closes
```

### 2.1 Grant to PlayerPrefs, not to a callback

The single most important decision, and the one that differs from the ad flow.

A rewarded ad pays out in seconds, to a callback closed over the caller's local
state. A purchase does not. It can be interrupted, and a payment that clears
while your app is closed is **replayed at the next launch**, through
`OnPurchasesFetched`, long before any gameplay object exists.

So the grant writes to persistent storage directly:

```csharp
static void Grant(PowerUp which, string why)
{
    string key = CounterKeyFor(which);          // "Meowdoku_Hints"
    int now = PlayerPrefs.GetInt(key, 0) + RefillAmount;
    PlayerPrefs.SetInt(key, now);
    PlayerPrefs.Save();

    var handler = OnChargesGranted;             // let live UI catch up
    if (handler != null) handler(which, now);
}
```

The event is a courtesy for open UI, not the delivery mechanism. If nothing is
listening, the charges are still there next time the counters are read.

The old shape — `ShowRewardedVideo(() => _hintsLeft = 3)` — cannot survive this,
because `_hintsLeft` belongs to an object that may not exist when the purchase
lands. Replace the closure with a plain enum argument:

```csharp
// before
if (_hintsLeft <= 0) { OfferPowerUpRefill(refilled => _hintsLeft = refilled); return; }

// after
if (_hintsLeft <= 0) { OfferPowerUpRefill(PowerUpStore.PowerUp.Hint); return; }
```

### 2.2 Products must be consumables, and must be confirmed

A remove-ads purchase is a non-consumable, bought once. A power-up refill is a
**consumable** — bought repeatedly.

Two consequences:

1. **Declare the right type.** `new ProductDefinition(id, ProductType.Consumable)`.
2. **Confirm every purchase.** `ConfirmPurchase(order)` is what releases a
   consumable to be bought again. Skip it and the *first* purchase works while
   every later one fails silently — and after three days the store refunds it
   automatically as unacknowledged.

Ignore already-confirmed orders on startup: for a consumable, a confirmed order
was granted and spent long ago, and re-granting it hands out free charges on
every launch.

```csharp
static void OnPurchasesFetched(Orders orders)
{
    // Paid for but never acknowledged — grant, then confirm.
    // ConfirmedOrders are deliberately not replayed: consumables are spent.
    foreach (var order in orders.PendingOrders)
    {
        string id = ProductIn(order);
        if (id == null) continue;
        Grant(FromProductId(id), "pending order completed");
        _controller.ConfirmPurchase(order);
    }
}
```

### 2.3 Reuse the panel you already have

If the project had a remove-ads offer panel, that is your buy panel. Repurposing
it beats writing a new one: the art is already sliced and assigned, the entrance
animation is tuned, the close button already handles the Android back key, and
the result *looks like the game* for free.

**Rename the file and its `.meta` together** and the scene keeps working — Unity
matches a `MonoBehaviour` by the `.meta` GUID, so the component survives the
rename as long as the class name matches the new file name:

```bash
git mv Scripts/IAP/RemoveAdsPanel.cs      Scripts/IAP/PowerUpOfferPanel.cs
git mv Scripts/IAP/RemoveAdsPanel.cs.meta Scripts/IAP/PowerUpOfferPanel.cs.meta
```

Then make it about a subject instead of a fixed message:

- Add a field for **which** power-up is being offered, set by `Show(which)`.
- Swap the fixed title for one looked up per power-up.
- Point the icon at the power-up's own button art, read from your UI config, so
  the panel shows exactly the icon the player just tapped rather than a second
  drawing of it.
- Keep the layout constants. They were measured against the frame art; the new
  content sits in the slots the old content had.

Serialized fields you drop (`adsBubbleSprite`, `titleText`, `menuButton`) are
ignored on the next scene save. If you are editing the scene YAML anyway, remove
those lines and add the new fields' defaults so the committed scene is honest.

**Shorten the close delay.** An ad offer that appears unprompted can justify
holding the X back for five seconds. This panel opens *in response to the
player's own tap*, so it has to get out of the way quickly when the answer is
no. 1.5s.

Whatever the delay, keep it on **unscaled** time. If anything ever parks
`Time.timeScale` at zero, a scaled wait never completes and the panel becomes a
trap with no way out.

### 2.4 Show a price even when there is no price

The store's localized price is the button's label, and it is not available on
the first frame — sometimes not at all, in the Editor's fake store or on a
device with no billing. Never present a blank button:

```csharp
public const string FallbackPrice = "$1.99";

public static string PriceFor(PowerUp which)
{
    string price;
    if (_prices.TryGetValue(ProductIdFor(which), out price) && !string.IsNullOrEmpty(price))
        return price;               // real, localized, in the player's currency
    return FallbackPrice;
}
```

Refresh the label from the store's state-changed event too, so a price that
arrives while the panel is already open replaces the fallback in place.

### 2.5 Change what the empty badge says

The count badge said `AD` at zero, because tapping played an ad. It should now
say something that means "get more" — `+` works in every language you ship and
fits the badge without a localization entry.

Keep the button **interactable** at zero. It is not disabled; it does something
different.

### 2.6 Decide the non-power-up rewarded slots explicitly

Most projects have a rewarded placement that is not a power-up — a revive, a
continue, a daily bonus. It does not automatically become a purchase.

Purrdoku's revive is now **free**. The power-ups carry the monetization, and a
paywall on the fail screen is the worst place a player can meet one. Whichever
way you decide, decide it on purpose and write the reason in the code — the
next reader will assume it was an oversight otherwise.

---

## Part 3 — Verify

### 3.1 It compiles

The real check is the Unity console with zero errors. To check without the
Editor — in CI, or before reopening — compile the assembly directly against the
references Unity already wrote into the generated `.csproj`:

```bash
python3 - <<'PY'
import re, io, os
s = io.open("Assembly-CSharp.csproj", encoding="utf-8").read()
refs = [h for h in re.findall(r"<HintPath>(.*?)</HintPath>", s) if os.path.exists(h)]
io.open("/tmp/refs.rsp", "w", encoding="utf-8").write("\n".join('-r:"%s"' % r for r in refs))
src = [os.path.join(r, f) for r, d, fs in os.walk("Assets") for f in fs
       if f.endswith(".cs") and "Editor" not in os.path.join(r, f).split(os.sep)]
io.open("/tmp/src.rsp", "w", encoding="utf-8").write("\n".join('"%s"' % f for f in src))
print(len(refs), "references,", len(src), "sources")
PY

csc -nologo -target:library -langversion:9.0 -nostdlib+ -noconfig \
    -define:UNITY_2022_3\;UNITY_ANDROID -out:/tmp/out.dll @/tmp/refs.rsp @/tmp/src.rsp
```

Any `HintPath` that no longer exists on disk is itself a finding: it names an
SDK DLL you deleted, and confirms the csproj is stale rather than that something
is missing.

**Compile the player configuration too** — without `UNITY_EDITOR`. Ad code is
full of `#if UNITY_EDITOR` branches that granted rewards for free in the Editor,
and the branch that only compiles on device is the one nobody looks at.

### 3.2 Nothing dangles

- No scene or prefab references a GUID you deleted (the script in step 1.4).
- `grep` for every deleted class name across `Assets/`, comments included.
- Open each scene in the Editor and check the Hierarchy for
  "Missing (Mono Script)".

### 3.3 The store wiring is right

Product id → power-up → PlayerPrefs key is three mappings, usually three
`switch` statements with a `default` arm. A typo does not fail to compile; it
sells the wrong thing or tops up the wrong counter. Assert it:

```csharp
[MenuItem("YourGame/Power-Ups/Verify Store Mapping")]
static void Verify()
{
    // distinct product ids, distinct counter keys, correct key prefix,
    // counters round-trip through PlayerPrefs, every power-up has a price,
    // RefillAmount > 0
}
```

Purrdoku's lives in `PowerUpOfferPanelBuilder.cs`, next to the debug menu items
that empty and restore the counters.

### 3.4 The flows actually bridge

Editor, with the counters emptied (*Meowdoku > Power-Ups > Empty All Charges*):

- [ ] Each of the three power-up buttons opens the panel, with the right title and icon
- [ ] The badge reads `+` at zero and the button is still clickable
- [ ] The X appears after the delay and closes the panel
- [ ] Android back closes the panel and does **not** fall through to gameplay
- [ ] The buy button shows a price, never a blank
- [ ] Closing the panel leaves gameplay exactly as it was
- [ ] The revive button (or your equivalent) still works

On a device, installed from a store track, with a real test account:

- [ ] The real localized price replaces the fallback
- [ ] A purchase adds exactly N charges
- [ ] **The same power-up can be bought twice** — this is the consumable/confirm bug
- [ ] Killing the app mid-purchase and relaunching grants the charges once, not twice or never
- [ ] Cancelling the sheet grants nothing and shows no error

---

## What was done to this project

| Area | Change |
|---|---|
| **SDKs deleted** | AppLovin MAX (+13 mediation adapters), Google Mobile Ads, Firebase (App/Analytics/RemoteConfig), AppsFlyer, ByteBrew, `google-services.json`, `GeneratedLocalRepo` |
| **Scripts deleted** | `AD Scripts/` (`MediationHandler`, `InterstitialAdManager`, `MRecAdManager`, `RectAd`, `AdGate`, `AppOpenDebugMenu`), `Analytics/` (`GameEvents`, `AppsFlyerBootstrap`) |
| **Native payloads deleted** | `Plugins/iOS`, `Plugins/tvOS`, `googlemobileads-unity.aar`, `GoogleMobileAdsPlugin.androidlib`, `FirebaseApp.androidlib` |
| **Stragglers deleted** | `Editor Default Resources/Firebase` (18 console icons), `StreamingAssets/google-services-desktop.json`, `ProjectSettings/AndroidResolverDependencies.xml`, `ProjectSettings/AppLovinInternalSettings.json` |
| **Rescued** | `Toast.ShowToast` → `Scripts/Toast.cs` (used by the daily-challenge card, never by ads) |
| **Renamed** | `RemoveAdsStore` → `PowerUpStore`, `RemoveAdsPanel` → `PowerUpOfferPanel`, `RemoveAdsPanelBuilder` → `PowerUpOfferPanelBuilder` |
| **Manifest** | dropped `com.google.android.gms.permission.AD_ID` |
| **Gradle** | `mainTemplate` resolver block cut from 40 dependencies to the 2 Play Review needs; `settingsTemplate` repository block cut from 8 maven repos to `mavenLocal()` |
| **Defines** | `BYTEBREW` cleared on Android and iOS; `PLAY_REVIEW` kept |
| **Gameplay** | power-up refill → IAP panel; post-level interstitial deleted (store review keeps the slot); revive is free; challenge unlock level is a constant again; tutorial/level analytics calls deleted |
| **Scenes** | `Splash`: AppsFlyer object, ByteBrew prefab instance, `MediationHandler` + `InterstitialAdManager` components removed (host GameObject kept — it also runs the splash, renamed `Splash`). `SampleScene`: "Remove Ads" button removed, panel renamed and re-serialized |
| **Strings** | added `Out of Auto Cats` / `Out of Hints` / `Out of Undos` / `Get {0} more` in all 9 languages; removed `Remove ads` |
| **Kept deliberately** | External Dependency Manager, Google Play Review, Unity IAP, consent gate, local notifications |

Net: 738 files, +1,588 / −55,822 lines.

The porting guides in [docs/](docs/README.md) that describe ad features
(`AD-GATE`, `APP-OPEN-ADS`, `BYTEBREW-EVENTS`, `REWARDED-ADS`,
`REWARDED-UNAVAILABLE`, `REMOVE-ADS-IAP`) are kept on purpose. They are
generic porting guides for *other* projects, not descriptions of this one, and
the implementations they document are no longer in this repository.

---

## Store setup checklist

Three consumables, one per power-up, priced at $1.99:

| Power-up | Product id | Type |
|---|---|---|
| Auto Cat | `com.dss.purr.doku.autocats3` | Consumable |
| Hint | `com.dss.purr.doku.hints3` | Consumable |
| Undo | `com.dss.purr.doku.undos3` | Consumable |

- Create them in the console **before** testing — an id that does not exist is
  simply not returned by `FetchProducts`, with no error of its own.
- Mark them **Active**. A drafted product fetches as nothing.
- Google Play calls a consumable a "Managed product"; App Store Connect calls it
  "Consumable"; RuStore calls it "Consumable" too.
- The build must be **installed from a store track** and signed with the upload
  key, or billing never connects. A sideloaded APK cannot fetch a price.
- Run *Assets > External Dependency Manager > Android Resolver > Force Resolve*
  after cloning — see trap 1.

**On RuStore specifically:** Unity IAP has no RuStore billing backend. The code
in `PowerUpStore` is store-agnostic in shape — connect, fetch, buy, grant,
confirm — but for a RuStore build the `StoreController` calls must be swapped
for RuStore's own billing client. `Grant`, `PriceFor` and the whole panel are
unchanged by that swap; only the five store callbacks move. Budget for it.

---

## Traps

### 1. Unity IAP needs the External Dependency Manager

EDM4U arrives bundled with ad SDKs, which makes it look like part of the ad
stack. It is not. Unity IAP 5.x declares `com.android.billingclient:billing`
**in code** rather than in a `Dependencies.xml`, and EDM is what turns that into
a Gradle dependency.

Delete EDM and billing quietly fails to link. Worse, a *fresh clone* gets no
price until someone runs **Android Resolver > Force Resolve** — the resolve
leaves no tracked file change, so there is nothing in the diff to hint at it.

The Editor's fake store fabricates a price, so **Editor success proves nothing
here**. Only a device build installed from a store track does.

### 2. A consumable that is never confirmed can only be bought once

Covered in [2.2](#22-products-must-be-consumables-and-must-be-confirmed), and
repeated because it is the bug you will actually ship. It passes every test that
only buys something once.

### 3. Renaming a `.cs` without its `.meta` detaches every scene reference

Unity binds `MonoBehaviour`s by the GUID in the `.meta`, not by class name. Move
the pair together (`git mv` both) and scene components survive a rename intact.
Move only the `.cs` and Unity generates a fresh GUID, leaving every scene
instance as "Missing (Mono Script)".

### 4. Don't delete the consent gate by reflex

It gated the SDKs you removed, so it looks like ad infrastructure. It is not: it
is the Terms/Privacy acceptance panel, it is a store requirement, and it usually
gates billing too — `RunWhenAccepted(Connect)`.

Delete it and IAP initialization goes with it. Keep it, and update its doc
comment to say what it actually holds back now, or the next reader will find a
gate with nothing behind it and delete it for you.

### 5. The Editor cannot prove a remote config value is live

Not a trap in the removal — a trap in what you *replace*. Remote config getters
typically return the default in the Editor. If you are converting a dashboard
key back to a constant, you cannot verify from an Editor run that you picked the
value production was actually serving. Read it off the dashboard before you
delete the dashboard.

### 6. `#if UNITY_EDITOR` branches hide the code that ships

Ad integrations are full of them: `#if UNITY_EDITOR` grants the reward for free
so the flow stays testable, `#else` shows the ad. When you rewrite the call
site, the Editor branch is the one you will look at and the `#else` branch is
the one that ships. Compile both configurations (see
[3.1](#31-it-compiles)).

### 7. The generated `.csproj` and `.sln` lie

They are regenerated on every asset import and still list deleted SDK files
until Unity next reimports. They are not evidence of anything. Don't commit
them, and don't trust them for "is this reference still used" — check the actual
files.
