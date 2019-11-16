using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

// Why is that not defined in the apropriate headers?
enum NET_FW_EDGE_TRAVERSAL_TYPE_ // values as used by INetFwRule2.EdgeTraversalOptions
{
    NET_FW_EDGE_TRAVERSAL_TYPE_DENY = 0,
    NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW, // 1
    NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP, // 2
    NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER // 3
}

namespace PrivateWin10
{
    [Serializable()]
    public class FirewallRule
    {
        public enum Directions
        {
            Unknown = 0,
            Outboun = 1,
            Inbound = 2,
            Bidirectiona = 3
        }

        public enum Actions
        {
            Undefined = 0,
            Allow = 1,
            Block = 2
        }

        public enum Profiles // same as NET_FW_PROFILE_TYPE2_ 
        {
            Undefined = 0,
            Domain = 0x0001,
            Private = 0x0002,
            Public = 0x0004,
            All = 0x7FFFFFFF
        }

        public enum Interfaces
        {
            All = 0,
            Lan = 0x0001,
            Wireless = 0x0002,
            RemoteAccess = 0x0004
        }

        // supported port keywords:
        public const string PortKeywordRpc = "RPC";
        public const string PortKeywordRpcEp = "RPC-EPMap";
        public const string PortKeywordTeredo = "Teredo";
#if (!FW_COM_ITF)
        // sinve win7
        public const string PortKeywordIpTlsIn = "IPHTTPSIn";
        public const string PortKeywordIpTlsOut = "IPHTTPSOut";
        // since win8
        public const string PortKeywordDhcp = "DHCP";
        public const string PortKeywordPly2Disc = "Ply2Disc";
        // since win10
        public const string PortKeywordMDns = "mDNS";
        // since 1607
        public const string PortKeywordCortan = "Cortana";
        // since 1903
        public const string PortKeywordProximalTcpCdn = "ProximalTcpCdn"; // wtf is that?
#endif

        // supported address keywords:
        public const string AddrKeywordLocalSubnet = "LocalSubnet"; // indicates any local address on the local subnet.
        public const string AddrKeywordDNS = "DNS";
        public const string AddrKeywordDHCP = "DHCP";
        public const string AddrKeywordWINS = "WINS";
        public const string AddrKeywordDefaultGateway = "DefaultGateway";
#if (!FW_COM_ITF)
        // since win8
        public const string AddrKeywordIntrAnet = "LocalIntranet";
        public const string AddrKeywordRmtIntrAnet = "RemoteIntranet";
        public const string AddrKeywordIntErnet = "Internet";
        public const string AddrKeywordPly2Renders = "Ply2Renders";
        // since 1903
        public const string AddrKeywordCaptivePortal = "CaptivePortal";
#endif


        public string guid = null; // Note: usually this is a guid but some default windows rules use a sting name instead
        public ProgramID ProgID;
        public int Index = 0; // this is only used for sorting by newest rules

        public string Name;
        public string Grouping;
        public string Description;

        public bool Enabled;
        public Actions Action = Actions.Undefined;
        public Directions Direction = Directions.Unknown;
        public int Profile = (int)Profiles.All;

        public int Protocol = (int)NetFunc.KnownProtocols.Any;
        public int Interface = (int)Interfaces.All;
        public string LocalPorts;
        public string LocalAddresses = "*";
        public string RemoteAddresses = "*";
        public string RemotePorts;
        public WindowsFirewall.FW_ICMP_TYPE_CODE[] IcmpTypesAndCodes = null;

        public WindowsFirewall.FW_OS_PLATFORM[] OsPlatformValidity = null;

        public int EdgeTraversal = 0;

        public enum KnownProtocols
        {
            ICMP = 1,
            TCP = 6,
            UDP = 17,
            ICMPv6 = 58,
        }

        public FirewallRule(ProgramID id)
        {
            ProgID = id;
        }

        public FirewallRule()
        {
        }

