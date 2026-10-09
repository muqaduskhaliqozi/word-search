using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the self-spawning panels (consent, no-internet, remove-ads offer) in the Sky look,
/// on their own sorting layer so they sit above any scene. Layout is in 1080x1920 design pixels.
/// Backdrop stays full-bleed on the canvas; the card goes inside a SafeArea node.
/// </summary>
public static class RuntimeUI
{
    public static readonly Color Backdrop = new Color(0.05f, 0.22f, 0.38f, 0.72f);
    public static readonly Color TextDark = new Color32(0x2B, 0x44, 0x57, 0xFF);
    public static readonly Color TextBlue = new Color32(0x1E, 0x8C, 0xF0, 0xFF);
    public static readonly Color GreenText = new Color32(0x13, 0x57, 0x0D, 0xFF);
    public static readonly Color BlueText = new Color32(0x0E, 0x3D, 0x8E, 0xFF);

    public class Panel
    {
        public GameObject root;
        public Canvas canvas;
        public RectTransform safe;
        public RectTransform card;
    }

    public static Panel Create(string name, int sortingOrder, Vector2 cardSize, bool headerCard, bool persistent)
    {
        var p = new Panel();
        p.root = new GameObject(name, typeof(RectTransform));
        if (persistent) UnityEngine.Object.DontDestroyOnLoad(p.root);
        p.canvas = p.root.AddComponent<Canvas>();
        p.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        p.canvas.overrideSorting = true;
        p.canvas.sortingOrder = sortingOrder;
        var scaler = p.root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = Screen.height > 0 && (float)Screen.width / Screen.height > 1080f / 1920f ? 1f : 0f;
        p.root.AddComponent<GraphicRaycaster>();

        // full-bleed, raycast-blocking backdrop
        Image bd = NewImage("Backdrop", p.root.transform, null);
        bd.color = Backdrop; bd.raycastTarget = true;
        Stretch(bd.rectTransform);

        GameObject safeGo = new GameObject("SafeArea", typeof(RectTransform));
        p.safe = (RectTransform)safeGo.transform;
        p.safe.SetParent(p.root.transform, false);
        Stretch(p.safe);
        safeGo.AddComponent<SafeArea>();

        SkyUIResources r = SkyUIResources.Get();
        Image card = NewImage("Card", p.safe, r != null ? (headerCard ? r.headerCard : r.plainCard) : null);
        card.type = Image.Type.Sliced;
        if (card.sprite == null) card.color = Color.white;
        card.raycastTarget = true;
        p.card = card.rectTransform;
        p.card.anchorMin = p.card.anchorMax = new Vector2(0.5f, 0.5f);
        p.card.sizeDelta = cardSize + new Vector2(48, 48); // sprites carry a 24px shadow pad
        p.card.anchoredPosition = Vector2.zero;
        return p;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Rect anchored to the top-centre of the card face; y measured down from the face's top edge.</summary>
    public static RectTransform OnCard(Panel p, string name, float x, float yFromTop, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(p.card, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, -(24f + yFromTop));
        rt.sizeDelta = size;
        return rt;
    }

    public static TextMeshProUGUI Label(RectTransform rt, string text, float size, Color color, bool buttonFont = false,
                                        TextAlignmentOptions align = TextAlignmentOptions.Center, bool wrap = false)
    {
        var t = rt.gameObject.GetComponent<TextMeshProUGUI>();
        if (t == null) t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (t == null)
        {
            // the object already has another Graphic (e.g. an Image): put the text on a child instead
            var child = new GameObject("Text", typeof(RectTransform));
            var cr = (RectTransform)child.transform;
            cr.SetParent(rt, false);
            Stretch(cr);
            t = child.AddComponent<TextMeshProUGUI>();
        }
        SkyUIResources r = SkyUIResources.Get();
        TMP_FontAsset f = r != null ? (buttonFont && r.buttonFont != null ? r.buttonFont : r.font) : null;
        if (f != null) t.font = f;
        t.text = text;
        t.fontSize = SystemFont.Size(size);
        t.enableAutoSizing = true;
        t.fontSizeMax = SystemFont.Size(size);
        t.fontSizeMin = size * 0.55f;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = wrap;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Artist pill button (green or blue) with a label centred on its face.</summary>
    public static Button PillButton(Panel p, string name, float yFromTop, float width, bool green, string label, Action onClick)
    {
        SkyUIResources r = SkyUIResources.Get();
        Sprite s = r != null ? (green ? r.greenButton : r.blueButton) : null;
        float h = s != null ? width * s.rect.height / s.rect.width : width * 0.27f;
        RectTransform rt = OnCard(p, name, 0, yFromTop, new Vector2(width, h));
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        if (s == null) img.color = green ? new Color(0.55f, 0.85f, 0f) : new Color(0.1f, 0.65f, 1f);
        img.raycastTarget = true;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        rt.gameObject.AddComponent<UIButtonJuice>();
        if (onClick != null) b.onClick.AddListener(() => onClick());

        GameObject tgo = new GameObject("Label", typeof(RectTransform));
        RectTransform tr = (RectTransform)tgo.transform;
        tr.SetParent(rt, false);
        float sc = width / 606f; // face inset of the artist pill (sprite px 14,2,14,38)
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(14 * sc, 38 * sc); tr.offsetMax = new Vector2(-14 * sc, -2 * sc);
        Label(tr, label, h * 0.36f, green ? GreenText : BlueText, true);
        return b;
    }

    /// <summary>Little left-right shake, for a Retry tap that did not help.</summary>
    public static IEnumerator Shake(RectTransform rt)
    {
        Vector2 basePos = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 60f) * 18f * (1f - t / 0.4f), 0f);
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    /// <summary>Scale-pop for the card when a panel opens.</summary>
    public static IEnumerator Pop(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * Tween.OutBack(Mathf.Clamp01(t / 0.3f), 1.6f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
}
