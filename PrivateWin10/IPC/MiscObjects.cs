using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrivateAPI;
using WinFirewallAPI;

namespace PrivateWin10
{
    [Flags]
    public enum NameSources : int
    {
        None            = 0x00,
        ReverseDns      = 0x01,
        CachedQuery     = 0x02, // good
        CapturedQuery   = 0x04 // best
    }
    
    [Serializable()]
    [DataContract(Name = "WithHost", Namespace = "http://schemas.datacontract.org/")]
    public class WithHost
    {
        [DataMember()]
        public NameSources RemoteHostNameSource = NameSources.None;
        [DataMember()]
        public string RemoteHostName = "";
        [DataMember()]
        public string RemoteHostNameAlias = "";
        
        public bool Update(WithHost other)
        {
            if (MiscFunc.Equals(RemoteHostName, other.RemoteHostName))
                return false;
            RemoteHostNameSource = other.RemoteHostNameSource;
            RemoteHostName = other.RemoteHostName;
            return true;
        }

        public bool HasHostName()
        {
            return RemoteHostNameSource != NameSources.None;
        }

        public string GetHostName()
        {
            return RemoteHostName + (RemoteHostNameAlias.Length > 0 ? " -> " + RemoteHostNameAlias : "");
        }
    }


    [Serializable()]
    [DataContract(Name = "FirewallEvent", Namespace = "http://schemas.datacontract.org/")]
    public class FirewallEvent : EventArgs
    {
        [DataMember()]
        public int ProcessId;
        [DataMember()]
        public string ProcessFileName;

        [DataMember()]
        public FirewallRule.Actions Action;

        [DataMember()]
        public UInt32 Protocol;
        [DataMember()]
        public FirewallRule.Directions Direction;
        [DataMember()]
        public IPAddress LocalAddress;
        [DataMember()]
        public UInt16 LocalPort;
        [DataMember()]
        public IPAddress RemoteAddress;
        [DataMember()]
        public UInt16 RemotePort;

        [DataMember()]
        public DateTime TimeStamp;
    }

    public class FirewallMonitor
    {
        public enum Auditing : int
        {
            Off = 0,
            Blocked = 1,
            Allowed = 2,
            All = 3
        }
    }

    public class FirewallManager
    {
        public enum FilteringModes
        {
            Unknown = 0,
            NoFiltering = 1,
            BlackList = 2,
            WhiteList = 3,
            //BlockAll = 4,
        }

        public const string RuleGroup = "PrivateWin10";
        public const string RulePrefix = "priv10";
        public const string TempRulePrefix = "priv10temp";
        public const string CustomName = "Custom Rule";
        public const string AllowAllName = "Allow All Network";
        public const string BlockAllName = "Block All Network";
        public const string AllowLan = "Allow Local Subnet";
        public const string BlockInet = "Block Internet";

        public static string MakeRuleName(string action, bool temp, string descr)
        {
            return (temp ? TempRulePrefix : RulePrefix) + " - " + (descr != null ? descr + " - " : "") + action;
        }
    }


    public class FirewallGuard
    {

        public enum Mode : int
        {
            Alert = 0,
            Disable = 1,
            Fix = 2
        }
    }




    [Serializable()]
    [DataContract(Name = "DomainFilter", Namespace = "http://schemas.datacontract.org/")]
    public class DomainFilter
    {
        [DataMember()]
        public bool Enabled = true;
        public enum Formats
        {
            Plain = 0,
            RegExp,
            WildCard
        }
        [DataMember()]
        public Formats Format = Formats.Plain;
        [DataMember()]
        public string Domain = "";
        [DataMember()]
        public int HitCount = 0;
        [DataMember()]
        public DateTime? LastHit = null;
    }

    [Serializable()]
    [DataContract(Name = "DomainBlocklist", Namespace = "http://schemas.datacontract.org/")]
    public class DomainBlocklist
    {
        [DataMember()]
        public bool Enabled = true;
        [DataMember()]
        public string Url = "";
        [DataMember()]
        public DateTime? LastUpdate = null;
        [DataMember()]
        public int EntryCount = 0;
        [DataMember()]
        public string Status = "";
        [DataMember()]
        public string FileName = "";
    }

    public class DnsBlockList
    {
        public enum Lists
        {
            Undefined = 0,
            Whitelist,
            Blacklist,
        }

