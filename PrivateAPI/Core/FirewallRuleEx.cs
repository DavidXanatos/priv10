using MiscHelpers;
using PrivateAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WinFirewallAPI;

namespace PrivateAPI
{
    [Serializable()]
    public class FirewallRuleEx: FirewallRule
    {
        public ProgramID ProgID;

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

        public Int64 HitCount = 0;

        public FirewallRule Backup = null;

        public FirewallRuleEx()
        {

        }

        public FirewallRuleEx(FirewallRuleEx other, FirewallRule rule)
        {
            ProgID = other.ProgID;

            State = other.State;

            //Changed = other.Changed;
            LastChangedTime = other.LastChangedTime;
            ChangedCount = other.ChangedCount;

            Expiration = other.Expiration;

            HitCount = other.HitCount;

            Backup = other.Backup;

            Assign(rule);
        }

        public override void Assign(FirewallRule rule)
        {
            ProgID = GetIdFromRule(rule);

            base.Assign(rule);
        }

        public void Assign(FirewallRuleEx rule)
        {
            this.ProgID = rule.ProgID;

            base.Assign(rule);
        }

        public static void SetProgID(FirewallRule rule, ProgramID progID)
        {
            switch (progID.Type)
            {
                case ProgramID.Types.Global:
                    rule.BinaryPath = null;
                    break;
                case ProgramID.Types.System:
                    rule.BinaryPath = "System";
                    break;
                default:
                    if (progID.Path != null && progID.Path.Length > 0)
                        rule.BinaryPath = progID.Path;
                    break;
            }

            if (progID.Type == ProgramID.Types.App)
                rule.AppSID = progID.GetPackageSID();
            else
                rule.AppSID = null;

            if (progID.Type == ProgramID.Types.Service)
                rule.ServiceTag = progID.GetServiceId();
            else
                rule.ServiceTag = null;
        }

        public void SetProgID(ProgramID progID)
        {
            ProgID = progID;

            SetProgID(this, progID);
        }

        public static ProgramID GetIdFromRule(FirewallRule rule)
        {
            ProgramID progID;
            string fullPath = rule.BinaryPath != null ? Environment.ExpandEnvironmentVariables(rule.BinaryPath) : null;
            if (rule.BinaryPath != null && rule.BinaryPath.Equals("System", StringComparison.OrdinalIgnoreCase))
                progID = ProgramID.NewID(ProgramID.Types.System);
            // Win 8+
            else if (rule.AppSID != null)
            {
                if (rule.ServiceTag != null)
                    AppLog.Debug("Firewall paremeter conflict in rule: {0}", rule.Name);
                progID = ProgramID.NewAppID(rule.AppSID, fullPath);
            }
            //
            else if (rule.ServiceTag != null)
                progID = ProgramID.NewSvcID(rule.ServiceTag, fullPath);
            else if (rule.BinaryPath != null)
                progID = ProgramID.NewProgID(fullPath);
            else // if nothing is configured than its a global roule
                progID = ProgramID.NewID(ProgramID.Types.Global);

            return AdjustProgID(progID);
        }

        static public ProgramID AdjustProgID(ProgramID progID)
        {
            /*
                Windows Internals Edition 6 / Chapter 4 / Service Tags:

                "Windows implements a service attribute called the service tag, ... The attribute is simply an 
                index identifying the service. The service tag is stored in the SubProcessTag field of the 
                thread environment block (TEB) of each thread (see Chapter 5, ...) and is propagated across all 
                threads that a main service thread creates (except threads created indirectly by thread-pool APIs).
                ... the TCP/IP stack saves the service tag of the threads that create TCP/IP end points ..."

                Well isn't that "great" in the end we can not really relay on the Service Tags :/
                A workable workaround to this issue is imho to ignore the Service Tags all together 
                for all services which are not hosted in svchost.exe as those should have unique binaries anyways.
             */

            if (progID.Type == ProgramID.Types.Service && progID.Path.Length > 0) // if its a service
            {
                if (System.IO.Path.GetFileName(progID.Path).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase) == false) // and NOT hosted in svchost.exe
                {
                    progID = ProgramID.NewProgID(progID.Path); // handle it as just a normal program
                }
            }

            return progID;
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

            ProgID.Store(writer, "ProgID");

            base.Store(writer, true);

            writer.WriteElementString("State", State.ToString());

            //if (Changed) writer.WriteElementString("Changed", Changed.ToString());
            if (LastChangedTime != DateTime.MinValue) writer.WriteElementString("LastChangedTime", LastChangedTime.ToString());
            if (ChangedCount != 0) writer.WriteElementString("ChangedCount", ChangedCount.ToString());

            if(Expiration != 0) writer.WriteElementString("Expiration", Expiration.ToString());

            if(HitCount != 0) writer.WriteElementString("HitCount", HitCount.ToString());

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
                if (node.Name == "ProgID")
                {
                    ProgID = new ProgramID();
                    ProgID.Load(node);
                }

                else if (node.Name == "State")
                    Enum.TryParse<States>(node.InnerText, out State);

                //else if (node.Name == "Changed")
                //    bool.TryParse(node.InnerText, out Changed);
                else if (node.Name == "LastChangedTime")
                    DateTime.TryParse(node.InnerText, out LastChangedTime);
                else if (node.Name == "ChangedCount")
                    int.TryParse(node.InnerText, out ChangedCount);

                else if (node.Name == "Expiration")
                    UInt64.TryParse(node.InnerText, out Expiration);

                else if (node.Name == "HitCount")
                    Int64.TryParse(node.InnerText, out HitCount);


                else if (node.Name == "Backup")
                {
                    Backup = new FirewallRule();
                    if (!Backup.Load(node))
                        Backup = null;
                }
            }

            return ProgID != null;
        }

    }
}
