using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
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

    [Header("Next Button Safety")]
    [Tooltip("Ignore Next unless the complete panel is visible (blocks stray calls).")]
    [SerializeField] private bool requireCompletePanelVisible = true;

    [Header("After Last Level")]
    [SerializeField] private bool loopToFirstLevel = false;
    [SerializeField] private UnityEvent onAllLevelsCompleted;

    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentLevelNumber => currentLevelIndex + 1;

    private WordSearchLevel activeLevelScript;
    private int lastNextFrame = -1;

    private void Start()
    {
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

        // Initialize active level
        activeLevelScript.InitializeLevel();

        UpdateLevelText();

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
            Debug.LogWarning("[LevelManager] LoadNextLevel called twice in the same frame - ignored. " +
                             "Check the Next button's OnClick list in the Inspector.");
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
        Debug.Log($"[LevelManager] LoadNextLevel: {currentLevelIndex + 1} -> {nextIndex + 1}");

        if (nextIndex >= levels.Count)
        {
            if (!loopToFirstLevel)
            {
                Debug.Log("[LevelManager] All levels completed.");
                onAllLevelsCompleted?.Invoke();
                return;
            }

            Debug.Log("[LevelManager] All levels completed! Looping back to Level 1.");
            nextIndex = 0;
        }

        currentLevelIndex = nextIndex;
        HideCompletePanel();
        SetupCurrentLevel();
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