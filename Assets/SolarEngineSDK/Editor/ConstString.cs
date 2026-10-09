namespace SolarEngineSDK.Editor
{
    public class ConstString
    {
        
        
        public const string solorEngine = "[SolorEngineSDK]\n";


        #region EditorPanel

        public  const string chinaMainland="China Mainland";
        //Non-China-Mainland
        public const string nonChinaMainland = "Non-China-Mainland";

        public const string oaid = "OAID";
        
        
        //Please confirm whether you want to enable Oaid in Oversea
        public const string oaidEnable = "When the selected data storage region is outside Mainland China, please confirm whether OAID (Open Advertising ID) is needed. This could impact the application's compliance and eligibility for listing on the Google Play Store.";
      
        public const string storageEnableOaidCN = "When the data storage region is set to China Mainland, OAID cannot be disabled.";
        //he specified version can be used. If not filled in, the latest version will be used by default
        public const string confirmVersion = "Specify the Android/iOS SDK version. If not provided, the latest version will be used by default.";



        public const string remoteConfigMsg = "Please confirm the platform for the online parameter plugin.";
        
        
        public const string removeSDKMsg = "删除.";
        
      
       

        #endregion


        #region pop

        public const string nostorageWarning = solorEngine+"It is mandatory to select the data storage region for products created in the SolarEngine backend.";

        
        public const string confirmMessage2 = "Are you sure you want to perform this operation?";
            
        //Please set up your data storage area.
        public const string storageAreaMessage = "Please select the data storage region for the products created in the SolarEngine backend.";
        
        //Confirm Operation
        public const string pleaseConfirm = "Please Confirm";

        public const string applyFail = solorEngine+"Setup failed.Please check the console error log, make the necessary modifications, and try applying the changes again.";

        #endregion
        
     
        
        
        
        public const string storageWarning = "You can only choose either China or Overseas！";

        //
        public const string applySuccess = "";
        //public const string applyFail = "";
        //
        
        public const string confirmMessage = "You cancelled the operation";
        
        
      

        public const string storage = "";

        public static string currentData =solorEngine+ "The current data storage region is set to";
            
                                       
       
        
          
     

        public const string OK = "OK";
        public const string cancel = "Cancel";
        
      
        //confirm
        public const string confirm = "Confirm";
      //  public const string apply = "Apply";
        public const string success = "Success";
        public const string fail = "Fail";
        
        
        
 
        
        
        
        
     


    }
}