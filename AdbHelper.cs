using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSSRoot
{
    public class AdbHelper : IDisposable
    {
        private string? AdbExe = null;

        private string? Ps1AfterProcessing = null;

        Process shell = new Process();

        // linux only import :
        
        [DllImport("libc")]
        private static extern int setenv(string name, string value, bool overwrite);

        [DllImport("libc")]
        private static extern int chmod(string path, int mode);

        private string setupLibaryPath(string env)
        {
            string linuxLibPath = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "lib64");

            string? libaryPath = Environment.GetEnvironmentVariable(env);
            if (libaryPath is null) libaryPath = linuxLibPath;
            else libaryPath += ";" + linuxLibPath;

            Environment.SetEnvironmentVariable(env, libaryPath);
            setenv(env, libaryPath, true);

            return libaryPath;
        }


        public AdbHelper()
        {
            Log.Task("Setting up ADB ...");
            extractAdb();
            startShell();
        }

        private string readUntilBashPrompt()
        {
            string s = "";
            char c = '\0';
            do
            {
                int asciichar = shell.StandardOutput.Read();
                if (asciichar == -1) break;

                c = Convert.ToChar(asciichar);
                if ((c < ' ' || c > '~') && c != '\n') continue;
                s += c;

            } while (!shell.HasExited && (c != '$' && c != '#'));

            s = s.Trim();

            if (shell.HasExited && shell.ExitCode != 0) s = shell.StandardError.ReadToEnd();
            if (s.Contains("no devices/emulators found")) throw new Exception("ADB: no device connected");
            if (shell.HasExited && shell.ExitCode != 0) throw new Exception("ADB: exited with error code " + shell.ExitCode);

            return s;
        }
        private void startShell()
        {
            if (AdbExe is null) throw new NullReferenceException("ADB: ADB_EXE is null");
            Log.Command("Starting adb shell session ...");

            shell.StartInfo.FileName = AdbExe;
            shell.StartInfo.WorkingDirectory = Path.GetDirectoryName(AdbExe);
            shell.StartInfo.Arguments = "shell";
            shell.StartInfo.RedirectStandardInput = true;
            shell.StartInfo.RedirectStandardOutput = true;
            shell.StartInfo.RedirectStandardError = true;

            // add LD_LIBRARY_PATH if linux or MacOS.
            if (OperatingSystem.IsLinux())
                shell.StartInfo.Environment.Add("LD_LIBRARY_PATH", setupLibaryPath("LD_LIBRARY_PATH"));
            if (OperatingSystem.IsMacOS())
                shell.StartInfo.Environment.Add("DYLD_LIBRARY_PATH", setupLibaryPath("DYLD_LIBRARY_PATH"));

            shell.Start();

            readUntilBashPrompt();
            UpdatePs1String();
        }
        private void killRunningAdb()
        {
            Log.Command("Exiting any running adb instances ...");

            if (AdbExe is not null) adbCmd("kill-server");
            foreach (Process p in Process.GetProcessesByName("adb")) { p.Kill(); p.WaitForExit(); };
            
        }
        private void extractAdb()
        {
            killRunningAdb();

            if (Directory.Exists(Constants.PSS_ROOT_TMP_FOLDER)) Directory.Delete(Constants.PSS_ROOT_TMP_FOLDER, true);
            Directory.CreateDirectory(Constants.PSS_ROOT_TMP_FOLDER);

            if (OperatingSystem.IsWindows())
            {
                this.AdbExe = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "adb.exe");
                string adbWinApi = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "AdbWinApi.dll");
                string adbUsbApi = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "AdbWinUsbApi.dll");

                if (!File.Exists(this.AdbExe))
                {
                    Log.Command("Extracting " + Path.GetFileName(AdbExe) + " ...");
                    File.WriteAllBytes(this.AdbExe, RootResources.adb_win);

                    Log.Command("Extracting " + Path.GetFileName(adbWinApi) + " ...");
                    File.WriteAllBytes(adbWinApi, RootResources.AdbWinApi_win);

                    Log.Command("Extracting " + Path.GetFileName(adbUsbApi) + " ...");
                    File.WriteAllBytes(adbUsbApi, RootResources.AdbWinUsbApi_win);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                string linuxLibDirectory = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "lib64");
                Directory.CreateDirectory(linuxLibDirectory);

                this.AdbExe = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "adb");
                string cppLib = Path.Combine(linuxLibDirectory, "libc++.so");

                if (!File.Exists(this.AdbExe))
                {
                    Log.Command("Extracting " + Path.GetFileName(AdbExe) + " ...");
                    File.WriteAllBytes(this.AdbExe, RootResources.adb_lin);
                    chmod(this.AdbExe, Constants.ANDROID_MODE_EXECUTABLE);

                    Log.Command("Extracting " + Path.GetFileName(cppLib) + " ...");
                    File.WriteAllBytes(cppLib, RootResources.libcxx_lin);
                    chmod(cppLib, Constants.ANDROID_MODE_EXECUTABLE);
                }
            }
            else if(OperatingSystem.IsMacOS())
            {
                string macLibDirectory = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "lib64");
                Directory.CreateDirectory(macLibDirectory);

                this.AdbExe = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "adb");
                string cppLib = Path.Combine(macLibDirectory, "libc++.dylib");


                if (!File.Exists(this.AdbExe))
                {
                    Log.Command("Extracting " + Path.GetFileName(AdbExe) + " ...");
                    File.WriteAllBytes(this.AdbExe, RootResources.adb_mac);
                    chmod(this.AdbExe, Constants.ANDROID_MODE_EXECUTABLE);

                    Log.Command("Extracting " + Path.GetFileName(cppLib) + " ...");
                    File.WriteAllBytes(cppLib, RootResources.libcxx_mac);
                    chmod(cppLib, Constants.ANDROID_MODE_EXECUTABLE);
                }
            }
        }

        private string adbCmd(string command)
        {
            if (AdbExe is null) throw new NullReferenceException("ADB: ADB_EXE is null");


            string s = "";
            using (Process adb = new Process())
            {
                adb.StartInfo.FileName = AdbExe;
                adb.StartInfo.WorkingDirectory = Path.GetDirectoryName(AdbExe);
                adb.StartInfo.Arguments = command;
                adb.StartInfo.RedirectStandardOutput = true;
                adb.StartInfo.RedirectStandardError = true;

                // add LD_LIBRARY_PATH if linux.
                if (OperatingSystem.IsLinux())
                    shell.StartInfo.Environment.Add("LD_LIBRARY_PATH", setupLibaryPath("LD_LIBRARY_PATH"));
                if (OperatingSystem.IsMacOS())
                    shell.StartInfo.Environment.Add("DYLD_LIBRARY_PATH", setupLibaryPath("DYLD_LIBRARY_PATH"));


                adb.Start();
                adb.WaitForExit();

                s += adb.StandardOutput.ReadToEnd();
                s += adb.StandardError.ReadToEnd();

                if (s.Contains("no devices/emulators found")) throw new Exception("ADB: no device connected");
                if (adb.ExitCode != 0) throw new Exception("ADB: exited with error code " + adb.ExitCode);

            }

            Log.Debug("adbCmd: " + command + " out: " + s);
            return s;
        }

        public void UpdatePs1String()
        {
            SendShellCmd(String.Empty);
            Ps1AfterProcessing = SendShellCmd(String.Empty);
            Log.Debug("shell ps1: \"" + Ps1AfterProcessing + "\" ...");
        }

        public string SendShellCmd(string command)
        {
            shell.StandardInput.WriteLine(command);
            string res = readUntilBashPrompt();

            int pos = res.IndexOf('\n') + 1;
            if (pos != -1 && res.Length >= pos) res = res.Substring(pos);

            // on android 4.1 apparenty the PS1 changes to include exit code of last process,
            // which is annoying, guess ill just read it every single time
            if (!String.IsNullOrEmpty(command)) UpdatePs1String();

            return res;
        }
        public void Push(string srcFile, string dstFile)
        {
            adbCmd("push \"" + srcFile + "\" \"" + dstFile + "\"");
        }

        public void Pull(string srcFile, string dstFile)
        {
            adbCmd("pull \"" + srcFile + "\" \"" + dstFile + "\"");
        }

        public string ExtractOutputOnly(string txt)
        {
            if (Ps1AfterProcessing is null) return txt;
            return txt.Replace(Ps1AfterProcessing.Replace('$', '#'), String.Empty).Replace(Ps1AfterProcessing.Replace('#', '$'), String.Empty).ReplaceLineEndings("");
        }
        public bool MatchesEmptyOutput(string txt)
        {
            if (Ps1AfterProcessing is null) return false;

            bool matchesNoRoot = txt.Equals(Ps1AfterProcessing.Replace('#', '$'), StringComparison.InvariantCultureIgnoreCase);
            bool matchesRoot = txt.Equals(Ps1AfterProcessing.Replace('$', '#'), StringComparison.InvariantCultureIgnoreCase);
            return (matchesNoRoot || matchesRoot);
        }

        public string Shell(string command)
        {
            return adbCmd("shell " + command);
        }

        public string Install(string Apk)
        {
            return adbCmd("install -r \"" + Apk + "\"");
        }

        public void UploadExecutable(byte[] data, string filename)
        {

            Log.Command("Uploading executable " + filename + " ...");

            string resTmpPath = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "resource.bin");
            File.WriteAllBytes(resTmpPath, data);

            Push(resTmpPath, filename);
            Shell("chmod " + Constants.ANDROID_MODE_EXECUTABLE.ToString() + " \"" + filename + "\"");

            File.Delete(resTmpPath);

        }

        public void InstallApk(byte[] data)
        {

            string resTmpPath = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "resource.apk");
            File.WriteAllBytes(resTmpPath, data);
            this.Install(resTmpPath);
            File.Delete(resTmpPath);

        }

        public void Dispose()
        {
            Log.Task("Cleaning up ADB ...");

            Log.Command("Exiting adb shell");
            shell.Kill();
            shell.Dispose();

            killRunningAdb();

            Log.Command("Cleaning up pssroot temporary folder");
            if (Directory.Exists(Constants.PSS_ROOT_TMP_FOLDER)) Directory.Delete(Constants.PSS_ROOT_TMP_FOLDER, true);
        }
    }
}
