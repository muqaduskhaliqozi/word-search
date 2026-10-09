using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GamePlay";
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private string levelLabelFormat = "LEVEL {0}";

    [Header("Level Select")]
    [SerializeField] private Button levelsButton;
    [SerializeField] private LevelSelectPanel levelSelect;

    private void Start()
    {
        if (levelLabel != null)
        {
            levelLabel.text = string.Format(levelLabelFormat, NextLevelIndex() + 1);
        }

        if (levelsButton != null && levelSelect != null)
        {
            levelsButton.onClick.AddListener(levelSelect.Open);
        }

        if (levelSelect != null) levelSelect.gameObject.SetActive(false);
    }

    /// <summary>The level PLAY starts: the furthest unlocked one (or the saved one when everything is done).</summary>
    private static int NextLevelIndex()
    {
        int count = LevelProgress.LevelCount;
        int unlocked = LevelProgress.Unlocked;
        if (count > 0 && unlocked >= count)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(LevelManager.ProgressKey, 0), 0, count - 1);
        }
        return count > 0 ? Mathf.Clamp(unlocked, 0, count - 1) : unlocked;
    }

    public void PlayGame()
    {
        if (SceneFader.IsBusy) return;
        SfxPlayer.Play(SfxPlayer.Sfx.Popup);
        LevelProgress.RequestLevel(NextLevelIndex());
        SceneFader.LoadScene(gameSceneName);
    }
}
