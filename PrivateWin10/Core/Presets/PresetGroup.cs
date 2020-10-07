using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    public class PresetGroup
    {
        public Guid guid = Guid.NewGuid();
        public string Name = "";
        public string Icon = "";
        public string Description = "";
        //public string Category = "";

        public bool State = false;
        public int AutoUndo = 0;
        public DateTime? UndoTime = null;

        public Dictionary<Guid, PresetItem> Items = new Dictionary<Guid, PresetItem>();

        public PresetGroup()
        {

        }

        public PresetGroup Clone()
        {
            PresetGroup preset = new PresetGroup();

            preset.guid = this.guid;
            preset.Name = this.Name;
            preset.Icon = this.Icon;
            preset.Description = this.Description;
            //preset.Category = this.Category;

            preset.AutoUndo = this.AutoUndo;
            preset.UndoTime = this.UndoTime;

            preset.Items = this.Items.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());

            return preset;
        }

        public bool SetState(bool State)
        {
            this.State = State;

            if(!State)
                UndoTime = null;
            else if (AutoUndo != 0)
                UndoTime = DateTime.Now.AddSeconds(AutoUndo);

            foreach (PresetItem item in Items.Values)
                item.SetState(State);

            return true;
        }

        public void Store(XmlWriter writer)
        {
            writer.WriteStartElement("Preset");

            writer.WriteElementString("Guid", guid.ToString());

            writer.WriteElementString("Name", Name);
            writer.WriteElementString("Icon", Icon);
            writer.WriteElementString("Description", Description);
            //writer.WriteElementString("Category", Category);
            
            writer.WriteElementString("State", State.ToString());
            if(AutoUndo != 0) writer.WriteElementString("AutoUndo", AutoUndo.ToString());

            foreach (PresetItem item in Items.Values)
                item.Store(writer);

            writer.WriteEndElement();
        }

        public bool Load(XmlNode presetNode)
        {
            if(presetNode.Name != "Preset")
                return false;

#if !DEBUG
            try
#endif
            {
                foreach (XmlNode node in presetNode.ChildNodes)
                {
                    if (node.Name == "Guid")
                        guid = Guid.Parse(node.InnerText);
                    else if (node.Name == "Name")
                        Name = node.InnerText;
                    else if (node.Name == "Icon")
                        Icon = node.InnerText;
                    else if (node.Name == "Description")
                        Description = node.InnerText;
                    //else if (node.Name == "Category")
                    //    Category = node.InnerText;
                    else if (node.Name == "State")
                        State = bool.Parse(node.InnerText);
                    else if (node.Name == "AutoUndo")
                        AutoUndo = int.Parse(node.InnerText);
                    else if (node.Name == "Item")
                    {
                        PresetType Type;
                        if (!Enum.TryParse(node.Attributes["Type"].Value, true, out Type))
                            throw new Exception("Invalid Preset Item Type");

                        PresetItem item = PresetItem.New(Type);
                        if (item != null && item.Load(node) && !Items.ContainsKey(item.guid))
                        {
                            //item.Sync();
                            Items.Add(item.guid, item);
                        }
                    }
                }
            }
#if !DEBUG
            catch
            {
                return false;
            }
#endif

            return Name != null;
        }

        public string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\control.exe");
        }
    }

}
