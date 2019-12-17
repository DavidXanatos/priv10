using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using TaskScheduler;
using LocalPolicy;
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Windows.Threading;

namespace PrivateWin10
{
    public class TweakManager
    {
        public Dictionary<string, TweakStore.Category> Categorys = new Dictionary<string, TweakStore.Category>();

        public IEnumerable<Tweak> GetAllTweaks()
        {
            foreach (TweakStore.Category category in Categorys.Values)
            {
                foreach (TweakStore.Group group in category.Groups.Values)
                {
                    foreach (Tweak tweak in group.Tweaks.Values)
                    {
                        yield return tweak;
                    }
                }
            }
        }

        //public Dictionary<Guid, Tweak> TweakList = new Dictionary<Guid, Tweak>();

        DispatcherTimer Timer;
        UInt64 NextTweakCheck = 0;
        UInt64 LastSaveTime = MiscFunc.GetTickCount64();

        public TweakManager()
        {
            TweakStore.InitTweaks(Categorys);

            if (!Load(Categorys))
            {
                foreach (Tweak tweak in GetAllTweaks())
                    tweak.State = TweakEngine.TestTweak(tweak) ? Tweak.States.SelGroupe : Tweak.States.Unsellected;

                Store();
            }

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


            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(OnTimerTick);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            Timer.Start();

            NextTweakCheck = 0;
        }

        protected void OnTimerTick(object sender, EventArgs e)
        {
            if (NextTweakCheck <= MiscFunc.GetCurTick())
            {
                NextTweakCheck = MiscFunc.GetCurTick() + (UInt64)App.GetConfigInt("TweakGuard", "CheckInterval", 15 * 60) * 1000;

                if (App.GetConfigInt("TweakGuard", "AutoCheck", 1) != 0)
                    TestTweaks(false, App.GetConfigInt("TweakGuard", "AutoFix", 0) != 0);
            }


            if (MiscFunc.GetTickCount64() - LastSaveTime > 15 * 60 * 1000) // every 15 minutes
            {
                LastSaveTime = MiscFunc.GetTickCount64();
                Store();
            }
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

        public bool ApplyTweak(Tweak tweak)
        {
            if (!tweak.IsAvailable())
                return false;

            bool success;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel)
                success = TweakEngine.ApplyTweak(tweak);
            else
                success = App.client.ApplyTweak(tweak);

            //StatusChanged?.Invoke(this, new EventArgs());
            return success;
        }

        public bool UndoTweak(Tweak tweak)
        {
            if (!tweak.IsAvailable())
                return false;

            bool success;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel)
                success = TweakEngine.UndoTweak(tweak);
            else
                success = App.client.UndoTweak(tweak);
            //StatusChanged?.Invoke(this, new EventArgs());
            return success;
        }

        public void TestTweaks(bool bAll = true, bool fixChanged = false)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            //foreach (Tweak tweak in TweakList.Values)
            foreach (Tweak tweak in GetAllTweaks())
            {
                if(bAll || tweak.State != Tweak.States.Unsellected)
                    TestTweak(tweak, fixChanged);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("TestAllTweaks took: " + elapsedMs + "ms");
        }

        public bool TestTweak(Tweak tweak, bool fixChanged = false)
        {
            if (!tweak.IsAvailable())
                return false;

            bool status;
            if (AdminFunc.IsAdministrator() || tweak.usrLevel || !App.client.IsConnected())
                status = TweakEngine.TestTweak(tweak);
            else
                status = App.client.TestTweak(tweak);

            if (tweak.Status != status)
            {
                tweak.Status = status;
                tweak.LastChangeTime = DateTime.Now;

                Dictionary<string, string> Params = new Dictionary<string, string>();
                Params.Add("Name", tweak.Name);
                Params.Add("Group", tweak.Group);
                Params.Add("Category", tweak.Category);

                if (tweak.Status == false && tweak.State != Tweak.States.Unsellected)
                {
                    if (fixChanged == true && tweak.FixFailed == false)
                    {
                        ApplyTweak(tweak);

                        if (TestTweak(tweak, false) != true)
                        {
                            tweak.FixFailed = true;
                            App.LogError(App.EventIDs.TweakError, Params, App.EventFlags.Notifications, Translate.fmt("msg_tweak_stuck", tweak.Name, tweak.Group));
                        }
                        else
                        {
                            tweak.FixedCount++;
                            App.LogInfo(App.EventIDs.TweakFixed, Params, App.EventFlags.Notifications, Translate.fmt("msg_tweak_fixed", tweak.Name, tweak.Group));
                        }
                    }
                    else
                    {
                        App.LogWarning(App.EventIDs.TweakChanged, Params, App.EventFlags.Notifications, Translate.fmt("msg_tweak_un_done", tweak.Name, tweak.Group));
                    }
                }
            }
            return status;
        }

