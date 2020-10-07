using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using System.Xml;
using MiscHelpers;

namespace TweakEngine
{
    public class TweakList
    {
        public Dictionary<string, TweakPresets.Category> Categorys = new Dictionary<string, TweakPresets.Category>();

        public IEnumerable<Tweak> GetAllTweaks()
        {
            foreach (TweakPresets.Group group in GetAllGroups())
            {
                foreach (Tweak tweak in group.Tweaks.Values)
                {
                    yield return tweak;
                }
            }   
        }

        public IEnumerable<TweakPresets.Group> GetAllGroups()
        {
            foreach (TweakPresets.Category category in Categorys.Values)
            {
                foreach (TweakPresets.Group group in category.Groups.Values)
                {
                    yield return group;
                }
            }
        }

        //public Dictionary<Guid, Tweak> TweakList = new Dictionary<Guid, Tweak>();
        

        public TweakList()
        {
            TweakPresets.InitTweaks(Categorys);
            
            /*foreach (TweakStore.Category category in Categorys.Values)
            {
                foreach (TweakStore.Group group in category.Groups.Values)
                {
                    foreach (Tweak tweak in group.Tweaks.Values)
                    {
                        TweakList.Add(tweak.guid, tweak);
                    }
                }
            }*/
        }
        
/*public Dictionary<Guid, TweakManager.Tweak> GetTweaks(List<Guid> guids = null)
{
    if (guids != null)
    {
        Dictionary<Guid, TweakManager.Tweak> tweaks = new Dictionary<Guid, TweakManager.Tweak>();
        foreach (Guid guid in guids)
        {
            TweakManager.Tweak tweak;
            if (TweakList.TryGetValue(guid, out tweak))
                tweaks.Add(guid, tweak);
        }
        return tweaks;
    }
    return TweakList;
}*/

        public TweakPresets.Group GetGroup(string tweakGroup)
        {
            foreach (TweakPresets.Category category in Categorys.Values)
            {
                foreach (TweakPresets.Group group in category.Groups.Values)
                {
                    if (group.Name.Equals(tweakGroup, StringComparison.OrdinalIgnoreCase))
                        return group;
                }
            }
            return null;
        }

        public Tweak GetTweak(string tweakName)
        {
            foreach (TweakPresets.Category category in Categorys.Values)
            {
                foreach (TweakPresets.Group group in category.Groups.Values)
                {
                    foreach (Tweak tweak in group.Tweaks.Values)
                    {
                        if (tweak.Name.Equals(tweakName, StringComparison.OrdinalIgnoreCase))
                            return tweak;
                    }
                }
            }  
            return null;  
        }

        // tweak storage
        static double xmlVersion = 1.2;

        public void Load(string FilePath)
        {
            if (!Load(FilePath, Categorys))
            {
                foreach (Tweak tweak in GetAllTweaks())
                    tweak.State = TweakTools.TestTweak(tweak) ? Tweak.States.SelGroupe : Tweak.States.Unsellected;

                Store(FilePath);
            }
        }

        public bool Load(string FilePath, Dictionary<string, TweakPresets.Category> Categorys)
        {
            if (!File.Exists(FilePath))
                return false;

            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(FilePath);

                double fileVersion = 0.0;
                double.TryParse(xDoc.DocumentElement.GetAttribute("Version"), out fileVersion);
#if false
                if (fileVersion != xmlVersion)
                {
                    if (fileVersion != 0 && fileVersion < xmlVersion)
                    {
                        FileOps.MoveFile(FilePath, App.dataPath + @"\Tweaks_old.xml", true);
                        Priv10Logger.LogWarning(App.EventIDs.AppWarning, null, App.EventFlags.Notifications, Translate.fmt("msg_tweaks_updated", App.dataPath + @"\Tweaks_old.xml"));
                    }
                    else 
                        Priv10Logger.LogError("Failed to load tweaklist, unknown file version {0}, expected {1}", fileVersion, xmlVersion);
                    return false;
                }
#endif

                int TotalCount = 0;
                int ErrorCount = 0;

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    TotalCount++;
                    Tweak tweak = new Tweak();
                    if (!tweak.Load(node))
                    {
                        ErrorCount++;
                        continue;
                    }
                    
                    TweakPresets.Category tweak_cat;
                    if (!Categorys.TryGetValue(tweak.Category, out tweak_cat))
                    {
                        tweak_cat = new TweakPresets.Category(tweak.Category);
                        Categorys.Add(tweak.Category, tweak_cat);
                    }

                    tweak_cat.Add(tweak);
                }

#if false
                if (ErrorCount != 0)
                    Priv10Logger.LogError("Failed to load {0} tweak entries out of {1}", ErrorCount, TotalCount);
                Priv10Logger.LogInfo("TweakManager loaded {0} entries", TotalCount - ErrorCount);
#endif
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            return true;
        }

