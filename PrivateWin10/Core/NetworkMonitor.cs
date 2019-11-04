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
    public class NetworkMonitor: IDisposable
    {
        MultiValueDictionary<UInt64, NetworkSocket> SocketList = new MultiValueDictionary<UInt64, NetworkSocket>();

        UInt64 LastUpdate = 0;

        Microsoft.O365.Security.ETW.KernelTrace kernelTrace;
        Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider networkProvider;
        Thread kernelThread;


        public NetworkMonitor()
        {
            LastUpdate = MiscFunc.GetTickCount64();

            kernelTrace = new Microsoft.O365.Security.ETW.KernelTrace("priv10_KernelLogger");
            networkProvider = new Microsoft.O365.Security.ETW.Kernel.NetworkTcpipProvider();
            networkProvider.OnEvent += OnNetworkEvent;
            kernelTrace.Enable(networkProvider);
            
            kernelThread = new Thread(() => { kernelTrace.Start(); });
            kernelThread.Start();
        }

        public void Dispose()
        {
            kernelTrace.Stop();
            kernelThread.Join();
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
            if (record.ProviderId.Equals(UdpIpGuid))
                ProtocolType |= (UInt32)IPHelper.AF_PROT.UDP;

            int ProcessId = -1;
            UInt32 TransferSize = 0;

            IPAddress LocalAddress = null;
            UInt16 LocalPort = 0;
            IPAddress RemoteAddress = null;
            UInt16 RemotePort = 0;

            if ((ProtocolType & (UInt32)IPHelper.AF_INET.IP4) != 0)
            {
                TcpIpOrUdpIp_IPV4_Header data = (TcpIpOrUdpIp_IPV4_Header)Marshal.PtrToStructure(record.UserData, typeof(TcpIpOrUdpIp_IPV4_Header));

                ProcessId = (int)data.PID;
                TransferSize = data.size;

                LocalAddress = new IPAddress((UInt32)data.saddr);
                LocalPort = (UInt16)IPAddress.NetworkToHostOrder((short)data.sport);

                RemoteAddress = new IPAddress((UInt32)data.daddr);
                RemotePort = (UInt16)IPAddress.NetworkToHostOrder((short)data.dport);
            }
            else if ((ProtocolType & (UInt32)IPHelper.AF_INET.IP6) != 0)
            {
                TcpIpOrUdpIp_IPV6_Header data = (TcpIpOrUdpIp_IPV6_Header)Marshal.PtrToStructure(record.UserData, typeof(TcpIpOrUdpIp_IPV6_Header));

                ProcessId = (int)data.PID;
                TransferSize = data.size;

                LocalAddress = new IPAddress(data.saddr);
                LocalPort = (UInt16)IPAddress.NetworkToHostOrder((short)data.sport);

                RemoteAddress = new IPAddress(data.daddr);
                RemotePort = (UInt16)IPAddress.NetworkToHostOrder((short)data.dport);
            }

            // Note: Incomming UDP packets have the endpoints swaped :/
            if ((ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) != 0 && Type == EtwNetEventType.Recv)
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

                NetworkSocket Socket = FindSocket(SocketList, ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort, NetworkSocket.MatchMode.Fuzzy);
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

            MultiValueDictionary <UInt64, NetworkSocket> OldSocketList = SocketList.Clone();

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

                    if(Socket.RemoteAddress != null)
                        App.engine.DnsInspector.GetHostName(Socket.ProcessId, Socket.RemoteAddress, Socket, NetworkSocket.HostSetter);

                    var moduleInfo = SocketRow.Module;
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

                // a program may have been removed than the sockets get unasigned and has to be re asigned
                if (Socket.Assigned == false)
                {
                    Program prog = Socket.ProgID == null ? null : App.engine.ProgramList.GetProgram(Socket.ProgID, true, ProgramList.FuzzyModes.Any);
                    prog?.AddSocket(Socket);
                }

                Socket.Update(SocketRow, Interval);

                //IPHelper.ModuleInfo Info = SocketRow.Module;
                //AppLog.Debug("Socket {0}:{1} {2}:{3} {4}", Socket.LocalAddress, Socket.LocalPort, Socket.RemoteAddress, Socket.RemotePort, (Info != null ? (Info.ModulePath + " (" + Info.ModuleName + ")") : "") + " [PID: " + Socket.ProcessId + "]");
            }

            UInt64 CurTick = MiscFunc.GetCurTick();

            foreach (NetworkSocket Socket in OldSocketList.GetAllValues())
            {
                if (Socket.RemovedTimeStamp == 0)
                    Socket.RemovedTimeStamp = CurTick;
                else if(Socket.RemovedTimeStamp < CurTick + 3000) // todo: customize retention time
                {
                    SocketList.Remove(Socket.HashID, Socket);

                    Program prog = Socket.ProgID == null ? null : App.engine.ProgramList.GetProgram(Socket.ProgID);
                    prog?.RemoveSocket(Socket);
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

        private Dictionary<IPAddress, int> FirewallProfilesByIP = new Dictionary<IPAddress, int>();

        private static NetworkListManagerClass netMgr = new NetworkListManagerClass();

        private UInt64 LastNetworkUpdate = 0;

        // todo: get the right default behavioure there is a policy for that
        public int DefaultProfile = (int)FirewallRule.Profiles.Public; // default windows behavioure: default profile is public

        public void UpdateNetworks()
        {
            LastNetworkUpdate = MiscFunc.GetTickCount64();

            Dictionary<string, int> NetworkProfiles = new Dictionary<string, int>();

            foreach (INetwork network in netMgr.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>())
            {
                int FirewallProfile = 0;
                switch (network.GetCategory())
                {
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE:                 FirewallProfile = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE; break;
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PUBLIC:                  FirewallProfile = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC; break;
                    case NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_DOMAIN_AUTHENTICATED:    FirewallProfile = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN; break;
                }

                foreach (INetworkConnection connection in network.GetNetworkConnections().Cast<INetworkConnection>())
                {
                    string id = ("{" + connection.GetAdapterId().ToString() + "}").ToLower();

                    NetworkProfiles.Add(id, FirewallProfile);
                }
            }

            //DefaultProfile = App.engine.FirewallManager.GetCurrentProfiles();

            FirewallProfilesByIP.Clear();

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) // this is a bit slow!
            {
                string id = adapter.Id.ToLower();

                foreach (var ip in adapter.GetIPProperties().UnicastAddresses)
                {
                    int profile = 0;
                    if (!NetworkProfiles.TryGetValue(id, out profile))
                        profile = DefaultProfile;

                    // Sanitize IPv6 addresses 
                    IPAddress _ip = new IPAddress(ip.Address.GetAddressBytes());

                    FirewallProfilesByIP[_ip] = profile;
                }
            }
        }

        public int GetFirewallProfileByIP(IPAddress IP)
        {
            int Profile = 0;
            if (!FirewallProfilesByIP.TryGetValue(IP, out Profile))
            {
                if (MiscFunc.GetTickCount64() - LastNetworkUpdate >= 1000)
                {
                    UpdateNetworks();

                    // retry
                    FirewallProfilesByIP.TryGetValue(IP, out Profile);
                }
            }
            return Profile != 0 ? Profile : DefaultProfile;
        }
    }
}
