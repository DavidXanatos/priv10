using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateSetup
{
    static public class MiscFunc
    {
        public static UInt64 GetUTCTime()
        {
            return (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }

        public static UInt64 GetUTCTimeMs()
        {
            return (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
        }

        public static UInt64 DateTime2Ms(DateTime dateTime)
        {
            return (UInt64)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
        }

        static public List<string> EnumAllFiles(string sourcePath)
        {
            List<string> files = new List<string>();

            foreach (string fileName in Directory.GetFiles(sourcePath))
                files.Add(fileName);

            foreach (string dirName in Directory.GetDirectories(sourcePath))
            {
                if ((new DirectoryInfo(dirName).Attributes & FileAttributes.ReparsePoint) != 0)
                    continue; // skip junctions

                files.AddRange(EnumAllFiles(dirName));
            }

            return files;
        }

        public static long GetDirSize(string targetPath)
        {
            long totalSize = 0;
            if (Directory.Exists(targetPath))
            {
                foreach (var file in Directory.GetFiles(targetPath))
                {
                    totalSize += new FileInfo(file).Length;
                }

                foreach (var directory in Directory.GetDirectories(targetPath))
                {
                    totalSize += GetDirSize(directory);
                }
            }
            return totalSize;
        }

        static public string FormatSize(decimal size)
        {
            if (size > 1024 * 1024 * 1024)
                return (size / (1024 * 1024 * 1024)).ToString("F") + " GB";
            if (size > 1024 * 1024)
                return (size / (1024 * 1024)).ToString("F") + " MB";
            if (size > 1024)
                return (size / (1024)).ToString("F") + " KB";
            return ((Int64)size).ToString() + " B";
        }

        public static bool SafeDelete(string targetPath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.SetAttributes(targetPath, FileAttributes.Normal); // clear read only attribute, just in case
                        File.Delete(targetPath);
                    }
                    return true;
                }
                catch
                {
                    Thread.Sleep(1000 * (i + 1));
                }
            }
            return false;
        }

        public static bool DeleteEmptyDir(string targetPath)
        {
            try
            {
                bool Success = true;

                if (Directory.Exists(targetPath))
                {
                    if (Directory.GetFiles(targetPath).Length > 0)
                        return false;

                    foreach (var directory in Directory.GetDirectories(targetPath))
                    {
                        if (!DeleteEmptyDir(directory))
                            Success = false;
                    }
                }

                if (Success)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            Directory.Delete(targetPath);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(500 * (i + 1));
                        }
                    }
                }
                return Success;
            }
            catch
            {
                return false;
            }
        }

        static public void SetAnyDirSec(string filePath)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            DirectorySecurity security = info.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-1-0"), FileSystemRights.Modify, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-1-0"), FileSystemRights.Modify, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            info.SetAccessControl(security);
        }

        public static bool Exec(string cmd, string args, bool hidden = true)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                if (hidden)
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                }
                process.StartInfo.FileName = cmd;
                process.StartInfo.Arguments = args;
                process.StartInfo.Verb = "runas"; // run as admin
                process.Start();
                process.WaitForExit();
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
