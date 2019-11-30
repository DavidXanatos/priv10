using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public static class DnsApi
    {
        public const uint ERROR_SUCCESS = 0;
        public const uint DNS_ERROR_RECORD_DOES_NOT_EXIST = 9701;

        [Flags]
        public enum DnsQueryType : uint
        {
            DNS_QUERY_STANDARD = 0x00000000,
            DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 0x00000001,
            DNS_QUERY_USE_TCP_ONLY = 0x00000002,
            DNS_QUERY_NO_RECURSION = 0x00000004,
            DNS_QUERY_BYPASS_CACHE = 0x00000008,
            DNS_QUERY_NO_WIRE_QUERY = 0x00000010,
            DNS_QUERY_NO_LOCAL_NAME = 0x00000020,
            DNS_QUERY_NO_HOSTS_FILE = 0x00000040,
            DNS_QUERY_NO_NETBT = 0x00000080,
            DNS_QUERY_WIRE_ONLY = 0x00000100,
            DNS_QUERY_RETURN_MESSAGE = 0x00000200,
            DNS_QUERY_MULTICAST_ONLY = 0x00000400,
            DNS_QUERY_NO_MULTICAST = 0x00000800,
            DNS_QUERY_TREAT_AS_FQDN = 0x00001000,
            DNS_QUERY_ADDRCONFIG = 0x00002000,
            DNS_QUERY_DUAL_ADDR = 0x00004000,
            DNS_QUERY_UN_DOCUMENTED = 0x00008000, // undicumented flag nececery for complete dns cache retrival
            DNS_QUERY_DONT_RESET_TTL_VALUES = 0x00100000,
            DNS_QUERY_DISABLE_IDN_ENCODING = 0x00200000,
            DNS_QUERY_APPEND_MULTILABEL = 0x00800000,
            DNS_QUERY_DNSSEC_OK = 0x01000000,
            DNS_QUERY_DNSSEC_CHECKING_DISABLED = 0x02000000,
            DNS_QUERY_RESERVED = 0xf0000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsRecord
        {
            public IntPtr Next;
            [MarshalAs(UnmanagedType.LPWStr)] public string Name;
            [MarshalAs(UnmanagedType.U2)] public DnsRecordType Type;
            [MarshalAs(UnmanagedType.U2)] public ushort DataLength;
            [MarshalAs(UnmanagedType.U4)] public uint Flags;
            [MarshalAs(UnmanagedType.U4)] public uint Ttl;
            [MarshalAs(UnmanagedType.U4)] public uint Reserved;
            public IntPtr Data;
        }

        [Flags]
        public enum DnsRecordType : ushort
        {
            A       = 0x0001,
            CNAME   = 0x0005,
            AAAA    = 0x001c,
            PTR     = 0x000c,
            SRV     = 0x0021,
            MX      = 0x000f,
            //DNAME = 0x0027,
            ANY     = 0x00FF
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsARecord
        {
            [MarshalAs(UnmanagedType.U4)] public UInt32 IpAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsAAAARecord
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] IpAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsPTRRecord
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string NameHost;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsSRVRecord
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string NameTarget;
            [MarshalAs(UnmanagedType.U4)] public uint Priority;
            [MarshalAs(UnmanagedType.U4)] public uint Weight;
            [MarshalAs(UnmanagedType.U4)] public uint Port;
            [MarshalAs(UnmanagedType.U4)] public uint Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DnsMXRecord
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string NameExchange;
            [MarshalAs(UnmanagedType.U4)] public uint Preference;
            [MarshalAs(UnmanagedType.U4)] public uint Pad;
        }



        [StructLayout(LayoutKind.Sequential)]
        public struct DnsCacheEntry
        {
            [MarshalAs(UnmanagedType.SysUInt)] public IntPtr Next;
            [MarshalAs(UnmanagedType.LPWStr)] public string Name;
            [MarshalAs(UnmanagedType.U2)] public DnsRecordType Type;
            [MarshalAs(UnmanagedType.U2)] public ushort DataLength;
            [MarshalAs(UnmanagedType.U4)] public uint Flags;
        }

        public enum DnsFreeType : uint
        {
            DnsFreeFlat = 0,
            DnsFreeRecordList,
            DnsFreeParsedMessageFields
        }

        [DllImport("dnsapi.dll", EntryPoint = "DnsQuery_W", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint DnsQuery(string name, DnsRecordType type, DnsQueryType opts, IntPtr Servers, ref IntPtr queryResults, IntPtr reserved);

        [DllImport("dnsapi.dll", EntryPoint = "DnsRecordListFree", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern void DnsRecordListFree(IntPtr records, DnsFreeType freeType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool DnsGetCacheDataTable(out IntPtr entries);

        [DllImport("dnsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool DnsFlushResolverCache();


        // kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);
    }
}
