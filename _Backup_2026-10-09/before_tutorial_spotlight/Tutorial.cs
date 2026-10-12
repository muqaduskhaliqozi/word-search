using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// First-level tutorial: a hand swipes across the first word (whose letters pulse), with a tip bubble.
/// Ends when the player finds that word -> short "Great!" message -> never shown again.
/// Does not block input: the player plays the real level while being guided.
/// </summary>
public class Tutorial : MonoBehaviour
{
    public const string DoneKey = "WS_TutorialDone";
    public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;

    private static Tutorial instance;

    private WordSearchLevel level;
    private RectTransform root;      // full-screen layer on the gameplay canvas
    private RectTransform hand;
    private CanvasGroup handGroup;
    private RectTransform bubble;
    private TextMeshProUGUI bubbleText;
    private List<LetterTile> path;
    private WordTarget target;
    private bool finished;

    public static void Begin(WordSearchLevel level, RectTransform canvas)
    {
        if (Done || level == null || canvas == null) return;
        if (instance != null) { if (instance.level == level) return; Destroy(instance.gameObject); }

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
    }

    private IEnumerator Start()
    {
        // let the tiles finish their intro pop and the grid lay out
        yield return new WaitForSecondsRealtime(0.9f);
        if (level == null) { Destroy(gameObject); yield break; }

        foreach (WordTarget w in level.TargetWords)
        {
            if (w == null || w.IsCompleted) continue;
            List<LetterTile> tiles = level.GetWordTiles(w);
            if (tiles != null && tiles.Count >= 2) { target = w; path = tiles; break; }
        }
        if (target == null) { Destroy(gameObject); yield break; }

        BuildUI();
        level.ShowTutorialHint(); // pulses the first word's letters (cleared by the game on first touch)
        level.WordFound += OnWordFound;
        StartCoroutine(HandLoop());
    }

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
        bubbleText = RuntimeUI.Label(tr, "Swipe across the letters to find the word <color=#1E8CF0>" +
                                         target.TargetWord.ToUpper() + "</color>!", 40, RuntimeUI.TextDark, false,
                                         TextAlignmentOptions.Center, true);
        PlaceBubble();

        // hand (pivot on the fingertip)
        Image h = RuntimeUI.NewImage("Hand", root, r != null ? r.hand : null);
        hand = h.rectTransform;
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.38f, 0.93f);
        hand.sizeDelta = new Vector2(170, 170);
        handGroup = h.gameObject.AddComponent<CanvasGroup>();
        handGroup.blocksRaycasts = false;
        handGroup.interactable = false;

        StartCoroutine(RuntimeUI.Pop(bubble));
    }

    /// <summary>Bubble goes above the word if there is room on the board, otherwise below it.</summary>
    private void PlaceBubble()
    {
        Vector2 a = Local(path[0]), z = Local(path[path.Count - 1]);
        float top = Mathf.Max(a.y, z.y), bottom = Mathf.Min(a.y, z.y);
        float cell = CellSize();
        float above = top + cell * 0.5f + 40f + bubble.sizeDelta.y * 0.5f;
        float below = bottom - cell * 0.5f - 170f - bubble.sizeDelta.y * 0.5f; // leave room for the hand
        float limit = root.rect.height * 0.5f - 420f; // keep clear of the top bar + word card
        bubble.anchoredPosition = new Vector2(0f, above < limit ? above : below);
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
        while (!finished)
        {
            // hide while the player is touching the board
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
            {
                handGroup.alpha = 0f;
                yield return null;
                continue;
            }

            Vector2 from = Local(path[0]), to = Local(path[path.Count - 1]);
            float t = 0f;
            // appear + press
            while (t < 0.35f && !finished && !Touching())
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
            float dur = 0.25f + 0.18f * path.Count;
            while (t < dur && !finished && !Touching())
            {
                t += Time.unscaledDeltaTime;
                hand.anchoredPosition = Vector2.Lerp(from, to, Tween.InOutSine(Mathf.Clamp01(t / dur)));
                yield return null;
            }
            // lift + fade
            t = 0f;
            while (t < 0.35f && !finished && !Touching())
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                handGroup.alpha = 1f - k;
                hand.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, k);
                yield return null;
            }
            handGroup.alpha = 0f;
            float wait = 0.35f;
            while (wait > 0f && !finished && !Touching()) { wait -= Time.unscaledDeltaTime; yield return null; }
        }
    }

    private static bool Touching() => Input.touchCount > 0 || Input.GetMouseButton(0);

    private void OnWordFound(WordTarget found, List<LetterTile> tiles)
    {
        if (finished) return;
        finished = true;
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        StartCoroutine(Outro());
    }

    private IEnumerator Outro()
    {
        if (handGroup != null) handGroup.alpha = 0f;
        if (bubbleText != null)
        {
            bubbleText.text = "Great! Now find the rest of the words.";
            if (bubble != null) StartCoroutine(RuntimeUI.Pop(bubble));
        }
        yield return new WaitForSecondsRealtime(2.5f);
        float t = 0f;
        CanvasGroup g = bubble != null ? bubble.gameObject.AddComponent<CanvasGroup>() : null;
        while (g != null && t < 0.3f) { t += Time.unscaledDeltaTime; g.alpha = 1f - t / 0.3f; yield return null; }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (level != null) level.WordFound -= OnWordFound;
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
