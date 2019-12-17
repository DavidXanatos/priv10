using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;

namespace PrivateSetup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Title = "Private Win10 - Setup";
        public static string[] args = null;
        public static bool HasConsole = false;
        public static string exePath = "";
        public static string appPath = "";
        public static string exeName = "PrivateSetup.exe";
        public static Packer packer;


        protected override void OnStartup(StartupEventArgs e)
        {
            // Class "ReflectionContext" exists from .NET 4.5 onwards.
            if (Type.GetType("System.Reflection.ReflectionContext", false) == null)
            {
                MessageBox.Show(string.Format("{0} requirers .NET Framework 4.5 or newer. In order to run {0} please update your .NET installation.", App.Title) , App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
                Environment.Exit(0);
            }

            args = Environment.GetCommandLineArgs();

            HasConsole = WinConsole.Initialize(TestArg("-console"));

            if (TestArg("-dbg_wait"))
                MessageBox.Show("Waiting for debugger. (press ok when attached)");

            if (TestArg("-dbg_log"))
                AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;

            App.exePath = Assembly.GetExecutingAssembly().Location;
            App.appPath = Path.GetDirectoryName(exePath);
            //App.exeName = Path.GetFileName(exePath);
            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
            //App.Title = fvi.FileDescription; // PrivateWin10 - Setup
            //App.exeName = fvi.OriginalFilename; // PrivateSetup.exe

            base.OnStartup(e);
        }

        static private void FirstChanceExceptionHandler(object source, FirstChanceExceptionEventArgs e)
        {
            Console.WriteLine("FirstChanceException event raised in {0}: {1}\r\n{2}", AppDomain.CurrentDomain.FriendlyName, e.Exception.Message, e.Exception.StackTrace);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(HasConsole)
                Console.WriteLine("\r\n\r\n{0} Starting...", Title);

            packer = new Packer();

            string prepare = GetArg("-prepare");
            if (prepare != null)
            {
                bool ret = packer.PrepareSetup(prepare);
                Environment.Exit(ret ? 0 : -1);
            }

            string target = GetArg("-extract");
            if (target != null)
            {
                bool ret = packer.Extract(target);
                Environment.Exit(ret ? 0 : -1);
            }


            if (packer.IsValid())
            {
                bool ret = packer.Test();
                if (!ret)
                {
                    ShowMessage("{0} file is corrupted! Please download a working copy.", App.Title);
                    Environment.Exit(-1);
                }
            }
            /*else if(!App.TestArg("-uninstall"))
            {
                ShowMessage("Setup is empty, please run PrivateSetup.exe -prepare");
                Environment.Exit(-1);
            }*/

#if DEBUG
            if (TestArg("-test"))
            {
                bool ret = packer.Test();
                Environment.Exit(ret ? 0 : -1);
            }

            if (TestArg("-enum"))
            {
                packer.Enum();
                Environment.Exit(0);
            }
#endif

            /*if (TestArg("-empty"))
            {
                installer.Empty();
                Environment.Exit(0);
            }*/
            

            SetupData Data = SetupData.FromArgs();
            bool Uninstaller = App.TestArg("-uninstall") || !packer.IsValid(false);

            SetupWindow wnd = new SetupWindow(Data, Uninstaller);
            wnd.Show();
        }

        public static void LogMessage(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        public static void ShowMessage(string message, params object[] args)
        {
            Console.WriteLine(message, args);
            if (!HasConsole)
                MessageBox.Show(string.Format(message, args), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool Restart(string[] args, bool bFromTemp = false)
        {
            string arguments = "\"" + string.Join("\" \"", args) + "\"";
            string fileName = exePath;
            if (bFromTemp)
            {
                arguments += " \"-temp\"";

                fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_" + App.exeName);
                File.Copy(exePath, fileName);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments);
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
                Environment.Exit(0);
                return true;
            }
            catch
            {
                //MessageBox.Show("Failed to restart with administrative privilegs, setup aborted", App.Title);
                //Environment.Exit(-1);
                return false;
            }
        }

        static public bool TestArg(string name)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static public string GetArg(string name, string def = null)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    string temp = App.args.Length <= (i + 1) ? "" : App.args[i + 1];
                    if (temp.Length > 0 && temp[0] != '-')
                        return temp;
                    return "";
                }
            }
            return def;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Config

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        public static void IniWriteValue(string INIPath, string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, INIPath);
        }

        public static void IniDeleteSection(string INIPath, string Section)
        {
            WritePrivateProfileString(Section, null, null, INIPath);
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, [In, Out] char[] retVal, int size, string filePath);
        public static string IniReadValue(string INIPath, string Section, string Key, string Default = "")
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(Section, Key, Default, chars, 8193, INIPath);
            /*int size = GetPrivateProfileString(Section, Key, "\xff", chars, 8193, INIPath);
            if (size == 1 && chars[0] == '\xff')
            {
                WritePrivateProfileString(Section, Key, Default, INIPath != null ? INIPath : GetINIPath());
                return Default;
            }*/
            return new String(chars, 0, size);
        }

        public static List<string> IniEnumSections(string INIPath)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(null, null, null, chars, 8193, INIPath);
            return new String(chars, 0, size).Split('\0').ToList();
        }

    }
}