        public static readonly string[] DefaultLists = {
            "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
            "http://mirror1.malwaredomains.com/files/justdomains",
            "http://sysctl.org/cameleon/hosts",
            "https://s3.amazonaws.com/lists.disconnect.me/simple_tracking.txt",
            "https://s3.amazonaws.com/lists.disconnect.me/simple_ad.txt",
            "https://hosts-file.net/ad_servers.txt"
        };

    }
    

    public class DnsApi
    {
        [Flags]
        public enum DnsRecordType : ushort
        {
            A       = 0x0001,
            CNAME   = 0x0005,
            AAAA    = 0x001c,
            PTR     = 0x000c,
            SRV     = 0x0021,
            MX      = 0x000f,
            //DNAME = 0x0027,
            ANY     = 0x00FF
        }
    }


    public class DnsCacheMonitor 
    {
        [Serializable()]
        [DataContract(Name = "DnsCacheEntry", Namespace = "http://schemas.datacontract.org/")]
        public class DnsCacheEntry
        {
            [DataMember()]
            public string HostName;
            [DataMember()]
            public DnsApi.DnsRecordType RecordType = DnsApi.DnsRecordType.ANY;
            [DataMember()]
            public IPAddress Address;
            [DataMember()]
            public String ResolvedString;
            public enum States
            {
                Unknown = 0,
                Resolved,
                Blocked,
                NotFound
            }
            [DataMember()]
            public States State = States.Unknown;
            [DataMember()]
            public DateTime TimeStamp = DateTime.Now;
            [DataMember()]
            public DateTime ExpirationTime;
            public DateTime GetAge()
            {
                DateTime CurrentTime = DateTime.Now;
                if (CurrentTime <= ExpirationTime)
                    return CurrentTime;
                return ExpirationTime;
            }
            public int GetTimeLeft()
            {
                DateTime CurrentTime = DateTime.Now;
                if (CurrentTime <= ExpirationTime)
                    return (int)(ExpirationTime - CurrentTime).TotalSeconds;
                return 0;
            }
            public int GetTTL()
            {
                if (TimeStamp <= ExpirationTime)
                    return (int)(ExpirationTime - TimeStamp).TotalSeconds;
                return 0;
            }
        };
    }



    public class DnsProxyServer
    {
        public  const int DEFAULT_PORT = 53;
    }

    public class Priv10Engine //: IDisposable
    {
        public delegate void FX();

        [Serializable()]
        public class FwEventArgs : EventArgs
        {
            public Guid guid;
            public Program.LogEntry entry;
            public ProgramID progID;
            public List<String> services = null;
            public bool update;
        }

        [Serializable()]
        public class UpdateArgs : EventArgs
        {
            public Guid guid;
            public enum Types
            {
                ProgSet = 0,
                Rules
            }
            public Types type;
        }

        [Serializable()]
        public class ChangeArgs : EventArgs
        {
            public Program prog;
            public FirewallRuleEx rule;
            public Priv10Engine.RuleEventType type;
            public Priv10Engine.RuleFixAction action;
        }
        
        public enum RuleEventType
        {
            Changed = 0,
            Added,
            Removed,
            UnChanged, // role was changed to again match the aproved configuration
        }

        public enum RuleFixAction
        {
            None = 0,
            Restored,
            Disabled,
            Updated,
            Deleted
        }
     
        
        public enum ApprovalMode
        {
            ApproveCurrent = 0,
            RestoreRules,
            ApproveChanges
        }

        public enum CleanupMode
        {
            RemoveExpired = 0,
            RemoveTemporary,
            RemoveDuplicates,
            RemoveDuplicatesAllow,
            RemoveDuplicatesBlock,
        }
    }


    public class NetworkMonitor
    {
        
        public class AdapterInfo
        {
            public FirewallRule.Profiles Profile = FirewallRule.Profiles.Undefined;
            public FirewallRule.Interfaces Type = FirewallRule.Interfaces.All;

            public ICollection<UnicastIPAddressInformation> Addresses = null;

            public ICollection<IPAddress> GatewayAddresses = null;
            public ICollection<IPAddress> DnsAddresses = null;
            public ICollection<IPAddress> DhcpServerAddresses = null;
            public ICollection<IPAddress> WinsServersAddresses = null;
        }
        
    }
}