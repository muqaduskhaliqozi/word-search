using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Remove Ads" offer panel with the store's localized price. Opens automatically after every Nth
/// interstitial (and can be opened from code with Show()). The close X appears after a short delay.
/// Buying goes through IAPManager; the panel closes itself once ads are removed.
/// </summary>
public class RemoveAdsOffer : MonoBehaviour
{
    public const int ShowEveryNthInterstitial = 5;
    public const float CloseDelaySeconds = 5f;
    public const int SortingOrder = 30000;
    private const string CounterKey = "WS_InterstitialCount";

    private static RemoveAdsOffer instance;
    private RuntimeUI.Panel panel;
    private TextMeshProUGUI priceLabel;
    private GameObject closeButton;

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.root != null && instance.panel.root.activeSelf;

    /// <summary>Call from the interstitial dismissed callback.</summary>
    public static void NotifyInterstitialClosed()
    {
        if (GameSettings.AdsRemoved) return;
        int n = PlayerPrefs.GetInt(CounterKey, 0) + 1;
        PlayerPrefs.SetInt(CounterKey, n);
        PlayerPrefs.Save();
        if (n % ShowEveryNthInterstitial == 0) Show();
    }

    public static void Show()
    {
        if (GameSettings.AdsRemoved || IsOpen) return;
        GameObject go = new GameObject("RemoveAdsOffer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RemoveAdsOffer>();
    }

    private void Start()
    {
        panel = RuntimeUI.Create("RemoveAdsOffer UI", SortingOrder, new Vector2(900, 820), true, true);
        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Title", 0, 40, new Vector2(700, 76)), "Remove Ads", 52, Color.white);

        SkyUIResources r = SkyUIResources.Get();
        Image badge = RuntimeUI.NewImage("Badge", panel.card, r != null ? r.noAdsBadge : null);
        badge.preserveAspect = true;
        RectTransform br = badge.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.anchoredPosition = new Vector2(0, -(24 + 250));
        br.sizeDelta = new Vector2(220, 235);

        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Body", 0, 440, new Vector2(760, 130)),
            "Enjoy every puzzle without banner and pop-up ads. Rewarded videos stay optional.",
            36, RuntimeUI.TextDark, false, TextAlignmentOptions.Center, true);

        Button buy = RuntimeUI.PillButton(panel, "Buy", 640, 560, true, PriceText(), () =>
        {
            if (IAPManager.Instance != null) IAPManager.Instance.BuyRemoveAds();
            else SkyMenu.RequestRemoveAds();
        });
        priceLabel = buy.GetComponentInChildren<TextMeshProUGUI>();

        // close X (top-right of the card), shown after a delay
        Sprite cs = r != null ? r.closeButton : null;
        Image ci = RuntimeUI.NewImage("Close", panel.card, cs);
        ci.raycastTarget = true;
        RectTransform cr = ci.rectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 1f);
        cr.anchoredPosition = new Vector2(-40, -40);
        cr.sizeDelta = cs != null ? cs.rect.size : new Vector2(96, 96);
        Button close = ci.gameObject.AddComponent<Button>();
        close.targetGraphic = ci;
        close.onClick.AddListener(Close);
        ci.gameObject.AddComponent<UIButtonJuice>();
        closeButton = ci.gameObject;
        closeButton.SetActive(false);

        IAPManager.PriceUpdated += OnPrice;
        GameSettings.Changed += OnSettingsChanged;
        StartCoroutine(RuntimeUI.Pop(panel.card));
        StartCoroutine(RevealClose());
    }

    private static string PriceText()
    {
        string p = IAPManager.RemoveAdsPrice; // read live: the price may have arrived before this panel existed
        return string.IsNullOrEmpty(p) ? "Remove Ads" : "Buy for " + p;
    }

    private IEnumerator RevealClose()
    {
        yield return new WaitForSecondsRealtime(CloseDelaySeconds);
        if (closeButton != null) closeButton.SetActive(true);
    }

    private void OnPrice(string _) { if (priceLabel != null) priceLabel.text = PriceText(); }
    private void OnSettingsChanged() { if (GameSettings.AdsRemoved) Close(); }

    private void Update()
    {
        // Android back = the close button, but only once the close button is available
        if (Input.GetKeyDown(KeyCode.Escape) && closeButton != null && closeButton.activeSelf) Close();
    }

    public void Close()
    {
        IAPManager.PriceUpdated -= OnPrice;
        GameSettings.Changed -= OnSettingsChanged;
        if (panel != null && panel.root != null) Destroy(panel.root);
        Destroy(gameObject);
        if (instance == this) instance = null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Word Search/Remove Ads/Show Offer Panel (Play mode)")]
    private static void DebugShow() { if (Application.isPlaying) Show(); }

    [UnityEditor.MenuItem("Tools/Word Search/Remove Ads/Reset Purchase + Interstitial Counter")]
    private static void ResetAll()
    {
        PlayerPrefs.DeleteKey(CounterKey); PlayerPrefs.DeleteKey(GameSettings.AdsRemovedKey); PlayerPrefs.DeleteKey(GameSettings.MediationRemoveAdsKey);
        PlayerPrefs.Save(); Debug.Log("[IAP] Remove Ads + interstitial counter reset (Editor PlayerPrefs only)");
    }

    [UnityEditor.MenuItem("Tools/Word Search/Remove Ads/Grant Without Buying (debug)")]
    private static void Grant() { GameSettings.AdsRemoved = true; Debug.Log("[IAP] Remove Ads granted (debug)"); }
#endif
}
