using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Numerics;


public class NetFunc
{
    public static BigInteger IpToInt(IPAddress ip)
    {
        List<Byte> ipFormat = ip.GetAddressBytes().ToList();
        ipFormat.Reverse();
        ipFormat.Add(0);
        return new BigInteger(ipFormat.ToArray());
    }

    public static BigInteger IpStrToInt(string strIP, out int type)
    {
        IPAddress ip;
        if (!IPAddress.TryParse(strIP, out ip))
        {
            type = 0;
            return BigInteger.Zero;
        }
        byte[] bytes = ip.GetAddressBytes();
        if (bytes.Length == 4)
            type = 4;
        else
            type = 6;
        List<Byte> ipFormat = bytes.ToList();
        ipFormat.Reverse();
        ipFormat.Add(0);
        return new BigInteger(ipFormat.ToArray());
    }

    public static BigInteger MaxIPofType(int type)
    {
        IPAddress ip = (type == 4) ? IPAddress.Parse("255.255.255.255") : IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
        List<Byte> ipFormat = ip.GetAddressBytes().ToList();
        ipFormat.Reverse();
        ipFormat.Add(0);
        return new BigInteger(ipFormat.ToArray());
    }

    public enum KnownProtocols
    {
        Min = 0,
        Max = 142,
        Any = 256
    }

