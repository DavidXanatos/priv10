using Microsoft.Win32;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Numerics;

namespace PrivateWin10
{
    public class WinNetFwCom : IDisposable
    {
        INetFwPolicy2 NetFwPolicy;

        class NetFwRule
        {
            public INetFwRule2 Entry;
            public FirewallRule Rule;
        }

        int RuleCounter = 0;
        Dictionary<string, NetFwRule> Rules = new Dictionary<string, NetFwRule>();

        public WinNetFwCom()
        {
            // this exists since windows vista
            NetFwPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        }

        public void Dispose()
        {

        }

        public List<FirewallRule> LoadRules()
        {
            RuleCounter = 0;
            Rules.Clear();

            List<FirewallRule> rules = new List<FirewallRule>();
            try
            {
                int Count = NetFwPolicy.Rules.Count;
                foreach (INetFwRule2 entry in NetFwPolicy.Rules)
                {
                    FirewallRule rule = new FirewallRule();
                    if (LoadRule(rule, entry))
                    {
                        rule.Index = Count - ++RuleCounter;
                        rule.guid = Guid.NewGuid().ToString("B");
                        Rules.Add(rule.guid, new NetFwRule() { Entry = entry, Rule = rule });
                        rules.Add(rule);
                    }
                }
            }
            catch // firewall service is deisabled :/
            {
                return null;
            }
            return rules;
        }

        public FirewallRule GetRule(string guid)
        {
            NetFwRule FwRule;
            if (!Rules.TryGetValue(guid, out FwRule))
                return null;
            return FwRule.Rule;
        }

        public List<FirewallRule> GetRules()
        {
            List<FirewallRule> rules = new List<FirewallRule>();
            foreach (var FwRule in Rules.Values)
                rules.Add(FwRule.Rule);
            return rules;
        }

