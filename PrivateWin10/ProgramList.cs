using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    public class ProgramList
    {
        public enum Types
        {
            Global = 0,
            System,
            Service,
            Program,
            App
        }

        public class ChangeArgs : EventArgs
        {
            public Guid guid;
        }

        [Serializable()]
        public class ID : IComparable
        {
            public Types Type = Types.Program;
            public string Path; // process path
            public string Name; // service name or app package sid
            public string DescrStr;

            [OnDeserialized()]
            private void OnDeserializing(StreamingContext c)
            {
                if (Path == null)
                    Path = "";
                if (Name == null)
                    Name = "";

                if (App.engine != null)
                    MakeDisplayName();
            }

            public ID()
            {
            }

            public ID(Types type, string path = null, string name = null)
            {
                Type = type;
                Path = path ?? "";
                Name = name ?? "";

                MakeDisplayName();
            }

            private void MakeDisplayName()
            {
                switch (Type)
                {
                    case ProgramList.Types.System: DescrStr = Translate.fmt("name_system"); break;
                    case ProgramList.Types.Service: DescrStr = ServiceHelper.GetServiceName(Name); break;
                    case ProgramList.Types.Program: DescrStr = MiscFunc.GetExeName(Path); break;
                    case ProgramList.Types.App: DescrStr = App.engine != null ? App.engine.appMgr.GetAppName(Name) : ""; break;
                    default:
                    case ProgramList.Types.Global: DescrStr = Translate.fmt("name_global"); break;
                }
            }

            public string GetDisplayName(bool detailed = true)
            {
                if (DescrStr.Length == 0)
                    return System.IO.Path.GetFileName(Path);

                if (!detailed)
                    return DescrStr;

                switch (Type)
                {
                    case ProgramList.Types.Service: return DescrStr + " (" + Name + ")"; 
                    case ProgramList.Types.Program: return DescrStr + " (" + System.IO.Path.GetFileName(Path) + ")"; 
                    case ProgramList.Types.App: return DescrStr + " (" + AppManager.SidToPackageID(Name) + ")";
                    default: return DescrStr;
                }
            }

            public int CompareTo(object obj)
            {
                if ((int)Type > (int)(obj as ID).Type)
                    return 1;
                else if ((int)Type < (int)(obj as ID).Type)
                    return -1;

                int ret = String.Compare(Name, (obj as ID).Name, true); 
                if (ret != 0)
                    return ret;

                // Note: for services and rules if on one path is not set we ignore path all alltogether
                if (Type == Types.App || Type == Types.Service)
                {
                    if (Path.Length == 0 || (obj as ID).Path.Length == 0)
                        return 0;
                }

                return String.Compare(Path, (obj as ID).Path, true);
            }

            public void Store(XmlWriter writer)
            {
                writer.WriteStartElement("id");

                writer.WriteElementString("Type", Type.ToString());
                writer.WriteElementString("Path", Path);
                writer.WriteElementString("Name", Name);

                writer.WriteEndElement();
            }

            public bool Load(XmlNode idNode)
            {
                try
                {
                    Type = (Types)Enum.Parse(typeof(Types), idNode.SelectSingleNode("Type").InnerText);
                    Name = idNode.SelectSingleNode("Name").InnerText;
                    Path = idNode.SelectSingleNode("Path").InnerText;
                    MakeDisplayName();
                    return true;
                }
                catch{}
                return false;
            }

            public string AsString()
            {
                switch (Type)
                {
                    case Types.System: return Translate.fmt("name_system");
                    case Types.Service: return Translate.fmt("name_service", Path, Name);
                    case Types.Program: return Path;
                    case Types.App: return Translate.fmt("name_app", Path, Name);
                    default:
                    case Types.Global: return Translate.fmt("name_global");
                }
            }
        }

        public SortedDictionary<ID, Program> byID = new SortedDictionary<ID, Program>();
        public Dictionary<Guid, Program> Progs = new Dictionary<Guid, Program>();

        public int MaxLogLength = App.GetConfigInt("GUI", "LogLimit", 1000);

        public ProgramList()
        {
        }

        public List<Program> GetPrograms(List<Guid> guids = null)
        {
            if(guids == null || guids.Count == 0)
                return Progs.Values.ToList();

            List<Program> list = new List<Program>();
            foreach (Guid guid in guids)
            {
                Program prog;
                if (Progs.TryGetValue(guid, out prog))
                    list.Add(prog);
            }
            return list;
        }

        public bool LoadList()
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(App.appPath + @"\Programs.xml");
                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    if (node.Name != "program")
                        continue;
                    
                    Program entry = new Program();
                    if (!entry.Load(node))
                    {
                        AppLog.Line("Failed to load program entry {0}", entry.config.Name);
                        continue;
                    }

                    Progs.Add(entry.guid, entry);
                    foreach (ID id in entry.IDs)
                        try { byID.Add(id, entry); } catch { }
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Failed to load programlist, Error: {0}", err.Message);
                return false;
            }
            return true;
        }

        public int CleanUp()
        {
            App.engine.firewall.CleanUpRules(true);

            int Count = 0;
            foreach (Program program in Progs.Values.ToList())
            {
                if (program.Rules.Count == 0)
                {
                    AppLog.Line("Removing: {0}", program.config.Name);
                    Count++;

                    RemoveProgram(program.guid);
                }
            }
            return Count;
        }

        public void StoreList()
        {
            // Note: we have to filter out duplicate entries
            HashSet<Program> list = new HashSet<Program>();

            foreach (Program entry in byID.Values)
                list.Add(entry);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter writer = XmlWriter.Create(App.appPath + @"\Programs.xml", settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("programs");

            foreach (Program entry in list)
                entry.Store(writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Dispose();
        }

        static public string GetProcessPathById(int id)
        {
            try
            {
                Process proc = Process.GetProcessById(id);
                string path = proc.MainModule.FileName;
                if (path.Contains('~'))
                    return Path.GetFullPath(path);
                return path;
            }
            catch
            {
                return null;
            }
        }

        public Program GetProgram(ID id, bool canAdd = false)
        {
            Program prog = null;
            if (!byID.TryGetValue(id, out prog) && canAdd)
            {
                prog = new Program(id);
                Progs.Add(prog.guid, prog);
                byID.Add(id, prog);

                App.engine.NotifyChange(prog);
            }
            return prog;
        }

        public bool AddProgram(ProgramList.ID id, Guid guid)
        {
            if (byID.ContainsKey(id))
                return false; // already exist
            if (guid == Guid.Empty)
                GetProgram(id, true);
            else // add id to existing program
            {
                Program prog;
                if (!Progs.TryGetValue(guid, out prog))
                    return false;
                prog.IDs.Add(id);
                byID.Add(id, prog);
            }
            return true;
        }

        public bool UpdateProgram(Guid guid, Program.Config config)
        {
            Program prog;
            if (!Progs.TryGetValue(guid, out prog))
                return false;
            prog.config = config;

            App.engine.firewall.EvaluateRules(prog, true);

            App.engine.NotifyChange(prog);

            return true;
        }

        static public bool IsSpecialProgram(Program prog)
        {
            ID id = prog.IDs.First();
            if (id.Type == Types.System || id.Type == Types.Global)
                return true;
            return false;
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            Program from_prog;
            if (!Progs.TryGetValue(from, out from_prog) || IsSpecialProgram(from_prog))
                return false;

            Program to_prog;
            if (!Progs.TryGetValue(to, out to_prog) || IsSpecialProgram(to_prog))
                return false;

            foreach (ID id in from_prog.IDs)
            {
                byID.Remove(id);
                byID.Add(id, to_prog);
                to_prog.IDs.Add(id);
            }

            bool test = Progs.Remove(from_prog.guid);

            foreach (FirewallRule rule in from_prog.Rules.Values)
                to_prog.Rules.Add(rule.guid, rule);
            from_prog.Rules.Clear();

            foreach (Program.LogEntry entry in from_prog.Log)
                to_prog.Log.Add(entry);
            from_prog.Log.Clear();

            App.engine.firewall.EvaluateRules(to_prog, false);

            App.engine.NotifyChange(null);

            return true;
        }

        public bool SplitPrograms(Guid from, ID mID)
        {
            return SplitPrograms(from, mID, true);
        }

        private bool SplitPrograms(Guid from, ID mID, bool addNew)
        {
            Program from_prog;
            if (!Progs.TryGetValue(from, out from_prog) || IsSpecialProgram(from_prog))
                return false;

            from_prog.IDs.RemoveWhere((id) => { return id.CompareTo(mID) == 0; });
            byID.Remove(mID);

            Program to_prog = null;
            if (addNew)
            {
                to_prog = GetProgram(mID, true);
                to_prog.config.Category = from_prog.config.Category;
                to_prog.config.NetAccess = from_prog.config.NetAccess;
                to_prog.config.Notify = from_prog.config.Notify;
            }

            foreach (FirewallRule rule in from_prog.Rules.Values.ToList())
            {
                if (rule.mID.CompareTo(mID) == 0)
                {
                    from_prog.Rules.Remove(rule.guid);
                    if(to_prog != null)
                        to_prog.Rules.Add(rule.guid, rule);
                    else
                        App.engine.firewall.RemoveRule(rule, true);
                }
            }

            foreach (Program.LogEntry entry in from_prog.Log.ToList())
            {
                if (entry.mID.CompareTo(mID) == 0)
                {
                    from_prog.Log.Remove(entry);
                    if (to_prog != null)
                        to_prog.Log.Add(entry);
                }
            }

            App.engine.NotifyChange(null);

            return true;
        }

        public bool RemoveProgram(Guid guid, ProgramList.ID id = null)
        {
            if (id != null)
                return SplitPrograms(guid, id, false);

            Program prog;
            if (!Progs.TryGetValue(guid, out prog) || IsSpecialProgram(prog))
                return true; // already gone - or cant be removed

            Progs.Remove(guid);
            foreach (ID _id in prog.IDs)
                byID.Remove(_id);

            foreach (FirewallRule rule in prog.Rules.Values.ToList())
                App.engine.firewall.RemoveRule(rule, true);

            App.engine.NotifyChange(null);

            return true;
        }
    }
}
