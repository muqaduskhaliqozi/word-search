using UnityEngine;
using UnityEngine.UI;

public class SmoothSelectionLine : MonoBehaviour
{
    [Header("Line")]
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private Image lineImage;

    [Header("Settings")]
    [SerializeField] private float lineThickness = 70f;

    private RectTransform canvasRect;

    private void Awake()
    {
        if (lineRect == null)
            lineRect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        Hide();
    }

    public void ShowBetween(
        RectTransform start,
        RectTransform end)
    {
        if (start == null || end == null)
            return;

        if (canvasRect == null)
            return;

        Vector2 startPosition =
            WorldToCanvasPosition(start);

        Vector2 endPosition =
            WorldToCanvasPosition(end);

        Vector2 direction =
            endPosition - startPosition;

        float distance =
            direction.magnitude;

        Vector2 middle =
            (startPosition + endPosition) * 0.5f;

        lineRect.anchoredPosition = middle;

        lineRect.sizeDelta =
            new Vector2(
                distance,
                lineThickness
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        lineRect.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (lineRect != null)
        {
            lineRect.gameObject.SetActive(false);
        }
    }

    private Vector2 WorldToCanvasPosition(
        RectTransform target)
    {
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(
                null,
                target.position
            );

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out localPoint
        );

        return localPoint;
    }
}