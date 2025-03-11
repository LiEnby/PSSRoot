using PSSRoot.Resources;

namespace PSSRoot
{ 
    class CmdHelper : IDisposable
    {
        private AdbHelper adb;
        public CmdHelper(AdbHelper adbHelper, byte[] busyboxBinary)
        {
            Log.Task("Setting up Environment ...");

            this.adb = adbHelper;

            adbHelper.UploadExecutable(RootResources.busybox, Constants.ANDROID_BUSYBOX);
        }

        public void Dispose()
        {
            Log.Task("Cleaning up environment ...");
            
            // if you are root; exit root
            if(this.GetCurrentUid() == 0) this.ExitRootEnvironment();

            Log.Task("Cleaning up busybox binary ...");
            this.RemoveFile(Constants.ANDROID_BUSYBOX);
        }

        public bool FileExists(string path)
        {
            Log.Command("Checking file exists: " + path + " ...");
            string res = this.BusyboxCmd("stat \"" + path + "\"");
            if (adb.ExtractOutputOnly(res).Contains("No such file or directory")) return false;
            else return true;
        }

        public string RunExploit(string path, string dest)
        {
            Log.Command("Running CVE-2016-5195 ...");
            string res = adb.Shell(Constants.ANDROID_EXPLOIT + " \"" + path + "\" \"" + dest + "\" --no-pad");
            return res;
        }


        public void RemoveFile(string path)
        {
            Log.Command("Removing " + path + " ...");
            string res = BusyboxCmd("rm -f \"" + path + "\"");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Touch error: " + adb.ExtractOutputOnly(res));
        }

        public int GetCurrentUid()
        {
            int uid = int.Parse(adb.ExtractOutputOnly(BusyboxCmd("id -u")));
            Log.Command("Got user id: " + uid.ToString() + " ...");

            return uid;
        }

        public string BusyboxCmd(string cmd)
        {
            string res = adb.SendShellCmd(Constants.ANDROID_BUSYBOX + " " + cmd);
            Log.Debug("Run busybox cmd: " + cmd + " output: " + adb.ExtractOutputOnly(res));
            return res;
        }

        public void RootChown(string path, int userId, int groupId)
        {
            Log.Command("Changing owner of " + path + " to "+userId.ToString()+":"+groupId.ToString()+" ...");
            string res = this.BusyboxCmd("chown " + userId.ToString()+":"+groupId.ToString()+" \"" + path + "\"");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Chown error: " + adb.ExtractOutputOnly(res));
        }

        public void RootChmod(string path, int mode)
        {
            Log.Command("Changing " + path + " permission to: "+mode.ToString()+" ...");
            string res = this.BusyboxCmd("chmod "+ mode.ToString() +" \"" + path + "\"");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Chmod failed: " + adb.ExtractOutputOnly(res));
        }

        public void RootCatOverwriteFile(string path, string dest)
        {
            Log.Command("Copying " + path + " to " + dest + " using cat ...");
            string res = this.BusyboxCmd("cat \"" + path + "\" > \"" + dest + "\"");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Error writing " + path + ": " + adb.ExtractOutputOnly(res));
        }

        public void CopyFile(string path, string dest)
        {
            Log.Command("Copying " + path + " to " + dest + " ...");
            string res = this.BusyboxCmd("cp -f \"" + path + "\" \"" + dest + "\"");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Error copying: " + adb.ExtractOutputOnly(res));
        }
        
        public void RootRemountRw(string path)
        {
            Log.Command("Remounting "+path+" as read-write ...");
            string res = BusyboxCmd("mount -o rw,remount " + path);
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("Error mounting " + path + " as read-write: " + adb.ExtractOutputOnly(res));
        }

        public void RootRemountRo(string path)
        {
            Log.Command("Remounting "+path+" as read-only ...");
            string res = BusyboxCmd("mount -o ro,remount " + path);
            if (!adb.MatchesJustPs1Output(res)) Log.Warn("Warn: " + adb.ExtractOutputOnly(res));
        }

        public void RootSuInstall()
        {
            Log.Command("Running su install ...");
            string res = adb.SendShellCmd(Constants.ANDROID_SU_INSTALL + " --install");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("su install error: " + adb.ExtractOutputOnly(res));
        }
        public void RootSuDaemon()
        {
            Log.Command("Running su daemon ...");
            string res = adb.SendShellCmd(Constants.ANDROID_SU_INSTALL + " --daemon");
            if (!adb.MatchesJustPs1Output(res)) throw new Exception("su install error: " + adb.ExtractOutputOnly(res));
        }
        public void ExitRootEnvironment()
        {
            Log.Command("Exiting root shell ...");
            do
                adb.SendShellCmd("exit");
            while (GetCurrentUid() == 0);
        }
        public bool EnterRootEnvironment(string binary)
        {
            Log.Command("Attemping to get a root shell via " + binary + " ...");
            string res = adb.SendShellCmd(binary);

            int uid = GetCurrentUid();
            if (uid != 0)
            {
                Log.Warn("Warn: failed to getting root shell: " + adb.ExtractOutputOnly(res) + " (uid: " + uid.ToString() + ") ...");
                return false;
            }

            return true;
        }


    }
}
