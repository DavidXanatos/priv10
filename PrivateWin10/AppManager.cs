using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
//using Windows.Management.Deployment;


public class AppManager
{
    Windows.Management.Deployment.PackageManager packageManager;

    SortedDictionary<string, string> Apps = new SortedDictionary<string, string>();
    SortedDictionary<string, AppInfo> AppInfos = new SortedDictionary<string, AppInfo>();

    public AppManager()
    {
        packageManager = new Windows.Management.Deployment.PackageManager();

        LoadApps();
    }

    public void LoadApps()
    {
        Apps.Clear();
        AppInfos.Clear();

        //packageManager.RemovePackageAsync(package.Id.FullName);

        IEnumerable<Windows.ApplicationModel.Package> packages = (IEnumerable<Windows.ApplicationModel.Package>)packageManager.FindPackages();
        // Todo: is there a better way to get this ?
        foreach (var package in packages)
        {
            string name;
            //string fullname;
            string path;
            string publisher;
            bool isFramework;
            try
            {
                name = package.Id.Name;
                //fullname = package.Id.FullName;
                publisher = package.Id.PublisherId;
                path = package.InstalledLocation.Path;
                isFramework = package.IsFramework;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                continue;
            }

            string appPackageID = name.ToLower() + "_" + publisher;
            string appSID = PackageIDToSid(appPackageID).ToLower();

            if (Apps.ContainsKey(package.InstalledLocation.Path.ToLower()))
                AppLog.Line("Warning an app with the path: {0} is already listed", package.InstalledLocation.Path.ToLower());
            else
                Apps.Add(package.InstalledLocation.Path.ToLower(), appSID);

            AppInfo? info = GetInfo(path, name, appPackageID, appSID);
            if (info != null)
            {
                AppInfo old_info;
                if (AppInfos.TryGetValue(appSID, out old_info))
                    continue;
                if (AppInfos.ContainsKey(appSID))
                    AppLog.Line("Warning an app with the SID: {0} is already listed", appSID);
                else
                    AppInfos.Add(appSID, info.Value);
            }
        }
    }

    public List<AppInfo> GetAllApps() { return AppInfos.Values.ToList(); }

    public string GetAppPackage(string path)
    {
        string SID;
        // Todo: better path checking sub dirs etc...
        if (!Apps.TryGetValue(Path.GetDirectoryName(path).ToLower(), out SID))
            return null;
        return SID;
    }

    public string GetAppName(string SID)
    {
        AppInfo info;
        if (!AppInfos.TryGetValue(SID.ToLower(), out info))
            return SidToPackageID(SID.ToLower());
        return info.Name;
    }


