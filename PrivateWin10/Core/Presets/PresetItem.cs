using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    public enum PresetType
    {
        Unknown = 0,
        Tweak,
        Firewall,
        //Terminator,
        //Dns,
        Control,
        Custom
    }

    public abstract class PresetItem
    {
        public Guid guid = Guid.NewGuid();
        public string Name = "";
        public string Icon = "";

        public PresetType Type;

        public static PresetItem New(PresetType Type)
        {
            PresetItem item = null;
            switch (Type)
            {
                case PresetType.Tweak:     item = new TweakPreset(); break;
                case PresetType.Firewall:  item = new FirewallPreset(); break;
                case PresetType.Control:   item = new ControlPreset(); break;
                case PresetType.Custom:    item = new CustomPreset(); break;
                default: return null;
            }
            item.Type = Type;
            return item;
        }

        public abstract PresetItem Clone();

        protected void Clone(PresetItem item)
        {
            item.Type = this.Type;
            item.guid = this.guid;
            item.Name = this.Name;
            item.Icon = this.Icon;
        }

        public abstract bool SetState(bool State);

        public void Store(XmlWriter writer)
        {
            writer.WriteStartElement("Item");
            writer.WriteAttributeString("Type", Type.ToString());

            writer.WriteElementString("Guid", guid.ToString());

            writer.WriteElementString("Name", Name);
            writer.WriteElementString("Icon", Icon);
            StoreNodes(writer);

            writer.WriteEndElement();
        }

        protected abstract void StoreNodes(XmlWriter writer);

        public bool Load(XmlNode presetNode)
        {
            if (presetNode.Name != "Item")
                return false;

#if !DEBUG
            try
#endif
            {
                foreach (XmlNode node in presetNode.ChildNodes)
                {
                    LoadNode(node);
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

        protected virtual bool LoadNode(XmlNode node)
        {
            if (node.Name == "Guid")
                guid = Guid.Parse(node.InnerText);
            else if (node.Name == "Name")
                Name = node.InnerText;
            else if (node.Name == "Icon")
                Icon = node.InnerText;
            else
                return false;
            return true;
        }

        // Update sub items
        public virtual bool Sync(bool CleanUp = false) { return true; } 

        public virtual string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\shell32.dll");
        }
    }
}
