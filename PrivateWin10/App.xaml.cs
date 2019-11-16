using Microsoft.Win32;
using QLicense;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool HasConsole = false;
        public static string[] args = null;
        public static string exePath = "";
        public static string mVersion = "0.0";
        //public static string mName = "Private WinTen";
        public static string mName = "Private Win10";
        public static string mAppName = "PrivateWin10";
#if DEBUG
        public static string mSvcName = "priv10dbg";
#else
        public static string mSvcName = "priv10";
#endif
        public static string appPath = "";
        public static string dataPath = "";
        public static bool isPortable = false;
        public static int mSession = 0;

        public static AppLog Log = null;

        public static Service svc = null;
        public static Engine engine = null;
        public static TweakManager tweaks = null;

        public static AppManager PkgMgr = null; // Windows 8 & 10 App Manager

        public static TrayIcon mTray = null;
        public static MainWindow mMainWnd = null;

        public static Priv10Client client = null;
        public static Priv10Host host = null;

        enum StartModes
        {
            Normal = 0,
            Service,
            Engine
        }

        public enum EventIDs : long
        {
            Undefined = 0x0000,

            // generic
            Exception,
            AppError,
            AppWarning,
            AppInfo,

            TweakBegin = 0x0100,
            TweakChanged,
            TweakFixed,
            TweakError,
            TweakEnd = 0x01FF,

            FirewallBegin = 0x0200,
            RuleChanged,
            RuleDeleted,
            RuleAdded,
            //FirewallNewProg
            FirewallEnd = 0x02FF,
        }

        public enum EventFlags : short
        {
            DebugEvents = 0x0100,
            AppLogEntries = 0x0200,
            Notifications = 0x0400, // Show a Notification
            PopUpMessages = 0x0800, // Show a PopUp Message
        }

        [STAThread]
        public static void Main(string[] args)
        {
            App.args = args;

            HasConsole = WinConsole.Initialize(TestArg("-console"));

            if (TestArg("-dbg_wait"))
                MessageBox.Show("Waiting for debugger. (press ok when attached)");

            StartModes startMode = StartModes.Normal; // Normal GUI Mode
            if (TestArg("-svc"))
                startMode = StartModes.Service;
            else if (TestArg("-engine"))
                startMode = StartModes.Engine;

            svc = new Service(mSvcName);

            Log = new AppLog(mAppName);
            AppLog.ExceptionLogID = (long)EventIDs.Exception;
            AppLog.ExceptionCategory = (short)EventFlags.DebugEvents;

            if (startMode == StartModes.Normal)
            {
                Log.EnableLogging();
                Log.LoadLog(); 
            }
            // When running as worker we need the windows event log
            else if (!Log.UsingEventLog())
                Log.SetupEventLog(mAppName);

            // execute service commands
            if (ExecuteCommands())
                return;

            // load current version
            exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
            mVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;
            if (fvi.FileBuildPart != 0)
                mVersion += (char)('a' + (fvi.FileBuildPart - 1));
            appPath = Path.GetDirectoryName(exePath);

            Translate.Load();

            App.LogInfo("PrivateWin10 Process Started, Mode {0}.", startMode.ToString());

            dataPath = appPath;
            if (File.Exists(GetINIPath())) // if an ini exists in the app path, its considdered to be a portable run
            {
                isPortable = true;

                AppLog.Debug("Portable Mode");
            }
            else
            {
                string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (progData == null)
                    progData = @"C:\ProgramData";

                dataPath = progData + "\\" + mAppName;
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                    FileOps.SetAnyDirSec(dataPath);
                }

                AppLog.Debug("Config Directory: {0}", progData);
            }

            mSession = Process.GetCurrentProcess().SessionId;

            // setup custom assembly resolution for x86/x64 synamic compatybility
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);

            if (!UwpFunc.IsWindows7OrLower)
            {
                Console.WriteLine("Initializing app manager...");
                PkgMgr = new AppManager();
            }

            // is the process starting as a service/worker?
            if (startMode != StartModes.Normal)
            {
                engine = new Engine();
                if(startMode == StartModes.Service)
                    ServiceBase.Run(svc);
                else
                    engine.Run();
                return;
            }

            client = new Priv10Client(mSvcName);

            // Encure wie have the required privilegs
            //if (!AdminFunc.IsDebugging())
            {
                AppLog.Debug("Trying to connect to Engine...");
                int conRes = client.Connect(1000);
                if (conRes == 0)
                {
                    if (!AdminFunc.IsAdministrator())
                    {
                        AppLog.Debug("Trying to obtain Administrative proivilegs...");
                        if (AdminFunc.SkipUacRun(mName, App.args))
                            return;

                        AppLog.Debug("Trying to start with 'runas'...");
                        // Restart program and run as admin
                        var exeName = Process.GetCurrentProcess().MainModule.FileName;
                        string arguments = "\"" + string.Join("\" \"", args) + "\"";
                        ProcessStartInfo startInfo = new ProcessStartInfo(exeName, arguments);
                        startInfo.UseShellExecute = true;
                        startInfo.Verb = "runas";
                        try
                        {
                            Process.Start(startInfo);
                            return; // we restarted as admin
                        }
                        catch
                        {
                            //MessageBox.Show(Translate.fmt("msg_admin_rights", mName), mName);
                            //return; // no point in cintinuing without admin rights or an already running engine
                        }
                    }
                    else if (svc.IsInstalled())
                    {
                        AppLog.Debug("Trying to start service...");
                        if (svc.Startup())
                        {
                            AppLog.Debug("Trying to connect to service...");

                            if (client.Connect() != 0)
                                AppLog.Debug("Connected to service...");
                            else
                                AppLog.Debug("Failed to connect to service...");
                        }
                        else
                            AppLog.Debug("Failed to start service...");
                    }
                }
                else if (conRes == -1)
                {
                    MessageBox.Show(Translate.fmt("msg_dupliate_session", mName), mName);
                    return; // no point in cintinuing without admin rights or an already running engine
                }
            }

            //

            tweaks = new TweakManager();

            // if we couldn't connect to the engine start it and connect
            if (!client.IsConnected() && AdminFunc.IsAdministrator())
            {
                AppLog.Debug("Starting Engine Thread...");

                engine = new Engine();

                engine.Start();

                AppLog.Debug("... engine started.");

                client.Connect();
            }

            var app = new App();
            app.InitializeComponent();

            InitLicense();

            mTray = new TrayIcon();
            mTray.Action += TrayAction;
            mTray.Visible = GetConfigInt("Startup", "Tray", 0) != 0;

            mMainWnd = new MainWindow();
            if (!App.TestArg("-autorun") || !mTray.Visible)
                mMainWnd.Show();

            app.Run();

            mTray.DestroyNotifyicon();

            client.Close();

            tweaks.StoreTweaks();

            if (engine != null)
                engine.Stop();
        }

        static private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            string strTempAssmbPath = "";

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.
                    //The following line is probably the only line of code in this method you may need to modify:
                    if(System.Environment.Is64BitProcess)
                        strTempAssmbPath = appPath + @"\x64\";
                    else
                        strTempAssmbPath = appPath + @"\x86\";
                    strTempAssmbPath += args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }
            }

            //Load the assembly from the specified path.
            Assembly MyAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return MyAssembly;
        }

        static bool ExecuteCommands()
        {
            if (TestArg("-help") || TestArg("/?"))
            {
                string Message = "Available command line options\r\n";
                string[] Help = {
                                    "Available Console Commands:",
                                    "========================================",
                                    "",
                                    "-state\t\t\tShow instalation state",
                                    "-uninstall\t\tUninstall PrivateWinTen",
                                    "",
                                    "-svc_install\t\tInstall priv10 service (invokes -log_install)",
                                    "-svc_remove\t\tRemove priv10 service",
                                    "",
                                    "-log_install\t\tInstall PrivateWin10 Custom Event Log",
                                    "-log_remove\t\tRemove PrivateWin10 Custom Event Log",
                                    "",
                                    "-console\t\tShow console with debug output",
                                    "-help\t\t\tShow this help message" };
                if (!HasConsole)
                    MessageBox.Show(Message + string.Join("\r\n", Help));
                else
                {
                    Console.WriteLine(Message);
                    for (int j = 0; j < Help.Length; j++)
                        Console.WriteLine(" " + Help[j]);
                }
                return true;
            }

           
            bool bDone = false;

            if (TestArg("-svc_install"))
            {
                AppLog.Debug("Installing Service...");
                svc.Install(TestArg("-svc_start"));
                bDone = true;
            }

            if (TestArg("-log_install") || TestArg("-svc_install")) // service needs the event log
            {
                AppLog.Debug("Setting up Event Log...");
                Log.SetupEventLog(mAppName);
                bDone = true;
            }

            if (TestArg("-uninstall"))
            {
                AppLog.Debug("Uninstalling PrivateWinTen");
                bDone = true;
            }

            if (TestArg("-svc_remove") || (svc.IsInstalled() && TestArg("-uninstall")))
            {
                AppLog.Debug("Removing Service...");
                svc.Uninstall();
                bDone = true;
            }

            if (TestArg("-log_remove") || (Log.UsingEventLog() && TestArg("-uninstall")))
            {
                AppLog.Debug("Removing Event Log...");
                Log.RemoveEventLog(mAppName);
                bDone = true;
            }

            if (TestArg("-uninstall") && AdminFunc.IsSkipUac(mName))
            {
                AppLog.Debug("Removing UAC Bypass...");
                AdminFunc.SkipUacEnable(mName, false);
                bDone = true;
            }

            if (TestArg("-uninstall") && App.IsAutoStart())
            {
                AppLog.Debug("Removing Autostart...");
                App.AutoStart(false);
                bDone = true;
            }

            if (bDone)
                AppLog.Debug("done");


            if (TestArg("-state"))
            {
                Console.WriteLine();
                Console.WriteLine("Instalation State:");
                Console.WriteLine("========================="); // 25
                Console.Write("Auto Start:\t");
                Console.WriteLine(App.IsAutoStart());
                Console.Write("UAC Bypass:\t");
                Console.WriteLine(AdminFunc.IsSkipUac(mName));
                Console.Write("Event Log:\t");
                Console.WriteLine(Log.UsingEventLog());
                Console.Write("Service:\t");
                Console.WriteLine(svc.IsInstalled());
                Console.WriteLine();
            }

            if (TestArg("-wait"))
            {
                Console.WriteLine();
                for (int i = 10; i >= 0; i--)
                {
                    Console.Write("\r{0}%   ", i);
                    Thread.Sleep(1000);
                }
            }

            return bDone;
        }

        static void TrayAction(object sender, TrayIcon.TrayEventArgs args)
        {
            switch (args.Action)
            {
                case TrayIcon.Actions.ToggleWindow:
                    {
                        if (mMainWnd.IsVisible)
                            mMainWnd.Hide();
                        else
                            mMainWnd.Show();
                        break;
                    }
                case TrayIcon.Actions.CloseApplication:
                    {
                        if (svc.IsInstalled())
                        {
                            MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_stop_svc"), App.mName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            switch (res)
                            {
                                case MessageBoxResult.Yes:
                                    if(!client.Quit())
                                        MessageBox.Show(Translate.fmt("msg_stop_svc_err"), App.mName, MessageBoxButton.OK, MessageBoxImage.Stop);
                                    break;
                                case MessageBoxResult.Cancel:
                                    return;
                            }
                        }
                        Application.Current.Shutdown();
                        break;
                    }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Misc Helpers

        static public bool TestArg(string name)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static public string GetArg(string name)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    string temp = App.args[i + 1];
                    if (temp.Length > 0 && temp[0] != '-')
                        return temp;
                    return "";
                }
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Config

        static public string GetConfig(string Section, string Key, string Default = "")
        {
            return IniReadValue(Section, Key, Default);
        }

        static public int GetConfigInt(string Section, string Key, int Default = 0)
        {
            return MiscFunc.parseInt(IniReadValue(Section, Key, Default.ToString()));
        }

        static public void SetConfig(string Section, string Key, bool Value)
        {
            SetConfig(Section, Key, Value ? 1 : 0);
        }

        static public void SetConfig(string Section, string Key, int Value)
        {
            IniWriteValue(Section, Key, Value.ToString());
        }

        static public void SetConfig(string Section, string Key, string Value)
        {
            IniWriteValue(Section, Key, Value);
        }

        private static string GetINIPath() { return dataPath + "\\" + mAppName + ".ini"; }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        public static void IniWriteValue(string Section, string Key, string Value, string INIPath = null)
        {
            WritePrivateProfileString(Section, Key, Value, INIPath != null ? INIPath : GetINIPath());
        }

        public static void IniDeleteSection(string Section, string INIPath = null)
        {
            WritePrivateProfileString(Section, null, null, INIPath != null ? INIPath : GetINIPath());
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, [In, Out] char[] retVal, int size, string filePath);
        public static string IniReadValue(string Section, string Key, string Default = "", string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(Section, Key, Default, chars, 8193, INIPath != null ? INIPath : GetINIPath());
            /*int size = GetPrivateProfileString(Section, Key, "\xff", chars, 8193, INIPath != null ? INIPath : GetINIPath());
            if (size == 1 && chars[0] == '\xff')
            {
                WritePrivateProfileString(Section, Key, Default, INIPath != null ? INIPath : GetINIPath());
                return Default;
            }*/
            return new String(chars, 0, size);
        }

        public static List<string> IniEnumSections(string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(null, null, null, chars, 8193, INIPath != null ? INIPath : GetINIPath());
            return TextHelpers.SplitStr(new String(chars, 0, size), "\0");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Start & Restart

        public static void AutoStart(bool enable)
        {
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (enable)
            {
                string value = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " -autorun";
                subKey.SetValue("PrivateWin10", value);
            }
            else
                subKey.DeleteValue("PrivateWin10", false);
        }

        public static bool IsAutoStart()
        {
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            return (subKey != null && subKey.GetValue("PrivateWin10") != null);
        }

        public static void Restart(bool RunAs = false, bool bService = false)
        {
            if (bService && App.svc.IsInstalled())
            {
                App.svc.Terminate();
                App.svc.Startup();
            }

            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            string arguments = "\"" + string.Join("\" \"", args) + "\"";
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName, arguments);
            startInfo.UseShellExecute = true;
            if (RunAs)
                startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
                Environment.Exit(-1);
            }
            catch
            {
                //MessageBox.Show(Translate.fmt("msg_admin_req", mName), mName);
                App.LogWarning("Failed to restart Application");
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Event Logging

        static public void LogCriticalError(string message, params object[] args)
        {
#if DEBUG
            Debugger.Break();
#endif
            LogError("Critical Error: " + message, args);
        }

        static public void LogError(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Error, (long)EventIDs.AppError, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogError(App.EventIDs eventID, Dictionary<string, string> Params, App.EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Error, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }

        static public void LogWarning(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Warning, (long)EventIDs.AppWarning, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogWarning(App.EventIDs eventID, Dictionary<string, string> Params, App.EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Warning, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }

        static public void LogInfo(string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Information, (long)EventIDs.AppInfo, (short)EventFlags.AppLogEntries, args.Length == 0 ? message : string.Format(message, args));
        }

        static public void LogInfo(App.EventIDs eventID, Dictionary<string, string> Params, App.EventFlags flags, string message, params object[] args)
        {
            AppLog.Add(EventLogEntryType.Information, (long)eventID, (short)flags, args.Length == 0 ? message : string.Format(message, args), Params);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Licensing

        public static MyLicense lic = null;

        public static void InitLicense()
        {
            MemoryStream mem = new MemoryStream();
            Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateWin10.LicenseVerify.cer").CopyTo(mem);
            byte[] certPubicKeyData = mem.ToArray();

            string msg = string.Empty;
            LicenseStatus status = LicenseStatus.UNDEFINED;
            if (File.Exists("license.lic"))
                lic = (MyLicense)LicenseHandler.ParseLicenseFromBASE64String(typeof(MyLicense), File.ReadAllText("license.lic"), certPubicKeyData, out status, out msg);
            else
                msg = "Your copy of this application is not activated";
            if (lic == null)
                lic = new MyLicense(); // we always want this object
            lic.LicenseStatus = status;
            if (status != LicenseStatus.VALID && msg.Length == 0)
                msg = "Your license file is invalid or broken";

            if (status == LicenseStatus.INVALID || status == LicenseStatus.CRACKED)
                MessageBox.Show(msg, App.mName, MessageBoxButton.OK, MessageBoxImage.Error);

            Console.WriteLine(msg);
        }
    }
}
