using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Level 1 tutorial, one guided step per word (e.g. WIN, then PLAY) until the level is complete.
/// Each step:
///  - the screen is dimmed except a "spotlight" on that word's letters and its word chip
///    (stencil cut-outs, so popups still draw above it),
///  - only that word's letters respond to touch (WordSearchLevel input lock); Hint/Shuffle are locked,
///  - the word chip hops, the letters pulse, a hand swipes across them and a tip bubble explains.
/// Found -> "Great!" -> next word. After the last word the board is unlocked and the tutorial never shows again.
/// </summary>
[DefaultExecutionOrder(1000)] // LateUpdate after WordChipFlow, so the chip hop is not overwritten by the layout
public class Tutorial : MonoBehaviour
{
    public const string DoneKey = "WS_TutorialDone";
    public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;
    /// <summary>True while the tutorial is running (boosters are locked meanwhile).</summary>
    public static bool Active => instance != null && !instance.completed;

    private const float DimAlpha = 0.62f;
    private const float TilePad = 10f;   // overlapping pads join the tile holes into one lane
    private const float ChipPad = 12f;

    private static Tutorial instance;

    private WordSearchLevel level;
    private RectTransform root;      // full-screen layer on the gameplay canvas
    private RectTransform hand;
    private CanvasGroup handGroup;
    private RectTransform bubble;
    private TextMeshProUGUI bubbleText;
    private List<LetterTile> path;
    private WordTarget target;
    private int step;                // 0 = first word
    private bool guiding;            // a word is highlighted and waiting to be found
    private bool completed;
    private bool wasTouching;

    // spotlight
    private Image dim;
    private float dimTarget;
    private RectTransform holesWrite, holesClear;
    private readonly List<Hole> holes = new List<Hole>();
    private Material matWrite, matDim, matClear;

    // chip hop
    private RectTransform chip;
    private Vector2 chipOffset;
    private bool hopping;

    private class Hole
    {
        public RectTransform follow;
        public float pad;
        public RectTransform write, clear;
    }

    public static void Begin(WordSearchLevel level, RectTransform canvas)
    {
        if (Done || level == null || canvas == null) return;
        // same level already guided and still at its start -> keep it; otherwise (restart / new level) start over
        if (instance != null)
        {
            if (instance.level == level && instance.step == 0 && !instance.guiding) return;
            DestroyImmediate(instance.gameObject);
        }

        GameObject go = new GameObject("SK_Tutorial", typeof(RectTransform));
        go.layer = canvas.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        // above the board and HUD, below popups (settings / level complete) and the FX layer
        Transform complete = canvas.Find("LevelCompletePanel");
        if (complete != null) rt.SetSiblingIndex(complete.GetSiblingIndex());
        instance = go.AddComponent<Tutorial>();
        instance.level = level;
        instance.root = rt;
        level.SetInputLock(new LetterTile[0]); // nothing playable until the first word is highlighted
    }

    private IEnumerator Start()
    {
        // let the tiles finish their intro pop and the grid lay out
        yield return new WaitForSecondsRealtime(0.9f);
        if (level == null) { Destroy(gameObject); yield break; }

        BuildSpotlight();
        BuildUI();
        level.WordFound += OnWordFound;
        dimTarget = DimAlpha;
        StartCoroutine(HandLoop());
        if (!NextStep()) Finish();
    }

    /// <summary>Highlights the next unfound word. False when there is none left.</summary>
    private bool NextStep()
    {
        WordTarget next = null;
        List<LetterTile> tiles = null;
        foreach (WordTarget w in level.TargetWords)
        {
            if (w == null || w.IsCompleted) continue;
            List<LetterTile> t = level.GetWordTiles(w);
            if (t != null && t.Count >= 2) { next = w; tiles = t; break; }
        }
        if (next == null) return false;

        target = next;
        path = tiles;
        SetChip(target.transform as RectTransform);

        ClearHoles();
        foreach (LetterTile t in path) AddHole(t.transform as RectTransform, TilePad);
        AddHole(chip, ChipPad);
        UpdateHoles();

        level.SetInputLock(path);      // only this word's letters are playable
        level.ShowTutorialHint(target); // pulse them

        string word = "<color=#1E8CF0>" + target.TargetWord.ToUpper() + "</color>";
        bubbleText.text = step == 0
            ? "Swipe across the letters to find the word " + word + "!"
            : "Now find the word " + word + "!";
        PlaceBubble();
        bubble.gameObject.SetActive(true);
        StartCoroutine(RuntimeUI.Pop(bubble));

        hopping = true;
        guiding = true;
        return true;
    }