        public static bool HasAdministrator()
        {
            return AdminFunc.IsAdministrator() || (App.engine == null && App.client.IsConnected());
        }

        // tweak storage
        static double xmlVersion = 1.2;

        public bool Load(Dictionary<string, TweakStore.Category> Categorys)
        {
            if (!File.Exists(App.dataPath + @"\Tweaks.xml"))
                return false;

            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(App.dataPath + @"\Tweaks.xml");

                double fileVersion = 0.0;
                double.TryParse(xDoc.DocumentElement.GetAttribute("Version"), out fileVersion);
                if (fileVersion != xmlVersion)
                {
                    if (fileVersion != 0 && fileVersion < xmlVersion)
                    {
                        FileOps.MoveFile(App.dataPath + @"\Tweaks.xml", App.dataPath + @"\Tweaks_old.xml", true);
                        App.LogWarning(App.EventIDs.AppWarning, null, App.EventFlags.Notifications, Translate.fmt("msg_tweaks_updated", App.dataPath + @"\Tweaks_old.xml"));
                    }
                    else 
                        App.LogError("Failed to load tweaklist, unknown file version {0}, expected {1}", fileVersion, xmlVersion);
                    return false;
                }

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
                    
                    TweakStore.Category tweak_cat;
                    if (!Categorys.TryGetValue(tweak.Category, out tweak_cat))
                    {
                        tweak_cat = new TweakStore.Category(tweak.Category);
                        Categorys.Add(tweak.Category, tweak_cat);
                    }

                    tweak_cat.Add(tweak);
                }

                if (ErrorCount != 0)
                    App.LogError("Failed to load {0} tweak entries out of {1}", ErrorCount, TotalCount);
                App.LogInfo("TweakManager loaded {0} entries", TotalCount - ErrorCount);
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
            XmlWriter writer = XmlWriter.Create(App.dataPath + @"\Tweaks.xml", settings);

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
            //public Guid guid;

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
                    case TweakType.DisableTask: return TweakEngine.IsTaskPresent(Path, Key);
                    case TweakType.BlockFile:
                    {
                        string FullPath = Environment.ExpandEnvironmentVariables(Path);
                        bool ret = File.Exists(FullPath);
                        return ret;
                    }
                        
                }
                return winVer.TestHost();
            }

            public static string GetTypeStr(TweakType type)
            {
                switch (type)
                {
                    case TweakType.SetRegistry: return Translate.fmt("tweak_reg");
                    case TweakType.SetGPO: return Translate.fmt("tweak_gpo");
                    case TweakType.DisableService: return Translate.fmt("tweak_svc");
                    case TweakType.DisableTask: return Translate.fmt("tweak_task");
                    case TweakType.BlockFile: return Translate.fmt("tweak_file");
                        //case TweakType.UseFirewall:     return Translate.fmt("tweak_fw");
                }
                return Translate.fmt("txt_unknown");
            }

            public bool Apply(bool? byUser = false)
            {
                if(byUser != null)
                    State = byUser == true ? States.Sellected : States.SelGroupe;
                Status = true;
                return App.tweaks.ApplyTweak(this);
            }

            public bool Test()
            {
                return App.tweaks.TestTweak(this);
            }

            public bool Undo()
            {
                State = States.Unsellected;
                Status = false;
                return App.tweaks.UndoTweak(this);
            }

            public void Store(XmlWriter writer)
            {
                writer.WriteStartElement("Tweak");

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
                        if (node.Name == "Category")
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
