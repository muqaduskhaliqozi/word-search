using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SolarEngine.Build
{
    public class DefineSymbolsEditor
    {
        const string SOLORENGINE_DISABLE_REMOTECONFIG = "SOLORENGINE_DISABLE_REMOTECONFIG";
        const string SE_DIS_RC = "SE_DIS_RC";
        const string SE_MINI_DIS_RC = "SE_MINI_DIS_RC";

        public static void add_DISABLE_REMOTECONFIG(BuildTargetGroup target, bool isminigame)
        {

            string str = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            string newvalue = isminigame ? SE_MINI_DIS_RC : SE_DIS_RC;

            if (str.Contains(SOLORENGINE_DISABLE_REMOTECONFIG))
            {
                str = str.Replace(SOLORENGINE_DISABLE_REMOTECONFIG, newvalue);

            }
            else if (!str.Contains(newvalue))
            {
                str += $";{newvalue}";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target,
                str);



        }



        public static void delete_DISABLE_REMOTECONFIG(BuildTargetGroup target, bool isMiniGame)
        {
            string str = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

            string define = isMiniGame ? SE_MINI_DIS_RC : SE_DIS_RC;
            if (str.Contains(SOLORENGINE_DISABLE_REMOTECONFIG))
            {
                str = str.Replace(SOLORENGINE_DISABLE_REMOTECONFIG, "");

            }

            if (str.Contains(define))
            {
                str = str.Replace(define, "");

            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target,
                str);
        }


    }


    public class DefineSymbolsEditor_MiniGame : MonoBehaviour
    {
        public const string SolarEngineSDK = "SolarEngineSDK";

        public const string MiniGame =SolarEngineSDK+ "/MiniGame/DefineSymbol/";
        // public const string WEBGL =SolarEngineSDK+MiniGame+ "WEBGL/";
        // public const string ANDROID = SolarEngineSDK+MiniGame+"ANDROID/";

        public const string BYTEDANCE = "BYTEDANCE/";
        public const string BYTEDANCE_CLOUD = "BYTEDANCE_CLOUD/";
        public const string BYTEDANCE_STARK = "BYTEDANCE_STARK/";
        public const string CUSTOMEDITOR = "CUSTOMEDITOR/";
        public const string KUAISHOU = "KUAISHOU/";
        public const string WECHAT = "WECHAT/";
        public const string DEV = "DEV/";

        public const string ADD = "ADD";
        public const string DELETE = "DELETE";


        public const string SOLARENGINE_ = "SOLARENGINE_";
        public const string SE_ = "SE_";

        public const string SOLARENGINE_WECHAT = SOLARENGINE_ + "WECHAT";
        public const string SOLARENGINE_KUAISHOU = SOLARENGINE_ + "KUAISHOU";
        public const string SOLARENGINE_BYTEDANCE = SOLARENGINE_ + "BYTEDANCE";
        public const string SOLARENGINE_BYTEDANCE_CLOUD = SOLARENGINE_ + "BYTEDANCE_CLOUD";
        public const string SOLARENGINE_BYTEDANCE_STARK = SOLARENGINE_ + "BYTEDANCE_STARK";
        public const string SOLARENGINE_CUSTOMEDITOR = SOLARENGINE_ + "CUSTOMEDITOR";
        public const string SE_DEV = SE_ + "DEV";


#if UNITY_WEBGL
        //BYTEDANCE
        [MenuItem(MiniGame + BYTEDANCE + ADD, false, 0)]

        public static void addSymbols_BYTEDANCE()
        {
            addSymbols(SOLARENGINE_BYTEDANCE, BuildTargetGroup.WebGL);
        }

        [MenuItem(MiniGame + BYTEDANCE + DELETE, false, 0)]
        public static void deleteSymbols_BYTEDANCE()
        {
            deleteSymbols(SOLARENGINE_BYTEDANCE, BuildTargetGroup.WebGL);
        }

        [MenuItem(MiniGame + WECHAT + ADD, false, 0)]
        public static void addSymbols_WECHAT()
        {
#if TUANJIE_2022_3_OR_NEWER
             addSymbols(SOLARENGINE_WECHAT,BuildTargetGroup.WeixinMiniGame);
#else
            addSymbols(SOLARENGINE_WECHAT, BuildTargetGroup.WebGL);
#endif
        }

        [MenuItem(MiniGame + WECHAT + DELETE, false, 0)]
        public static void deleteSymbols_WECHAT()
        {
#if TUANJIE_2022_3_OR_NEWER
                deleteSymbols(SOLARENGINE_WECHAT,BuildTargetGroup.WeixinMiniGame);
#else
            deleteSymbols(SOLARENGINE_WECHAT, BuildTargetGroup.WebGL);
#endif


        }


        [MenuItem(MiniGame + KUAISHOU + ADD, false, 0)]
        public static void addSymbols_KUAISHOU()
        {
            addSymbols(SOLARENGINE_KUAISHOU, BuildTargetGroup.WebGL);

        }

        [MenuItem(MiniGame + KUAISHOU + DELETE, false, 0)]
        public static void deleteSymbols_KUAISHOU()
        {
            deleteSymbols(SOLARENGINE_KUAISHOU, BuildTargetGroup.WebGL);
        }
        // [MenuItem(MiniGame+DEV+ADD, false, 0)]
        // public static void addSymbols_DEV()
        // {
        //     addSymbols(SE_DEV,BuildTargetGroup.WebGL);
        // }
        // [MenuItem(MiniGame+DEV+DELETE, false, 0)]
        // public static void deleteSymbols_DEV()
        // {
        //     deleteSymbols(SE_DEV,BuildTargetGroup.WebGL);
        // }
#endif



#if UNITY_ANDROID
    [MenuItem(MiniGame+BYTEDANCE_CLOUD+DELETE, false, 0)]
    public static void deleteSymbols_BYTEDANCE_CLOUD()
    {
        deleteSymbols(SOLARENGINE_BYTEDANCE_CLOUD,BuildTargetGroup.Android);
    }
    [MenuItem(MiniGame+BYTEDANCE_CLOUD+ADD, false, 0)]
    public static void addSymbols_BYTEDANCE_CLOUD()
    {
        addSymbols(SOLARENGINE_BYTEDANCE_CLOUD,BuildTargetGroup.Android);
    }
    
    [MenuItem(MiniGame + BYTEDANCE + ADD, false, 0)]

    public static void addSymbols_BYTEDANCE()
    {
        addSymbols(SOLARENGINE_BYTEDANCE, BuildTargetGroup.Android);
    }

    [MenuItem(MiniGame + BYTEDANCE + DELETE, false, 0)]
    public static void deleteSymbols_BYTEDANCE()
    {
        deleteSymbols(SOLARENGINE_BYTEDANCE, BuildTargetGroup.Android);
    }
    // [MenuItem(MiniGame+DEV+ADD, false, 0)]
    // public static void addSymbols_DEV()
    // {
    //     addSymbols(SE_DEV,BuildTargetGroup.Android);
    // }
    // [MenuItem(MiniGame+DEV+DELETE, false, 0)]
    // public static void deleteSymbols_DEV()
    // {
    //     deleteSymbols(SE_DEV,BuildTargetGroup.Android);
    // }
#endif



      










        public static void addSymbols(string symbol, BuildTargetGroup target)
        {
            string str = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

            if (str.Contains(symbol))
            {
               Debug.Log( $"{SolarEngineSDK} : {symbol} already present");
            }
            else
            {

                str += ";" + symbol;

                PlayerSettings.SetScriptingDefineSymbolsForGroup(target,
                    str);
                Debug.Log( $"{SolarEngineSDK} : {symbol} Added successfully");


            }



        }
        //  [MenuItem("SolarEngineSDK/test2 ", false, 0)]

        public static void deleteSymbols(string symbols, BuildTargetGroup target)
        {
            string str = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

            if (str.Contains(symbols))
            {
                str = str.Replace(symbols, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target,
                    str);
                Debug.Log( $"{SolarEngineSDK} : {symbols} Deleted successfully");
            }
            else
            {
                Debug.Log( $"{SolarEngineSDK} : {symbols} not present");
            }


        }
    }
}



// public  class 
