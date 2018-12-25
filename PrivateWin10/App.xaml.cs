using Microsoft.Win32;
using PrivateWin10.IPC;
using PrivateWin10.Windows;
using QLicense;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
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
        public static bool mConsole = false;
        public static string[] args = null;
        public static string exePath = "";
        public static string mVersion = "0.0";
        public static string mName = "Private Winten";
        public static string mSvcName = "priv10";
        public static string appPath = "";
        public static int mSession = 0;

        public static Service svc = null;
        public static Engine engine = null;
        public static Tweaks tweaks = null;

        public static TrayIcon mTray = null;

        public static MainWindow mMainWnd = null;


        public static IPCInterface itf;
        public static IPCCallback cb;
        public static PipeClient client;
        public static PipeHost host;

        [STAThread]
        public static void Main(string[] args)
        {
            App.args = args;

            mConsole = WinConsole.Initialize(TestArg("-console") || TestArg("-console-debug"));

            if (TestArg("-help") || TestArg("/?"))
            {
                ShowHelp();
                return;
            }
            else if (TestArg("-dbg_wait"))
                MessageBox.Show("Waiting for debugger. (press ok when attached)");

            Thread.CurrentThread.Name = "Main";

            Console.WriteLine("Starting...");

            AppLog Log = new AppLog();

            exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
            mVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;
            if (fvi.FileBuildPart != 0)
                mVersion += (char)('a' + (fvi.FileBuildPart - 1));
            appPath = Path.GetDirectoryName(exePath);
            mSession = Process.GetCurrentProcess().SessionId;

            Translate.Load();

            svc = new Service(mSvcName);

            if (TestArg("-engine"))
            {
                engine = new Engine();

                engine.Run();
                return;
            }
            else if (TestArg("-svc"))
            {
                if (TestArg("-install"))
                {
                    Console.WriteLine("Installing service...");
                    svc.Install(TestArg("-start"));
                    Console.WriteLine("... done");
                }
                else if (TestArg("-remove"))
                {
                    Console.WriteLine("Removing service...");
                    svc.Uninstall();
                    Console.WriteLine("... done");
                }
                else
                {
                    engine = new Engine();

                    ServiceBase.Run(svc);
                }
                return;
            }

            tweaks = new Tweaks();

            client = new PipeClient();

            if (!AdminFunc.IsDebugging())
            {
                Console.WriteLine("Trying to connect to Engine...");
                if (!client.Connect(1000))
                {
                    if (!AdminFunc.IsAdministrator())
                    {
                        Console.WriteLine("Trying to obtain Administrative proivilegs...");
                        if (AdminFunc.SkipUacRun(mName))
                            return;

                        Console.WriteLine("Trying to start with 'runas'...");
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
                            MessageBox.Show(Translate.fmt("msg_admin_rights", mName), mName);
                            return; // no point in cintinuing without admin rights or an already running engine
                        }
                    }
                    else if (svc.IsInstalled())
                    {
                        Console.WriteLine("Trying to start service...");
                        if (svc.Startup())
                        {
                            Console.WriteLine("Trying to connect to service...");

                            if(client.Connect())
                                Console.WriteLine("Connected to service...");
                            else
                                Console.WriteLine("Failed to connect to service...");
                        }
                        else
                            Console.WriteLine("Failed to start service...");
                    }
                }
            }

            // if we couldn't connect to the engine start it and connect
            if (!client.IsConnected() && AdminFunc.IsAdministrator())
            {
                Console.WriteLine("Starting Engine Thread...");

                engine = new Engine();

                engine.Start();

                Console.WriteLine("... engine started.");

                client.Connect();
            }

            // ToDo: use a more direct communication when running in one process

            itf = client;
            cb = client;

            /*if (TestArg("-console-debug"))
            {
                Console.WriteLine("Private Winten reporting for duty, sir!");
                Console.WriteLine("");

                for (bool running = true; running;)
                {
                    String Line = Console.ReadLine();
                    if (Line.Length == 0)
                        continue;

                    String Command = TextHelpers.GetLeft(ref Line).ToLower();

                    if (Command == "quit" || Command == "exit")
                        running = false;

                    if (Command == "test")
                    {
                    }
                    else
                    {
                        Console.WriteLine("Unknown Command, sir!");
                        continue;
                    }
                    Console.WriteLine("Yes, sir!");
                }

                return;
            }*/

            Console.WriteLine("Preparing GUI...");

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

            if(engine != null)
                engine.Stop();
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
                        Application.Current.Shutdown();
                        break;
                    }
            }
        }


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

        static public bool TestArg(string name)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        static public string GetArg(string name)
        {
            for (int i = 0; i < App.args.Length; i++)
            {
                if (App.args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    string temp = App.args[i + 1];
                    if (temp.Length > 0 && temp[0] != '-')
                        return temp;
                    return "";
                }
            }
            return null;
        }

        private static string GetINIPath() { return appPath + @"\Config.ini"; }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        public static void IniWriteValue(string Section, string Key, string Value, string INIPath = null)
        {
            WritePrivateProfileString(Section, Key, Value, INIPath != null ? INIPath : GetINIPath());
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, [In, Out] char[] retVal, int size, string filePath);
        public static string IniReadValue(string Section, string Key, string Default = "", string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(Section, Key, Default, chars, 8193, INIPath != null ? INIPath : GetINIPath());
            return new String(chars, 0, size);
        }

        public static string[] IniEnumSections(string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(null, null, null, chars, 8193, INIPath != null ? INIPath : GetINIPath());
            return new String(chars, 0, size).Split('\0');
        }

        private static void ShowHelp()
        {
            string Message = "Available command line options\r\n";
            string[] Help = {
                                    "-console\t\tshow console (for debugging)",
                                    "-help\t\tShow this help message" };
            if (!mConsole)
                MessageBox.Show(Message + string.Join("\r\n", Help));
            else
            {
                Console.WriteLine(Message);
                for (int j = 0; j < Help.Length; j++)
                    Console.WriteLine(" " + Help[j]);
            }
        }


        public static void AutoStart(bool enable)
        {
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
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
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return (subKey != null && subKey.GetValue("PrivateWin10") != null);
        }

        public static long GetInstallDate()
        {
            FileInfo info = new FileInfo(exePath);
            return Math.Max(((DateTimeOffset)info.CreationTime).ToUnixTimeSeconds(), ((DateTimeOffset)info.LastWriteTime).ToUnixTimeSeconds());
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
            if(RunAs)
                startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
                Environment.Exit(-1);
            }
            catch
            {
                //MessageBox.Show(Translate.fmt("msg_admin_req", mName), mName);
                AppLog.Line("Failed to restart Application");
            }
        }

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
            if(lic == null)
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