    /*public static string Protocol2Str(int Protocol, string unkStr = "Unknow")
    {
        switch (Protocol)
        {
            case 0: return "HOPOPT (IPv6 Hop-by-Hop Option)";
            case 1: return "ICMP (Internet Control Message Protocol)";
            case 2: return "IGMP (Internet Group Management Protocol)";
            case 3: return "GGP (Gateway-to-Gateway)";
            case 4: return "IP (IP in IP (encapsulation))";
            case 5: return "Stream";
            case 6: return "TCP (Transmission Control Protocol)";
            case 7: return "CBT (Core Based Trees)";
            case 8: return "EGP (Exterior Gateway Protocol)";
            case 9: return "IGP (any private interior gateway)";
            case 10: return "BBN-RCC-MON (BBN RCC Monitoring)";
            case 11: return "NVP-II (Network Voice Protocol)";
            case 12: return "PUP";
            case 13: return "ARGUS";
            case 14: return "EMCON";
            case 15: return "XNET (Cross Net Debugger)";
            case 16: return "CHAOS";
            case 17: return "UDP (User Datagram Protocol)";
            case 18: return "Multiplexing";
            case 19: return "DCN-MEAS (DCN Measurement Subsystems)";
            case 20: return "HMP (Host Monitoring)";
            case 21: return "PRM (Packet Radio Measurement)";
            case 22: return "XNS-IDP (XEROX NS IDP)";
            case 23: return "TRUNK-1";
            case 24: return "TRUNK-2";
            case 25: return "LEAF-1";
            case 26: return "LEAF-2";
            case 27: return "RDP (Reliable Data Protocol)";
            case 28: return "IRTP (Internet Reliable Transaction Protocol)";
            case 29: return "ISO-TP4 (ISO Transport Protocol Class 4)";
            case 30: return "NETBLT (Bulk Data Transfer Protocol)";
            case 31: return "MFE-NSP (MFE Network Services Protocol)";
            case 32: return "MERIT-INP (MERIT Internodal Protocol)";
            case 33: return "DCCP (Datagram Congestion Control Protocol)";
            case 34: return "3PC (Third Party Connect Protocol)";
            case 35: return "IDPR (Inter-Domain Policy Routing Protocol)";
            case 36: return "XTP";
            case 37: return "DDP (Datagram Delivery Protocol)";
            case 38: return "IDPR-CMTP (IDPR Control Message Transport Proto)";
            case 39: return "TP++ (TP++ Transport Protocol)";
            case 40: return "IL (IL Transport Protocol)";
            case 41: return "Verkapselung von IPv6- in IPv4-Pakete";
            case 42: return "SDRP (Source Demand Routing Protocol)";
            case 43: return "IPv6-Route (Routing Header for IPv6)";
            case 44: return "IPv6-Frag (Fragment Header for IPv6)";
            case 45: return "IDRP (Inter-Domain Routing Protocol)";
            case 46: return "RSVP (Reservation Protocol)";
            case 47: return "GRE (Generic Routing Encapsulation)";
            case 48: return "MHRP (Mobile Host Routing Protocol)";
            case 49: return "BNA";
            case 50: return "ESP (Encapsulating Security Payload)";
            case 51: return "AH (Authentication Header)";
            case 52: return "I-NLSP (Integrated Net Layer Security TUBA)";
            case 53: return "SWIPE (IP with Encryption)";
            case 54: return "NARP (NBMA Address Resolution Protocol)";
            case 55: return "MOBILE (IP Mobility)";
            case 56: return "TLSP (Transport Layer Security Protocol)";
            case 57: return "SKIP";
            case 58: return "IPv6-ICMP (ICMP for IPv6)";
            case 59: return "IPv6-NoNxt (Kein nächster Header für IPv6)";
            case 60: return "IPv6-Opts (Destination Options for IPv6)";
            case 61: return "Jedes Host-interne Protokoll";
            case 62: return "CFTP";
            case 63: return "Jedes lokale Netz";
            case 64: return "SAT-EXPAK (SATNET and Backroom EXPAK)";
            case 65: return "KRYPTOLAN";
            case 66: return "RVD (MIT Remote Virtual Disk Protocol)";
            case 67: return "IPPC (Internet Pluribus Packet Core)";
            case 68: return "Jedes verteilte Dateisystem";
            case 69: return "SAT-MON (SATNET Monitoring)";
            case 70: return "VISA";
            case 71: return "IPCV (Internet Packet Core Utility)";
            case 72: return "CPNX (Computer Protocol Network Executive)";
            case 73: return "CPHB (Computer Protocol Heart Beat)";
            case 74: return "WSN (Wang Span Network)";
            case 75: return "PVP (Packet Video Protocol)";
            case 76: return "BR-SAT-MON (Backroom SATNET Monitoring)";
            case 77: return "SUN-ND (SUN ND PROTOCOL-Temporary)";
            case 78: return "WB-MON (WIDEBAND Monitoring)";
            case 79: return "WB-EXPAK (WIDEBAND EXPAK)";
            case 80: return "ISO-IP (ISO Internet Protocol)";
            case 81: return "VMTP";
            case 82: return "SECURE-VMTP";
            case 83: return "VINES";
            case 84: return "TTP";
            case 85: return "NSFNET-IGP (NSFNET-IGP)";
            case 86: return "DGP (Dissimilar Gateway Protocol)";
            case 87: return "TCF";
            case 88: return "EIGRP";
            case 89: return "OSPF";
            case 90: return "Sprite-RPC (Sprite RPC Protocol)";
            case 91: return "LARP (Locus Address Resolution Protocol)";
            case 92: return "MTP (Multicast Transport Protocol)";
            case 93: return "AX.25 (AX.25 Frames)";
            case 94: return "IPIP (IP-within-IP Encapsulation Protocol)";
            case 95: return "MICP (Mobile Internetworking Control Pro.)";
            case 96: return "SCC-SP (Semaphore Communications Sec. Pro.)";
            case 97: return "ETHERIP (Ethernet-within-IP Encapsulation)";
            case 98: return "ENCAP (Encapsulation Header)";
            case 99: return "Jeder private Verschlüsselungsentwurf";
            case 100: return "GMTP";
            case 101: return "IFMP (Ipsilon Flow Management Protocol)";
            case 102: return "PNNI (over IP)";
            case 103: return "PIM (Protocol Independent Multicast)";
            case 104: return "ARIS";
            case 105: return "SCPS";
            case 106: return "QNX";
            case 107: return "A/N (Active Networks)";
            case 108: return "IPComp (IP Payload Compression Protocol)";
            case 109: return "SNP (Sitara Networks Protocol)";
            case 110: return "Compaq-Peer (Compaq Peer Protocol)";
            case 111: return "IPX-in-IP (IPX in IP)";
            case 112: return "VRRP (Virtual Router Redundancy Protocol)";
            case 113: return "PGM (PGM Reliable Transport Protocol)";
            case 114: return "any 0-hop protocol";
            case 115: return "L2TP (Layer Two Tunneling Protocol)";
            case 116: return "DDX (D-II Data Exchange (DDX))";
            case 117: return "IATP (Interactive Agent Transfer Protocol)";
            case 118: return "STP (Schedule Transfer Protocol)";
            case 119: return "SRP (SpectraLink Radio Protocol)";
            case 120: return "UTI";
            case 121: return "SMP (Simple Message Protocol)";
            case 122: return "SM";
            case 123: return "PTP (Performance Transparency Protocol)";
            case 124: return "ISIS over IPv4";
            case 125: return "FIRE";
            case 126: return "CRTP (Combat Radio Transport Protocol)";
            case 127: return "CRUDP (Combat Radio User Datagram)";
            case 128: return "SSCOPMCE";
            case 129: return "IPLT";
            case 130: return "SPS (Secure Packet Shield)";
            case 131: return "PIPE (Private IP Encapsulation within IP)";
            case 132: return "SCTP (Stream Control Transmission Protocol)";
            case 133: return "FC (Fibre Channel)";
            case 134: return "RSVP-E2E-IGNORE";
            case 135: return "Mobility Header";
            case 136: return "UDPLite";
            case 137: return "MPLS-in-IP";
            case 138: return "manet (MANET Protocols)";
            case 139: return "HIP (Host Identity Protocol)";
            case 140: return "Shim6 (Shim6 Protocol)";
            case 141: return "WESP (Wrapped Encapsulating Security Payload)";
            case 142: return "ROHC (Robust Header Compression)";
            case 256: return "Unspecifyed";
            default: return unkStr;
        }
    }*/

