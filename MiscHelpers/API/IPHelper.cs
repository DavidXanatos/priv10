using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiscHelpers
{

    public class IPHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TCPIP_OWNER_MODULE_BASIC_INFO
        {
            public IntPtr pModuleName;
            public IntPtr pModulePath;
        }


        public class ModuleInfo
        {
            public string ModuleName;
            public string ModulePath;

            public ModuleInfo(TCPIP_OWNER_MODULE_BASIC_INFO inf)
            {
                ModuleName = inf.pModuleName != null ? Marshal.PtrToStringAuto(inf.pModuleName) : "";
                ModulePath = inf.pModulePath != null ? Marshal.PtrToStringAuto(inf.pModulePath) : "";
            }
        }

        public interface I_SOCKET_ROW
        {
            int ProcessId { get; }
            ModuleInfo Module { get; }

            UInt32 ProtocolType { get; }
            IPAddress RemoteAddress { get; }
            UInt16 RemotePort { get; }
            IPAddress LocalAddress { get; }
            UInt16 LocalPort { get; }
            MIB_TCP_STATE State { get; }
            DateTime CreationTime { get; }
        }



        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, IntPtr size);

        public const UInt32 NO_ERROR = 0;
        public const UInt32 ERROR_INSUFFICIENT_BUFFER = 122;
        public const UInt32 ERROR_NOT_FOUND = 1168;

        public enum AF_INET
        {
            IP4 = 2,
            IP6 = 23
        }

        public enum AF_PROT
        {
            TCP = 6,
            UDP = 17
        }

        //////////////////////////////////////////////////////
        // TCP
        //      

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetExtendedTcpTable(IntPtr pTcpTable, ref UInt32 dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool order, AF_INET ipVersion, TCP_TABLE_CLASS tblClass, UInt32 reserved);

        public enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        public enum TCPIP_OWNER_MODULE_INFO_CLASS
        {
            TCPIP_OWNER_MODULE_INFO_BASIC
        }

        public enum MIB_TCP_STATE
        {
            CLOSED = 1,
            LISTENING,
            SYN_SENT,
            SYN_RCVD,
            ESTABLISHED,
            FIN_WAIT1,
            FIN_WAIT2,
            CLOSE_WAIT,
            CLOSING,
            LAST_ACK,
            TIME_WAIT,
            DELETE_TCB,
            UNDEFINED = 65535
        }

        public enum TCP_ESTATS_TYPE
        {
            TcpConnectionEstatsSynOpts,
            TcpConnectionEstatsData,
            TcpConnectionEstatsSndCong,
            TcpConnectionEstatsPath,
            TcpConnectionEstatsSendBuff,
            TcpConnectionEstatsRec,
            TcpConnectionEstatsObsRec,
            TcpConnectionEstatsBandwidth,
            TcpConnectionEstatsFineRtt,
            TcpConnectionEstatsMaximum
        }


        //////////////////////////////////////////////////////
        // TCPv4
        // 

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetOwnerModuleFromTcpEntry(ref MIB_TCPROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref UInt32 pdwSize);

        /*[DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetPerTcpConnectionEStats(ref MIB_TCPROW_OWNER_MODULE Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, UInt32 RwVersion, UInt32 RwSize, IntPtr Ros, UInt32 RosVersion, UInt32 RosSize, IntPtr Rod, UInt32 RodVersion, UInt32 RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 SetPerTcpConnectionEStats(ref MIB_TCPROW_OWNER_MODULE Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, UInt32 RwVersion, UInt32 RwSize, UInt32 Offset);*/

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_MODULE : I_SOCKET_ROW
        {
            internal UInt32 dwState;
            internal UInt32 dwLocalAddr;
            internal UInt32 dwLocalPort;
            internal UInt32 dwRemoteAddr;
            internal UInt32 dwRemotePort;
            internal UInt32 dwOwningPid;
            internal Int64 liCreateTimestamp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal ulong[] OwningModuleInfo;

            public int ProcessId { get { return (int)dwOwningPid; } }
            public ModuleInfo Module
            {
                get
                {
                    ModuleInfo Info = null;

                    uint buffSize = 0;
                    GetOwnerModuleFromTcpEntry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                    IntPtr buffer = Marshal.AllocHGlobal((int)buffSize);

                    if (GetOwnerModuleFromTcpEntry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize) == NO_ERROR && buffSize != 0)
                        Info = new ModuleInfo((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));

                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);

                    return Info;
                }
            }

            public UInt32 ProtocolType { get { return (UInt32)AF_PROT.TCP | (UInt32)AF_INET.IP4 << 8; } }
            public IPAddress RemoteAddress { get { return new IPAddress((UInt32)dwRemoteAddr); } }
            public UInt16 RemotePort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwRemotePort); } }
            public IPAddress LocalAddress { get { return new IPAddress((UInt32)dwLocalAddr); } }
            public UInt16 LocalPort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwLocalPort); } }
            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)dwState; } }
            public DateTime CreationTime { get { return liCreateTimestamp == 0 ? DateTime.Now : DateTime.FromFileTime(liCreateTimestamp); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_MODULE
        {
            public UInt32 dwNumEntries;
            public MIB_TCPROW_OWNER_MODULE FirstEntry;
        }

        public static IntPtr GetTcpSockets(ref List<I_SOCKET_ROW> Sockets)
        {
            uint tcp4Size = 0;
            IntPtr tcp4Table = IntPtr.Zero;
            IPHelper.GetExtendedTcpTable(IntPtr.Zero, ref tcp4Size, false, IPHelper.AF_INET.IP4, IPHelper.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
            tcp4Table = Marshal.AllocHGlobal((int)tcp4Size);

            if (IPHelper.GetExtendedTcpTable(tcp4Table, ref tcp4Size, false, IPHelper.AF_INET.IP4, IPHelper.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0) == IPHelper.NO_ERROR)
            {
                IPHelper.MIB_TCPTABLE_OWNER_MODULE table = ((IPHelper.MIB_TCPTABLE_OWNER_MODULE)Marshal.PtrToStructure(tcp4Table, typeof(IPHelper.MIB_TCPTABLE_OWNER_MODULE)));
                IntPtr rowPtr = (IntPtr)((long)tcp4Table + (long)Marshal.OffsetOf(typeof(IPHelper.MIB_TCPTABLE_OWNER_MODULE), "FirstEntry"));

                for (uint i = 0; i < table.dwNumEntries; i++)
                {
                    IPHelper.MIB_TCPROW_OWNER_MODULE mibRow = (IPHelper.MIB_TCPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(IPHelper.MIB_TCPROW_OWNER_MODULE));
                    rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(mibRow));

                    Sockets.Add(mibRow);
                }
            }

            return tcp4Table;
        }

        //////////////////////////////////////////////////////
        // TCPv6
        // 

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetOwnerModuleFromTcp6Entry(ref MIB_TCP6ROW_OWNER_MODULE pTcpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref UInt32 pdwSize);

        /*[DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetPerTcp6ConnectionEStats(ref MIB_TCP6ROW_OWNER_MODULE Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, UInt32 RwVersion, UInt32 RwSize, IntPtr Ros, UInt32 RosVersion, UInt32 RosSize, IntPtr Rod, UInt32 RodVersion, UInt32 RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 SetPerTcp6ConnectionEStats(ref MIB_TCP6ROW_OWNER_MODULE Row, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, UInt32 RwVersion, UInt32 RwSize, UInt32 Offset);*/

        //[DllImport("ntdll.dll", SetLastError = true)]
        //public static extern void RtlIpv6AddressToString(byte[] Addr, out StringBuilder res);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIB_TCP6ROW_OWNER_MODULE : I_SOCKET_ROW
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] ucLocalAddress;
            internal UInt32 dwLocalScopeId;
            internal UInt32 dwLocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] ucRemoteAddress;
            internal UInt32 dwRemoteScopeId;
            internal UInt32 dwRemotePort;
            internal UInt32 dwState;
            internal UInt32 dwOwningPid;
            internal Int64 liCreateTimestamp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal ulong[] OwningModuleInfo;

            public int ProcessId { get { return (int)dwOwningPid; } }
            public ModuleInfo Module
            {
                get
                {
                    ModuleInfo Info = null;

                    uint buffSize = 0;
                    GetOwnerModuleFromTcp6Entry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                    IntPtr buffer = Marshal.AllocHGlobal((int)buffSize);

                    if (GetOwnerModuleFromTcp6Entry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize) == NO_ERROR && buffSize > 0)
                        Info = new ModuleInfo((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));

                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);

                    return Info;
                }
            }

            public UInt32 ProtocolType { get { return (UInt32)AF_PROT.TCP | (UInt32)AF_INET.IP6 << 8; } }
            public IPAddress RemoteAddress { get { return new IPAddress(ucLocalAddress); } }
            public UInt16 RemotePort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwRemotePort); } }
            public IPAddress LocalAddress { get { return new IPAddress(ucLocalAddress); } }
            public UInt16 LocalPort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwLocalPort); } }
            public MIB_TCP_STATE State { get { return (MIB_TCP_STATE)dwState; } }
            public DateTime CreationTime { get { return liCreateTimestamp == 0 ? DateTime.Now : DateTime.FromFileTime(liCreateTimestamp); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCP6TABLE_OWNER_MODULE
        {
            public UInt32 dwNumEntries;
            public MIB_TCP6ROW_OWNER_MODULE FirstEntry;
        }

        public static IntPtr GetTcp6Sockets(ref List<I_SOCKET_ROW> Sockets)
        {
            uint tcp6Size = 0;
            IntPtr tcp6Table = IntPtr.Zero;
            IPHelper.GetExtendedTcpTable(IntPtr.Zero, ref tcp6Size, false, IPHelper.AF_INET.IP6, IPHelper.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0);
            tcp6Table = Marshal.AllocHGlobal((int)tcp6Size);

            if (IPHelper.GetExtendedTcpTable(tcp6Table, ref tcp6Size, false, IPHelper.AF_INET.IP6, IPHelper.TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, 0) == IPHelper.NO_ERROR)
            {
                IPHelper.MIB_TCP6TABLE_OWNER_MODULE table = ((IPHelper.MIB_TCP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(tcp6Table, typeof(IPHelper.MIB_TCP6TABLE_OWNER_MODULE)));
                IntPtr rowPtr = (IntPtr)((long)tcp6Table + (long)Marshal.OffsetOf(typeof(IPHelper.MIB_TCP6TABLE_OWNER_MODULE), "FirstEntry"));

                for (uint i = 0; i < table.dwNumEntries; i++)
                {
                    IPHelper.MIB_TCP6ROW_OWNER_MODULE mibRow = (IPHelper.MIB_TCP6ROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(IPHelper.MIB_TCP6ROW_OWNER_MODULE));
                    rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(mibRow));

                    Sockets.Add(mibRow);
                }
            }

            return tcp6Table;
        }

        //////////////////////////////////////////////////////
        // UDP
        //

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetExtendedUdpTable(IntPtr pUdpTable, ref UInt32 dwOutBufLen, bool order, AF_INET ipVersion, UDP_TABLE_CLASS tblClass, UInt32 reserved);

        public enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        //////////////////////////////////////////////////////
        // UDPv4
        // 

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetOwnerModuleFromUdpEntry(ref MIB_UDPROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref UInt32 pdwSize);


        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPROW_OWNER_MODULE : I_SOCKET_ROW
        {
            internal UInt32 dwLocalAddr;
            internal UInt32 dwLocalPort;
            internal UInt32 dwOwningPid;
            internal Int64 liCreateTimestamp;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal ulong[] OwningModuleInfo;

            public int ProcessId { get { return (int)dwOwningPid; } }
            public ModuleInfo Module
            {
                get
                {
                    ModuleInfo Info = null;

                    uint buffSize = 0;
                    GetOwnerModuleFromUdpEntry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                    IntPtr buffer = Marshal.AllocHGlobal((int)buffSize);

                    if (GetOwnerModuleFromUdpEntry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize) == NO_ERROR)
                        Info = new ModuleInfo((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));

                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);

                    return Info;
                }
            }

            public UInt32 ProtocolType { get { return (UInt32)AF_PROT.UDP | (UInt32)AF_INET.IP4 << 8; } }
            public IPAddress RemoteAddress { get { return null; } }
            public UInt16 RemotePort { get { return 0; } }
            public IPAddress LocalAddress { get { return new IPAddress((UInt32)dwLocalAddr); } }
            public UInt16 LocalPort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwLocalPort); } }
            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.UNDEFINED; } }
            public DateTime CreationTime { get { return liCreateTimestamp == 0 ? DateTime.Now : DateTime.FromFileTime(liCreateTimestamp); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPTABLE_OWNER_MODULE
        {
            public UInt32 dwNumEntries;
            public MIB_UDPROW_OWNER_MODULE FirstEntry;
        }

        public static IntPtr GetUdpSockets(ref List<I_SOCKET_ROW> Sockets)
        {
            uint udp4Size = 0;
            IntPtr udp4Table = IntPtr.Zero;
            IPHelper.GetExtendedUdpTable(IntPtr.Zero, ref udp4Size, false, IPHelper.AF_INET.IP4, IPHelper.UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
            udp4Table = Marshal.AllocHGlobal((int)udp4Size);

            if (IPHelper.GetExtendedUdpTable(udp4Table, ref udp4Size, false, IPHelper.AF_INET.IP4, IPHelper.UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0) == IPHelper.NO_ERROR)
            {
                IPHelper.MIB_UDPTABLE_OWNER_MODULE table = ((IPHelper.MIB_UDPTABLE_OWNER_MODULE)Marshal.PtrToStructure(udp4Table, typeof(IPHelper.MIB_UDPTABLE_OWNER_MODULE)));
                IntPtr rowPtr = (IntPtr)((long)udp4Table + (long)Marshal.OffsetOf(typeof(IPHelper.MIB_UDPTABLE_OWNER_MODULE), "FirstEntry"));

                for (uint i = 0; i < table.dwNumEntries; i++)
                {
                    IPHelper.MIB_UDPROW_OWNER_MODULE mibRow = (IPHelper.MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(IPHelper.MIB_UDPROW_OWNER_MODULE));
                    rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(mibRow));

                    Sockets.Add(mibRow);
                }
            }

            return udp4Table;
        }

        //////////////////////////////////////////////////////
        // UDPv6
        // 

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern UInt32 GetOwnerModuleFromUdp6Entry(ref MIB_UDP6ROW_OWNER_MODULE pUdpEntry, TCPIP_OWNER_MODULE_INFO_CLASS Class, IntPtr Buffer, ref UInt32 pdwSize);


        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDP6ROW_OWNER_MODULE : I_SOCKET_ROW
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] ucLocalAddress;
            internal UInt32 dwLocalScopeId;
            internal UInt32 dwLocalPort;
            internal UInt32 dwOwningPid;
            internal Int64 liCreateTimestamp;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal ulong[] OwningModuleInfo;

            public int ProcessId { get { return (int)dwOwningPid; } }
            public ModuleInfo Module
            {
                get
                {
                    ModuleInfo Info = null;

                    uint buffSize = 0;
                    GetOwnerModuleFromUdp6Entry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, IntPtr.Zero, ref buffSize);
                    IntPtr buffer = Marshal.AllocHGlobal((int)buffSize);

                    if (GetOwnerModuleFromUdp6Entry(ref this, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref buffSize) == NO_ERROR)
                        Info = new ModuleInfo((TCPIP_OWNER_MODULE_BASIC_INFO)Marshal.PtrToStructure(buffer, typeof(TCPIP_OWNER_MODULE_BASIC_INFO)));

                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);

                    return Info;
                }
            }

            public UInt32 ProtocolType { get { return (UInt32)AF_PROT.UDP | (UInt32)AF_INET.IP6 << 8; } }
            public IPAddress RemoteAddress { get { return null; } }
            public UInt16 RemotePort { get { return 0; } }
            public IPAddress LocalAddress { get { return new IPAddress(ucLocalAddress); } }
            public UInt16 LocalPort { get { return (UInt16)IPAddress.NetworkToHostOrder((short)dwLocalPort); } }
            public MIB_TCP_STATE State { get { return MIB_TCP_STATE.UNDEFINED; } }
            public DateTime CreationTime { get { return liCreateTimestamp == 0 ? DateTime.Now : DateTime.FromFileTime(liCreateTimestamp); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDP6TABLE_OWNER_MODULE
        {
            public UInt32 dwNumEntries;
            public MIB_UDP6ROW_OWNER_MODULE FirstEntry;
        }

        public static IntPtr GetUdp6Sockets(ref List<I_SOCKET_ROW> Sockets)
        {
            uint udp6Size = 0;
            IntPtr udp6Table = IntPtr.Zero;
            IPHelper.GetExtendedUdpTable(IntPtr.Zero, ref udp6Size, false, IPHelper.AF_INET.IP4, IPHelper.UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0);
            udp6Table = Marshal.AllocHGlobal((int)udp6Size);

            if (IPHelper.GetExtendedUdpTable(udp6Table, ref udp6Size, false, IPHelper.AF_INET.IP4, IPHelper.UDP_TABLE_CLASS.UDP_TABLE_OWNER_MODULE, 0) == IPHelper.NO_ERROR)
            {
                IPHelper.MIB_UDP6TABLE_OWNER_MODULE table = ((IPHelper.MIB_UDP6TABLE_OWNER_MODULE)Marshal.PtrToStructure(udp6Table, typeof(IPHelper.MIB_UDP6TABLE_OWNER_MODULE)));
                IntPtr rowPtr = (IntPtr)((long)udp6Table + (long)Marshal.OffsetOf(typeof(IPHelper.MIB_UDP6TABLE_OWNER_MODULE), "FirstEntry"));

                for (uint i = 0; i < table.dwNumEntries; i++)
                {
                    IPHelper.MIB_UDPROW_OWNER_MODULE mibRow = (IPHelper.MIB_UDPROW_OWNER_MODULE)Marshal.PtrToStructure(rowPtr, typeof(IPHelper.MIB_UDPROW_OWNER_MODULE));
                    rowPtr = (IntPtr)((long)rowPtr + (long)Marshal.SizeOf(mibRow));

                    Sockets.Add(mibRow);
                }
            }

            return udp6Table;
        }
    }
}