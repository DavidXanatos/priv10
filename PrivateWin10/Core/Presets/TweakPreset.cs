using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TweakEngine;

namespace PrivateWin10
{
    public class TweakPreset: PresetItem
    {
        public string TweakGroup;

        public struct SingleTweak
        {
            public string TweakName;
            public bool? OnState;
            public bool? OffState;
        }

        public Dictionary<string, SingleTweak> Tweaks = new Dictionary<string, SingleTweak>();

        public override PresetItem Clone()
        {
            TweakPreset item = new TweakPreset();

            Clone(item);
            item.TweakGroup = this.TweakGroup;
            
            item.Tweaks = this.Tweaks.ToDictionary(entry => entry.Key, entry => entry.Value);

            return item;
        }

        public override bool SetState(bool State)
        {
            foreach(var item in Tweaks)
            {
                TweakList.Tweak tweak = App.tweaks.GetTweak(item.Value.TweakName);
                if (tweak == null)
                    continue; // todo log error

                bool? newState = null;
                if (State && item.Value.OnState != null)
                    newState = item.Value.OnState;
                else if (State == false && item.Value.OffState != null)
                    newState = item.Value.OffState;

                if (newState != null)
                {
                    if (newState.Value)
                        App.tweaks.ApplyTweak(tweak, true);
                    else
                        App.tweaks.UndoTweak(tweak);
                }
            }

            return true;
        }

        protected override void StoreNodes(XmlWriter writer)
        {
            writer.WriteElementString("TweakGroup", TweakGroup);

            foreach (SingleTweak item in Tweaks.Values)
            {
                writer.WriteStartElement("Tweak");
                
                writer.WriteElementString("TweakName", item.TweakName);
                if(item.OnState != null) writer.WriteElementString("OnState", item.OnState.Value.ToString());
                if(item.OffState != null) writer.WriteElementString("OffState", item.OffState.Value.ToString());
                
                writer.WriteEndElement();
            }
        }

        protected override bool LoadNode(XmlNode node)
        {
            if (node.Name == "TweakGroup")
                TweakGroup = node.InnerText;
            else if (node.Name == "Tweak")
            {
                SingleTweak Tweak = new SingleTweak();
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode.Name == "TweakName")
                        Tweak.TweakName = subNode.InnerText;
                    else if (subNode.Name == "OnState")
                        Tweak.OnState = bool.Parse(subNode.InnerText);
                    else if (subNode.Name == "OffState")
                        Tweak.OffState = bool.Parse(subNode.InnerText);
                }
                if(Tweak.TweakName != null)
                    Tweaks.Add(Tweak.TweakName, Tweak);
            }
            else if (!base.LoadNode(node))
                return false;
            return true;
        }

        public override bool Sync(bool CleanUp = false)
        {
            TweakPresets.Group group = App.tweaks.GetGroup(TweakGroup);
            if (group == null)
                return false;

            foreach (var tweak in group.Tweaks.Values)
            {
                if (Tweaks.ContainsKey(tweak.Name))
                    continue;

                Tweaks.Add(tweak.Name, new SingleTweak() { TweakName = tweak.Name });
            }

            Name = group.Name;

            return true;
        }

        public override string GetIcon()
        {
            if (Icon != null && Icon.Length > 0)
                return Icon;
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\regedit.exe"); // @"%SystemRoot%\system32\gpedit.dll"
        }
    }
}
