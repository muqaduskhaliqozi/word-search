using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Out of Hints / Out of Shuffles" buy panel. The HUD opens it when a power-up button is tapped with no
/// charges left (it used to play a rewarded video there). Buy goes through PowerUpStore -> RuStore; the panel
/// closes itself once the charges land. Was the Remove Ads offer panel - same art, pop and close button.
/// </summary>
public class PowerUpOfferPanel : MonoBehaviour
{
    // The player opened this with their own tap, so "no" has to be quick. Unscaled time, so a parked
    // Time.timeScale can never turn the panel into a trap.
    public const float CloseDelaySeconds = 1.5f;
    public const int SortingOrder = 30000;

    private static PowerUpOfferPanel instance;
    private PowerUpStore.PowerUp which;
    private Sprite icon;
    private RuntimeUI.Panel panel;
    private TextMeshProUGUI priceLabel;
    private GameObject closeButton;
    private static int closedFrame = -1;

    // Also true for the frame the panel closed in: the Back press that closed it must not reach the HUD,
    // whose Update may run after ours in that same frame.
    public static bool IsOpen => (instance != null && instance.panel != null && instance.panel.root != null && instance.panel.root.activeSelf)
                                 || closedFrame == Time.frameCount;

    /// <param name="which">Power-up being offered.</param>
    /// <param name="buttonIcon">The art of the button the player just tapped, so the panel shows exactly that icon.</param>
    public static void Show(PowerUpStore.PowerUp which, Sprite buttonIcon)
    {
        if (IsOpen) return;
        GameObject go = new GameObject("PowerUpOfferPanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PowerUpOfferPanel>();
        instance.which = which;
        instance.icon = buttonIcon;
    }

    private void Start()
    {
        string name = PowerUpStore.NameFor(which);
        panel = RuntimeUI.Create("PowerUpOffer UI", SortingOrder, new Vector2(900, 820), true, true);
        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Title", 0, 40, new Vector2(700, 76)), "Out of " + name, 52, Color.white);

        SkyUIResources r = SkyUIResources.Get();
        Image badge = RuntimeUI.NewImage("Icon", panel.card, icon);
        badge.preserveAspect = true;
        if (icon == null) badge.enabled = false;
        RectTransform br = badge.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.anchoredPosition = new Vector2(0, -(24 + 250));
        br.sizeDelta = new Vector2(220, 235);

        RuntimeUI.Label(RuntimeUI.OnCard(panel, "Body", 0, 440, new Vector2(760, 130)),
            $"Get {PowerUpStore.RefillAmount} more {name.ToLowerInvariant()} and keep playing!",
            40, RuntimeUI.TextDark, false, TextAlignmentOptions.Center, true);

        Button buy = RuntimeUI.PillButton(panel, "Buy", 640, 560, true, PriceText(), Buy);
        priceLabel = buy.GetComponentInChildren<TextMeshProUGUI>();

        // close X (top-right of the card), shown after a short delay
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

        PowerUpStore.PricesChanged += OnPrices;
        PowerUpStore.PurchaseFinished += OnPurchaseFinished;
        StartCoroutine(RuntimeUI.Pop(panel.card));
        StartCoroutine(RevealClose());
    }

    /// <summary>Never a blank button: the localized RuStore price once it is known, a plain label until then.</summary>
    private string PriceText()
    {
        string p = PowerUpStore.PriceFor(which); // read live: the price may have arrived before this panel existed
        return string.IsNullOrEmpty(p) ? $"Get +{PowerUpStore.RefillAmount}" : "Buy for " + p;
    }

    private void Buy()
    {
        if (IAPLoading.IsOpen) return; // a purchase is already in flight
        IAPLoading.Show(); // immediate feedback; closed by the purchase result (bought / cancelled / failed)
        if (!PowerUpStore.Buy(which)) IAPLoading.Hide();
    }

    private IEnumerator RevealClose()
    {
        yield return new WaitForSecondsRealtime(CloseDelaySeconds);
        if (closeButton != null) closeButton.SetActive(true);
    }

    private void OnPrices() { if (priceLabel != null) priceLabel.text = PriceText(); }

    private void OnPurchaseFinished(PowerUpStore.PowerUp p, bool success)
    {
        if (p != which) return;
        IAPLoading.Hide(); // cancelled = no message, as the player chose it
        if (success)
        {
            SfxPlayer.Play(SfxPlayer.Sfx.Star);
            Close();
        }
    }

    private void Update()
    {
        // Android back = the close button. Swallowed while the store is busy (IAPLoading is on top).
        if (Input.GetKeyDown(KeyCode.Escape) && !IAPLoading.IsOpen) Close();
    }

    public void Close()
    {
        closedFrame = Time.frameCount;
        PowerUpStore.PricesChanged -= OnPrices;
        PowerUpStore.PurchaseFinished -= OnPurchaseFinished;
        if (panel != null && panel.root != null) Destroy(panel.root);
        Destroy(gameObject);
        if (instance == this) instance = null;
    }

    private void OnDestroy()
    {
        PowerUpStore.PricesChanged -= OnPrices;
        PowerUpStore.PurchaseFinished -= OnPurchaseFinished;
        if (instance == this) instance = null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Word Search/Power-Ups/Show Hint Offer (Play mode)")]
    private static void DebugShowHint() { if (Application.isPlaying) Show(PowerUpStore.PowerUp.Hint, null); }

    [UnityEditor.MenuItem("Tools/Word Search/Power-Ups/Show Shuffle Offer (Play mode)")]
    private static void DebugShowShuffle() { if (Application.isPlaying) Show(PowerUpStore.PowerUp.Shuffle, null); }
#endif
}
