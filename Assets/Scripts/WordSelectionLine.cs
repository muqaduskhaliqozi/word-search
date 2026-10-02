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

    private Vector2 targetPosition;
    private Vector2 targetSize;
    private float targetRotation;

    private bool visible = false;
    private bool shaking = false;

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
        }

        // Every newly created/loaded line starts unlocked.
        IsLocked = false;

        Hide();
    }

    private void Update()
    {
        if (!visible || shaking || lineRect == null)
            return;

        // Position and size are smoothed; rotation is set directly
        // so diagonal lines never swing or wobble.
        lineRect.anchoredPosition = Vector2.Lerp(
            lineRect.anchoredPosition,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );

        lineRect.sizeDelta = Vector2.Lerp(
            lineRect.sizeDelta,
            targetSize,
            Time.deltaTime * smoothSpeed
        );

        lineRect.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
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
            lineRect.sizeDelta = targetSize;
            lineRect.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
        }

        visible = true;

        if (lineImage != null)
        {
            lineImage.enabled = true;
        }
    }

    public void SetColor(Color color)
    {
        if (lineImage != null)
        {
            lineImage.color = color;
        }
    }

    public void Lock()
    {
        IsLocked = true;
        visible = true;

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
        visible = false;
        shaking = false;

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

        Vector2 originalPosition = lineRect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float x = Random.Range(-shakeAmount, shakeAmount);
            float y = Random.Range(-shakeAmount, shakeAmount);

            lineRect.anchoredPosition = originalPosition + new Vector2(x, y);

            yield return null;
        }

        lineRect.anchoredPosition = originalPosition;

        shaking = false;
        Hide();

        // Wrong line becomes available again.
        IsLocked = false;
    }
}