    // ------------------------------------------------------------------ spotlight

    private void BuildSpotlight()
    {
        matWrite = StencilMaterial(CompareFunction.Always, StencilOp.Replace, false);
        matDim = StencilMaterial(CompareFunction.NotEqual, StencilOp.Keep, true);
        matClear = StencilMaterial(CompareFunction.Always, StencilOp.Zero, false);

        // draw order: write holes -> dim (skips holes) -> clear holes (leaves the stencil clean for later masks)
        holesWrite = Layer("HolesWrite");
        dim = RuntimeUI.NewImage("Dim", root, null);
        RectTransform d = dim.rectTransform;
        d.anchorMin = Vector2.zero; d.anchorMax = Vector2.one;
        d.offsetMin = new Vector2(-50f, -50f); d.offsetMax = new Vector2(50f, 50f);
        dim.color = new Color(0f, 0f, 0f, 0f);
        dim.material = matDim;
        holesClear = Layer("HolesClear");
    }

    private RectTransform Layer(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void ClearHoles()
    {
        foreach (Hole h in holes)
        {
            if (h.write != null) Destroy(h.write.gameObject);
            if (h.clear != null) Destroy(h.clear.gameObject);
        }
        holes.Clear();
    }

    private void AddHole(RectTransform follow, float pad)
    {
        if (follow == null) return;
        Sprite shape = ShapeOf(follow);
        Hole h = new Hole { follow = follow, pad = pad };
        h.write = HoleImage(holesWrite, shape, matWrite);
        h.clear = HoleImage(holesClear, shape, matClear);
        holes.Add(h);
    }

    private static RectTransform HoleImage(RectTransform parent, Sprite shape, Material mat)
    {
        Image img = RuntimeUI.NewImage("Hole", parent, shape);
        img.type = shape != null && shape.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.material = mat;
        img.color = Color.white;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        return rt;
    }

    /// <summary>The rounded shape of a tile / chip, so the cut-out matches it.</summary>
    private static Sprite ShapeOf(RectTransform t)
    {
        Image own = t.GetComponent<Image>();
        if (own != null && own.sprite != null) return own.sprite;
        foreach (Image i in t.GetComponentsInChildren<Image>(true))
            if (i.sprite != null) return i.sprite;
        SkyUIResources r = SkyUIResources.Get();
        return r != null ? r.plainCard : null;
    }

    private static Material StencilMaterial(CompareFunction comp, StencilOp op, bool drawColor)
    {
        Material m = new Material(Canvas.GetDefaultCanvasMaterial());
        m.name = "TutorialStencil_" + op;
        m.SetFloat("_Stencil", 1f);
        m.SetFloat("_StencilComp", (float)comp);
        m.SetFloat("_StencilOp", (float)op);
        m.SetFloat("_StencilReadMask", 255f);
        m.SetFloat("_StencilWriteMask", 255f);
        m.SetFloat("_ColorMask", drawColor ? 15f : 0f);
        if (!drawColor) m.EnableKeyword("UNITY_UI_ALPHACLIP"); // rounded corners: transparent pixels don't cut
        return m;
    }

    private void UpdateHoles()
    {
        Vector3[] c = new Vector3[4];
        foreach (Hole h in holes)
        {
            bool on = h.follow != null && h.follow.gameObject.activeInHierarchy;
            h.write.gameObject.SetActive(on);
            h.clear.gameObject.SetActive(on);
            if (!on) continue;
            h.follow.GetWorldCorners(c);
            Vector3 a = root.InverseTransformPoint(c[0]), b = root.InverseTransformPoint(c[2]);
            Vector2 centre = new Vector2((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f);
            Vector2 size = new Vector2(Mathf.Abs(b.x - a.x) + h.pad * 2f, Mathf.Abs(b.y - a.y) + h.pad * 2f);
            h.write.anchoredPosition = h.clear.anchoredPosition = centre;
            h.write.sizeDelta = h.clear.sizeDelta = size;
        }
    }

    // ------------------------------------------------------------------ per frame

    private void Update()
    {
        // undo last frame's hop before WordChipFlow lays the chips out again
        if (chip != null && chipOffset != Vector2.zero)
        {
            chip.anchoredPosition -= chipOffset;
            chipOffset = Vector2.zero;
        }

        if (dim != null)
        {
            Color col = dim.color;
            col.a = Mathf.MoveTowards(col.a, dimTarget, Time.unscaledDeltaTime * 2.2f);
            dim.color = col;
        }

        // a touch clears the letter pulse; bring it back after a missed swipe
        bool touching = Touching();
        if (wasTouching && !touching && guiding && level != null && target != null && !target.IsCompleted)
            level.ShowTutorialHint(target);
        wasTouching = touching;
    }

    private void LateUpdate()
    {
        if (hopping && chip != null)
        {
            // hop up and down with a little squash on landing
            float p = Time.unscaledTime * 3.4f;
            float s = Mathf.Abs(Mathf.Sin(p));
            chipOffset = new Vector2(0f, s * 18f);
            chip.anchoredPosition += chipOffset;
            float squash = 1f - Mathf.Clamp01(1f - s * 4f) * 0.08f;
            chip.localScale = new Vector3((2f - squash) * 1.06f, squash * 1.06f, 1f);
        }
        if (holes.Count > 0) UpdateHoles();
    }

    private void SetChip(RectTransform next)
    {
        if (chip != null)
        {
            if (chipOffset != Vector2.zero) chip.anchoredPosition -= chipOffset;
            if (hopping) chip.localScale = Vector3.one;
        }
        chipOffset = Vector2.zero;
        hopping = false;
        chip = next;
    }

    private void StopHop()
    {
        if (!hopping) return;
        hopping = false;
        if (chip != null) chip.localScale = Vector3.one; // WordTarget's own found-punch takes over from here
    }

    // ------------------------------------------------------------------ bubble + hand

    private void BuildUI()
    {
        SkyUIResources r = SkyUIResources.Get();

        // tip bubble
        Image b = RuntimeUI.NewImage("Bubble", root, r != null ? r.plainCard : null);
        b.type = Image.Type.Sliced;
        if (b.sprite == null) b.color = Color.white;
        bubble = b.rectTransform;
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.sizeDelta = new Vector2(860, 190);

        GameObject tg = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)tg.transform;
        tr.SetParent(bubble, false);
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(60, 40); tr.offsetMax = new Vector2(-60, -40);
        bubbleText = RuntimeUI.Label(tr, "", 40, RuntimeUI.TextDark, false, TextAlignmentOptions.Center, true);
        bubble.gameObject.SetActive(false);

        // hand (pivot on the fingertip)
        Image h = RuntimeUI.NewImage("Hand", root, r != null ? r.hand : null);
        hand = h.rectTransform;
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.38f, 0.93f);
        hand.sizeDelta = new Vector2(170, 170);
        handGroup = h.gameObject.AddComponent<CanvasGroup>();
        handGroup.alpha = 0f;
        handGroup.blocksRaycasts = false;
        handGroup.interactable = false;
    }

