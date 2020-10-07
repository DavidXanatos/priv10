using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MiscHelpers;
using PrivateService;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "NetworkSocket", Namespace = "http://schemas.datacontract.org/")]
    public class NetworkSocket : WithHost
    {
        [DataMember()]
        public Guid guid;
        [DataMember()]
        public ProgramID ProgID;
        public Program Program = null;
        [DataMember()]
        public UInt64 RemovedTimeStamp = 0;

        public UInt64 HashID;

        [DataMember()]
        public int ProcessId;

        [DataMember()]
        public UInt32 ProtocolType;
        [DataMember()]
        public IPAddress LocalAddress;
        [DataMember()]
        public UInt16 LocalPort;
        [DataMember()]
        public IPAddress RemoteAddress;
        [DataMember()]
        public UInt16 RemotePort;

        [DataMember()]
        public DateTime CreationTime = DateTime.Now;
        [DataMember()]
        public int State;

        [DataMember()]
        public Tuple<int, int> Access = Tuple.Create(0, 0); // outbound, inbound

        public NetworkStats Stats;
        [DataMember()]
        public UInt64 UploadRate { get { return Stats.UploadRate.ByteRate; } set { } }
        [DataMember()]
        public UInt64 DownloadRate { get { return Stats.DownloadRate.ByteRate; } set { } }

        [DataMember()]
        public DateTime LastActivity;

        public NetworkSocket()
        {
            guid = Guid.NewGuid();

            Stats = new NetworkStats();
            LastActivity = DateTime.Now;
        }

        public NetworkSocket(int processId, UInt32 protocolType, IPAddress localAddress, UInt16 localPort, IPAddress remoteAddress, UInt16 remotePort)
        {
            guid = Guid.NewGuid();

            Stats = new NetworkStats();
            LastActivity = DateTime.Now;

            ProcessId = processId;

            ProtocolType = protocolType;
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;

            HashID = NetworkSocket.MkHash(ProcessId, ProtocolType, LocalAddress, LocalPort, RemoteAddress, RemotePort);
        }

        public void Update(IPHelper.I_SOCKET_ROW socketRow, UInt64 Interval)
        {
            if (socketRow != null)
            {
                State = (int)socketRow.State;
            }

            // a program may have been removed than the sockets get unasigned and has to be re asigned
            if (Program == null)
            {
                Program prog = ProgID == null ? null : App.engine.ProgramList.FindProgram(ProgID, true, ProgramList.FuzzyModes.Any);
                if (prog != null)
                {
                    Program = prog;
                    prog.AddSocket(this);
                    Access = prog.LookupRuleAccess(this);
                }
            }

            Stats.Update(Interval);
        }

        public static UInt64 MkHash(int processId, UInt32 protocolType, IPAddress localAddress, UInt16 localPort, IPAddress remoteAddress, UInt16 remotePort)
        {
	        if ((protocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP)
		        remotePort = 0;

            UInt64 HashID = ((UInt64)localPort << 0) | ((UInt64)remotePort << 16) | ((UInt64)processId << 32);

	        return HashID;
        }

        public enum MatchMode
        {
            Fuzzy = 0,
            Strict,
        }

        public bool MatchIP(IPAddress L, IPAddress R)
        {
            if (L == null)
                return (R == null);
            return L.GetAddressBytes().SequenceEqual(R.GetAddressBytes());
        }

        public bool Match(int processId, UInt32 protocolType, IPAddress localAddress, UInt16 localPort, IPAddress remoteAddress, UInt16 remotePort, MatchMode mode)
        {
            if (ProcessId != processId)
                return false;

            if (ProtocolType != protocolType)
                return false;

            if ((ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == (UInt32)IPHelper.AF_PROT.TCP || (ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP)
            {
                if (LocalPort != localPort)
                    return false;
            }

            // a socket may be bount to all adapters than it has a local null address
            if (mode == MatchMode.Strict || LocalAddress == IPAddress.None)
            {
                if (!MatchIP(LocalAddress, localAddress))
                    return false;
            }

            // don't test the remote endpoint if this is a udp socket
            if (mode == MatchMode.Strict || (ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == (UInt32)IPHelper.AF_PROT.TCP)
            {
                if ((ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == (UInt32)IPHelper.AF_PROT.TCP || (ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP)
                {
                    if (RemotePort != remotePort)
                        return false;
                }

                if (!MatchIP(RemoteAddress, remoteAddress))
                    return false;
            }

            return true;
        }

        public void CountUpload(uint transferSize)
        {
            Stats.SentBytes += transferSize;
            LastActivity = DateTime.Now;
        }

        public void CountDownload(uint transferSize)
        {
            Stats.ReceivedBytes += transferSize;
            LastActivity = DateTime.Now;
        }
    }
}