        public void Assign(FirewallRule rule)
        {
            this.guid = rule.guid;
            this.ProgID = rule.ProgID;
            this.Index = rule.Index;

            this.Name = rule.Name;
            this.Grouping = rule.Grouping;
            this.Description = rule.Description;

            this.Enabled = rule.Enabled;

            this.Action = rule.Action;
            this.Direction = rule.Direction;
            this.Profile = rule.Profile;

            this.Protocol = rule.Protocol;
            this.Interface = rule.Interface;
            this.LocalPorts = rule.LocalPorts;
            this.LocalAddresses = rule.LocalAddresses;
            this.RemoteAddresses = rule.RemoteAddresses;
            this.RemotePorts = rule.RemotePorts;
            this.IcmpTypesAndCodes = rule.IcmpTypesAndCodes;

            this.EdgeTraversal = rule.EdgeTraversal;

            // todo: xxx
        }

        public FirewallRule Duplicate()
        {
            FirewallRule rule = new FirewallRule(ProgID);
            rule.Assign(this);
            return rule;
        }

        public bool SafeEquals<T>(T L, T R)
        {
            if (L == null || R == null)
                return (L == null && R == null);
            return L.Equals(R);
        }

        public bool SafeEqualsStr(string L, string R)
        {
            if (IsEmptyOrStar(L) || IsEmptyOrStar(R))
                return (IsEmptyOrStar(L) && IsEmptyOrStar(R));
            return L.Equals(R, StringComparison.OrdinalIgnoreCase);
        }

        public bool SafeEqualsArr<T>(T[] L, T[] R)
        {
            if (L == null || L.Length == 0 || R == null || R.Length == 0)
                return ((L == null || L.Length == 0) && (R == null || L.Length == 0));
            if (L.Length != R.Length)
                return false;
            for (int i = 0; i < L.Length; i++)
            {
                if (!L[i].Equals(R[i]))
                    return false;
            }
            return true;
        }

        public enum MatchResult
        {
            Identical = 0,
            NameChanged = 1,
            StateChanged = 2,
            DataChanged = 3,
            TargetChanged = 4
        };

        public MatchResult Match(FirewallRule other)
        {
            if (this.ProgID.CompareTo(other.ProgID) != 0) return MatchResult.TargetChanged;

            if (!SafeEquals(this.Direction, other.Direction)) return MatchResult.DataChanged;
            if (!SafeEquals(this.Profile, other.Profile)) return MatchResult.DataChanged;

            if (!SafeEquals(this.Protocol, other.Protocol)) return MatchResult.DataChanged;
            if (!SafeEquals(this.Interface, other.Interface)) return MatchResult.DataChanged;
            if (!SafeEqualsStr(this.LocalPorts, other.LocalPorts)) return MatchResult.DataChanged;
            if (!SafeEqualsStr(this.LocalAddresses, other.LocalAddresses)) return MatchResult.DataChanged;
            if (!SafeEqualsStr(this.RemoteAddresses, other.RemoteAddresses)) return MatchResult.DataChanged;
            if (!SafeEqualsStr(this.RemotePorts, other.RemotePorts)) return MatchResult.DataChanged;
            if (!SafeEqualsArr(this.IcmpTypesAndCodes, other.IcmpTypesAndCodes)) return MatchResult.DataChanged;

            if (!SafeEquals(this.EdgeTraversal, other.EdgeTraversal)) return MatchResult.DataChanged;


            if (!SafeEquals(this.Enabled, other.Enabled)) return MatchResult.StateChanged;
            if (!SafeEquals(this.Action, other.Action)) return MatchResult.StateChanged;

            if (!SafeEquals(this.Name, other.Name)) return MatchResult.NameChanged;
            if (!SafeEquals(this.Grouping, other.Grouping)) return MatchResult.NameChanged;
            if (!SafeEquals(this.Description, other.Description)) return MatchResult.NameChanged;

            return MatchResult.Identical; 
        }

