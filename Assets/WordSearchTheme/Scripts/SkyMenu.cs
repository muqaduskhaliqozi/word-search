using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Saved player options shared by every scene.
/// </summary>
public static class GameSettings
{
    public const string MusicKey = "WS_MusicOn";
    public const string VibrateKey = "WS_VibrateOn";
    public const string AdsRemovedKey = "WS_AdsRemoved";

    public const string CoinsKey = "WS_Coins";
    public const int StartingCoins = 300;

    public static event Action Changed;

    public static int Coins
    {
        get => PlayerPrefs.GetInt(CoinsKey, StartingCoins);
        set { PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    public static bool VibrateOn
    {
        get => PlayerPrefs.GetInt(VibrateKey, 1) == 1;
        set { PlayerPrefs.SetInt(VibrateKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    /// <summary>Set this to true from your purchase code when "Remove Ads" is bought.</summary>
    public static bool AdsRemoved
    {
        get => PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;
        set { PlayerPrefs.SetInt(AdsRemovedKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    /// <summary>Short buzz if the player has vibration on.</summary>
    public static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (VibrateOn && Application.isPlaying) Handheld.Vibrate();
#endif
    }
}

/// <summary>
/// Main menu in the "Sky" look: Levels / New Game, settings screen, no-ads, leaderboard, rate, privacy, more games.
/// Built by Tools > Word Search > Apply Sky Menu Theme.
/// </summary>
public class SkyMenu : MonoBehaviour
{
    [SerializeField] private MainMenuController menu;

    [Header("Main menu")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button noAdsButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button rateButton;
    [SerializeField] private Button privacyButton;
    [SerializeField] private Button moreGamesButton;
    [Tooltip("If on, New Game starts again from Level 1. If off, it continues from the next unplayed level.")]
    [SerializeField] private bool newGameStartsFromLevelOne = false;

    [Header("Settings screen")]
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private GameObject removeAdsCard;
    [SerializeField] private Button removeAdsBuyButton;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private string price = "Rs: 2990";
    [SerializeField] private Button soundToggle;
    [SerializeField] private Button musicToggle;
    [SerializeField] private Button vibrateToggle;
    [SerializeField] private Sprite toggleOn;
    [SerializeField] private Sprite toggleOff;
    [SerializeField] private Button settingsPrivacyButton;
    [SerializeField] private Button termsButton;

    [Header("Links (fill these in)")]
    [SerializeField] private string privacyPolicyUrl = "https://example.com/privacy";
    [SerializeField] private string termsUrl = "https://example.com/terms";
    [Tooltip("Your Google Play developer page, e.g. https://play.google.com/store/apps/dev?id=XXXX")]
    [SerializeField] private string moreGamesUrl = "https://play.google.com/store/apps/developer?id=YOUR_DEV_NAME";

    [Header("Hooks for your SDKs")]
    [Tooltip("Called when the player taps Remove Ads / the price button. Start your in-app purchase here.")]
    public UnityEvent onRemoveAdsClicked;
    [Tooltip("Called when the player taps Leaderboard. Open Google Play Games leaderboard here.")]
    public UnityEvent onLeaderboardClicked;

    public static event Action RemoveAdsRequested;
    public static event Action LeaderboardRequested;

    /// <summary>Lets other screens (gameplay No-Ads badge) start the same Remove Ads purchase.</summary>
    public static void RequestRemoveAds() => RemoveAdsRequested?.Invoke();

    private CanvasGroup settingsGroup;

    private void Awake()
    {
        if (menu == null) menu = FindObjectOfType<MainMenuController>();
    }

    private void Start()
    {
        Hook(settingsButton, OpenSettings);
        Hook(noAdsButton, OpenSettings); // the badge leads to the Remove Ads offer
        Hook(newGameButton, NewGame);
        Hook(leaderboardButton, Leaderboard);
        Hook(rateButton, RateUs);
        Hook(privacyButton, () => OpenUrl(privacyPolicyUrl));
        Hook(moreGamesButton, () => OpenUrl(moreGamesUrl));

        Hook(settingsBackButton, CloseSettings);
        Hook(removeAdsBuyButton, RemoveAds);
        Hook(soundToggle, () => { SfxPlayer.SoundOn = !SfxPlayer.SoundOn; RefreshToggles(); });
        Hook(musicToggle, () => { GameSettings.MusicOn = !GameSettings.MusicOn; RefreshToggles(); });
        Hook(vibrateToggle, () => { GameSettings.VibrateOn = !GameSettings.VibrateOn; RefreshToggles(); GameSettings.Vibrate(); });
        Hook(settingsPrivacyButton, () => OpenUrl(privacyPolicyUrl));
        Hook(termsButton, () => OpenUrl(termsUrl));

        if (priceText != null) priceText.text = price;
        if (settingsScreen != null)
        {
            settingsGroup = settingsScreen.GetComponent<CanvasGroup>();
            if (settingsGroup == null) settingsGroup = settingsScreen.AddComponent<CanvasGroup>();
            settingsScreen.SetActive(false);
        }

        GameSettings.Changed += RefreshAds;
        RefreshToggles();
        RefreshAds();
    }

    private void OnDestroy()
    {
        GameSettings.Changed -= RefreshAds;
    }

    private void Update()
    {
        // Android back button closes the settings screen
        if (Input.GetKeyDown(KeyCode.Escape) && settingsScreen != null && settingsScreen.activeSelf) CloseSettings();
    }

    private static void Hook(Button b, UnityAction a)
    {
        if (b == null) return;
        b.onClick.AddListener(a); // click sound comes from UIButtonJuice
    }

    // ------------------------------------------------------------------ menu actions
    private void NewGame()
    {
        if (menu == null) return;
        if (newGameStartsFromLevelOne) LevelProgress.RequestLevel(0);
        menu.PlayGame();
    }

    private void Leaderboard()
    {
        onLeaderboardClicked?.Invoke();
        LeaderboardRequested?.Invoke();
    }

    private void RateUs()
    {
        string id = Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL("market://details?id=" + id);
#else
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + id);
#endif
    }

    private static void OpenUrl(string url)
    {
        if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
    }

    private void RemoveAds()
    {
        onRemoveAdsClicked?.Invoke();
        RemoveAdsRequested?.Invoke();
    }

    // ------------------------------------------------------------------ settings screen
    public void OpenSettings()
    {
        if (settingsScreen == null) return;
        RefreshToggles();
        settingsScreen.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Fade(true));
    }

    public void CloseSettings()
    {
        if (settingsScreen == null || !settingsScreen.activeSelf) return;
        StopAllCoroutines();
        StartCoroutine(Fade(false));
    }

    private IEnumerator Fade(bool show)
    {
        RectTransform rt = settingsScreen.transform as RectTransform;
        float from = show ? 0f : 1f, to = show ? 1f : 0f, t = 0f;
        const float dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float a = Mathf.Lerp(from, to, Tween.OutCubic(k));
            settingsGroup.alpha = a;
            if (rt != null) rt.anchoredPosition = new Vector2(0f, (1f - a) * -60f);
            yield return null;
        }
        settingsGroup.alpha = to;
        if (rt != null) rt.anchoredPosition = Vector2.zero;
        if (!show) settingsScreen.SetActive(false);
    }

    private void RefreshToggles()
    {
        SetToggle(soundToggle, SfxPlayer.SoundOn);
        SetToggle(musicToggle, GameSettings.MusicOn);
        SetToggle(vibrateToggle, GameSettings.VibrateOn);
    }

    private void SetToggle(Button b, bool on)
    {
        if (b == null) return;
        Image img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img != null) img.sprite = on ? toggleOn : toggleOff;
    }

    private void RefreshAds()
    {
        bool removed = GameSettings.AdsRemoved;
        if (noAdsButton != null) noAdsButton.gameObject.SetActive(!removed);
        if (removeAdsCard != null) removeAdsCard.SetActive(!removed);
    }
}
