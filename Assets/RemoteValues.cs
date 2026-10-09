using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RemoteValues : MonoBehaviour
{



    public static RemoteValues Instance;
    public bool is_initialized;
    System.Collections.Generic.Dictionary<string, object> defaults;

void Awake()
    {
Instance = this;
        DontDestroyOnLoad(gameObject);

       
    }

    public void Initialize()
    {

        if (!is_initialized)
        {
            defaults = new System.Collections.Generic.Dictionary<string, object>();

            defaults.Add("PreGameplayMhAds", true);
        
            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
            FetchDataAsync();
           is_initialized = true;
        }

    }
  

    private Task FetchDataAsync ()
    {
        log("Fetching data...");
        System.Threading.Tasks.Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
            System.TimeSpan.FromMinutes(1));
        return fetchTask.ContinueWith(FetchComplete);
    }


    private void FetchComplete (Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            log("Fetch canceled.");
        }
        else if (fetchTask.IsFaulted)
        {
            log("Fetch encountered an error.");
        }
        else if (fetchTask.IsCompleted)
        {
            log("Fetch completed successfully!");
        }

        var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

                log(String.Format("Remote data loaded and ready (last fetch time {0}).", info.FetchTime));
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case Firebase.RemoteConfig.FetchFailureReason.Error:
                        log("Fetch failed for unknown reason");
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                        log("Fetch throttled until " + info.ThrottledEndTime);
                        break;
                }
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Pending:
                log("Latest Fetch call still pending.");
                break;
        }
    }




    public Firebase.RemoteConfig.ConfigValue getRemoteValue (string key)
    {
        log("Getting Remote Value for Key: " + key + " Value:" + Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue);
        return Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key);

    }

   
   
   
    public bool IsPreGameplayMhAds()
    {
        if (is_initialized)
        {
            return getRemoteValue("PreGameplayMhAds").BooleanValue;
        }
        return true;

    }

    
    private static void log (string msg)
    {
        Debug.Log("RemoteConfig::" + msg);
    }


}
