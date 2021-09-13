using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    /// <summary>
    /// A control preset allows to control other presets
    /// </summary>
    class ControlPreset : PresetItem
    {
        public Guid Preset = Guid.Empty;
        public bool? OnState;
        public bool? OffState;

        public ControlPreset()
        {
            Type = PresetType.Control;
        }

        public override PresetItem Clone()
        {
            ControlPreset item = new ControlPreset();

            Clone(item);
            item.Preset = this.Preset;
            item.OnState = this.OnState;
            item.OffState = this.OffState;

            return item;
        }

        public override bool SetState(bool State)
        {
            return SetState(State, new List<Guid>());
        }

        public bool SetState(bool State, List<Guid> Trace)
        {
            // avoid circle calls
            if (Trace.Contains(this.guid))
                return false;
            Trace.Add(this.guid);

            PresetGroup group;
            if (!App.presets.Presets.TryGetValue(Preset, out group))
                return false;

            group.State = State;

            foreach (PresetItem item in group.Items.Values)
            {
                ControlPreset CtrlItem = item as ControlPreset;
                if (CtrlItem != null)
                    CtrlItem.SetState(State, Trace);
                else
                    item.SetState(State);
            }

            return true;
        }

        protected override void StoreNodes(XmlWriter writer)
        {
            writer.WriteElementString("Preset", Preset.ToString());
            if (OnState != null) writer.WriteElementString("OnState", OnState.Value.ToString());
            if (OffState != null) writer.WriteElementString("OffState", OffState.Value.ToString());
        }

        protected override bool LoadNode(XmlNode node)
        {
            if (node.Name == "Preset")
                Preset = Guid.Parse(node.InnerText);
            else if (node.Name == "OnState")
                OnState = bool.Parse(node.InnerText);
            else if (node.Name == "OffState")
                OffState = bool.Parse(node.InnerText);
            else if (!base.LoadNode(node))
                return false;
            return true;
        }

        public override string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\wbem\WmiPrvSE.exe");
        }
    }
}
