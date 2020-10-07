using MiscHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "Program", Namespace = "http://schemas.datacontract.org/")]
    public class Program
    {
        //public Guid guid;

        [DataMember()]
        public ProgramID ID;
        [DataMember()]
        public string Description;

        //[NonSerialized()] // Note: BinaryFormatter can handle circular references
        public ProgramSet ProgSet = null;

        [DataMember()]
        public int RuleCount = 0;
        [DataMember()]
        public int EnabledRules = 0;
        [DataMember()]
        public int DisabledRules = 0;
        [DataMember()]
        public int ChgedRules = 0;

        [DataMember()]
        public DateTime LastAllowed = DateTime.MinValue;
        [DataMember()]
        public int AllowedCount = 0;
        [DataMember()]
        public DateTime LastBlocked = DateTime.MinValue;
        [DataMember()]
        public int BlockedCount = 0;
        public DateTime LastActivity { get { return MiscFunc.Max(LastAllowed, LastBlocked); } }

        [DataMember()]
        public int SocketCount = 0;
        [DataMember()]
        public int SocketsWeb = 0;
        [DataMember()]
        public int SocketsTcp = 0;
        [DataMember()]
        public int SocketsSrv = 0;
        [DataMember()]
        public int SocketsUdp = 0;

        [DataMember()]
        public UInt64 UploadRate = 0;
        [DataMember()]
        public UInt64 DownloadRate = 0;
        [DataMember()]
        public UInt64 TotalUpload = 0;
        [DataMember()]
        public UInt64 TotalDownload = 0;

        // The old values keep the last total of all closed sockets
        internal UInt64 OldUpload = 0;
        internal UInt64 OldDownload = 0;

        public void AssignSet(ProgramSet progSet)
        {
            // unlink old config
            if (ProgSet != null)
                ProgSet.Programs.Remove(ID);

            // link program with its config
            ProgSet = progSet;
            ProgSet.Programs.Add(ID, this);
        }


        public Program()
        {
            //guid = Guid.NewGuid();
        }

        public bool IsSpecial()
        {
            if (ID.Type == ProgramID.Types.System || ID.Type == ProgramID.Types.Global)
                return true;
            return false;
        }
        

        [Serializable()]
        [DataContract(Name = "LogEntry", Namespace = "http://schemas.datacontract.org/")]
        public class LogEntry : WithHost
        {
            [DataMember()]
            public Guid guid;

            [DataMember()]
            public ProgramID ProgID;
            [DataMember()]
            public FirewallEvent FwEvent;

            public enum States
            {
                Undefined = 0,
                FromLog,
                UnRuled, // there was no rule found for this connection
                RuleAllowed,
                RuleBlocked,
                RuleError, // a rule was found but it appears it was not obeyed (!)
            }
            [DataMember()]
            public States State = States.Undefined;

            public LogEntry()
            {
            }
        }
        

        [DataContract(Name = "DnsEntry", Namespace = "http://schemas.datacontract.org/")]
        public class DnsEntry
        {
            [DataMember()]
            public Guid guid;
            [DataMember()]
            public ProgramID ProgID;
            [DataMember()]
            public string HostName;
            //public IPAddress LastSeenIP;
            [DataMember()]
            public DateTime LastSeen;
            [DataMember()]
            public int SeenCounter = 0;
            [DataMember()]
            public int ConCounter = 0;
            [DataMember()]
            public UInt64 TotalUpload = 0;
            [DataMember()]
            public UInt64 TotalDownload = 0;

            public DnsEntry()
            {
            }
        }
   
    }
}
