using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI particles (no ParticleSystem needed): falling leaves, rising sparkles or twinkles.
/// Put it on a full-screen RectTransform that does not block raycasts.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIAmbientParticles : MonoBehaviour
{
    public enum Mode { FallingLeaves, RisingSparkles, Twinkle }

    [SerializeField] private Mode mode = Mode.FallingLeaves;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Color[] colors = { Color.white };
    [SerializeField] private int count = 10;
    [SerializeField] private Vector2 sizeRange = new Vector2(30f, 60f);
    [SerializeField] private Vector2 speedRange = new Vector2(40f, 90f);
    [SerializeField] private float swayAmount = 40f;

    private class P
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos;
        public float speed, phase, spin, life, maxLife, size, baseAlpha;
    }

    private readonly List<P> particles = new List<P>();
    private RectTransform area;

    public void Configure(Mode m, Sprite[] s, Color[] c, int n, Vector2 size, Vector2 speed)
    {
        mode = m; sprites = s; colors = c; count = n; sizeRange = size; speedRange = speed;
    }

    private void Start()
    {
        area = (RectTransform)transform;
        if (sprites == null || sprites.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("p", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            P p = new P
            {
                rt = go.GetComponent<RectTransform>(),
                img = go.GetComponent<Image>()
            };
            p.img.raycastTarget = false;
            p.img.preserveAspect = true;
            particles.Add(p);
            Respawn(p, true);
        }
    }

    private void Respawn(P p, bool anywhere)
    {
        Rect r = area.rect;
        p.img.sprite = sprites[Random.Range(0, sprites.Length)];
        Color c = colors[Random.Range(0, colors.Length)];
        p.baseAlpha = c.a;
        p.img.color = c;
        p.size = Random.Range(sizeRange.x, sizeRange.y);
        p.rt.sizeDelta = new Vector2(p.size, p.size);
        p.speed = Random.Range(speedRange.x, speedRange.y);
        p.phase = Random.value * 10f;
        p.spin = Random.Range(-60f, 60f);
        p.maxLife = Random.Range(1.5f, 3.5f);
        p.life = anywhere ? Random.value * p.maxLife : 0f;

        float x = Random.Range(r.xMin, r.xMax);
        float y;
        switch (mode)
        {
            case Mode.FallingLeaves: y = anywhere ? Random.Range(r.yMin, r.yMax) : r.yMax + p.size; break;
            case Mode.RisingSparkles: y = anywhere ? Random.Range(r.yMin, r.yMax) : r.yMin - p.size; break;
            default: y = Random.Range(r.yMin, r.yMax); break;
        }
        p.pos = new Vector2(x, y);
    }

    private void Update()
    {
        if (area == null) return;
        float dt = Time.unscaledDeltaTime;
        Rect r = area.rect;

        foreach (P p in particles)
        {
            p.phase += dt;
            switch (mode)
            {
                case Mode.FallingLeaves:
                    p.pos.y -= p.speed * dt;
                    p.rt.anchoredPosition = p.pos + new Vector2(Mathf.Sin(p.phase * 1.3f) * swayAmount, 0f);
                    p.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p.phase * 0.9f) * 50f + p.phase * p.spin * 0.3f);
                    p.rt.localScale = new Vector3(Mathf.Sin(p.phase * 2.1f) * 0.35f + 0.65f, 1f, 1f);
                    if (p.pos.y < r.yMin - p.size * 2f) Respawn(p, false);
                    break;

                case Mode.RisingSparkles:
                    p.pos.y += p.speed * dt;
                    p.rt.anchoredPosition = p.pos + new Vector2(Mathf.Sin(p.phase * 1.7f) * swayAmount * 0.4f, 0f);
                    float tw = 0.6f + Mathf.Sin(p.phase * 5f) * 0.4f;
                    p.rt.localScale = Vector3.one * tw;
                    SetAlpha(p, p.baseAlpha * tw);
                    if (p.pos.y > r.yMax + p.size * 2f) Respawn(p, false);
                    break;

                case Mode.Twinkle:
                    p.life += dt;
                    float k = Mathf.Clamp01(p.life / p.maxLife);
                    float s = Mathf.Sin(k * Mathf.PI);
                    p.rt.anchoredPosition = p.pos;
                    p.rt.localScale = Vector3.one * s;
                    p.rt.localRotation = Quaternion.Euler(0f, 0f, p.phase * 90f);
                    SetAlpha(p, p.baseAlpha * s);
                    if (p.life >= p.maxLife) Respawn(p, false);
                    break;
            }
        }
    }

    private static void SetAlpha(P p, float a)
    {
        Color c = p.img.color;
        c.a = a;
        p.img.color = c;
    }
}
