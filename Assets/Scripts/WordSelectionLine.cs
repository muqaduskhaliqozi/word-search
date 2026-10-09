using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WordSelectionLine : MonoBehaviour
{
    [Header("Line UI")]
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private Image lineImage;

    [Header("Settings")]
    [SerializeField] private float thickness = 70f;
    [SerializeField] private float smoothSpeed = 25f;

    [Header("Wrong Selection")]
    [SerializeField] private float shakeAmount = 8f;

    public bool IsLocked { get; private set; } = false;
    public float Thickness { get => thickness; set => thickness = value; }

    private Vector2 targetPosition;
    private Vector2 targetSize;
    private float targetRotation;

    private bool visible = false;
    private bool shaking = false;

    private Color baseColor = Color.white;
    private float fadeIn = 1f;      // 0..1 alpha ramp when the bar first appears
    private float lockPop = -1f;    // >=0 while the "found" pop is playing

    private void Awake()
    {
        // Automatically use this object's RectTransform
        if (lineRect == null)
        {
            lineRect = GetComponent<RectTransform>();
        }

        // Automatically find Image on this object
        if (lineImage == null)
        {
            lineImage = GetComponent<Image>();
        }

        // The bar should never block tile touches
        if (lineImage != null)
        {
            lineImage.raycastTarget = false;
            baseColor = lineImage.color;
        }

        // Every newly created/loaded line starts unlocked.
        IsLocked = false;

        Hide();
    }

    private void Update()
    {
        if (!visible || shaking || lineRect == null)
            return;

        float dt = Time.unscaledDeltaTime;

        // Position and size are smoothed; rotation is set directly
        // so diagonal lines never swing or wobble.
        lineRect.anchoredPosition = Vector2.Lerp(
            lineRect.anchoredPosition,
            targetPosition,
            dt * smoothSpeed
        );

        Vector2 size = targetSize;
        if (lockPop >= 0f)
        {
            lockPop += dt;
            float k = Mathf.Clamp01(lockPop / 0.35f);
            size.y *= 1f + Tween.Punch(k) * 0.35f;
            if (k >= 1f) lockPop = -1f;
        }

        lineRect.sizeDelta = Vector2.Lerp(
            lineRect.sizeDelta,
            size,
            dt * smoothSpeed
        );

        lineRect.localRotation = Quaternion.Euler(0f, 0f, targetRotation);

        if (fadeIn < 1f && lineImage != null)
        {
            fadeIn = Mathf.Min(1f, fadeIn + dt * 8f);
            ApplyAlpha(fadeIn);
        }
    }

    private void ApplyAlpha(float k)
    {
        if (lineImage == null) return;
        Color c = baseColor;
        c.a = baseColor.a * k;
        lineImage.color = c;
    }

    public void ShowBetween(RectTransform first, RectTransform last)
    {
        if (first == null || last == null)
            return;

        if (lineRect == null)
            return;

        RectTransform parentRect = lineRect.parent as RectTransform;

        if (parentRect == null)
            return;

        Camera uiCamera = null;

        Canvas canvas = lineRect.GetComponentInParent<Canvas>();

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        Vector2 firstScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, first.position);
        Vector2 lastScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, last.position);

        Vector2 start;
        Vector2 end;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, firstScreen, uiCamera, out start);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, lastScreen, uiCamera, out end);

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        targetPosition = (start + end) * 0.5f;
        targetSize = new Vector2(distance + thickness, thickness);
        targetRotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // First time the bar appears: place it instantly (no swinging in from an old state)
        bool firstShow = !visible;

        lineRect.gameObject.SetActive(true);

        if (firstShow)
        {
            lineRect.anchoredPosition = targetPosition;
            lineRect.sizeDelta = new Vector2(thickness, thickness); // grows out from the first tile
            lineRect.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
            fadeIn = 0f;
            ApplyAlpha(0f);
        }

        visible = true;

        if (lineImage != null)
        {
            lineImage.enabled = true;
        }
    }

    /// <summary>World-space centre of the bar (used to spawn effects).</summary>
    public Vector3 WorldCenter => lineRect != null ? lineRect.position : transform.position;

    public Color BaseColor => baseColor;

    public void SetColor(Color color)
    {
        baseColor = color;
        if (lineImage != null)
        {
            lineImage.color = color;
        }
    }

    public void Lock()
    {
        IsLocked = true;
        visible = true;
        fadeIn = 1f;
        ApplyAlpha(1f);
        lockPop = 0f;

        if (lineRect != null)
        {
            lineRect.gameObject.SetActive(true);
        }

        if (lineImage != null)
        {
            lineImage.enabled = true;
        }
    }

    public void Hide()
    {
        // a hidden bar is free to be used again (restart / next level)
        IsLocked = false;
        visible = false;
        shaking = false;
        lockPop = -1f;

        if (lineImage != null)
        {
            lineImage.enabled = false;
        }

        if (lineRect != null)
        {
            lineRect.gameObject.SetActive(false);
        }
    }

    public IEnumerator ShakeAndHide(float duration)
    {
        // Wrong lines should NEVER become locked.
        IsLocked = false;

        if (lineRect == null)
        {
            Hide();
            yield break;
        }

        visible = true;
        shaking = true;

        lineRect.gameObject.SetActive(true);

        if (lineImage != null)
        {
            lineImage.enabled = true;
        }

        // Snap to the final shape so the shake happens on the full bar.
        lineRect.sizeDelta = targetSize;
        Vector2 originalPosition = targetPosition;
        Color wrong = Color.Lerp(baseColor, new Color(0.95f, 0.3f, 0.25f, baseColor.a), 0.6f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);

            float x = Mathf.Sin(elapsed * 70f) * shakeAmount * (1f - k);
            lineRect.anchoredPosition = originalPosition + new Vector2(x, 0f);

            if (lineImage != null)
            {
                Color c = wrong;
                c.a = wrong.a * (1f - k * k);
                lineImage.color = c;
            }

            yield return null;
        }

        lineRect.anchoredPosition = originalPosition;
        if (lineImage != null) lineImage.color = baseColor;

        shaking = false;
        Hide();

        // Wrong line becomes available again.
        IsLocked = false;
    }
}
