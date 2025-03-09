using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSRoot
{
    public class AdbHelper : IDisposable
    {
        private string? ADB_EXE = null;
        private string? ADB_WIN_API = null;
        private string? ADB_WIN_USB_API = null;

        private string? emptyShellLine = null;
        Process shell = new Process();

        public AdbHelper()
        {
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
            if (s.Contains("no devices/emulators found")) throw new Exception("[*] ADB: no device connected");
            if (shell.HasExited && shell.ExitCode != 0) throw new Exception("[*] ADB: exited with error code " + shell.ExitCode);

            return s;
        }
        private void startShell()
        {
            if (ADB_EXE is null) throw new NullReferenceException("ADB: ADB_EXE is null");
            Console.WriteLine("[*] Starting adb shell session ...");

            shell.StartInfo.FileName = ADB_EXE;
            shell.StartInfo.WorkingDirectory = Path.GetDirectoryName(ADB_EXE);
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
            Console.WriteLine("[*] Quitting previous adb instances ...");

            if (ADB_EXE is not null) adbCmd("kill-server");
            foreach (Process p in Process.GetProcessesByName("adb")) { p.Kill(); p.WaitForExit(); };
            
        }
        private void extractAdb()
        {
            Console.WriteLine("[*] Extracting adb ...");
            killRunningAdb();

            string TMP_FOLDER = Path.Combine(Path.GetTempPath(), "pssroot");

            if (Directory.Exists(TMP_FOLDER)) Directory.Delete(TMP_FOLDER, true);
            Directory.CreateDirectory(TMP_FOLDER);


            ADB_EXE = Path.Combine(Path.GetTempPath(), "pssroot", "adb.exe");
            ADB_WIN_API = Path.Combine(Path.GetTempPath(), "pssroot", "AdbWinApi.dll");
            ADB_WIN_USB_API = Path.Combine(Path.GetTempPath(), "pssroot", "AdbWinUsbApi.dll");

            if(!File.Exists(ADB_EXE))
            {
                File.WriteAllBytes(ADB_EXE, RootResources.adb);
                File.WriteAllBytes(ADB_WIN_API, RootResources.AdbWinApi);
                File.WriteAllBytes(ADB_WIN_USB_API, RootResources.AdbWinUsbApi);
            }

            return;
        }

        private string adbCmd(string command)
        {
            if (ADB_EXE is null) throw new NullReferenceException("[*] ADB: ADB_EXE is null");

            string s = "";
            using (Process adb = new Process())
            {
                adb.StartInfo.FileName = ADB_EXE;
                adb.StartInfo.WorkingDirectory = Path.GetDirectoryName(ADB_EXE);
                adb.StartInfo.Arguments = command;
                adb.StartInfo.RedirectStandardOutput = true;
                adb.StartInfo.RedirectStandardError = true;
                adb.Start();
                adb.WaitForExit();

                s += adb.StandardOutput.ReadToEnd();
                s += adb.StandardError.ReadToEnd();

                if (s.Contains("no devices/emulators found")) throw new Exception("[*] ADB: no device connected");
                if (adb.ExitCode != 0) throw new Exception("[*] ADB: exited with error code " + adb.ExitCode);

            }
            return s;
        }

        public void NotifyShellChanged()
        {
            SendShellCmd("");
            emptyShellLine = SendShellCmd("");
            Console.WriteLine("[*] Shell header: \"" + emptyShellLine + "\"");
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
            adbCmd("shell chmod 777 \"" + dstFile + "\"");
        }

        public void Pull(string srcFile, string dstFile)
        {
            adbCmd("push \"" + srcFile + "\" \"" + dstFile + "\"");
        }

        public bool MatchesEmptyOutput(string txt)
        {
            if (emptyShellLine is null) return false;

            bool matchesNoRoot = txt.Equals(emptyShellLine.Replace('#', '$'), StringComparison.InvariantCultureIgnoreCase);
            bool matchesRoot = txt.Equals(emptyShellLine.Replace('$', '#'), StringComparison.InvariantCultureIgnoreCase);
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

        public void Dispose()
        {
            shell.Kill();
            shell.Dispose();
            killRunningAdb();
        }
    }
}
