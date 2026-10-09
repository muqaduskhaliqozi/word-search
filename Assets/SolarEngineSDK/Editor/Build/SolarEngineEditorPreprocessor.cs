using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SolarEngine.Build
{

#if UNITY_2018_1_OR_NEWER
    public class SolarEngineEditorPreprocessor : IPreprocessBuildWithReport
#else
    public class SolarEngineEditorPreprocessor : IPreprocessBuild
#endif
    {
        public int callbackOrder => -1;
#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            OnPreprocessBuild(report.summary.platform, string.Empty);
        }
#endif

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.Android || target == BuildTarget.iOS)
            {

#if UNITY_ANDROID&&!SOLARENGINE_BYTEDANCE&&!SOLARENGINE_BYTEDANCE_CLOUD&&!SOLARENGINE_BYTEDANCE_STARK
                RunPostProcessTasksAndroid();
                CheckConfusion();
#endif 
#if (UNITY_ANDROID||UNITY_IOS)&&!SOLARENGINE_BYTEDANCE&&!SOLARENGINE_BYTEDANCE_CLOUD&&!SOLARENGINE_BYTEDANCE_STARK
                if (ApplySetting.checkApplyWithAndroidPackage())
                {

                    Debug.Log(string.Format(SolorEngine) + "  SolarEngine is  apply ");

                }
                else
                {

                    // Debug.Log(string.Format(SolorEngine) +
                    //           "  SolarEngine is  not apply , please check your project setting");
                    throw new UnityEditor.Build.BuildFailedException("SolarEngine is  not apply , please check your SDK Editor Setting");

                }
#endif




            }


            if (target==BuildTarget.StandaloneWindows|| target==BuildTarget.StandaloneWindows64)
            {

                ApplySetting.setMainLand();
                ApplySetting.copyMainLand();

            }
        }




        private const string SolorEngine = "[SolorEngine]";

        public static void CheckConfusion()
        {
            if (PlayerSettings.Android.minifyRelease || PlayerSettings.Android.minifyDebug)
            {
                Debug.Log(string.Format(SolorEngine) + " minifyRelease or minifyDebug is true");

                var androidPluginsPath = Path.Combine(Application.dataPath, "Plugins/Android");
                var appproguardPath = Path.Combine(Application.dataPath, "Plugins/Android/proguard-user.txt");
                var seproguardPath =
                    Path.Combine(Application.dataPath, "Plugins/SolarEngine/Android/proguard-user.txt");



                if (!File.Exists(appproguardPath))
                {
                    if (!Directory.Exists(androidPluginsPath))
                    {
                        Directory.CreateDirectory(androidPluginsPath);

                    }


                    File.Copy(seproguardPath, appproguardPath);
                    Debug.Log(string.Format(SolorEngine) + " copy proguard-user.txt");
                }
                else
                {
                    // 读取文件现有内容
                    string appContent = File.ReadAllText(appproguardPath);
                    string seContent = File.ReadAllText(seproguardPath);

                    if (!appContent.Contains(seContent))
                    {
                        string updatedContent = appContent + seContent;
                        // 写入更新后的内容到文件
                        File.WriteAllText(appproguardPath, updatedContent);

                        Debug.Log(string.Format(SolorEngine) +
                                  $"Successfully added rule to keep  in proguard-user.txt  {seContent}");

                    }
                    else
                    {
                        Debug.Log(string.Format(SolorEngine) + $"already exists in proguard-user.txt   {seContent}  ");
                    }


                }
            }


        }


        private static bool AddPermissions(XmlDocument manifest)
        {

            var manifestHasChanged = false;

            // If enabled by the user && android.permission.INTERNET permission is missing, add it.

            manifestHasChanged |= AddPermission(manifest, "android.permission.INTERNET");

            // If enabled by the user && com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE permission is missing, add it.

            manifestHasChanged |= AddPermission(manifest, "android.permission.ACCESS_NETWORK_STATE");


            manifestHasChanged |= AddPermission(manifest, "android.permission.ACCESS_WIFI_STATE");


            manifestHasChanged |= AddPermission(manifest, "android.permission.ACCESS_NETWORK_STATE");


            manifestHasChanged |= AddPermission(manifest, "com.google.android.gms.permission.AD_ID");
            return manifestHasChanged;
        }

        private static bool AddPermission(XmlDocument manifest, string permissionValue)
        {
            if (DoesPermissionExist(manifest, permissionValue))
            {
                Debug.Log(string.Format(
                    SolorEngine + " Your app's AndroidManifest.xml file already contains {0} permission.",
                    permissionValue));
                return false;
            }

            var element = manifest.CreateElement("uses-permission");
            AddAndroidNamespaceAttribute(manifest, "name", permissionValue, element);
            manifest.DocumentElement.AppendChild(element);
            Debug.Log(string.Format(
                SolorEngine + " {0} permission successfully added to your app's AndroidManifest.xml file.",
                permissionValue));

            return true;
        }

        private static bool DoesPermissionExist(XmlDocument manifest, string permissionValue)
        {
            var xpath = string.Format("/manifest/uses-permission[@android:name='{0}']", permissionValue);
            return manifest.DocumentElement.SelectSingleNode(xpath, GetNamespaceManager(manifest)) != null;
        } 
        
        
     //   [MenuItem("SolarEngineSDK/RunPostProcessTasksAndroid ", false, 0)]

        public static void RunPostProcessTasksAndroid()
        {
            var isSEManifestUsed = false;
            var androidPluginsPath = Path.Combine(Application.dataPath, "Plugins/Android");
            var seManifestPath =
                Path.Combine(Application.dataPath, "Plugins/SolarEngine/Android/AndroidManifest.xml");
            var appManifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            var manifestHasChanged = false;
            if (!File.Exists(appManifestPath))
            {
                if (!Directory.Exists(androidPluginsPath))
                {
                    Directory.CreateDirectory(androidPluginsPath);
                }

                isSEManifestUsed = true;

                File.Copy(seManifestPath, appManifestPath);

            }

            var manifestFile = new XmlDocument();
            manifestFile.Load(appManifestPath);

            if (!isSEManifestUsed)
            {
                manifestHasChanged |= AddPermissions(manifestFile);

            }

            manifestHasChanged |= AddURISchemes(manifestFile);
            if (manifestHasChanged)
            {
                manifestFile.Save(appManifestPath);

                Debug.Log(string.Format(SolorEngine) + "Successfully added URI schemes to AndroidManifest.xml");
            }

        }

        private static bool AddURISchemes(XmlDocument manifest)
        {
            if (SolarEngineSettings.AndroidUrlSchemes.Length == 0)
                return false;

            var intentRoot = manifest.DocumentElement.SelectSingleNode(
                "/manifest/application/activity[@android:name='com.unity3d.player.UnityPlayerActivity']",
                GetNamespaceManager(manifest));
            var usedIntentFiltersChanged = false;
            var usedIntentFilters = GetIntentFilter(manifest);


            Debug.Log(string.Format(SolorEngine) + "Adding URI schemes to AndroidManifest.xml" +
                      SolarEngineSettings.AndroidUrlSchemes[0]);
            foreach (var uriScheme in SolarEngineSettings.AndroidUrlSchemes)
            {
                Uri uri;
                // try
                // {
                //     // The first element is android:scheme and the second one is android:host.
                //     uri = new Uri(uriScheme);
                //
                //     // Uri class converts implicit file paths to explicit file paths with the file:// scheme.
                //     if (!uriScheme.StartsWith(uri.Scheme))
                //     {
                //         throw new UriFormatException();
                //     }
                // }
                // catch (UriFormatException)
                // {
                //     Debug.LogError(string.Format("[SolorEngine]: Android deeplink URI scheme \"{0}\" is invalid and will be ignored.", uriScheme));
                //     Debug.LogWarning(string.Format("[SolorEngine]: Make sure that your URI scheme entry ends with ://"));
                //     continue;
                // }
                //
                if (!IsIntentFilterAlreadyExist(manifest, uriScheme))
                {
                    Debug.Log("[SolorEngine]: Adding new URI with scheme: " + uriScheme);
                    var androidSchemeNode = manifest.CreateElement("data");
                    AddAndroidNamespaceAttribute(manifest, "scheme", uriScheme, androidSchemeNode);
                    // AddAndroidNamespaceAttribute(manifest, "host", uri.Host, androidSchemeNode);
                    usedIntentFilters.AppendChild(androidSchemeNode);
                    usedIntentFiltersChanged = true;

                    Debug.Log(string.Format(
                        "[SolorEngine]: Android deeplink URI scheme \"{0}\" successfully added to your app's AndroidManifest.xml file.",
                        uriScheme));
                }
            }

            if (usedIntentFiltersChanged && usedIntentFilters.ParentNode == null)
            {
                intentRoot.AppendChild(usedIntentFilters);
            }

            return usedIntentFiltersChanged;
        }



        private static XmlElement GetIntentFilter(XmlDocument manifest)
        {
            // var xpath = "/manifest/application/activity/intent-filter[data/@android:scheme and data/@android:host]";
            var xpath = "/manifest/application/activity/intent-filter[data/@android:scheme]";

            var intentFilter =
                manifest.DocumentElement.SelectSingleNode(xpath, GetNamespaceManager(manifest)) as XmlElement;
            Debug.Log(
                "[SolorEngine]: Adding missing android.intent.action.VIEW to intent filter in AndroidManifest.xml");

            if (intentFilter == null)
            {
                const string androidName = "name";
                const string category = "category";

                intentFilter = manifest.CreateElement("intent-filter");

                var actionElement = manifest.CreateElement("action");
                AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.action.VIEW", actionElement);
                intentFilter.AppendChild(actionElement);

                var defaultCategory = manifest.CreateElement(category);
                AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.category.DEFAULT", defaultCategory);
                intentFilter.AppendChild(defaultCategory);

                var browsableCategory = manifest.CreateElement(category);
                AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.category.BROWSABLE",
                    browsableCategory);
                intentFilter.AppendChild(browsableCategory);
            } // 检查是否存在android.intent.action.VIEW

            //     else
            //     {
            //         Debug.Log("[SolorEngine]: Found existing intent filter in AndroidManifest.xml");
            //         XmlNodeList actionNodes = intentFilter.GetElementsByTagName("action");
            //   
            //         
            //     bool hasViewAction = false;
            //     foreach (XmlElement actionNode in actionNodes)
            //     {
            //         string actionValue = actionNode.GetAttribute("android:name");
            //     
            //         if (actionValue == "android.intent.action.VIEW")
            //         {
            //             hasViewAction = true;
            //             break;
            //         }
            //     }
            //
            //     // 检查是否存在android.intent.category.DEFAULT
            //     XmlNodeList categoryNodes = intentFilter.GetElementsByTagName("category");
            //     bool hasDefaultCategory = false;
            //     foreach (XmlElement categoryNode in categoryNodes)
            //     {
            //         string categoryValue = categoryNode.GetAttribute("android:name");
            //         if (categoryValue == "android.intent.category.DEFAULT")
            //         {
            //             hasDefaultCategory = true;
            //             break;
            //         }
            //     }
            //
            //     // 检查是否存在android.intent.category.BROWSABLE
            //     bool hasBrowsableCategory = false;
            //     foreach (XmlElement categoryNode in categoryNodes)
            //     {
            //         string categoryValue = categoryNode.GetAttribute("android:name");
            //         if (categoryValue == "android.intent.category.BROWSABLE")
            //         {
            //             hasBrowsableCategory = true;
            //             break;
            //         }
            //     }
            //     const string androidName = "name";
            //     const string category = "category";
            //     if (!hasViewAction)
            //     {
            //      
            //         Debug.Log("[SolorEngine]: Adding missing android.intent.action.VIEW to intent filter in AndroidManifest.xml");
            //         var actionElement = manifest.CreateElement("action");
            //   
            //         AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.action.VIEW", actionElement);
            //         intentFilter.AppendChild(actionElement);
            //    
            //     }
            //
            //     // 如果不存在android.intent.category.DEFAULT，则添加
            //     if (!hasDefaultCategory)
            //     {
            //
            //         var defaultCategory = manifest.CreateElement(category);
            //         AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.category.DEFAULT", defaultCategory);
            //         intentFilter.AppendChild(defaultCategory);
            //     }
            //
            //     // 如果不存在android.intent.category.BROWSABLE，则添加
            //     if (!hasBrowsableCategory)
            //     {
            //         var browsableCategory = manifest.CreateElement(category);
            //         AddAndroidNamespaceAttribute(manifest, androidName, "android.intent.category.BROWSABLE", browsableCategory);
            //         intentFilter.AppendChild(browsableCategory);
            //     }
            //
            // }
            return intentFilter;
        }

        private static void AddAndroidNamespaceAttribute(XmlDocument manifest, string key, string value,
            XmlElement node)
        {
            var androidSchemeAttribute =
                manifest.CreateAttribute("android", key, "http://schemas.android.com/apk/res/android");
            androidSchemeAttribute.InnerText = value;
            node.SetAttributeNode(androidSchemeAttribute);

        }

        private static bool IsIntentFilterAlreadyExist(XmlDocument manifest, string link)
        {
            var xpath = string.Format("/manifest/application/activity/intent-filter/data[@android:scheme='{0}']", link);
            return manifest.DocumentElement.SelectSingleNode(xpath, GetNamespaceManager(manifest)) != null;
        }

        private static XmlNamespaceManager GetNamespaceManager(XmlDocument manifest)
        {
            var namespaceManager = new XmlNamespaceManager(manifest.NameTable);
            namespaceManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");
            return namespaceManager;
        }
    }



    public class SolarEngineEditorPostBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneWindows ||
                report.summary.platform == BuildTarget.StandaloneWindows64)
            {
                ApplySetting.deleteMainLand();
            }

        }
    }

}
