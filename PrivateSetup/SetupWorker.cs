
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateSetup
{
    public class SetupWorker
    {
        static string AutoRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        static string InstalListFile = "install.lst";
        static string LicenseFile = "license.lic";

        static List<string> UnsafeFolders = new List<string>() {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) ,
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        public static bool IsUnsafePath(string installPath)
        {
            var driveLetter = new Regex("^[a-zA-Z]:\\\\?$");
            if (driveLetter.IsMatch(installPath))
                return true;
            return UnsafeFolders.Contains(installPath, StringComparer.OrdinalIgnoreCase);
        }

        public event EventHandler<EventArgs> Finished;
        public class ProgressArgs : EventArgs
        {
            public int Progress;
            public string Message = null;
            public bool Show = false;
        }
        public event EventHandler<ProgressArgs> Progress;

        SetupData Data;

        public SetupWorker(SetupData Data)
        {
            this.Data = Data;
        }

        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(Run));
            thread.Name = "SetupWorker";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void Run()
        {
            /*for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);

                Progress?.Invoke(this, new ProgressArgs() { Progress = i });
            }*/

            switch (Data.Action)
            {
                case SetupData.Actions.Extract:     Extract(); break;
                case SetupData.Actions.Install:     Install(); break;
                case SetupData.Actions.Update:      Update(); break;
                case SetupData.Actions.Uninstall:   Remove(); break;
            }

            Finished?.Invoke(this, new EventArgs());
        }

        private List<String> Extract(bool Install = false)
        {
            List<String> fileList = new List<string>();

            List<Packer.FileInfo> files = App.packer.EnumFiles();

            string ExtractPath = Data.InstallationPath;
            if (!Install)
            {
                if (ExtractPath.Contains(App.appPath))
                    ExtractPath = "." + ExtractPath.Remove(0, App.appPath.Length);
            }

            for (int i=0; i < files.Count; i++)
            {
                var file = files[i];

                string targetPath = Data.InstallationPath + @"\" + file.FileName;
                fileList.Add(targetPath);

                if (!Install && File.Exists(targetPath))
                {
                    string hashStr;
                    using (var md5 = MD5.Create())
                    {
                        using (var inStream = File.OpenRead(targetPath))
                        {
                            byte[] hash = md5.ComputeHash(inStream);
                            hashStr = BitConverter.ToString(hash).Replace("-", "");
                        }
                    }
                    if (hashStr.Equals(file.Hash))
                    {
#if DEBUG
                        Progress?.Invoke(this, new ProgressArgs() { Progress = (Install ? 75 : 100) * (i + 1) / files.Count, Message = "Skipping: " + ExtractPath + @"\" + file.FileName });
#endif
                        continue;
                    }
                }

                string Message = (Install ? "Installing:" : "Extracting: ") + ExtractPath + @"\" + file.FileName;
                Progress?.Invoke(this, new ProgressArgs() { Progress = (Install ? 75 : 100) * i / files.Count, Message = Message });

                Message = null;
                if (!App.packer.ExtractFile(file, Data.InstallationPath))
                    Message = "Extraction failed!";

                Progress?.Invoke(this, new ProgressArgs() { Progress = (Install ? 75 : 100) * (i + 1) / files.Count, Message = Message });
                Thread.Sleep(10);
            }

            string IniPath;
            if (Install)
            {
                string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (progData == null)
                    progData = @"C:\ProgramData";

                IniPath = progData + "\\" + SetupData.AppKey;
            }
            else // Note: when the ini file ins inside the application directory the app starts in portable mode
                IniPath = Data.InstallationPath + @"\Data";

            if (!Directory.Exists(IniPath))
                Directory.CreateDirectory(IniPath);
            MiscFunc.SetAnyDirSec(IniPath); // ensure access for non admins

            IniPath += @"\" + SetupData.AppKey + ".ini";

            App.IniWriteValue(IniPath, "Startup", "Usage", Data.Use.ToString());

            if (Data.LicenseFile.Length > 0)
            {
                try
                {
                    File.Copy(Data.LicenseFile, Data.InstallationPath + @"\" + LicenseFile);
                }
                catch
                {
                    string Message = "Failed to copy license file to:" + Data.InstallationPath + @"\" + LicenseFile;
                    Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = Message, Show = true});
                }
            }

            if (Install)
            {
                using (StreamWriter indexStram = new StreamWriter(Data.InstallationPath + @"\" + InstalListFile))
                {
                    foreach (var filePath in fileList)
                        indexStram.WriteLine(filePath);
                }
            }
            else
            {
                Progress?.Invoke(this, new ProgressArgs() { Progress = 100, Message = "Extraction completed." });
                Thread.Sleep(100);
            }

            return fileList;
        }

        private void Install()
        {
            Extract(true);

            Progress?.Invoke(this, new ProgressArgs() { Progress = 75, Message = "Updating Registry..." });
            if (!Data.UpdateRegistry())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to update the Registry", Show = true });

            if (Data.AutoStart)
            {
                using (var subKey = Registry.CurrentUser.CreateSubKey(AutoRunKey))
                {
                    string value = "\"" + Data.InstallationPath + @"\" + SetupData.AppBinary + "\"" + " -autorun";
                    subKey.SetValue(SetupData.AppKey, value);
                }
            }
            Thread.Sleep(100);

            Progress?.Invoke(this, new ProgressArgs() { Progress = 85, Message = "Creatign priv10 Service..." });
            if (!InstallSvc())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to create Service", Show = true });
            Thread.Sleep(100);

            Progress?.Invoke(this, new ProgressArgs() { Progress = 95, Message = "Creatign Start Menu Entries..." });
            if (!CreateLinks())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to create Start Menu Entries", Show = true });
            Thread.Sleep(100);

            Progress?.Invoke(this, new ProgressArgs() { Progress = 100, Message = "Instalation completed." });
            Thread.Sleep(100);
        }

        private bool InstallSvc()
        {
            try
            {
                // This installs service and event log 
                Process proc = Process.Start(Data.InstallationPath + @"\" + SetupData.AppBinary, "-svc_install");
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool UpdateSvc()
        {
            try
            {
                // This updates the service
                Process proc = Process.Start(Data.InstallationPath + @"\" + SetupData.AppBinary, "-svc_update");
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool CreateLinks()
        {
            try
            {
                string lnkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), SetupData.AppTitle + ".lnk");

                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkPath);

                shortcut.Description = SetupData.AppTitle;
                shortcut.WorkingDirectory = Data.InstallationPath;
                //shortcut.IconLocation = @"C:\Program Files (x86)\TestApp\TestApp.ico"; //uncomment to set the icon of the shortcut
                shortcut.TargetPath = Data.InstallationPath + @"\" + SetupData.AppBinary; ;
                shortcut.Save();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return false;
            }
            return true;
        }

        private void Update()
        {
            Progress?.Invoke(this, new ProgressArgs() { Progress = 0, Message = "Clossing running instances..." });
            Shutdown();

            List<string> oldList = ReadFileList(); // get a list of old files

            List<string> fileList = Extract(true); // note: this updates install.lst

            // remove old files
            if (oldList != null)
            {
                foreach (var filePath in oldList)
                {
                    if (!fileList.Contains(filePath, StringComparer.OrdinalIgnoreCase))
                    {
                        // don't touch files outside the instalation directory
                        if (!filePath.Substring(0, Data.InstallationPath.Length).Equals(Data.InstallationPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Removing: " + filePath });
                        if (!MiscFunc.SafeDelete(filePath))
                            Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Removing failed!", Show = true });
                    }
                }
            }


            Progress?.Invoke(this, new ProgressArgs() { Progress = 75, Message = "Updating Registry..." });
            if (!Data.UpdateRegistry())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to update the Registry", Show = true });
            Thread.Sleep(100);

            /*try
            {
                RestartService();
            }
            catch { }*/

            Progress?.Invoke(this, new ProgressArgs() { Progress = 85, Message = "Updating priv10 Service..." });
            if (!UpdateSvc())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to update Service", Show = true });
            Thread.Sleep(100);

            Progress?.Invoke(this, new ProgressArgs() { Progress = 100, Message = "Update completed." });
            Thread.Sleep(100);
        }

        private List<string> ReadFileList()
        {
            List<string> fileList = null;
            if (File.Exists(Data.InstallationPath + @"\" + InstalListFile))
            {
                fileList = new List<string>();
                using (StreamReader indexStram = new StreamReader(Data.InstallationPath + @"\" + InstalListFile))
                {
                    while (!indexStram.EndOfStream)
                    {
                        var filePath = indexStram.ReadLine();
                        fileList.Add(filePath);
                    }
                }
            }
            return fileList;
        }

        private bool Shutdown()
        {
            try
            {
                Process proc = Process.Start(Data.InstallationPath + @"\" + SetupData.AppBinary, "-shutdown");
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /*private void RestartService()
        {
            ServiceController controller = new ServiceController("priv10");
            if (controller.Status == ServiceControllerStatus.Stopped)
                controller.Start();
            Progress?.Invoke(this, new ProgressArgs() { Progress = 90, Message = "Service Restarted" });
        }*/

        private void Remove()
        {
            Progress?.Invoke(this, new ProgressArgs() { Progress = 10, Message = "Removing priv10 Service..." });
            if (!Uninstall())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to remove Service", Show = true });
            Thread.Sleep(100);


            List<string> fileList = ReadFileList(); // get a list of old files
            if (fileList != null)
            {
                for (int i = 0; i < fileList.Count; i++)
                {
                    var filePath = fileList[i];

                    // don't touch files outside the instalation directory
                    if (!filePath.Substring(0, Data.InstallationPath.Length).Equals(Data.InstallationPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Progress?.Invoke(this, new ProgressArgs() { Progress = 20 + 30 * (i + 1) / fileList.Count, Message = "Removing: " + filePath });
                    if (!MiscFunc.SafeDelete(filePath))
                        Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Removing failed!", Show = true });
                }
                MiscFunc.SafeDelete(Data.InstallationPath + @"\" + InstalListFile);
                MiscFunc.SafeDelete(Data.InstallationPath + @"\" + LicenseFile);

                // In case the user installed the application where he shouldn't have
                if (IsUnsafePath(Data.InstallationPath))
                    Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Can not clean up instalation folder!", Show = true });
                else if (!MiscFunc.DeleteEmptyDir(Data.InstallationPath))
                {
                    string Message = "Failed to remove the application directory" + Data.InstallationPath + "\r\nThe folder is eider not empty or in use.";
                    Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = Message, Show = true });
                }
            }
            else
            {
                string Message = "install.lst not found in: " + Data.InstallationPath + "\r\nPlease remove the applciatrion files manually.";
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = Message, Show = true });
            }



            Progress?.Invoke(this, new ProgressArgs() { Progress = 50, Message = "Removing Start Menu Entries..." });
            string lnkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), SetupData.AppTitle + ".lnk");
            MiscFunc.SafeDelete(lnkPath);
            Thread.Sleep(100);

            if (Data.RemoveUserData)
            {
                Progress?.Invoke(this, new ProgressArgs() { Progress = 60, Message = "Removing configuration data..." });

                string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (progData == null)
                    progData = @"C:\ProgramData";

                var IniPath = progData + "\\" + SetupData.AppKey;
                if (Directory.Exists(IniPath))
                {
                    var userFiles = MiscFunc.EnumAllFiles(IniPath);
                    foreach (var filePath in userFiles)
                        MiscFunc.SafeDelete(filePath);

                    if (!MiscFunc.DeleteEmptyDir(IniPath))
                    {
                        string Message = "Failed to remove the application data" + IniPath + "\r\nThe folder is eider not empty or in use.";
                        Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = Message, Show = true });
                    }
                }
            }

            Progress?.Invoke(this, new ProgressArgs() { Progress = 80, Message = "Cleaningup Registry..." });
            if (!Data.CleanupRegistry())
                Progress?.Invoke(this, new ProgressArgs() { Progress = -1, Message = "Failed to cleanup Registry", Show = true });
            Thread.Sleep(100);

            if (Data.ResetFirewall)
            {
                Progress?.Invoke(this, new ProgressArgs() { Progress = 90, Message = "Restoring default Firewall configuration..." });
                MiscFunc.Exec("netsh.exe", "advfirewall reset");
            }

            Progress?.Invoke(this, new ProgressArgs() { Progress = 100, Message = "Uninstalation completed." });
            Thread.Sleep(100);
        }

        private bool Uninstall()
        {
            try
            {
                // this undos all critical changes, removes service, removed event log, restores DNS, etc....
                Process proc = Process.Start(Data.InstallationPath + @"\" + SetupData.AppBinary, "-uninstall");
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