        public bool SetIcmpTypesAndCodes(string Str)
        {
            // The icmpTypesAndCodes parameter is a list of ICMP (types:codes) separated by semicolon. "*" indicates all ICMP types and codes.
            if (IsEmptyOrStar(Str))
            {
                IcmpTypesAndCodes = null;
                return true;
            }

            List<string> Strs = TextHelpers.SplitStr(Str, ",");

            IcmpTypesAndCodes = new WindowsFirewall.FW_ICMP_TYPE_CODE[Strs.Count];

            try
            {
                for (int i = 0; i < Strs.Count; i++)
                {
                    var TypeCode = TextHelpers.Split2(Strs[i], ":");

                    IcmpTypesAndCodes[i].Type = byte.Parse(TypeCode.Item1);
                    if (TypeCode.Item2 == "*" || TypeCode.Item2 == "256")
                        IcmpTypesAndCodes[i].Code = 256;
                    else
                        IcmpTypesAndCodes[i].Code = byte.Parse(TypeCode.Item2);
                }
            }
            catch {
                IcmpTypesAndCodes = null;
            }

            return IcmpTypesAndCodes != null;
        }

        public string GetIcmpTypesAndCodes()
        {
            if (IcmpTypesAndCodes == null || IcmpTypesAndCodes.Length == 0)
                return "*";

            List<string> Strs = new List<string>(IcmpTypesAndCodes.Length);

            foreach (var TypeCode in IcmpTypesAndCodes)
            {
                string Str = TypeCode.Type.ToString();
                if (TypeCode.Code == 256)
                    Str += ":*";
                else
                    Str += ":" + TypeCode.Code.ToString();
                Strs.Add(Str);
            }

            return string.Join(",", Strs); // Note: white spaces are not valid
        }

        public virtual void Store(XmlWriter writer, bool bRaw = false)
        {
            if(!bRaw) writer.WriteStartElement("FwRule");

            writer.WriteElementString("Guid", guid);
            if (!bRaw) ProgID.Store(writer, "ProgID");

            if (Name != null) writer.WriteElementString("Name", Name);
            if (Grouping != null) writer.WriteElementString("Grouping", Grouping);
            if (Description != null) writer.WriteElementString("Description", Description);

            writer.WriteElementString("Enabled", Enabled.ToString());

            writer.WriteElementString("Action", Action.ToString());
            writer.WriteElementString("Direction", Direction.ToString());
            if(Profile != (int)Profiles.All) writer.WriteElementString("Profile", Profile.ToString());

            if (Protocol != (int)NetFunc.KnownProtocols.Any) writer.WriteElementString("Protocol", Protocol.ToString());
            if (Interface != (int)Interfaces.All) writer.WriteElementString("Interface", Interface.ToString());
            if (!IsEmptyOrStar(LocalPorts)) writer.WriteElementString("LocalPorts", LocalPorts);
            if (!IsEmptyOrStar(LocalAddresses)) writer.WriteElementString("LocalAddresses", LocalAddresses);
            if (!IsEmptyOrStar(RemoteAddresses)) writer.WriteElementString("RemoteAddresses", RemoteAddresses);
            if (!IsEmptyOrStar(RemotePorts)) writer.WriteElementString("RemotePorts", RemotePorts);

            if (IcmpTypesAndCodes != null) writer.WriteElementString("IcmpTypesAndCodes", GetIcmpTypesAndCodes());

            if(EdgeTraversal != 0) writer.WriteElementString("EdgeTraversal", EdgeTraversal.ToString());

            if (!bRaw) writer.WriteEndElement();
        }

        public virtual bool Load(XmlNode entryNode)
        {
            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "Guid")
                    guid = node.InnerText;
                else if (node.Name == "ProgID")
                {
                    ProgID = new ProgramID();
                    ProgID.Load(node);
                }

                else if (node.Name == "Name")
                    Name = node.InnerText;
                else if (node.Name == "Grouping")
                    Grouping = node.InnerText;
                else if (node.Name == "Description")
                    Description = node.InnerText;

                else if (node.Name == "Enabled")
                    bool.TryParse(node.InnerText, out Enabled);

                else if (node.Name == "Action")
                    Enum.TryParse<Actions>(node.InnerText, out Action);
                else if (node.Name == "Direction")
                    Enum.TryParse<Directions>(node.InnerText, out Direction);
                else if (node.Name == "Profile")
                    int.TryParse(node.InnerText, out Profile);

