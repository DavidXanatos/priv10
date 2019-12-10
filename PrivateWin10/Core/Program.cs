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
    public class Program
    {
        //public Guid guid;

        public ProgramID ID;
        public string Description;

        //[NonSerialized()] // Note: BinaryFormatter can handle circular references
        public ProgramSet ProgSet = null;

        [NonSerialized()]
        public Dictionary<string, FirewallRuleEx> Rules = new Dictionary<string, FirewallRuleEx>();

        [NonSerialized()]
        public Dictionary<Guid, NetworkSocket> Sockets = new Dictionary<Guid, NetworkSocket>();

        [NonSerialized()]
        public List<LogEntry> Log = new List<LogEntry>();

        [NonSerialized()]
        public Dictionary<string, DnsEntry> DnsLog = new Dictionary<string, DnsEntry>();

        public int RuleCount = 0;
        public int EnabledRules = 0;
        public int DisabledRules = 0;
        public int ChgedRules = 0;

        public DateTime LastAllowed = DateTime.MinValue;
        public int AllowedCount = 0;
        public DateTime LastBlocked = DateTime.MinValue;
        public int BlockedCount = 0;
        public DateTime LastActivity { get { return MiscFunc.Max(LastAllowed, LastBlocked); } }
        private bool ActivityChanged = false;

        public int SocketCount = 0;
        public int SocketsWeb = 0;
        public int SocketsTcp = 0;
        public int SocketsSrv = 0;
        public int SocketsUdp = 0;

        public UInt64 UploadRate = 0;
        public UInt64 DownloadRate = 0;
        // todo: xxx
        public UInt64 TotalUpload = 0;
        public UInt64 TotalDownload = 0;

        public void AssignSet(ProgramSet progSet)
        {
            // unlink old config
            if (ProgSet != null)
                ProgSet.Programs.Remove(ID);

            // link program with its config
            ProgSet = progSet;
            ProgSet.Programs.Add(ID, this);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext c)
        {
            RuleCount = 0;
            EnabledRules = 0;
            DisabledRules = 0;
            ChgedRules = 0;            
            foreach (FirewallRuleEx rule in Rules.Values)
            {
                RuleCount++;
                if (rule.Enabled)
                    EnabledRules++;
                else
                    DisabledRules++;
                if (rule.State != FirewallRuleEx.States.Approved)
                    ChgedRules++;
            }
            
            SocketsWeb = 0;
            SocketsTcp = 0;
            SocketsUdp = 0;
            SocketsSrv = 0;
            foreach (NetworkSocket entry in Sockets.Values)
            {
                if ((entry.ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) != 0)
                {
                    SocketsUdp++;
                }
                else if ((entry.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) != 0)
                {
                    SocketsTcp++;
                    if (entry.RemotePort == 80 || entry.RemotePort == 443)
                        SocketsWeb++;
                    if (entry.State == (int)IPHelper.MIB_TCP_STATE.LISTENING)
                        SocketsSrv++;
                }
            }
        }

        public Program()
        {
            //guid = Guid.NewGuid();
        }

        public Program(ProgramID progID)
        {
            //guid = Guid.NewGuid();

            ID = progID.Duplicate();

            string Name = "";
            string Info = null;

            switch (ID.Type)
            {
                case ProgramID.Types.System:
                    Name = Translate.fmt("name_system");
                    break;
                case ProgramID.Types.Global:
                    Name = Translate.fmt("name_global");
                    break;
                case ProgramID.Types.Program:
                    Name = System.IO.Path.GetFileName(ID.Path);
                    Info = MiscFunc.GetExeDescription(ID.Path);
                    break;
                case ProgramID.Types.Service:
                    Name = ID.GetServiceId();
                    Info = ID.GetServiceName();
                    break;
                case ProgramID.Types.App:
                    Name = ID.GetPackageName();
                    Info = App.PkgMgr?.GetAppInfoBySid(ID.GetPackageSID())?.Name;
                    break;
            }

            if (Info != null && Info.Length > 0)
                Description = Info + " (" + Name + ")";
            else
                Description = Name;
        }

        public bool Update()
        {
            UInt64 uploadRate = 0;
            UInt64 downloadRate = 0;
            foreach (NetworkSocket Socket in Sockets.Values)
            {
                uploadRate += Socket.Stats.UploadRate.ByteRate;
                downloadRate += Socket.Stats.DownloadRate.ByteRate;
            }

            if (UploadRate != uploadRate || DownloadRate != downloadRate || SocketCount != Sockets.Count || ActivityChanged)
            {
                SocketCount = Sockets.Count;

                UploadRate = uploadRate;
                DownloadRate = downloadRate;

                ActivityChanged = false;

                return true;
            }
            return false;
        }

        public bool IsSpecial()
        {
            if (ID.Type == ProgramID.Types.System || ID.Type == ProgramID.Types.Global)
                return true;
            return false;
        }

        public bool Exists()
        {
            bool PathMissing = (ID.Path != null && ID.Path.Length > 0 && !File.Exists(ID.Path));

            switch (ID.Type)
            {
                case ProgramID.Types.Program:   return !PathMissing;
                case ProgramID.Types.Service:   return (ServiceHelper.GetServiceState(ID.GetServiceId()) != ServiceHelper.ServiceState.NotFound) && !PathMissing;
                case ProgramID.Types.App:       return App.PkgMgr?.GetAppInfoBySid(ID.GetPackageSID()) != null && !PathMissing;
                default:        return true;
            }
        }

        public void AddLogEntry(LogEntry logEntry)
        {
            switch (logEntry.FwEvent.Action)
            {
                case FirewallRule.Actions.Allow:
                    AllowedCount++; 
                    LastAllowed = logEntry.FwEvent.TimeStamp;
                    break;
                case FirewallRule.Actions.Block:
                    BlockedCount++;
                    LastBlocked = logEntry.FwEvent.TimeStamp;
                    break;
            }

            // add to log
            Log.Add(logEntry);
            while (Log.Count > ProgramList.MaxLogLength)
                Log.RemoveAt(0);
        }

        public FirewallRule.Actions LookupRuleAction(FirewallEvent FwEvent, NetworkMonitor.AdapterInfo NicInfo)
        {
            int BlockRules = 0;
            int AllowRules = 0;
            foreach (FirewallRule rule in Rules.Values)
            {
                if (!rule.Enabled)
                    continue;
                if (rule.Direction != FwEvent.Direction)
                    continue;
                if (rule.Protocol != (int)NetFunc.KnownProtocols.Any && FwEvent.Protocol != rule.Protocol)
                    continue;
                if (((int)NicInfo.Profile & rule.Profile) == 0)
                    continue;
                if (rule.Interface != (int)FirewallRule.Interfaces.All && (int)NicInfo.Type != rule.Interface)
                    continue;
                if (!FirewallRule.MatchEndpoint(rule.RemoteAddresses, rule.RemotePorts, FwEvent.RemoteAddress, FwEvent.RemotePort, NicInfo))
                    continue;
                if (!FirewallRule.MatchEndpoint(rule.LocalAddresses, rule.LocalPorts, FwEvent.RemoteAddress, FwEvent.RemotePort, NicInfo))
                    continue;

                if (rule.Action == FirewallRule.Actions.Allow)
                    AllowRules++;
                else if (rule.Action == FirewallRule.Actions.Block)
                    BlockRules++;
            }

            // Note: block rules take precedence
            if (BlockRules > 0)
                return FirewallRule.Actions.Block;
            if (AllowRules > 0)
                return FirewallRule.Actions.Allow;
            return FirewallRule.Actions.Undefined;
        }

        public Tuple<int, int> LookupRuleAccess(NetworkSocket Socket)
        {
            int AllowOutProfiles = 0;
            int BlockOutProfiles = 0;
            int AllowInProfiles = 0;
            int BlockInProfiles = 0;

            int Protocol = 0;
            if ((Socket.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) != 0)
                Protocol = (int)IPHelper.AF_PROT.TCP;
            else if ((Socket.ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) != 0)
                Protocol = (int)IPHelper.AF_PROT.UDP;
            else
                return Tuple.Create(0, 0);

            foreach (FirewallRule rule in Rules.Values)
            {
                if (!rule.Enabled)
                    continue;

                if (rule.Protocol != (int)NetFunc.KnownProtocols.Any && Protocol != rule.Protocol)
                    continue;
                if (Protocol == (int)IPHelper.AF_PROT.TCP)
                {
                    if (!FirewallRule.MatchEndpoint(rule.RemoteAddresses, rule.RemotePorts, Socket.RemoteAddress, Socket.RemotePort))
                        continue;
                }
                if (!FirewallRule.MatchEndpoint(rule.LocalAddresses, rule.LocalPorts, Socket.LocalAddress, Socket.LocalPort))
                    continue;

                switch (rule.Direction)
                {
                    case FirewallRule.Directions.Outboun:
                    {
                        if (rule.Action == FirewallRule.Actions.Allow)
                            AllowOutProfiles |= rule.Profile;
                        else if (rule.Action == FirewallRule.Actions.Block)
                            BlockOutProfiles |= rule.Profile;
                        break;
                    }
                    case FirewallRule.Directions.Inbound:
                    {
                        if (rule.Action == FirewallRule.Actions.Allow)
                            AllowInProfiles |= rule.Profile;
                        else if (rule.Action == FirewallRule.Actions.Block)
                            BlockInProfiles |= rule.Profile;
                        break;
                    }
                }
            }

            for (int i = 0; i < FirewallManager.FwProfiles.Length; i++)
            {
                if ((AllowOutProfiles & (int)FirewallManager.FwProfiles[i]) == 0
                 && (BlockOutProfiles & (int)FirewallManager.FwProfiles[i]) == 0)
                {
                    if (App.engine.FirewallManager.GetDefaultOutboundAction(FirewallManager.FwProfiles[i]) == FirewallRule.Actions.Allow)
                        AllowOutProfiles |= (int)FirewallManager.FwProfiles[i];
                    else
                        BlockOutProfiles |= (int)FirewallManager.FwProfiles[i];
                }

                if ((AllowInProfiles & (int)FirewallManager.FwProfiles[i]) == 0
                 && (BlockInProfiles & (int)FirewallManager.FwProfiles[i]) == 0)
                {
                    if (App.engine.FirewallManager.GetDefaultInboundAction(FirewallManager.FwProfiles[i]) == FirewallRule.Actions.Allow)
                        AllowInProfiles |= (int)FirewallManager.FwProfiles[i];
                    else
                        BlockInProfiles |= (int)FirewallManager.FwProfiles[i];
                }
            }

            AllowOutProfiles &= ~BlockOutProfiles;
            AllowInProfiles &= ~BlockInProfiles;

            return Tuple.Create(AllowOutProfiles, AllowInProfiles);
        }

        [Serializable()]
        public class LogEntry : WithHost
        {
            public Guid guid;

            public ProgramID ProgID;
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
            public States State = States.Undefined;

            public void CheckAction(FirewallRule.Actions action)
            {
                switch (action)
                {
                    case FirewallRule.Actions.Undefined:
                        State = States.UnRuled;
                        break;
                    case FirewallRule.Actions.Allow:
                        if (FwEvent.Action == FirewallRule.Actions.Allow)
                            State = LogEntry.States.RuleAllowed;
                        else
                            State = LogEntry.States.RuleError;
                        break;
                    case FirewallRule.Actions.Block:
                        if (FwEvent.Action == FirewallRule.Actions.Block)
                            State = LogEntry.States.RuleBlocked;
                        else
                            State = LogEntry.States.RuleError;
                        break;
                }
            }

            public LogEntry(FirewallEvent Event, ProgramID progID)
            {
                guid = Guid.NewGuid();
                FwEvent = Event;
                ProgID = progID;
            }
        }

        public void AddSocket(NetworkSocket socket)
        {
            socket.Assigned = true;
            Sockets.Add(socket.guid, socket);
        }

        public void RemoveSocket(NetworkSocket socket)
        {
            Sockets.Remove(socket.guid);
        }


        [Serializable()]
        public class DnsEntry
        {
            public Guid guid;
            public ProgramID ProgID;
            public string HostName;
            //public IPAddress LastSeenIP;
            public DateTime LastSeen;
            public int SeenCounter;

            public DnsEntry(ProgramID progID)
            {
                guid = Guid.NewGuid();
                ProgID = progID;
                SeenCounter = 0;
            }

            public void Store(XmlWriter writer)
            {
                writer.WriteStartElement("Entry");

                writer.WriteElementString("HostName", HostName);
                writer.WriteElementString("LastSeen", LastSeen.ToString());
                writer.WriteElementString("SeenCounter", SeenCounter.ToString());

                writer.WriteEndElement();
            }

            public bool Load(XmlNode entryNode)
            {
                foreach (XmlNode node in entryNode.ChildNodes)
                {
                    if (node.Name == "HostName")
                        HostName = node.InnerText;
                    else if (node.Name == "LastSeenUTC")
                        DateTime.TryParse(node.InnerText, out LastSeen);
                    else if (node.Name == "SeenCounter")
                        int.TryParse(node.InnerText, out SeenCounter);
                }
                return HostName != null;
            }
        }

        public void LogDomain(string HostName, DateTime TimeStamp)
        {
            DnsEntry Entry = null;
            if (!DnsLog.TryGetValue(HostName, out Entry))
            {
                Entry = new DnsEntry(ID);
                Entry.HostName = HostName;
                DnsLog.Add(HostName, Entry);
            }
            else if (Entry.LastSeen == TimeStamp)
                return; // dont count duplicates

            Entry.LastSeen = TimeStamp;
            //Entry.LastSeenIP = IP;
            Entry.SeenCounter++;
        }


        public void Store(XmlWriter writer)
        {
            writer.WriteStartElement("Program");

            // Note: ID must be first!!!
            ID.Store(writer);

            writer.WriteElementString("Description", Description);

            writer.WriteStartElement("FwRules");
            foreach (FirewallRuleEx rule in Rules.Values)
                rule.Store(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("DnsLog");
            foreach (DnsEntry Entry in DnsLog.Values)
                Entry.Store(writer);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public bool Load(XmlNode entryNode)
        {
            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "ID")
                {
                    ProgramID id = new ProgramID();
                    if (id.Load(node))
                        ID = id;
                }
                else if (node.Name == "Description")
                    Description = node.InnerText;
                else if (node.Name == "FwRules")
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        FirewallRuleEx rule = new FirewallRuleEx();
                        rule.ProgID = ID;
                        if (rule.Load(childNode) && !Rules.ContainsKey(rule.guid))
                            Rules.Add(rule.guid, rule);
                        else
                            App.LogError("Failed to load Firewall RuleEx {0} in {1}", rule.Name != null ? rule.Name : "[un named]", this.Description);
                    }
                }
                else if (node.Name == "DnsLog")
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        DnsEntry Entry = new DnsEntry(ID);
                        if (Entry.Load(childNode) && !DnsLog.ContainsKey(Entry.HostName))
                            DnsLog.Add(Entry.HostName, Entry);
                        else
                            App.LogError("Failed to load DnsLog Entry in {0}", this.Description);
                    }
                }
                else
                    AppLog.Debug("Unknown Program Value, '{0}':{1}", node.Name, node.InnerText);
            }
            return ID != null;
        }
    }
}
