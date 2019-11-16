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
    public class ProgramSet
    {
        public Guid guid;

        public SortedDictionary<ProgramID, Program> Programs = new SortedDictionary<ProgramID, Program>();

        public int EnabledRules = 0;
        public int DisabledRules = 0;
        public int ChgedRules = 0;

        public int SocketsWeb = 0;
        public int SocketsTcp = 0;
        public int SocketsSrv = 0;
        public int SocketsUdp = 0;

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
                StopNotify,
                AnyValue,
                WarningState
            }

            public bool? Notify = null;
            public bool? GetNotify() { return IsSilenced() ? (bool?)false : Notify; }
            public void SetNotify(bool? set) { SilenceUntill = 0; Notify = set; }
            public UInt64 SilenceUntill = 0;
            public bool IsSilenced() { return SilenceUntill != 0 && SilenceUntill > MiscFunc.GetUTCTime(); }
            public AccessLevels NetAccess = AccessLevels.Unconfigured;
            public AccessLevels CurAccess = AccessLevels.Unconfigured;

            // Custom option
            // todo
        }

        public Config config = new Config();

        public ProgramSet()
        {
            guid = Guid.NewGuid();
        }

        public ProgramSet(Program prog)
        {
            guid = Guid.NewGuid();
            prog.AssignSet(this);

            config.Name = prog.Description;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext c)
        {
            EnabledRules = 0;
            DisabledRules = 0;
            ChgedRules = 0;
            foreach (Program prog in Programs.Values)
            {
                foreach (FirewallRuleEx rule in prog.Rules.Values)
                {
                    if(rule.Enabled)
                        EnabledRules++;
                    else
                        DisabledRules++;
                    if (rule.State != FirewallRuleEx.States.Approved)
                        ChgedRules++;
                }
            }

            SocketsWeb = 0;
            SocketsTcp = 0;
            SocketsUdp = 0;
            SocketsSrv = 0;
            foreach (Program prog in Programs.Values)
            {
                foreach (NetworkSocket entry in prog.Sockets.Values)
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
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            
        }
        
        public string GetIcon()
        {
            if (config.Icon != null && config.Icon.Length > 0)
                return config.Icon;
            return Programs.First().Key.Path;
        }

        public bool IsSpecial()
        {
            foreach (Program prog in Programs.Values)
            {
                if (prog.IsSpecial())
                    return true;
            }
            return false;
        }

        public bool UpdateSet()
        {
            bool bRet = false;
            foreach (Program prog in Programs.Values)
            {
                if (prog.Update())
                    bRet = true;
            }
            return bRet;
        }

        public int CleanUp(bool ExtendedCleanup = false)
        {
            int Count = 0;
            foreach (Program prog in Programs.Values.ToList())
            {
                bool Remove = !prog.Exists();
                if (ExtendedCleanup && prog.Rules.Count == 0 && prog.Sockets.Count == 0)
                    Remove = true;

                if (Remove)
                {
                    // remove all rules for this program, if there are any
                    foreach (var guid in prog.Rules.Keys.ToList())
                        App.engine.FirewallManager.RemoveRule(guid);

                    Programs.Remove(prog.ID);

                    App.LogInfo("CleanUp Removed program: {0}", prog.ID.FormatString());
                    Count++;
                }
            }
            return Count;
        }

        /////////////////////////////////////////////////////////////
        // merged data

        public DateTime GetLastActivity(bool Allowed = true, bool Blocked = true)
        {
            DateTime lastActivity = DateTime.MinValue;
            foreach (Program prog in Programs.Values)
            {
                if (Allowed && prog.lastAllowed > lastActivity)
                    lastActivity = prog.lastAllowed;
                if (Blocked && prog.lastBlocked > lastActivity)
                    lastActivity = prog.lastBlocked;
            }
            return lastActivity;
        }

        public UInt64 GetDataRate()
        {
            UInt64 DataRate = 0;
            foreach (Program prog in Programs.Values)
                DataRate += prog.UploadRate + prog.DownloadRate;
            return DataRate;
        }

        public int GetSocketCount()
        {
            int SocketCount = 0;
            foreach (Program prog in Programs.Values)
                SocketCount += prog.SocketCount;
            return SocketCount;
        }

        public List<FirewallRuleEx> GetRules()
        {
            List<FirewallRuleEx> list = new List<FirewallRuleEx>();
            foreach (Program prog in Programs.Values)
            {
                foreach (FirewallRuleEx rule in prog.Rules.Values)
                    list.Add(rule);
            }
            return list;
        }

        public List<Program.LogEntry> GetConnections()
        {
            List<Program.LogEntry> list = new List<Program.LogEntry>();
            foreach (Program prog in Programs.Values)
            {
                foreach (Program.LogEntry entry in prog.Log)
                    list.Add(entry);
            }
            return list;
        }

        public List<NetworkSocket> GetSockets()
        {
            List<NetworkSocket> list = new List<NetworkSocket>();
            foreach (Program prog in Programs.Values)
            {
                foreach (NetworkSocket entry in prog.Sockets.Values)
                    list.Add(entry);
            }
            return list;
        }

        public List<Program.DnsEntry> GetDomains()
        {
            List<Program.DnsEntry> list = new List<Program.DnsEntry>();
            foreach (Program prog in Programs.Values)
            {
                foreach (Program.DnsEntry entry in prog.DnsLog.Values)
                    list.Add(entry);
            }
            return list;
        }

        /////////////////////////////////////////////////////////////
        ///

        public void StoreSet(XmlWriter writer)
        {
            writer.WriteStartElement("ProgramSet");

            writer.WriteElementString("Name", config.Name);
            if (config.Category != null && config.Category.Length > 0)
                writer.WriteElementString("Category", config.Category);
            if (config.Icon != null && config.Icon.Length > 0)
                writer.WriteElementString("Icon", config.Icon);

            if (config.NetAccess != Config.AccessLevels.Unconfigured)
                writer.WriteElementString("NetAccess", config.NetAccess.ToString());
            if (config.Notify != null)
                writer.WriteElementString("Notify", config.Notify.ToString());

            foreach (Program prog in Programs.Values)
                prog.Store(writer);

            writer.WriteEndElement();
        }

        public bool LoadSet(XmlNode entryNode)
        {
            if (entryNode.Name != "ProgramSet")
                return false;

            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "Program")
                {
                    Program prog = new Program();
                    if (prog.Load(node))
                        prog.AssignSet(this);
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
                    AppLog.Debug("Unknown Program Value, '{0}':{1}", node.Name, node.InnerText);
            }
            return Programs.Count > 0 && config.Name != null;
        }
    }
}
