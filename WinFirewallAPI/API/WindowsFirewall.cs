using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 169 // disable warning CS0169: The field 'Test.unused' is never used


namespace WinFirewallAPI
{
    public static class WindowsFirewall
    {
        // based on informations form https://github.com/processhacker/plugins-extra/blob/master/FirewallMonitorPlugin/wf.h

        //http://msdn.microsoft.com/en-us/library/cc231461.aspx
        public enum FW_BINARY_VERSION : ushort
        {
            //FW_BINARY_VERSION_VISTA     = 0x0200,
            //FW_BINARY_VERSION_SERVER2K8 = 0x0201,
            FW_BINARY_VERSION_SEVEN = 0x020A,
            FW_BINARY_VERSION_WIN8 = 0x0214,

            FW_BINARY_VERSION_WIN10 = 0x0216,
            FW_BINARY_VERSION_THRESHOLD = 0x0218, // 1507
            FW_BINARY_VERSION_THRESHOLD2 = 0x0219, // 1511
            FW_BINARY_VERSION_REDSTONE1 = 0x021A, // 1607
            FW_BINARY_VERSION_170x = 0x021B, // Redstone 2 & Redstone 3
            FW_BINARY_VERSION_180x = 0x021C, // Redstone 4 & Redstone 5
            FW_BINARY_VERSION_19Hx = 0x021E
        }

        public const int ERROR_SUCCESS = 0x0;
        public const int ERROR_RULE_NOT_FOUND = 0x2;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Firewall Policy

        public enum FW_POLICY_ACCESS_RIGHT
        {
            FW_POLICY_ACCESS_RIGHT_INVALID,
            FW_POLICY_ACCESS_RIGHT_READ,
            FW_POLICY_ACCESS_RIGHT_READ_WRITE,

            FW_POLICY_ACCESS_RIGHT_MAX
        }

        [Flags]
        public enum FW_POLICY_STORE_FLAGS
        {
            FW_POLICY_STORE_FLAGS_NONE = 0,
            FW_POLICY_STORE_FLAGS_DELETE_DYNAMIC_RULES_AFTER_CLOSE = 1,

            FW_POLICY_STORE_FLAGS_MAX = 2
        }

        public enum FW_STORE_TYPE
        {
            FW_STORE_TYPE_INVALID,
            FW_STORE_TYPE_GP_RSOP,
            FW_STORE_TYPE_LOCAL,
            FW_STORE_TYPE_NOT_USED_VALUE_3,
            FW_STORE_TYPE_NOT_USED_VALUE_4,
            FW_STORE_TYPE_DYNAMIC,
            FW_STORE_TYPE_GPO,
            FW_STORE_TYPE_DEFAULTS,
            FW_STORE_TYPE_NOT_USED_VALUE_8,
            FW_STORE_TYPE_NOT_USED_VALUE_9,
            FW_STORE_TYPE_NOT_USED_VALUE_10,
            FW_STORE_TYPE_NOT_USED_VALUE_11,

            FW_STORE_TYPE_MAX
        }


        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWOpenPolicyStore(ushort wBinaryVersion, string wszMachineOrGPO, FW_STORE_TYPE StoreType, FW_POLICY_ACCESS_RIGHT AccessRight, FW_POLICY_STORE_FLAGS dwFlags, out IntPtr phPolicy);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWClosePolicyStore(IntPtr hPolicy);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FwIsGroupPolicyEnforced(string wszMachine, uint Profiles, out uint dwEnforced);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWStatusMessageFromStatusCode(FW_RULE_STATUS StatusCode, StringBuilder pszMsg, out uint pcchMsg);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Notifications

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWChangeNotificationCreate(SafeWaitHandle hEvent, out IntPtr hNewNotifyObject);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWChangeNotificationDestroy(out IntPtr hNotifyObject);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Transactions

        public enum FW_TRANSACTIONAL_STATE
        {
            FW_TRANSACTIONAL_STATE_NONE,
            FW_TRANSACTIONAL_STATE_NO_FLUSH,
            FW_TRANSACTIONAL_STATE_MAX,
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWChangeTransactionalState(IntPtr hPolicy, FW_TRANSACTIONAL_STATE TransactionState);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWRevertTransaction(IntPtr hPolicy);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Firewall Rules

        [Flags]
        public enum FW_ENUM_RULES_FLAGS : ushort
        {
            FW_ENUM_RULES_FLAG_NONE = 0x0000,
            FW_ENUM_RULES_FLAG_RESOLVE_NAME = 0x0001,
            FW_ENUM_RULES_FLAG_RESOLVE_DESCRIPTION = 0x0002,
            FW_ENUM_RULES_FLAG_RESOLVE_APPLICATION = 0x0004,
            FW_ENUM_RULES_FLAG_RESOLVE_KEYWORD = 0x0008,
            FW_ENUM_RULES_FLAG_RESOLVE_GPO_NAME = 0x0010,
            FW_ENUM_RULES_FLAG_EFFECTIVE = 0x0020,
            FW_ENUM_RULES_FLAG_INCLUDE_METADATA = 0x0040,

            FW_ENUM_RULES_FLAG_MAX = 0x0080
        }

        public enum FW_RULE_STATUS_CLASS : uint
        {
            FW_RULE_STATUS_CLASS_OK = 0x00010000,
            FW_RULE_STATUS_CLASS_PARTIALLY_IGNORED = 0x00020000,
            FW_RULE_STATUS_CLASS_IGNORED = 0x00040000,
            FW_RULE_STATUS_CLASS_PARSING_ERROR = 0x00080000,
            FW_RULE_STATUS_CLASS_SEMANTIC_ERROR = 0x00100000,
            FW_RULE_STATUS_CLASS_RUNTIME_ERROR = 0x00200000,
            FW_RULE_STATUS_CLASS_ERROR = 0x00380000,
            FW_RULE_STATUS_CLASS_ALL = 0xFFFF0000
        }

        public struct FW_QUERY
        {
            public ushort wSchemaVersion;
            public uint dwNumEntries;
            public IntPtr pORConditions;
            public FW_RULE_STATUS Status;
        }

        public struct FW_QUERY_CONDITIONS
        {
            public uint dwNumEntries;
            public IntPtr pAndedConditions;
        }

        public enum FW_MATCH_KEY
        {
            FW_MATCH_KEY_PROFILE = 0,
            FW_MATCH_KEY_STATUS = 1,
            FW_MATCH_KEY_OBJECTID = 2,
            FW_MATCH_KEY_FILTERID = 3,
            FW_MATCH_KEY_APP_PATH = 4,
            FW_MATCH_KEY_PROTOCOL = 5,
            FW_MATCH_KEY_LOCAL_PORT = 6,
            FW_MATCH_KEY_REMOTE_PORT = 7,
            FW_MATCH_KEY_GROUP = 8,
            FW_MATCH_KEY_SVC_NAME = 9,
            FW_MATCH_KEY_DIRECTION = 10,
            FW_MATCH_KEY_LOCAL_USER_OWNER = 11,
            FW_MATCH_KEY_PACKAGE_ID = 12,
            FW_MATCH_KEY_FQBN = 13,
            FW_MATCH_KEY_COMPARTMENT_ID = 14,

            FW_MATCH_KEY_MAX = 15
        }

        public enum FW_MATCH_TYPE
        {
            FW_MATCH_TYPE_TRAFFIC_MATCH = 0,
            FW_MATCH_TYPE_EQUAL = 1,

            FW_MATCH_TYPE_MAX = 2
        }

        public enum FW_DATA_TYPE
        {
            FW_DATA_TYPE_EMPTY,
            FW_DATA_TYPE_UINT8,
            FW_DATA_TYPE_UINT16,
            FW_DATA_TYPE_UINT32,
            FW_DATA_TYPE_UINT64,
            FW_DATA_TYPE_UNICODE_STRING,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FW_MATCH_VALUE_DATA
        {
            [FieldOffset(0)]
            public ushort uInt8;
            [FieldOffset(0)]
            public ushort uInt16;
            [FieldOffset(0)]
            public uint uInt32;
            [FieldOffset(0)]
            public ulong uInt64;
            [FieldOffset(0)]
            public IntPtr pString;
        }

        public struct FW_MATCH_VALUE
        {
            public FW_DATA_TYPE type;
            public FW_MATCH_VALUE_DATA data;
        }

        public struct FW_QUERY_CONDITION
        {
            public FW_MATCH_KEY matchKey;
            public FW_MATCH_TYPE matchType;
            public FW_MATCH_VALUE matchValue;
        }

