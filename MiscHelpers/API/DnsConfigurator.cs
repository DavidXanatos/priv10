using Microsoft.Win32;
using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiscHelpers
{
    public static class DnsConfigurator
    {
        [DllImport("dhcpcsvc.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint DhcpNotifyConfigChange(string ServerName, string AdapterName, bool NewIpAddress, uint IpIndex, uint IpAddress, uint SubnetMask, int DhcpAction);

        public static void ApplyChanges(string AdapterName)
        {
            // this is how explorer suposedly does it
            DhcpNotifyConfigChange(null, AdapterName, false, 0, 0, 0, 0);
            DnsApi.DnsFlushResolverCache();
        }

        public const string NameServerKey = @"NameServer";
        public const string NetworkInterfacesKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
        public const string NetworkInterfacesV6Key = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces";
        public const string LocalHost = "127.0.0.1";
        public const string LocalHostV6 = "::1";

        private static bool IsLocalDNS(string regKey, string value)
        {
            var itfKey = Registry.LocalMachine.OpenSubKey(regKey, false);
            if (itfKey != null)
            {
                var interfaces = itfKey.GetSubKeyNames();
                itfKey.Close();
                foreach (var itf in interfaces)
                {
                    using (var subKey = Registry.LocalMachine.OpenSubKey(regKey + @"\" + itf, true))
                    {
                        if (subKey.GetValue(NameServerKey).ToString() != value)
                            return false;
                    }
                }
            }
            return true;
        }

        public static bool IsLocalDNS()
        {
            return IsLocalDNS(NetworkInterfacesKey, LocalHost) && IsLocalDNS(NetworkInterfacesV6Key, LocalHostV6);
        }

        private static bool SetLocalDNS(string regKey, string value)
        {
            var itfKey = Registry.LocalMachine.OpenSubKey(regKey, true);
            if (itfKey == null)
                return false;
            foreach (var itf in itfKey.GetSubKeyNames())
            {
                var subKey = Registry.LocalMachine.OpenSubKey(regKey + @"\" + itf, true);
                if (subKey.GetValueNames().Contains(NameServerKey))
                {
                    if (subKey.GetValue(NameServerKey).ToString() != value)
                    {
                        if (!subKey.GetValueNames().Contains(NameServerKey + "_old"))
                        {
                            var old = subKey.GetValue(NameServerKey);
                            if (old != null)
                                subKey.SetValue(NameServerKey + "_old", old);
                        }

                        subKey.SetValue(NameServerKey, value);
                        ApplyChanges(itf);
                    }
                }
                subKey.Close();
            }
            itfKey.Close();
            return true;
        }

        public static bool SetLocalDNS()
        {
            return SetLocalDNS(NetworkInterfacesKey, LocalHost) && SetLocalDNS(NetworkInterfacesV6Key, LocalHostV6);
        }

        private static void RestoreDNS(string regKey)
        {
            var itfKey = Registry.LocalMachine.OpenSubKey(regKey, true);
            if (itfKey == null)
                return;
            foreach (var itf in itfKey.GetSubKeyNames())
            {
                var subKey = Registry.LocalMachine.OpenSubKey(regKey + @"\" + itf, true);
                var old = subKey.GetValue(NameServerKey + "_old");
                if (old != null)
                {
                    subKey.SetValue(NameServerKey, old);
                    ApplyChanges(itf);

                    subKey.DeleteValue(NameServerKey + "_old");
                }
                subKey.Close();
            }
            itfKey.Close();
        }

        public static void RestoreDNS()
        {
            RestoreDNS(NetworkInterfacesKey);
            RestoreDNS(NetworkInterfacesV6Key);
        }

        private static bool IsAnyLocalDNS(string regKey)
        {
            var itfKey = Registry.LocalMachine.OpenSubKey(regKey, false);
            if (itfKey != null)
            {
                var interfaces = itfKey.GetSubKeyNames();
                itfKey.Close();
                foreach (var itf in interfaces)
                {
                    using (var subKey = Registry.LocalMachine.OpenSubKey(regKey + @"\" + itf, false))
                    {
                        if (subKey.GetValueNames().Contains(NameServerKey + "_old"))
                            return true;
                    }
                }
                itfKey.Close();
            }
            return false;
        }

        public static bool IsAnyLocalDNS()
        {
            return IsAnyLocalDNS(NetworkInterfacesKey) || IsAnyLocalDNS(NetworkInterfacesV6Key);
        }
    }
}
