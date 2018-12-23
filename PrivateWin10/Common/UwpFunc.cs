using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


public class UwpFunc
{

    const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

    static public bool IsRunningAsUwp()
    {
        if (IsWindows7OrLower)
        {
            return false;
        }
        else
        {
            int length = 0;
            StringBuilder sb = new StringBuilder(0);
            int result = GetCurrentPackageFullName(ref length, sb);

            sb = new StringBuilder(length);
            result = GetCurrentPackageFullName(ref length, sb);

            return result != APPMODEL_ERROR_NO_PACKAGE;
        }
    }

    static public bool IsWindows7OrLower
    {
        get
        {
            int versionMajor = Environment.OSVersion.Version.Major;
            int versionMinor = Environment.OSVersion.Version.Minor;
            double version = versionMajor + (double)versionMinor / 10;
            return version <= 6.1;
        }
    }


}
