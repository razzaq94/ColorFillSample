using UnityEngine;
using System.Collections;
using GoogleMobileAds.Api;
using System;

public class AdManager_Admob : MonoBehaviour
{
    private InterstitialAd interstitial;
    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool IsInit = false;

    public const string AdRemovedKey = "IsAdsRemoved";

    public bool IsAdsEnabled
    {
        get => PlayerPrefs.GetInt(AdRemovedKey, 0) == 0;
        set
        {
            PlayerPrefs.SetInt(AdRemovedKey, value ? 0 : 1);
        }
    }

    [SerializeField] string _androidGameId = "ca-app-pub-3940256099942544~3347511713";
    [SerializeField] string _iOSGameId = "ca-app-pub-3940256099942544~1458002511";

    private string _gameId;

    [SerializeField] AdID _bannerID = new AdID() { Android_ID = "ca-app-pub-3940256099942544/6300978111", IOS_ID = "ca-app-pub-3940256099942544/2934735716" };
    [SerializeField] AdID _interstitialID = new AdID() { Android_ID = "ca-app-pub-3940256099942544/1033173712", IOS_ID = "ca-app-pub-3940256099942544/4411468910" };
    [SerializeField] AdID _rewardedVideoID = new AdID() { Android_ID = "ca-app-pub-3940256099942544/5224354917", IOS_ID = "ca-app-pub-3940256099942544/1712485313" };
    [SerializeField] AdPosition _bannerPosition = AdPosition.Bottom;

    public event Action OnRewardedAdClosed;

    public static AdManager_Admob instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAds();
    }

    #region Initialize

    public void InitializeAds()
    {
        if (IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer) ? _iOSGameId : _androidGameId;
        MobileAds.Initialize(OnInitializationComplete);
    }

    public void OnInitializationComplete(InitializationStatus status)
    {
        Debug.Log("Admob Ads initialization complete.");

        IsInit = true;

        //LoadBannerAd();
        LoadInterstitialAd();
        LoadRewardedVideoAd();
    }

    #endregion

    #region Banner Ads

    public void LoadBannerAd()
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        string _adUnitId = getAdID(AdType.Banner);
        this.bannerView = new BannerView(_adUnitId, AdSize.Banner, _bannerPosition);

        // Raised when an ad is loaded into the banner view.
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + bannerView.GetResponseInfo());
        };
        // Raised when an ad fails to load into the banner view.
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            LoadBannerAd();
            Debug.LogError("Banner view failed to load an ad with error : "
                + error);
        };
        // Raised when the ad is estimated to have earned money.
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");
        };

        AdRequest request = new AdRequest();
        this.bannerView.LoadAd(request);
    }

    public void HandleOnBannerAdLoaded(object sender, EventArgs args)
    {
        Debug.Log("Banner loaded");
        ShowBannerAd();
    }

    void ShowBannerAd()
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        // Show the loaded Banner Ad Unit:
        bannerView.Show();
    }

    void HideBannerAd()
    {
        // Hide the banner:
        if (!IsInit)
            return;

        if (bannerView != null)
            bannerView.Destroy();

        bannerView = null;
    }

    #endregion

    #region Interstitial Ads

    public void LoadInterstitialAd()
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        string _adUnitId = getAdID(AdType.Interstitial);
        if (_adUnitId == null)
        {
            Debug.Log("Ads Not supported on this platform");
            return;
        }

        // Clean up the old ad before loading a new one.
        if (this.interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        AdRequest request = new AdRequest();

        InterstitialAd.Load(_adUnitId, request,
          (InterstitialAd ad, LoadAdError error) =>
          {
              // if error is not null, the load request failed.
              if (error != null || ad == null)
              {
                  Debug.LogError("interstitial ad failed to load an ad " +
                                 "with error : " + error);
                  return;
              }

              Debug.Log("Interstitial ad loaded with response : "
                        + ad.GetResponseInfo());

              this.interstitial = ad;

              RegisternterstitialEventHandlers(this.interstitial);
          });
    }

    private void RegisternterstitialEventHandlers(InterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);
            LoadInterstitialAd();
        };
    }

    public bool IsInterstitialAdLoaded()
    {
        return IsInit && IsAdsEnabled && interstitial != null && interstitial.CanShowAd();
    }

    public void ShowInterstitialAd()
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        if (IsInterstitialAdLoaded())
            interstitial.Show();
        else
            LoadInterstitialAd();
    }

    #endregion

    #region Rewarded Video Ads

    public void LoadRewardedVideoAd()
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        string _adUnitId = getAdID(AdType.Rewarded);
        if (_adUnitId == null)
        {
            Debug.Log("Ads Not supported on this platform");
            return;
        }

        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_adUnitId, request,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                rewardedAd = ad;
                RegisterRewardedEventHandlers(rewardedAd);
            });
    }

    private void RegisterRewardedEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            OnRewardedAdClosed?.Invoke();
            LoadRewardedVideoAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
            LoadRewardedVideoAd();
        };

    }

    public bool IsRewardedAdLoaded()
    {
        return IsInit && IsAdsEnabled && rewardedAd != null && rewardedAd.CanShowAd();
    }

    public void ShowRewardedVideoAd(Action onRewarded = null)
    {
        if (!IsInit)
            return;

        if (!IsAdsEnabled)
            return;

        if (IsRewardedAdLoaded())
            this.rewardedAd.Show((reward) =>
            {
                onRewarded?.Invoke();
            });
        else
            LoadRewardedVideoAd();
    }

    #endregion

    #region Utility Functions

    string getAdID(AdType adType)
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (adType == AdType.Banner)
                return _bannerID.IOS_ID;
            else if (adType == AdType.Interstitial)
                return _interstitialID.IOS_ID;
            else if (adType == AdType.Rewarded)
                return _rewardedVideoID.IOS_ID;
            else
                return null;
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            if (adType == AdType.Banner)
                return _bannerID.Android_ID;
            else if (adType == AdType.Interstitial)
                return _interstitialID.Android_ID;
            else if (adType == AdType.Rewarded)
                return _rewardedVideoID.Android_ID;
            else
                return null;
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
        {
            if (adType == AdType.Banner)
                return _bannerID.Android_ID != null ? _bannerID.Android_ID : _bannerID.IOS_ID;
            else if (adType == AdType.Interstitial)
                return _interstitialID.Android_ID != null ? _interstitialID.Android_ID : _interstitialID.IOS_ID;
            else if (adType == AdType.Rewarded)
                return _rewardedVideoID.Android_ID != null ? _rewardedVideoID.Android_ID : _rewardedVideoID.IOS_ID;
            else
                return null;
        }
        else
        {
            return null;
        }
    }

    public void OnAdsRemoved()
    {
        HideBannerAd();
    }

    #endregion
}

[Serializable]
public class AdID
{
    public string Android_ID;
    public string IOS_ID;
}

[Serializable]
public enum AdType
{
    Banner, Interstitial, Rewarded
}