using MiscHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;

namespace PrivateWin10
{
    public class PresetManager
    {
        public SortedDictionary<Guid, PresetGroup> Presets = new SortedDictionary<Guid, PresetGroup>();

        DispatcherTimer Timer;

        public class PresetChangeArgs : EventArgs
        {
            public PresetGroup preset;
        }

        public event EventHandler<PresetChangeArgs> PresetChange;

        public PresetManager()
        { 
            Load(App.dataPath + @"\Presets.xml");

            // on restart disable any left over presets
            foreach (PresetGroup preset in Presets.Values)
            {
                if (preset.State == true && preset.AutoUndo != null)
                    preset.SetState(false);
            }

            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(OnTimerTick);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            Timer.Start();
        }

        public void Store()
        {
            Store(App.dataPath + @"\Presets.xml");
        }

        protected void OnTimerTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (PresetGroup preset in Presets.Values)
            {
                if (preset.UndoTime != null && preset.UndoTime < now)
                {
                    preset.SetState(false);

                    PresetChange?.Invoke(this, new PresetChangeArgs() { preset = preset });
                }
            }
        }

        public void UpdatePreset(PresetGroup preset)
        {
            if (!Presets.ContainsKey(preset.guid))
            {
                Presets.Add(preset.guid, preset);
                preset = null; // trigger full list refresh
            }
            else
            {
                var oldPreset = Presets[preset.guid];
                Presets[preset.guid] = preset;
                if (oldPreset.State)
                {
                    oldPreset.SetState(false);
                    preset.SetState(true);
                }
            }

            PresetChange?.Invoke(this, new PresetChangeArgs() { preset = preset });
        }

        public void RemovePreset(Guid guid)
        {
            if (!Presets.ContainsKey(guid))
                return;

            var preset = Presets[guid];
            if(preset.State)
                preset.SetState(false);

            Presets.Remove(guid);

            PresetChange?.Invoke(this, new PresetChangeArgs());
        }

        public void SetPreset(Guid guid, bool State)
        {
            PresetGroup preset;
            if (!Presets.TryGetValue(guid, out preset) || preset.State == State)
                return;

            preset.SetState(State);
            
            PresetChange?.Invoke(this, new PresetChangeArgs() { preset = preset });
        }


        public bool Load(string FilePath)
        {
            if (!File.Exists(FilePath))
                return false;

#if !DEBUG
            try
#endif
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(FilePath);

                foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                {
                    //LoadCount++;
                    PresetGroup preset = new PresetGroup();
                    if (!preset.Load(node))
                    {
                        //ErrorCount++;
                        continue;
                    }

                    if (!Presets.ContainsKey(preset.guid))
                        Presets.Add(preset.guid, preset);
                }
            }
#if !DEBUG
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
#endif
            return true;
        }

        public void Store(string FilePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter writer = XmlWriter.Create(FilePath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("ControlPresets");
            
            foreach (PresetGroup preset in Presets.Values)
                preset.Store(writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Dispose();
        }

        public PresetGroup FindPreset(string name, bool orMake = true)
        {
            foreach (PresetGroup preset in Presets.Values)
            {
                if (preset.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return preset;
            }

            if(!orMake)
                return null;
            
            var newPreset = new PresetGroup();
            newPreset.Name = name;
            return newPreset;
        }

        public List<string> GetProgPins(Guid ProgSetId)
        {
            List<string> list = new List<string>();
            foreach (PresetGroup preset in Presets.Values)
            {
                foreach (var item in preset.Items.Values)
                {
                    var fwItem = item as FirewallPreset;
                    if (fwItem == null)
                        continue;

                    if (fwItem.ProgSetId.Equals(ProgSetId))
                    {
                        list.Add(preset.Name);
                        break;
                    }
                }
            }
            return list;
        }

        public void PinProg(Guid ProgSetId, string name)
        {
            PresetGroup preset = FindPreset(name);

            FirewallPreset item = new FirewallPreset();   
            item.ProgSetId = ProgSetId;

            item.Sync(); // gets the name and so on

            preset.Items.Add(item.guid, item);

            UpdatePreset(preset);
        }

        public void UnPinProg(Guid ProgSetId)
        {
            foreach (PresetGroup preset in Presets.Values)
            {
                foreach (var item in new Dictionary<Guid, PresetItem>(preset.Items))
                {
                    var progItem = item.Value as FirewallPreset;
                    if (progItem == null)
                        continue;

                    if (progItem.ProgSetId.Equals(ProgSetId))
                    {
                        preset.Items.Remove(item.Key);
                    }
                }
            }
        }

        public List<string> GetTweakPins(string TweakGroup)
        {
            List<string> list = new List<string>();
            foreach (PresetGroup preset in Presets.Values)
            {
                foreach (var item in preset.Items.Values)
                {
                    var tweakItem = item as TweakPreset;
                    if (tweakItem == null)
                        continue;

                    if (tweakItem.TweakGroup.Equals(TweakGroup, StringComparison.InvariantCultureIgnoreCase))
                    {
                        list.Add(preset.Name);
                        break;
                    }
                }
            }
            return list;
        }

        public void PinTweak(string TweakGroup, string name)
        {
            PresetGroup preset = FindPreset(name);

            TweakPreset item = new TweakPreset();
            item.TweakGroup = TweakGroup;

            item.Sync(); // gets the name and so on

            preset.Items.Add(item.guid, item);

            UpdatePreset(preset);
        }

        public void UnPinTweak(string TweakGroup)
        {
            foreach (PresetGroup preset in Presets.Values)
            {
                foreach (var item in new Dictionary<Guid, PresetItem>(preset.Items))
                {
                    var tweakItem = item.Value as TweakPreset;
                    if (tweakItem == null)
                        continue;

                    if (tweakItem.TweakGroup.Equals(TweakGroup, StringComparison.InvariantCultureIgnoreCase))
                    {
                        preset.Items.Remove(item.Key);
                    }
                }
            }
        }
    }
}
