using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot UI effects (sparkle bursts, confetti rain, floating text) drawn on a top-most FX layer.
/// Put one on a full-screen RectTransform as the LAST child of the canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIBurst : MonoBehaviour
{
    public static UIBurst Instance { get; private set; }

    [SerializeField] private Sprite sparkleSprite;
    [SerializeField] private Sprite glowSprite;
    [SerializeField] private Sprite confettiSprite;
    [SerializeField] private Sprite starSprite;

    private static readonly Color[] ConfettiColors =
    {
        new Color(1.00f, 0.25f, 0.35f), // red
        new Color(1.00f, 0.60f, 0.10f), // orange
        new Color(1.00f, 0.88f, 0.15f), // yellow
        new Color(0.30f, 0.85f, 0.25f), // green
        new Color(0.15f, 0.75f, 1.00f), // sky blue
        new Color(0.20f, 0.45f, 1.00f), // blue
        new Color(0.65f, 0.35f, 1.00f), // purple
        new Color(1.00f, 0.40f, 0.80f)  // pink
    };

    private RectTransform layer;
    private Camera uiCamera;

    public void SetSprites(Sprite sparkle, Sprite glow, Sprite confetti, Sprite star)
    {
        sparkleSprite = sparkle; glowSprite = glow; confettiSprite = confetti; starSprite = star;
    }

    private void Awake()
    {
        Instance = this;
        layer = (RectTransform)transform;
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) uiCamera = c.worldCamera;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private Vector2 ToLocal(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out Vector2 local);
        return local;
    }

    private Image Spawn(Sprite s, Vector2 pos, float size, Color c)
    {
        GameObject go = new GameObject("fx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(layer, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        Image img = go.GetComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    // ------------------------------------------------------------------ sparkles
    public void Sparkles(Vector3 worldPos, Color tint, int count = 14, float radius = 220f)
    {
        if (!isActiveAndEnabled) return;
        Vector2 p = ToLocal(worldPos);

        if (glowSprite != null) StartCoroutine(GlowRoutine(Spawn(glowSprite, p, 120f, new Color(tint.r, tint.g, tint.b, 0.9f))));

        for (int i = 0; i < count; i++)
        {
            Sprite s = (i % 3 == 0 && starSprite != null) ? starSprite : sparkleSprite;
            if (s == null) continue;
            Color c = i % 2 == 0 ? Color.white : Color.Lerp(tint, Color.white, 0.4f);
            float size = Random.Range(28f, 60f);
            float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            StartCoroutine(FlyRoutine(Spawn(s, p, size, c), dir * Random.Range(radius * 0.5f, radius), Random.Range(0.55f, 0.85f)));
        }
    }

    private IEnumerator GlowRoutine(Image img)
    {
        RectTransform rt = img.rectTransform;
        float t = 0f;
        const float dur = 0.5f;
        Color c = img.color;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 3.2f, Tween.OutCubic(k));
            c.a = 0.9f * (1f - k);
            img.color = c;
            yield return null;
        }
        Destroy(img.gameObject);
    }

    private IEnumerator FlyRoutine(Image img, Vector2 offset, float dur)
    {
        RectTransform rt = img.rectTransform;
        Vector2 start = rt.anchoredPosition;
        float spin = Random.Range(-360f, 360f);
        Color c = img.color;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = start + offset * Tween.OutCubic(k);
            rt.localScale = Vector3.one * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * k);
            c.a = 1f - k * k;
            img.color = c;
            yield return null;
        }
        Destroy(img.gameObject);
    }

    // ------------------------------------------------------------------ confetti
    public void Confetti(int count = 70)
    {
        if (!isActiveAndEnabled || confettiSprite == null) return;
        Rect r = layer.rect;
        for (int i = 0; i < count; i++)
        {
            Vector2 start = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(20f, 400f));
            Image img = Spawn(confettiSprite, start, Random.Range(22f, 36f), ConfettiColors[Random.Range(0, ConfettiColors.Length)]);
            img.preserveAspect = false;
            img.rectTransform.sizeDelta = new Vector2(Random.Range(14f, 22f), Random.Range(24f, 38f));
            StartCoroutine(ConfettiRoutine(img, r.yMin - 60f));
        }
    }

    /// <summary>Upward confetti cannon from a world position.</summary>
    public void ConfettiBurst(Vector3 worldPos, int count = 40)
    {
        if (!isActiveAndEnabled || confettiSprite == null) return;
        Vector2 p = ToLocal(worldPos);
        Rect r = layer.rect;
        for (int i = 0; i < count; i++)
        {
            Image img = Spawn(confettiSprite, p, 24f, ConfettiColors[Random.Range(0, ConfettiColors.Length)]);
            img.preserveAspect = false;
            img.rectTransform.sizeDelta = new Vector2(Random.Range(14f, 22f), Random.Range(24f, 38f));
            Vector2 v = new Vector2(Random.Range(-700f, 700f), Random.Range(900f, 1700f));
            StartCoroutine(BallisticRoutine(img, v, r.yMin - 60f));
        }
    }

    private IEnumerator ConfettiRoutine(Image img, float floorY)
    {
        RectTransform rt = img.rectTransform;
        float speed = Random.Range(380f, 650f);
        float sway = Random.Range(30f, 90f);
        float phase = Random.value * 10f;
        float spin = Random.Range(180f, 540f) * (Random.value > 0.5f ? 1f : -1f);
        Vector2 p = rt.anchoredPosition;
        float t = 0f;
        while (p.y > floorY && t < 8f)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            p.y -= speed * dt;
            rt.anchoredPosition = p + new Vector2(Mathf.Sin((t + phase) * 3f) * sway, 0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            rt.localScale = new Vector3(Mathf.Cos((t + phase) * 7f), 1f, 1f);
            yield return null;
        }
        Destroy(img.gameObject);
    }

    private IEnumerator BallisticRoutine(Image img, Vector2 v, float floorY)
    {
        RectTransform rt = img.rectTransform;
        Vector2 p = rt.anchoredPosition;
        float spin = Random.Range(-720f, 720f);
        float t = 0f;
        while (p.y > floorY && t < 6f)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            v.y -= 2200f * dt;
            v *= Mathf.Exp(-1.2f * dt);
            if (v.y < -500f) v.y = -500f;
            p += v * dt;
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            rt.localScale = new Vector3(Mathf.Cos(t * 8f), 1f, 1f);
            yield return null;
        }
        Destroy(img.gameObject);
    }
}
