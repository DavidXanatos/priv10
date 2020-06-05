using Microsoft.Win32;
using NetFwTypeLib;
using NETWORKLIST;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class NetworkMonitorEtw : IDisposable
    {
        Microsoft.O365.Security.ETW.KernelTrace kernelTrace;
        Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider tcpProvider;
        Microsoft.O365.Security.ETW.Kernel.NetworkUdpipProvider udpProvider;
        Thread kernelThread = null;

        public NetworkMonitorEtw(Microsoft.O365.Security.ETW.IEventRecordDelegate OnNetworkEvent)
        {
            kernelTrace = new Microsoft.O365.Security.ETW.KernelTrace("priv10_TcpipLogger");

            tcpProvider = new Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider();
            tcpProvider.OnEvent += OnNetworkEvent;
            kernelTrace.Enable(tcpProvider);

            udpProvider = new Microsoft.O365.Security.ETW.Kernel.NetworkUdpipProvider();
            udpProvider.OnEvent += OnNetworkEvent;
            kernelTrace.Enable(udpProvider);

            kernelThread = new Thread(() => { kernelTrace.Start(); });
            kernelThread.Start();
        }

        public void Dispose()
        {
            kernelTrace.Stop();
            kernelThread.Join();
        }
    }

    public class NetworkMonitor: IDisposable
    {
        MultiValueDictionary<UInt64, NetworkSocket> SocketList = new MultiValueDictionary<UInt64, NetworkSocket>();
        UInt64 LastUpdate = 0;

        NetworkMonitorEtw Etw = null;

        RegistryMonitor[] regWatchers;

        public NetworkMonitor()
        {
            LastUpdate = MiscFunc.GetTickCount64();

            try
            {
                InitEtw();
                //AppLog.Debug("Successfully initialized NetworkMonitorETW");
            }
            catch
            {
                App.LogError("Failed to initialized NetworkMonitorETW");
            }

            regWatchers = new RegistryMonitor[] {
                new RegistryMonitor(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"),
                new RegistryMonitor(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces"),
                new RegistryMonitor(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles") // firewall profile configuration
            };

            foreach (var regWatcher in regWatchers)
            {
                regWatcher.RegChanged += new EventHandler(OnRegChanged);
                regWatcher.Start();
            }
        }

        private void InitEtw()
        {
            Etw = new NetworkMonitorEtw(OnNetworkEvent);
        }

        public void Dispose()
        {
            if (Etw != null)
                Etw.Dispose();

            foreach (var regWatcher in regWatchers)
                regWatcher.Stop();
        }

        public enum EtwNetEventType
        {
            Unknown = 0,
            Send,
            Recv
        }

        static Guid TcpIpGuid = new Guid(0x9a280ac0, 0xc8e0, 0x11d1, 0x84, 0xe2, 0x00, 0xc0, 0x4f, 0xb9, 0x98, 0xa2);
        static Guid UdpIpGuid = new Guid(0xbf3a50c5, 0xa9c9, 0x4988, 0xa0, 0x05, 0x2d, 0xf0, 0xb7, 0xc8, 0x0f, 0x80);

        [StructLayout(LayoutKind.Sequential)]
        public struct TcpIpOrUdpIp_IPV4_Header
        {
            public UInt32 PID;
            public UInt32 size;
            public UInt32 daddr;
            public UInt32 saddr;
            public UInt16 dport;
            public UInt16 sport;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TcpIpOrUdpIp_IPV6_Header
        {
            public UInt32 PID;
            public UInt32 size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] daddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] saddr;
            public UInt16 dport;
            public UInt16 sport;
        }

        private void OnNetworkEvent(Microsoft.O365.Security.ETW.IEventRecord record)
        {
            // WARNING: this function is called from the worker thread

            EtwNetEventType Type = EtwNetEventType.Unknown;
            UInt32 ProtocolType = 0;

            switch (record.Opcode)
            {
                case 0x0a: // send
                    Type = EtwNetEventType.Send;
                    ProtocolType = (UInt32)IPHelper.AF_INET.IP4 << 8;
                    break;
                case 0x0b: // receive
                    Type = EtwNetEventType.Recv;
                    ProtocolType = (UInt32)IPHelper.AF_INET.IP4 << 8;
                    break;
                case 0x0a + 16: // send ipv6
                    Type = EtwNetEventType.Send;
                    ProtocolType = (UInt32)IPHelper.AF_INET.IP6 << 8;
                    break;
                case 0x0b + 16: // receive ipv6
                    Type = EtwNetEventType.Recv;
                    ProtocolType = (UInt32)IPHelper.AF_INET.IP6 << 8;
                    break;
                default:
                    return;
            }

            if (record.ProviderId.Equals(TcpIpGuid))
                ProtocolType |= (UInt32)IPHelper.AF_PROT.TCP;
            else if (record.ProviderId.Equals(UdpIpGuid))
                ProtocolType |= (UInt32)IPHelper.AF_PROT.UDP;
            else
                return;

            int ProcessId = -1;
            UInt32 TransferSize = 0;

            IPAddress LocalAddress = null;
            UInt16 LocalPort = 0;
            IPAddress RemoteAddress = null;
            UInt16 RemotePort = 0;

            if ((ProtocolType & ((UInt32)IPHelper.AF_INET.IP4 << 8)) == ((UInt32)IPHelper.AF_INET.IP4 << 8))
            {
                TcpIpOrUdpIp_IPV4_Header data = (TcpIpOrUdpIp_IPV4_Header)Marshal.PtrToStructure(record.UserData, typeof(TcpIpOrUdpIp_IPV4_Header));

                ProcessId = (int)data.PID;
                TransferSize = data.size;

                LocalAddress = new IPAddress((UInt32)data.saddr);
                LocalPort = (UInt16)IPAddress.NetworkToHostOrder((short)data.sport);

                RemoteAddress = new IPAddress((UInt32)data.daddr);
                RemotePort = (UInt16)IPAddress.NetworkToHostOrder((short)data.dport);
            }
            else if ((ProtocolType & ((UInt32)IPHelper.AF_INET.IP6 << 8)) == ((UInt32)IPHelper.AF_INET.IP6 << 8))
            {
                TcpIpOrUdpIp_IPV6_Header data = (TcpIpOrUdpIp_IPV6_Header)Marshal.PtrToStructure(record.UserData, typeof(TcpIpOrUdpIp_IPV6_Header));

                ProcessId = (int)data.PID;
                TransferSize = data.size;

                LocalAddress = new IPAddress(data.saddr);
                LocalPort = (UInt16)IPAddress.NetworkToHostOrder((short)data.sport);

                RemoteAddress = new IPAddress(data.daddr);
                RemotePort = (UInt16)IPAddress.NetworkToHostOrder((short)data.dport);
            }
            else
                return;

            // Note: Incomming UDP packets have the endpoints swaped :/
            if ((ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP && Type == EtwNetEventType.Recv)
            {
                IPAddress TempAddresss = LocalAddress;
                UInt16 TempPort = LocalPort;
                LocalAddress = RemoteAddress;
                LocalPort = RemotePort;
                RemoteAddress = TempAddresss;
                RemotePort = TempPort;
            }

            App.engine?.RunInEngineThread(() =>
            {
                // Note: this happens in the engine thread

                NetworkSocket Socket = FindSocket(SocketList, ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort, NetworkSocket.MatchMode.Strict);
                if (Socket == null)
                {
                    Socket = new NetworkSocket(ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort);
                    SocketList.Add(Socket.HashID, Socket);
                }

                switch (Type)
                {
                    case EtwNetEventType.Send:  Socket.CountUpload(TransferSize); break;
                    case EtwNetEventType.Recv:  Socket.CountDownload(TransferSize); break;
                }
            });
        }


        private NetworkSocket FindSocket(MultiValueDictionary<UInt64, NetworkSocket> List, int ProcessId, UInt32 ProtocolType, IPAddress LocalAddress, UInt16 LocalPort, IPAddress RemoteAddress, UInt16 RemotePort, NetworkSocket.MatchMode Mode)
        {
            UInt64 HashID = NetworkSocket.MkHash(ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort);

            List<NetworkSocket> Matches = List.GetValues(HashID, false);
            if (Matches != null)
            {
                foreach (NetworkSocket CurSocket in Matches)
                {
                    if (CurSocket.Match(ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort, Mode))
                    {
                        return CurSocket;
                    }
                }
            }

            return null;
        }

        public NetworkSocket FindSocket(int ProcessId, UInt32 ProtocolType, IPAddress LocalAddress, UInt16 LocalPort, IPAddress RemoteAddress, UInt16 RemotePort, NetworkSocket.MatchMode Mode)
        {
            return FindSocket(SocketList, ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort, Mode);
        }

        public void UpdateSockets()
        {
            UInt64 curTick = MiscFunc.GetTickCount64();
            UInt64 Interval = curTick - LastUpdate;
            LastUpdate = curTick;

            List<IPHelper.I_SOCKET_ROW> Sockets = new List<IPHelper.I_SOCKET_ROW>();

            // enum all ockets
            IntPtr tcp4Table = IPHelper.GetTcpSockets(ref Sockets);
            IntPtr tcp6Table = IPHelper.GetTcp6Sockets(ref Sockets);
            IntPtr udp4Table = IPHelper.GetUdpSockets(ref Sockets);
            IntPtr udp6Table = IPHelper.GetUdp6Sockets(ref Sockets);

            MultiValueDictionary<UInt64, NetworkSocket> OldSocketList = SocketList.Clone();

            for (int i = 0; i < Sockets.Count; i++)
            {
                IPHelper.I_SOCKET_ROW SocketRow = Sockets[i];

                NetworkSocket Socket = FindSocket(OldSocketList, SocketRow.ProcessId, SocketRow.ProtocolType, SocketRow.LocalAddress, SocketRow.LocalPort, SocketRow.RemoteAddress, SocketRow.RemotePort, NetworkSocket.MatchMode.Strict);
                if (Socket != null)
                {
                    //AppLog.Debug("Found Socket {0}:{1} {2}:{3}", Socket.LocalAddress, Socket.LocalPort, Socket.RemoteAddress, Socket.RemotePort);
                    OldSocketList.Remove(Socket.HashID, Socket);
                }
                else
                {
                    Socket = new NetworkSocket(SocketRow.ProcessId, SocketRow.ProtocolType, SocketRow.LocalAddress, SocketRow.LocalPort, SocketRow.RemoteAddress, SocketRow.RemotePort);
                    //AppLog.Debug("Added Socket {0}:{1} {2}:{3}", Socket.LocalAddress, Socket.LocalPort, Socket.RemoteAddress, Socket.RemotePort);
                    SocketList.Add(Socket.HashID, Socket);
                }

                // Note: sockets observed using ETW are not yet initialized as we are missing owner informations there
                if (Socket.ProgID == null)
                {
                    Socket.CreationTime = SocketRow.CreationTime;

                    if (App.engine.DnsInspector != null && Socket.RemoteAddress != null)
                        App.engine.DnsInspector.GetHostName(Socket.ProcessId, Socket.RemoteAddress, Socket, NetworkSocket.HostSetter);

                    var moduleInfo = SocketRow.Module;

                    /*if (moduleInfo != null)
                        Console.WriteLine("Module {0} ({1})", moduleInfo.ModuleName,  moduleInfo.ModulePath);*/

                    if (moduleInfo == null || moduleInfo.ModulePath.Equals("System", StringComparison.OrdinalIgnoreCase))
                        Socket.ProgID = ProgramID.NewID(ProgramID.Types.System);
                    else
                    {
                        string fileName = moduleInfo.ModulePath;
                        string serviceTag = moduleInfo.ModuleName;

                        // Note: for services and system TCPIP_OWNER_MODULE_BASIC_INFO.pModuleName is the same TCPIP_OWNER_MODULE_BASIC_INFO.pModulePath
                        // hence we don't have the actuall exe path and we will have to resolve it.
                        if (serviceTag.Equals(fileName))
                            fileName = null; // filename not valid
                        else
                            serviceTag = null; // service tag not valid

                        Socket.ProgID = App.engine.GetProgIDbyPID(Socket.ProcessId, serviceTag, fileName);
                    }
                }

                Socket.Update(SocketRow, Interval);

                //IPHelper.ModuleInfo Info = SocketRow.Module;
                //AppLog.Debug("Socket {0}:{1} {2}:{3} {4}", Socket.LocalAddress, Socket.LocalPort, Socket.RemoteAddress, Socket.RemotePort, (Info != null ? (Info.ModulePath + " (" + Info.ModuleName + ")") : "") + " [PID: " + Socket.ProcessId + "]");
            }

            foreach (NetworkSocket Socket in OldSocketList.GetAllValues())
            {
                bool bIsUDPPseudoCon = (Socket.ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP && Socket.RemotePort != 0;

                // Note: sockets observed using ETW are not yet initialized as we are missing owner informations there
                if (Socket.ProgID == null)
                {
                    Socket.CreationTime = DateTime.Now;

                    if (App.engine.DnsInspector != null && Socket.RemoteAddress != null)
                        App.engine.DnsInspector.GetHostName(Socket.ProcessId, Socket.RemoteAddress, Socket, NetworkSocket.HostSetter);

                    // Note: etw captured connections does not handle services to well :/
                    Socket.ProgID = App.engine.GetProgIDbyPID(Socket.ProcessId, null, null);
                }

                Socket.Update(null, Interval);

                if (bIsUDPPseudoCon && (DateTime.Now - Socket.LastActivity).TotalMilliseconds < 5000) // 5 sec // todo: customize udp pseudo con time
                {
                    OldSocketList.Remove(Socket.HashID, Socket);

                    if (Socket.RemovedTimeStamp != 0)
                        Socket.RemovedTimeStamp = 0;
                }
                else
                    Socket.State = (int)IPHelper.MIB_TCP_STATE.CLOSED;
            }

            UInt64 CurTick = MiscFunc.GetCurTick();

            foreach (NetworkSocket Socket in OldSocketList.GetAllValues())
            {
                if (Socket.RemovedTimeStamp == 0)
                    Socket.RemovedTimeStamp = CurTick;
                else if(Socket.RemovedTimeStamp < CurTick + 3000) // todo: customize retention time
                {
                    SocketList.Remove(Socket.HashID, Socket);

                    Socket.Program?.RemoveSocket(Socket);
                }

                //AppLog.Debug("Removed Socket {0}:{1} {2}:{3}", CurSocket.LocalAddress, CurSocket.LocalPort, CurSocket.RemoteAddress, CurSocket.RemotePort);
            }

            // cleanup
            if (tcp4Table != IntPtr.Zero)
                Marshal.FreeHGlobal(tcp4Table);
            if (tcp6Table != IntPtr.Zero)
                Marshal.FreeHGlobal(tcp6Table);
            if (udp4Table != IntPtr.Zero)
                Marshal.FreeHGlobal(udp4Table);
            if (udp6Table != IntPtr.Zero)
                Marshal.FreeHGlobal(udp6Table);
        }

        public class AdapterInfo
        {
            public FirewallRule.Profiles Profile = FirewallRule.Profiles.Undefined;
            public FirewallRule.Interfaces Type = FirewallRule.Interfaces.All;

            public ICollection<UnicastIPAddressInformation> Addresses = null;

            public ICollection<IPAddress> GatewayAddresses = null;
            public ICollection<IPAddress> DnsAddresses = null;
            public ICollection<IPAddress> DhcpServerAddresses = null;
            public ICollection<IPAddress> WinsServersAddresses = null;
        }

        private Dictionary<IPAddress, AdapterInfo> AdapterInfoByIP = new Dictionary<IPAddress, AdapterInfo>();

        private static NetworkListManagerClass netMgr = new NetworkListManagerClass();

        // todo: get the right default behavioure there is a policy for that
        public FirewallRule.Profiles DefaultProfile = FirewallRule.Profiles.Public; // default windows behavioure: default profile is public

        private bool UpdateInterfaces = true;
        public event EventHandler<EventArgs> NetworksChanged;

        private void OnRegChanged(object sender, EventArgs e)
        {
            UpdateInterfaces = true;
        }

        private NetworkInterface[] Interfaces = new NetworkInterface[0];

        public bool UpdateNetworks()
        {
            //  foreach (NetworkInterface adapter in Interfaces) 
            // todo: get data rates etc... adapter.GetIPStatistics()

            if (!UpdateInterfaces)
                return false;
            UpdateInterfaces = false;

            Dictionary<string, FirewallRule.Profiles> NetworkProfiles = new Dictionary<string, FirewallRule.Profiles>();

            foreach (INetwork network in netMgr.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>())
            {
                FirewallRule.Profiles FirewallProfile = FirewallRule.Profiles.Undefined;
                switch (network.GetCategory())
                {
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE:                 FirewallProfile = FirewallRule.Profiles.Private; break;
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PUBLIC:                  FirewallProfile = FirewallRule.Profiles.Public; break;
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_DOMAIN_AUTHENTICATED:    FirewallProfile = FirewallRule.Profiles.Domain; break;
                }

                foreach (INetworkConnection connection in network.GetNetworkConnections().Cast<INetworkConnection>())
                {
                    string id = ("{" + connection.GetAdapterId().ToString() + "}").ToLower();

                    NetworkProfiles.Add(id, FirewallProfile);
                }
            }

            //DefaultProfile = App.engine.FirewallManager.GetCurrentProfiles();

            AdapterInfoByIP.Clear();
            //AppLog.Debug("ListingNetworks:");

            Interfaces = NetworkInterface.GetAllNetworkInterfaces(); // this is a bit slow!
            foreach (NetworkInterface adapter in Interfaces) 
            {
                try
                {
                    //AppLog.Debug("{0} {1} {2} {3}", adapter.Description, adapter.Id, adapter.NetworkInterfaceType.ToString(), adapter.OperationalStatus.ToString());

                    string id = adapter.Id.ToLower();

                    AdapterInfo Info = new AdapterInfo();
                    if (!NetworkProfiles.TryGetValue(id, out Info.Profile))
                        Info.Profile = DefaultProfile;

                    switch (adapter.NetworkInterfaceType)
                    {
                        case NetworkInterfaceType.Ethernet:
                        case NetworkInterfaceType.GigabitEthernet:
                        case NetworkInterfaceType.FastEthernetT:
                        case NetworkInterfaceType.FastEthernetFx:
                        case NetworkInterfaceType.Ethernet3Megabit:
                        case NetworkInterfaceType.TokenRing: Info.Type = FirewallRule.Interfaces.Lan; break;
                        case NetworkInterfaceType.Wireless80211: Info.Type = FirewallRule.Interfaces.Wireless; break;
                        case NetworkInterfaceType.Ppp: Info.Type = FirewallRule.Interfaces.RemoteAccess; break;
                        default: Info.Type = FirewallRule.Interfaces.All; break;
                    }

                    Info.Addresses = new List<UnicastIPAddressInformation>();
                    IPInterfaceProperties ip_info = adapter.GetIPProperties();
                    Info.GatewayAddresses = new List<IPAddress>();
                    foreach (var gw in ip_info.GatewayAddresses)
                        Info.GatewayAddresses.Add(gw.Address);
                    Info.DnsAddresses = ip_info.DnsAddresses;
                    Info.DhcpServerAddresses = ip_info.DhcpServerAddresses;
                    Info.WinsServersAddresses = ip_info.WinsServersAddresses;

                    foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                    {
                        Info.Addresses.Add(ip);

                        // Sanitize IPv6 addresses 
                        IPAddress _ip = new IPAddress(ip.Address.GetAddressBytes());

                        //AppLog.Debug("{2}({5}) has IP {0}/{3}/{4} has profile {1}", ip.Address.ToString(), ((FirewallRule.Profiles)Info.Profile).ToString(),
                        //    adapter.Description, ip.IPv4Mask.ToString(), ip.PrefixLength.ToString(), adapter.NetworkInterfaceType.ToString());

                        if (!AdapterInfoByIP.ContainsKey(_ip))
                            AdapterInfoByIP[_ip] = Info;
                    }
                }
                catch { } // in case a adapter becomes invalid justa fter the enumeration
            }
            //AppLog.Debug("+++");

            NetworksChanged?.Invoke(this, new EventArgs());
            return true;
        }

        public AdapterInfo GetAdapterInfoByIP(IPAddress IP)
        {
            if(UpdateInterfaces)
                UpdateNetworks();

            AdapterInfo Info = null;
            if (!AdapterInfoByIP.TryGetValue(IP, out Info))
            {
                Info = new AdapterInfo();
                Info.Profile = DefaultProfile;
            }
            return Info;
        }
    }
}
