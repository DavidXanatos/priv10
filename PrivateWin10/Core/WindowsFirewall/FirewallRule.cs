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

        public const string PortKeywordIpTlsIn = "IPHTTPS"; //"IPHTTPSIn"
        public const string PortKeywordIpTlsOut = "IPHTTPS"; //"IPHTTPSOut"
        public const string PortKeywordPly2Disc = "Ply2Disc";
        public const string PortKeywordTeredo = "Teredo";
        public const string PortKeywordRpc = "RPC";
        public const string PortKeywordRpcEp = "RPC-EPMap";
        public const string PortKeywordDhcp = "DHCP";
        public const string PortKeywordMDns = "mDNS";
        public const string PortKeywordCortan = "Cortana";
        public const string PortKeywordProximalTcpCdn = "ProximalTcpCdn"; // wtf is that?

        public const string AddrKeywordDefaultgateway = "Defaultgateway";
        public const string AddrKeywordDHCP = "DHCP";
        public const string AddrKeywordDNS = "DNS";
        public const string AddrKeywordWINS = "WINS";
        public const string AddrKeywordIntranet = "Intranet"; // supported on Windows versions 1809+
        public const string AddrKeywordRmttranet = "Rmttranet"; // supported on Windows versions 1809+
        public const string AddrKeywordInternet = "Internet"; // supported on Windows versions 1809+
        public const string AddrKeywordPly2Renders = "Ply2Renders"; // supported on Windows versions 1809+
        public const string AddrKeywordLocalSubnet = "LocalSubnet"; // indicates any local address on the local subnet.
        public const string AddrKeywordCaptivePortal = "CaptivePortal";



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

        public bool MatchRemoteEndpoint(IPAddress Address, UInt16 Port)
        {
            return MatchEndpoint(RemoteAddresses, RemotePorts, Address, Port);
        }

        public bool MatchLocalEndpoint(IPAddress Address, UInt16 Port)
        {
            return MatchEndpoint(LocalAddresses, LocalPorts, Address, Port);
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
            // todo: xxx some rule port values are strings :(
            foreach (string range in strPorts.Split(','))
            {
                string[] strTemp = range.Split('-');
                if (strTemp.Length == 1)
                {
                    if (MiscFunc.parseInt(strTemp[0]) == numPort)
                        return true;
                }
                else if (strTemp.Length == 2)
                {
                    if (MiscFunc.parseInt(strTemp[0]) <= numPort && numPort <= MiscFunc.parseInt(strTemp[1]))
                        return true;
                }
            }
            return false;
        }

        public static bool MatchAddress(IPAddress Address, string strRanges)
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
                    else if (FirewallRule.SpecialAddresses.Contains(strTemp[0].Trim(), StringComparer.OrdinalIgnoreCase))
                        return MatchAddress(Address, GetSpecialNet(strTemp[0].Trim()));
                    else
                    {
                        int temp;
                        BigInteger num1 = NetFunc.IpStrToInt(strTemp[0], out temp);
                        if (type == temp && num1 == numIP)
                            return true;
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

        public static bool MatchEndpoint(string Addresses, string Ports, IPAddress Address, UInt16 Port)
        {
            if (!IsEmptyOrStar(Ports) && !MatchPort(Port, Ports))
                return false;
            if (Address != null && !IsEmptyOrStar(Addresses) && !MatchAddress(Address, Addresses))
                return false;
            return true;
        }

        public static string GetSpecialNet(string SubNet)
        {
            List<string> IpRanges = new List<string>();
            if (SubNet.Equals("LocalSubnet", StringComparison.OrdinalIgnoreCase))
            {
                // IPv4
                IpRanges.Add("10.0.0.0-10.255.255.255");
                IpRanges.Add("127.0.0.0-127.255.255.255");
                IpRanges.Add("172.16.0.0-172.31.255.255");
                IpRanges.Add("192.168.0.0-192.168.255.255");

                // IPv6
                IpRanges.Add("::1"); // Note: if this is present windows firewall does not accept this range
                IpRanges.Add("fc00::-fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
                IpRanges.Add("fe80::-febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
            }
            // else // todo
            return string.Join(",", IpRanges.ToArray());
        }
    }
}