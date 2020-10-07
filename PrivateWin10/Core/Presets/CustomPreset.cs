using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    public class CustomPreset: PresetItem
    {
        public string OnCommand;
        public string OffCommand;

        public override PresetItem Clone()
        {
            CustomPreset item = new CustomPreset();

            Clone(item);
            item.OnCommand = this.OnCommand;
            item.OffCommand = this.OffCommand;

            return item;
        }

        public override bool SetState(bool State)
        {

            // todo y<<<<<<<<<<<<<<<<<< xxxxxxxxxxxxxxxx

            return true;
        }

        protected override void StoreNodes(XmlWriter writer)
        {
            writer.WriteElementString("OnCommand", OnCommand);
            writer.WriteElementString("OffCommand", OffCommand);
        }

        protected override bool LoadNode(XmlNode node)
        {
            if (node.Name == "OnCommand")
                OnCommand = node.InnerText;
            else if (node.Name == "OffCommand")
                OffCommand = node.InnerText;
            else if (!base.LoadNode(node))
                return false;
            return true;
        }

        public override string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\cmd.exe");
        }
    }
}
