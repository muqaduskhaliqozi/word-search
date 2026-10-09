
using System;
 using UnityEngine;
using UnityEditor;
using System.Collections;
 using System.IO;
 using System.Linq;
 using System.Reflection;
 using System.Xml.Linq;
using SolarEngine.Build;
using SolarEngineSDK.Editor;

namespace SolarEngine
{
    [CustomEditor(typeof(SolarEngineSettings))]
    public class SolarEngineSettingsEditor : Editor
    {
      

// 序列化属性，用于表示是否选择中国存储区域的设置，方便在编辑器中操作和获取对应的值
        private SerializedProperty chinaProperty;
// 序列化属性，用于表示是否选择海外存储区域的设置，方便在编辑器中操作和获取对应的值
        private SerializedProperty overseaProperty;

// 序列化属性，用于表示是否使用远程配置的设置
        private SerializedProperty useRemoteConfig;
// 序列化属性，用于表示是否使用 OAID 的设置
        private SerializedProperty useOaid;
// 序列化属性，用于表示是否使用深度链接的设置
        private SerializedProperty useDeepLink;
// 序列化属性，用于表示是否使用指定版本的设置
        private SerializedProperty useSpecifyVersion;

// 序列化属性，用于表示 iOS 平台远程配置相关的设置
        private SerializedProperty iOSRemoteConfig;
// 序列化属性，用于表示 Android 平台远程配置相关的设置
        private SerializedProperty androidRemoteConfig;
// 序列化属性，用于表示小游戏平台远程配置相关的设置
        private SerializedProperty miniGameRemoteConfig;

// 序列化属性，用于表示 iOS 平台 URL 标识符相关的设置
        SerializedProperty iOSUrlIdentifier;
// 序列化属性，用于表示 iOS 平台 URL 方案相关的设置
        SerializedProperty iOSUrlSchemes;
// 序列化属性，用于表示 iOS 平台通用链接域名相关的设置
        SerializedProperty iOSUniversalLinksDomains;
// 序列化属性，用于表示 iOS 平台版本相关的设置
        SerializedProperty iOSVersion;
        
        private SerializedProperty useiOSSDK;
        private SerializedProperty removeAndroidSDK;

// 序列化属性，用于表示 Android 平台 URL 方案相关的设置
        SerializedProperty AndroidUrlSchemes;
// 序列化属性，用于表示 Android 平台版本相关的设置
        SerializedProperty AndroidVersion;

// 用于记录之前中国存储区域选择的布尔值，方便对比属性值变化
        private bool oldChinaValue;
// 用于记录之前海外存储区域选择的布尔值，方便对比属性值变化
        private bool oldOverseaValue;
// 以下类似的多个布尔值用于记录对应属性之前的旧值，便于处理属性变更逻辑
        private bool oldDisAllValue;
        private bool oldDisiOSValue;
        private bool oldDisAndroidValue;
        private bool oldDisMiniGameValue;
        private bool oldDisOaidValue;

    

      

        private object SolarEngineSetting;

        void OnEnable()
        {
            // 获取当前正在编辑的SolarEngineSettings类型的目标对象实例
            SolarEngineSetting = target as SolarEngineSettings;

            // 通过序列化对象查找并获取对应的属性，以下依次是获取iOS版本、Android版本相关的序列化属性
            iOSVersion = serializedObject.FindProperty("_iOSVersion");
            AndroidVersion = serializedObject.FindProperty("_AndroidVersion");

            // 获取iOS平台URL相关的几个序列化属性，如标识符、方案、通用链接域名等
            iOSUrlIdentifier = serializedObject.FindProperty("_iOSUrlIdentifier");
            iOSUrlSchemes = serializedObject.FindProperty("_iOSUrlSchemes");
            iOSUniversalLinksDomains = serializedObject.FindProperty("_iOSUniversalLinksDomains");

            // 获取Android平台URL方案相关的序列化属性
            AndroidUrlSchemes = serializedObject.FindProperty("_AndroidUrlSchemes");

            // 获取表示中国、海外存储区域选择的序列化属性
            chinaProperty = serializedObject.FindProperty("_China");
            overseaProperty = serializedObject.FindProperty("_Oversea");
            
            //是否移除iOS or Android
            useiOSSDK = serializedObject.FindProperty("_UseiOSSDK");
            removeAndroidSDK = serializedObject.FindProperty("_RemoveAndroidSDK");
            
            
            

            // 获取是否使用远程配置、OAID、深度链接、指定版本等相关的序列化属性
            useRemoteConfig = serializedObject.FindProperty("_RemoteConfig");
            useOaid = serializedObject.FindProperty("_Oaid");
            useDeepLink = serializedObject.FindProperty("_DeepLink");
            useSpecifyVersion = serializedObject.FindProperty("_SpecifyVersion");

            // 获取不同平台（iOS、Android、小游戏）远程配置相关的序列化属性
            iOSRemoteConfig = serializedObject.FindProperty("_iOS");
            androidRemoteConfig = serializedObject.FindProperty("_Android");
            miniGameRemoteConfig = serializedObject.FindProperty("_MiniGame");

            // 记录初始时中国存储区域选择的布尔值
            oldChinaValue = chinaProperty.boolValue;
            // 记录初始时海外存储区域选择的布尔值
            oldOverseaValue = overseaProperty.boolValue;

            // 记录初始时是否使用OAID的布尔值
            oldDisOaidValue = useOaid.boolValue;
        }


