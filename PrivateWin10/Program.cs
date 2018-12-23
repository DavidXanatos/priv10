using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    [Serializable()]
    public class Program
    {
        public Guid guid;
        public HashSet<ProgramList.ID> IDs = new HashSet<ProgramList.ID>();

        [Serializable()]
        public class Config
        {
            public string Name;
            public string Category;
            public string Icon;

            public enum AccessLevels
            {
                Unconfigured = 0,
                FullAccess,
                CustomConfig,
                LocalOnly,
                BlockAccess,
                StopNotify
            }

            public bool? Notify = null;
            public bool? GetNotify() { return IsSilenced() ? (bool?)false : Notify;}
            public void SetNotify(bool? set) { SilenceUntill = 0; Notify = set; }
            public long SilenceUntill = 0;
            public bool IsSilenced() { return SilenceUntill != 0 && SilenceUntill > DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
            public AccessLevels NetAccess = AccessLevels.Unconfigured;
            public AccessLevels CurAccess = AccessLevels.Unconfigured;

            // Custom option
            // todo
        }

        public Config config = new Config();

        public DateTime lastActivity = DateTime.MinValue;
        public int blockedConnections = 0;
        public int allowedConnections = 0;

        [NonSerialized()]
        public Dictionary<Guid,FirewallRule> Rules = new Dictionary<Guid, FirewallRule>();

        [NonSerialized()]
        public List<LogEntry> Log = new List<LogEntry>();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            Rules = new Dictionary<Guid, FirewallRule>();
            Log = new List<LogEntry>();
        }

        public Program()
        {
            guid = Guid.NewGuid();
        }

        public Program(ProgramList.ID id)
        {
            guid = Guid.NewGuid();

            IDs.Add(id);

            config.Name = id.GetDisplayName();
        }

        public ProgramList.ID GetMainID()
        {
            return IDs.First();
        }

        public string GetIcon()
        {
            if (config.Icon != null && config.Icon.Length > 0)
                return config.Icon;
            return GetMainID().Path;
        }


        public void LogActivity(LogEntry logEntry, bool fromLog)
        {
            lastActivity = logEntry.TimeStamp;
            switch (logEntry.Action)
            {
                case Firewall.Actions.Allow: allowedConnections++; break;
                case Firewall.Actions.Block: blockedConnections++; break;
            }

            Log.Add(logEntry);

            while (Log.Count > App.engine.programs.MaxLogLength)
                Log.RemoveAt(0);

            if (fromLog) {
                logEntry.Type = LogEntry.Types.FromLog;
                return;
            }

            Firewall.Actions action = LookupAction(logEntry);
            switch (action)
            {
                case Firewall.Actions.Undefined:
                    logEntry.Type = LogEntry.Types.UnRuled;
                    break;
                case Firewall.Actions.Allow:
                    if(logEntry.Action == Firewall.Actions.Allow)
                        logEntry.Type = LogEntry.Types.RuleAllowed;
                    else
                        logEntry.Type = LogEntry.Types.RuleError;
                    break;
                case Firewall.Actions.Block:
                    if (logEntry.Action == Firewall.Actions.Block)
                        logEntry.Type = LogEntry.Types.RuleBlocked;
                    else
                        logEntry.Type = LogEntry.Types.RuleError;
                    break;
            }
        }

        public Firewall.Actions LookupAction(LogEntry logEntry)
        {
            Firewall.MatchAddress(logEntry.RemoteAddress, "");


            int BlockRules = 0;
            int AllowRules = 0;
            foreach (FirewallRule rule in Rules.Values)
            {
                // todo: make a map with rules by ID
                if (rule.mID.CompareTo(logEntry.mID) != 0)
                    continue;

                if (!rule.Enabled)
                    continue;
                if (rule.Direction != logEntry.Direction)
                    continue;

                if (!Firewall.IsEmptyOrStar(rule.LocalPorts) && !Firewall.MatchPort(logEntry.LocalPort, rule.LocalPorts))
                    continue;
                if (!Firewall.IsEmptyOrStar(rule.RemotePorts) && !Firewall.MatchPort(logEntry.RemotePort, rule.RemotePorts))
                    continue;

                //if (!Firewall.IsEmptyOrStar(rule.SrcAddresses) && !Firewall.MatchAddress(logEntry.SrcAddress, rule.SrcAddresses))
                //    continue;
                if (!Firewall.IsEmptyOrStar(rule.RemoteAddresses) && !Firewall.MatchAddress(logEntry.RemoteAddress, rule.RemoteAddresses))
                    continue;

                if (rule.Protocol != (int)NetFunc.KnownProtocols.Any && logEntry.Protocol != rule.Protocol)
                    continue;

                if(!Firewall.MatchProfiles(logEntry.Profile, rule.Profile))
                    continue;

                if (rule.Action == Firewall.Actions.Allow)
                    AllowRules++;
                else if (rule.Action == Firewall.Actions.Block)
                    BlockRules++;
            }

            if (BlockRules > 0)
                return Firewall.Actions.Block;
            if (AllowRules > 0)
                return Firewall.Actions.Allow;
            return Firewall.Actions.Undefined;
        }

        public void Store(XmlWriter writer)
        {
            writer.WriteStartElement("program");

            foreach (ProgramList.ID id in IDs)
                id.Store(writer);

            writer.WriteElementString("Name", config.Name);
            if (config.Category != null && config.Category.Length > 0)
                writer.WriteElementString("Category", config.Category);
            if (config.Icon != null && config.Icon.Length > 0)
                writer.WriteElementString("Icon", config.Icon);

            if(config.NetAccess != Config.AccessLevels.Unconfigured)
                writer.WriteElementString("NetAccess", config.NetAccess.ToString());
            if(config.Notify != null)
                writer.WriteElementString("Notify", config.Notify.ToString());

            writer.WriteEndElement();
        }

        public bool Load(XmlNode entryNode)
        {
            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "id")
                {
                    ProgramList.ID id = new ProgramList.ID();
                    if (id.Load(node))
                        IDs.Add(id);
                }
                else if (node.Name == "Name")
                    config.Name = node.InnerText;
                else if (node.Name == "Category")
                    config.Category = node.InnerText;
                else if (node.Name == "Icon")
                    config.Icon = node.InnerText;
                else if (node.Name == "NetAccess")
                    Enum.TryParse(node.InnerText, out config.NetAccess);
                else if (node.Name == "Notify")
                    config.Notify = MiscFunc.parseBool(node.InnerText, null);
                else
                    AppLog.Line("Unknown Program Value, '{0}':{1}", node.Name, node.InnerText);
            }
            return IDs.Count > 0 && config.Name != null;
        }

        /*public bool HasActiveRules()
        {
            foreach (FirewallRule rule in Rules)
            {
                if (rule.Entry.Enabled == true)
                    return true;
            }
            return false;
        }*/

        [Serializable()]
        public class LogEntry
        {
            public Guid guid;
            public ProgramList.ID mID;
            public Firewall.Actions Action;
            public Firewall.Directions Direction;
            public string LocalAddress;
            public int LocalPort;
            public string RemoteAddress;
            public int RemotePort;
            public int Protocol;
            public int Profile = (int)Firewall.Profiles.Undefined;
            public int PID;
            public DateTime TimeStamp;

            public enum Types
            {
                Undefined = 0,
                FromLog,
                UnRuled,
                RuleAllowed,
                RuleBlocked,
                RuleError,
            }
            public Types Type = Types.Undefined;

            public LogEntry() { }

            public LogEntry(ProgramList.ID id, Firewall.Actions action, Firewall.Directions direction, string localAddress, int localPort, string remoteAddress, int remotePort, int protocol, int processId, DateTime timeStamp)
            {
                guid = Guid.NewGuid();

                mID = id;
                Action = action;
                Direction = direction;
                LocalAddress = localAddress;
                LocalPort = localPort;
                RemoteAddress = remoteAddress;
                RemotePort = remotePort;
                Protocol = protocol;
                PID = processId;
                TimeStamp = timeStamp;
            }
        }
    }
}
