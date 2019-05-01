using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    [Serializable()]
    public class WinVer
    {
        public const float ver10 = 10.0f;// Win 10 & Server 2016, 2019
        public const float ver81 = 6.3f; // Win 8.1 & Server 2012 R2 
        public const float ver8  = 6.2f; // Win 8 & Server 2012
        public const float ver7  = 6.1f; // Win 7 & Server 2008 R2
        public const float ver6  = 6.0f; // Vista & Server 2008
        public const float ver52 = 5.2f; // Win XP 64-Bit & Server 2008 & Server 2008 R2
        public const float verXP = 5.1f; // Win XP
        public const float ver2k = 5.0f; // Win 2k

        public const int ten1507 = 10240;
        public const int ten1511 = 10586;
        public const int ten1607 = 14393;
        public const int ten1703 = 15063;
        public const int ten1709 = 16299;
        public const int ten1803 = 17134;
        public const int ten1809 = 17763;
        //...

        static public WinVer Win2k          { get { return new WinVer() { minVer = ver2k }; } }
        static public WinVer WinXP          { get { return new WinVer() { minVer = verXP }; } }
        static public WinVer WinXPonly      { get { return new WinVer() { minVer = verXP, maxVer = verXP }; } }
        static public WinVer WinXPto7       { get { return new WinVer() { minVer = verXP, maxVer = ver7 }; } }
        static public WinVer Win6           { get { return new WinVer() { minVer = ver6 }; } }
        static public WinVer Win6to7        { get { return new WinVer() { minVer = ver6, maxVer = ver7 }; } }
        static public WinVer Win7           { get { return new WinVer() { minVer = ver7 }; } }
        static public WinVer Win7to81       { get { return new WinVer() { minVer = ver7, maxVer = ver81 }; } }
        static public WinVer Win8           { get { return new WinVer() { minVer = ver8 }; } }
        static public WinVer Win81          { get { return new WinVer() { minVer = ver81 }; } }
        static public WinVer Win81only      { get { return new WinVer() { minVer = ver81, maxVer = ver81 }; } }
        static public WinVer Win10          { get { return new WinVer() { minVer = ver10 }; } }
        static public WinVer Win10EE        { get { return new WinVer() { minVer = ver10, win10Ed = Edition10.Enterprise }; } }
        static public WinVer Win1703        { get { return new WinVer() { minVer = ver10, build10 = ten1703 }; } }
        static public WinVer Win10to1709    { get { return new WinVer() { minVer = ver10, build10max = ten1709 }; } }
        static public WinVer Win1709        { get { return new WinVer() { minVer = ver10, build10 = ten1709 }; } }
        static public WinVer Win1803        { get { return new WinVer() { minVer = ver10, build10 = ten1803 }; } }
        static public WinVer Win1809        { get { return new WinVer() { minVer = ver10, build10 = ten1809 }; } }


        public enum Edition10
        {
            Any = 0,
            Home = 1,
            Pro = 2,
            Enterprise = 3 // or education or server
        }

        public float minVer = 0.0f;
        public float maxVer = 0.0f;

        // Windows 10 home and or pro ignore some policys
        public Edition10 win10Ed = Edition10.Any;

        public int build10 = 0;
        public int build10max = 0;

        public bool TestHost()
        {
            float curVer = GetWinVersion();
            if (minVer > curVer)
                return false;
            if (maxVer != 0.0f && maxVer < curVer)
                return false;
            if (curVer >= ver10)
            {
                int curBuild = GetWin10build();
                if (build10 != 0 && build10 > curBuild)
                    return false;
                if (build10max != 0 && build10max < curBuild)
                    return false;
                if (win10Ed > GetWin10Edition())
                    return false;
            }
            return true;
        }

        static private float mWinVersion = 0.0f;
        static public float GetWinVersion()
        {
            if (mWinVersion == 0.0f)
            {
                try
                {
                    var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    //string Majorversion = subKey.GetValue("CurrentMajorVersionNumber", "0").ToString(); // this is 10 on 10 but not present on earlier editions
                    string version = subKey.GetValue("CurrentVersion", "0").ToString();
                    float version_num = float.Parse(version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

                    if (version_num >= 6.3) // WTF why is this not 1 on win 10
                    {
                        //!name.Contains("8.1") && !name.Contains("2012 R2");
                        if (GetWin10build() >= 10000) // 1507 RTM release
                            version_num = 10.0f;
                    }

                    mWinVersion = version_num;
                }
                catch
                {
                }
            }
            return mWinVersion;
        }

        static private int mWinBuild = 0;
        static public int GetWin10build() // works also for win 8
        {
            if (mWinBuild == 0)
            {
                try
                {
                    var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    mWinBuild = int.Parse(subKey.GetValue("CurrentBuildNumber", "0").ToString());
                }
                catch
                {
                }
            }
            return mWinBuild;
        }

        static private Edition10 mWinEd = Edition10.Any;
        static public Edition10 GetWin10Edition()
        {
            if (mWinEd == Edition10.Any)
            {
                try
                {
                    var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    //string edition = subKey.GetValue("EditionID", "").ToString();
                    string name = subKey.GetValue("ProductName", "").ToString();
                    string type = subKey.GetValue("InstallationType", "").ToString();

                    if (GetWinVersion() < 10.0f || type.Equals("Server", StringComparison.CurrentCultureIgnoreCase) || name.Contains("Education") || name.Contains("Enterprise"))
                        mWinEd = Edition10.Enterprise;
                    else if (type.Equals("Client", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (name.Contains("Pro"))
                            mWinEd = Edition10.Pro;
                        else if (name.Contains("Home"))
                            mWinEd = Edition10.Home;
                    }
                }
                catch
                {
                }
            }
            return mWinEd;
        }
    }
}
