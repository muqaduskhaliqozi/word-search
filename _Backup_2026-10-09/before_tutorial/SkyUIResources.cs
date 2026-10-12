using TMPro;
using UnityEngine;

/// <summary>
/// Art + fonts for panels that build themselves at runtime (consent, no-internet, remove-ads offer).
/// Lives at Assets/WordSearchTheme/Resources/SkyUIResources.asset - filled by Tools > Word Search > Apply Sky Theme.
/// </summary>
public class SkyUIResources : ScriptableObject
{
    public TMP_FontAsset font;        // Fredoka
    public TMP_FontAsset buttonFont;  // Lato
    public Sprite headerCard;         // white card with blue header (9-sliced)
    public Sprite plainCard;          // white card (9-sliced)
    public Sprite greenButton;
    public Sprite blueButton;
    public Sprite closeButton;
    public Sprite noAdsBadge;
    public Sprite logo;
    public Sprite coin;

    private static SkyUIResources cached;
    public static SkyUIResources Get()
    {
        if (cached == null) cached = Resources.Load<SkyUIResources>("SkyUIResources");
        return cached;
    }
}