        public enum FW_PROFILE_TYPE : uint
        {
            FW_PROFILE_TYPE_INVALID = 0,
            FW_PROFILE_TYPE_DOMAIN = 0x001,
            FW_PROFILE_TYPE_STANDARD = 0x002,
            FW_PROFILE_TYPE_PRIVATE = FW_PROFILE_TYPE_STANDARD,
            FW_PROFILE_TYPE_PUBLIC = 0x004,
            FW_PROFILE_TYPE_ALL = 0x7FFFFFFF,
            FW_PROFILE_TYPE_CURRENT = 0x80000000,
            FW_PROFILE_TYPE_NONE = FW_PROFILE_TYPE_CURRENT | FW_PROFILE_TYPE_DOMAIN
        }

        public enum FW_DIRECTION : uint
        {
            FW_DIR_INVALID = 0,
            FW_DIR_IN = 1,
            FW_DIR_OUT = 2,
            FW_DIR_BOTH = 3 // MAX
        }

        public enum IP_PROTOCOL : ushort
        {
            HOPOPT = 0,
            ICMPv4 = 1,
            IGMP = 2,
            TCP = 6,
            UDP = 17,
            IPv6 = 41,
            IPv6Route = 43,
            IPv6Frag = 44,
            GRE = 47,
            ICMPv6 = 58,
            IPv6NoNxt = 59,
            IPv6Opts = 60,
            VRRP = 112,
            PGM = 113,
            L2TP = 115,

            Any = 256,
            Custom = 257
        }

        [Flags]
        public enum FW_PORT_KEYWORD : ushort
        {
            FW_PORT_KEYWORD_NONE = 0x00,
            FW_PORT_KEYWORD_DYNAMIC_RPC_PORTS = 0x01,
            FW_PORT_KEYWORD_RPC_EP = 0x02,
            FW_PORT_KEYWORD_TEREDO_PORT = 0x04,

            // sinve win7
            FW_PORT_KEYWORD_IP_TLS_IN = 0x08,
            FW_PORT_KEYWORD_IP_TLS_OUT = 0x10,

            // since win8
            FW_PORT_KEYWORD_DHCP = 0x20,
            FW_PORT_KEYWORD_PLAYTO_DISCOVERY = 0x40,

            // since win10
            FW_PORT_KEYWORD_MDNS = 0x80,

            // since 1607
            FW_PORT_KEYWORD_CORTANA_OUT = 0x100,

            // since 1903
            FW_PORT_KEYWORD_PROXIMAL_TCP_CDP = 0x200,

            FW_PORT_KEYWORD_MAX_V2_1 = FW_PORT_KEYWORD_IP_TLS_IN,
            FW_PORT_KEYWORD_MAX_V2_10 = FW_PORT_KEYWORD_DHCP,
            FW_PORT_KEYWORD_MAX_V2_20 = FW_PORT_KEYWORD_MDNS,
            FW_PORT_KEYWORD_MAX_V2_24 = FW_PORT_KEYWORD_CORTANA_OUT,
            FW_PORT_KEYWORD_MAX_V2_25 = FW_PORT_KEYWORD_PROXIMAL_TCP_CDP,
            FW_PORT_KEYWORD_MAX = 0x400
        }

        public struct FW_PORT_RANGE
        {
            public ushort uBegin;
            public ushort uEnd;
        }

        public struct FW_PORT_RANGE_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pPorts; // FW_PORT_RANGE
        }

        public struct FW_PORTS
        {
            public FW_PORT_KEYWORD wPortKeywords;
            public FW_PORT_RANGE_LIST Ports;
        }

        public struct FW_PORT_SET
        {
            public FW_PORTS LocalPorts;
            public FW_PORTS RemotePorts;
        }

        public struct FW_ICMP_TYPE_CODE_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pEntries;
        }

        [Serializable()]
        public struct FW_ICMP_TYPE_CODE
        {
            public byte Type;
            public ushort Code; // 0-255; 256 = any
        }

        public struct FW_IPV4_ADDRESS_RANGE
        {
            public uint dwBegin;
            public uint dwEnd;
        }

        public struct FW_IPV4_SUBNET
        {
            public uint dwAddress;
            public uint dwSubNetMask;
        }

        public struct FW_IPV6_ADDRESS
        {
            public uint a1;
            public uint a2;
            public uint a3;
            public uint a4;
        }

        public struct FW_IPV6_ADDRESS_RANGE
        {
            public FW_IPV6_ADDRESS Begin;
            public FW_IPV6_ADDRESS End;
        }

        public struct FW_IPV6_SUBNET
        {
            public FW_IPV6_ADDRESS Address;
            public ushort wNumPrefixBits;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FW_PROTOCOL_UNION
        {
            [FieldOffset(0)]
            public FW_PORT_SET Ports;
            [FieldOffset(0)]
            public FW_ICMP_TYPE_CODE_LIST TypeCodeList;
        }

        [Flags]
        public enum FW_ADDRESS_KEYWORD : uint
        {
            FW_ADDRESS_KEYWORD_NONE = 0x0000,
            FW_ADDRESS_KEYWORD_LOCAL_SUBNET = 0x0001,
            FW_ADDRESS_KEYWORD_DNS = 0x0002,
            FW_ADDRESS_KEYWORD_DHCP = 0x0004,
            FW_ADDRESS_KEYWORD_WINS = 0x0008,
            FW_ADDRESS_KEYWORD_DEFAULT_GATEWAY = 0x0010,

            // since win8
            FW_ADDRESS_KEYWORD_INTRANET = 0x0020,
            FW_ADDRESS_KEYWORD_INTERNET = 0x0040,
            FW_ADDRESS_KEYWORD_PLAYTO_RENDERERS = 0x0080,
            FW_ADDRESS_KEYWORD_REMOTE_INTRANET = 0x0100,

            // since 1903
            FW_ADDRESS_KEYWORD_CAPTIVE_PORTAL = 0x0200,

            FW_ADDRESS_KEYWORD_MAX_V2_10 = FW_ADDRESS_KEYWORD_INTRANET,
            FW_ADDRESS_KEYWORD_MAX_V2_29 = FW_ADDRESS_KEYWORD_CAPTIVE_PORTAL,
            FW_ADDRESS_KEYWORD_MAX = 0x0400
        }

        public struct FW_IPV4_SUBNET_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pSubNets;
        }

        public struct FW_IPV4_RANGE_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pRanges;
        }

