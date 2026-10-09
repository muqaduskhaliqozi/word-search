using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tooling for the ported features: notification settings/icons, schedule checks, safe-area math.
/// Notification settings are applied through reflection so this file compiles before the
/// com.unity.mobile.notifications package has been installed.
/// </summary>
public static class PortableFeaturesEditor
{
    private const string IconDir = "Assets/WordSearchTheme/Notifications/Icons/";

    // ------------------------------------------------------------------ notifications
    [MenuItem("Tools/Word Search/Notifications/Configure Icons + Settings")]
    public static void ConfigureNotifications()
    {
        Type settings = FindType("Unity.Notifications.NotificationSettings");
        if (settings == null)
        {
            Debug.LogWarning("[Notifications] package not installed yet (Window > Package Manager > Mobile Notifications). Run this menu again after it imports.");
            return;
        }
        try
        {
            Texture2D small = PrepareIcon(IconDir + "ws_small.png");
            Texture2D large = PrepareIcon(IconDir + "ws_large.png");
            BindingFlags st = BindingFlags.Public | BindingFlags.Static;
            Type android = settings.GetNestedType("AndroidSettings", BindingFlags.Public);
            Type iconType = FindType("Unity.Notifications.NotificationIconType");
            if (android != null && iconType != null)
            {
                SetStatic(android, "RescheduleOnDeviceRestart", true);   // a reboot drops alarms otherwise
                MethodInfo remove = android.GetMethods(st).FirstOrDefault(m => m.Name == "RemoveDrawableResource" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
                MethodInfo add = android.GetMethod("AddDrawableResource", st);
                if (remove != null) { TryInvoke(remove, GameNotifications.SmallIconId); TryInvoke(remove, GameNotifications.LargeIconId); }
                if (add != null)
                {
                    add.Invoke(null, new object[] { GameNotifications.SmallIconId, small, Enum.Parse(iconType, "Small") });
                    add.Invoke(null, new object[] { GameNotifications.LargeIconId, large, Enum.Parse(iconType, "Large") });
                }
            }
            Type ios = settings.GetNestedType("iOSSettings", BindingFlags.Public);
            if (ios != null) SetStatic(ios, "RequestAuthorizationOnAppLaunch", false); // ask after a win instead
            AssetDatabase.SaveAssets();
            Debug.Log("[Notifications] icons + settings configured (small=ws_small, large=ws_large, reschedule on restart ON, iOS launch prompt OFF)");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Notifications] could not configure automatically - set the icons in Project Settings > Mobile Notifications. " + e.Message);
        }
    }

    private static Texture2D PrepareIcon(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = true;                 // the package rejects icons without Read/Write
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    [MenuItem("Tools/Word Search/Notifications/Verify Schedule")]
    public static void VerifySchedule()
    {
        int problems = 0;
        for (int hour = 0; hour < 24; hour++)
        {
            DateTime now = DateTime.Today.AddHours(hour).AddMinutes(17);
            var list = GameNotifications.BuildSchedule(now, 7);
            if (list.Count < 2) { problems++; Debug.LogError($"[Notifications] {hour}h: fewer than 2 reminders"); }
            if (list.Count > GameNotifications.MaxScheduled) { problems++; Debug.LogError($"[Notifications] {hour}h: over the iOS cap"); }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].fireTime <= now) { problems++; Debug.LogError($"[Notifications] {hour}h: reminder {i} is not in the future"); }
                if (i > 0 && (list[i].fireTime - list[i - 1].fireTime).TotalHours < GameNotifications.MinSpacingHours) { problems++; Debug.LogError($"[Notifications] {hour}h: reminders {i - 1}/{i} too close"); }
                double h = (list[i].fireTime - now).TotalHours;
                if (Math.Abs(h / GameNotifications.IdleIntervalHours - Math.Round(h / GameNotifications.IdleIntervalHours)) > 1e-6) { problems++; Debug.LogError($"[Notifications] {hour}h: reminder {i} off the ladder"); }
            }
        }
        Debug.Log(problems == 0 ? "[Notifications] schedule OK for every hour of the day" : $"[Notifications] {problems} problem(s)");
    }

    [MenuItem("Tools/Word Search/Notifications/Dump Schedule")]
    public static void DumpSchedule()
    {
        foreach (var r in GameNotifications.BuildSchedule(DateTime.Now, 7))
            Debug.Log($"[Notifications] {r.fireTime:ddd HH:mm}  {r.title} - {r.body}");
    }

    // ------------------------------------------------------------------ safe area
    [MenuItem("Tools/Word Search/Verify Safe Area Math")]
    public static void VerifySafeArea()
    {
        int bad = 0;
        // portrait phone, 1080x2400, 100px notch on top, 60px gesture bar at the bottom
        bad += Check(new Rect(0, 60, 1080, 2240), 1080, 2400, new Vector2(0, 60f / 2400), new Vector2(1, 2300f / 2400));
        // landscape notch on the left - catches an x/y mix-up
        bad += Check(new Rect(100, 0, 2300, 1080), 2400, 1080, new Vector2(100f / 2400, 0), new Vector2(1, 1));
        // no cutout
        bad += Check(new Rect(0, 0, 1080, 1920), 1080, 1920, Vector2.zero, Vector2.one);
        Debug.Log(bad == 0 ? "[SafeArea] anchor math OK" : $"[SafeArea] {bad} case(s) wrong");
    }

    private static int Check(Rect area, int w, int h, Vector2 wantMin, Vector2 wantMax)
    {
        Vector2 min, max;
        SafeArea.Compute(area, w, h, out min, out max);
        bool ok = (min - wantMin).sqrMagnitude < 1e-8f && (max - wantMax).sqrMagnitude < 1e-8f;
        if (!ok) Debug.LogError($"[SafeArea] {area} on {w}x{h}: got {min}-{max}, want {wantMin}-{wantMax}");
        return ok ? 0 : 1;
    }

    // ------------------------------------------------------------------ helpers
    private static Type FindType(string fullName)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = a.GetType(fullName, false);
            if (t != null) return t;
        }
        return null;
    }

    private static void SetStatic(Type t, string prop, object value)
    {
        PropertyInfo p = t.GetProperty(prop, BindingFlags.Public | BindingFlags.Static);
        if (p != null && p.CanWrite) p.SetValue(null, value);
    }

    private static void TryInvoke(MethodInfo m, params object[] args)
    {
        try { m.Invoke(null, args); } catch (Exception) { }
    }
}
