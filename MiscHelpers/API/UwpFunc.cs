using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MiscHelpers
{
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

        /*
        +------------------------------------------------------------------------------+
        |                    |   PlatformID    |   Major version   |   Minor version   |
        +------------------------------------------------------------------------------+
        | Windows 95         |  Win32Windows   |         4         |          0        |
        | Windows 98         |  Win32Windows   |         4         |         10        |
        | Windows Me         |  Win32Windows   |         4         |         90        |
        | Windows NT 4.0     |  Win32NT        |         4         |          0        |
        | Windows 2000       |  Win32NT        |         5         |          0        |
        | Windows XP         |  Win32NT        |         5         |          1        |
        | Windows 2003       |  Win32NT        |         5         |          2        |
        | Windows Vista      |  Win32NT        |         6         |          0        |
        | Windows 2008       |  Win32NT        |         6         |          0        |
        | Windows 7          |  Win32NT        |         6         |          1        |
        | Windows 2008 R2    |  Win32NT        |         6         |          1        |
        | Windows 8          |  Win32NT        |         6         |          2        |
        | Windows 8.1        |  Win32NT        |         6         |          3        |
        +------------------------------------------------------------------------------+
        | Windows 10         |  Win32NT        |        10         |          0        |
        +------------------------------------------------------------------------------+
        */

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

        static public bool IsWindows8
        {
            get
            {
                int versionMajor = Environment.OSVersion.Version.Major;
                int versionMinor = Environment.OSVersion.Version.Minor;
                double version = versionMajor + (double)versionMinor / 10;
                return version == 6.2 || version == 6.3;
            }
        }

        [Serializable()]
        [DataContract(Name = "AppInfo", Namespace = "http://schemas.datacontract.org/")]
        public class AppInfo
        {
            [DataMember()]
            public string Name;
            [DataMember()]
            public string Logo;
            [DataMember()]
            public string ID;
            [DataMember()]
            public string SID;
        }

        public static void AddBinding(System.Windows.Controls.Control ctrl, KeyGesture keyGesture, ExecutedRoutedEventHandler executed)
        {
            RoutedCommand cmd = new RoutedCommand();
            cmd.InputGestures.Add(keyGesture);
            ctrl.CommandBindings.Add(new CommandBinding(cmd, executed));
        }
    }

}