using UnityEngine;

/// <summary>
/// Arranges child chips in centred, wrapping rows (like the word list in the reference)
/// and slides them smoothly when their order changes.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class WordChipFlow : MonoBehaviour
{
    [SerializeField] private float spacing = 18f;
    [SerializeField] private float rowSpacing = 16f;
    [SerializeField] private float slideSpeed = 14f;

    private RectTransform rt;

    public void Configure(float hSpacing, float vSpacing)
    {
        spacing = hSpacing;
        rowSpacing = vSpacing;
    }

    private void OnEnable()
    {
        rt = (RectTransform)transform;
        Layout(true);
    }

    private void LateUpdate()
    {
        Layout(!Application.isPlaying);
    }

    public void Layout(bool snap)
    {
        if (rt == null) rt = (RectTransform)transform;
        int n = rt.childCount;
        if (n == 0) return;

        float[] widths = new float[n];
        float height = 0f;
        for (int i = 0; i < n; i++)
        {
            RectTransform c = (RectTransform)rt.GetChild(i);
            widths[i] = c.gameObject.activeSelf ? c.sizeDelta.x : -1f;
            height = Mathf.Max(height, c.sizeDelta.y);
        }

        // Shrink the whole word list when there are too many words for the panel (big levels).
        int rowCount = 0;
        int[] rowOf = new int[n];
        System.Collections.Generic.List<float> rowWidths = new System.Collections.Generic.List<float>();
        float totalH = 0f;
        float scale = 1f;
        for (scale = 1f; scale >= 0.5f; scale -= 0.04f)
        {
            float maxW = rt.rect.width / scale;
            rowCount = 0;
            rowWidths.Clear();
            float rowW = 0f;
            for (int i = 0; i < n; i++)
            {
                if (widths[i] < 0f) { rowOf[i] = -1; continue; }
                float add = (rowW > 0f ? spacing : 0f) + widths[i];
                if (rowW > 0f && rowW + add > maxW)
                {
                    rowWidths.Add(rowW);
                    rowCount++;
                    rowW = widths[i];
                }
                else
                {
                    rowW += add;
                }
                rowOf[i] = rowCount;
            }
            rowWidths.Add(rowW);
            rowCount++;
            totalH = rowCount * height + (rowCount - 1) * rowSpacing;
            if (totalH * scale <= rt.rect.height + 0.5f) break;
        }
        scale = Mathf.Max(0.5f, scale);
        if (Mathf.Abs(rt.localScale.x - scale) > 0.001f) rt.localScale = new Vector3(scale, scale, 1f);

        float[] cursor = new float[rowCount];
        for (int r = 0; r < rowCount; r++) cursor[r] = -rowWidths[r] * 0.5f;

        float k = snap ? 1f : 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime);

        for (int i = 0; i < n; i++)
        {
            if (rowOf[i] < 0) continue;
            RectTransform c = (RectTransform)rt.GetChild(i);
            int r = rowOf[i];
            c.anchorMin = c.anchorMax = new Vector2(0.5f, 0.5f);
            c.pivot = new Vector2(0.5f, 0.5f);
            float x = cursor[r] + widths[i] * 0.5f;
            float y = totalH * 0.5f - height * 0.5f - r * (height + rowSpacing);
            cursor[r] += widths[i] + spacing;
            Vector2 target = new Vector2(x, y);
            c.anchoredPosition = Vector2.Lerp(c.anchoredPosition, target, k);
        }
    }
}