    public static string Protocol2Str(int Protocol)
    {
        switch (Protocol)
        {
            case 0: return "HOPOPT";
            case 1: return "ICMP";
            case 2: return "IGMP";
            case 3: return "GGP";
            case 4: return "IP";
            //case 5: return "Stream";
            case 6: return "TCP";
            case 7: return "CBT";
            case 8: return "EGP";
            case 9: return "IGP";
            case 10: return "BBN-RCC-MON";
            case 11: return "NVP-II";
            case 12: return "PUP";
            case 13: return "ARGUS";
            case 14: return "EMCON";
            case 15: return "XNET";
            case 16: return "CHAOS";
            case 17: return "UDP";
            //case 18: return "Multiplexing";
            case 19: return "DCN-MEAS";
            case 20: return "HMP";
            case 21: return "PRM";
            case 22: return "XNS-IDP";
            case 23: return "TRUNK-1";
            case 24: return "TRUNK-2";
            case 25: return "LEAF-1";
            case 26: return "LEAF-2";
            case 27: return "RDP";
            case 28: return "IRTP";
            case 29: return "ISO-TP4";
            case 30: return "NETBLT";
            case 31: return "MFE-NSP";
            case 32: return "MERIT-INP";
            case 33: return "DCCP";
            case 34: return "3PC";
            case 35: return "IDPR";
            case 36: return "XTP";
            case 37: return "DDP";
            case 38: return "IDPR-CMTP";
            case 39: return "TP++";
            case 40: return "IL";
            ///case 41: return "Verkapselung von IPv6- in IPv4-Pakete";
            case 42: return "SDRP";
            case 43: return "IPv6-Route";
            case 44: return "IPv6-Frag";
            case 45: return "IDRP";
            case 46: return "RSVP";
            case 47: return "GRE";
            case 48: return "MHRP";
            case 49: return "BNA";
            case 50: return "ESP";
            case 51: return "AH";
            case 52: return "I-NLSP";
            case 53: return "SWIPE";
            case 54: return "NARP";
            case 55: return "MOBILE";
            case 56: return "TLSP";
            case 57: return "SKIP";
            case 58: return "IPv6-ICMP";
            case 59: return "IPv6-NoNxt";
            case 60: return "IPv6-Opts";
            //case 61: return "Jedes Host-interne Protokoll";
            case 62: return "CFTP";
            //case 63: return "Jedes lokale Netz";
            case 64: return "SAT-EXPAK";
            case 65: return "KRYPTOLAN";
            case 66: return "RVD";
            case 67: return "IPPC";
            //case 68: return "Jedes verteilte Dateisystem";
            case 69: return "SAT-MON";
            case 70: return "VISA";
            case 71: return "IPCV";
            case 72: return "CPNX";
            case 73: return "CPHB";
            case 74: return "WSN";
            case 75: return "PVP";
            case 76: return "BR-SAT-MON";
            case 77: return "SUN-ND";
            case 78: return "WB-MON";
            case 79: return "WB-EXPAK";
            case 80: return "ISO-IP";
            case 81: return "VMTP";
            case 82: return "SECURE-VMTP";
            case 83: return "VINES";
            case 84: return "TTP";
            case 85: return "NSFNET-IGP";
            case 86: return "DGP";
            case 87: return "TCF";
            case 88: return "EIGRP";
            case 89: return "OSPF";
            case 90: return "Sprite-RPC";
            case 91: return "LARP";
            case 92: return "MTP";
            case 93: return "AX.25";
            case 94: return "IPIP";
            case 95: return "MICP";
            case 96: return "SCC-SP";
            case 97: return "ETHERIP";
            case 98: return "ENCAP";
            //case 99: return "Jeder private Verschlüsselungsentwurf";
            case 100: return "GMTP";
            case 101: return "IFMP";
            case 102: return "PNNI";
            case 103: return "PIM";
            case 104: return "ARIS";
            case 105: return "SCPS";
            case 106: return "QNX";
            case 107: return "A/N";
            case 108: return "IPComp";
            case 109: return "SNP";
            case 110: return "Compaq-Peer";
            case 111: return "IPX-in-IP";
            case 112: return "VRRP";
            case 113: return "PGM";
            case 114: return "any 0-hop protocol";
            case 115: return "L2TP";
            case 116: return "DDX";
            case 117: return "IATP";
            case 118: return "STP";
            case 119: return "SRP";
            case 120: return "UTI";
            case 121: return "SMP";
            case 122: return "SM";
            case 123: return "PTP";
            case 124: return "ISIS";
            case 125: return "FIRE";
            case 126: return "CRTP";
            case 127: return "CRUDP";
            case 128: return "SSCOPMCE";
            case 129: return "IPLT";
            case 130: return "SPS";
            case 131: return "PIPE";
            case 132: return "SCTP";
            case 133: return "FC";
            case 134: return "RSVP-E2E-IGNORE";
            case 135: return "Mobility Header";
            case 136: return "UDPLite";
            case 137: return "MPLS-in-IP";
            case 138: return "manet";
            case 139: return "HIP";
            case 140: return "Shim6";
            case 141: return "WESP";
            case 142: return "ROHC";
            case 256: return "???";
            default: return "#" + Protocol.ToString();
        }
    }

