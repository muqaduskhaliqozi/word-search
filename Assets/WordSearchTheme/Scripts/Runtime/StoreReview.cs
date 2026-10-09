using System.Collections;
using UnityEngine;

/// <summary>
/// Native store review (Google Play In-App Review / iOS SKStoreReviewController) after milestone levels,
/// taking the post-level interstitial slot. Neither store says whether a dialog was shown (both rate-limit
/// hard), so this is a request, never an event. Nothing appears in the Editor.
/// Android: Google's Play In-App Review library (com.google.android.play:review, declared in
/// WordSearchTheme/Editor/ReviewDependencies.xml and pulled in by External Dependency Manager), called over JNI.
/// If the library is missing from a build, the request logs a warning and does nothing.
/// adb logcat -s Unity | grep Review
/// </summary>
public class StoreReview : MonoBehaviour
{
    [Tooltip("1-based level numbers after which the review prompt is requested.")]
    [SerializeField] private int[] reviewAfterLevels = { 5 };
    [SerializeField] private float delaySeconds = 0.4f;

    private const string LastLevelKey = "WS_ReviewLastLevel";
    private const string AdSkipUsedKey = "WS_ReviewAdSkipUsed";

    private static StoreReview instance;
    private bool inFlight;

    private static StoreReview Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<StoreReview>();
                if (instance == null)
                {
                    GameObject go = new GameObject("StoreReview");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<StoreReview>();
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Call right after a WIN, before showing the post-level interstitial.
    /// Returns true when the ad should be skipped (only on the first genuine attempt; later milestones
    /// still ask but let the ad through, because a quota-blocked prompt would otherwise leave neither).
    /// </summary>
    public static bool TryRequestInsteadOfAd(int completedLevel) => Instance.TryRequest(completedLevel);

    private bool TryRequest(int level)
    {
        if (inFlight) return false;
        int lastAsked = PlayerPrefs.GetInt(LastLevelKey, 0);
        int milestone = 0;
        foreach (int m in reviewAfterLevels)
            if (level >= m && m > lastAsked && m > milestone) milestone = m; // replays never re-trigger
        if (milestone == 0) return false;

        PlayerPrefs.SetInt(LastLevelKey, milestone);
        bool skipAd = PlayerPrefs.GetInt(AdSkipUsedKey, 0) == 0;
        if (skipAd) PlayerPrefs.SetInt(AdSkipUsedKey, 1);
        PlayerPrefs.Save();

        Debug.Log($"[Review] milestone {milestone} reached at level {level} - requesting review (ad skipped: {skipAd})");
        GameEvents.ReviewRequested(level);
        StartCoroutine(RequestRoutine());
        return skipAd;
    }

    private IEnumerator RequestRoutine()
    {
        inFlight = true;
        yield return new WaitForSecondsRealtime(delaySeconds);
#if UNITY_ANDROID && !UNITY_EDITOR
        bool done = false;
        try
        {
            AndroidJavaObject activity;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject manager;
            using (var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory"))
                manager = factory.CallStatic<AndroidJavaObject>("create", activity);
            // request + launch back to back: ReviewInfo expires
            AndroidJavaObject request = manager.Call<AndroidJavaObject>("requestReviewFlow");
            request.Call<AndroidJavaObject>("addOnCompleteListener", new TaskListener(task =>
            {
                try
                {
                    if (!task.Call<bool>("isSuccessful")) { Debug.Log("[Review] request failed"); done = true; return; }
                    AndroidJavaObject info = task.Call<AndroidJavaObject>("getResult");
                    AndroidJavaObject launch = manager.Call<AndroidJavaObject>("launchReviewFlow", activity, info);
                    launch.Call<AndroidJavaObject>("addOnCompleteListener", new TaskListener(t2 =>
                    {
                        Debug.Log("[Review] flow finished (does NOT mean a dialog was shown)");
                        done = true;
                    }));
                }
                catch (System.Exception e) { Debug.LogWarning("[Review] launch failed: " + e.Message); done = true; }
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Review] Play In-App Review library missing from the build? Run Android Resolver > Force Resolve. " + e.Message);
            done = true;
        }
        float timeout = 15f;
        while (!done && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
#elif UNITY_IOS && !UNITY_EDITOR
        bool ok = UnityEngine.iOS.Device.RequestStoreReview();
        Debug.Log("[Review] iOS request sent: " + ok);
#else
        Debug.Log("[Review] (Editor) a review prompt would be requested here");
#endif
        inFlight = false;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>com.google.android.gms.tasks.OnCompleteListener</summary>
    private class TaskListener : AndroidJavaProxy
    {
        private readonly System.Action<AndroidJavaObject> callback;
        public TaskListener(System.Action<AndroidJavaObject> cb) : base("com.google.android.gms.tasks.OnCompleteListener") { callback = cb; }
        public void onComplete(AndroidJavaObject task) { callback?.Invoke(task); }
    }
#endif

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Word Search/Store Review/Reset Review State")]
    private static void ResetState()
    {
        PlayerPrefs.DeleteKey(LastLevelKey); PlayerPrefs.DeleteKey(AdSkipUsedKey); PlayerPrefs.Save();
        Debug.Log("[Review] state reset");
    }
#endif
}
