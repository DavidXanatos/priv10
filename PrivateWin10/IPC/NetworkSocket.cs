using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
        //public Program Program = null;
        [DataMember()]
        public UInt64 RemovedTimeStamp = 0;

        //public UInt64 HashID;

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
        
        [DataMember()]
        public UInt64 SentBytes;
        [DataMember()]
        public UInt64 UploadRate;
        [DataMember()]
        public UInt64 ReceivedBytes;
        [DataMember()]
        public UInt64 DownloadRate;

        [DataMember()]
        public DateTime LastActivity;

        public NetworkSocket()
        {

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
