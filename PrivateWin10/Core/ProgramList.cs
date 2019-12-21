using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    public class ProgramList
    {
        public static int MaxLogLength = App.GetConfigInt("GUI", "LogLimit", 1000);

        public SortedDictionary<ProgramID, Program> Programs = new SortedDictionary<ProgramID, Program>();

        public Dictionary<Guid, ProgramSet> ProgramSets = new Dictionary<Guid, ProgramSet>();


        public ProgramList()
        {
         
        }

        private Program AddProgram(ProgramID progID)
        {
            Program prog = new Program(progID);
            Programs.Add(progID, prog);

            ProgramSet progs = new ProgramSet(prog);
            ProgramSets.Add(progs.guid, progs);

            Changed?.Invoke(this, new ListEvent() { guid = progs.guid });

            return prog;
        }

        public enum FuzzyModes : int
        {
            No = 0,
            Tag = 1,
            Path = 2,
            Any = 3
        };

        public static T GetProgramFuzzy<T>(SortedDictionary<ProgramID, T> Programs, ProgramID progID, FuzzyModes fuzzyMode) where T : class
        {
            T prog = null;
            if (Programs.TryGetValue(progID, out prog))
                return prog;

            // Only works for services and apps 
            if (!(progID.Type == ProgramID.Types.Service || progID.Type == ProgramID.Types.App))
                return null;

            if ((fuzzyMode & FuzzyModes.Tag) != 0 && progID.Aux.Length > 0)
            {
                // first drop path and try to get by serviceTag or application SID
                ProgramID auxId = ProgramID.New(progID.Type, null, progID.Aux);
                if (Programs.TryGetValue(auxId, out prog))
                    return prog;
            }

            if ((fuzzyMode & FuzzyModes.Path) != 0 && progID.Path.Length > 0
             && (progID.Type == ProgramID.Types.Service || progID.Type == ProgramID.Types.App)
             && System.IO.Path.GetFileName(progID.Path).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase) == false) // dont use this for svchost.exe
            {
                // than try to get an entry by path only
                ProgramID pathId = ProgramID.New(ProgramID.Types.Program, progID.Path, null);
                if (Programs.TryGetValue(pathId, out prog))
                    return prog;
            }

            return null;
        }

        public Program FindProgram(ProgramID progID, bool canAdd = false, FuzzyModes fuzzyMode = FuzzyModes.No)
        {
            Program prog = GetProgramFuzzy(Programs, progID, fuzzyMode);
            
            if (prog == null && canAdd)
                prog = AddProgram(progID);

            return prog;
        }

        public Program GetProgram(ProgramID progID, bool canAdd = false)
        {
            Program prog;
            if (!Programs.TryGetValue(progID, out prog) && canAdd)
                prog = AddProgram(progID);
            return prog;
        }

        public List<ProgramSet> GetPrograms(List<Guid> guids = null)
        {
            if (guids == null || guids.Count == 0)
                return ProgramSets.Values.ToList();

            List<ProgramSet> setList = new List<ProgramSet>();
            foreach (Guid guid in guids)
            {
                ProgramSet progSet;
                if (ProgramSets.TryGetValue(guid, out progSet))
                    setList.Add(progSet);
            }
            return setList;
        }

        public bool AddProgram(ProgramID progID, Guid guid)
        {
            if (Programs.ContainsKey(progID))
                return false; // already exist

            if (guid == Guid.Empty)
            {
                AddProgram(progID);
            }
            else // add id to existing programSet
            {
                ProgramSet progs;
                if (!ProgramSets.TryGetValue(guid, out progs))
                    return false;

                Program prog = new Program(progID);
                Programs.Add(progID, prog);
                prog.AssignSet(progs);

                Changed?.Invoke(this, new ListEvent() { guid = progs.guid });
            }
            return true;
        }

        public bool UpdateProgram(Guid guid, ProgramSet.Config config, UInt64 expiration = 0)
        {
            ProgramSet progs;
            if (!ProgramSets.TryGetValue(guid, out progs))
                return false;
            progs.config = config;

            App.engine.FirewallManager.ApplyRules(progs, expiration);

            Changed?.Invoke(this, new ListEvent() { guid = progs.guid });

            return true;
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            ProgramSet from_prog;
            if (!ProgramSets.TryGetValue(from, out from_prog) || from_prog.IsSpecial())
                return false;

            ProgramSet to_prog;
            if (!ProgramSets.TryGetValue(to, out to_prog) || to_prog.IsSpecial())
                return false;

            foreach (Program prog in from_prog.Programs.Values.ToList())
                prog.AssignSet(to_prog);

            ProgramSets.Remove(from);

            App.engine.FirewallManager.EvaluateRules(to_prog);

            Changed?.Invoke(this, new ListEvent());

            return true;
        }

        public void UpdatePrograms()
        {
            foreach (ProgramSet progSet in ProgramSets.Values)
            {
                if(progSet.UpdateSet())
                    Changed?.Invoke(this, new ListEvent() { guid = progSet.guid });
            }
        }

        public bool SplitPrograms(Guid from, ProgramID progID)
        {
            ProgramSet from_prog;
            if (!ProgramSets.TryGetValue(from, out from_prog) || from_prog.IsSpecial())
                return false;

            if (from_prog.Programs.Count == 1)
                return true; // nothing to do

            Program prog = null;
            if(!from_prog.Programs.TryGetValue(progID, out prog))
                return true; // no found

            ProgramSet to_prog = new ProgramSet(prog); // prog.AssignSet taked care of the internal associaltions
            to_prog.config.Category = from_prog.config.Category;
            ProgramSets.Add(to_prog.guid, to_prog);

            App.engine.FirewallManager.EvaluateRules(from_prog);
            App.engine.FirewallManager.EvaluateRules(to_prog);

            Changed?.Invoke(this, new ListEvent());

            return true;
        }

        public bool RemoveProgram(Guid guid, ProgramID id = null)
        {
            ProgramSet progs = null;
            if (!ProgramSets.TryGetValue(guid, out progs) || progs.IsSpecial())
                return false; // already gone or can not be removed

            List<ProgramID> IDs = new List<ProgramID>();
            if (id != null)
                IDs.Add(id);
            else
                IDs = progs.Programs.Keys.ToList();

            foreach (ProgramID progID in IDs)
            {
                Program prog;
                if (!Programs.TryGetValue(progID, out prog))
                    continue; // already gone

                progs.Programs.Remove(progID);

                Programs.Remove(progID);

                foreach (FirewallRule rule in prog.Rules.Values)
                    App.engine.FirewallManager.RemoveRule(rule.guid);

                foreach (NetworkSocket socket in prog.Sockets.Values)
                    socket.Program = null;
            }

            if (progs.Programs.Count == 0)
                ProgramSets.Remove(guid);

            Changed?.Invoke(this, new ListEvent());

            return true;
        }

        public void ClearLog()
        {
            foreach (Program process in Programs.Values)
                process.Log.Clear();
        }

        public void ClearDnsLog()
        {
            foreach (Program process in Programs.Values)
                process.DnsLog.Clear();
        }

        
        public int CleanUp(bool ExtendedCleanup = false)
        {
            int Count = 0;
            foreach (ProgramSet progSet in ProgramSets.Values.ToList())
            {
                Count += progSet.CleanUp(ExtendedCleanup);

                if (progSet.Programs.Count == 0)
                    ProgramSets.Remove(progSet.guid);
            }

            if(Count > 0)
                Changed?.Invoke(this, new ListEvent());
            return Count;
        }

        static double xmlVersion = 1;

        public bool Load()
        {
            if (!File.Exists(App.dataPath + @"\Programs.xml"))
                return false;

            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(App.dataPath + @"\Programs.xml");

                double fileVersion = 0.0;
                double.TryParse(xDoc.DocumentElement.GetAttribute("Version"), out fileVersion);
                if (fileVersion != xmlVersion)
                {
                    App.LogError("Failed to load programlist, unknown file version {0}, expected {1}", fileVersion, xmlVersion);
                    return false;
                }

                int TotalCount = 0;
                int ErrorCount = 0;

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    TotalCount++;
                    ProgramSet entry = new ProgramSet();
                    if (!entry.LoadSet(node))
                    {
                        ErrorCount++;
                        continue;
                    }

                    foreach (Program prog in entry.Programs.Values.ToList())
                    {
                        // COMPAT: merge "duplicates"
                        Program knownProg;
                        if (App.engine.ProgramList.Programs.TryGetValue(prog.ID, out knownProg))
                        {
                            foreach (var rule in prog.Rules)
                                knownProg.Rules.Add(rule.Key, rule.Value);

                            entry.Programs.Remove(prog.ID);
                        }
                        else
                            Programs.Add(prog.ID, prog);
                    }

                    if(entry.Programs.Count > 0)
                        ProgramSets.Add(entry.guid, entry);
                }

                if (ErrorCount != 0)
                    App.LogError("Failed to load {0} program entry out of {1}", ErrorCount, TotalCount);
                App.LogInfo("ProgramList loaded {0} entries", TotalCount - ErrorCount);
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            return true;
        }

        public void Store()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter writer = XmlWriter.Create(App.dataPath + @"\Programs.xml", settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("ProgramList");
            writer.WriteAttributeString("Version", xmlVersion.ToString());

            foreach (ProgramSet entry in ProgramSets.Values)
                entry.StoreSet(writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Dispose();
        }

        [Serializable()]
        public class ListEvent : EventArgs
        {
            public Guid guid;
        }

        public event EventHandler<ListEvent> Changed;
    }
}
