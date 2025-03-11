namespace PSSRoot
{ 
    class Commands : IDisposable
    {
        private AdbHelper adb;
        private string res = String.Empty;
        public Commands(AdbHelper adbHelper, byte[] busyboxBinary)
        {
            Log.Task("Setup Environment ...");

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
            res = this.BusyboxCmd("stat \"" + path + "\"");
            if (adb.ExtractOutputOnly(res).Contains("No such file or directory")) return false;
            else return true;
        }

        public string RunExploit(string path, string dest)
        {
            Log.Command("Running CVE-2016-5195 ...");
            res = adb.Shell(Constants.ANDROID_EXPLOIT + " \"" + path + "\" \"" + dest + "\" --no-pad");
            return res;
        }


        public void RemoveFile(string path)
        {
            Log.Command("Removing " + path + " ...");
            res = BusyboxCmd("rm -f \"" + path + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("touch error: " + adb.ExtractOutputOnly(res));
        }

        public int GetCurrentUid()
        {
            int uid = int.Parse(adb.ExtractOutputOnly(BusyboxCmd("id -u")));
            Log.Command("Current user id: " + uid.ToString() + " ...");

            return uid;
        }

        public string BusyboxCmd(string cmd)
        {
            res = adb.SendShellCmd(Constants.ANDROID_BUSYBOX + " " + cmd);
            Log.Debug("run busybox cmd: " + cmd + " output: " + adb.ExtractOutputOnly(res));
            return res;
        }

        public void RootChown(string path, int userId, int groupId)
        {
            Log.Command("Changing owner of " + path + " to "+userId.ToString()+":"+groupId.ToString()+" ...");
            res = this.BusyboxCmd("chown " + userId.ToString()+":"+groupId.ToString()+" \"" + path + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("chown error: " + adb.ExtractOutputOnly(res));
        }

        public void RootChmod(string path, int mode)
        {
            Log.Command("Changing " + path + " + permission to: "+mode.ToString()+" ...");
            res = this.BusyboxCmd("chmod "+ mode.ToString() +" \"" + path + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("chmod failed: " + adb.ExtractOutputOnly(res));
        }

        public void RootCatOverwriteFile(string path, string dest)
        {
            Log.Command("Writing " + path + " to " + dest + " ...");
            res = this.BusyboxCmd("cat \"" + path + "\" > \"" + dest + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("error writing " + path + ": " + adb.ExtractOutputOnly(res));
        }

        public void CopyFile(string path, string dest)
        {
            Log.Command("Copying " + path + " to " + dest + " ...");
            res = this.BusyboxCmd("cp -f \"" + path + "\" \"" + dest + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("error copying: " + adb.ExtractOutputOnly(res));
        }
        
        public void RootRemountRw(string path)
        {
            Log.Command("Remounting "+path+" as read-write ...");
            string res = BusyboxCmd("mount -o rw,remount " + path);
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("error mounting " + path + " as read-write: " + adb.ExtractOutputOnly(res));
        }

        public void RootRemountRo(string path)
        {
            Log.Command("Remounting "+path+" as read-only ...");
            string res = BusyboxCmd("mount -o ro,remount " + path);
            if (!adb.MatchesEmptyOutput(res)) Log.Warn("warn: " + adb.ExtractOutputOnly(res));
        }

        public void RootSuInstall()
        {
            Log.Command("Running su install ...");
            res = adb.SendShellCmd(Constants.ANDROID_SU_INSTALL + " --install");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("su install error: " + adb.ExtractOutputOnly(res));
        }
        public void RootSuDaemon()
        {
            Log.Command("Running su daemon ...");
            res = adb.SendShellCmd(Constants.ANDROID_SU_INSTALL + " --daemon");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("su install error: " + adb.ExtractOutputOnly(res));
        }
        public void ExitRootEnvironment()
        {
            if (adb is null) return;

            Log.Command("Exiting root shell ...");
            do
            {
                res = adb.SendShellCmd("exit");
                adb.NotifyShellChanged();
            }
            while (GetCurrentUid() == 0);

            adb.NotifyShellChanged();
        }
        public bool EnterRootEnvironment(string binary)
        {
            Log.Command("Attemping to get a root shell via " + binary + " ...");
            res = adb.SendShellCmd(binary);
            adb.NotifyShellChanged();

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
