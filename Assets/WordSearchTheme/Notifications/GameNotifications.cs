using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WS_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if WS_NOTIFICATIONS && UNITY_IOS
using Unity.Notifications.iOS;
#endif

/// <summary>
/// Re-engagement reminders scheduled on the device (no server / FCM).
/// Cancel-all-and-reschedule: going to the background lays down a fresh ladder from that moment,
/// coming back cancels it, so a returning player never sees a stale nudge.
/// Needs the com.unity.mobile.notifications package (WS_NOTIFICATIONS define comes from it);
/// without it everything here is a no-op.
/// adb logcat -s Unity | grep Notifications
/// </summary>
public class GameNotifications : MonoBehaviour
{
    public const string ChannelId = "ws_reminders";
    public const string SmallIconId = "ws_small";   // white-on-transparent silhouette (Project Settings > Mobile Notifications)
    public const string LargeIconId = "ws_large";

    public const int IdleIntervalHours = 6;   // nudge cadence since the last session
    public const int LadderSlots = 12;        // 12 x 6h = 3 days
    public const int MinSpacingHours = 3;     // closer than this reads as spam
    public const int MaxScheduled = 60;       // iOS silently drops past 64 pending

    // PlayerPrefs written by the game (kept as literals: this assembly cannot see the game's classes)
    private const string UnlockedKey = "WS_Unlocked";

    public struct Reminder
    {
        public DateTime fireTime;
        public string title;
        public string body;
    }

    private static readonly string[] Titles = { "Word Search", "New puzzle waiting!", "Time for a word break?", "Keep your streak of words going!" };
    private static readonly string[] Bodies =
    {
        "Level {0} is ready - can you find every word?",
        "A fresh grid of hidden words is waiting for you.",
        "Take a 2-minute break and find some words.",
        "Your next puzzle (level {0}) is just one tap away.",
    };

    private static GameNotifications instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("GameNotifications");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameNotifications>();
    }

    private void Start()
    {
#if WS_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidNotificationCenter.RegisterNotificationChannel(
                new AndroidNotificationChannel(ChannelId, "Reminders", "Puzzle reminders", Importance.Default));
        }
        catch (Exception e) { Debug.LogWarning("[Notifications] channel: " + e.Message); }
#elif !WS_NOTIFICATIONS
        Debug.Log("[Notifications] com.unity.mobile.notifications not installed - reminders disabled");
#endif
        CancelAll();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) { CancelAll(); ScheduleAll(DateTime.Now); }
        else CancelAll();
    }

    private void OnApplicationQuit()
    {
        CancelAll(); ScheduleAll(DateTime.Now); // Android often skips this; Pause is the reliable path
    }

    /// <summary>
    /// Pure: the reminders to schedule from <paramref name="now"/>. No Unity calls, so the editor
    /// "Verify Schedule" menu can sweep it without a device.
    /// </summary>
    public static List<Reminder> BuildSchedule(DateTime now, int nextLevel)
    {
        var list = new List<Reminder>();
        DateTime last = DateTime.MinValue;
        for (int i = 1; i <= LadderSlots && list.Count < MaxScheduled; i++)
        {
            DateTime t = now.AddHours(IdleIntervalHours * i);
            if (last != DateTime.MinValue && (t - last).TotalHours < MinSpacingHours) continue;
            int k = (i - 1) % Bodies.Length;
            list.Add(new Reminder
            {
                fireTime = t,
                title = Titles[(i - 1) % Titles.Length],
                body = string.Format(Bodies[k], nextLevel),
            });
            last = t;
        }
        return list;
    }

    private static void ScheduleAll(DateTime now)
    {
#if WS_NOTIFICATIONS && !UNITY_EDITOR
        int nextLevel = PlayerPrefs.GetInt(UnlockedKey, 0) + 1;
        List<Reminder> schedule = BuildSchedule(now, nextLevel);
        try
        {
            foreach (Reminder r in schedule)
            {
#if UNITY_ANDROID
                var n = new AndroidNotification
                {
                    Title = r.title,
                    Text = r.body,
                    FireTime = r.fireTime,
                    SmallIcon = SmallIconId,
                    LargeIcon = LargeIconId,
                };
                AndroidNotificationCenter.SendNotification(n, ChannelId);
#elif UNITY_IOS
                var n = new iOSNotification
                {
                    Identifier = "ws_" + r.fireTime.Ticks,
                    Title = r.title,
                    Body = r.body,
                    ShowInForeground = false,
                    Trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = r.fireTime - now, Repeats = false },
                };
                iOSNotificationCenter.ScheduleNotification(n);
#endif
            }
            Debug.Log($"[Notifications] scheduled {schedule.Count} reminder(s), first at {(schedule.Count > 0 ? schedule[0].fireTime.ToString("g") : "-")}");
        }
        catch (Exception e) { Debug.LogWarning("[Notifications] schedule failed: " + e.Message); }
#endif
    }

    private static void CancelAll()
    {
#if WS_NOTIFICATIONS && !UNITY_EDITOR
        try
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }
        catch (Exception e) { Debug.LogWarning("[Notifications] cancel failed: " + e.Message); }
#endif
    }

    /// <summary>
    /// Asks for notification permission if the OS still needs it (Android 13+ / iOS). Call after a win:
    /// Android only offers the prompt a couple of times per install. No local "already asked" flag on purpose -
    /// the OS tracks the real state and the request no-ops when granted.
    /// </summary>
    public static void RequestPermissionIfNeeded()
    {
#if WS_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var request = new PermissionRequest();
            Debug.Log("[Notifications] permission status: " + request.Status);
        }
        catch (Exception e) { Debug.LogWarning("[Notifications] permission: " + e.Message); }
#elif WS_NOTIFICATIONS && UNITY_IOS && !UNITY_EDITOR
        if (instance != null) instance.StartCoroutine(RequestIOS());
#endif
    }

#if WS_NOTIFICATIONS && UNITY_IOS && !UNITY_EDITOR
    private static IEnumerator RequestIOS()
    {
        using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, false))
        {
            while (!req.IsFinished) yield return null;
            Debug.Log("[Notifications] iOS authorization granted: " + req.Granted);
        }
    }
#endif
}
