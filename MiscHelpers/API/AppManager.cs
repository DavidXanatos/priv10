﻿using Microsoft.Win32;
using MiscHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
//using Windows.ApplicationModel;
//using Windows.Management.Deployment;

namespace MiscHelpers
{

    public class AppManager
    {

        public AppManager()
        {

        }

        [DllImport("kernelbase", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerLookupMoniker(IntPtr Sid, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string packageFamilyName);

        [DllImport("kernelbase", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerDeriveSidFromMoniker([In, MarshalAs(UnmanagedType.LPWStr)] string packageFamilyName, ref IntPtr pSID);

        [DllImport("advapi32", CharSet = CharSet.Unicode)]
        public static extern bool ConvertStringSidToSid([In, MarshalAs(UnmanagedType.LPWStr)] string pStringSid, ref IntPtr pSID);

        [DllImport("advapi32", CharSet = CharSet.Unicode)]
        public static extern bool ConvertSidToStringSid(IntPtr pSID, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string pStringSid);

        static public string SidToAppPackage(string sid)
        {
            string packageID = "";

            IntPtr pSid = new IntPtr();
            ConvertStringSidToSid(sid, ref pSid);

            int ret = AppContainerLookupMoniker(pSid, ref packageID);

            Marshal.FreeHGlobal(pSid);

            if (ret != ERROR_SUCCESS)
            {
                return sid;

                /*var subKey = Registry.ClassesRoot.OpenSubKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Mappings\" + sid, false);
                if (subKey != null)
                    packageID = subKey.GetValue("Moniker").ToString();*/
            }

            return packageID;
        }

        static public string AppPackageToSid(string packageID)
        {
            string strSID = "";

            IntPtr pSid = new IntPtr();

            int ret = AppContainerDeriveSidFromMoniker(packageID, ref pSid);

            ConvertSidToStringSid(pSid, ref strSID);

            Marshal.FreeHGlobal(pSid);

            return strSID;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Int32 GetApplicationUserModelId(IntPtr hProcess, ref UInt32 AppModelIDLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);

        const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
        const int ERROR_SUCCESS = 0x0;

        public string GetAppPackageByPID(int PID)
        {
            // WARNING: this is not consistent with the windows firewall, we need to go by the proces token

            //var process = System.Diagnostics.Process.GetProcessById(PID); // throws error if pid is not found
            var processHandle = ProcFunc.OpenProcess(0x1000/*PROCESS_QUERY_LIMITED_INFORMATION*/, false, PID);
            if (processHandle == IntPtr.Zero)
                return null;

            uint cchLen = 256;
            StringBuilder sbName = new StringBuilder((int)cchLen);
            Int32 lResult = GetApplicationUserModelId(processHandle, ref cchLen, sbName);
            if (ERROR_INSUFFICIENT_BUFFER == lResult)
            {
                sbName = new StringBuilder((int)cchLen);
                lResult = GetApplicationUserModelId(processHandle, ref cchLen, sbName);
            }
            ProcFunc.CloseHandle(processHandle);

            if (lResult != ERROR_SUCCESS)
                return null;

            string sResult = sbName.ToString();
            int pos = sResult.LastIndexOf("!"); // bla!App
            if (pos != -1)
                sResult = sResult.Substring(0, pos);

            return sResult;
        }

        private Windows.Management.Deployment.PackageManager packageManager = new Windows.Management.Deployment.PackageManager();

        public UwpFunc.AppInfo GetAppInfoByID(string appPackageID)
        {
            UwpFunc.AppInfo info = null;

            IEnumerable<Windows.ApplicationModel.Package> packages;

            try
            {
                packages = packageManager.FindPackages(appPackageID);
            }
            catch
            {
                return null;
            }

            foreach (var package in packages)
            {
                info = GetInfo(package);
                if (info != null)
                    break;
            }

            return info;
        }

        private UwpFunc.AppInfo GetInfo(Windows.ApplicationModel.Package package)
        {
            string path;
            string manifest;
            try
            {
                path = package.InstalledLocation.Path; // that call takes a long time

                manifest = Path.Combine(path, !package.IsBundle ? @"AppxManifest.xml" : @"AppxMetadata\AppxBundleManifest.xml");
                if (!File.Exists(manifest))
                    return null;
            }
            catch
            {
                return null;
            }

            XElement xelement;
            string manifestXML;
            try
            {
                manifestXML = File.ReadAllText(manifest);

                int startIndex = manifestXML.IndexOf("<Properties>", StringComparison.Ordinal);
                int num = manifestXML.IndexOf("</Properties>", StringComparison.Ordinal);
                xelement = XElement.Parse(manifestXML.Substring(startIndex, num - startIndex + 13).Replace("uap:", string.Empty));
            }
            catch (Exception err)
            {
                //AppLog.Exception(err);
                return null;
            }

            string displayName = null;
            string logoPath = null;
            try
            {
                displayName = xelement.Element((XName)"DisplayName")?.Value;
                logoPath = xelement.Element((XName)"Logo")?.Value;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }

            if (displayName == null)
                return null;

            try
            {
                Uri result;
                if (Uri.TryCreate(displayName, UriKind.Absolute, out result))
                {
                    string pathToPri = Path.Combine(path, "resources.pri");
                    string resourceKey1 = "ms-resource://" + package.Id.Name + "/resources/" + ((IEnumerable<string>)result.Segments).Last<string>();
                    string stringFromPriFile = MiscFunc.GetResourceStr(pathToPri, resourceKey1);
                    if (!string.IsNullOrEmpty(stringFromPriFile.Trim()))
                        displayName = stringFromPriFile;
                    else
                    {
                        string str = string.Concat(((IEnumerable<string>)result.Segments).Skip<string>(1));
                        string resourceKey2 = "ms-resource://" + package.Id.Name + "/" + str;
                        stringFromPriFile = MiscFunc.GetResourceStr(pathToPri, resourceKey2);
                        if (!string.IsNullOrEmpty(stringFromPriFile.Trim()))
                            displayName = stringFromPriFile;
                    }
                }

                if (logoPath != null)
                {
                    List<string> extensions = new List<string> { "", "scale-100.png", "scale-125.png", "scale-150.png", "scale-200.png", "scale-400.png" };
                    List<string> sub_dirs = new List<string> { "", "en-us" };

                    string foundPath = null;
                    foreach (string extension in extensions)
                    {
                        foreach (string sub_dir in sub_dirs)
                        {
                            string testPath = path;
                            if(sub_dir.Length > 0)
                                testPath = Path.Combine(testPath, sub_dir);

                            string testName = logoPath;
                            if(extension.Length > 0)
                                testName = Path.ChangeExtension(testName, extension);

                            testPath = Path.Combine(testPath, testName);
                            if (File.Exists(testPath))
                            {
                                foundPath = testPath;
                                goto done;
                            }
                        }
                    }

                done:
                    logoPath = foundPath;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }

            return new UwpFunc.AppInfo() { Name = displayName, Logo = logoPath, ID = package.Id.FamilyName, Path = path};
        }

        /*
        private Dictionary<string, UwpFunc.AppInfo> AppInfosBySid = new Dictionary<string, UwpFunc.AppInfo>();
        private ReaderWriterLockSlim AppInfosBySidLock = new ReaderWriterLockSlim();

        public UwpFunc.AppInfo GetAppInfoBySid(string sid)
        {
            UwpFunc.AppInfo info = null;
            AppInfosBySidLock.EnterReadLock();
            AppInfosBySid.TryGetValue(sid, out info);
            AppInfosBySidLock.ExitReadLock();
            if (info != null)
                return info;

            string appPackageID = SidToAppPackage(sid);
            if (appPackageID == null || appPackageID.Length == 0)
                return null;

            IEnumerable<Windows.ApplicationModel.Package> packages;

            try
            {
                packages = packageManager.FindPackages(appPackageID);
            }
            catch
            {
                return null;
            }

            foreach (var package in packages)
            {
                info = GetInfo(package);
                if (info != null)
                {
                    info.SID = sid;
                    AppInfosBySidLock.EnterWriteLock();
                    if (!AppInfosBySid.ContainsKey(sid))
                        AppInfosBySid.Add(sid, info);
                    AppInfosBySidLock.ExitWriteLock();
                    break;
                }
            }

            return info;
        }

        bool FullListFetched = false;

        public void UpdateAppCache()
        {
            Dictionary<string, UwpFunc.AppInfo> AppInfos = new Dictionary<string, UwpFunc.AppInfo>();

            IEnumerable<Windows.ApplicationModel.Package> packages = (IEnumerable<Windows.ApplicationModel.Package>)packageManager.FindPackages();
            foreach (var package in packages)
            {
                string appSID = AppPackageToSid(package.Id.FamilyName).ToLower();

                UwpFunc.AppInfo info = GetInfo(package, appSID);
                if (info != null)
                {
                    if (!AppInfos.ContainsKey(appSID))
                        AppInfos.Add(appSID, info);
                    // UwpFunc.AppInfo old_info;
                    //if (AppInfos.TryGetValue(appSID, out old_info))
                    //    AppLog.Debug("Warning an app with the SID: {0} is already listed", appSID);
                }
            }

            AppInfosBySidLock.EnterWriteLock();
            AppInfosBySid = AppInfos;
            AppInfosBySidLock.ExitWriteLock();
            FullListFetched = true;
        }

        public List<UwpFunc.AppInfo> GetAllApps(bool bReload = false)
        {
            if (!FullListFetched || bReload)
                UpdateAppCache();

            List<UwpFunc.AppInfo> Apps = new List<UwpFunc.AppInfo>();
            AppInfosBySidLock.EnterReadLock();
            foreach (UwpFunc.AppInfo info in AppInfosBySid.Values)
                Apps.Add(info);
            AppInfosBySidLock.ExitReadLock();
            return Apps;
        }
        */

        //////////////////////////////////////////////////////////////////////////////////////////////
        // App resource handling


        public string GetAppResourceStr(string resourcePath)
        {
            // Note: PackageManager requirers admin privilegs

            try
            {
                var AppResource = TextHelpers.Split2(resourcePath.Substring(2, resourcePath.Length - 3), "?");
                var package = packageManager.FindPackage(AppResource.Item1);
                if (package != null)
                {
                    string pathToPri = Path.Combine(package.InstalledLocation.Path, "resources.pri");
                    return MiscFunc.GetResourceStr(pathToPri, AppResource.Item2);
                }
            }
            catch{ }

            return resourcePath;
        }
    }
}