    internal static class AppModel
    {
        // undocumented API: https://stackoverflow.com/questions/47521346/api-to-get-appcontainername-from-appcontainersid
        [DllImport("api-ms-win-appmodel-identity-l1-2-0", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerLookupMoniker(IntPtr Sid, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string packageFamilyName);

        [DllImport("api-ms-win-appmodel-identity-l1-2-0", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerDeriveSidFromMoniker([In, MarshalAs(UnmanagedType.LPWStr)] string packageFamilyName, ref IntPtr pSID);
    }

    internal static class AppModel8
    {
        // todo this is for windows 8.1 but does it work on 8 ?
        [DllImport("api-ms-win-appmodel-identity-l1-1-0", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerLookupMoniker(IntPtr Sid, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string packageFamilyName);

        [DllImport("api-ms-win-appmodel-identity-l1-1-0", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int AppContainerDeriveSidFromMoniker([In, MarshalAs(UnmanagedType.LPWStr)] string packageFamilyName, ref IntPtr pSID);
    }

    [DllImport("advapi32", CharSet = CharSet.Unicode)]
    static extern bool ConvertStringSidToSid([In, MarshalAs(UnmanagedType.LPWStr)] string pStringSid, ref IntPtr pSID);

    [DllImport("advapi32", CharSet = CharSet.Unicode)]
    static extern bool ConvertSidToStringSid(IntPtr pSID, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string pStringSid);

    public static string SidToPackageID(string sid)
    {
        string packageID = "";
        if (!UwpFunc.IsWindows7OrLower)
        {
            IntPtr pSid = new IntPtr();
            ConvertStringSidToSid(sid, ref pSid);

            //string test = "";
            //ConvertSidToStringSid(pSid, ref test);

            int ret = UwpFunc.IsWindows8 ? AppModel8.AppContainerLookupMoniker(pSid, ref packageID) : AppModel.AppContainerLookupMoniker(pSid, ref packageID);

            Marshal.FreeHGlobal(pSid);
        }
        return packageID;
    }

    public static string PackageIDToSid(string packageID)
    {
        string strSID = "";
        if (!UwpFunc.IsWindows7OrLower)
        {
            IntPtr pSid = new IntPtr();

            int ret = UwpFunc.IsWindows8 ? AppModel8.AppContainerDeriveSidFromMoniker(packageID, ref pSid) : AppModel.AppContainerDeriveSidFromMoniker(packageID, ref pSid);

            ConvertSidToStringSid(pSid, ref strSID);

            Marshal.FreeHGlobal(pSid);
        }
        return strSID;
    }

    /*void AppXtest(PSID Sid)
    {
        LONG (WINAPI* AppContainerLookupMoniker)(PSID Sid, PWSTR* packageFamilyName);
        BOOLEAN (WINAPI* AppContainerFreeMemory)(void* ptr);

        if (HMODULE hmod = LoadLibraryW(L"api-ms-win-appmodel-identity-l1-2-0"))
        {
            if ((*(void**)&AppContainerLookupMoniker = GetProcAddress(hmod, "AppContainerLookupMoniker")) &&
                (*(void**)&AppContainerFreeMemory = GetProcAddress(hmod, "AppContainerFreeMemory")))
            {

                PWSTR packageFamilyName;
                LONG err = AppContainerLookupMoniker(Sid, &packageFamilyName);

                if (err == NOERROR)
                {
                    DbgPrint("%S\n", packageFamilyName);

                    UINT32 count = 0, bufferLength = 0;

                    if (ERROR_INSUFFICIENT_BUFFER == GetPackagesByPackageFamily(packageFamilyName, &count, 0, &bufferLength, 0))
                    {
                        PWSTR *packageFullNames = (PWSTR*)alloca(count * sizeof(PWSTR) + bufferLength*sizeof(WCHAR));
                        PWSTR buffer = (PWSTR)(packageFullNames+ count);

                        if (NOERROR == GetPackagesByPackageFamily(packageFamilyName, &count, packageFullNames, &bufferLength, buffer))
                        {
                            if (count)
                            {
                                do 
                                {
                                    PCWSTR packageFullName = *packageFullNames++;
                                    DbgPrint("%S\n", packageFullName);

                                    WCHAR path[MAX_PATH];
                                    UINT32 len = RTL_NUMBER_OF(path);

                                    if (NOERROR == GetStagedPackagePathByFullName(packageFullName, &len, path))
                                    {
                                        DbgPrint("%S\n", path);
                                    }
                                } while (--count);
                            }
                        }
                    }

                    AppContainerFreeMemory(packageFamilyName);
                }
            }
        }
    }*/

    [Serializable()]
    public struct AppInfo
    {
        public string Name;
        public string Logo;
        public string ID;
        public string SID;
    }

    public AppInfo? GetInfo(string path, string name, string id, string sid)
    {
        string manifest = Path.Combine(path, "AppxManifest.xml");

        if (!File.Exists(manifest))
            return null;

        XElement xelement;
        try
        {
            string manifestXML = File.ReadAllText(manifest);

            int startIndex = manifestXML.IndexOf("<Properties>", StringComparison.Ordinal);
            int num = manifestXML.IndexOf("</Properties>", StringComparison.Ordinal);
            xelement = XElement.Parse(manifestXML.Substring(startIndex, num - startIndex + 13).Replace("uap:", string.Empty));
        }
        catch (Exception err)
        {
            AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            return null;
        }

        string displayName = name;
        string logoPath = null;
        try
        {
            displayName = xelement.Element((XName)"DisplayName")?.Value;
            logoPath = xelement.Element((XName)"Logo")?.Value;
        }
        catch (Exception err)
        {
            AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
        }

        try
        {
            Uri result;
            if (Uri.TryCreate(displayName, UriKind.Absolute, out result))
            {
                string pathToPri = Path.Combine(path, "resources.pri");
                string resourceKey1 = "ms-resource://" + name + "/resources/" + ((IEnumerable<string>)result.Segments).Last<string>();
                string stringFromPriFile = MiscFunc.GetResourceStr(pathToPri, resourceKey1);
                if (!string.IsNullOrEmpty(stringFromPriFile.Trim()))
                    displayName = stringFromPriFile;
                else
                {
                    string str = string.Concat(((IEnumerable<string>)result.Segments).Skip<string>(1));
                    string resourceKey2 = "ms-resource://" + name + "/" + str;
                    stringFromPriFile = MiscFunc.GetResourceStr(pathToPri, resourceKey2);
                    if (!string.IsNullOrEmpty(stringFromPriFile.Trim()))
                        displayName = stringFromPriFile;
                }
            }

            if (logoPath != null)
            {
                string path1 = Path.Combine(path, logoPath);
                if (File.Exists(path1))
                    logoPath = path1;
                else
                {
                    string path2 = Path.Combine(path, Path.ChangeExtension(path1, "scale-100.png"));
                    if (File.Exists(path2))
                        logoPath = path2;
                    else
                    {
                        string path3 = Path.Combine(Path.Combine(path, "en-us"), logoPath);
                        string path4 = Path.Combine(path, Path.ChangeExtension(path3, "scale-100.png"));
                        if (File.Exists(path4))
                            logoPath = path4;
                        else
                            logoPath = null;
                    }
                }
            }
        }
        catch (Exception err)
        {
            AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
        }

        return new AppInfo() { Name = displayName, Logo = logoPath, ID = id , SID = sid};
    }
}