        public void Store(string FilePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter writer = XmlWriter.Create(FilePath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("TweakList");
            writer.WriteAttributeString("Version", xmlVersion.ToString());

            //foreach (Tweak tweak in TweakList.Values)
            foreach (Tweak tweak in GetAllTweaks())
                tweak.Store(writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Dispose();
        }


        public enum TweakType
        {
            None = 0,
            SetRegistry,
            SetGPO,
            DisableService,
            DisableTask,
            BlockFile
            //UseFirewall
        }

        [Serializable()]
        public class Tweak
        {
            public Guid guid = Guid.NewGuid();

            // Description
            public string Category = "";
            public string Group = "";
            public string Name = "";

            // Tweak Details
            public TweakType Type;
            public string Key;
            public string Path;
            public bool usrLevel = false;
            public object Value;

            // Aplicable windows versions restrictions
            public WinVer winVer = null;

            public enum Hints : int
            {
                None = 0,
                Recommended = 1,
                Optional = 2
            }
            public Hints Hint = Hints.None;

            public enum States : int
            {
                Unsellected = 0,
                Sellected, // explicitly enabled
                SelGroupe // enabled as part of a tweak groupe
            }
            public States State = States.Unsellected;

            public bool? Status;
            public bool FixFailed = false;
            public int FixedCount = 0;
            public DateTime LastChangeTime = DateTime.MinValue;


            //[field: NonSerialized]
            //public event EventHandler<EventArgs> StatusChanged;

            public Tweak()
            {
                //guid = Guid.NewGuid();
            }

            public Tweak(string lebel, TweakType type, WinVer ver)
            {
                //guid = Guid.NewGuid();

                Name = lebel;
                Type = type;
                winVer = ver;
            }

            public bool IsAvailable()
            {
                switch (Type)
                {
                    case TweakType.DisableService: return ServiceHelper.GetServiceState(Key) != ServiceHelper.ServiceState.NotFound;
                    case TweakType.DisableTask: return TweakTools.IsTaskPresent(Path, Key);
                    case TweakType.BlockFile:
                    {
                        string FullPath = Environment.ExpandEnvironmentVariables(Path);
                        bool ret = File.Exists(FullPath);
                        return ret;
                    }
                        
                }
                return winVer.TestHost();
            }

            public void Store(XmlWriter writer)
            {
                writer.WriteStartElement("Tweak");

                //writer.WriteElementString("Guid", guid.ToString());

                writer.WriteElementString("Category", Category);
                writer.WriteElementString("Group", Group);
                writer.WriteElementString("Name", Name);

                writer.WriteElementString("Type", Type.ToString());
                writer.WriteElementString("Path", Path);
                writer.WriteElementString("Key", Key);
                if(usrLevel)
                    writer.WriteElementString("Level", "User");
                else
                    writer.WriteElementString("Level", "Admin");

                if (Value != null)
                {
                    if (Value.GetType() == typeof(int))
                        writer.WriteElementString("ValueInt", Value.ToString());
                    else if (Value.GetType() == typeof(Int64))
                        writer.WriteElementString("ValueU64", Value.ToString());
                    else //if (Value.GetType() == typeof(string))
                        writer.WriteElementString("Value", Value.ToString());
                }

                writer.WriteElementString("Platform", winVer.AsString());

                writer.WriteElementString("Hint", Hint.ToString());
                writer.WriteElementString("State", State.ToString());

                writer.WriteElementString("FixFailed", FixFailed.ToString());
                writer.WriteElementString("FixedCount", FixedCount.ToString());
                writer.WriteElementString("LastChange", LastChangeTime.ToString());

                writer.WriteEndElement();
            }

            public bool Load(XmlNode tweakNode)
            {
                if(tweakNode.Name != "Tweak")
                    return false;

                try
                {
                    foreach (XmlNode node in tweakNode.ChildNodes)
                    {
                        /*if (node.Name == "Guid")
                            guid = Guid.Parse(node.InnerText);
                        else*/ if (node.Name == "Category")
                            Category = node.InnerText;
                        else if (node.Name == "Group")
                            Group = node.InnerText;
                        else if (node.Name == "Name")
                            Name = node.InnerText;

                        else if (node.Name == "Type")
                            Type = (TweakType)Enum.Parse(typeof(TweakType), node.InnerText);
                        else if (node.Name == "Path")
                            Path = node.InnerText;
                        else if (node.Name == "Key")
                            Key = node.InnerText;
                        else if (node.Name == "Level")
                            usrLevel = node.InnerText.Equals("User");

                        else if (node.Name == "ValueInt")
                            Value = int.Parse(node.InnerText);
                        else if (node.Name == "ValueU64")
                            Value = UInt64.Parse(node.InnerText);
                        else if (node.Name == "Value")
                            Value = node.InnerText;

                        else if (node.Name == "Platform")
                            winVer = WinVer.Parse(node.InnerText);

                        else if (node.Name == "Hint")
                            Hint = (Hints)Enum.Parse(typeof(Hints), node.InnerText);
                        else if (node.Name == "State")
                            State = (States)Enum.Parse(typeof(States), node.InnerText);

                        else if (node.Name == "FixFailed")
                            bool.TryParse(node.InnerText, out FixFailed);
                        else if (node.Name == "FixedCount")
                            int.TryParse(node.InnerText, out FixedCount);
                        else if (node.Name == "LastChange")
                            DateTime.TryParse(node.InnerText, out LastChangeTime);

                    }
                }
                catch
                {
                    return false;
                }

                return Name != null && Type != TweakType.None && winVer != null;
            }
        }
    }
}
