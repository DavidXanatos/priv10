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
    }
}
