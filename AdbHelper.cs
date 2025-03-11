using System.Diagnostics;

namespace PSSRoot
{
    public class AdbHelper : IDisposable
    {
        private string? AdbExe = null;
        private string? AdbWinApi = null;
        private string? AdbUsbApi = null;

        private string? Ps1AfterProcessing = null;
        Process shell = new Process();

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
            shell.Start();

            readUntilBashPrompt();
            NotifyShellChanged();
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

            this.AdbExe = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "adb.exe");
            this.AdbWinApi = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "AdbWinApi.dll");
            this.AdbUsbApi = Path.Combine(Constants.PSS_ROOT_TMP_FOLDER, "AdbWinUsbApi.dll");

            if(!File.Exists(this.AdbExe))
            {
                Log.Command("Extracting " + Path.GetFileName(AdbExe) + " ...");
                File.WriteAllBytes(this.AdbExe, RootResources.adb);

                Log.Command("Extracting " + Path.GetFileName(AdbWinApi) + " ...");
                File.WriteAllBytes(this.AdbWinApi, RootResources.AdbWinApi);

                Log.Command("Extracting " + Path.GetFileName(AdbUsbApi) + " ...");
                File.WriteAllBytes(this.AdbUsbApi, RootResources.AdbWinUsbApi);
            }

            return;
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

        public void NotifyShellChanged()
        {
            SendShellCmd("");
            Ps1AfterProcessing = SendShellCmd("");
            Log.Debug("Shell ps1: \"" + Ps1AfterProcessing + "\" ...");
        }

        public string SendShellCmd(string command)
        {
            shell.StandardInput.WriteLine(command);
            string res = readUntilBashPrompt();

            int pos = res.IndexOf('\n') + 1;
            if (pos != -1 && res.Length >= pos) res = res.Substring(pos);

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