    /// <summary>Bubble sits between the word chip and the word if it fits there, otherwise below the word.</summary>
    private void PlaceBubble()
    {
        Vector2 a = Local(path[0]), z = Local(path[path.Count - 1]);
        float top = Mathf.Max(a.y, z.y), bottom = Mathf.Min(a.y, z.y);
        float cell = CellSize();
        float half = bubble.sizeDelta.y * 0.5f;
        float above = top + cell * 0.5f + 30f + half;
        float below = bottom - cell * 0.5f - 170f - half; // leave room for the hand

        float limit = root.rect.height * 0.5f - 420f; // fallback: clear of the top bar + word card
        if (chip != null)
        {
            Vector3[] c = new Vector3[4];
            chip.GetWorldCorners(c);
            limit = root.InverseTransformPoint(c[0]).y - ChipPad - 20f; // bubble top stays under the chip
        }
        bubble.anchoredPosition = new Vector2(0f, above + half <= limit ? above : below);
    }

    private float CellSize()
    {
        RectTransform t = path[0].transform as RectTransform;
        if (t == null) return 80f;
        Vector3 s = t.lossyScale, rs = root.lossyScale;
        return t.rect.height * (rs.y > 0f ? s.y / rs.y : 1f);
    }

    private Vector2 Local(LetterTile tile)
    {
        Vector3 p = root.InverseTransformPoint(tile.transform.position);
        return new Vector2(p.x, p.y);
    }

