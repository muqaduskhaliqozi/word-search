using UnityEngine;

/// <summary>
/// Saved level progress shared by the Main Menu (level select) and GamePlay.
/// "Unlocked" = index of the furthest level the player may play (all before it are completed).
/// </summary>
public static class LevelProgress
{
    public const string UnlockedKey = "WS_Unlocked";
    public const string CountKey = "WS_LevelCount";
    public const string RequestKey = "WS_PlayLevel";

    public static int Unlocked
    {
        get => PlayerPrefs.GetInt(UnlockedKey, PlayerPrefs.GetInt(LevelManager.ProgressKey, 0));
        set { PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    /// <summary>Total levels in GamePlay (hand-made + generated, see ProceduralLevels.TotalLevels).</summary>
    public static int LevelCount
    {
        get => Mathf.Max(PlayerPrefs.GetInt(CountKey, 0), ProceduralLevels.TotalLevels);
        set { PlayerPrefs.SetInt(CountKey, value); PlayerPrefs.Save(); }
    }

    public static bool IsCompleted(int index) => index < Unlocked;
    public static bool IsLocked(int index) => index > Unlocked;

    public static void MarkCompleted(int index)
    {
        if (index + 1 > Unlocked) Unlocked = index + 1;
    }

    /// <summary>Ask GamePlay to start on a specific level (consumed once by LevelManager).</summary>
    public static void RequestLevel(int index)
    {
        PlayerPrefs.SetInt(RequestKey, index);
        PlayerPrefs.Save();
    }

    public static bool TryConsumeRequestedLevel(out int index)
    {
        index = PlayerPrefs.GetInt(RequestKey, -1);
        if (index < 0) return false;
        PlayerPrefs.DeleteKey(RequestKey);
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.DeleteKey(RequestKey);
        PlayerPrefs.DeleteKey(LevelManager.ProgressKey);
        PlayerPrefs.Save();
    }
}
