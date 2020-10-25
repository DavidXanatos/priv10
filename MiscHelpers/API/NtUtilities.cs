using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiscHelpers
{
    public class NtUtilities
    {
        public static string NtOsKrnlPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\ntoskrnl.exe");

        public static string Shell32Path = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\shell32.dll");

        public static string parsePath(string path)
        {
            try
            {
                if (path.Contains(@"\device\mup\"))
                    return @"\" + path.Substring(11, path.Length - 11);
                string[] strArray = path.Split(new char[1] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string vol = @"\" + strArray[0] + @"\" + strArray[1];
                path = path.Replace(vol, GetDriveLetter(vol));
                if (path.Contains('~'))
                    path = Path.GetFullPath(path);
                return path;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return "";
        }
        
        [DllImport("kernel32.dll")]
        public static extern uint QueryDosDevice(string lpDeviceName, [In, Out] char[] lpTargetPath, int ucchMax);

        private static Dictionary<string, Tuple<string, UInt64>> DriveLetterCache = new Dictionary<string, Tuple<string, UInt64>>();
        private static ReaderWriterLockSlim DriveLetterCacheLock = new ReaderWriterLockSlim();

        private static string GetDriveLetter(string longPath)
        {
            Tuple<string, UInt64> temp;
            DriveLetterCacheLock.EnterReadLock();
            if (DriveLetterCache.TryGetValue(longPath.ToLower(), out temp))
            {
                if (temp.Item2 > MiscFunc.GetTickCount64())
                {
                    DriveLetterCacheLock.ExitReadLock();
                    return temp.Item1;
                }
                DriveLetterCache.Remove(longPath.ToLower());
            }
            DriveLetterCacheLock.ExitReadLock();

            // ToDo: build a cache on WM_DEVICECHANGE

            string ret = null;
            char[] lpTargetPath = new char[260 + 1];
            for (char ltr = 'A'; ltr <= 'Z'; ltr++)
            {
                uint size = QueryDosDevice(ltr + ":", lpTargetPath, 260);
                if (size > 0 && longPath.Equals(new String(lpTargetPath, 0, (int)size - 2), StringComparison.OrdinalIgnoreCase))
                {
                    ret = ltr + ":";
                    break;
                }
            }

            if (ret == null)
                return "?:";

            DriveLetterCacheLock.EnterWriteLock();
            if (DriveLetterCache.ContainsKey(longPath.ToLower()) == false)
                DriveLetterCache.Add(longPath.ToLower(), new Tuple<string, UInt64>(ret, MiscFunc.GetTickCount64() + 1 * 60 * 1000)); // cahce values for 1 minutes
            DriveLetterCacheLock.ExitWriteLock();
            return ret;
        }

        public static string GetExeDescription(string appPath)
        {
            string descr = null;
            if (File.Exists(appPath))
            {
                try
                {
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(appPath);
                    descr = info?.FileDescription;
                }
                catch { }
            }
            return (descr != null && descr.Length > 0) ? descr : "";
        }



        private static string Sys32Path = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\");
        private static string SysWOWPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\SysWOW64\");

        public static bool IsWindowsBinary(string path)
        {
            if (path == null || path.Length == 0)
                return false;

            if (path.IndexOf(Sys32Path, StringComparison.OrdinalIgnoreCase) == 0 || path.IndexOf(SysWOWPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                try
                {
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);

                    bool isMS = info.CompanyName == "Microsoft Corporation";

                    return isMS;
                }
                catch { }
            }

            return false;
        }
    }
}
