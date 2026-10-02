using UnityEngine;

/// <summary>
/// Scales (and optionally slides) an element in with an overshoot every time it is enabled.
/// </summary>
public class UIPopIn : MonoBehaviour
{
    [SerializeField] private float delay = 0f;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private Vector2 slideFrom = Vector2.zero;
    [SerializeField] private float startScale = 0f;
    [SerializeField] private bool fade = true;

    private RectTransform rt;
    private Vector2 basePos;
    private Vector3 baseScale;
    private CanvasGroup group;
    private bool captured;

    public void Configure(float delaySeconds, Vector2 slide, float fromScale = 0f, float time = 0.45f)
    {
        delay = delaySeconds; slideFrom = slide; startScale = fromScale; duration = time;
    }

    private void Awake()
    {
        rt = transform as RectTransform;
        if (rt != null)
        {
            basePos = rt.anchoredPosition;
            baseScale = rt.localScale == Vector3.zero ? Vector3.one : rt.localScale;
            captured = true;
        }
        if (fade)
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        if (!captured) return;
        StopAllCoroutines();
        Apply(0f);
        Tween.Run(this, duration, Apply, null, delay);
    }

    private void Apply(float t)
    {
        float e = Tween.OutBack(t);
        rt.localScale = baseScale * Mathf.LerpUnclamped(startScale, 1f, e);
        rt.anchoredPosition = basePos + slideFrom * (1f - Tween.OutCubic(t));
        if (group != null) group.alpha = Mathf.Clamp01(t * 2.5f);
    }
}
