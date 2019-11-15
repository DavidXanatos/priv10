using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    [Serializable()]
    public class FirewallRuleEx: FirewallRule
    {
        public enum States
        {
            Unknown = 0,
            Approved,
            Changed,
            Deleted
        }
        public States State = States.Unknown;

        //public bool Changed = false;
        public DateTime LastChangedTime = DateTime.MinValue;
        public int ChangedCount = 0;

        public UInt64 Expiration = 0;

        public FirewallRule Backup = null;

        public FirewallRuleEx()
        {
        }

        public FirewallRuleEx(FirewallRuleEx other, FirewallRule rule)
        {
            this.State = other.State;

            //this.Changed = other.Changed;
            this.LastChangedTime = other.LastChangedTime;
            this.ChangedCount = other.ChangedCount;

            this.Expiration = other.Expiration;

            this.Backup = other.Backup;

            this.Assign(rule);
        }

        public void SetChanged() // or added or removed
        {
            //Changed = true;
            LastChangedTime = DateTime.Now;
            ChangedCount++;
        }

        public void SetApplied()
        {
            //Changed = false;
            State = States.Approved;
            Backup = null;
        }

        public override void Store(XmlWriter writer, bool bRaw = false)
        {
            if (!bRaw) writer.WriteStartElement("FwRule");

            base.Store(writer, true);

            writer.WriteElementString("State", State.ToString());

            //if (Changed) writer.WriteElementString("Changed", Changed.ToString());
            if (LastChangedTime != DateTime.MinValue) writer.WriteElementString("LastChangedTime", LastChangedTime.ToString());
            if (ChangedCount != 0) writer.WriteElementString("ChangedCount", ChangedCount.ToString());

            if(Expiration != 0) writer.WriteElementString("Expiration", Expiration.ToString());

            if (Backup != null)
            {
                writer.WriteStartElement("Backup");
                Backup.Store(writer, true);
                writer.WriteEndElement();
            }

            if (!bRaw) writer.WriteEndElement();
        }

        public override bool Load(XmlNode entryNode)
        {
            if (!base.Load(entryNode))
                return false;

            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "State")
                    Enum.TryParse<States>(node.InnerText, out State);

                //else if (node.Name == "Changed")
                //    bool.TryParse(node.InnerText, out Changed);
                else if (node.Name == "LastChangedTime")
                    DateTime.TryParse(node.InnerText, out LastChangedTime);
                else if (node.Name == "ChangedCount")
                    int.TryParse(node.InnerText, out ChangedCount);

                else if (node.Name == "Expiration")
                    UInt64.TryParse(node.InnerText, out Expiration);

                else if (node.Name == "Backup")
                {
                    Backup = new FirewallRule();
                    if (!Backup.Load(node))
                        Backup = null;
                }
            }

            return true;
        }
    }
}
