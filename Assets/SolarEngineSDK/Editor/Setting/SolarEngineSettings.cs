
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;


namespace SolarEngine
{
	#if UNITY_EDITOR
    [InitializeOnLoad]
	#endif
    public class SolarEngineSettings : ScriptableObject
	{
		
		private const string ASSET_NAME = "SolarEngineSettings";
		private const string ASSET_PATH = "Resources";
		private const string ASSET_EXT = ".asset";

		private static SolarEngineSettings instance;

		[SerializeField]
		private bool _China;
		[SerializeField]
		private bool _Oversea;
		
		
		//默认开启
		[SerializeField]
		private bool _RemoteConfig=true;
		[SerializeField]
		private bool _All=true;
		[SerializeField]
		private bool _iOS=true;
		[SerializeField]
		private bool _Android=true;
		[SerializeField]
		private bool _MiniGame=true;
		
		[SerializeField]
		private bool _Oaid=true;
		
		
		// [SerializeField]
		// private bool _UseiOSSDK;
		[SerializeField]
		private bool _RemoveAndroidSDK;
		
		
		
		
		[SerializeField]
		private bool _DeepLink;
		[SerializeField]
		private bool _SpecifyVersion;
		
		
		
		[SerializeField]
		private string _iOSVersion;
		[SerializeField]
		private string _AndroidVersion;
		[SerializeField]
		private string _iOSUrlIdentifier;
		[SerializeField]
		private string[] _iOSUrlSchemes = new string[0];
		[SerializeField]
		private string[] _iOSUniversalLinksDomains = new string[0];
		
		
		[SerializeField]
		private string[] _AndroidUrlSchemes = new string[0];

		private static SolarEngineSettings Instance
	    {
	        get
	        {
	            if (instance == null)
	            {
					instance = Resources.Load(ASSET_NAME) as SolarEngineSettings;
	                if (instance == null)
	                {
	                    // If not found, autocreate the asset object.
	                    instance = ScriptableObject.CreateInstance<SolarEngineSettings>();
	                    #if UNITY_EDITOR
	                    string properPath = Path.Combine(Application.dataPath, ASSET_PATH);
	                    if (!Directory.Exists(properPath))
	                    {
	                        Directory.CreateDirectory(properPath);
	                    }

	                    string fullPath = Path.Combine(Path.Combine("Assets", ASSET_PATH), ASSET_NAME + ASSET_EXT);
	                    AssetDatabase.CreateAsset(instance, fullPath);
	                    #endif
	                }
				}

	            return instance;
	        }
	    }

		public static string iOSUrlIdentifier
		{
			get { return Instance._iOSUrlIdentifier; }
			set { Instance._iOSUrlIdentifier = value; }
		}

		public static string[] iOSUrlSchemes
		{
			get { return Instance._iOSUrlSchemes; }
			set { Instance._iOSUrlSchemes = value; }
		}

		public static string[] iOSUniversalLinksDomains
		{
			get { return Instance._iOSUniversalLinksDomains; }
			set { Instance._iOSUniversalLinksDomains = value; }
		}
		public static string[] AndroidUrlSchemes
		{
			get { return Instance._AndroidUrlSchemes; }
			set { Instance._AndroidUrlSchemes = value; }
		}

		public static bool isCN
		{
			get{return  Instance._China;}
			
		}
		
		
		public static bool isOversea
		{
			get{return  Instance._Oversea;}
			
		}

		// public static bool isUseiOSSDK
		// {
		// 	get{return  Instance._UseiOSSDK;}
		// }
		public static bool removeAndroidSDK
		{
			get { return Instance._RemoveAndroidSDK; }
		}

		
		public static bool isUseMiniGame
		{
			get{return  Instance._MiniGame;}
		
		}

		public static bool isUseAndroid
		{
			get{return  Instance._Android;}
			
			
		}
		public static  bool isUseiOS
			{
			get{return  Instance._iOS;}
		
			
		}
		public static bool isUseAll
		{
			get{return  Instance._All;}
		
		}

		public static bool isUseOaid
		{
			get{return  Instance._Oaid;}
			set{Instance._Oaid = value;}
			
		}
		public static bool isSpecifyVersio
		{
			get{return  Instance._SpecifyVersion;}
			set{Instance._SpecifyVersion = value;}
		}

		public static string iOSVersion
		{
			get{return  Instance._iOSVersion;}
			set{Instance._iOSVersion = value;}
		}
		public static string AndroidVersion
		{
			get{return  Instance._AndroidVersion;}
			set{Instance._AndroidVersion = value;}
		}
		#if UNITY_EDITOR
		[MenuItem("SolarEngineSDK/SDK Edit Settings", false, 0)]
	    public static void EditSettings ()
	    {
	        Selection.activeObject = Instance;
	    }
		#endif

		
		
	
		
	}

}