                else if (node.Name == "Protocol")
                    int.TryParse(node.InnerText, out Protocol);
                else if (node.Name == "Interface")
                    int.TryParse(node.InnerText, out Interface);
                else if (node.Name == "LocalPorts")
                    LocalPorts = node.InnerText;
                else if (node.Name == "LocalAddresses")
                    LocalAddresses = node.InnerText;
                else if (node.Name == "RemoteAddresses")
                    RemoteAddresses = node.InnerText;
                else if (node.Name == "RemotePorts")
                    RemotePorts = node.InnerText;

                else if (node.Name == "IcmpTypesAndCodes")
                    SetIcmpTypesAndCodes(node.InnerText);

                else if (node.Name == "EdgeTraversal")
                    int.TryParse(node.InnerText, out EdgeTraversal);

            }

            return ProgID != null && guid != null;
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Helpers

        public static bool IsEmptyOrStar(string str)
        {
            return str == null || str == "" || str == "*";
        }

        public static bool MatchPort(UInt16 numPort, string strPorts)
        {
            foreach (string range in strPorts.Split(','))
            {
                string[] strTemp = range.Split('-');
                if (strTemp.Length == 1)
                {
                    UInt16 Port = 0;
                    if (!UInt16.TryParse(strTemp[0], out Port))
                    {
                        // todo: xxx some rule port values are strings :(
                        // how can we test that?!
                    }
                    else if (Port == numPort)
                        return true;
                }
                else if (strTemp.Length == 2)
                {
                    UInt16 beginPort = 0;
                    UInt16 endPort = 0;
                    if (UInt16.TryParse(strTemp[0], out beginPort) && UInt16.TryParse(strTemp[0], out endPort))
                    {
                        if (beginPort <= numPort && numPort <= endPort)
                            return true;
                    }
                }
            }
            return false;
        }

        public static bool MatchAddress(IPAddress Address, string strRanges, NetworkMonitor.AdapterInfo NicInfo = null)
        {
            int type = Address.GetAddressBytes().Length == 4 ? 4 : 6;
            BigInteger numIP = NetFunc.IpToInt(Address);

            foreach (string range in strRanges.Split(','))
            {
                string[] strTemp = range.Split('-');
                if (strTemp.Length == 1)
                {
                    if (strTemp[0].Contains("/")) // ip/net
                    {
                        string[] strTemp2 = strTemp[0].Split('/');
                        int temp;
                        BigInteger num1 = NetFunc.IpStrToInt(strTemp2[0], out temp);
                        int pow = MiscFunc.parseInt(strTemp2[1]);
                        BigInteger num2 = num1 + BigInteger.Pow(new BigInteger(2), pow);

                        if (type == temp && num1 <= numIP && numIP <= num2)
                            return true;
                    }
                    else
                    {
                        string Addresses = GetSpecialNet(strTemp[0].Trim(), NicInfo);
                        if (Addresses != null)
                        {
                            if (Addresses.Length > 0)
                                return MatchAddress(Address, Addresses);
                        }
                        else
                        {
                            int temp;
                            BigInteger num1 = NetFunc.IpStrToInt(strTemp[0], out temp);
                            if (type == temp && num1 == numIP)
                                return true;
                        }
                    }
                }
                else if (strTemp.Length == 2)
                {
                    int temp;
                    BigInteger num1 = NetFunc.IpStrToInt(strTemp[0], out temp);
                    BigInteger num2 = NetFunc.IpStrToInt(strTemp[1], out temp);
                    if (type == temp && num1 <= numIP && numIP <= num2)
                        return true;
                }
            }
            return false;
        }

        public static bool MatchEndpoint(string Addresses, string Ports, IPAddress Address, UInt16 Port, NetworkMonitor.AdapterInfo NicInfo = null)
        {
            if (!FirewallRule.IsEmptyOrStar(Ports) && !MatchPort(Port, Ports))
                return false;
            if (Address != null && !FirewallRule.IsEmptyOrStar(Addresses) && !MatchAddress(Address, Addresses, NicInfo))
                return false;
            return true;
        }