    private IEnumerator HandLoop()
    {
        while (!completed)
        {
            // hidden between steps and while the player is touching the board
            if (!guiding || Touching() || path == null)
            {
                handGroup.alpha = 0f;
                yield return null;
                continue;
            }

            List<LetterTile> p = path;
            Vector2 from = Local(p[0]), to = Local(p[p.Count - 1]);
            float t = 0f;
            // appear + press
            while (t < 0.35f && Guiding(p))
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                handGroup.alpha = k;
                hand.anchoredPosition = from;
                hand.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
                yield return null;
            }
            // drag along the word
            t = 0f;
            float dur = 0.25f + 0.18f * p.Count;
            while (t < dur && Guiding(p))
            {
                t += Time.unscaledDeltaTime;
                hand.anchoredPosition = Vector2.Lerp(from, to, Tween.InOutSine(Mathf.Clamp01(t / dur)));
                yield return null;
            }
            // lift + fade
            t = 0f;
            while (t < 0.35f && Guiding(p))
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                handGroup.alpha = 1f - k;
                hand.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, k);
                yield return null;
            }
            handGroup.alpha = 0f;
            float wait = 0.35f;
            while (wait > 0f && Guiding(p)) { wait -= Time.unscaledDeltaTime; yield return null; }
        }
    }

    private bool Guiding(List<LetterTile> p) => guiding && path == p && !completed && !Touching();

    private static bool Touching() => Input.touchCount > 0 || Input.GetMouseButton(0);

    // ------------------------------------------------------------------ steps

    private void OnWordFound(WordTarget found, List<LetterTile> tiles)
    {
        if (!guiding) return;
        guiding = false;
        StopHop();
        if (handGroup != null) handGroup.alpha = 0f;
        level.SetInputLock(new LetterTile[0]); // nothing playable during the "Great!" beat
        StartCoroutine(AfterWord());
    }

    private IEnumerator AfterWord()
    {
        bool more = false;
        foreach (WordTarget w in level.TargetWords)
            if (w != null && !w.IsCompleted) { more = true; break; }

        if (!more) { Finish(); yield break; }

        bubbleText.text = "Great!";
        StartCoroutine(RuntimeUI.Pop(bubble));
        yield return new WaitForSecondsRealtime(1.3f);
        if (level == null) yield break;
        step++;
        if (!NextStep()) Finish();
    }

    /// <summary>All words of level 1 found: unlock everything and never show again.</summary>
    private void Finish()
    {
        if (completed) return;
        completed = true;
        guiding = false;
        StopHop();
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        if (level != null) level.SetInputLock(null);
        dimTarget = 0f;
        StartCoroutine(Outro());
    }

    private IEnumerator Outro()
    {
        if (handGroup != null) handGroup.alpha = 0f;
        float t = 0f;
        CanvasGroup g = bubble != null && bubble.gameObject.activeSelf ? bubble.gameObject.AddComponent<CanvasGroup>() : null;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            if (g != null) g.alpha = 1f - t / 0.35f;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (chip != null && chipOffset != Vector2.zero) chip.anchoredPosition -= chipOffset;
        if (chip != null && hopping) chip.localScale = Vector3.one;
        if (level != null)
        {
            level.WordFound -= OnWordFound;
            level.SetInputLock(null); // never leave the board locked
        }
        if (matWrite != null) Destroy(matWrite);
        if (matDim != null) Destroy(matDim);
        if (matClear != null) Destroy(matClear);
        if (instance == this) instance = null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Word Search/Tutorial/Reset Tutorial + First Session")]
    private static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(DoneKey);
        PlayerPrefs.DeleteKey(SplashController.FirstSessionKey);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] reset - next Play from the splash goes straight to level 1 with the tutorial");
    }
#endif
}
