using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using MiscHelpers;
using System.Drawing;
using TweakEngine;
using System.Windows.Forms;
using PrivateWin10;
using PrivateAPI;

namespace PrivateService
{
    static class App
    {
        public static bool HasConsole = false;
        public static string[] args = null;
        public static string exePath = "";
        //public static string Version = "0.0";
        public static string Key = "PrivateWin10";
#if DEBUG
        public static string SvcName = "priv10dbg";
#else
        public static string SvcName = "priv10";
#endif
        public static string appPath = "";
        public static string dataPath = "";
        public static bool isPortable = false;

        public static AppLog Log = null;

        public static Priv10Engine engine = null;

        public static Priv10Host host = null;

        enum StartModes
        {
            Normal = 0,
            Service,
            Engine
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
            AppLog.ExceptionLogID = (long)Priv10Logger.EventIDs.Exception;
            AppLog.ExceptionCategory = (short)Priv10Logger.EventFlags.DebugEvents;

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
            //*FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
            //Version = fvi.FileMajorPart + "." + fvi.FileMinorPart;
            //if (fvi.FileBuildPart != 0)
            //    Version += "." + fvi.FileBuildPart;
            //if (fvi.FilePrivatePart != 0)
            //    Version += (char)('a' + (fvi.FilePrivatePart - 1));
            appPath = Path.GetDirectoryName(exePath);

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

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            if(AdminFunc.IsAdministrator())
                FileOps.SetAnyDirSec(dataPath);

            Priv10Logger.LogInfo("PrivateWin10 Service Process Started, Mode {0}.", startMode.ToString());

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

        static public string GetResourceStr(string resourcePath)
        {
            if (resourcePath == null)
                return "";
            if (resourcePath.Length == 0 || resourcePath[0] != '@')
                return resourcePath;

            // modern app resource string
            if (resourcePath.Length > 2 && resourcePath.Substring(0, 2) == "@{")
                return App.engine?.PkgMgr?.GetAppResourceStr(resourcePath) ?? resourcePath;
            
            // classic win32 resource string
            return MiscFunc.GetResourceStr(resourcePath);
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
    }
}
