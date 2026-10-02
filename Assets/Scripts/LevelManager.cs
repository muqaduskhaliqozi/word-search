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
    public int LevelCount => levels != null ? levels.Count : 0;
    public WordSearchLevel ActiveLevel => activeLevelScript;

    /// <summary>Raised every time a level is (re)started.</summary>
    public event Action<WordSearchLevel> LevelStarted;
    /// <summary>Raised after the last level when not looping.</summary>
    public event Action AllLevelsCompleted;

    private WordSearchLevel activeLevelScript;
    private WordSearchLevel hookedLevel;
    private int lastNextFrame = -1;

    private void Start()
    {
        if (levels != null && levels.Count > 0)
        {
            LevelProgress.LevelCount = levels.Count;

            if (LevelProgress.TryConsumeRequestedLevel(out int requested))
            {
                // chosen from the Main Menu / Level Select (never past the furthest unlocked level)
                currentLevelIndex = Mathf.Clamp(requested, 0, Mathf.Min(levels.Count - 1, LevelProgress.Unlocked));
            }
            else if (saveProgress)
            {
                currentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, currentLevelIndex), 0, levels.Count - 1);
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

        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
        {
            Debug.LogWarning($"[LevelManager] Level index {currentLevelIndex} out of range! Resetting to Level 1.");
            currentLevelIndex = 0;
        }

        // Activate only the current level GameObject
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                levels[i].SetActive(i == currentLevelIndex);
            }
        }

        GameObject currentLevel = levels[currentLevelIndex];
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

        Debug.Log($"[LevelManager] Active Level: {currentLevelIndex + 1} of {levels.Count} " +
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

        int nextIndex = currentLevelIndex + 1;

        if (nextIndex >= levels.Count)
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
        if (levelText != null)
        {
            levelText.text = string.Format(levelTextFormat, CurrentLevelNumber);
        }
    }

    private void HideCompletePanel()
    {
        if (sharedCompletePanel != null)
        {
            sharedCompletePanel.SetActive(false);
        }
    }
}
