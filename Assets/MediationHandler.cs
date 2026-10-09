using System;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using SolarEngine;
using static MaxSdkBase;
using static SolarEngine.Analytics;
using System.Collections.Generic;
using static MaxSdkCallbacks;
using System.Collections;

public class MediationHandler : MonoBehaviour
{
    #region  Variable Declaration
    public static MediationHandler Instance;
    DependencyStatus _dependencyStatus = DependencyStatus.UnavailableOther;
    [Header("TestIng Devices ")]
    [SerializeField]
    private string[] TestDevices;
    [SerializeField]
    private bool isTestMode = false;
    [Space]
    [Header("Banner Ads ")]
    [SerializeField]
    private bool isShowBanner = false;
    [SerializeField]
    private bool isShowBanner2 = false;
    [Space]
    [Header("Set Banner Position")]
    [SerializeField]
    private MaxSdkBase.BannerPosition _BannerPosition = MaxSdkBase.BannerPosition.TopCenter;
    [Space]
    [Header("Set Banner 2 Position")]
    [SerializeField]
    private MaxSdkBase.BannerPosition _BannerPosition2 = MaxSdkBase.BannerPosition.TopCenter;


    public delegate void RewardUserDelegate();
    private static RewardUserDelegate NotifyReward;

    public static bool interReward = false;

    [Header("AdUnit ID's")]
    [SerializeField]
    private string MaxSdkKey = "HvHk6ATpuzmHMN1fnTXLJBlPyUuntJd3y_GtvTcyuxlwsRx39uEzXp5BI05Sbyemc3dRz_CBmGmstLQoBNhPdq";
    [Space]
    [SerializeField]
    private string ApplovinBannerId = "bdf65775132f48c2";
    [Space]
    [SerializeField]
    private string ApplovinBannerId2 = "fd404905a977b2b7";

    [Space]
    [SerializeField]
    private string ApplovinInterstitialId = "c44c750a9844fcf7";
    [Space]
    [SerializeField]
    private string ApplovinRewarded = "e322476df6348b25";
    [Space]
    [Header("SolarEngine AppKey")]
    [SerializeField]
    private string AppKey = "d5a1fce47992f8bf";



    private bool _isBannerShowing;
    private bool _isBannerShowing2;
    private int _interstitialRetryAttempt;
    private int _rewardedRetryAttempt;

    // SDK readiness gates — ads must not load until all three are true
    private bool _maxSdkReady = false;
    private bool _solarEngineReady = false;
    private bool _firebaseReady = false;
    private bool _adLoadingStarted = false;

    // On devices with less than 2 GB RAM: skip Max, SolarEngine, and Firebase entirely.
    // Only AdMob interstitial (InterstitialAdManager) is used on these devices.
    private bool _isLowRamDevice = false;
    private const int LowRamThresholdMB = 2048;

    #endregion

    #region Start Initilization
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        Application.targetFrameRate = 60;
        interReward = false;

        _isLowRamDevice = SystemInfo.systemMemorySize < LowRamThresholdMB;

        if (_isLowRamDevice)
        {
#if UNITY_EDITOR
            Debug.LogError("Low RAM device (" + SystemInfo.systemMemorySize + " MB) — skipping Max, SolarEngine, Firebase. AdMob only.");
#endif
            // InterstitialAdManager loads its own ad in its Start(), nothing else needed here
            return;
        }

