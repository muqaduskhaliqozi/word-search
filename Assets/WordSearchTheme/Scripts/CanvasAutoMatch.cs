using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the portrait layout readable on every phone / tablet:
/// tall phones match width, wide screens (tablets) match height.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasAutoMatch : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);

    private CanvasScaler scaler;
    private int lastW, lastH;

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    private void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        if (lastH == 0) return;
        float aspect = lastW / (float)lastH;
        float refAspect = referenceResolution.x / referenceResolution.y;
        scaler.matchWidthOrHeight = aspect > refAspect ? 1f : 0f;
    }
}