        public override void OnInspectorGUI()
        {
          
            this._GUI();
        }
     
        private void ChinaOrOversea(  GUIStyle darkerCyanTextFieldStyles)
        {
            EditorGUILayout.HelpBox(ConstString.storageAreaMessage, MessageType.Info);
            
            EditorGUI.indentLevel += 1;
            EditorGUILayout.PropertyField(chinaProperty,new GUIContent(ConstString.chinaMainland));
            EditorGUILayout.PropertyField(overseaProperty,new GUIContent(ConstString.nonChinaMainland));
            EditorGUI.indentLevel -= 1;
            if (chinaProperty.boolValue && overseaProperty.boolValue)
            {
                EditorGUILayout.HelpBox(ConstString.storageWarning, MessageType.Warning);
            }
           
            if (serializedObject.ApplyModifiedProperties())
            {
                
                // 处理 China 值变化
                ProcessPropertyChange(chinaProperty, ref oldChinaValue, "_China",null, () =>
                {
                    overseaProperty.boolValue = false;
                    oldOverseaValue = overseaProperty.boolValue;
                    
                });
            
                // 处理 Oversea 值变化
                ProcessPropertyChange(overseaProperty, ref oldOverseaValue, "_Oversea",null, () =>
                {
                    chinaProperty.boolValue = false;
                    oldChinaValue = chinaProperty.boolValue;
                    if (overseaProperty.boolValue)
                    {
                        useOaid.boolValue = false;

                    }
                  
                });
            }
        }




        bool changleStorageValue()
        {
            if (chinaProperty.boolValue)
            {
             return   XmlModifier.cnxml(true);
            }
            else if (overseaProperty.boolValue)
            {
                return   XmlModifier.Overseaxml(true);
            }

            return false;

        }


        private bool removesdk=false;
        

        private void removeSDK()
        {
            // removesdk =EditorGUILayout.Foldout(removesdk, "SDK");
            // if (removesdk)
            // {
            //     
                // EditorGUILayout.HelpBox(ConstString.removeSDKMsg, MessageType.Info);
               // EditorGUI.indentLevel += 1;
               // EditorGUILayout.PropertyField(useiOSSDK,new GUIContent("USE iOS SDK"));
                EditorGUILayout.PropertyField(removeAndroidSDK,new GUIContent("Remove Android SDK"));
               // EditorGUI.indentLevel -= 1;
                
                
          //  }
        }

