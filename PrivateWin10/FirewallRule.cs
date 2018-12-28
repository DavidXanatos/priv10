#define win10

using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    [Serializable()]
    public class FirewallRule
    {
        public Guid guid;
        public ProgramList.ID mID;

        public string Name;
        public string Grouping;
        public string Description;

        public bool Enabled;
        public Firewall.Actions Action = Firewall.Actions.Undefined;
        public Firewall.Directions Direction = Firewall.Directions.Unknown;
        public int Profile = (int)Firewall.Profiles.All;

        public int Protocol = (int)NetFunc.KnownProtocols.Any;
        public int Interface = (int)Firewall.Interfaces.All;
        public string LocalPorts;
        public string LocalAddresses = "*";
        public string RemoteAddresses = "*";
        public string RemotePorts;
        public string IcmpTypesAndCodes;

        public int EdgeTraversal;

        public long Expiration = 0;

        public enum KnownProtocols
        {
            ICMP = 1,
            TCP = 6,
            UDP = 17,
            ICMPv6 = 58,
        }

        static public List<string> SpecialPorts = new List<string>() {
            "IPHTTPS",
            "RPC-EPMap",
            "RPC",
            "Teredo",
            "Ply2Disc",
            "mDNS"
        };

        static public List<string> SpecialAddresses = new List<string>() {
            "LocalSubnet",
            "DefaultGateway",
            "DNS",
            "DHCP",
            "WINS"
        };


        public FirewallRule()
        {
            guid = Guid.NewGuid();
        }

        public FirewallRule(ProgramList.ID id)
        {
            guid = Guid.Empty;
            mID = id;
        }

        public FirewallRule Clone()
        {
            FirewallRule rule = new FirewallRule(mID);

            rule.Name = Name;
            rule.Grouping = Grouping;
            rule.Description = Description;

            rule.Enabled = Enabled;
            rule.Action = Action;
            rule.Direction = Direction;
            rule.Profile = Profile;

            rule.Protocol = Protocol;
            rule.Interface = Interface;
            rule.LocalPorts = LocalPorts;
            rule.LocalAddresses = LocalAddresses;
            rule.RemoteAddresses = RemoteAddresses;
            rule.RemotePorts = RemotePorts;
            rule.IcmpTypesAndCodes = IcmpTypesAndCodes;

            rule.EdgeTraversal = EdgeTraversal;

            rule.Expiration = Expiration;

            return rule;
        }

        public static bool LoadRule(FirewallRule rule, INetFwRule2 entry)
        {
            try
            {
#if win10
                INetFwRule3 entry3 = entry as INetFwRule3;
#endif
                ProgramList.Types type;
                string path = entry.ApplicationName;
                string name = null;
                if (path != null && path.Equals("System", StringComparison.OrdinalIgnoreCase))
                    type = ProgramList.Types.System;
                else if (entry.serviceName != null)
                {
                    type = ProgramList.Types.Service;
                    name = entry.serviceName;
                }
#if win10
                else if (entry3 != null && entry3.LocalAppPackageId != null)
                {
                    type = ProgramList.Types.App;
                    name = entry3.LocalAppPackageId;
                }
#endif
                else if (path != null)
                    type = ProgramList.Types.Program;
                else
                    type = ProgramList.Types.Global;

                rule.mID = new ProgramList.ID(type, path, name);

                // https://docs.microsoft.com/en-us/windows/desktop/api/netfw/nn-netfw-inetfwrule

                rule.Name = entry.Name;
                rule.Grouping = entry.Grouping;
                rule.Description = entry.Description;

                //rule.ProgramPath = entry.ApplicationName;
                //rule.ServiceName = entry.serviceName;

                rule.Enabled = entry.Enabled;

                switch (entry.Direction)
                {
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN: rule.Direction = Firewall.Directions.Inbound; break;
                    case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT: rule.Direction = Firewall.Directions.Outboun; break;
                }

                switch (entry.Action)
                {
                    case NET_FW_ACTION_.NET_FW_ACTION_ALLOW: rule.Action = Firewall.Actions.Allow; break;
                    case NET_FW_ACTION_.NET_FW_ACTION_BLOCK: rule.Action = Firewall.Actions.Block; break;
                }

                rule.Profile = entry.Profiles;

                if (entry.InterfaceTypes.Equals("All", StringComparison.OrdinalIgnoreCase))
                    rule.Interface = (int)Firewall.Interfaces.All;
                else
                {
                    rule.Interface = (int)Firewall.Interfaces.None;
                    if (entry.InterfaceTypes.IndexOf("Lan", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)Firewall.Interfaces.Lan;
                    if (entry.InterfaceTypes.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)Firewall.Interfaces.Wireless;
                    if (entry.InterfaceTypes.IndexOf("RemoteAccess", StringComparison.OrdinalIgnoreCase) != -1)
                        rule.Interface |= (int)Firewall.Interfaces.RemoteAccess;
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
                        //The icmpTypesAndCodes parameter is a list of ICMP types and codes separated by semicolon. "*" indicates all ICMP types and codes.
                        rule.IcmpTypesAndCodes = entry.IcmpTypesAndCodes;
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

#if win10
                if (entry3 != null)
                {
                    //rule.AppID = entry3.LocalAppPackageId;
                    /*string s1 = entry3.LocalAppPackageId;
                    string s2 = entry3.RemoteMachineAuthorizedList;
                    string s3 = entry3.LocalUserAuthorizedList;
                    string s4 = entry3.LocalUserOwner;
                    int i1 = entry3.SecureFlags;*/
                }
#endif
            }
            catch (Exception err)
            {
                AppLog.Line("Reading Firewall Rule failed {0}", err.ToString());
                return false;
            }
            return true;
        }

        public static bool SaveRule(FirewallRule rule, INetFwRule2 entry)
        {
            try
            {
                entry.EdgeTraversalOptions = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DENY;

#if win10
                INetFwRule3 entry3 = entry as INetFwRule3;
#endif

                switch (rule.mID.Type)
                {
                    case ProgramList.Types.Global:
                        entry.ApplicationName = null;
                        break;
                    case ProgramList.Types.System:
                        entry.ApplicationName = "System";
                        break;
                    default:
                        if (rule.mID.Path != null && rule.mID.Path.Length > 0)
                            entry.ApplicationName = rule.mID.Path;
                        break;
                }

                if (rule.mID.Type == ProgramList.Types.Service)
                    entry.serviceName = rule.mID.Name;
                else
                    entry.serviceName = null;

#if win10
                if (rule.mID.Type == ProgramList.Types.App)
                    entry3.LocalAppPackageId = rule.mID.Name;
                else
                    entry3.LocalAppPackageId = null;
#endif

                entry.Name = rule.Name;
                entry.Grouping = rule.Grouping;
                entry.Description = rule.Description;

                //entry.ApplicationName = rule.ProgramPath;
                //entry.serviceName = rule.ServiceName;

                entry.Enabled = rule.Enabled;

                switch (rule.Direction)
                {
                    case Firewall.Directions.Inbound: entry.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN; break;
                    case Firewall.Directions.Outboun: entry.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT; break;
                }

                switch (rule.Action)
                {
                    case Firewall.Actions.Allow: entry.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW; break;
                    case Firewall.Actions.Block: entry.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK; break;
                }

                entry.Profiles = rule.Profile;

                if (rule.Interface == (int)Firewall.Interfaces.All)
                    entry.InterfaceTypes = "All";
                else
                {
                    List<string> interfaces = new List<string>();
                    if ((rule.Interface & (int)Firewall.Interfaces.Lan) != 0)
                        interfaces.Add("Lan");
                    if ((rule.Interface & (int)Firewall.Interfaces.Wireless) != 0)
                        interfaces.Add("Wireless");
                    if ((rule.Interface & (int)Firewall.Interfaces.RemoteAccess) != 0)
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
                        entry.IcmpTypesAndCodes = rule.IcmpTypesAndCodes;
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


#if win10
                if (entry3 != null)
                {
                    //entry3.LocalAppPackageId = rule.AppID;

                    /*entry3.LocalAppPackageId;
                    entry3.RemoteMachineAuthorizedList;
                    entry3.LocalUserAuthorizedList;
                    entry3.LocalUserOwner;
                    entry3.SecureFlags;*/
                }
#endif
            }
            catch (Exception err)
            {
                AppLog.Line("Firewall Rule Commit failed {0}", err.ToString());
                return false;
            }
            return true;
        }


        public const string RuleGroup = "PrivateWin10";
        public const string RulePrefix = "priv10";
        public const string TempRulePrefix = "priv10temp";
        public const string AllowAllName = "Allow All Network";
        public const string BlockAllName = "Block All Network";
        public const string AllowLan = "Allow Local Subnet";
        public const string BlockInet = "Block Internet";

        public static string MakeRuleName(string descr, bool temp = false) {return (temp ? TempRulePrefix : RulePrefix) + " - " + descr; }

        public static FirewallRule MakeAllowRule(ProgramList.ID id, Firewall.Directions direction, long expiration = 0)
        {
            FirewallRule rule = new FirewallRule(id);
            rule.Name = MakeRuleName(AllowAllName, expiration != 0);
            rule.Grouping = RuleGroup;
            rule.Action = Firewall.Actions.Allow;
            rule.Direction = direction;
            rule.Enabled = true;
            rule.Expiration = expiration;
            return rule;
        }

        public static FirewallRule MakeAllowLanRule(ProgramList.ID id, Firewall.Directions direction, long expiration = 0)
        {
            FirewallRule rule = new FirewallRule(id);
            rule.Name = MakeRuleName(AllowLan, expiration != 0);
            rule.Grouping = RuleGroup;
            rule.Action = Firewall.Actions.Allow;
            rule.Direction = direction;
            rule.Enabled = true;
            rule.RemoteAddresses = "LocalSubnet";
            rule.Expiration = expiration;
            return rule;
        }

        public static FirewallRule MakeBlockInetRule(ProgramList.ID id, Firewall.Directions direction, long expiration = 0)
        {
            FirewallRule rule = new FirewallRule(id);
            rule.Name = MakeRuleName(BlockInet, expiration != 0);
            rule.Grouping = RuleGroup;
            rule.Action = Firewall.Actions.Block;
            rule.Direction = direction;
            rule.Enabled = true;
            rule.RemoteAddresses = NetFunc.GetNonLocalNet();
            rule.Expiration = expiration;
            return rule;
        }

        public static FirewallRule MakeBlockRule(ProgramList.ID id, Firewall.Directions direction, long expiration = 0)
        {
            FirewallRule rule = new FirewallRule(id);
            rule.Name = MakeRuleName(BlockAllName, expiration != 0);
            rule.Grouping = RuleGroup;
            rule.Action = Firewall.Actions.Block;
            rule.Direction = direction;
            rule.Enabled = true;
            rule.Expiration = expiration;
            return rule;
        }

    }
}