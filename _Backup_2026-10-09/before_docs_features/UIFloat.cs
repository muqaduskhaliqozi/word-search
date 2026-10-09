using UnityEngine;

/// <summary>
/// Gentle idle motion: bob up/down, sway rotation and breathe scale. Phase is randomised.
/// </summary>
public class UIFloat : MonoBehaviour
{
    [SerializeField] private float bobAmount = 10f;
    [SerializeField] private float bobSpeed = 1.6f;
    [SerializeField] private float swayDegrees = 0f;
    [SerializeField] private float swaySpeed = 1.2f;
    [SerializeField] private float breatheAmount = 0f;
    [SerializeField] private float breatheSpeed = 2f;
    [SerializeField] private float driftX = 0f; // units/sec, wraps across the parent (clouds)

    private RectTransform rt;
    private Vector2 basePos;
    private Vector3 baseScale;
    private float phase;
    private bool captured;

    public void Configure(float bob, float bobSpd, float sway, float breathe, float drift = 0f)
    {
        bobAmount = bob; bobSpeed = bobSpd; swayDegrees = sway; breatheAmount = breathe; driftX = drift;
    }

    private void Awake()
    {
        rt = transform as RectTransform;
        phase = Random.value * 20f;
    }

    private void OnEnable()
    {
        Capture();
    }

    private void Capture()
    {
        if (rt == null) rt = transform as RectTransform;
        if (rt == null) return;
        basePos = rt.anchoredPosition;
        baseScale = rt.localScale;
        captured = true;
    }

    private void Update()
    {
        if (!captured || rt == null) return;
        float t = Time.unscaledTime + phase;

        Vector2 p = basePos;
        p.y += Mathf.Sin(t * bobSpeed) * bobAmount;

        if (driftX != 0f)
        {
            basePos.x += driftX * Time.unscaledDeltaTime;
            RectTransform parent = rt.parent as RectTransform;
            if (parent != null)
            {
                float half = parent.rect.width * 0.5f + rt.rect.width;
                if (basePos.x > half) basePos.x = -half;
                if (basePos.x < -half) basePos.x = half;
            }
            p.x = basePos.x;
        }

        rt.anchoredPosition = p;

        if (swayDegrees != 0f)
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * swaySpeed) * swayDegrees);

        if (breatheAmount != 0f)
            rt.localScale = baseScale * (1f + Mathf.Sin(t * breatheSpeed) * breatheAmount);
    }
}