        private bool _useRemoteConfig = false;
        private void RemoteConfig()
        {
          
            
           // EditorGUILayout.PropertyField(useRemoteConfig);
           _useRemoteConfig = EditorGUILayout.Foldout(_useRemoteConfig, "Remote Config");
            if (_useRemoteConfig)
            {  
               
                EditorGUILayout.HelpBox(ConstString.remoteConfigMsg, MessageType.Info);
                
                EditorGUI.indentLevel += 1;
                // EditorGUILayout.PropertyField(disAllRemoteConfig);
                EditorGUILayout.PropertyField(iOSRemoteConfig);
                EditorGUILayout.PropertyField(androidRemoteConfig);
                EditorGUILayout.PropertyField(miniGameRemoteConfig);
                EditorGUI.indentLevel -= 1;
                
            }
            
            if (removeAndroidSDK.boolValue)
            {
                androidRemoteConfig.boolValue = false;
            }
            // else
            // {
            //     iOSRemoteConfig.boolValue = true;
            //     androidRemoteConfig.boolValue = true;
            //     miniGameRemoteConfig.boolValue = true;
            // }

        }
        private void UseOaid()
        {
            if (chinaProperty.boolValue)
            {
                EditorGUILayout.HelpBox(ConstString.storageEnableOaidCN, MessageType.Info);
                useOaid.boolValue = true;

            }
            if (removeAndroidSDK.boolValue)
            {
                useOaid.boolValue = false;
            }

            if (overseaProperty.boolValue)
            {
                if (useOaid.boolValue)
                {
                     EditorGUILayout.HelpBox(ConstString.oaidEnable, MessageType.Warning);
                }
                else
                {
                 //   EditorGUILayout.HelpBox(ConstString.storageDisableOaid, MessageType.Info);
                }
            }
            EditorGUILayout.PropertyField(useOaid,new GUIContent(ConstString.oaid));
          
        }

      

        private void UseDeepLink( GUIStyle darkerCyanTextFieldStyles )
        {
            
            EditorGUILayout.PropertyField(useDeepLink, new GUIContent("DeepLink"));
            if (useDeepLink.boolValue) 
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.LabelField("iOS:", darkerCyanTextFieldStyles);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(iOSUrlIdentifier,
                    new GUIContent("iOS URL Identifier",
                        "Value of CFBundleURLName property of the root CFBundleURLTypes element. " +
                        "If not needed otherwise, value should be your bundle ID."),
                    true);
                EditorGUILayout.PropertyField(iOSUrlSchemes,
                    new GUIContent("iOS URL Schemes",
                        "URL schemes handled by your app. " +
                        "Make sure to enter just the scheme name without :// part at the end."),
                    true);
                EditorGUILayout.PropertyField(iOSUniversalLinksDomains,
                    new GUIContent("iOS Universal Links Domains",
                        "Associated domains handled by your app. State just the domain part without applinks: part in front."),
                    true);
                EditorGUI.indentLevel -= 1;

                
                EditorGUILayout.LabelField("Android:", darkerCyanTextFieldStyles);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(AndroidUrlSchemes);
                EditorGUI.indentLevel -= 1;
                EditorGUI.indentLevel -= 1;
            }
        }
        
     

        private bool _useSpecifyVersion = false;
        private void SdkVersion(GUIStyle darkerCyanTextFieldStyles)
        {
            
            _useSpecifyVersion = EditorGUILayout.Foldout(_useSpecifyVersion, "SDK Version");
            if (_useSpecifyVersion)
            {
                // EditorGUILayout.PropertyField(useSpecifyVersion);

                EditorGUILayout.HelpBox(ConstString.confirmVersion, MessageType.Warning);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(iOSVersion, new GUIContent("iOS Version"));
                EditorGUILayout.PropertyField(AndroidVersion);
                EditorGUI.indentLevel--;
                if (!iOSVersion.stringValue.Equals(SolarEngineSettings.iOSVersion))
                    SolarEngineSettings.iOSVersion = iOSVersion.stringValue;
                if (!AndroidVersion.stringValue.Equals(SolarEngineSettings.AndroidVersion))
                    SolarEngineSettings.AndroidVersion = AndroidVersion.stringValue;
            }
            // else
            //     {
            //         iOSVersion.stringValue = "";
            //         AndroidVersion.stringValue = "";
            //     }
            // }
        }

       
        
        
        private void ProcessPropertyChange(SerializedProperty property, ref bool oldValue, string propertyName, System.Action<bool> xmlAction, System.Action additionalAction = null)
        {
       
            if (property.boolValue!= oldValue)
            {
         
                oldValue = property.boolValue;
                additionalAction?.Invoke();
            }
        }
       
