using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gameplay HUD: back, hint (with counter badge), shuffle, settings popup, level banner pop.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public const string HintsKey = "WS_Hints";

    [SerializeField] private LevelManager levelManager;
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Top Bar")]
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform levelBanner;

    [Header("Board")]
    [Tooltip("Wooden board behind the grid; resized to fit each level's grid.")]
    [SerializeField] private RectTransform board;
    [SerializeField] private float boardPadding = 50f;

    [Header("Coins")]
    [SerializeField] private TMP_Text coinText;
    [Tooltip("Buttons that offer coins for watching a rewarded video (the + on the coin bar, the shop bag).")]
    [SerializeField] private Button[] getCoinsButtons;
    [SerializeField] private RectTransform coinBar;
    [SerializeField] private Button noAdsButton;
    [SerializeField] private TMP_Text levelRewardText;
    [SerializeField] private bool hintsCostCoins = true;
    [SerializeField] private int hintCost = 50;
    [SerializeField] private int coinsPerLevel = 25;
    [SerializeField] private int rewardedVideoCoins = 100;

    [Header("Boosters")]
    [SerializeField] private Button[] hintButtons;
    [SerializeField] private TMP_Text[] hintCountTexts;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private int startingHints = 3;
    [SerializeField] private int hintsPerLevelComplete = 1;

    [Header("Settings")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button[] moreSettingsButtons;
    [SerializeField] private PopupAnimator settingsPopup;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button settingsHomeButton;
    [SerializeField] private Button settingsRestartButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private TMP_Text soundLabel;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button vibrateButton;
    [SerializeField] private Sprite toggleOn;
    [SerializeField] private Sprite toggleOff;

    private WordSearchLevel hookedLevel;

    private int Hints
    {
        get => PlayerPrefs.GetInt(HintsKey, startingHints);
        set { PlayerPrefs.SetInt(HintsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); RefreshHints(); }
    }

    private void Awake()
    {
        if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnEnable()
    {
        if (levelManager != null)
        {
            levelManager.LevelStarted += OnLevelStarted;
            levelManager.AllLevelsCompleted += OnAllLevelsCompleted;
        }
    }

    private void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.LevelStarted -= OnLevelStarted;
            levelManager.AllLevelsCompleted -= OnAllLevelsCompleted;
        }
        Unhook();
    }

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(GoHome);
        if (hintButtons != null)
            foreach (Button b in hintButtons) if (b != null) { Button captured = b; b.onClick.AddListener(() => UseHint(captured)); }
        if (shuffleButton != null) shuffleButton.onClick.AddListener(Shuffle);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (moreSettingsButtons != null)
            foreach (Button b in moreSettingsButtons) if (b != null) b.onClick.AddListener(OpenSettings);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(CloseSettings);
        if (settingsHomeButton != null) settingsHomeButton.onClick.AddListener(GoHome);
        if (settingsRestartButton != null) settingsRestartButton.onClick.AddListener(Restart);
        if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
        if (musicButton != null) musicButton.onClick.AddListener(() => { GameSettings.MusicOn = !GameSettings.MusicOn; RefreshSound(); });
        if (vibrateButton != null) vibrateButton.onClick.AddListener(() => { GameSettings.VibrateOn = !GameSettings.VibrateOn; RefreshSound(); GameSettings.Vibrate(); });
        if (getCoinsButtons != null)
            foreach (Button b in getCoinsButtons) if (b != null) b.onClick.AddListener(GetCoins);
        if (noAdsButton != null) noAdsButton.onClick.AddListener(SkyMenu.RequestRemoveAds);
        if (levelRewardText != null) levelRewardText.text = "+" + coinsPerLevel;
        GameSettings.Changed += RefreshCoins;
        RefreshCoins();

        if (settingsPopup != null) settingsPopup.gameObject.SetActive(false);

        RefreshHints();
        RefreshSound();

        // LevelManager.Start may already have run before we subscribed.
        if (levelManager != null && levelManager.ActiveLevel != null)
        {
            Hook(levelManager.ActiveLevel);
            FitBoard(levelManager.ActiveLevel, false);
        }
    }

    // ------------------------------------------------------------------ level hooks
    private void OnLevelStarted(WordSearchLevel level)
    {
        Hook(level);
        if (levelBanner != null && isActiveAndEnabled) StartCoroutine(BannerPop());
        FitBoard(level, isActiveAndEnabled);
    }

    private void Hook(WordSearchLevel level)
    {
        if (hookedLevel == level) return;
        Unhook();
        hookedLevel = level;
        if (hookedLevel != null) hookedLevel.LevelCompleted += OnLevelCompleted;
    }

    private void Unhook()
    {
        if (hookedLevel != null) hookedLevel.LevelCompleted -= OnLevelCompleted;
        hookedLevel = null;
    }

    private void OnLevelCompleted()
    {
        if (hintsCostCoins) GameSettings.Coins += coinsPerLevel;
        else if (hintsPerLevelComplete > 0) Hints = Hints + hintsPerLevelComplete;
    }

    private void OnDestroy()
    {
        GameSettings.Changed -= RefreshCoins;
    }

    // ------------------------------------------------------------------ coins
    private void RefreshCoins()
    {
        if (coinText != null) coinText.text = GameSettings.Coins.ToString("N0");
        if (noAdsButton != null) noAdsButton.gameObject.SetActive(!GameSettings.AdsRemoved);
    }

    /// <summary>Rewarded video for coins (uses the project's MediationHandler / AppLovin MAX).</summary>
    private void GetCoins()
    {
        MediationHandler ads = MediationHandler.Instance;
        bool ready = false;
        try { ready = ads != null && ads.IsRewardedAdReady(); } catch (System.Exception) { ready = false; }
        if (!ready)
        {
            if (ads != null) { try { ads.LoadRewardedVideo(); } catch (System.Exception) { } }
            SfxPlayer.Play(SfxPlayer.Sfx.Wrong);
            if (coinBar != null) StartCoroutine(Shake(coinBar));
            return;
        }
        ads.ShowRewardedVideo(() => GameSettings.Coins += rewardedVideoCoins);
    }

    private void OnAllLevelsCompleted()
    {
        SceneFader.LoadScene(menuSceneName);
    }

    private void FitBoard(WordSearchLevel level, bool animate)
    {
        if (board == null || level == null) return;
        GridLayoutGroup grid = level.GetComponentInChildren<GridLayoutGroup>(true);
        if (grid == null) return;
        RectTransform gridRect = (RectTransform)grid.transform;

        int count = 0;
        foreach (Transform c in grid.transform) if (c.gameObject.activeSelf) count++;
        int cols = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, grid.constraintCount) : Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));
        Vector2 content = new Vector2(
            cols * grid.cellSize.x + (cols - 1) * grid.spacing.x,
            rows * grid.cellSize.y + (rows - 1) * grid.spacing.y);

        Vector2 target = content + Vector2.one * boardPadding * 2f;
        board.anchoredPosition = new Vector2(board.anchoredPosition.x, gridRect.anchoredPosition.y);

        if (!animate) { board.sizeDelta = target; return; }
        Vector2 from = board.sizeDelta;
        Tween.Run(this, 0.35f, k => board.sizeDelta = Vector2.LerpUnclamped(from, target, Tween.OutBack(k)));
    }

    private IEnumerator BannerPop()
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.5f);
            levelBanner.localScale = Vector3.one * (1f + Tween.Punch(k) * 0.18f);
            yield return null;
        }
        levelBanner.localScale = Vector3.one;
    }

    // ------------------------------------------------------------------ boosters
    private void UseHint(Button source)
    {
        WordSearchLevel level = levelManager != null ? levelManager.ActiveLevel : null;
        if (level == null) return;

        if (hintsCostCoins)
        {
            if (GameSettings.Coins < hintCost)
            {
                SfxPlayer.Play(SfxPlayer.Sfx.Wrong);
                if (source != null) StartCoroutine(Shake(source.transform));
                if (coinBar != null) StartCoroutine(Punch(coinBar));
                return;
            }
            if (level.UseHint())
            {
                GameSettings.Coins -= hintCost;
                if (coinBar != null) StartCoroutine(Punch(coinBar));
            }
            return;
        }

        if (Hints <= 0)
        {
            SfxPlayer.Play(SfxPlayer.Sfx.Wrong);
            if (source != null) StartCoroutine(Shake(source.transform));
            return;
        }

        if (level.UseHint())
        {
            Hints = Hints - 1;
            foreach (TMP_Text t in hintCountTexts)
                if (t != null) StartCoroutine(Punch(t.transform.parent != null ? t.transform.parent : t.transform));
        }
    }

    private void Shuffle()
    {
        WordSearchLevel level = levelManager != null ? levelManager.ActiveLevel : null;
        if (level != null) level.ShuffleWordList();
        if (shuffleButton != null)
        {
            Transform icon = shuffleButton.transform.Find("Icon");
            StartCoroutine(Spin(icon != null ? icon : shuffleButton.transform));
        }
    }

    private void RefreshHints()
    {
        if (hintCountTexts == null) return;
        int h = Hints;
        foreach (TMP_Text t in hintCountTexts)
        {
            if (t == null) continue;
            t.text = h > 0 ? h.ToString() : "+";
        }
    }

    // ------------------------------------------------------------------ settings
    private void OpenSettings()
    {
        if (settingsPopup != null) settingsPopup.gameObject.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPopup != null) settingsPopup.Close();
    }

    private void Restart()
    {
        if (settingsPopup != null)
            settingsPopup.Close(() => { if (levelManager != null) levelManager.RestartCurrentLevel(); });
        else if (levelManager != null) levelManager.RestartCurrentLevel();
    }

    private void GoHome()
    {
        SceneFader.LoadScene(menuSceneName);
    }

    private void ToggleSound()
    {
        SfxPlayer.SoundOn = !SfxPlayer.SoundOn;
        RefreshSound();
    }

    private void RefreshSound()
    {
        if (soundLabel != null) soundLabel.text = SfxPlayer.SoundOn ? "SOUND: ON" : "SOUND: OFF";
        SetToggle(soundButton, SfxPlayer.SoundOn);
        SetToggle(musicButton, GameSettings.MusicOn);
        SetToggle(vibrateButton, GameSettings.VibrateOn);
    }

    private void SetToggle(Button b, bool on)
    {
        if (b == null || toggleOn == null || toggleOff == null) return;
        Image img = b.targetGraphic as Image;
        if (img != null) img.sprite = on ? toggleOn : toggleOff;
    }

    // ------------------------------------------------------------------ tiny animations
    private IEnumerator Shake(Transform tr)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 50f) * 12f * (1f - t / 0.35f));
            yield return null;
        }
        tr.localRotation = Quaternion.identity;
    }

    private IEnumerator Spin(Transform tr)
    {
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            tr.localRotation = Quaternion.Euler(0f, 0f, -360f * Tween.OutCubic(Mathf.Clamp01(t / 0.45f)));
            yield return null;
        }
        tr.localRotation = Quaternion.identity;
    }

    private IEnumerator Punch(Transform tr)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            tr.localScale = Vector3.one * (1f + Tween.Punch(Mathf.Clamp01(t / 0.35f)) * 0.4f);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }
}
