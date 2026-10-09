
#if UNITY_IOS

using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace SolarEngine
{

    public class InstallSdkInXcode
	{

		private static readonly string[] SKIP_FILES = {@".*\.meta$"};

		private static string BuildPath { get; set; }

		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
		{
			if (buildTarget == BuildTarget.iOS)
			{
				AfterBuild (pathToBuiltProject);
				RunPostBuildScript(target: buildTarget, projectPath: pathToBuiltProject);
			}
		}
		
#if TUANJIE_2022_3_OR_NEWER		
			// Must be between 40 and 50 to ensure that it's not overriden by Podfile generation (40) and
			// that it's added before "pod install" (50).
			[PostProcessBuildAttribute(45)]
			private static void PostProcessBuild_iOS(BuildTarget target, string buildPath)
			{

				if (target == BuildTarget.iOS)
				{
		
					string podfilePath = Path.Combine(buildPath, "Podfile");
					try
					{
						string originalContent = File.ReadAllText(podfilePath, Encoding.UTF8);
						string modifiedContent = Regex.Replace(originalContent, "Unity-iPhone|UnityFramework", match =>
						{
							if (match.Value == "Unity-iPhone")
							{
								return "Tuanjie-iPhone";
							}
							else
							{
								return "TuanjieFramework";
							}
						});
						File.WriteAllText(podfilePath, modifiedContent);
					}
					catch (IOException e)
					{
						Debug.LogError($"SolarEngine {e.Message}");
					}
					
				}
				
			}
		#endif

		public static void AfterBuild (string buildPath)
		{
			BuildPath = buildPath;

			if (BuildPath == null)
			{
				return;
			}

		//	AddFrameworks();

            UnityEngine.Debug.Log ("Build success!");
		}

		private static void AddFrameworks ()
		{
#if TUANJIE_2022_3_OR_NEWER
			string projectFile = Path.Combine (BuildPath, "Tuanjie-iPhone.xcodeproj/project.pbxproj");
#else
			string projectFile = Path.Combine (BuildPath, "Unity-iPhone.xcodeproj/project.pbxproj");

#endif
			

			PBXProject pbxProject = new PBXProject ();
			pbxProject.ReadFromFile (projectFile);

			string target = pbxProject.TargetGuidByName ("UnityFramework");

			// ********** Frameworks ********** //
			pbxProject.AddFrameworkToProject (target, "AdServices.framework", false);
			pbxProject.AddFrameworkToProject (target, "iAd.framework", false);
			pbxProject.AddFrameworkToProject (target, "SystemConfiguration.framework", false);
			pbxProject.AddFrameworkToProject (target, "Security.framework", false);
			pbxProject.AddFrameworkToProject (target, "CoreTelephony.framework", false);
			pbxProject.AddFrameworkToProject (target, "AdSupport.framework", false);

			// pbxProject.AddFrameworkToProject (target, "AppTrackingTransparency.framework", false);

			// ********** Link Flags ********** //
			pbxProject.AddBuildProperty (target, "OTHER_LDFLAGS", "-ObjC -lz -lsqlite3 -licucore");
			pbxProject.WriteToFile (projectFile); 
		}


		private static void RunPostBuildScript(BuildTarget target, string projectPath = "")
		{
			if (target == BuildTarget.iOS)
			{
#if UNITY_IOS

				#if TUANJIE_2022_3_OR_NEWER
				string xcodeProjectPath = projectPath + "/Tuanjie-iPhone.xcodeproj/project.pbxproj";
#else
				string xcodeProjectPath = projectPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
#endif
			//	string xcodeProjectPath = projectPath + "/Unity-iPhone.xcodeproj/project.pbxproj";

				PBXProject xcodeProject = new PBXProject();
				xcodeProject.ReadFromFile(xcodeProjectPath);

#if UNITY_2019_3_OR_NEWER
				string xcodeTarget = xcodeProject.GetUnityMainTargetGuid();
#else
            string xcodeTarget = xcodeProject.TargetGuidByName("Unity-iPhone");
#endif
				HandlePlistIosChanges(projectPath);

                if (SolarEngineSettings.iOSUniversalLinksDomains != null && SolarEngineSettings.iOSUniversalLinksDomains.Length > 0)
                {
                    AddUniversalLinkDomains(xcodeProject, xcodeProjectPath, xcodeTarget);
                }

				xcodeProject.WriteToFile(xcodeProjectPath);

			}
		}


		private static void HandlePlistIosChanges(string projectPath)
		{

			// Get and read info plist.
			var plistPath = Path.Combine(projectPath, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);
			var plistRoot = plist.root;

			if (SolarEngineSettings.iOSUrlIdentifier != null && SolarEngineSettings.iOSUrlSchemes != null)
			{
				if (SolarEngineSettings.iOSUrlIdentifier.Length > 0 && SolarEngineSettings.iOSUrlSchemes.Length > 0)
				{
					AddUrlSchemesIOS(plistRoot, SolarEngineSettings.iOSUrlIdentifier, SolarEngineSettings.iOSUrlSchemes);
				}
			}
			// Write any info plist change.
			File.WriteAllText(plistPath, plist.WriteToString());
		}


		private static void AddUrlSchemesIOS(PlistElementDict plistRoot, string urlIdentifier, string[] urlSchemes)
  		{

			// ********** CFBundleURLTypes ********* //
			PlistElementArray bundleUrlTypes = null;
			if (plistRoot.values.ContainsKey("CFBundleURLTypes"))
			{
				bundleUrlTypes = plistRoot["CFBundleURLTypes"].AsArray();
			}
			else
			{
				bundleUrlTypes = plistRoot.CreateArray("CFBundleURLTypes");
			}

            foreach (var link in urlSchemes)
            {
				PlistElementDict bundleUrlSchemes = bundleUrlTypes.AddDict();
				PlistElementArray bundleUrlSchemesArray = null;

				bundleUrlSchemes.SetString("CFBundleURLName", urlIdentifier);
				bundleUrlSchemesArray = bundleUrlSchemes.CreateArray("CFBundleURLSchemes");
				bundleUrlSchemesArray.AddString(string.Format("{0}", link));
				 
			}
		}

		private static void AddUniversalLinkDomains(PBXProject project, string xCodeProjectPath, string xCodeTarget)
		{
			string entitlementsFileName = "Unity-iPhone.entitlements";

#if UNITY_2019_3_OR_NEWER
			var projectCapabilityManager = new ProjectCapabilityManager(xCodeProjectPath, entitlementsFileName, null, project.GetUnityMainTargetGuid());
#else
        var projectCapabilityManager = new ProjectCapabilityManager(xCodeProjectPath, entitlementsFileName, PBXProject.GetUnityTargetName());
#endif

			var uniqueDomains = SolarEngineSettings.iOSUniversalLinksDomains.Distinct().ToArray();
			const string applinksPrefix = "applinks:";
			for (int i = 0; i < uniqueDomains.Length; i++)
			{
				if (!uniqueDomains[i].StartsWith(applinksPrefix))
				{
					uniqueDomains[i] = applinksPrefix + uniqueDomains[i];
				}
			}

			projectCapabilityManager.AddAssociatedDomains(uniqueDomains);
			projectCapabilityManager.WriteToFile();

			project.AddCapability(xCodeTarget, PBXCapabilityType.AssociatedDomains, entitlementsFileName);
		}
#endif


		private static PlistElementArray CreatePlistElementArray(PlistElementDict root, string key)
		{
			if (!root.values.ContainsKey(key))
			{
				Debug.Log(string.Format("[SolarEngine]: {0} not found in Info.plist. Creating a new one.", key));
				return root.CreateArray(key);
			}
			var result = root.values[key].AsArray();
			return result != null ? result : root.CreateArray(key);
		}

		private static PlistElementDict CreatePlistElementDict(PlistElementArray rootArray)
		{
			if (rootArray.values.Count == 0)
			{
				Debug.Log("[SolarEngine]: Deeplinks array doesn't contain dictionary for deeplinks. Creating a new one.");
				return rootArray.AddDict();
			}

			var urlSchemesItems = rootArray.values[0].AsDict();
			Debug.Log("[SolarEngine]: Reading deeplinks array");
			if (urlSchemesItems == null)
			{
				Debug.Log("[SolarEngine]: Deeplinks array doesn't contain dictionary for deeplinks. Creating a new one.");
				urlSchemesItems = rootArray.AddDict();
			}

			return urlSchemesItems;
		}


	}




}

#endif