        public static List<string> CopyStrIPs(ICollection<IPAddress> IPs)
        {
            List<string> StrIPs = new List<string>();
            if (IPs != null)
            {
                foreach (var ip in IPs)
                {
                    var _ip = new IPAddress(ip.GetAddressBytes());
                    StrIPs.Add(_ip.ToString());
                }
            }
            return StrIPs;
        }

        public static string GetSpecialNet(string SubNet, NetworkMonitor.AdapterInfo NicInfo = null)
        {
            List<string> IpRanges = new List<string>();
            if (SubNet.Equals(FirewallRule.AddrKeywordLocalSubnet, StringComparison.OrdinalIgnoreCase) || SubNet.Equals(FirewallRule.AddrKeywordIntrAnet, StringComparison.OrdinalIgnoreCase))
            {
                // todo: ceate the list base on NicInfo.Addresses
                // IPv4
                IpRanges.Add("10.0.0.0-10.255.255.255");
                IpRanges.Add("127.0.0.0-127.255.255.255"); // localhost
                IpRanges.Add("172.16.0.0-172.31.255.255");
                IpRanges.Add("192.168.0.0-192.168.255.255");
                IpRanges.Add("224.0.0.0-239.255.255.255"); // multicast

                // IPv6
                IpRanges.Add("::1"); // localhost
                IpRanges.Add("fc00::-fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"); // Unique local address
                IpRanges.Add("fe80::-fe80::ffff:ffff:ffff:ffff"); //IpRanges.Add("fe80::-febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff"); // Link-local address
                IpRanges.Add("ff00::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"); // multicast
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordIntErnet, StringComparison.OrdinalIgnoreCase))
            {
                // todo: ceate the list base on NicInfo.Addresses
                // IPv4
                IpRanges.Add("0.0.0.0-9.255.255.255");
                // 10.0.0.0 - 10.255.255.255
                IpRanges.Add("11.0.0.0-126.255.255.255");
                // 127.0.0.0 - 127.255.255.255 // localhost
                IpRanges.Add("128.0.0.0-172.15.255.255");
                // 172.16.0.0 - 172.31.255.255
                IpRanges.Add("172.32.0.0-192.167.255.255");
                // 192.168.0.0 - 192.168.255.255
                IpRanges.Add("192.169.0.0-223.255.255.255");
                // 224.0.0.0-239.255.255.255 // multicast
                IpRanges.Add("240.0.0.0-255.255.255.255");

                // ipv6
                //"::1" // localhost
                IpRanges.Add("::2-fc00::");
                //"fc00::-fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" // Unique local address
                IpRanges.Add("fc00::ffff:ffff:ffff:ffff-fe80::");
                //"fe80::-fe80::ffff:ffff:ffff:ffff" // fe80::-febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff // Link-local address 
                IpRanges.Add("fe80::ffff:ffff:ffff:ffff-ff00::");
                //"ff00::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" // multicast
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordDNS, StringComparison.OrdinalIgnoreCase))
            {
                IpRanges = CopyStrIPs(NicInfo?.DnsAddresses);
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordDHCP, StringComparison.OrdinalIgnoreCase))
            {
                IpRanges = CopyStrIPs(NicInfo?.DhcpServerAddresses);
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordWINS, StringComparison.OrdinalIgnoreCase))
            {
                IpRanges = CopyStrIPs(NicInfo?.WinsServersAddresses);
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordDefaultGateway, StringComparison.OrdinalIgnoreCase))
            {
                IpRanges = CopyStrIPs(NicInfo?.GatewayAddresses);
            }
            else if (SubNet.Equals(FirewallRule.AddrKeywordRmtIntrAnet, StringComparison.OrdinalIgnoreCase)
                  || SubNet.Equals(FirewallRule.AddrKeywordPly2Renders, StringComparison.OrdinalIgnoreCase)
                  || SubNet.Equals(FirewallRule.AddrKeywordCaptivePortal, StringComparison.OrdinalIgnoreCase))
                ; // todo:
            else
                return null;
            return string.Join(",", IpRanges.ToArray());
        }
    }
}