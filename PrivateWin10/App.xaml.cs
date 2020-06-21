using Microsoft.Win32;
using QLicense;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
        public static string Version = "0.0";
        public static string Title = "Private Win10";
        public static string Key = "PrivateWin10";
#if DEBUG
        public static string SvcName = "priv10dbg";
#else
        public static string SvcName = "priv10";
#endif
        public static string appPath = "";
        public static string dataPath = "";
        public static bool isPortable = false;
        public static int Session = 0;

        public static AppLog Log = null;

        public static Priv10Engine engine = null;
        public static TweakManager tweaks = null;

        public static TrayIcon TrayIcon = null;
        public static MainWindow MainWnd = null;

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

            if (TestArg("-dbg_log"))
                AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;

            StartModes startMode = StartModes.Normal; // Normal GUI Mode
            if (TestArg("-svc"))
                startMode = StartModes.Service;
            else if (TestArg("-engine"))
                startMode = StartModes.Engine;

            Log = new AppLog(Key);
            AppLog.ExceptionLogID = (long)EventIDs.Exception;
            AppLog.ExceptionCategory = (short)EventFlags.DebugEvents;

            if (startMode == StartModes.Normal)
            {
                Log.EnableLogging();
                Log.LoadLog(); 
            }
            // When running as worker we need the windows event log
            else if (!Log.UsingEventLog())
                Log.SetupEventLog(Key);

            // load current version
            exePath = Process.GetCurrentProcess().MainModule.FileName; //System.Reflection.Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
            Version = fvi.FileMajorPart + "." + fvi.FileMinorPart;
            if (fvi.FileBuildPart != 0)
                Version += "." + fvi.FileBuildPart;
            if (fvi.FilePrivatePart != 0)
                Version += (char)('a' + (fvi.FilePrivatePart - 1));
            appPath = Path.GetDirectoryName(exePath);

            Translate.Load();

            dataPath = appPath + @"\Data";
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

                dataPath = progData + "\\" + Key;
            }

            AppLog.Debug("Config Directory: {0}", dataPath);

            // execute commandline commands
            if (ExecuteCommands())
                return;

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            if(AdminFunc.IsAdministrator())
                FileOps.SetAnyDirSec(dataPath);

            App.LogInfo("PrivateWin10 Process Started, Mode {0}.", startMode.ToString());

            Session = Process.GetCurrentProcess().SessionId;

            // setup custom assembly resolution for x86/x64 synamic compatybility
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;

            // is the process starting as a service/worker?
            if (startMode != StartModes.Normal)
            {
                engine = new Priv10Engine();
                if (startMode == StartModes.Service)
                {
                    using (Priv10Service svc = new Priv10Service())
                        ServiceBase.Run(svc);
                }
                else
                    engine.Run();
                return;
            }

            Thread.CurrentThread.Name = "Gui";

            client = new Priv10Client();

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
                        if (AdminFunc.SkipUacRun(App.Key, App.args))
                            return;

                        AppLog.Debug("Trying to start with 'runas'...");
                        // Restart program and run as admin
                        string arguments = "\"" + string.Join("\" \"", args) + "\"";
                        ProcessStartInfo startInfo = new ProcessStartInfo(exePath, arguments);
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
                    else if (Priv10Service.IsInstalled())
                    {
                        AppLog.Debug("Trying to start service...");
                        if (Priv10Service.Startup())
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
                    MessageBox.Show(Translate.fmt("msg_dupliate_session", Title), Title);
                    client.Close();
                    return; // no point in cintinuing without admin rights or an already running engine
                }
            }

            //

            tweaks = new TweakManager();

            // if we couldn't connect to the engine start it and connect
            if (!client.IsConnected() && AdminFunc.IsAdministrator())
            {
                AppLog.Debug("Starting Engine Thread...");

                engine = new Priv10Engine();

                engine.Start();

                AppLog.Debug("... engine started.");

                client.Connect();
            }

            var app = new App();
            app.InitializeComponent();

            InitLicense();

            MainWnd = new MainWindow();

            TrayIcon = new TrayIcon();
            TrayIcon.Action += TrayAction;
            TrayIcon.Visible = (GetConfigInt("Startup", "Tray", 0) != 0) || App.TestArg("-autorun");

            if (!App.TestArg("-autorun") || !TrayIcon.Visible)
                MainWnd.Show();

            app.Run();

            TrayIcon.DestroyNotifyicon();

            client.Close();

            tweaks.Store();

            if (engine != null)
                engine.Stop();
        }

        static private Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
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

        static private void FirstChanceExceptionHandler(object source, FirstChanceExceptionEventArgs e)
        {
            AppLog.Debug("FirstChanceException event raised in {0}: {1}\r\n{2}", AppDomain.CurrentDomain.FriendlyName, e.Exception.Message, e.Exception.StackTrace);
        }

        //https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
        void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var comException = e.Exception as System.Runtime.InteropServices.COMException;

            if (comException != null && comException.ErrorCode == -2147221040)
                e.Handled = true;
        }

        static private Dictionary<string, string> AppResourceStrCache = new Dictionary<string, string>();
        static private ReaderWriterLockSlim AppResourceStrLock = new ReaderWriterLockSlim();

        static public string GetResourceStr(string resourcePath)
        {
            if (resourcePath == null)
                return "";
            if (resourcePath.Length == 0 || resourcePath[0] != '@')
                return resourcePath;

            string resourceStr = null;
            AppResourceStrLock.EnterReadLock();
            AppResourceStrCache.TryGetValue(resourcePath, out resourceStr);
            AppResourceStrLock.ExitReadLock();
            if (resourceStr != null)
                return resourceStr;

            if (resourcePath.Length > 2 && resourcePath.Substring(0, 2) == "@{")
            {
                if (App.engine != null)
                    resourceStr = App.engine?.PkgMgr?.GetAppResourceStr(resourcePath) ?? resourcePath;
                else
                    resourceStr = App.client.GetAppPkgRes(resourcePath);
            }
            else
                resourceStr = MiscFunc.GetResourceStr(resourcePath);

            AppResourceStrLock.EnterWriteLock();
            if (!AppResourceStrCache.ContainsKey(resourcePath))
                AppResourceStrCache.Add(resourcePath, resourceStr);
            AppResourceStrLock.ExitWriteLock();

            return resourceStr;
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
                                    "-uninstall\t\tUninstall Private Win10",
                                    "-shutdown\t\tClose Private Win10 instances",
                                    "-restart\t\tRestart Win10 and reload settings",
                                    "",
                                    "-svc_install\t\tInstall priv10 service (invokes -log_install)",
                                    "-svc_remove\t\tRemove priv10 service",
                                    "",
                                    "-log_install\t\tInstall PrivateWin10 Custom Event Log",
                                    "-log_remove\t\tRemove PrivateWin10 Custom Event Log",
                                    "",
                                    "-restore_dns\t\tRestore original DNS Configuration",
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

            if (TestArg("-uninstall"))
            {
                AppLog.Debug("Uninstalling Private Win10");
                bDone = true;
            }

            if (TestArg("-svc_remove") || (Priv10Service.IsInstalled() && TestArg("-uninstall")))
            {
                AppLog.Debug("Removing Service...");
                Priv10Service.Uninstall();
                bDone = true;
            }

            if (TestArg("-shutdown") || TestArg("-restart") || TestArg("-restore") || TestArg("-uninstall"))
            {
                AppLog.Debug("Closing instances...");
                if(Priv10Service.IsInstalled())
                    Priv10Service.Terminate();

                Thread.Sleep(500);

                foreach (var proc in Process.GetProcessesByName(App.Key))
                {
                    if (proc.Id == ProcFunc.CurID)
                        continue;
                    proc.Kill();
                }

                bDone = true;
            }

            if (TestArg("-restore"))
            {
                string zipPath = GetArg("-restore");

                try
                {
                    if (zipPath == null || !File.Exists(zipPath))
                        throw new Exception("Data backup zip not specifyed or invalid path");

                    Console.WriteLine("Restoring settings from {0}", zipPath);

                    string extractPath = App.dataPath;

                    // Normalizes the path.
                    extractPath = Path.GetFullPath(extractPath);

                    // Ensures that the last character on the extraction path
                    // is the directory separator char. 
                    // Without this, a malicious zip file could try to traverse outside of the expected
                    // extraction path.
                    if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                        extractPath += Path.DirectorySeparatorChar;

                    // create data directory
                    if (!Directory.Exists(dataPath))
                        Directory.CreateDirectory(dataPath);

                    // ensure its writable by non administrators
                    FileOps.SetAnyDirSec(dataPath);

                    // Extract the backuped files
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Gets the full path to ensure that relative segments are removed.
                            string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                            // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                            // are case-insensitive.
                            if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                continue;

                            Console.WriteLine("Restored file {0}", entry.FullName);
                            if (File.Exists(destinationPath))
                                FileOps.DeleteFile(destinationPath);
                            else if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            
                            entry.ExtractToFile(destinationPath);
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                    MessageBox.Show(Translate.fmt("msg_restore_error", err.Message), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
                }

                bDone = true;
            }

            if (TestArg("-restart") || TestArg("-restore"))
            {
                Thread.Sleep(500);

                AppLog.Debug("Starting instances...");
                if (Priv10Service.IsInstalled())
                    Priv10Service.Startup();

                Thread.Sleep(500);

                ProcessStartInfo startInfo = new ProcessStartInfo(App.exePath);
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                
                bDone = true;
            }

            if (TestArg("-log_remove") || (Log.UsingEventLog() && TestArg("-uninstall")))
            {
                AppLog.Debug("Removing Event Log...");
                Log.RemoveEventLog(Key);
                bDone = true;
            }
            
            if (TestArg("-svc_install"))
            {
                AppLog.Debug("Installing Service...");
                Priv10Service.Install(TestArg("-svc_start"));
                bDone = true;
            }

            if (TestArg("-log_install") || TestArg("-svc_install")) // service needs the event log
            {
                AppLog.Debug("Setting up Event Log...");
                Log.SetupEventLog(Key);
                bDone = true;
            }

            if (TestArg("-restore_dns") || (DnsConfigurator.IsAnyLocalDNS() && TestArg("-uninstall")))
            {
                AppLog.Debug("Restoring DNS Config...");
                DnsConfigurator.RestoreDNS();
                bDone = true;
            }

            if (TestArg("-uninstall") && AdminFunc.IsSkipUac(App.Key))
            {
                AppLog.Debug("Removing UAC Bypass...");
                AdminFunc.SkipUacEnable(App.Key, false);
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
                Console.WriteLine(AdminFunc.IsSkipUac(App.Key));
                Console.Write("Service:\t");
                Console.WriteLine(Priv10Service.IsInstalled());
                Console.Write("Event Log:\t");
                Console.WriteLine(Log.UsingEventLog());
                Console.Write("Local DNS:\t");
                Console.WriteLine(DnsConfigurator.IsAnyLocalDNS());
                Console.WriteLine();
                bDone = true;
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
            if (MainWnd == null || !MainWnd.FullyLoaded)
                return;

            switch (args.Action)
            {
                case TrayIcon.Actions.ToggleWindow:
                    {
                        if (MainWnd.IsVisible)
                            MainWnd.Hide();
                        else
                            MainWnd.Show();
                        break;
                    }
                case TrayIcon.Actions.ToggleNotify:
                    {
                        if (MainWnd.notificationWnd.IsVisible)
                            MainWnd.notificationWnd.HideWnd();
                        else if (!MainWnd.notificationWnd.IsEmpty())
                            MainWnd.notificationWnd.ShowWnd();
                        break;
                    }
                case TrayIcon.Actions.CloseApplication:
                    {
                        if (Priv10Service.IsInstalled() && AdminFunc.IsAdministrator())
                        {
                            MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_stop_svc"), App.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            switch (res)
                            {
                                case MessageBoxResult.Yes:
                                    if(!Priv10Service.Terminate())
                                        MessageBox.Show(Translate.fmt("msg_stop_svc_err"), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
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

        private static string GetINIPath() { return dataPath + "\\" + Key + ".ini"; }

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
                subKey.SetValue(App.Key, value);
            }
            else
                subKey.DeleteValue(App.Key, false);
        }

        public static bool IsAutoStart()
        {
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            return (subKey != null && subKey.GetValue(App.Key) != null);
        }

        public static void Restart(bool RunAs = false/*, bool bService = false*/)
        {
            /*if (bService && Priv10Service.IsInstalled())
            {
                Priv10Service.Terminate();
                Priv10Service.Startup();
            }*/

            string arguments = "\"" + string.Join("\" \"", args) + "\"";
            ProcessStartInfo startInfo = new ProcessStartInfo(exePath, arguments);
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
                lic = (MyLicense)LicenseHandler.ParseLicenseFromBASE64String(typeof(MyLicense), File.ReadAllText(App.appPath + @"\license.lic"), certPubicKeyData, out status, out msg);
            else
                msg = "Your copy of this application is not activated";
            if (lic == null)
                lic = new MyLicense(); // we always want this object
            lic.LicenseStatus = status;
            if (status != LicenseStatus.VALID && msg.Length == 0)
                msg = "Your license file is invalid or broken";

            if (status == LicenseStatus.INVALID || status == LicenseStatus.CRACKED)
                MessageBox.Show(msg, App.Title, MessageBoxButton.OK, MessageBoxImage.Error);

            Console.WriteLine(msg);

            if (status != LicenseStatus.VALID)
            {
                string use = App.GetConfig("Startup", "Usage", "");
                if (use.Equals("business", StringComparison.OrdinalIgnoreCase) || use.Equals("commertial", StringComparison.OrdinalIgnoreCase))
                {
                    lic.CommercialUse = true;

                    if (IsEvaluationExpired())
                    {
                        MessageBox.Show("The commercial evaluation period of PrivateWin10 has expired, please purchase an appropriate license.", string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        public static DateTime GetInstallDate()
        {
            FileInfo info = new FileInfo(exePath);
            return MiscFunc.Min(info.CreationTime, info.LastWriteTime);
        }

        public static bool IsEvaluationExpired()
        {
            return App.GetInstallDate().AddDays(30) < DateTime.Now;
        }
    }
}