    public static readonly Dictionary<int, string> KnownIcmp4Types = new Dictionary<int, string>() {
        {0,"Echo Reply"},
        {3,"Destination Unreachable"},
        {4,"Source Quench"},
        {5,"Redirect"},
        {8,"Echo Request"},
        {9,"Router Advertisement"},
        {10,"Router Solicitation"},
        {11,"Time Exceeded"},
        {12,"Parameter Problem"},
        {13,"Timestamp (erleichtert die Zeitsynchronisation)"},
        {14,"Timestamp Reply"},
        {15,"Information Request"},
        {16,"Information Reply"},
        {17,"Address Mask Request"},
        {18,"Address Mask Reply"},
        {19,"Reserved (for Security)"},
        {30,"Traceroute"},
        {31,"Datagram Conversion Error"},
        {32,"Mobile Host Redirect"},
        {33,"Ursprünglich IPv6 Where-Are-You (ersetzt durch ICMPv6)"},
        {34,"Ursprünglich IPv6 I-Am-Here (ersetzt durch ICMPv6)"},
        {35,"Mobile Registration Request"},
        {36,"Mobile Registration Reply"},
        {37,"Domain Name Request"},
        {38,"Domain Name Reply"},
        {39,"SKIP"},
        {40,"Photuris"},
        {41,"ICMP messages utilized by experimental mobility protocols such as Seamoby"},
    };