        // Init MaxApplovin — SolarEngine starts after MaxAppLovin signals ready
        InitializeMaxlovin();
    }
    #endregion

    #region Max Initilization

    public void InitializeMaxlovin()
    {
        MaxSdk.SetHasUserConsent(true);
#if UNITY_EDITOR
        Debug.LogError("Initial Max AppLovin");
#endif

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
#if UNITY_EDITOR
            Debug.LogError("MaxSDK InitSuccess");
#endif
            _maxSdkReady = true;

            // Start SolarEngine now that MaxAppLovin is fully ready.
            // Small delay lets the main thread settle before the next SDK init.
            Invoke(nameof(InitializeSolarEngine), 2f);

            // Do NOT load ads here — wait until all SDKs are ready
        };

        if (isTestMode && TestDevices != null)
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(TestDevices);
        MaxSdk.SetSdkKey(MaxSdkKey);
        MaxSdk.InitializeSdk();
    }

    // Called once all three SDKs have signalled ready.
    // Ads are loaded one at a time, each format separated by a delay.
    private void OnAllSDKsReady()
    {
        if (_adLoadingStarted) return;
        if (!_maxSdkReady || !_solarEngineReady || !_firebaseReady) return;
        if (Application.internetReachability == NetworkReachability.NotReachable) return;

        _adLoadingStarted = true;
#if UNITY_EDITOR
        Debug.LogError("All SDKs ready — starting ad cache");
#endif
        StartCoroutine(LoadAdsSequentially());
    }

    // Each ad format is loaded one at a time with a gap between them.
    // No ad loads during app open — this coroutine only runs after all SDKs are fully up.
    private IEnumerator LoadAdsSequentially()
    {
        // Buffer after SDK init before touching any ad network
        yield return new WaitForSeconds(2f);

        if (!IsRemoveAds())
        {
            LoadInterstitialAds();
            yield return new WaitForSeconds(3f);

            InitializeSmallBanner();
            yield return new WaitForSeconds(3f);

            InitializeSmallBanner2();
            yield return new WaitForSeconds(3f);
        }

        InitializeRewardedAds();
    }

    #endregion

    #region SolarEngine Initilization

    public void InitializeSolarEngine()
    {
#if UNITY_EDITOR
        Debug.LogError("Initial Solar Engine");
#endif

        Analytics.preInitSeSdk(AppKey);
        SEConfig seConfig = new SEConfig();
#if UNITY_EDITOR
        Debug.LogError("Function MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography Checking Region -----  " + MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography);
#endif
        if (MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography == MaxSdkBase.ConsentFlowUserGeography.Unknown ||
            MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography == MaxSdkBase.ConsentFlowUserGeography.Other)
        {
            seConfig.logEnabled = true;
            seConfig.isDebugModel = false;
            Analytics.initSeSdk(AppKey, seConfig);
#if UNITY_EDITOR
            Debug.LogError("This is not an EU User — Hide Consent Form");
#endif
        }
        else if (MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography == MaxSdkBase.ConsentFlowUserGeography.Gdpr)
        {
            seConfig.logEnabled = true;
            seConfig.isGDPRArea = true;
            seConfig.isDebugModel = true;
            Analytics.initSeSdk(AppKey, seConfig);
            Analytics.setGDPRArea(true);
#if UNITY_EDITOR
            Debug.LogError("This is an EU User — Show Consent Form");
#endif
        }
        _solarEngineReady = true;
#if UNITY_EDITOR
        Debug.LogError("SolarEngine ready");
#endif
        // Firebase init is the last step — OnAllSDKsReady is called from there
        FireBaseInitilization();
    }

    #endregion

    #region Banner1 Ad Methods
    public void InitializeSmallBanner()
    {
        // Attach Callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;


        LoadSmallBanner();
    }
    public void LoadSmallBanner()
    {
#if UNITY_EDITOR
        Debug.LogError("Initialized Banner1");
#endif

        MaxSdk.CreateBanner(ApplovinBannerId, _BannerPosition);
        MaxSdk.SetBannerExtraParameter(ApplovinBannerId, "adaptive_banner", "true");
        MaxSdk.SetBannerWidth(ApplovinBannerId, 300f);
    }

    public void ShowSmallBanner()
    {
        if (_isLowRamDevice) return;
        if (Application.internetReachability != NetworkReachability.NotReachable && IsRemoveAds() == false)
        {
            if (_isBannerShowing)
            {
                MaxSdk.ShowBanner(ApplovinBannerId);
                MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
                MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
#if UNITY_EDITOR
                Debug.LogError("Show Banner1");
#endif
            }
        }
    }

    public void HideSmallBanner()
    {
        MaxSdk.HideBanner(ApplovinBannerId);
#if UNITY_EDITOR
        Debug.LogError("Hide Banner1");
#endif
    }

    public void ResetBannerPos(MaxSdk.BannerPosition pos)
    {
        MaxSdk.HideBanner(ApplovinBannerId);
        MaxSdk.UpdateBannerPosition(ApplovinBannerId, pos);
        MaxSdk.ShowBanner(ApplovinBannerId);
#if UNITY_EDITOR
        Debug.LogError("Show Banner1 on New Position");
#endif
    }
    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner1 ad loaded");