        public struct FW_IPV6_SUBNET_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pSubNets;
        }

        public struct FW_IPV6_RANGE_LIST
        {
            public ushort dwNumEntries;
            public IntPtr pRanges;
        }

        public struct FW_ADDRESSES
        {
            public FW_ADDRESS_KEYWORD dwV4AddressKeywords;
            public FW_ADDRESS_KEYWORD dwV6AddressKeywords;
            public FW_IPV4_SUBNET_LIST V4SubNets;
            public FW_IPV4_RANGE_LIST V4Ranges;
            public FW_IPV6_SUBNET_LIST V6SubNets;
            public FW_IPV6_RANGE_LIST V6Ranges;
        }

        public struct FW_INTERFACE_LUIDS
        {
            public uint dwNumLUIDs;
            public IntPtr pLUIDs;
        }

        [Flags]
        public enum FW_INTERFACE_TYPE : uint
        {
            FW_INTERFACE_TYPE_ALL = 0x0000,
            FW_INTERFACE_TYPE_LAN = 0x0001,
            FW_INTERFACE_TYPE_WIRELESS = 0x0002,
            FW_INTERFACE_TYPE_REMOTE_ACCESS = 0x0004,

            FW_INTERFACE_TYPE_MAX = 0x0008
        }

        public enum FW_RULE_ACTION : uint
        {
            FW_RULE_ACTION_INVALID = 0,
            // Rules with this action allow traffic but are applicable only to rules that at least specify the FW_RULE_FLAGS_AUTHENTICATE flag. This symbolic constant has a value of 1.
            FW_RULE_ACTION_ALLOW_BYPASS,
            FW_RULE_ACTION_BLOCK,
            FW_RULE_ACTION_ALLOW,

            FW_RULE_ACTION_MAX
        }

        [Flags]
        public enum FW_RULE_FLAGS : ushort
        {
            FW_RULE_FLAGS_NONE = 0x0000,
            FW_RULE_FLAGS_ACTIVE = 0x0001,
            FW_RULE_FLAGS_AUTHENTICATE = 0x0002,
            FW_RULE_FLAGS_AUTHENTICATE_WITH_ENCRYPTION = 0x0004,
            FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE = 0x0008,
            FW_RULE_FLAGS_LOOSE_SOURCE_MAPPED = 0x0010,

            // since win 7
            FW_RULE_FLAGS_AUTH_WITH_NO_ENCAPSULATION = 0x0020,

            FW_RULE_FLAGS_AUTH_WITH_ENC_NEGOTIATE = 0x0040,
            FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_APP = 0x0080,
            FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_USER = 0x0100,
            FW_RULE_FLAGS_AUTHENTICATE_BYPASS_OUTBOUND = 0x0200,

            // since win8
            FW_RULE_FLAGS_ALLOW_PROFILE_CROSSING = 0x0400,
            FW_RULE_FLAGS_LOCAL_ONLY_MAPPED = 0x0800,

            // since 1507
            FW_RULE_FLAGS_LUA_CONDITIONAL_ACE = 0x1000,

            FW_RULE_FLAGS_BIND_TO_INTERFACE = 0x2000, // not used

            FW_RULE_FLAGS_MAX_V2_1 = FW_RULE_FLAGS_AUTH_WITH_NO_ENCAPSULATION,
            FW_RULE_FLAGS_MAX_V2_9 = FW_RULE_FLAGS_AUTH_WITH_ENC_NEGOTIATE,
            FW_RULE_FLAGS_MAX_V2_10 = FW_RULE_FLAGS_ALLOW_PROFILE_CROSSING,
            FW_RULE_FLAGS_MAX_V2_20 = FW_RULE_FLAGS_LUA_CONDITIONAL_ACE,
            FW_RULE_FLAGS_MAX = 0x4000
        }

        public struct FW_OS_PLATFORM_LIST
        {
            public uint NumEntries;
            public IntPtr Platforms;
        }

        [Serializable()]
        public struct FW_OS_PLATFORM
        {
            public byte Platform;
            public byte MajorVersion;
            public byte MinorVersion;
            public byte Reserved;
        }

        public enum FW_RULE_ORIGIN_TYPE
        {
            FW_RULE_ORIGIN_INVALID,
            FW_RULE_ORIGIN_LOCAL,
            FW_RULE_ORIGIN_GP,
            FW_RULE_ORIGIN_DYNAMIC,
            FW_RULE_ORIGIN_AUTOGEN,
            FW_RULE_ORIGIN_HARDCODED,

            FW_RULE_ORIGIN_MAX
        }

        [Flags]
        public enum FW_TRUST_TUPLE_KEYWORD : uint
        {
            FW_TRUST_TUPLE_KEYWORD_NONE = 0x0000,
            FW_TRUST_TUPLE_KEYWORD_PROXIMITY = 0x0001,
            FW_TRUST_TUPLE_KEYWORD_PROXIMITY_SHARING = 0x0002,

            // since win10
            FW_TRUST_TUPLE_KEYWORD_WFD_PRINT = 0x0004,
            FW_TRUST_TUPLE_KEYWORD_WFD_DISPLAY = 0x0008,
            FW_TRUST_TUPLE_KEYWORD_WFD_DEVICES = 0x0010,

            // since 1703
            FW_TRUST_TUPLE_KEYWORD_WFD_KM_DRIVER = 0x0020,
            FW_TRUST_TUPLE_KEYWORD_UPNP = 0x0040,

            // since 1803
            FW_TRUST_TUPLE_KEYWORD_WFD_CDP = 0x0080,

            FW_TRUST_TUPLE_KEYWORD_MAX_V2_20 = FW_TRUST_TUPLE_KEYWORD_WFD_PRINT,
            FW_TRUST_TUPLE_KEYWORD_MAX_V2_26 = FW_TRUST_TUPLE_KEYWORD_WFD_KM_DRIVER,
            FW_TRUST_TUPLE_KEYWORD_MAX_V2_27 = FW_TRUST_TUPLE_KEYWORD_WFD_CDP,
            FW_TRUST_TUPLE_KEYWORD_MAX = 0x0100
        }

        public struct FW_NETWORK_NAMES
        {
            public ushort dwNumEntries;
            public IntPtr wszNames;
        }

        [Flags]
        public enum FW_RULE_FLAGS2 : ushort
        {
            FW_RULE_FLAGS2_NONE = 0x0000,
            FW_RULE_FLAGS2_SYSTEMOS_ONLY = 0x0001,
            FW_RULE_FLAGS2_GAMEOS_ONLY = 0x0002,
            FW_RULE_FLAGS2_DEVMODE = 0x0004,

            FW_RULE_FLAGS2_NOT_USED_VALUE_8 = 0x0008,
            FW_RULE_FLAGS2_NOT_USED_VALUE_16 = 0x0010,
            FW_RULE_FLAGS2_NOT_USED_VALUE_32 = 0x0020,
            FW_RULE_FLAGS2_NOT_USED_VALUE_64 = 0x0040,
            FW_RULE_FLAGS2_CALLOUT_AND_AUDIT = 0x0080,
            FW_RULE_FLAGS2_NOT_USED_VALUE_256 = 0x0100,
            FW_RULE_FLAGS2_NOT_USED_VALUE_512 = 0x0200,
            FW_RULE_FLAGS2_NOT_USED_VALUE_1024 = 0x0400,

            FW_RULE_FLAGS_MAX_V2_26 = 0x0008,
            FW_RULE_FLAGS2_MAX = 0x0800
        }

        [Flags]
        public enum FW_OBJECT_CTRL_FLAG : uint
        {
            FW_OBJECT_CTRL_FLAG_NONE = 0x0000,
            FW_OBJECT_CTRL_FLAG_INCLUDE_METADATA = 0x0001,
        }

        [Flags]
        public enum FW_ENFORCEMENT_STATE : uint
        {
            FW_ENFORCEMENT_STATE_INVALID = 0,
            FW_ENFORCEMENT_STATE_FULL = 1,
            FW_ENFORCEMENT_STATE_WF_OFF_IN_PROFILE = 2,
            FW_ENFORCEMENT_STATE_CATEGORY_OFF = 3,
            FW_ENFORCEMENT_STATE_DISABLED_OBJECT = 4,
            FW_ENFORCEMENT_STATE_INACTIVE_PROFILE = 5,
            FW_ENFORCEMENT_STATE_LOCAL_ADDRESS_RESOLUTION_EMPTY = 6,
            FW_ENFORCEMENT_STATE_REMOTE_ADDRESS_RESOLUTION_EMPTY = 7,
            FW_ENFORCEMENT_STATE_LOCAL_PORT_RESOLUTION_EMPTY = 8,
            FW_ENFORCEMENT_STATE_REMOTE_PORT_RESOLUTION_EMPTY = 9,
            FW_ENFORCEMENT_STATE_INTERFACE_RESOLUTION_EMPTY = 10,
            FW_ENFORCEMENT_STATE_APPLICATION_RESOLUTION_EMPTY = 12,
            FW_ENFORCEMENT_STATE_REMOTE_MACHINE_EMPTY = 12,
            FW_ENFORCEMENT_STATE_REMOTE_USER_EMPTY = 13,
            FW_ENFORCEMENT_STATE_LOCAL_GLOBAL_OPEN_PORTS_DISALLOWED = 14,
            FW_ENFORCEMENT_STATE_LOCAL_AUTHORIZED_APPLICATIONS_DISALLOWED = 15,
            FW_ENFORCEMENT_STATE_LOCAL_FIREWALL_RULES_DISALLOWED = 16,
            FW_ENFORCEMENT_STATE_LOCAL_CONSEC_RULES_DISALLOWED = 17,
            FW_ENFORCEMENT_STATE_MISMATCHED_PLATFORM = 18,
            FW_ENFORCEMENT_STATE_OPTIMIZED_OUT = 19,
            FW_ENFORCEMENT_STATE_LOCAL_USER_EMPTY = 20,
            FW_ENFORCEMENT_STATE_TRANSPORT_MACHINE_SD_EMPTY = 21,
            FW_ENFORCEMENT_STATE_TRANSPORT_USER_SD_EMPTY = 22,
            FW_ENFORCEMENT_STATE_TUPLE_RESOLUTION_EMPTY = 23,

            FW_ENFORCEMENT_STATE_MAX = 24
        };

        public struct FW_OBJECT_METADATA
        {
            ulong qwFilterContextID; // UInt64
            uint dwNumEntries;
            IntPtr pEnforcementStates; // FW_ENFORCEMENT_STATE
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_RULE
        {
            public IntPtr pNext;

            public ushort wSchemaVersion;
            public string wszRuleId;
            public string wszName;
            public string wszDescription;
            public FW_PROFILE_TYPE dwProfiles;
            public FW_DIRECTION Direction;
            public IP_PROTOCOL wIpProtocol;
            public FW_PROTOCOL_UNION ProtocolUnion;
            public FW_ADDRESSES LocalAddresses;
            public FW_ADDRESSES RemoteAddresses;
            public FW_INTERFACE_LUIDS LocalInterfaceIds;
            public FW_INTERFACE_TYPE dwLocalInterfaceTypes;
            public string wszLocalApplication;
            public string wszLocalService;
            public FW_RULE_ACTION Action;
            public FW_RULE_FLAGS wFlags;
            public string wszRemoteMachineAuthorizationList;
            public string wszRemoteUserAuthorizationList;
            public string wszEmbeddedContext;
            public FW_OS_PLATFORM_LIST PlatformValidity;
            public FW_RULE_STATUS Status;
            public FW_RULE_ORIGIN_TYPE Origin;
            public string wszGPOName;
            public uint Reserved; // FW_OBJECT_CTRL_FLAG
                                  // up to here its windows 7 comatible

            // since Win8
            public IntPtr pMetaData; //(Reserved & FW_OBJECT_CTRL_FLAG_INCLUDE_METADATA) ? 1 : 0)
            public string wszLocalUserAuthorizationList; //  string in SDDL format ([MS-DTYP] section 2.5.1). 
            public string wszPackageId; //  string in SID string format ([MS-DTYP] section 2.4.2.1)
            public string wszLocalUserOwner; // string in SID string format. The SID specifies the security principal that owns the rule.
            public FW_TRUST_TUPLE_KEYWORD dwTrustTupleKeywords;

            // since 1507
            public FW_NETWORK_NAMES OnNetworkNames; // Specifies the networks, identified by name, in which the rule must be enforced. 
            public string wszSecurityRealmId; // string in SID string format. The SID specifies the Security Realm ID, which identifies a security realm that this firewall rule is associated with.

            // since 1511
            public FW_RULE_FLAGS2 wFlags2; // just set it 0?

            // since 1607
            public FW_NETWORK_NAMES RemoteOutServerNames; // whats that?

            // since 1703
            public string wszFqbn; // FQBN is a Fully Qualified Binary Name, and it is a string in the following form: {Publisher\Product\Filename,Version}
            public uint compartmentId; // The ID of the compartment or Windows Server Container.

            // up to at least 1909
        }


        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWEnumFirewallRules(IntPtr hPolicy, FW_RULE_STATUS_CLASS dwFilteredByStatus, FW_PROFILE_TYPE dwProfileFilter, FW_ENUM_RULES_FLAGS wFlags, out uint dwNumRules, out IntPtr fwRules);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWQueryFirewallRules(IntPtr hPolicy, /*FW_QUERY*/ IntPtr pQuery, FW_ENUM_RULES_FLAGS wFlags, out uint pdwNumRules, out IntPtr fwRules);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWFreeFirewallRules(IntPtr pRules);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteAllFirewallRules(IntPtr hPolicy);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteFirewallRule(IntPtr hPolicy, string wszRuleID);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWAddFirewallRule(IntPtr hPolicy, ref FW_RULE pRule);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetFirewallRule(IntPtr hPolicy, ref FW_RULE pRule);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWVerifyFirewallRule(ushort wBinaryVersion, ref FW_RULE pRule);


        public enum FW_RULE_STATUS : uint
        {
            FW_RULE_STATUS_OK = 0x00010000,
            FW_RULE_STATUS_PARTIALLY_IGNORED = 0x00020000,
            FW_RULE_STATUS_IGNORED = 0x00040000,
            FW_RULE_STATUS_PARSING_ERROR_NAME = 0x00080001,
            FW_RULE_STATUS_PARSING_ERROR_DESC = 0x00080002,
            FW_RULE_STATUS_PARSING_ERROR_APP = 0x00080003,
            FW_RULE_STATUS_PARSING_ERROR_SVC = 0x00080004,
            FW_RULE_STATUS_PARSING_ERROR_RMA = 0x00080005,
            FW_RULE_STATUS_PARSING_ERROR_RUA = 0x00080006,
            FW_RULE_STATUS_PARSING_ERROR_EMBD = 0x00080007,
            FW_RULE_STATUS_PARSING_ERROR_RULE_ID = 0x00080008,
            FW_RULE_STATUS_PARSING_ERROR_PHASE1_AUTH = 0x00080009,
            FW_RULE_STATUS_PARSING_ERROR_PHASE2_CRYPTO = 0x0008000A,
            FW_RULE_STATUS_PARSING_ERROR_REMOTE_ENDPOINTS = 0x0008000F,
            FW_RULE_STATUS_PARSING_ERROR_REMOTE_ENDPOINT_FQDN = 0x00080010,
            FW_RULE_STATUS_PARSING_ERROR_KEY_MODULE = 0x00080011,
            FW_RULE_STATUS_PARSING_ERROR_PHASE2_AUTH = 0x0008000B,
            FW_RULE_STATUS_PARSING_ERROR_RESOLVE_APP = 0x0008000C,
            FW_RULE_STATUS_PARSING_ERROR_MAINMODE_ID = 0x0008000D,
            FW_RULE_STATUS_PARSING_ERROR_PHASE1_CRYPTO = 0x0008000E,
            FW_RULE_STATUS_PARSING_ERROR = 0x00080000,
            FW_RULE_STATUS_SEMANTIC_ERROR_RULE_ID = 0x00100010,
            FW_RULE_STATUS_SEMANTIC_ERROR_PORTS = 0x00100020,
            FW_RULE_STATUS_SEMANTIC_ERROR_PORT_KEYW = 0x00100021,
            FW_RULE_STATUS_SEMANTIC_ERROR_PORT_RANGE = 0x00100022,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V4_SUBNETS = 0x00100040,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V6_SUBNETS = 0x00100041,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V4_RANGES = 0x00100042,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V6_RANGES = 0x00100043,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_RANGE = 0x00100044,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_MASK = 0x00100045,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_PREFIX = 0x00100046,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_KEYW = 0x00100047,
            FW_RULE_STATUS_SEMANTIC_ERROR_LADDR_PROP = 0x00100048,
            FW_RULE_STATUS_SEMANTIC_ERROR_RADDR_PROP = 0x00100049,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V6 = 0x0010004A,
            FW_RULE_STATUS_SEMANTIC_ERROR_LADDR_INTF = 0x0010004B,
            FW_RULE_STATUS_SEMANTIC_ERROR_ADDR_V4 = 0x0010004C,
            FW_RULE_STATUS_SEMANTIC_ERROR_TUNNEL_ENDPOINT_ADDR = 0x0010004D,
            FW_RULE_STATUS_SEMANTIC_ERROR_DTE_VER = 0x0010004E,
            FW_RULE_STATUS_SEMANTIC_ERROR_DTE_MISMATCH_ADDR = 0x0010004F,
            FW_RULE_STATUS_SEMANTIC_ERROR_PROFILE = 0x00100050,
            FW_RULE_STATUS_SEMANTIC_ERROR_ICMP = 0x00100060,
            FW_RULE_STATUS_SEMANTIC_ERROR_ICMP_CODE = 0x00100061,
            FW_RULE_STATUS_SEMANTIC_ERROR_IF_ID = 0x00100070,
            FW_RULE_STATUS_SEMANTIC_ERROR_IF_TYPE = 0x00100071,
            FW_RULE_STATUS_SEMANTIC_ERROR_ACTION = 0x00100080,
            FW_RULE_STATUS_SEMANTIC_ERROR_ALLOW_BYPASS = 0x00100081,
            FW_RULE_STATUS_SEMANTIC_ERROR_DO_NOT_SECURE = 0x00100082,
            FW_RULE_STATUS_SEMANTIC_ERROR_ACTION_BLOCK_IS_ENCRYPTED_SECURE = 0x00100083,
            FW_RULE_STATUS_SEMANTIC_ERROR_DIR = 0x00100090,
            FW_RULE_STATUS_SEMANTIC_ERROR_PROT = 0x001000A0,
            FW_RULE_STATUS_SEMANTIC_ERROR_PROT_PROP = 0x001000A1,
            FW_RULE_STATUS_SEMANTIC_ERROR_DEFER_EDGE_PROP = 0x001000A2,
            FW_RULE_STATUS_SEMANTIC_ERROR_ALLOW_BYPASS_OUTBOUND = 0x001000A3,
            FW_RULE_STATUS_SEMANTIC_ERROR_DEFER_USER_INVALID_RULE = 0x001000A4,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS = 0x001000B0,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTO_AUTH = 0x001000B1,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTO_BLOCK = 0x001000B2,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTO_DYN_RPC = 0x001000B3,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTHENTICATE_ENCRYPT = 0x001000B4,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTH_WITH_ENC_NEGOTIATE_VER = 0x001000B5,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTH_WITH_ENC_NEGOTIATE = 0x001000B6,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_ESP_NO_ENCAP_VER = 0x001000B7,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_ESP_NO_ENCAP = 0x001000B8,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_TUNNEL_AUTH_MODES_VER = 0x001000B9,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_TUNNEL_AUTH_MODES = 0x001000BA,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_IP_TLS_VER = 0x001000BB,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_PORTRANGE_VER = 0x001000BC,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_ADDRS_TRAVERSE_DEFER_VER = 0x001000BD,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTH_WITH_ENC_NEGOTIATE_OUTBOUND = 0x001000BE,
            FW_RULE_STATUS_SEMANTIC_ERROR_FLAGS_AUTHENTICATE_WITH_OUTBOUND_BYPASS_VER = 0x001000BF,
            FW_RULE_STATUS_SEMANTIC_ERROR_REMOTE_AUTH_LIST = 0x001000C0,
            FW_RULE_STATUS_SEMANTIC_ERROR_REMOTE_USER_LIST = 0x001000C1,
            FW_RULE_STATUS_SEMANTIC_ERROR_PLATFORM = 0x001000E0,
            FW_RULE_STATUS_SEMANTIC_ERROR_PLATFORM_OP_VER = 0x001000E1,
            FW_RULE_STATUS_SEMANTIC_ERROR_PLATFORM_OP = 0x001000E2,
            FW_RULE_STATUS_SEMANTIC_ERROR_DTE_NOANY_ADDR = 0x001000F0,
            FW_RULE_STATUS_SEMANTIC_TUNNEL_EXEMPT_WITH_GATEWAY = 0x001000F1,
            FW_RULE_STATUS_SEMANTIC_TUNNEL_EXEMPT_VER = 0x001000F2,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_AUTH_SET_ID = 0x00100500,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_SET_ID = 0x00100510,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_SET_ID = 0x00100511,
            FW_RULE_STATUS_SEMANTIC_ERROR_SET_ID = 0x00101000,
            FW_RULE_STATUS_SEMANTIC_ERROR_IPSEC_PHASE = 0x00101010,
            FW_RULE_STATUS_SEMANTIC_ERROR_EMPTY_SUITES = 0x00101020,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_AUTH_METHOD = 0x00101030,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_AUTH_METHOD = 0x00101031,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_METHOD_ANONYMOUS = 0x00101032,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_METHOD_DUPLICATE = 0x00101033,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_METHOD_VER = 0x00101034,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_SUITE_FLAGS = 0x00101040,
            FW_RULE_STATUS_SEMANTIC_ERROR_HEALTH_CERT = 0x00101041,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_SIGNCERT_VER = 0x00101042,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_INTERMEDIATE_CA_VER = 0x00101043,
            FW_RULE_STATUS_SEMANTIC_ERROR_MACHINE_SHKEY = 0x00101050,
            FW_RULE_STATUS_SEMANTIC_ERROR_CA_NAME = 0x00101060,
            FW_RULE_STATUS_SEMANTIC_ERROR_MIXED_CERTS = 0x00101061,
            FW_RULE_STATUS_SEMANTIC_ERROR_NON_CONTIGUOUS_CERTS = 0x00101062,
            FW_RULE_STATUS_SEMANTIC_ERROR_MIXED_CA_TYPE_IN_BLOCK = 0x00101063,
            FW_RULE_STATUS_SEMANTIC_ERROR_MACHINE_USER_AUTH = 0x00101070,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_NON_DEFAULT_ID = 0x00105000,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_FLAGS = 0x00105001,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_TIMEOUT_MINUTES = 0x00105002,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_TIMEOUT_SESSIONS = 0x00105003,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_KEY_EXCHANGE = 0x00105004,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_ENCRYPTION = 0x00105005,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_HASH = 0x00105006,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_ENCRYPTION_VER = 0x00105007,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE1_CRYPTO_HASH_VER = 0x00105008,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_PFS = 0x00105020,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_PROTOCOL = 0x00105021,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_ENCRYPTION = 0x00105022,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_HASH = 0x00105023,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_TIMEOUT_MINUTES = 0x00105024,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_TIMEOUT_KBYTES = 0x00105025,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_ENCRYPTION_VER = 0x00105026,
            FW_RULE_STATUS_SEMANTIC_ERROR_PHASE2_CRYPTO_HASH_VER = 0x00105027,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_OR_AND_CONDITIONS = 0x00106000,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_AND_CONDITIONS = 0x00106001,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_CONDITION_KEY = 0x00106002,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_CONDITION_MATCH_TYPE = 0x00106003,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_CONDITION_DATA_TYPE = 0x00106004,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_CONDITION_KEY_AND_DATA_TYPE = 0x00106005,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEYS_PROTOCOL_PORT = 0x00106006,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_PROFILE = 0x00106007,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_STATUS = 0x00106008,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_FILTERID = 0x00106009,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_APP_PATH = 0x00106010,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_PROTOCOL = 0x00106011,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_LOCAL_PORT = 0x00106012,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_REMOTE_PORT = 0x00106013,
            FW_RULE_STATUS_SEMANTIC_ERROR_QUERY_KEY_SVC_NAME = 0x00106015,
            FW_RULE_STATUS_SEMANTIC_ERROR_REQUIRE_IN_CLEAR_OUT_ON_TRANSPORT = 0x00107000,
            FW_RULE_STATUS_SEMANTIC_ERROR_TUNNEL_BYPASS_TUNNEL_IF_SECURE_ON_TRANSPORT = 0x00107001,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_NOENCAP_ON_TUNNEL = 0x00107002,
            FW_RULE_STATUS_SEMANTIC_ERROR_AUTH_NOENCAP_ON_PSK = 0x00107003,
            FW_RULE_STATUS_SEMANTIC_ERROR_CRYPTO_ENCR_HASH = 0x00105040,
            FW_RULE_STATUS_SEMANTIC_ERROR_CRYPTO_ENCR_HASH_COMPAT = 0x00105041,
            FW_RULE_STATUS_SEMANTIC_ERROR_SCHEMA_VERSION = 0x00105050,
            FW_RULE_STATUS_SEMANTIC_ERROR = 0x00100000,
            FW_RULE_STATUS_RUNTIME_ERROR_PHASE1_AUTH_NOT_FOUND = 0x00200001,
            FW_RULE_STATUS_RUNTIME_ERROR_PHASE2_AUTH_NOT_FOUND = 0x00200002,
            FW_RULE_STATUS_RUNTIME_ERROR_PHASE2_CRYPTO_NOT_FOUND = 0x00200003,
            FW_RULE_STATUS_RUNTIME_ERROR_AUTH_MCHN_SHKEY_MISMATCH = 0x00200004,
            FW_RULE_STATUS_RUNTIME_ERROR_PHASE1_CRYPTO_NOT_FOUND = 0x00200005,
            FW_RULE_STATUS_RUNTIME_ERROR_AUTH_NOENCAP_ON_TUNNEL = 0x00200006,
            FW_RULE_STATUS_RUNTIME_ERROR_AUTH_NOENCAP_ON_PSK = 0x00200007,
            FW_RULE_STATUS_RUNTIME_ERROR = 0x00200000,
            FW_RULE_STATUS_ERROR = FW_RULE_STATUS_PARSING_ERROR | FW_RULE_STATUS_SEMANTIC_ERROR | FW_RULE_STATUS_RUNTIME_ERROR,
            FW_RULE_STATUS_ALL = 0xFFFF0000
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Firewall Settings
        public enum FW_PROFILE_CONFIG : uint
        {
            FW_PROFILE_CONFIG_INVALID = 0,
            FW_PROFILE_CONFIG_ENABLE_FW = 1,
            FW_PROFILE_CONFIG_DISABLE_STEALTH_MODE = 2,
            FW_PROFILE_CONFIG_SHIELDED = 3,
            FW_PROFILE_CONFIG_DISABLE_UNICAST_RESPONSES_TO_MULTICAST_BROADCAST = 4,
            FW_PROFILE_CONFIG_LOG_DROPPED_PACKETS = 5,
            FW_PROFILE_CONFIG_LOG_SUCCESS_CONNECTIONS = 6,
            FW_PROFILE_CONFIG_LOG_IGNORED_RULES = 7,
            FW_PROFILE_CONFIG_LOG_MAX_FILE_SIZE = 8,
            FW_PROFILE_CONFIG_LOG_FILE_PATH = 9,
            FW_PROFILE_CONFIG_DISABLE_INBOUND_NOTIFICATIONS = 10,
            FW_PROFILE_CONFIG_AUTH_APPS_ALLOW_USER_PREF_MERGE = 11,
            FW_PROFILE_CONFIG_GLOBAL_PORTS_ALLOW_USER_PREF_MERGE = 12,
            FW_PROFILE_CONFIG_ALLOW_LOCAL_POLICY_MERGE = 13,
            FW_PROFILE_CONFIG_ALLOW_LOCAL_IPSEC_POLICY_MERGE = 14,
            FW_PROFILE_CONFIG_DISABLED_INTERFACES = 15,
            FW_PROFILE_CONFIG_DEFAULT_OUTBOUND_ACTION = 16,
            FW_PROFILE_CONFIG_DEFAULT_INBOUND_ACTION = 17,
            FW_PROFILE_CONFIG_DISABLE_STEALTH_MODE_IPSEC_SECURED_PACKET_EXEMPTION = 18,

            FW_PROFILE_CONFIG_MAX = 19
        }

        public enum FW_GLOBAL_CONFIG
        {
            FW_GLOBAL_CONFIG_INVALID = 0,
            FW_GLOBAL_CONFIG_POLICY_VERSION_SUPPORTED = 1,
            FW_GLOBAL_CONFIG_CURRENT_PROFILE = 2,
            FW_GLOBAL_CONFIG_DISABLE_STATEFUL_FTP = 3,
            FW_GLOBAL_CONFIG_DISABLE_STATEFUL_PPTP = 4,
            FW_GLOBAL_CONFIG_SA_IDLE_TIME = 5,
            FW_GLOBAL_CONFIG_PRESHARED_KEY_ENCODING = 6,
            FW_GLOBAL_CONFIG_IPSEC_EXEMPT = 7,
            FW_GLOBAL_CONFIG_CRL_CHECK = 8,
            FW_GLOBAL_CONFIG_IPSEC_THROUGH_NAT = 9,
            FW_GLOBAL_CONFIG_POLICY_VERSION = 10,
            FW_GLOBAL_CONFIG_BINARY_VERSION_SUPPORTED = 11,
            FW_GLOBAL_CONFIG_IPSEC_TUNNEL_REMOTE_MACHINE_AUTHORIZATION_LIST = 12,
            FW_GLOBAL_CONFIG_IPSEC_TUNNEL_REMOTE_USER_AUTHORIZATION_LIST = 13,
            FW_GLOBAL_CONFIG_OPPORTUNISTICALLY_MATCH_AUTH_SET_PER_KM = 14,
            FW_GLOBAL_CONFIG_IPSEC_TRANSPORT_REMOTE_MACHINE_AUTHORIZATION_LIST = 15,
            FW_GLOBAL_CONFIG_IPSEC_TRANSPORT_REMOTE_USER_AUTHORIZATION_LIST = 16,
            FW_GLOBAL_CONFIG_ENABLE_PACKET_QUEUE = 17,

            FW_GLOBAL_CONFIG_MAX = 18
        }

        [Flags]
        public enum FW_CONFIG_FLAGS : uint
        {
            FW_CONFIG_FLAG_NONE = 0,
            FW_CONFIG_FLAG_RETURN_DEFAULT_IF_NOT_FOUND = 1
        }

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWExportPolicy(string wszMachineOrGPO, [MarshalAs(UnmanagedType.Bool)] bool fGPO, string wszFilePath, [MarshalAs(UnmanagedType.Bool)] out bool fSomeInfoLost);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWImportPolicy(string wszMachineOrGPO, [MarshalAs(UnmanagedType.Bool)] bool fGPO, string wszFilePath, [MarshalAs(UnmanagedType.Bool)] out bool fSomeInfoLost);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWRestoreDefaults(string wszMachineOrGPO);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWRestoreGPODefaults(string wszGPOPath);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWGetGlobalConfig(ushort wBinaryVersion, string wszMachineOrGPO, FW_STORE_TYPE StoreType, FW_GLOBAL_CONFIG configId, FW_CONFIG_FLAGS dwFlags, out uint dwSetting, out uint pdwBufSize);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWGetGlobalConfig(ushort wBinaryVersion, string wszMachineOrGPO, FW_STORE_TYPE StoreType, FW_GLOBAL_CONFIG configId, FW_CONFIG_FLAGS dwFlags, StringBuilder str, out uint pdwBufSize);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetGlobalConfig(ushort wBinaryVersion, string wszMachineOrGPO, FW_STORE_TYPE StoreType, FW_GLOBAL_CONFIG configId, ref uint dwSetting, uint dwBufSize);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetGlobalConfig(ushort wBinaryVersion, string wszMachineOrGPO, FW_STORE_TYPE StoreType, FW_GLOBAL_CONFIG configId, string str, uint dwBufSize);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWGetConfig(IntPtr hPolicy, FW_PROFILE_CONFIG configId, FW_PROFILE_TYPE Profile, FW_CONFIG_FLAGS dwFlags, out uint dwBuffer, out uint pdwBufSize);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWGetConfig(IntPtr hPolicy, FW_PROFILE_CONFIG configId, FW_PROFILE_TYPE Profile, FW_CONFIG_FLAGS dwFlags, StringBuilder str, out uint pdwBufSize);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWSetConfig(IntPtr hPolicy, FW_PROFILE_CONFIG configId, FW_PROFILE_TYPE Profile, ref uint dwBuffer, uint dwBufSize);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetConfig(IntPtr hPolicy, FW_PROFILE_CONFIG configId, FW_PROFILE_TYPE Profile, string str, uint dwBufSize);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // App Packages

        public struct INET_FIREWALL_AC_CAPABILITIES
        {
            public uint count;
            public IntPtr capabilities;
        }

        public struct INET_FIREWALL_AC_BINARIES
        {
            public uint count;
            public IntPtr binaries;
        }

        public struct INET_FIREWALL_APP_CONTAINER
        {
            public IntPtr appContainerSid;
            public IntPtr userSid;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string appContainerName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string displayName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string description;
            public INET_FIREWALL_AC_CAPABILITIES capabilities;
            public INET_FIREWALL_AC_BINARIES binaries;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string workingDirectory;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string packageFullName;
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint NetworkIsolationEnumAppContainers(uint Flags, out uint pdwNumPublicAppCs, out IntPtr ppPublicAppCs);

        [DllImport("FirewallAPI.dll")]
        public static extern void NetworkIsolationFreeAppContainers(IntPtr pNetworks);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Network Info

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_NETWORK
        {
            public string pszName;
            public FW_PROFILE_TYPE ProfileType;
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumNetworks(IntPtr hPolicy, out uint pdwNumNetworks, out IntPtr ppNetworks);

        [DllImport("FirewallAPI.dll")]
        public static extern void FWFreeNetworks(IntPtr pNetworks);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Adapter Info

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_ADAPTER
        {
            public string pszFriendlyName;
            public Guid Guid;
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumAdapters(IntPtr hPolicy, out uint pdwNumAdapters, out IntPtr ppAdapters);

        [DllImport("FirewallAPI.dll")]
        public static extern void FWFreeAdapters(IntPtr pAdapters);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Product Info

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_PRODUCT
        {
            public uint dwFlags;
            public uint dwNumRuleCategories;
            public IntPtr pRuleCategories;
            public string pszDisplayName;
            public string pszPathToSignedProductExe;
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumProducts(IntPtr hPolicy, out uint pdwNumProducts, out IntPtr ppProducts);

        [DllImport("FirewallAPI.dll")]
        public static extern void FWFreeProducts(IntPtr pProducts);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Authentication

        public enum FW_IPSEC_PHASE
        {
            FW_IPSEC_PHASE_INVALID,
            FW_IPSEC_PHASE_1,
            FW_IPSEC_PHASE_2,

            FW_IPSEC_PHASE_MAX
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_AUTH_SET
        {
            public IntPtr pNext;
            public ushort wSchemaVersion;
            public FW_IPSEC_PHASE IpSecPhase;
            public string wszSetId;
            public string wszName;
            public string wszDescription;
            public string wszEmbeddedContext;
            public uint dwNumSuites;
            public IntPtr pSuites;
            public FW_RULE_ORIGIN_TYPE Origin;
            public string wszGPOName;
            public FW_RULE_STATUS Status;
            public uint dwAuthSetFlags;
        }


        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWAddAuthenticationSet(IntPtr hPolicy, ref FW_AUTH_SET pAuthSet);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetAuthenticationSet(IntPtr hPolicy, ref FW_AUTH_SET pAuthSet);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteAuthenticationSet(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, string wszSetId);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteAllAuthenticationSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWEnumAuthenticationSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, FW_RULE_STATUS_CLASS dwFilteredByStatus, FW_ENUM_RULES_FLAGS wFlags, out uint dwNumAuthSets, out IntPtr ppAuthSets);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWQueryAuthenticationSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, /*FW_QUERY*/ IntPtr pQuery, FW_ENUM_RULES_FLAGS wFlags, out uint dwNumAuthSets, out IntPtr ppAuthSets);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWVerifyAuthenticationSet(ushort wBinaryVersion, ref FW_AUTH_SET pAuthSet);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWFreeAuthenticationSets(IntPtr pAuthSets);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Connection security rules

        public enum FW_CS_RULE_ACTION
        {
            FW_CS_RULE_ACTION_INVALID,
            FW_CS_RULE_ACTION_SECURE_SERVER,
            FW_CS_RULE_ACTION_BOUNDARY,
            FW_CS_RULE_ACTION_SECURE,
            FW_CS_RULE_ACTION_DO_NOT_SECURE,

            FW_CS_RULE_ACTION_MAX
        }

        [Flags]
        public enum FW_CS_RULE_FLAGS : ushort
        {
            FW_CS_RULE_FLAGS_NONE = 0x00,
            FW_CS_RULE_FLAGS_ACTIVE = 0x01,
            FW_CS_RULE_FLAGS_DTM = 0x02,
            FW_CS_RULE_FLAGS_TUNNEL_BYPASS_IF_ENCRYPTED = 0x08,
            FW_CS_RULE_FLAGS_OUTBOUND_CLEAR = 0x10,
            FW_CS_RULE_FLAGS_APPLY_AUTHZ = 0x20,

            FW_CS_RULE_FLAGS_MAX_V2_1 = FW_CS_RULE_FLAGS_DTM,
            FW_CS_RULE_FLAGS_MAX_V2_8 = 0x04,
            FW_CS_RULE_FLAGS_MAX = 0x40
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_CS_RULE
        {
            public IntPtr pNext;
            public ushort wSchemaVersion;
            public string wszRuleId;
            public string wszName;
            public string wszDescription;
            public uint dwProfiles;
            public FW_ADDRESSES Endpoint1;
            public FW_ADDRESSES Endpoint2;
            public FW_INTERFACE_LUIDS LocalInterfaceIds;
            public FW_INTERFACE_TYPE dwLocalInterfaceTypes;
            public uint dwLocalTunnelEndpointV4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] LocalTunnelEndpointV6;
            public uint dwRemoteTunnelEndpointV4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] RemoteTunnelEndpointV6;
            public FW_PORTS Endpoint1Ports;
            public FW_PORTS Endpoint2Ports;
            public IP_PROTOCOL wIpProtocol;
            public string wszPhase1AuthSet;
            public string wszPhase2CryptoSet;
            public string wszPhase2AuthSet;
            public FW_CS_RULE_ACTION Action;
            public FW_CS_RULE_FLAGS wFlags;
            public string wszEmbeddedContext;
            public FW_OS_PLATFORM_LIST PlatformValidityList;
            public FW_RULE_ORIGIN_TYPE Origin;
            public string wszGPOName;
            public FW_RULE_STATUS Status;
            public string wszMMParentRuleId;
            // up to here its windows 7 comatible

            // since Win8
            public uint Reserved;
            public IntPtr pMetaData;
            public string RemoteTunnelEndpointFqdn;
            public FW_ADDRESSES RemoteTunnelEndpoints;
            public uint dwKeyModules;
            public uint FwdPathSaLifetime;
            public string wszTransportMachineAuthzSDDL;
            public string wszTransportUserAuthzSDDL;
        }


        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumConnectionSecurityRules(IntPtr hPolicy, FW_RULE_STATUS_CLASS dwFilteredByStatus, uint dwProfileFilter, FW_ENUM_RULES_FLAGS wFlags, out uint pdwNumRules, out IntPtr ppRules);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWQueryConnectionSecurityRules(IntPtr hPolicy, /*FW_QUERY*/ IntPtr pQuery, FW_ENUM_RULES_FLAGS wFlags, out uint pdwNumRules, out IntPtr fwRules);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWAddConnectionSecurityRule(IntPtr hPolicy, ref FW_CS_RULE pRule);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWSetConnectionSecurityRule(IntPtr hPolicy, ref FW_CS_RULE pRule);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteConnectionSecurityRule(IntPtr hPolicy, string wszRuleId);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteAllConnectionSecurityRules(IntPtr hPolicy);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWFreeConnectionSecurityRules(IntPtr pRule);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Crypto


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_CRYPTO_SET_PHASE1
        {
            public IntPtr pNext;
            public ushort wSchemaVersion;
            public FW_IPSEC_PHASE IpSecPhase;
            public string wszSetId;
            public string wszName;
            public string wszDescription;
            public string wszEmbeddedContext;
            public ushort wFlags;
            public uint dwNumPhase1Suites;
            public IntPtr pPhase1Suites;
            public uint dwTimeOutMinutes;
            public uint dwTimeOutSessions;
            public FW_RULE_ORIGIN_TYPE Origin;
            public string wszGPOName;
            public FW_RULE_STATUS Status;
            public uint dwCryptoSetFlags;
        }
        public enum FW_PHASE2_CRYPTO_PFS
        {
            FW_PHASE2_CRYPTO_PFS_INVALID = 0,
            FW_PHASE2_CRYPTO_PFS_DISABLE = 1,
            FW_PHASE2_CRYPTO_PFS_PHASE1 = 2,
            FW_PHASE2_CRYPTO_PFS_DH1 = 3,
            FW_PHASE2_CRYPTO_PFS_DH2 = 4,
            FW_PHASE2_CRYPTO_PFS_DH2048 = 5,
            FW_PHASE2_CRYPTO_PFS_ECDH256 = 6,
            FW_PHASE2_CRYPTO_PFS_ECDH384 = 7,
            FW_PHASE2_CRYPTO_PFS_DH24 = 8,

            FW_PHASE2_CRYPTO_PFS_MAX_V2_10 = FW_PHASE2_CRYPTO_PFS_DH24,
            FW_PHASE2_CRYPTO_PFS_MAX = 9
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FW_CRYPTO_SET_PHASE2
        {
            public IntPtr pNext;
            public ushort wSchemaVersion;
            public FW_IPSEC_PHASE IpSecPhase;
            public string wszSetId;
            public string wszName;
            public string wszDescription;
            public string wszEmbeddedContext;
            public FW_PHASE2_CRYPTO_PFS Pfs;
            public uint dwNumPhase2Suites;
            public IntPtr pPhase2Suites;
            private uint Reserved1;
            private uint Reserved2;
            public FW_RULE_ORIGIN_TYPE Origin;
            public string wszGPOName;
            public FW_RULE_STATUS Status;
            public uint dwCryptoSetFlags;
        }


        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWEnumCryptoSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, FW_RULE_STATUS_CLASS dwFilteredByStatus, FW_ENUM_RULES_FLAGS wFlags, out uint pdwNumCryptoSets, out IntPtr ppCryptoSets);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWQueryCryptoSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, FW_QUERY pQuery, FW_ENUM_RULES_FLAGS wFlags, out uint pdwNumCryptoSets, out IntPtr ppCryptoSets);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWAddCryptoSet(IntPtr hPolicy, ref FW_CRYPTO_SET_PHASE1 pCrypto);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWAddCryptoSet(IntPtr hPolicy, ref FW_CRYPTO_SET_PHASE2 pCrypto);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetCryptoSet(IntPtr hPolicy, ref FW_CRYPTO_SET_PHASE1 pCrypto);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWSetCryptoSet(IntPtr hPolicy, ref FW_CRYPTO_SET_PHASE2 pCrypto);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteCryptoSet(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase, string wszSetId);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWDeleteAllCryptoSets(IntPtr hPolicy, FW_IPSEC_PHASE IpSecPhase);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWVerifyCryptoSet(ushort wBinaryVersion, ref FW_CRYPTO_SET_PHASE1 pCryptoSet);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWVerifyCryptoSet(ushort wBinaryVersion, ref FW_CRYPTO_SET_PHASE2 pCryptoSet);

        [DllImport("FirewallAPI.dll", CharSet = CharSet.Unicode)]
        public static extern uint FWFreeCryptoSets(IntPtr pCryptoSets);


        public enum FW_PHASE1_KEY_MODULE_TYPE
        {
            FW_PHASE1_KEY_MODULE_INVALID = 0,
            FW_PHASE1_KEY_MODULE_IKE = 1,
            FW_PHASE1_KEY_MODULE_AUTH_IP = 2,

            FW_PHASE1_KEY_MODULE_MAX = 3
        }

        public enum FW_IP_VERSION
        {
            FW_IP_VERSION_INVALID = 0,
            FW_IP_VERSION_V4,
            FW_IP_VERSION_V6 = 2,

            FW_IP_VERSION_MAX = 3
        }

        public struct FW_ENDPOINTS
        {
            public FW_IP_VERSION IpVersion;
            public uint dwSourceV4Address;
            public uint dwDestinationV4Address;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] SourceV6Address;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] DestinationV6Address;
        }

        public enum FW_CRYPTO_KEY_EXCHANGE_TYPE
        {
            FW_CRYPTO_KEY_EXCHANGE_NONE = 0,
            FW_CRYPTO_KEY_EXCHANGE_DH1 = 1,
            FW_CRYPTO_KEY_EXCHANGE_DH2 = 2,
            FW_CRYPTO_KEY_EXCHANGE_ECDH256 = 3,
            FW_CRYPTO_KEY_EXCHANGE_ECDH384 = 4,
            FW_CRYPTO_KEY_EXCHANGE_DH2048 = 5,
            FW_CRYPTO_KEY_EXCHANGE_DH14 = FW_CRYPTO_KEY_EXCHANGE_DH2048,
            FW_CRYPTO_KEY_EXCHANGE_DH24 = 6,

            FW_CRYPTO_KEY_EXCHANGE_MAX_V2_10 = FW_CRYPTO_KEY_EXCHANGE_DH24,
            FW_CRYPTO_KEY_EXCHANGE_MAX = 7
        }

        public enum FW_CRYPTO_ENCRYPTION_TYPE
        {
            FW_CRYPTO_ENCRYPTION_NONE = 0,
            FW_CRYPTO_ENCRYPTION_DES = 1,
            FW_CRYPTO_ENCRYPTION_3DES = 2,
            FW_CRYPTO_ENCRYPTION_AES128 = 3,
            FW_CRYPTO_ENCRYPTION_AES192 = 4,
            FW_CRYPTO_ENCRYPTION_AES256 = 5,
            FW_CRYPTO_ENCRYPTION_AES_GCM128 = 6,
            FW_CRYPTO_ENCRYPTION_AES_GCM192 = 7,
            FW_CRYPTO_ENCRYPTION_AES_GCM256 = 8,

            FW_CRYPTO_ENCRYPTION_MAX_V2_0 = FW_CRYPTO_ENCRYPTION_AES_GCM128,
            FW_CRYPTO_ENCRYPTION_MAX = 9
        }

        public enum FW_CRYPTO_HASH_TYPE
        {
            FW_CRYPTO_HASH_NONE = 0,
            FW_CRYPTO_HASH_MD5 = 1,
            FW_CRYPTO_HASH_SHA1 = 2,
            FW_CRYPTO_HASH_SHA256 = 3,
            FW_CRYPTO_HASH_SHA384 = 4,
            FW_CRYPTO_HASH_AES_GMAC128 = 5,
            FW_CRYPTO_HASH_AES_GMAC192 = 6,
            FW_CRYPTO_HASH_AES_GMAC256 = 7,

            FW_CRYPTO_HASH_MAX_V2_0 = FW_CRYPTO_HASH_SHA256,
            FW_CRYPTO_HASH_MAX = 8
        }

        public struct FW_PHASE1_CRYPTO_SUITE
        {
            public FW_CRYPTO_KEY_EXCHANGE_TYPE KeyExchange;
            public FW_CRYPTO_ENCRYPTION_TYPE Encryption;
            public FW_CRYPTO_HASH_TYPE Hash;
            public uint dwP1CryptoSuiteFlags;
        }

        public struct FW_COOKIE_PAIR
        {
            public ulong Initiator;
            public ulong Responder;
        }

        public struct FW_PHASE1_SA_DETAILS
        {
            public ulong SaId;
            public FW_PHASE1_KEY_MODULE_TYPE KeyModuleType;
            public FW_ENDPOINTS Endpoints;
            public FW_PHASE1_CRYPTO_SUITE SelectedProposal;
            public uint dwProposalLifetimeKBytes;
            public uint dwProposalLifetimeMinutes;
            public uint dwProposalMaxNumPhase2;
            public FW_COOKIE_PAIR CookiePair;
            public IntPtr pFirstAuth;
            public IntPtr pSecondAuth;
            public uint dwP1SaFlags;
        }


        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumPhase1SAs(IntPtr hPolicy, IntPtr pEndpoints, out uint pdwNumSAs, out FW_PHASE1_SA_DETAILS ppSAs);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWDeletePhase1SAs(uint dwNumSAs, FW_ENDPOINTS pEndpoints);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWFreePhase1SAs(uint dwNumSAs, IntPtr pSAs);

        public enum FW_CRYPTO_PROTOCOL_TYPE
        {
            FW_CRYPTO_PROTOCOL_INVALID = 0,
            FW_CRYPTO_PROTOCOL_AH = 1,
            FW_CRYPTO_PROTOCOL_ESP = 2,
            FW_CRYPTO_PROTOCOL_BOTH = 3,
            FW_CRYPTO_PROTOCOL_AUTH_NO_ENCAP = 4,

            FW_CRYPTO_PROTOCOL_MAX_2_1 = (FW_CRYPTO_PROTOCOL_BOTH + 1),
            FW_CRYPTO_PROTOCOL_MAX = 5
        }

        public struct FW_PHASE2_CRYPTO_SUITE
        {
            public FW_CRYPTO_PROTOCOL_TYPE Protocol;
            public FW_CRYPTO_HASH_TYPE AhHash;
            public FW_CRYPTO_HASH_TYPE EspHash;
            public FW_CRYPTO_ENCRYPTION_TYPE Encryption;
            public uint dwTimeoutMinutes;
            public uint dwTimeoutKBytes;
            public uint dwP2CryptoSuiteFlags;
        }

        public struct FW_PHASE2_SA_DETAILS
        {
            public ulong SaId;
            public FW_DIRECTION Direction;
            public FW_ENDPOINTS Endpoints;
            public ushort wLocalPort;
            public ushort wRemotePort;
            public IP_PROTOCOL wIpProtocol;
            public FW_PHASE2_CRYPTO_SUITE SelectedProposal;
            public FW_PHASE2_CRYPTO_PFS Pfs;
            public Guid TransportFilterId;
            public uint dwP2SaFlags;
        }

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWEnumPhase2SAs(IntPtr hPolicy, IntPtr pEndpoints, out uint pdwNumSAs, out FW_PHASE2_SA_DETAILS ppSAs);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWDeletePhase2SAs(uint dwNumSAs, FW_ENDPOINTS pEndpoints);

        [DllImport("FirewallAPI.dll")]
        public static extern uint FWFreePhase2SAs(uint dwNumSAs, IntPtr pSAs);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // 

        /* todo:

        typedef struct _tag_FW_MM_RULE
        {
            struct _tag_FW_MM_RULE *pNext;
            USHORT wSchemaVersion;
            //[string, range(1,512), ref]
            PWCHAR wszRuleId;
            //[string, range(1,10001)]
            PWCHAR wszName;
            //[string, range(1,10001)]
            PWCHAR wszDescription;
            FW_PROFILE_TYPE dwProfiles;
            FW_ADDRESSES Endpoint1;
            FW_ADDRESSES Endpoint2;
            //[string, range(1,255)]
            PWCHAR wszPhase1AuthSet;
            //[string, range(1,255)]
            PWCHAR wszPhase1CryptoSet;
            USHORT wFlags;
            //[string, range(1,10001)]
            PWCHAR wszEmbeddedContext;
            FW_OS_PLATFORM_LIST PlatformValidityList;
            //[range(FW_RULE_ORIGIN_INVALID, FW_RULE_ORIGIN_MAX-1)]
            FW_RULE_ORIGIN_TYPE Origin;
            //[string, range(1,10001)]
            PWCHAR wszGPOName;
            FW_RULE_STATUS Status;
            ULONG Reserved;
            //[size_is((Reserved & FW_OBJECT_CTRL_FLAG_INCLUDE_METADATA) ? 1 : 0)]
            PFW_OBJECT_METADATA pMetaData;
        } FW_MM_RULE, *PFW_MM_RULE;


        typedef ULONG(WINAPI* _FWEnumMainModeRules)(_In_ FW_POLICY_STORE_HANDLE hPolicy, _In_ FW_RULE_STATUS_CLASS dwFilteredByStatus, _In_ FW_PROFILE_TYPE dwProfileFilter,
           _In_ FW_ENUM_RULES_FLAGS wFlags, __out PULONG pdwNumRules, __out_ecount(pdwNumRules) PFW_MM_RULE* ppMMRules);

        typedef ULONG(WINAPI* _FWQueryMainModeRules)(_In_ FW_POLICY_STORE_HANDLE hPolicy, _In_ PFW_QUERY pQuery, _In_ FW_ENUM_RULES_FLAGS wFlags, __out PULONG pdwNumRules,
         __out_ecount(pdwNumRules) PFW_MM_RULE* ppMMRules);

        typedef ULONG(WINAPI* _FWAddMainModeRule)(_In_ FW_POLICY_STORE_HANDLE hPolicy, _In_ PFW_MM_RULE pMMRule, __out FW_RULE_STATUS* pStatus);

        typedef ULONG(WINAPI* _FWSetMainModeRule)(_In_ FW_POLICY_STORE_HANDLE hPolicy, _In_ PFW_MM_RULE pMMRule, __out FW_RULE_STATUS* pStatus);

        typedef ULONG(WINAPI* _FWDeleteMainModeRule)(_In_ FW_POLICY_STORE_HANDLE hPolicy, _In_ LPWSTR pRuleId);

        typedef ULONG(WINAPI* _FWDeleteAllMainModeRules)(_In_ FW_POLICY_STORE_HANDLE hPolicy);

        NTSYSCALLAPI ULONG NTAPI FWFreeMainModeRules(_In_ PFW_MM_RULE pMMRules );

        */

    }
}