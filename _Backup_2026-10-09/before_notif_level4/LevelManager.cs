using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public const string ProgressKey = "WS_LevelIndex";

    [Header("Levels Container / Array")]
    [SerializeField] private List<GameObject> levels = new List<GameObject>();

    [Header("Shared Complete UI")]
    [SerializeField] private GameObject sharedCompletePanel;
    [SerializeField] private Button sharedNextButton;

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private string levelTextFormat = "Level {0}";

    [Header("Current Level")]
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Progress")]
    [Tooltip("Remember the last reached level between sessions (PlayerPrefs).")]
    [SerializeField] private bool saveProgress = true;

    [Header("Next Button Safety")]
    [Tooltip("Ignore Next unless the complete panel is visible (blocks stray calls).")]
    [SerializeField] private bool requireCompletePanelVisible = true;

    [Header("After Last Level")]
    [SerializeField] private bool loopToFirstLevel = false;
    [SerializeField] private UnityEvent onAllLevelsCompleted;

    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentLevelNumber => currentLevelIndex + 1;
    /// <summary>Total playable levels: hand-made scene levels first, then generated ones.</summary>
    public int LevelCount => Mathf.Max(SceneLevelCount, ProceduralLevels.TotalLevels);
    private int SceneLevelCount => levels != null ? levels.Count : 0;
    public WordSearchLevel ActiveLevel => activeLevelScript;

    /// <summary>Raised every time a level is (re)started.</summary>
    public event Action<WordSearchLevel> LevelStarted;
    /// <summary>Raised after the last level when not looping.</summary>
    public event Action AllLevelsCompleted;

    private WordSearchLevel activeLevelScript;
    private WordSearchLevel hookedLevel;
    private int lastNextFrame = -1;
    private WordSearchLevel proceduralHost;

    private void Start()
    {
        ResolveLevelText();

        if (levels != null && levels.Count > 0)
        {
            LevelProgress.LevelCount = LevelCount;

            if (LevelProgress.TryConsumeRequestedLevel(out int requested))
            {
                // chosen from the Main Menu / Level Select (never past the furthest unlocked level)
                currentLevelIndex = Mathf.Clamp(requested, 0, Mathf.Min(LevelCount - 1, LevelProgress.Unlocked));
            }
            else if (saveProgress)
            {
                currentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, currentLevelIndex), 0, LevelCount - 1);
            }
        }

        HideCompletePanel();
        SetupNextButton();   // wired ONCE here
        SetupCurrentLevel();
    }

    private void SetupCurrentLevel()
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[LevelManager] No levels assigned!");
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= LevelCount)
        {
            Debug.LogWarning($"[LevelManager] Level index {currentLevelIndex} out of range! Resetting to Level 1.");
            currentLevelIndex = 0;
        }

        bool generated = currentLevelIndex >= SceneLevelCount;

        // Deactivate everything first, then show only the current level
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null) levels[i].SetActive(false);
        }
        if (proceduralHost != null) proceduralHost.gameObject.SetActive(false);

        GameObject currentLevel;
        if (generated)
        {
            WordSearchLevel host = GetProceduralHost();
            if (host == null) return;
            int size = GridSizeOf(host);
            ProceduralLevels.Data data = ProceduralLevels.Generate(currentLevelIndex, size, SceneLevelCount);
            host.Rebuild(CurrentLevelNumber, data.title, data.rows, data.words, data.cells);
            currentLevel = host.gameObject;
        }
        else
        {
            currentLevel = levels[currentLevelIndex];
        }
        if (currentLevel != null) currentLevel.SetActive(true);
        if (currentLevel == null)
        {
            Debug.LogWarning($"[LevelManager] Level at index {currentLevelIndex} is null!");
            return;
        }

        activeLevelScript = currentLevel.GetComponent<WordSearchLevel>();
        if (activeLevelScript == null)
        {
            Debug.LogWarning("[LevelManager] WordSearchLevel component not found on current level!");
            return;
        }

        // Give shared UI references to active level
        if (sharedCompletePanel != null)
        {
            activeLevelScript.SetSharedCompletePanel(sharedCompletePanel);
        }

        if (sharedNextButton != null)
        {
            activeLevelScript.SetSharedNextButton(sharedNextButton);
        }

        // Unlock the next level when this one is finished
        if (hookedLevel != null) hookedLevel.LevelCompleted -= OnActiveLevelCompleted;
        hookedLevel = activeLevelScript;
        hookedLevel.LevelCompleted += OnActiveLevelCompleted;

        // Initialize active level
        activeLevelScript.InitializeLevel();

        UpdateLevelText();

        if (saveProgress)
        {
            PlayerPrefs.SetInt(ProgressKey, currentLevelIndex);
            PlayerPrefs.Save();
        }

        LevelStarted?.Invoke(activeLevelScript);
        GameEvents.LevelStart(CurrentLevelNumber);

        Debug.Log($"[LevelManager] Active Level: {currentLevelIndex + 1} of {LevelCount} " +
                  $"(object: {currentLevel.name}, title: {activeLevelScript.LevelTitle})");
    }

    private void SetupNextButton()
    {
        if (sharedNextButton == null) return;

        sharedNextButton.onClick.RemoveAllListeners();
        sharedNextButton.onClick.AddListener(LoadNextLevel);
    }

    public void LoadNextLevel()
    {
        // Ignore a second call in the same frame (e.g. two listeners on the Next button)
        if (lastNextFrame == Time.frameCount)
        {
            return;
        }
        lastNextFrame = Time.frameCount;

        // Only move on when the current level is really complete
        if (requireCompletePanelVisible && sharedCompletePanel != null && !sharedCompletePanel.activeInHierarchy)
        {
            Debug.LogWarning("[LevelManager] Next ignored: level complete panel is not visible.");
            return;
        }

        // Post-level slot (win path only - Next exists only on the Level Complete panel).
        // Review is checked OUTSIDE the ad gate so an ad cooldown can never swallow a review milestone.
        int completedLevel = CurrentLevelNumber;
        bool reviewTookTheSlot = StoreReview.TryRequestInsteadOfAd(completedLevel);
        if (!reviewTookTheSlot && AdGate.AllowInterstitial(completedLevel) && MediationHandler.Instance != null)
        {
            MediationHandler.Instance.ShowInterstitial();
        }

        int nextIndex = currentLevelIndex + 1;

        if (nextIndex >= LevelCount)
        {
            if (!loopToFirstLevel)
            {
                Debug.Log("[LevelManager] All levels completed.");
                if (saveProgress)
                {
                    PlayerPrefs.SetInt(ProgressKey, 0);
                    PlayerPrefs.Save();
                }
                onAllLevelsCompleted?.Invoke();
                AllLevelsCompleted?.Invoke();
                return;
            }

            nextIndex = 0;
        }

        currentLevelIndex = nextIndex;
        HideCompletePanel();
        SetupCurrentLevel();
    }

    private void OnActiveLevelCompleted()
    {
        LevelProgress.MarkCompleted(currentLevelIndex);
        GameEvents.LevelComplete(CurrentLevelNumber);
        // ask for notification permission after a win (Android 13+ only shows the prompt a couple of times)
        GameNotifications.RequestPermissionIfNeeded();
    }

    private void OnDestroy()
    {
        if (hookedLevel != null) hookedLevel.LevelCompleted -= OnActiveLevelCompleted;
    }

    public void RestartCurrentLevel()
    {
        HideCompletePanel();
        SetupCurrentLevel();
    }

    private void UpdateLevelText()
    {
        if (levelText == null || !levelText.gameObject.activeInHierarchy) ResolveLevelText();
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, CurrentLevelNumber);
        }
    }

    /// <summary>
    /// Safety net: if the assigned text is missing or hidden (e.g. the theme builder wired a hidden copy),
    /// use the visible "leveltxt" label in the top bar instead.
    /// </summary>
    private void ResolveLevelText()
    {
        if (levelText != null && levelText.gameObject.activeInHierarchy) return;
        foreach (TextMeshProUGUI t in FindObjectsOfType<TextMeshProUGUI>())
        {
            if (t.gameObject.name == "leveltxt" && t.gameObject.activeInHierarchy)
            {
                levelText = t;
                return;
            }
        }
    }

    // ------------------------------------------------------------------ generated levels
    /// <summary>One reusable level object (a copy of the last scene level) that every generated level is built into.</summary>
    private WordSearchLevel GetProceduralHost()
    {
        if (proceduralHost != null) return proceduralHost;

        WordSearchLevel source = null;
        for (int i = levels.Count - 1; i >= 0 && source == null; i--)
        {
            WordSearchLevel l = levels[i] != null ? levels[i].GetComponent<WordSearchLevel>() : null;
            if (l != null && l.GetComponentInChildren<LetterTile>(true) != null && l.GetComponentInChildren<WordTarget>(true) != null)
                source = l;
        }
        if (source == null)
        {
            Debug.LogError("[LevelManager] No scene level to copy for generated levels.");
            return null;
        }

        bool wasActive = source.gameObject.activeSelf;
        source.gameObject.SetActive(false); // so the copy is created inactive
        GameObject copy = Instantiate(source.gameObject, source.transform.parent);
        source.gameObject.SetActive(wasActive);
        copy.name = "Generated_Level";
        proceduralHost = copy.GetComponent<WordSearchLevel>();
        return proceduralHost;
    }

    private static int GridSizeOf(WordSearchLevel level)
    {
        GridLayoutGroup grid = level.GetComponentInChildren<GridLayoutGroup>(true);
        if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            return grid.constraintCount;
        int tiles = level.GetComponentsInChildren<LetterTile>(true).Length;
        return Mathf.Max(6, Mathf.RoundToInt(Mathf.Sqrt(tiles)));
    }

    private void HideCompletePanel()
    {
        if (sharedCompletePanel != null)
        {
            sharedCompletePanel.SetActive(false);
        }
    }
}