#endif
        _isBannerShowing = true;
        if (isShowBanner)
        {
            ShowSmallBanner();
        }
    }

    private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner1 ad failed to load with error code: " + errorInfo.Code);
#endif
        _isBannerShowing = false;
    }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner1 ad clicked");
#endif
        TrackAdClick(adUnitId, adInfo, 5, "BannerAd");
        Debug.LogError("TrackAdClick BannerAd 1 ");
        MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent;
    }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR

        Debug.LogError("Banner1 ad revenue paid");
#endif
        TrackAdImpression(adUnitId, adInfo, 5, "BannerAd");
        Debug.LogError("TrackAdImpression BannerAd 1 ");
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent;
    }

    #endregion

    #region Banner2 Ad Methods
    public void InitializeSmallBanner2()
    {
        // Attach Callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent2;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent2;

        LoadSmallBanner2();
    }
    public void LoadSmallBanner2()
    {
#if UNITY_EDITOR
        Debug.LogError("Initialized Banner2");
#endif

        MaxSdk.CreateBanner(ApplovinBannerId2, _BannerPosition2);
        MaxSdk.SetBannerExtraParameter(ApplovinBannerId2, "adaptive_banner", "true");
        // Fix: was incorrectly using ApplovinBannerId (Banner 1) instead of ApplovinBannerId2
        MaxSdk.SetBannerWidth(ApplovinBannerId2, 300f);
    }

    public void ShowSmallBanner2()
    {
        if (_isLowRamDevice) return;
        if (Application.internetReachability != NetworkReachability.NotReachable && IsRemoveAds() == false)
        {
            if (_isBannerShowing2)
            {
                MaxSdk.ShowBanner(ApplovinBannerId2);
                MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent2;
                MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent2;
#if UNITY_EDITOR
                Debug.LogError("Show Banner2");
#endif
            }
        }
    }

    public void HideSmallBanner2()
    {
        MaxSdk.HideBanner(ApplovinBannerId2);
#if UNITY_EDITOR
        Debug.LogError("Hide Banner2");
#endif
    }

    public void ResetBannerPos2(MaxSdk.BannerPosition pos)
    {
        MaxSdk.HideBanner(ApplovinBannerId2);
        MaxSdk.UpdateBannerPosition(ApplovinBannerId2, pos);
        MaxSdk.ShowBanner(ApplovinBannerId2);
#if UNITY_EDITOR
        Debug.LogError("Show Banner2 on New Position");
#endif
    }



    private void OnBannerAdLoadedEvent2(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner2 ad loaded");
#endif
        _isBannerShowing2 = true;
        if (isShowBanner2)
        {
            ShowSmallBanner2();
        }
    }

    private void OnBannerAdFailedEvent2(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner2 ad failed to load with error code: " + errorInfo.Code);
#endif
        _isBannerShowing2 = false;
    }

    private void OnBannerAdClickedEvent2(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Banner2 ad clicked");
#endif
        TrackAdClick(adUnitId, adInfo, 5, "BannerAd2");
        Debug.LogError("TrackAdClick BannerAd 2 ");
        MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent2;
    }

    private void OnBannerAdRevenuePaidEvent2(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR

        Debug.LogError("Banner2 ad revenue paid");
#endif
        TrackAdImpression(adUnitId, adInfo, 5, "BannerAd2");
        Debug.LogError("TrackAdImpression BannerAd 2 ");
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent2;
    }

    #endregion


    #region Interstitial Ad Methods


    public void LoadInterstitialAds()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable && IsRemoveAds() == false && !IsInterstitialAdReady())
        {
#if UNITY_EDITOR
            Debug.LogError("Load Interstitial AppLovin");
#endif
            // Unsubscribe first to prevent duplicate handlers on retry calls
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= InterstitialFailedToDisplayEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;

            MaxSdk.LoadInterstitial(ApplovinInterstitialId);
        }
    }

    public void ShowInterstitial()
    {
        if (IsRemoveAds()) return;
        if (Application.internetReachability == NetworkReachability.NotReachable) return;

        if (_isLowRamDevice)
        {
            // InterstitialAdManager.Instance.ShowAd();
            return;
        }

        if (IsInterstitialAdReady())
        {
#if UNITY_EDITOR
            Debug.LogError("Inters AppLovin Will Display");
#endif
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdk.ShowInterstitial(ApplovinInterstitialId, "V_" +Application.version);
        }
        else
        {
            // InterstitialAdManager.Instance.ShowAd();
            LoadInterstitialAds();
        }
    }


    public bool IsInterstitialAdReady() => MaxSdk.IsInterstitialReady(ApplovinInterstitialId);

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Interstitial loaded");
#endif

        _interstitialRetryAttempt = 0;
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
    }
    private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Interstitial ad displayed");