    public static readonly Dictionary<int, string> KnownIcmp6Types = new Dictionary<int, string>() {
        {1, "Destination Unreachable"},
        {2, "Packet Too Big"},
        {3, "Time Exceeded"},
        {4, "Parameter Problem"},
        {128, "Echo Request"},
        {129, "Echo Reply"},
        {130, "Multicast Listener Query"},
        {131, "Version 1 Multicast Listener Report"},
        {132, "Multicast Listener Done"},
        {133, "Router Solicitation"},
        {134, "Router Advertisement"},
        {135, "Neighbor Solicitation"},
        {136, "Neighbor Advertisement"},
        {137, "Redirect"},
        {138, "Router Renumbering"},
        {139, "ICMP Node Information Query"},
        {140, "ICMP Node Information Response"},
        {141, "Inverse Neighbor Discovery Solicitation Message"},
        {142, "Inverse Neighbor Discovery Advertisement Message"},
        {143, "Version 2 Multicast Listener Report"},
        {144, "Home Agent Address Discovery Request Message"},
        {145, "Home Agent Address Discovery Reply Message"},
        {146, "Mobile Prefix Solicitation"},
        {147, "Mobile Prefix Advertisement"},
        {148, "Certification Path Solicitation Message"},
        {149, "Certification Path Advertisement Message"},
        {150, "ICMP messages utilized by experimental mobility protocols such as Seamoby"},
        {151, "Multicast Router Advertisement"},
        {152, "Multicast Router Solicitation"},
        {153, "Multicast Router Termination"},
        {155, "RPL Control Message"},
    };

    public static bool IsLocalHost(string addrStr)
    {
        IPAddress addr = IPAddress.Parse(addrStr);
        return (addr.Equals(IPAddress.Loopback) || addr.Equals(IPAddress.IPv6Loopback));
    }

    public static bool IsMultiCast(string addrStr)
    {
        IPAddress addr = IPAddress.Parse(addrStr);
        // ipv4 multicast: 224.0.0.0 to 239.255.255.255
        // ipv6 multicast: ff00:: to ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff
        if (addr.IsIPv6Multicast)
            return true;
        byte[] addressBytes = addr.GetAddressBytes();
        if (addressBytes.Length == 4)
            return addressBytes[0] >= 224 && addressBytes[0] <= 239;
        return false;
    }

    public static string GetNonLocalNet()
    {
        List<string> IpRanges = new List<string>();

        // IPv4
        IpRanges.Add("0.0.0.0-9.255.255.255");
        // 10.0.0.0 - 10.255.255.255
        IpRanges.Add("11.0.0.0-126.255.255.255");
        // 127.0.0.0 - 127.255.255.255
        IpRanges.Add("128.0.0.0-172.15.255.255");
        // 172.16.0.0 - 172.31.255.255
        IpRanges.Add("172.32.0.0-192.167.255.255");
        // 192.168.0.0 - 192.168.255.255
        IpRanges.Add("192.169.0.0-255.255.255.255");

        // IPv6
        // ::1
        IpRanges.Add("::2-fbff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
        // fc00:: - fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff             - Unique local address
        IpRanges.Add("fe00::-fe7f:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
        // fe80:: - febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff             - Link-local address
        IpRanges.Add("fec0::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");

        return string.Join(",", IpRanges.ToArray());
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
            IpRanges.Add("::1");
            IpRanges.Add("fc00::-fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
            IpRanges.Add("fe80::-febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
        }
        // else // todo
        return string.Join(",", IpRanges.ToArray());
    }
    
}

// Why is that not defined in the apropriate headers?
enum NET_FW_EDGE_TRAVERSAL_TYPE_
{
    NET_FW_EDGE_TRAVERSAL_TYPE_DENY = 0,
    NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW, // 1
    NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP, // 2
    NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER // 3
}