using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animates a popup whenever it is enabled: overlay fades in, card pops with overshoot,
/// optional stars pop one by one, sun-rays spin and confetti rains.
/// Put it on the full-screen overlay object (the one that gets SetActive(true)).
/// </summary>
public class PopupAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform card;
    [SerializeField] private RectTransform[] stars;
    [SerializeField] private RectTransform rays;
    [SerializeField] private RectTransform[] lateItems; // buttons etc. that pop after the card
    [SerializeField] private bool confetti = false;
    [SerializeField] private bool celebrate = false;

    private CanvasGroup overlayGroup;

    public void Setup(RectTransform cardRect, RectTransform[] starRects, RectTransform raysRect, RectTransform[] late, bool withConfetti, bool withCelebrate)
    {
        card = cardRect; stars = starRects; rays = raysRect; lateItems = late; confetti = withConfetti; celebrate = withCelebrate;
    }

    private void Awake()
    {
        overlayGroup = GetComponent<CanvasGroup>();
        if (overlayGroup == null) overlayGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private void Update()
    {
        if (rays != null) rays.Rotate(0f, 0f, -20f * Time.unscaledDeltaTime);
    }

    private IEnumerator ShowRoutine()
    {
        overlayGroup.alpha = 0f;
        if (card != null) card.localScale = Vector3.one * 0.5f;
        if (stars != null) foreach (var s in stars) if (s != null) s.localScale = Vector3.zero;
        if (lateItems != null) foreach (var s in lateItems) if (s != null) s.localScale = Vector3.zero;
        if (rays != null) rays.localScale = Vector3.zero;

        if (celebrate) SfxPlayer.Play(SfxPlayer.Sfx.Complete);
        else SfxPlayer.Play(SfxPlayer.Sfx.Popup);

        // overlay + card
        float t = 0f;
        const float dur = 0.45f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            overlayGroup.alpha = Mathf.Clamp01(k * 2f);
            if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, Tween.OutBack(k, 2.2f));
            yield return null;
        }

        if (confetti && UIBurst.Instance != null)
        {
            UIBurst.Instance.Confetti();
            if (card != null)
            {
                UIBurst.Instance.ConfettiBurst(card.position + new Vector3(-card.rect.width * 0.4f * card.lossyScale.x, 0f, 0f), 30);
                UIBurst.Instance.ConfettiBurst(card.position + new Vector3(card.rect.width * 0.4f * card.lossyScale.x, 0f, 0f), 30);
            }
        }

        if (rays != null) StartCoroutine(Scale(rays, 0.6f, 0f));

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                StartCoroutine(Scale(stars[i], 0.4f, 0f, 2.6f));
                SfxPlayer.Play(SfxPlayer.Sfx.Star, 1f + i * 0.12f);
                if (UIBurst.Instance != null) UIBurst.Instance.Sparkles(stars[i].position, new Color(1f, 0.85f, 0.3f), 10, 160f);
                yield return new WaitForSecondsRealtime(0.22f);
            }
        }

        if (lateItems != null)
        {
            foreach (var item in lateItems)
            {
                if (item == null) continue;
                StartCoroutine(Scale(item, 0.4f, 0f));
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }
    }

    private IEnumerator Scale(RectTransform target, float dur, float delay, float overshoot = 1.70158f)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            target.localScale = Vector3.one * Tween.OutBack(k, overshoot);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>Animated close. Calls onClosed after the object is deactivated.</summary>
    public void Close(Action onClosed = null)
    {
        if (!isActiveAndEnabled)
        {
            onClosed?.Invoke();
            return;
        }
        StopAllCoroutines();
        StartCoroutine(CloseRoutine(onClosed));
    }

    private IEnumerator CloseRoutine(Action onClosed)
    {
        float t = 0f;
        const float dur = 0.2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            overlayGroup.alpha = 1f - k;
            if (card != null) card.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, Tween.InCubic(k));
            yield return null;
        }
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }
}