#endif
        string network = adInfo.NetworkName;

        if (network.Contains("Google") || network.Contains("AdMob"))
        {
            //    AppOpenAdManager.Instance.PauseAppOpenAds();
        }

        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
    }


    private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {

        _interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
#if UNITY_EDITOR
        Debug.LogError("Interstitial failed to load with error code: " + errorInfo.Code);
#endif
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialFailedEvent;
        Invoke(nameof(LoadInterstitialAds), (float)retryDelay);


    }
    private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= InterstitialFailedToDisplayEvent;
#if UNITY_EDITOR
        Debug.LogError("Interstitial failed to display with error code: " + errorInfo.Code);
#endif
        LoadInterstitialAds();

    }

    private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {

#if UNITY_EDITOR
        Debug.LogError("Interstitial dismissed");
#endif
        Invoke(nameof(LoadInterstitialAds), 1f);

        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialDismissedEvent;
    }


    private void OnInterstitialAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Interstitial Ad is Clicked");
#endif
        TrackAdClick(adUnitId, adInfo, 3, "InterstitialAd");
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialAdClickedEvent;
    }
    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Interstitial revenue paid");
#endif
        TrackAdImpression(adUnitId, adInfo, 3, "InterstitialAd");
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialRevenuePaidEvent;

    }


    #endregion

    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
#if UNITY_EDITOR
        Debug.LogError("Initial Reward AppLovin");
#endif

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        LoadRewardedVideo();
    }

    public void LoadRewardedVideo()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
#if UNITY_EDITOR
            Debug.LogError("Load Reward AppLovin");
#endif
            MaxSdk.LoadRewardedAd(ApplovinRewarded);
        }
    }

    public void ShowRewardedVideo(RewardUserDelegate _delegate)
    {
        if (_isLowRamDevice) return;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
#if UNITY_EDITOR
            Debug.LogError("Rewarded  AppLovin Show Call");
#endif
            if (IsRewardedAdReady())
            {
#if UNITY_EDITOR
                Debug.LogError("Rewarded  AppLovin Will Display");
#endif
                interReward = true;
                NotifyReward = _delegate;
                MaxSdk.ShowRewardedAd(ApplovinRewarded);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("Rewarded Inters AppLovin Not Loaded");
#endif
                LoadRewardedVideo();
            }
        }
    }

    public bool IsRewardedAdReady() => MaxSdk.IsRewardedAdReady(ApplovinRewarded);

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad loaded");
#endif

        _rewardedRetryAttempt = 0;
    }


    private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        _rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad failed to load with error code: " + errorInfo.Code);
#endif

        Invoke(nameof(LoadRewardedVideo), (float)retryDelay);

    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad failed to display with error code: " + errorInfo.Code);
#endif
        LoadRewardedVideo();
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad clicked");
#endif
        TrackAdClick(adUnitId, adInfo, 9, "RewardedAd");
    }

    private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad dismissed");
