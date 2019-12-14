using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    [Serializable()]
    public class NetworkSocket : WithHost
    {
        public Guid guid;
        public ProgramID ProgID;
        public bool Assigned;
        public UInt64 RemovedTimeStamp = 0;

        public UInt64 HashID;

        public int ProcessId;

        public UInt32 ProtocolType;
        public IPAddress LocalAddress;
        public UInt16 LocalPort;
        public IPAddress RemoteAddress;
        public UInt16 RemotePort;

        public DateTime CreationTime = DateTime.Now;
        public int State;
        public Tuple<int, int> Access = Tuple.Create(0, 0); // outbound, inbound

        public NetworkStats Stats;
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
            if (Assigned == false)
            {
                Program prog = ProgID == null ? null : App.engine.ProgramList.GetProgram(ProgID, true, ProgramList.FuzzyModes.Any);
                prog?.AddSocket(this);
                if (prog != null)
                    Access = prog.LookupRuleAccess(this);
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

        public string GetStateString()
        {
            if(RemovedTimeStamp != 0)
                return Translate.fmt("str_closed");

            if ((ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == (UInt32)IPHelper.AF_PROT.UDP)
            {
                if (State == (int)IPHelper.MIB_TCP_STATE.CLOSED)
                    return Translate.fmt("str_closed");
                return Translate.fmt("str_open");
            }

            // all these are TCP states
            switch (State)
            {
                case (int)IPHelper.MIB_TCP_STATE.CLOSED: return Translate.fmt("str_closed");
                case (int)IPHelper.MIB_TCP_STATE.LISTENING: return Translate.fmt("str_listen");
                case (int)IPHelper.MIB_TCP_STATE.SYN_SENT: return Translate.fmt("str_syn_sent");
                case (int)IPHelper.MIB_TCP_STATE.SYN_RCVD: return Translate.fmt("str_syn_received");
                case (int)IPHelper.MIB_TCP_STATE.ESTABLISHED: return Translate.fmt("str_established");
                case (int)IPHelper.MIB_TCP_STATE.FIN_WAIT1: return Translate.fmt("str_fin_wait_1");
                case (int)IPHelper.MIB_TCP_STATE.FIN_WAIT2: return Translate.fmt("str_fin_wait_2");
                case (int)IPHelper.MIB_TCP_STATE.CLOSE_WAIT: return Translate.fmt("str_close_wait");
                case (int)IPHelper.MIB_TCP_STATE.CLOSING: return Translate.fmt("str_closing");
                case (int)IPHelper.MIB_TCP_STATE.LAST_ACK: return Translate.fmt("str_last_ack");
                case (int)IPHelper.MIB_TCP_STATE.TIME_WAIT: return Translate.fmt("str_time_wait");
                case (int)IPHelper.MIB_TCP_STATE.DELETE_TCB: return Translate.fmt("str_delete_tcb");
                case -1: return Translate.fmt("str_fw_blocked");
                default: return Translate.fmt("str_undefined");
            }
        }
    }
}
