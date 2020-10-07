using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PrivateService;
using PrivateAPI;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "ProgramSet", Namespace = "http://schemas.datacontract.org/")]
    public class ProgramSet
    {
        [DataMember()]
        public Guid guid;

        [DataMember()]
        public SortedDictionary<ProgramID, Program> Programs = new SortedDictionary<ProgramID, Program>();

        [Serializable()]
        [DataContract(Name = "ProgramConfig", Namespace = "http://schemas.datacontract.org/")]
        public class Config
        {
            [DataMember()]
            public string Name = "";
            [DataMember()]
            public string Category = "";
            [DataMember()]
            public string Icon = "";

            public enum AccessLevels
            {
                Unconfigured = 0,
                FullAccess,
                //OutBoundAccess,
                //InBoundAccess,
                CustomConfig,
                LocalOnly,
                BlockAccess,
                StopNotify,
                AnyValue,
                WarningState
            }

            [DataMember()]
            public bool? Notify = null;
            public bool? GetNotify() { return IsSilenced() ? (bool?)false : Notify; }
            public void SetNotify(bool? set) { SilenceUntill = 0; Notify = set; }
            [DataMember()]
            public UInt64 SilenceUntill = 0;
            public bool IsSilenced() { return SilenceUntill != 0 && SilenceUntill > MiscFunc.GetUTCTime(); }
            [DataMember()]
            public AccessLevels NetAccess = AccessLevels.Unconfigured;
            [DataMember()]
            public AccessLevels CurAccess = AccessLevels.Unconfigured;
            public AccessLevels GetAccess()
            {
                if (NetAccess == AccessLevels.Unconfigured)
                    return CurAccess;
                else
                    return NetAccess;
            }

            // Custom option
            // todo
        }

        [DataMember()]
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

                    Priv10Logger.LogInfo("CleanUp Removed program: {0}", prog.ID.FormatString());
                    Count++;
                }
            }
            return Count;
        }

        /////////////////////////////////////////////////////////////
        // merged data
        
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

            writer.WriteElementString("Guid", guid.ToString());

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
                    {
                        // COMPAT: merge "duplicates"
                        Program knownProg;
                        if (Programs.TryGetValue(prog.ID, out knownProg))
                        {
                            foreach (var rule in prog.Rules)
                                knownProg.Rules.Add(rule.Key, rule.Value);
                        }
                        else
                            prog.AssignSet(this);
                    }
                }
                else if (node.Name == "Guid")
                    guid = Guid.Parse(node.InnerText);
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

            if(Programs.Count > 0 && config.Name == null || config.Name.Substring(0,2) == "@{")
                config.Name = Programs.First().Value.Description;

            return Programs.Count > 0 && config.Name != null;
        }
    }
}