        public bool UpdateRule(FirewallRule rule)
        {
            bool bAdd = (rule.guid == null);
            try
            {
                NetFwRule FwRule;
                if (bAdd)
                    FwRule = new NetFwRule() { Entry = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")), Rule = rule };
                else if (!Rules.TryGetValue(rule.guid, out FwRule))
                {
                    App.LogError("Failed Updating rule: ruls is not longer present");
                    return false;
                }
                else
                    FwRule.Rule = rule;

                if (!SaveRule(rule, FwRule.Entry))
                    return false;

                if (bAdd)
                {
                    NetFwPolicy.Rules.Add(FwRule.Entry);

                    rule.Index = RuleCounter++;
                    rule.guid = Guid.NewGuid().ToString("B");
                    Rules.Add(rule.guid, FwRule);
                }
            }
            catch (Exception err)
            {
                App.LogError("Failed to Write rule: " + err.Message);
                return false;
            }
            return true;
        }

        public bool RemoveRule(FirewallRule rule)
        {
            NetFwRule FwRule;
            if (!Rules.TryGetValue(rule.guid, out FwRule))
                return true; // tne rule is already gone

            try
            {
                // Note: if this is not set to null renam may fail as well as other sets :/
                FwRule.Entry.EdgeTraversalOptions = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DENY;

                // Note: the removal is done byname, howeever multiple rules may have the same name, WTF, so we set a temporary unique name
                FwRule.Entry.Name = "***_to_be_deleted_***"; // todo add rand string

                NetFwPolicy.Rules.Remove(FwRule.Entry.Name);

                Rules.Remove(rule.guid);
            }
            catch (Exception err)
            {
                App.LogError("Failed to Remove rule: " + err.Message);
                return false;
            }
            return true;
        }

        public static bool LoadRule(FirewallRule rule, INetFwRule2 entry)
        {
            try
            {
                INetFwRule3 entry3 = entry as INetFwRule3;

                rule.BinaryPath = entry.ApplicationName;
                rule.ServiceTag = entry.serviceName;
                if (entry3 != null)
                    rule.AppSID = entry3.LocalAppPackageId;

                // Note: while LocalAppPackageId and serviceName can be set at the same timea universall App can not be started as a service
                ProgramID progID;
                if (entry.ApplicationName != null && entry.ApplicationName.Equals("System", StringComparison.OrdinalIgnoreCase))
                    progID = ProgramID.NewID(ProgramID.Types.System);
                // Win10
                else if (entry3 != null && entry3.LocalAppPackageId != null)
                {
                    if (entry.serviceName != null)
                        throw new ArgumentException("Firewall paremeter conflict");
                    progID = ProgramID.NewAppID(entry3.LocalAppPackageId, entry.ApplicationName);
                }
                //
                else if (entry.serviceName != null)
                    progID = ProgramID.NewSvcID(entry.serviceName, entry.ApplicationName);
                else if (entry.ApplicationName != null)
                    progID = ProgramID.NewProgID(entry.ApplicationName);
                else // if nothing is configured than its a global roule
                    progID = ProgramID.NewID(ProgramID.Types.Global);

                rule.ProgID = Priv10Engine.AdjustProgID(progID);

                // https://docs.microsoft.com/en-us/windows/desktop/api/netfw/nn-netfw-inetfwrule

                rule.Name = entry.Name;
                rule.Grouping = entry.Grouping;
                rule.Description = entry.Description;

                //rule.ProgramPath = entry.ApplicationName;
                //rule.ServiceName = entry.serviceName;

                rule.Enabled = entry.Enabled;

                switch (entry.Direction)
                {
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN: rule.Direction = FirewallRule.Directions.Inbound; break;
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT: rule.Direction = FirewallRule.Directions.Outboun; break;
                }

                switch (entry.Action)
                {
                    case NET_FW_ACTION_.NET_FW_ACTION_ALLOW: rule.Action = FirewallRule.Actions.Allow; break;
                    case NET_FW_ACTION_.NET_FW_ACTION_BLOCK: rule.Action = FirewallRule.Actions.Block; break;
                }

                rule.Profile = entry.Profiles;

                if (entry.InterfaceTypes.Equals("All", StringComparison.OrdinalIgnoreCase))
                    rule.Interface = (int)FirewallRule.Interfaces.All;
                else
                {
                    rule.Interface = 0;
                    if (entry.InterfaceTypes.IndexOf("Lan", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)FirewallRule.Interfaces.Lan;
                    if (entry.InterfaceTypes.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)FirewallRule.Interfaces.Wireless;
                    if (entry.InterfaceTypes.IndexOf("RemoteAccess", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)FirewallRule.Interfaces.RemoteAccess;
                }

                rule.Protocol = entry.Protocol;

                /*The localAddrs parameter consists of one or more comma-delimited tokens specifying the local addresses from which the application can listen for traffic. "*" is the default value. Valid tokens include:

                "*" indicates any local address. If present, this must be the only token included.
                "Defaultgateway"
                "DHCP"
                "WINS"
                "LocalSubnet" indicates any local address on the local subnet. This token is not case-sensitive.
                A subnet can be specified using either the subnet mask or network prefix notation. If neither a subnet mask not a network prefix is specified, the subnet mask defaults to 255.255.255.255.
                A valid IPv6 address.
                An IPv4 address range in the format of "start address - end address" with no spaces included.
                An IPv6 address range in the format of "start address - end address" with no spaces included.*/

                switch (rule.Protocol)
                {
                    case (int)FirewallRule.KnownProtocols.ICMP:
                    case (int)FirewallRule.KnownProtocols.ICMPv6:
                        rule.SetIcmpTypesAndCodes(entry.IcmpTypesAndCodes);
                        break;
                    case (int)FirewallRule.KnownProtocols.TCP:
                    case (int)FirewallRule.KnownProtocols.UDP:
                        // , separated number or range 123-456
                        rule.LocalPorts = entry.LocalPorts;
                        rule.RemotePorts = entry.RemotePorts;
                        break;
                }

                rule.LocalAddresses = entry.LocalAddresses;
                rule.RemoteAddresses = entry.RemoteAddresses;

                // https://docs.microsoft.com/de-de/windows/desktop/api/icftypes/ne-icftypes-net_fw_edge_traversal_type_
                //EdgeTraversal = (int)(Entry.EdgeTraversal ? NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW : NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DENY);
                rule.EdgeTraversal = entry.EdgeTraversalOptions;

                if (entry3 != null)
                {
                    /*
                    string s0 = entry3.LocalAppPackageId // 8
                    string s1 = entry3.RemoteUserAuthorizedList; // 7
                    string s2 = entry3.RemoteMachineAuthorizedList; // 7
                    string s3 = entry3.LocalUserAuthorizedList; // 8
                    string s4 = entry3.LocalUserOwner; // 8
                    int i1 = entry3.SecureFlags; // ??
                    */
                }
            }
            catch (Exception err)
            {
                App.LogError("Reading Firewall Rule failed {0}", err.ToString());
                return false;
            }
            return true;
        }

        public static bool SaveRule(FirewallRule rule, INetFwRule2 entry)
        {
            try
            {
                entry.EdgeTraversalOptions = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DENY;

                INetFwRule3 entry3 = entry as INetFwRule3;

                entry.ApplicationName = rule.BinaryPath;
                entry.serviceName = rule.ServiceTag;
                if (entry3 != null)
                    entry3.LocalAppPackageId = rule.AppSID;

                /*
                switch (rule.ProgID.Type)
                {
                    case ProgramID.Types.Global:
                        entry.ApplicationName = null;
                        break;
                    case ProgramID.Types.System:
                        entry.ApplicationName = "System";
                        break;
                    default:
                        if (rule.ProgID.Path != null && rule.ProgID.Path.Length > 0)
                            entry.ApplicationName = rule.ProgID.Path;
                        break;
                }

                if (rule.ProgID.Type == ProgramID.Types.App)
                    entry3.LocalAppPackageId = rule.ProgID.GetPackageSID();
                else
                    entry3.LocalAppPackageId = null;

                if (rule.ProgID.Type == ProgramID.Types.Service)
                    entry.serviceName = rule.ProgID.GetServiceId();
                else
                    entry.serviceName = null;
                */

                entry.Name = rule.Name;
                entry.Grouping = rule.Grouping;
                entry.Description = rule.Description;

                entry.Enabled = rule.Enabled;

                switch (rule.Direction)
                {
                    case FirewallRule.Directions.Inbound: entry.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN; break;
                    case FirewallRule.Directions.Outboun: entry.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT; break;
                }

                switch (rule.Action)
                {
                    case FirewallRule.Actions.Allow: entry.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW; break;
                    case FirewallRule.Actions.Block: entry.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK; break;
                }

                entry.Profiles = rule.Profile;

                if (rule.Interface == (int)FirewallRule.Interfaces.All)
                    entry.InterfaceTypes = "All";
                else
                {
                    List<string> interfaces = new List<string>();
                    if ((rule.Interface & (int)FirewallRule.Interfaces.Lan) != 0)
                        interfaces.Add("Lan");
                    if ((rule.Interface & (int)FirewallRule.Interfaces.Wireless) != 0)
                        interfaces.Add("Wireless");
                    if ((rule.Interface & (int)FirewallRule.Interfaces.RemoteAccess) != 0)
                        interfaces.Add("RemoteAccess");
                    entry.InterfaceTypes = string.Join(",", interfaces.ToArray().Reverse());
                }

                // Note: if this is not cleared protocol change may trigger an exception
                if (entry.LocalPorts != null)
                    entry.LocalPorts = null;
                if (entry.RemotePorts != null)
                    entry.RemotePorts = null;
                if (entry.IcmpTypesAndCodes != null)
                    entry.IcmpTypesAndCodes = null;

                // Note: protocol must be set early enough or other sets will cause errors!
                entry.Protocol = rule.Protocol;

                switch (rule.Protocol)
                {
                    case (int)FirewallRule.KnownProtocols.ICMP:
                    case (int)FirewallRule.KnownProtocols.ICMPv6:
                        entry.IcmpTypesAndCodes = rule.GetIcmpTypesAndCodes();
                        break;
                    case (int)FirewallRule.KnownProtocols.TCP:
                    case (int)FirewallRule.KnownProtocols.UDP:
                        entry.LocalPorts = rule.LocalPorts;
                        entry.RemotePorts = rule.RemotePorts;
                        break;
                }

                if (rule.EdgeTraversal != (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER)
                {
                    entry.LocalAddresses = rule.LocalAddresses;
                    entry.RemoteAddresses = rule.RemoteAddresses;
                }

                entry.EdgeTraversalOptions = rule.EdgeTraversal;


                if (entry3 != null)
                {
                    /*
                    string s0 = entry3.LocalAppPackageId // 8
                    string s1 = entry3.RemoteUserAuthorizedList; // 7
                    string s2 = entry3.RemoteMachineAuthorizedList; // 7
                    string s3 = entry3.LocalUserAuthorizedList; // 8
                    string s4 = entry3.LocalUserOwner; // 8
                    int i1 = entry3.SecureFlags; // ??
                    */
                }
            }
            catch (Exception err)
            {
                App.LogError("Firewall Rule Commit failed {0}", err.ToString());
                return false;
            }
            return true;
        }


        public bool GetFirewallEnabled(FirewallRule.Profiles profileType)
        {
            return NetFwPolicy.get_FirewallEnabled((NET_FW_PROFILE_TYPE2_)profileType);
        }

        public void SetFirewallEnabled(FirewallRule.Profiles profileType, bool Enabled)
        {
            NetFwPolicy.set_FirewallEnabled((NET_FW_PROFILE_TYPE2_)profileType, Enabled);
        }

        public bool GetBlockAllInboundTraffic(FirewallRule.Profiles profileType)
        {
            return NetFwPolicy.get_BlockAllInboundTraffic((NET_FW_PROFILE_TYPE2_)profileType);
        }

        public void SetBlockAllInboundTraffic(FirewallRule.Profiles profileType, bool Block)
        {
            NetFwPolicy.set_BlockAllInboundTraffic((NET_FW_PROFILE_TYPE2_)profileType, Block);
        }

        public FirewallRule.Actions GetDefaultInboundAction(FirewallRule.Profiles profileType)
        {
            if (NetFwPolicy.get_DefaultInboundAction((NET_FW_PROFILE_TYPE2_)profileType) == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                return FirewallRule.Actions.Block;
            return FirewallRule.Actions.Allow;
        }

        public void SetDefaultInboundAction(FirewallRule.Profiles profileType, FirewallRule.Actions Action)
        {
            NetFwPolicy.set_DefaultInboundAction((NET_FW_PROFILE_TYPE2_)profileType, Action == FirewallRule.Actions.Block ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
        }

        public FirewallRule.Actions GetDefaultOutboundAction(FirewallRule.Profiles profileType)
        {
            if (NetFwPolicy.get_DefaultOutboundAction((NET_FW_PROFILE_TYPE2_)profileType) == NET_FW_ACTION_.NET_FW_ACTION_BLOCK)
                return FirewallRule.Actions.Block;
            return FirewallRule.Actions.Allow;
        }

        public void SetDefaultOutboundAction(FirewallRule.Profiles profileType, FirewallRule.Actions Action)
        {
            NetFwPolicy.set_DefaultOutboundAction((NET_FW_PROFILE_TYPE2_)profileType, Action == FirewallRule.Actions.Block ? NET_FW_ACTION_.NET_FW_ACTION_BLOCK : NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
        }

        public int GetCurrentProfiles()
        {
            return NetFwPolicy.CurrentProfileTypes;
        }
    }
}