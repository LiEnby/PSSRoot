namespace PSSRoot
{
    public class Constants
    {
        // constant values
        public const string ANDROID_SYSTEM_DIR    = "/system";
        public const string ANDROID_SUID_BINARY   = ANDROID_SYSTEM_DIR + "/bin/run-as"; // binary to overwrite with payload
        public const string ANDROID_ROOT_BACKDOOR = ANDROID_SYSTEM_DIR + "/xbin/root_backdoor"; // location of temp root backdoor
        public const string ANDROID_SU_INSTALL    = ANDROID_SYSTEM_DIR + "/xbin/su"; // location to install 'su' binary to
        
        public const string ANDROID_EXPLOIT      = Constants.ANDROID_TMP_FOLDER + "/" + "exploit";
        public const string ANDROID_PAYLOAD      = Constants.ANDROID_TMP_FOLDER + "/" + "payload";
        public const string ANDROID_SU_BINARY    = Constants.ANDROID_TMP_FOLDER + "/" + "su";
        public const string ANDROID_SUID_BACKUP  = Constants.ANDROID_TMP_FOLDER + "/" + "suid";
        public const string ANDROID_BUSYBOX      = Constants.ANDROID_TMP_FOLDER + "/" + "busybox";

        public const string ANDROID_TMP_FOLDER = "/data/local/tmp"; // work directory on device

        public const int ANDROID_MODE_EXECUTABLE = 755;
        public const int ANDROID_MODE_SUID = 6755;
        public const int ANDROID_UID_ROOT = 0;

        // dynamically generated strings ...
        public static string PSS_ROOT_TMP_FOLDER
        {
            get
            {
                return Path.Combine(Path.GetTempPath(), "pssroot");
            }
        }

    }
}
