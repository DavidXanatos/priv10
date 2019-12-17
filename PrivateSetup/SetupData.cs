using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PrivateSetup
{
    public class SetupData
    {
        static public string AppTitle = "Private Win10";
        static public string AppKey = "PrivateWin10";
        static public string AppBinary = "PrivateWin10.exe";
        public string AppVersion = "0.00";
        public string CurVersion = null;

        public enum Uses
        {
            Undefined = 0,
            Personal,
            Commertial
        }
        public Uses Use = Uses.Undefined;
        public string LicenseFile = "";

        public enum Actions
        {
            Undefined = 0,
            Install,
            Update,
            Extract,
            Uninstall
        }
        public Actions Action = Actions.Undefined;
        public string InstallationPath = "";
        public bool IsInstalled = false;
        public bool AutoStart = true;
        public bool ResetFirewall = false;
        public bool RemoveUserData = false;


        public SetupData()
        {
            InstallationPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\" + AppKey;

            using (RegistryKey uninstKey = Registry.LocalMachine.OpenSubKey(UninstallKey + @"\" + AppKey))
            {
                if (uninstKey != null)
                {
                    IsInstalled = true;

                    string installPath = uninstKey.GetValue("InstallationPath") as string;
                    if (installPath != null)
                        InstallationPath = installPath;

                    CurVersion = uninstKey.GetValue("DisplayVersion") as string;
                }
            }

            var curVer = Assembly.GetExecutingAssembly().GetName().Version;

            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(App.exePath);
            AppVersion = curVer.Major + "." + curVer.Minor;
            if (curVer.Build != 0)
                AppVersion += "." + curVer.Build;
            if (curVer.Revision != 0)
                AppVersion += (char)('a' + (curVer.Revision - 1));
        }

        static public SetupData FromArgs()
        {
            SetupData Data = new SetupData();

            string use = App.GetArg("-use", "");
            if (use.Equals("personal", StringComparison.OrdinalIgnoreCase) || use.Equals("private", StringComparison.OrdinalIgnoreCase))
                Data.Use = SetupData.Uses.Personal;
            else if (use.Equals("business", StringComparison.OrdinalIgnoreCase) || use.Equals("commertial", StringComparison.OrdinalIgnoreCase))
            {
                Data.Use = SetupData.Uses.Personal;
                Data.LicenseFile = App.GetArg("-license", "");
            }

            string action = App.GetArg("-action");
            if (action != null)
                Enum.TryParse(action, out Data.Action);
            string instDir = App.GetArg("-install_dir");
            if (instDir != null)
                Data.InstallationPath = instDir;
            Data.AutoStart = App.TestArg("-auto_start");
            Data.ResetFirewall = App.TestArg("-reset_fw");
            Data.RemoveUserData = App.TestArg("-clear_data");

            return Data;
        }

        public string[] MakeArgs()
        {
            var args = new List<string>();

            args.Add("-use");
            args.Add(Use.ToString());
            if (Use == Uses.Commertial)
            {
                args.Add("-license");
                args.Add(LicenseFile);
            }

            args.Add("-action");
            args.Add(Action.ToString());

            args.Add("-install_dir");
            args.Add(InstallationPath);

            if (AutoStart)
                args.Add("-auto_start");

            if (ResetFirewall)
                args.Add("-reset_fw");

            if (RemoveUserData)
                args.Add("-clear_data");

            return args.ToArray();
        }


        static public string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        static public string AppsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths";

        public bool UpdateRegistry()
        {
            try
            {
                using (RegistryKey uninstKey = Registry.LocalMachine.CreateSubKey(UninstallKey + @"\" + AppKey))
                {
                    uninstKey.SetValue("DisplayName", AppTitle);

                    uninstKey.SetValue("InstallationPath", InstallationPath);
                    uninstKey.SetValue("UninstallString", (InstallationPath + @"\" + App.exeName + " -Uninstall"));

                    uninstKey.SetValue("DisplayVersion", AppVersion);
                    uninstKey.SetValue("DisplayIcon", InstallationPath + @"\" + AppBinary);
                    uninstKey.SetValue("EstimatedSize", MiscFunc.GetDirSize(InstallationPath)/1024, RegistryValueKind.DWord);

                    uninstKey.SetValue("URLInfoAbout", "https://github.com/DavidXanatos/");
                    uninstKey.SetValue("HelpLink", "xanatosdavid" + "\x40" + "gmail.com");
                    uninstKey.SetValue("Publisher", "David Xanatos");
                }

                // https://docs.microsoft.com/en-us/windows/win32/shell/app-registration
                using (RegistryKey appKey = Registry.LocalMachine.CreateSubKey(AppsKey + @"\" + AppBinary))
                {
                    appKey?.SetValue("", InstallationPath + @"\" + AppBinary);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return false;
            }
            return true;
        }


        public bool CleanupRegistry()
        {
            try
            {
                using (RegistryKey appKey = Registry.LocalMachine.OpenSubKey(AppsKey, true))
                    appKey?.DeleteSubKeyTree(AppBinary, false);

                using (RegistryKey uninstKey = Registry.LocalMachine.OpenSubKey(UninstallKey, true))
                    uninstKey?.DeleteSubKeyTree(AppKey, false);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return false;
            }
            return true;
        }
    }
}
