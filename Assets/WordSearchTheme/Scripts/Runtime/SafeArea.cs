using System;
using UnityEngine;

/// <summary>
/// Keeps its RectTransform inside Screen.safeArea (notches, cutouts, rounded corners) and
/// re-applies on rotation / split-screen / resize. Put UI that must not be clipped under it;
/// leave full-screen backgrounds and dim backdrops outside so they still reach the screen edge.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    public event Action Changed;

    private RectTransform rt;
    private Rect lastArea;
    private Vector2Int lastSize;

    private void Awake() { rt = (RectTransform)transform; Apply(true); }
    private void OnEnable() => Apply(true);
    private void Update() => Apply(false);

    private void Apply(bool force)
    {
        if (rt == null) rt = (RectTransform)transform;
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0) return; // happens for a frame while resuming; dividing would give NaN anchors
        Rect area = Screen.safeArea;
        if (!force && area == lastArea && lastSize.x == w && lastSize.y == h) return;
        lastArea = area; lastSize = new Vector2Int(w, h);

        Vector2 min, max;
        Compute(area, w, h, out min, out max);
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Changed?.Invoke();
    }

    /// <summary>Pure anchor math (also used by the editor check).</summary>
    public static void Compute(Rect area, int screenW, int screenH, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(Mathf.Clamp01(area.xMin / screenW), Mathf.Clamp01(area.yMin / screenH));
        max = new Vector2(Mathf.Clamp01(area.xMax / screenW), Mathf.Clamp01(area.yMax / screenH));
    }

    /// <summary>
    /// Inserts a SafeArea node under <paramref name="parent"/> and moves its children into it,
    /// except the named ones (backgrounds, full-screen modals). Idempotent.
    /// </summary>
    public static RectTransform Wrap(Transform parent, params string[] keepOutside)
    {
        Transform existing = parent.Find("SafeArea");
        RectTransform node;
        if (existing != null) node = (RectTransform)existing;
        else
        {
            GameObject go = new GameObject("SafeArea", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            node = (RectTransform)go.transform;
            node.SetParent(parent, false);
            node.anchorMin = Vector2.zero; node.anchorMax = Vector2.one;
            node.offsetMin = Vector2.zero; node.offsetMax = Vector2.zero;
        }
        if (node.GetComponent<SafeArea>() == null) node.gameObject.AddComponent<SafeArea>();

        int firstIndex = -1;
        var move = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in parent)
        {
            if (c == node) continue;
            if (Array.IndexOf(keepOutside, c.name) >= 0) continue;
            if (firstIndex < 0) firstIndex = c.GetSiblingIndex();
            move.Add(c);
        }
        foreach (Transform c in move) c.SetParent(node, false);
        if (firstIndex >= 0) node.SetSiblingIndex(Mathf.Min(firstIndex, parent.childCount - 1));
        return node;
    }
}