        public void _GUI()
        {
            GUIStyle darkerCyanTextFieldStyles = new GUIStyle(EditorStyles.boldLabel);
            //darkerCyanTextFieldStyles.normal.textColor = Color.white;
         
          
            GUI.color= Color.white;
            DrawH2Title("SDK Setting");

          
            ChinaOrOversea(darkerCyanTextFieldStyles);
       
            DrawH2Title("SDK Plugins");
            removeSDK();
            RemoteConfig();
            UseOaid();

            UseDeepLink( darkerCyanTextFieldStyles);
            
    
            
            SdkVersion(darkerCyanTextFieldStyles);

            ApplyButton();

          
            serializedObject.ApplyModifiedProperties();
       
        }
      
       
 private void ApplyButton()
{
    // 创建一个用于按钮样式的GUIStyle对象
    GUIStyle buttonStyle = new GUIStyle();
    buttonStyle.normal.textColor = Color.white;

    // 创建一个单像素的纹理对象，用于设置按钮的背景颜色等样式
    Texture2D backgroundTexture = new Texture2D(1, 1);
    backgroundTexture.SetPixel(0, 0, Color.white);
    backgroundTexture.Apply();
    buttonStyle.normal.background = backgroundTexture;

    // 设置按钮的固定高度、固定宽度以及文本对齐方式等样式属性
    buttonStyle.fixedHeight = 25;
    buttonStyle.fixedWidth = 100;
    buttonStyle.alignment = TextAnchor.MiddleCenter;


    // 设置绘制按钮边框时的颜色
    GUI.color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
  
    
    // 当用户点击按钮区域时执行以下逻辑
    if (GUILayout.Button( "Apply"))
    {
        ApplySetting._applySetting(true);
    }
}

        //用户应用
        public  bool Apply()
        {
          return  iOSRemoteConfigValue()&&
           androidRemoteConfigValue()&&
           miniGameRemoteConfigValue()&&
             
           OaidValue()&&

           changleStorageValue();
         

        }


        private bool OaidValue()
        {
            if (useOaid.boolValue)
            {
              return  PluginsEdtior.showOaid();
            }
            else
            {
              return  PluginsEdtior.disableOaid();
            }
          
        }
   

        bool iOSRemoteConfigValue()
        {
            if (iOSRemoteConfig.boolValue)
            {
                return  PluginsEdtior.showiOS();
            }
               
            else
            {
                return  PluginsEdtior.disableiOS();

            }
        }
        bool androidRemoteConfigValue()
        {

            if (androidRemoteConfig.boolValue)
            {
              
              return  PluginsEdtior.showAndroid();
            

            }
              
            else
            {
              
                 return PluginsEdtior.disableAndroid();
               

            }
        }
        bool miniGameRemoteConfigValue()
        {

            if (miniGameRemoteConfig.boolValue)
            {  
               
               return  PluginsEdtior.showMiniGame();
            }
             
            else
            {
              
                return   PluginsEdtior.disableMiniGame();
            }
            
        }
        
    
        // 通用标签间的间距
        private const float COMMON_SPACE = 13f;

        // 最大间距
        private const float MAX_SPACE = 25f;

        protected void DrawH2Title(string title)
        {
            DrawAreaTitle(title, Color.black, TextAnchor.MiddleLeft, 16);
        }

        /// <summary>
        /// 绘制标题区域.
        /// </summary>
        /// <param name="title">标题.</param>
        /// <param name="color">字体颜色.</param>
        /// <param name="textAnchor">对齐方式.</param>
        /// <param name="fontSize">字体大小.</param>
        private static void DrawAreaTitle(string title, Color color, TextAnchor textAnchor, int fontSize)
        {
            EditorGUILayout.BeginVertical();
            DrawVerticalSpace(MAX_SPACE);

            var guiStyle = new GUIStyle();
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = color;
            guiStyle.fontStyle = FontStyle.BoldAndItalic;
            guiStyle.alignment = textAnchor;
            EditorGUILayout.TextArea(title, guiStyle);
            EditorGUILayout.EndVertical();
            DrawVerticalSpace(COMMON_SPACE);
        }

        /// <summary>
        /// 绘制垂直方向间距.
        /// </summary>
        /// <param name="pixels">间距.</param>
        private static void DrawVerticalSpace(float pixels)
        {
            GUILayout.Space(pixels);
        }
        
        
        
        
        
        /// <summary>
        /// 展示提示.
        /// </summary>
        /// <param name="title">标题.</param>
        /// <param name="content">具体内容.</param>
        public static void ShowTips(string title, string content)
        {
            // 展示提示信息.
            EditorUtility.DisplayDialog(title, content, "OK");
        }

        
        
        

    }



}