#endif
        // Fix: reset interReward flag so subsequent rewarded ads behave correctly
        interReward = false;
        LoadRewardedVideo();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad received reward");
#endif
        Time.timeScale = 1f;
        if (NotifyReward != null)
        {
            NotifyReward.Invoke();
            NotifyReward = null;
        }
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_EDITOR
        Debug.LogError("Rewarded ad revenue paid");
#endif

        TrackAdImpression(adUnitId, adInfo, 9, "RewardedAd");


    }

    #endregion


    #region RemoveAds
    private bool IsRemoveAds()
    {
        bool removed = PlayerPrefs.GetInt("RemoveAds") == 1;
#if UNITY_EDITOR
        if (removed) Debug.LogError("Purchase Remove ADs");
#endif
        return removed;
    }
    #endregion

    #region Tracking
    public void TrackAdClick(string adUnitId, MaxSdkBase.AdInfo adInfo, int adtype, string AdName)
    {
        if (_isLowRamDevice) return;
        if (adInfo == null || string.IsNullOrEmpty(adUnitId))
            return;
        AdClickAttributes adTrackAttributes = new AdClickAttributes
        {
            ad_platform = adInfo.NetworkName,
            mediation_platform = "Max",
            ad_id = adUnitId,
            ad_type = adtype,
            checkId = "123"
        };

        SolarEngine.Analytics.trackAdClick(adTrackAttributes);

    }

    public void TrackAdImpression(string adUnitId, MaxSdkBase.AdInfo adInfo, int adtype, string AdName)
    {
        if (_isLowRamDevice) return;
        if (adInfo == null)
            return;

        ImpressionAttributes impressionAttributes = new ImpressionAttributes
        {
            ad_platform = adInfo.NetworkName,
            mediation_platform = "Max",
            ad_id = adUnitId,
            ad_type = adtype,
            ad_ecpm = (adInfo.Revenue * 1000.00f),
            currency_type = "USD",
            is_rendered = true
        };
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);

        FireBaseTrackAdRevenue(adInfo);


    }
    public void OnPanelOpened(string panelName)
    {
        if (_isLowRamDevice) return;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            Dictionary<string, object> eventParams = new Dictionary<string, object>
         {
            { "panel_name", panelName },
            { "action", "opened" }
         };
            SolarEngine.Analytics.track("panel_interaction", eventParams);
        }
    }

    public void OnPanelClosed(string panelName)
    {
        if (_isLowRamDevice) return;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            Dictionary<string, object> eventParams = new Dictionary<string, object>
         {
            { "panel_name", panelName },

            { "action", "closed" }
         };

            SolarEngine.Analytics.track("panel_interaction", eventParams);
        }
    }

    #endregion

    #region Firebase Initilization

    private void FireBaseInitilization()
    {
        // ContinueWithOnMainThread ensures the callback runs on Unity's main thread,
        // which is required before calling StartCoroutine or any Unity API.
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            _dependencyStatus = task.Result;
            if (_dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                var app = Firebase.FirebaseApp.DefaultInstance;
                RemoteValues.Instance.Initialize();
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", _dependencyStatus));
#endif
            }

            // Mark Firebase ready and attempt to start ad loading.
            // OnAllSDKsReady will only proceed once Max and SolarEngine are also done.
            _firebaseReady = true;
#if UNITY_EDITOR
            Debug.LogError("Firebase ready");
#endif
            OnAllSDKsReady();
        });
    }

    public void LogEvent(string name)
    {
        if (_isLowRamDevice) return;
        if (_dependencyStatus == Firebase.DependencyStatus.Available)
            FirebaseAnalytics.LogEvent(name);
    }

    private void FireBaseTrackAdRevenue(MaxSdkBase.AdInfo impressionData)
    {
        double revenue = impressionData.Revenue;
        var impressionParameters = new[] {
        new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
        new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
        new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
        new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
        new Firebase.Analytics.Parameter("value", revenue),
        new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
        };
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
    }

    #endregion


}
