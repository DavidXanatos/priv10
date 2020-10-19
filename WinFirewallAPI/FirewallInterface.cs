using MiscHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static WinFirewallAPI.WindowsFirewall;

namespace WinFirewallAPI
{
    public class FirewallInterface : IDisposable
    {
        public static ushort fwApiVersion = GetApiVersion();

        private static ushort GetApiVersion()
        {
            if (WinVer.Win19H1.TestHost() || WinVer.Win19H2.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_19Hx;
            else if (WinVer.Win1803.TestHost() || WinVer.Win1809.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_180x;
            else if (WinVer.Win1703.TestHost() || WinVer.Win1709.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_170x;
            else if (WinVer.Win1607.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_REDSTONE1;
            else if (WinVer.Win1511.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD2;
            else if (WinVer.Win1507.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD;
            else if (WinVer.Win10.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN10;
            else if (WinVer.Win8.TestHost() || WinVer.Win81.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8;
            else //if (WinVer.Win7.TestHost())
                return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_SEVEN;
            //else if (WinVer.Win6.TestHost())
            //    return (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_VISTA;
        }

        public IntPtr policyHandle = IntPtr.Zero;

        class NetFwRule
        {
            //public FW_RULE Entry;
            public FirewallRule Rule;
        }

        int RuleCounter = 0;
        Dictionary<string, NetFwRule> Rules = new Dictionary<string, NetFwRule>();

        public FirewallInterface()
        {
            FWOpenPolicyStore(fwApiVersion, null, FW_STORE_TYPE.FW_STORE_TYPE_LOCAL, FW_POLICY_ACCESS_RIGHT.FW_POLICY_ACCESS_RIGHT_READ_WRITE, FW_POLICY_STORE_FLAGS.FW_POLICY_STORE_FLAGS_NONE, out policyHandle);
            //FWOpenPolicyStore(fwApiVersion, null, FW_STORE_TYPE.FW_STORE_TYPE_DYNAMIC, FW_POLICY_ACCESS_RIGHT.FW_POLICY_ACCESS_RIGHT_READ_WRITE, FW_POLICY_STORE_FLAGS.FW_POLICY_STORE_FLAGS_NONE, out policyHandle); // for testing only
        }

        public void Dispose()
        {
            FWClosePolicyStore(policyHandle);
            policyHandle = IntPtr.Zero;
        }

        public virtual void LogError(string message, params object[] args)
        {
            Console.WriteLine("Log: " + string.Format(message, args));
        }

        private class RuleCompat : IDisposable
        {
            int Size = 0;
            IntPtr Buffer = IntPtr.Zero;

            [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
            public static extern void CopyMemory(IntPtr destination, IntPtr source, int length);

            [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
            public static extern void FillMemory(IntPtr destination, int length, byte fill);

            public RuleCompat(ushort fwApiVersion)
            {
                switch (fwApiVersion)                                               // for every version enter the first invalid field name belonging to the next version
                {
                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_SEVEN:         Size = (int)Marshal.OffsetOf(typeof(FW_RULE), "pMetaData"); break;

                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8:
                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN10:         Size = (int)Marshal.OffsetOf(typeof(FW_RULE), "OnNetworkNames"); break;

                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD:     Size = (int)Marshal.OffsetOf(typeof(FW_RULE), "wFlags2"); break;
                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD2:    Size = (int)Marshal.OffsetOf(typeof(FW_RULE), "RemoteOutServerNames"); break;
                    case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_REDSTONE1:     Size = (int)Marshal.OffsetOf(typeof(FW_RULE), "wszFqbn"); break;

                    //case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_170x:
                    //case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_180x:
                    //case (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_19Hx:
                    default:                                                        Size = Marshal.SizeOf(typeof(FW_RULE)); break;
                }

                int MaxSize = Marshal.SizeOf(typeof(FW_RULE));
#if (!DEBUG)
                if (Size < MaxSize)
#endif
                {
                    Buffer = Marshal.AllocHGlobal(MaxSize);
                    FillMemory(Buffer, MaxSize, 0);
                }
            }

            public FW_RULE MapRule(IntPtr fwRule)
            {
                if (Buffer != IntPtr.Zero)
                {
                    CopyMemory(Buffer, fwRule, Size);

                    return (FW_RULE)Marshal.PtrToStructure(Buffer, typeof(FW_RULE));
                }
                else
                    return (FW_RULE)Marshal.PtrToStructure(fwRule, typeof(FW_RULE));
            }

            public void Dispose()
            {
                if (Buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(Buffer);
            }
        }

        public List<FirewallRule> LoadRules()
        {
            RuleCounter = 0;
            Rules.Clear();

            List<FirewallRule> rules = new List<FirewallRule>();

            FW_ENUM_RULES_FLAGS wFlags = FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_NONE;
            //wFlags |= FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_NAME | FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_DESCRIPTION;
            //wFlags |= FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_APPLICATION;
            //wFlags |= FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_KEYWORD; // for testing only dynamic_store


            uint dwNumRules;
            IntPtr fwRules;
            if (FWEnumFirewallRules(policyHandle, FW_RULE_STATUS_CLASS.FW_RULE_STATUS_CLASS_ALL, FW_PROFILE_TYPE.FW_PROFILE_TYPE_ALL, wFlags, out dwNumRules, out fwRules) != ERROR_SUCCESS)
                return null;

            RuleCompat Compat = new RuleCompat(fwApiVersion);

            IntPtr fwRule = fwRules;
            for (int i = 0; i < dwNumRules; i++)
            {
                //FW_RULE entry = (FW_RULE)Marshal.PtrToStructure(fwRule, typeof(FW_RULE));
                FW_RULE entry = Compat.MapRule(fwRule);
                fwRule = entry.pNext;

                FirewallRule rule = new FirewallRule();
                if (LoadRule(rule, entry))
                {
                    rule.Index = (int)dwNumRules - ++RuleCounter;

                    NetFwRule FwRule = new NetFwRule() { Rule = rule };
                    //FwRule.Entry = entry;
                    //FwRule.Entry.pNext = IntPtr.Zero;
                    Rules.Add(rule.guid, FwRule );
                    rules.Add(rule);
                }
            }

            Compat.Dispose();

            FWFreeFirewallRules(fwRules);

            return rules;
        }


        public Dictionary<string, FirewallRule> LoadRules(string[] ruleIds)
        {
            Dictionary<string, FirewallRule> rules = new Dictionary<string, FirewallRule>();
            
            FW_QUERY ruleQuery = new FW_QUERY();
            ruleQuery.dwNumEntries = 0U;
            ruleQuery.pORConditions = IntPtr.Zero;
            ruleQuery.Status = FW_RULE_STATUS.FW_RULE_STATUS_ALL;
            ruleQuery.wSchemaVersion = fwApiVersion;

            List<IntPtr> buffers = new List<IntPtr>();
            List<GCHandle> handles = new List<GCHandle>();

            ruleQuery.dwNumEntries = (uint)ruleIds.Length;
            FW_QUERY_CONDITIONS[] fwQueryConditionList = new FW_QUERY_CONDITIONS[ruleQuery.dwNumEntries];
            for (int i = 0; i < ruleQuery.dwNumEntries; i++)
            {
                FW_QUERY_CONDITION[] fwQueryConditions = new FW_QUERY_CONDITION[1];

                FW_MATCH_VALUE fwMatchValue = new FW_MATCH_VALUE();
                fwMatchValue.type = FW_DATA_TYPE.FW_DATA_TYPE_UNICODE_STRING;

                fwMatchValue.data = new FW_MATCH_VALUE_DATA();
                fwMatchValue.data.pString = Marshal.StringToCoTaskMemUni(ruleIds[i]);
                buffers.Add(fwMatchValue.data.pString);

                fwQueryConditions[0].matchType = FW_MATCH_TYPE.FW_MATCH_TYPE_TRAFFIC_MATCH;
                fwQueryConditions[0].matchKey = FW_MATCH_KEY.FW_MATCH_KEY_OBJECTID;
                fwQueryConditions[0].matchValue = fwMatchValue;

                fwQueryConditionList[i].dwNumEntries = 1;
                handles.Add(GCHandle.Alloc(fwQueryConditions, GCHandleType.Pinned));
                fwQueryConditionList[i].pAndedConditions = Marshal.UnsafeAddrOfPinnedArrayElement(fwQueryConditions, 0);
            }
            handles.Add(GCHandle.Alloc(fwQueryConditionList, GCHandleType.Pinned));
            ruleQuery.pORConditions = Marshal.UnsafeAddrOfPinnedArrayElement(fwQueryConditionList, 0);

            IntPtr pRuleQuery = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(FW_QUERY)));
            Marshal.StructureToPtr(ruleQuery, pRuleQuery, false);



            FW_ENUM_RULES_FLAGS wFlags = FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_NONE;
            //wFlags |= FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_NAME | FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_DESCRIPTION;
            //wFlags |= FW_ENUM_RULES_FLAGS.FW_ENUM_RULES_FLAG_RESOLVE_APPLICATION;

            RuleCompat Compat = new RuleCompat(fwApiVersion);

            uint dwNumRules;
            IntPtr fwRules;
            if (FWQueryFirewallRules(policyHandle, pRuleQuery, wFlags, out dwNumRules, out fwRules) == ERROR_SUCCESS)
            {
                IntPtr fwRule = fwRules;
                for (int i = 0; i < dwNumRules; i++)
                {
                    //FW_RULE entry = (FW_RULE)Marshal.PtrToStructure(fwRule, typeof(FW_RULE));
                    FW_RULE entry = Compat.MapRule(fwRule);
                    fwRule = entry.pNext;

                    FirewallRule rule = new FirewallRule();
                    if (LoadRule(rule, entry))
                        rules.Add(rule.guid, rule);
                }

                FWFreeFirewallRules(fwRules);
            }

            Compat.Dispose();

            Marshal.FreeCoTaskMem(pRuleQuery);
            foreach (IntPtr handle in buffers)
                Marshal.FreeCoTaskMem(handle);
            foreach (GCHandle handle in handles)
                handle.Free();

            // update rule cache with most up to date values
            foreach (string guid in ruleIds)
            {
                FirewallRule rule;
                if (rules.TryGetValue(guid, out rule))
                {
                    NetFwRule FwRule;
                    if (!Rules.TryGetValue(guid, out FwRule))
                    {
                        rule.Index = RuleCounter++;

                        FwRule = new NetFwRule();
                        Rules.Add(rule.guid, FwRule);
                    }
                    else
                        rule.Index = FwRule.Rule.Index;

                    FwRule.Rule = rule;
                }
                else if(Rules.ContainsKey(guid))
                    Rules.Remove(guid);
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

        public Dictionary<string, FirewallRule> GetRules(string[] ruleIds)
        {
            Dictionary<string, FirewallRule> rules = new Dictionary<string, FirewallRule>();
            foreach (var guid in ruleIds)
            {
                NetFwRule FwRule;
                if (Rules.TryGetValue(guid, out FwRule))
                    rules.Add(guid, FwRule.Rule);
            }
            return rules;
        }

        public bool UpdateRule(FirewallRule rule)
        {
            NetFwRule FwRule;

            bool bAdd = false;
            if (rule.guid == null || !Rules.TryGetValue(rule.guid, out FwRule))
            {
                bAdd = true;

                if(rule.guid == null)
                    rule.guid = Guid.NewGuid().ToString("B").ToUpperInvariant();

                FwRule = new NetFwRule() { Rule = rule };
                //FwRule.Entry = new FW_RULE();
                //FwRule.Entry.pNext = IntPtr.Zero;
            }
            else // update rule
                FwRule.Rule = rule;

            //ref FW_RULE entry = ref FwRule.Entry;
            FW_RULE entry = new FW_RULE();

            List<GCHandle> handles = new List<GCHandle>();
            bool bRet = SaveRule(rule, ref entry, ref handles);
            foreach (GCHandle handle in handles)
                handle.Free();
            if (!bRet)
                return false;

            uint uRet = bAdd ? ERROR_RULE_NOT_FOUND : FWSetFirewallRule(policyHandle, ref entry);
            if (uRet == ERROR_RULE_NOT_FOUND)
                uRet = FWAddFirewallRule(policyHandle, ref entry);

            if (uRet != ERROR_SUCCESS)
            {
                string message = null;
                try
                {
                    uint msgSize = 0;
                    if (FWStatusMessageFromStatusCode(entry.Status, null, out msgSize) == ERROR_SUCCESS && msgSize != 0)
                    {
                        msgSize++;
                        StringBuilder pszMsg = new StringBuilder((int)msgSize);
                        if (FWStatusMessageFromStatusCode(entry.Status, pszMsg, out msgSize) == ERROR_SUCCESS)
                            message = pszMsg.ToString();
                    }
                }
                catch { }

                if(message != null)
                    LogError("Failed to Write rule, status-code: {0} ({1})", entry.Status.ToString(), message);
                else
                    LogError("Failed to Write rule, error-code: {0} ({1})", uRet.ToString(), new Win32Exception((int)uRet).Message);
            }
            else if (bAdd)
            {
                rule.Index = RuleCounter++;

                Rules.Add(rule.guid, FwRule);
                //LogInfo("Added rule: {0}", FwRule.Rule.Name);
            }
            //else
            //    LogInfo("Updated rule: {0}", FwRule.Rule.Name);

            return uRet == ERROR_SUCCESS;
        }

        public bool RemoveRule(string guid)
        {
            NetFwRule FwRule;
            if (!Rules.TryGetValue(guid, out FwRule))
                return true; // tne rule is already gone

            uint uRet = FWDeleteFirewallRule(policyHandle, FwRule.Rule.guid); // FwRule.Entry.wszRuleId
            if (uRet != ERROR_SUCCESS)
                LogError("Failed to Remove rule, error-code: " + uRet.ToString());
            else
            {
                Rules.Remove(guid);
                //LogInfo("Removed rule: {0}", FwRule.Rule.Name);
            }

            return uRet == ERROR_SUCCESS;
        }

        public static bool LoadRule(FirewallRule rule, FW_RULE entry)
        {
            rule.guid = entry.wszRuleId;

            rule.BinaryPath = entry.wszLocalApplication;
            rule.ServiceTag = entry.wszLocalService;
            if(entry.wSchemaVersion >= (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
                rule.AppSID = entry.wszPackageId;


            rule.Name = entry.wszName;
            rule.Description = entry.wszDescription;
            rule.Grouping = entry.wszEmbeddedContext;

            rule.Enabled = GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ACTIVE);

            switch (entry.Direction)
            {
                case FW_DIRECTION.FW_DIR_IN: rule.Direction = FirewallRule.Directions.Inbound; break;
                case FW_DIRECTION.FW_DIR_OUT: rule.Direction = FirewallRule.Directions.Outbound; break;
                default:
                    AppLog.Debug("Unsupported direction in rule: {0}", rule.Name);
                    break;
            }

            switch (entry.Action)
            {
                case FW_RULE_ACTION.FW_RULE_ACTION_ALLOW: rule.Action = FirewallRule.Actions.Allow; break;
                case FW_RULE_ACTION.FW_RULE_ACTION_BLOCK: rule.Action = FirewallRule.Actions.Block; break;
                default:
                    AppLog.Debug("Unsupported action in rule: {0}", rule.Name);
                    break;
            }

            rule.Profile = (int)entry.dwProfiles;

            //FW_INTERFACE_LUIDS LocalInterfaceIds;

            rule.Interface = (int)entry.dwLocalInterfaceTypes;

            rule.Protocol = (int)entry.wIpProtocol;
            switch (rule.Protocol)
            {
                case (int)FirewallRule.KnownProtocols.ICMP:
                case (int)FirewallRule.KnownProtocols.ICMPv6:
                    rule.IcmpTypesAndCodes = GetIcmpTypesAndCodes(entry.ProtocolUnion.TypeCodeList);
                    break;
                case (int)FirewallRule.KnownProtocols.TCP:
                case (int)FirewallRule.KnownProtocols.UDP:
                    rule.LocalPorts = GetPortsString(entry.ProtocolUnion.Ports.LocalPorts);
                    rule.RemotePorts = GetPortsString(entry.ProtocolUnion.Ports.RemotePorts);
                    break;
            }

            rule.LocalAddresses = GetAddressesString(entry.LocalAddresses);
            rule.RemoteAddresses = GetAddressesString(entry.RemoteAddresses);

            if (GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE))
                rule.EdgeTraversal = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW;
            else if (GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_APP))
                rule.EdgeTraversal = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP;
            else if (GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_USER))
                rule.EdgeTraversal = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER;
            else
                rule.EdgeTraversal = 0;

            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_LOOSE_SOURCE_MAPPED)

            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_AUTHENTICATE)
            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_AUTH_WITH_NO_ENCAPSULATION)
            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_AUTHENTICATE_WITH_ENCRYPTION)
            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_AUTH_WITH_ENC_NEGOTIATE)
            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_AUTHENTICATE_BYPASS_OUTBOUND)


            //string RemoteMachineAuthorizationList;
            //string RemoteUserAuthorizationList;
            rule.OsPlatformValidity = GetPlatformValidity(entry.PlatformValidity);
            //rule.Status = (uint)entry.Status;
            //FW_RULE_ORIGIN_TYPE Origin;
            //string GPOName;
            //uint Reserved;

            //if (entry.wSchemaVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
            //    return true;
            //GetRuleFlag(entry, FW_RULE_FLAGS.FW_RULE_FLAGS_LOCAL_ONLY_MAPPED)
            //IntPtr pMetaData;
            //string LocalUserAuthorizationList;
            //string LocalUserOwner;
            //FW_TRUST_TUPLE_KEYWORD dwTrustTupleKeywords;



            //if (entry.wSchemaVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD) // 1507
            //    return true;
            //GetRuleFlag(entry, FW_RULE_FLAGS_LUA_CONDITIONAL_ACE)
            //FW_NETWORK_NAMES OnNetworkNames;
            //string SecurityRealmId;

            //if (entry.wSchemaVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD2) // 1511
            //    return true;
            //FW_RULE_FLAGS2 wFlags2;

            //if (entry.wSchemaVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_REDSTONE1) // 1607
            //    return true;
            //FW_NETWORK_NAMES RemoteOutServerNames;

            //if (entry.wSchemaVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_170x) // 1703
            //    return true;
            //string Fqbn;
            //uint compartmentId;

            return true;
        }

        public static bool SaveRule(FirewallRule rule, ref FW_RULE entry, ref List<GCHandle> handles)
        {
            entry.pNext = IntPtr.Zero;
            entry.wSchemaVersion = fwApiVersion;

            entry.wszRuleId = rule.guid;

            entry.wszLocalApplication = rule.BinaryPath;
            entry.wszLocalService = rule.ServiceTag;
            if (entry.wSchemaVersion >= (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
                entry.wszPackageId = rule.AppSID;


            entry.wszName = rule.Name;
            entry.wszDescription = rule.Description;
            entry.wszEmbeddedContext = rule.Grouping;

            entry.wFlags = FW_RULE_FLAGS.FW_RULE_FLAGS_NONE;
            SetRuleFlag(ref entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ACTIVE, rule.Enabled);

            switch (rule.Direction)
            {
                case FirewallRule.Directions.Inbound: entry.Direction = FW_DIRECTION.FW_DIR_IN; break;
                case FirewallRule.Directions.Outbound: entry.Direction = FW_DIRECTION.FW_DIR_OUT; break;
            }

            switch (rule.Action)
            {
                case FirewallRule.Actions.Allow: entry.Action = FW_RULE_ACTION.FW_RULE_ACTION_ALLOW; break;
                case FirewallRule.Actions.Block: entry.Action = FW_RULE_ACTION.FW_RULE_ACTION_BLOCK; break;
            }

            entry.dwProfiles = (FW_PROFILE_TYPE)rule.Profile;

            entry.LocalInterfaceIds.dwNumLUIDs = 0;
            entry.LocalInterfaceIds.pLUIDs = IntPtr.Zero;
                
            entry.dwLocalInterfaceTypes = (FW_INTERFACE_TYPE)rule.Interface;

            entry.wIpProtocol = (IP_PROTOCOL)rule.Protocol;
            switch (rule.Protocol)
            {
                case (int)FirewallRule.KnownProtocols.ICMP:
                case (int)FirewallRule.KnownProtocols.ICMPv6:
                    entry.ProtocolUnion.TypeCodeList = MakeIcmpTypesAndCodes(rule.IcmpTypesAndCodes, ref handles);
                    break;
                case (int)FirewallRule.KnownProtocols.TCP:
                case (int)FirewallRule.KnownProtocols.UDP:
                    entry.ProtocolUnion.Ports.LocalPorts = MakePortsArray(rule.LocalPorts, ref handles);
                    entry.ProtocolUnion.Ports.RemotePorts = MakePortsArray(rule.RemotePorts, ref handles);
                    break;
            }

            entry.LocalAddresses = MakeAddressList(rule.LocalAddresses, ref handles);
            entry.RemoteAddresses = MakeAddressList(rule.RemoteAddresses, ref handles);

            SetRuleFlag(ref entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE, (rule.EdgeTraversal == (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW));
            SetRuleFlag(ref entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_APP, (rule.EdgeTraversal == (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP));
            SetRuleFlag(ref entry, FW_RULE_FLAGS.FW_RULE_FLAGS_ROUTEABLE_ADDRS_TRAVERSE_DEFER_USER, (rule.EdgeTraversal == (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER));

            //entry.wFlags

            entry.wszRemoteMachineAuthorizationList = null;
            entry.wszRemoteUserAuthorizationList = null;

            if (entry.wszPackageId != null && entry.wszPackageId.Length > 0
                /*|| (entry.LocalUserAuthorizationList != null && entry.LocalUserAuthorizationList.Length > 0)
                || (entry.LocalUserOwner != null && entry.LocalUserOwner.Length > 0) 
                || entry.dwTrustTupleKeywords != FW_TRUST_TUPLE_KEYWORD.FW_TRUST_TUPLE_KEYWORD_NONE*/)
            {
                rule.OsPlatformValidity = new FW_OS_PLATFORM[1];
                rule.OsPlatformValidity[0].Platform = 10;
                rule.OsPlatformValidity[0].MajorVersion = 6;
                rule.OsPlatformValidity[0].MinorVersion = 2; // win 8
                rule.OsPlatformValidity[0].Reserved = 0;
            }
            else
                rule.OsPlatformValidity = null;
            entry.PlatformValidity = MakePlatformValidity(rule.OsPlatformValidity, ref handles);

            entry.Status = FW_RULE_STATUS.FW_RULE_STATUS_OK;
            entry.Origin = FW_RULE_ORIGIN_TYPE.FW_RULE_ORIGIN_LOCAL;
            entry.wszGPOName = null;
            entry.Reserved = (uint)FW_OBJECT_CTRL_FLAG.FW_OBJECT_CTRL_FLAG_NONE;

            // Windows 8+
            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
                return true;
            entry.pMetaData = IntPtr.Zero;
            entry.wszLocalUserAuthorizationList = null;
            entry.wszLocalUserOwner = null;
            entry.dwTrustTupleKeywords = FW_TRUST_TUPLE_KEYWORD.FW_TRUST_TUPLE_KEYWORD_NONE;


            // Windows 10+
            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD) // 1507
                return true;
            entry.OnNetworkNames.dwNumEntries = 0;
            entry.OnNetworkNames.wszNames = IntPtr.Zero;
            entry.wszSecurityRealmId = null;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_THRESHOLD2) // 1511
                return true;
            entry.wFlags2 = FW_RULE_FLAGS2.FW_RULE_FLAGS2_NONE;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_REDSTONE1) // 1607
                return true;
            entry.RemoteOutServerNames.dwNumEntries = 0;
            entry.RemoteOutServerNames.wszNames = IntPtr.Zero;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_170x) // 1703
                return true;
            entry.wszFqbn = null;
            entry.compartmentId = 0;

            return true;
        }


        ////////////////////////////////////////////////
        // Flags

        private static bool GetRuleFlag(FW_RULE entry, FW_RULE_FLAGS flag)
        {
            return (uint)(entry.wFlags & flag) > 0U;
        }

        private static void SetRuleFlag(ref FW_RULE entry, FW_RULE_FLAGS flag, bool value)
        {
            if (value)
                entry.wFlags |= flag;
            else
                entry.wFlags &= ~flag;
        }

        private static bool GetRuleFlag2(FW_RULE entry, FW_RULE_FLAGS2 flag)
        {
            return (uint)(entry.wFlags2 & flag) > 0U;
        }

        private static void SetRuleFlag2(ref FW_RULE entry, FW_RULE_FLAGS2 flag, bool value)
        {
            if (value)
                entry.wFlags2 |= flag;
            else
                entry.wFlags2 &= ~flag;
        }


        ////////////////////////////////////////////////
        // Icmp

        private static FW_ICMP_TYPE_CODE[] GetIcmpTypesAndCodes(FW_ICMP_TYPE_CODE_LIST TypeCodeList)
        {
            if (TypeCodeList.dwNumEntries == 0)
                return null;

            FW_ICMP_TYPE_CODE[] IcmpTypesAndCodes = new FW_ICMP_TYPE_CODE[TypeCodeList.dwNumEntries];
            IntPtr ptr = TypeCodeList.pEntries;
            for (int i = 0; i < TypeCodeList.dwNumEntries; i++)
            {
                IcmpTypesAndCodes[i] = (FW_ICMP_TYPE_CODE)Marshal.PtrToStructure(ptr, typeof(FW_ICMP_TYPE_CODE));
                ptr = (IntPtr)(ptr.ToInt64() + Marshal.SizeOf(typeof(FW_ICMP_TYPE_CODE)));
            }
            return IcmpTypesAndCodes;
        }

        private static FW_ICMP_TYPE_CODE_LIST MakeIcmpTypesAndCodes(FW_ICMP_TYPE_CODE[] IcmpTypesAndCodes, ref List<GCHandle> handles)
        {
            FW_ICMP_TYPE_CODE_LIST TypeCodeList;
            if (IcmpTypesAndCodes == null || IcmpTypesAndCodes.Length == 0)
            {
                TypeCodeList.dwNumEntries = 0;
                TypeCodeList.pEntries = IntPtr.Zero;
            }
            else
            {
                TypeCodeList.dwNumEntries = (ushort)IcmpTypesAndCodes.Length;
                handles.Add(GCHandle.Alloc(IcmpTypesAndCodes, GCHandleType.Pinned));
                TypeCodeList.pEntries = Marshal.UnsafeAddrOfPinnedArrayElement(IcmpTypesAndCodes, 0);
            }
            return TypeCodeList;
        }

        ////////////////////////////////////////////////
        // Ports

        private struct PortRange
        {
            public ushort Begin;
            public ushort End;
        }

        private static FW_PORT_KEYWORD MakeSpecialPort(List<string> portList)
        {
            FW_PORT_KEYWORD fwPortKeyword = FW_PORT_KEYWORD.FW_PORT_KEYWORD_NONE;

            if (portList.Contains(FirewallRule.PortKeywordRpc))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_DYNAMIC_RPC_PORTS;
            if (portList.Contains(FirewallRule.PortKeywordRpcEp))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_RPC_EP;
            if (portList.Contains(FirewallRule.PortKeywordTeredo))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_TEREDO_PORT;
            if (portList.Contains(FirewallRule.PortKeywordIpTlsIn))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_IP_TLS_IN;
            if (portList.Contains(FirewallRule.PortKeywordIpTlsOut))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_IP_TLS_OUT; // outgoing

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
                return fwPortKeyword;

            if (portList.Contains(FirewallRule.PortKeywordDhcp))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_DHCP;
            if (portList.Contains(FirewallRule.PortKeywordPly2Disc))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_PLAYTO_DISCOVERY;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN10)
                return fwPortKeyword;

            if (portList.Contains(FirewallRule.PortKeywordMDns))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_MDNS;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_REDSTONE1)
                return fwPortKeyword;

            if (portList.Contains(FirewallRule.PortKeywordCortan))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_CORTANA_OUT; // outgoing

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_19Hx)
                return fwPortKeyword;

            if (portList.Contains(FirewallRule.PortKeywordProximalTcpCdn))
                fwPortKeyword |= FW_PORT_KEYWORD.FW_PORT_KEYWORD_PROXIMAL_TCP_CDP;

            return fwPortKeyword;
        }

        private static FW_PORTS MakePortsArray(string portsStr, ref List<GCHandle> handles)
        {
            FW_PORTS ports = new FW_PORTS();
            ports.Ports.dwNumEntries = 0;
            ports.Ports.pPorts = IntPtr.Zero;
            if (portsStr == null)
                return ports;

            List<string> portList = portsStr.Split(',').ToList();

            ports.wPortKeywords = MakeSpecialPort(portList);

            List<PortRange> portRanges = new List<PortRange>();

            foreach (string portStr in portList)
            {
                string[] portRange = portStr.Split('-');
                if (portRange.Length == 2)
                {
                    ushort begin;
                    ushort end;
                    if (ushort.TryParse(portRange[0], out begin) && ushort.TryParse(portRange[1], out end))
                    {
                        portRanges.Add(new PortRange()
                        {
                            Begin = begin,
                            End = end
                        });
                    }
                }
                else
                {
                    ushort port;
                    if (ushort.TryParse(portStr, out port))
                    {
                        portRanges.Add(new PortRange()
                        {
                            Begin = port,
                            End = port
                        });
                    }
                }
            }

            if (portRanges.Count > 0)
            {
                FW_PORT_RANGE[] portRangeArray = new FW_PORT_RANGE[portRanges.Count];
                for (int i = 0; i < portRanges.Count; i++)
                {
                    portRangeArray[i].uBegin = portRanges[i].Begin;
                    portRangeArray[i].uEnd = portRanges[i].End;
                }

                ports.Ports.dwNumEntries = (ushort)portRanges.Count;
                handles.Add(GCHandle.Alloc(portRangeArray, GCHandleType.Pinned));
                ports.Ports.pPorts = Marshal.UnsafeAddrOfPinnedArrayElement(portRangeArray, 0);
            }

            return ports;
        }

        private static List<string> GetSpecialPorts(FW_PORT_KEYWORD portKeywords)
        {
            List<string> portList = new List<string>();

            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_DYNAMIC_RPC_PORTS)
                portList.Add(FirewallRule.PortKeywordRpc);
            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_RPC_EP)
                portList.Add(FirewallRule.PortKeywordRpcEp);
            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_TEREDO_PORT)
                portList.Add(FirewallRule.PortKeywordTeredo);
            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_IP_TLS_IN)
                portList.Add(FirewallRule.PortKeywordIpTlsIn);
            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_IP_TLS_OUT)
                portList.Add(FirewallRule.PortKeywordIpTlsOut); // outgoing

            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_DHCP)
                portList.Add(FirewallRule.PortKeywordDhcp);
            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_PLAYTO_DISCOVERY)
                portList.Add(FirewallRule.PortKeywordPly2Disc);

            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_MDNS)
                portList.Add(FirewallRule.PortKeywordMDns);

            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_CORTANA_OUT)
                portList.Add(FirewallRule.PortKeywordCortan); // outgoing

            if (portKeywords == FW_PORT_KEYWORD.FW_PORT_KEYWORD_PROXIMAL_TCP_CDP)
                portList.Add(FirewallRule.PortKeywordProximalTcpCdn);

            return portList;
        }

        private static string GetPortsString(FW_PORTS ports)
        {
            List<string> portList = GetSpecialPorts(ports.wPortKeywords);

            for (int i = 0; i < ports.Ports.dwNumEntries; i++)
            {
                FW_PORT_RANGE portRange = (FW_PORT_RANGE)Marshal.PtrToStructure((IntPtr)(ports.Ports.pPorts.ToInt64() + (Marshal.SizeOf(typeof(FW_PORT_RANGE)) * i)), typeof(FW_PORT_RANGE));
                portList.Add(portRange.uBegin >= portRange.uEnd ? portRange.uBegin.ToString() : portRange.uBegin.ToString() + "-" + portRange.uEnd.ToString());
            }

            return string.Join(",", portList);
        }

        ////////////////////////////////////////////////
        // Addresses

        private struct IpRange
        {
            public uint Begin;
            public uint End;
        }

        private struct IpRangeV6
        {
            public byte[] Begin;
            public byte[] End;
        }

        private struct IpSubnet
        {
            public uint Address;
            public uint Subnet;
        }

        private struct IpSubnetV6
        {
            public byte[] Address;
            public ushort Subnet;
        }

        private static bool IsIPv4(string ipv4)
        {
            IPAddress address;
            if (IPAddress.TryParse(ipv4, out address))
                return address.AddressFamily == AddressFamily.InterNetwork;
            return false;
        }

        private static uint ParseIPv4(string ipv4)
        {
            IPAddress address;
            if (IPAddress.TryParse(ipv4, out address) && address.AddressFamily == AddressFamily.InterNetwork)
                return (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(address.GetAddressBytes(), 0));
            return 0;
        }

        private static bool IsIPv6(string ipv6)
        {
            IPAddress address;
            if (IPAddress.TryParse(ipv6, out address))
                return address.AddressFamily == AddressFamily.InterNetworkV6;
            return false;
        }

        private static byte[] ParseIPv6(string ipv6)
        {
            IPAddress address;
            if (IPAddress.TryParse(ipv6, out address) && address.AddressFamily == AddressFamily.InterNetworkV6)
                return address.GetAddressBytes();
            return IPAddress.IPv6None.GetAddressBytes();
        }

        private static bool TestV6RangeOrder(byte[] v6Address1, byte[] v6Address2)
        {
            for (int i = 0; i < 16; i++)
            {
                if (v6Address1[i] < v6Address2[i])
                    return true;
                if (v6Address1[i] > v6Address2[i])
                    return false;
            }
            return false;
        }

        private static uint IntSubNet(uint value)
        {
            uint subnet = 0;
            for (int i = 31; (long)i >= (long)(32U - value); i--)
                subnet |= (uint)(1 << i);
            return subnet;
        }

        public static void PutIPv6Address(byte[] addr, ref FW_IPV6_ADDRESS ipv6)
        {
            if (addr.Length != 16)
            {
                ipv6.a1 = ipv6.a2 = ipv6.a3 = ipv6.a4 = 0;
                return;
            }
            ipv6.a1 = BitConverter.ToUInt32(addr, 0);
            ipv6.a2 = BitConverter.ToUInt32(addr, 4);
            ipv6.a3 = BitConverter.ToUInt32(addr, 8);
            ipv6.a4 = BitConverter.ToUInt32(addr, 12);
        }

        private static FW_ADDRESS_KEYWORD MakeSpecialAddress(List<string> addressList)
        {
            FW_ADDRESS_KEYWORD fwAddressKeyword = FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_NONE;

            if (addressList.Contains(FirewallRule.AddrKeywordLocalSubnet))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_LOCAL_SUBNET;
            if (addressList.Contains(FirewallRule.AddrKeywordDNS))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DNS;
            if (addressList.Contains(FirewallRule.AddrKeywordDHCP))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DHCP;
            if (addressList.Contains(FirewallRule.AddrKeywordWINS))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_WINS;
            if (addressList.Contains(FirewallRule.AddrKeywordDefaultGateway))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DEFAULT_GATEWAY;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_WIN8)
                return fwAddressKeyword;

            if (addressList.Contains(FirewallRule.AddrKeywordIntrAnet))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_INTRANET;
            if (addressList.Contains(FirewallRule.AddrKeywordIntErnet))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_INTERNET;
            if (addressList.Contains(FirewallRule.AddrKeywordPly2Renders))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_PLAYTO_RENDERERS;
            if (addressList.Contains(FirewallRule.AddrKeywordRmtIntrAnet))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_REMOTE_INTRANET;

            if (fwApiVersion < (ushort)FW_BINARY_VERSION.FW_BINARY_VERSION_19Hx)
                return fwAddressKeyword;

            if (addressList.Contains(FirewallRule.AddrKeywordCaptivePortal))
                fwAddressKeyword |= FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_CAPTIVE_PORTAL;

            return fwAddressKeyword;
        }

        private static FW_ADDRESSES MakeAddressList(string addressesStr, ref List<GCHandle> handles)
        {
            FW_ADDRESSES addresses = new FW_ADDRESSES();
            addresses.V4SubNets.dwNumEntries = 0;
            addresses.V4SubNets.pSubNets = IntPtr.Zero;
            addresses.V4Ranges.dwNumEntries = 0;
            addresses.V4Ranges.pRanges = IntPtr.Zero;
            addresses.V6SubNets.dwNumEntries = 0;
            addresses.V6SubNets.pSubNets = IntPtr.Zero;
            addresses.V6Ranges.dwNumEntries = 0;
            addresses.V6Ranges.pRanges = IntPtr.Zero;
            if (addressesStr == null)
                return addresses;

            List<string> addressList = addressesStr.Split(',').ToList();

            addresses.dwV4AddressKeywords = addresses.dwV6AddressKeywords = MakeSpecialAddress(addressList);

            List<IpSubnet> ipSubNets = new List<IpSubnet>();
            List<IpRange> ipRanges = new List<IpRange>();
            List<IpSubnetV6> ipSubNetsV6 = new List<IpSubnetV6>();
            List<IpRangeV6> ipRangesV6 = new List<IpRangeV6>();
            
            foreach (string addressStr in addressList)
            {
                string[] addrRange = addressStr.Split('-');
                if (addrRange.Length == 2)
                {
                    //  v4Range
                    if (IsIPv4(addrRange[0]) && IsIPv4(addrRange[1]))
                    {
                        var ipV4Range = new IpRange()
                        {
                            Begin = ParseIPv4(addrRange[0]),
                            End = ParseIPv4(addrRange[1])
                        };

                        if (ipV4Range.End > ipV4Range.Begin)
                            ipRanges.Add(ipV4Range);
                    }

                    //  v6Range
                    else if (IsIPv6(addrRange[0]) && IsIPv6(addrRange[1]))
                    {
                        var ipV6Range = new IpRangeV6()
                        {
                            Begin = ParseIPv6(addrRange[0]),
                            End = ParseIPv6(addrRange[1])
                        };

                        if (TestV6RangeOrder(ipV6Range.Begin, ipV6Range.End))
                            ipRangesV6.Add(ipV6Range);
                    }
                }
                else
                {
                    string[] addrSubNet = addressStr.Split('/');
                    if (addrSubNet.Length == 2)
                    {
                        // v4SubNet
                        if (IsIPv4(addrSubNet[0]))
                        {
                            uint subnet;
                            if (uint.TryParse(addrSubNet[1], out subnet) && subnet <= 32)
                            {
                                ipSubNets.Add(new IpSubnet()
                                {
                                    Address = ParseIPv4(addrSubNet[0]),
                                    Subnet = IntSubNet(subnet)
                                });
                            }
                        }

                        // v6SubNet
                        else if (IsIPv6(addrSubNet[0]))
                        {
                            ushort subnet;
                            if (ushort.TryParse(addrSubNet[1], out subnet) && subnet <= 128)
                            {
                                ipSubNetsV6.Add(new IpSubnetV6()
                                {
                                    Address = ParseIPv6(addrSubNet[0]),
                                    Subnet = subnet
                                });
                            }
                        }
                    }
                    else
                    {
                        // IPv4
                        if (IsIPv4(addressStr))
                        {
                            ipSubNets.Add(new IpSubnet()
                            {
                                Address = ParseIPv4(addressStr),
                                Subnet = 0xFFFFFFFF // /32
                            });
                        }

                        // IPv6
                        else if (IsIPv6(addressStr))
                        {
                            ipSubNetsV6.Add(new IpSubnetV6()
                            {
                                Address = ParseIPv6(addressStr),
                                Subnet = 128 // /128
                            });
                        }
                    }
                }
            }


            if (ipSubNets.Count > 0)
            {
                FW_IPV4_SUBNET[] fwIpV4SubnetArray = new FW_IPV4_SUBNET[ipSubNets.Count];
                for (int i = 0; i < ipSubNets.Count; i++)
                {
                    fwIpV4SubnetArray[i].dwAddress = ipSubNets[i].Address;
                    fwIpV4SubnetArray[i].dwSubNetMask = ipSubNets[i].Subnet;
                }

                addresses.V4SubNets.dwNumEntries = (ushort)ipSubNets.Count;
                handles.Add(GCHandle.Alloc(fwIpV4SubnetArray, GCHandleType.Pinned));
                addresses.V4SubNets.pSubNets = Marshal.UnsafeAddrOfPinnedArrayElement(fwIpV4SubnetArray, 0);
            }

            if (ipRanges.Count > 0)
            {
                FW_IPV4_ADDRESS_RANGE[] ipV4AddressRangeArray = new FW_IPV4_ADDRESS_RANGE[ipRanges.Count];
                for (int i = 0; i < ipRanges.Count; i++)
                {
                    ipV4AddressRangeArray[i].dwBegin = ipRanges[i].Begin;
                    ipV4AddressRangeArray[i].dwEnd = ipRanges[i].End;
                }

                addresses.V4Ranges.dwNumEntries = (ushort)ipRanges.Count;
                handles.Add(GCHandle.Alloc(ipV4AddressRangeArray, GCHandleType.Pinned));
                addresses.V4Ranges.pRanges = Marshal.UnsafeAddrOfPinnedArrayElement(ipV4AddressRangeArray, 0);
            }

            if (ipSubNetsV6.Count > 0)
            {
                FW_IPV6_SUBNET[] fwIpV6SubnetArray = new FW_IPV6_SUBNET[ipSubNetsV6.Count];
                for (int i = 0; i < ipSubNetsV6.Count; i++)
                {
                    PutIPv6Address(ipSubNetsV6[i].Address, ref fwIpV6SubnetArray[i].Address);
                    fwIpV6SubnetArray[i].wNumPrefixBits = ipSubNetsV6[i].Subnet;
                }

                addresses.V6SubNets.dwNumEntries = (ushort)ipSubNetsV6.Count;
                handles.Add(GCHandle.Alloc(fwIpV6SubnetArray, GCHandleType.Pinned));
                addresses.V6SubNets.pSubNets = Marshal.UnsafeAddrOfPinnedArrayElement(fwIpV6SubnetArray, 0);
            }

            if (ipRangesV6.Count > 0)
            {
                FW_IPV6_ADDRESS_RANGE[] ipV6AddressRangeArray = new FW_IPV6_ADDRESS_RANGE[ipRangesV6.Count];
                for (int i = 0; i < ipRangesV6.Count; i++)
                {
                    PutIPv6Address(ipRangesV6[i].Begin, ref ipV6AddressRangeArray[i].Begin);
                    PutIPv6Address(ipRangesV6[i].End, ref ipV6AddressRangeArray[i].End);
                }

                addresses.V6Ranges.dwNumEntries = (ushort)ipRangesV6.Count;
                handles.Add(GCHandle.Alloc(ipV6AddressRangeArray, GCHandleType.Pinned));
                addresses.V6Ranges.pRanges = Marshal.UnsafeAddrOfPinnedArrayElement(ipV6AddressRangeArray, 0);
            }

            return addresses;
        }

        private static string IPv4ToString(uint ipv4)
        {
            return new IPAddress((uint)IPAddress.HostToNetworkOrder((int)ipv4)).ToString();
        }

        private static string IPv6ToString(byte[] ipv6)
        {
            return new IPAddress(ipv6).ToString();
        }

        private static int SubNetToInt(uint subnet)
        {
            int value = 0;
            for (int i = 31; i >= 0 && ((int)subnet & 1 << i) != 0; i--)
                value++;
            return value;
        }

        public static byte[] GetIPv6Address(FW_IPV6_ADDRESS ipv6)
        {
            byte[] numArray = new byte[16];
            BitConverter.GetBytes(ipv6.a1).CopyTo((Array)numArray, 0);
            BitConverter.GetBytes(ipv6.a2).CopyTo((Array)numArray, 4);
            BitConverter.GetBytes(ipv6.a3).CopyTo((Array)numArray, 8);
            BitConverter.GetBytes(ipv6.a4).CopyTo((Array)numArray, 12);
            return numArray;
        }

        private static List<string> GetSpecialAddress(FW_ADDRESS_KEYWORD addressKeywords)
        {
            List<string> addressList = new List<string>();

            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_LOCAL_SUBNET) != 0)
                addressList.Add(FirewallRule.AddrKeywordLocalSubnet);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DNS) != 0)
                addressList.Add(FirewallRule.AddrKeywordDNS);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DHCP) != 0)
                addressList.Add(FirewallRule.AddrKeywordDHCP);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_WINS) != 0)
                addressList.Add(FirewallRule.AddrKeywordWINS);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_DEFAULT_GATEWAY) != 0)
                addressList.Add(FirewallRule.AddrKeywordDefaultGateway);

            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_INTRANET) != 0)
                addressList.Add(FirewallRule.AddrKeywordIntrAnet);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_INTERNET) != 0)
                addressList.Add(FirewallRule.AddrKeywordIntErnet);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_PLAYTO_RENDERERS) != 0)
                addressList.Add(FirewallRule.AddrKeywordPly2Renders);
            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_REMOTE_INTRANET) != 0)
                addressList.Add(FirewallRule.AddrKeywordRmtIntrAnet);

            if ((addressKeywords & FW_ADDRESS_KEYWORD.FW_ADDRESS_KEYWORD_CAPTIVE_PORTAL) != 0)
                addressList.Add(FirewallRule.AddrKeywordCaptivePortal);

            return addressList;
        }

        private static string GetAddressesString(FW_ADDRESSES addresses)
        {
            List<string> addressList = GetSpecialAddress(addresses.dwV4AddressKeywords | addresses.dwV6AddressKeywords);

            for (int i = 0; i < addresses.V4SubNets.dwNumEntries; i++)
            {
                FW_IPV4_SUBNET subNet = (FW_IPV4_SUBNET)Marshal.PtrToStructure((IntPtr)(addresses.V4SubNets.pSubNets.ToInt64() + Marshal.SizeOf(typeof(FW_IPV4_SUBNET)) * i), typeof(FW_IPV4_SUBNET));
                if (SubNetToInt(subNet.dwSubNetMask) == 32)
                    addressList.Add(IPv4ToString(subNet.dwAddress));
                else
                    addressList.Add(IPv4ToString(subNet.dwAddress) + "/" + SubNetToInt(subNet.dwSubNetMask));
            }

            for (int i = 0; i < addresses.V4Ranges.dwNumEntries; i++)
            {
                FW_IPV4_ADDRESS_RANGE ipRange = (FW_IPV4_ADDRESS_RANGE)Marshal.PtrToStructure((IntPtr)(addresses.V4Ranges.pRanges.ToInt64() + (Marshal.SizeOf(typeof(FW_IPV4_ADDRESS_RANGE)) * i)), typeof(FW_IPV4_ADDRESS_RANGE));
                if (ipRange.dwBegin == ipRange.dwEnd)
                    addressList.Add(IPv4ToString(ipRange.dwBegin));
                else
                    addressList.Add(IPv4ToString(ipRange.dwBegin) + "-" + IPv4ToString(ipRange.dwEnd));
            }

            for (int i = 0; i < addresses.V6SubNets.dwNumEntries; i++)
            {
                FW_IPV6_SUBNET subNet = (FW_IPV6_SUBNET)Marshal.PtrToStructure((IntPtr)(addresses.V6SubNets.pSubNets.ToInt64() + Marshal.SizeOf(typeof(FW_IPV6_SUBNET)) * i), typeof(FW_IPV6_SUBNET));
                if (subNet.wNumPrefixBits == 128)
                    addressList.Add(IPv6ToString(GetIPv6Address(subNet.Address)));
                else
                    addressList.Add(IPv6ToString(GetIPv6Address(subNet.Address)) + "/" + subNet.wNumPrefixBits);
            }

            for (int i = 0; i < addresses.V6Ranges.dwNumEntries; i++)
            {
                FW_IPV6_ADDRESS_RANGE ipRange = (FW_IPV6_ADDRESS_RANGE)Marshal.PtrToStructure((IntPtr)(addresses.V6Ranges.pRanges.ToInt64() + (32 * i)), typeof(FW_IPV6_ADDRESS_RANGE));
                if (ipRange.Begin.Equals(ipRange.End))
                    addressList.Add(IPv6ToString(GetIPv6Address(ipRange.Begin)));
                else
                    addressList.Add(IPv6ToString(GetIPv6Address(ipRange.Begin)) + "-" + IPv6ToString(GetIPv6Address(ipRange.End)));
            }

            return string.Join(",", addressList);
        }

        ////////////////////////////////////////////////
        // Platform Validity

        public static FW_OS_PLATFORM[] GetPlatformValidity(FW_OS_PLATFORM_LIST platformList)
        {
            if (platformList.NumEntries == 0U)
                return null;
            FW_OS_PLATFORM[] fwOsPlatformArray = new FW_OS_PLATFORM[(int)platformList.NumEntries];
            IntPtr ptr = platformList.Platforms;
            for (int i = 0; i < platformList.NumEntries; i++)
            {
                fwOsPlatformArray[i] = (FW_OS_PLATFORM)Marshal.PtrToStructure(ptr, typeof(FW_OS_PLATFORM));
                ptr = (IntPtr)(ptr.ToInt64() + Marshal.SizeOf(typeof(FW_OS_PLATFORM)));
            }
            return fwOsPlatformArray;
        }

        internal static FW_OS_PLATFORM_LIST MakePlatformValidity(FW_OS_PLATFORM[] platformListArray, ref List<GCHandle> handles)
        {
            FW_OS_PLATFORM_LIST fwOsPlatformList;
            if (platformListArray == null || platformListArray.Length == 0)
            {
                fwOsPlatformList.NumEntries = 0U;
                fwOsPlatformList.Platforms = IntPtr.Zero;
            }
            else
            {
                fwOsPlatformList.NumEntries = (uint)platformListArray.Length;
                handles.Add(GCHandle.Alloc(platformListArray, GCHandleType.Pinned));
                fwOsPlatformList.Platforms = Marshal.UnsafeAddrOfPinnedArrayElement(platformListArray, 0);
            }
            return fwOsPlatformList;
        }

        ////////////////////////////////////////////////
        // Settings

        private uint? GetFWConfig(FW_PROFILE_TYPE profile, FW_PROFILE_CONFIG conf)
        {
            FW_CONFIG_FLAGS dwFlags = FW_CONFIG_FLAGS.FW_CONFIG_FLAG_RETURN_DEFAULT_IF_NOT_FOUND;
            uint value;
            uint uSize = (uint)Marshal.SizeOf(typeof(uint));
            if (FWGetConfig(policyHandle, conf, profile, dwFlags, out value, out uSize) != ERROR_SUCCESS)
                return null;
            return value;
        }

        private uint SetFWConfig(FW_PROFILE_TYPE profile, FW_PROFILE_CONFIG conf, uint value)
        {
            uint uSize = (uint)Marshal.SizeOf(typeof(uint));
            return FWSetConfig(policyHandle, conf, profile, ref value, uSize);
        }


        public bool GetFirewallEnabled(FirewallRule.Profiles profileType)
        {
            return GetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_ENABLE_FW) == 1u;
        }

        public void SetFirewallEnabled(FirewallRule.Profiles profileType, bool Enabled)
        {
            SetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_ENABLE_FW, Enabled ? 1u : 0u);
        }

        public bool GetBlockAllInboundTraffic(FirewallRule.Profiles profileType)
        {
            return GetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_SHIELDED) == 1u;
        }

        public void SetBlockAllInboundTraffic(FirewallRule.Profiles profileType, bool Block)
        {
            SetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_SHIELDED, Block ? 1u : 0u);
        }

        public bool GetInboundEnabled(FirewallRule.Profiles profileType)
        {
            return GetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DISABLE_INBOUND_NOTIFICATIONS) == 0u;
        }

        public void SetInboundEnabled(FirewallRule.Profiles profileType, bool Enable)
        {
            SetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DISABLE_INBOUND_NOTIFICATIONS, Enable ? 0u : 1u);
        }

        public FirewallRule.Actions GetDefaultInboundAction(FirewallRule.Profiles profileType)
        {
            if (GetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DEFAULT_INBOUND_ACTION) == 1u)
                return FirewallRule.Actions.Block;
            return FirewallRule.Actions.Allow;
        }

        public void SetDefaultInboundAction(FirewallRule.Profiles profileType, FirewallRule.Actions Action)
        {
            SetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DEFAULT_INBOUND_ACTION, Action == FirewallRule.Actions.Block ? 1u : 0u);
        }

        public FirewallRule.Actions GetDefaultOutboundAction(FirewallRule.Profiles profileType)
        {
            if (GetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DEFAULT_OUTBOUND_ACTION) == 1u)
                return FirewallRule.Actions.Block;
            return FirewallRule.Actions.Allow;
        }

        public void SetDefaultOutboundAction(FirewallRule.Profiles profileType, FirewallRule.Actions Action)
        {
            SetFWConfig((FW_PROFILE_TYPE)profileType, FW_PROFILE_CONFIG.FW_PROFILE_CONFIG_DEFAULT_OUTBOUND_ACTION, Action == FirewallRule.Actions.Block ? 1u : 0u);
        }


        ////////////////////////////////////////////////
        // App Package List

        public class FwAppContainer
        {
            public SecurityIdentifier appContainerSid;
            //public SecurityIdentifier userSid;
            public string appContainerName;
            public string displayName;
            public string description;
        }

        public FwAppContainer[] GetAppContainers()
        {
            FwAppContainer[] fwAppContainers = null;

            uint pdwNumAppCs;
            IntPtr ppAppCs;
            if (NetworkIsolationEnumAppContainers(0, out pdwNumAppCs, out ppAppCs) != ERROR_SUCCESS)
                return fwAppContainers;
            
            try
            {
                fwAppContainers = new FwAppContainer[(int)pdwNumAppCs];
                long pAppCs = ppAppCs.ToInt64();
                for (int i = 0; i < pdwNumAppCs; i++)
                {
                    INET_FIREWALL_APP_CONTAINER inetFirewallAppContainer = (INET_FIREWALL_APP_CONTAINER)Marshal.PtrToStructure((IntPtr)pAppCs, typeof(INET_FIREWALL_APP_CONTAINER));
                    fwAppContainers[i] = new FwAppContainer()
                    {
                        appContainerSid = new SecurityIdentifier(inetFirewallAppContainer.appContainerSid),
                        //userSid = new SecurityIdentifier(inetFirewallAppContainer.userSid),
                        appContainerName = inetFirewallAppContainer.appContainerName,
                        displayName = inetFirewallAppContainer.displayName,
                        description = inetFirewallAppContainer.description
                    };
                    pAppCs += Marshal.SizeOf(typeof(INET_FIREWALL_APP_CONTAINER));
                }   
            }
            catch { }

            NetworkIsolationFreeAppContainers(ppAppCs);

            return fwAppContainers;
        }

        Dictionary<string, UwpFunc.AppInfo> AppPackages = new Dictionary<string, UwpFunc.AppInfo>();
        DateTime LastAppReload = DateTime.Now;

        public bool LoadAppPkgs()
        {
            AppPackages.Clear();
            LastAppReload = DateTime.Now;

            if (UwpFunc.IsWindows7OrLower)
                return false;

            var AppContainers = GetAppContainers();
            foreach (var AppContainer in AppContainers)
            {
                string SID = AppContainer.appContainerSid.ToString();
                if (AppPackages.ContainsKey(SID))
                    continue;

                UwpFunc.AppInfo AppInfo = new UwpFunc.AppInfo();
                AppInfo.ID = AppContainer.appContainerName;
                AppInfo.SID = SID;
                AppInfo.Name = AppContainer.displayName;

                AppPackages.Add(SID, AppInfo);
            }

            return true;
        }

        public Dictionary<string, UwpFunc.AppInfo> GetAllAppPkgs(bool bUpdate = false)
        {
            if (bUpdate || AppPackages.Count == 0 || (DateTime.Now - LastAppReload).TotalMilliseconds > 3000)
                LoadAppPkgs();
            return AppPackages;
        }

        public UwpFunc.AppInfo GetAppPkgBySid(string SID)
        {
            if (UwpFunc.IsWindows7OrLower || SID == null)
                return null;

            UwpFunc.AppInfo AppContainer = null;
            if (AppPackages.Count == 0 || (!AppPackages.TryGetValue(SID, out AppContainer) && (DateTime.Now - LastAppReload).TotalMilliseconds > 250))
            {
                LoadAppPkgs();
                AppPackages.TryGetValue(SID, out AppContainer);
            }
            return AppContainer;
        }

        ////////////////////////////////////////////////
        // Network List

        public FW_NETWORK[] GetNetworksInfo()
        {
            uint pdwNumNetworks;
            IntPtr ppNetworks;
            if (FWEnumNetworks(policyHandle, out pdwNumNetworks, out ppNetworks) != ERROR_SUCCESS || pdwNumNetworks > 0)
                return null;

            FW_NETWORK[] networks = new FW_NETWORK[pdwNumNetworks];
            try
            {
                long ptr = ppNetworks.ToInt64();
                for (int i = 0; i < pdwNumNetworks; i++)
                {
                    networks[i] = (FW_NETWORK)Marshal.PtrToStructure((IntPtr)ptr, typeof(FW_NETWORK));
                    ptr += Marshal.SizeOf(typeof(FW_NETWORK));
                }
            }
            catch {}

            FWFreeNetworks(ppNetworks);
            
            return networks;
        }

        public FW_ADAPTER[] GetAdaptersInfo()
        {
            uint pdwNumAdapters;
            IntPtr ppAdapters;
            if (FWEnumAdapters(policyHandle, out pdwNumAdapters, out ppAdapters) != ERROR_SUCCESS || pdwNumAdapters > 0)
                return null;

            FW_ADAPTER[] adapters = new FW_ADAPTER[pdwNumAdapters];
            try
            {
                long ptr = ppAdapters.ToInt64();
                for (int i = 0; i < pdwNumAdapters; i++)
                {
                    adapters[i] = (FW_ADAPTER)Marshal.PtrToStructure((IntPtr)ptr, typeof(FW_ADAPTER));
                    ptr += Marshal.SizeOf(typeof(FW_ADAPTER));
                }
            }
            catch { }

            FWFreeAdapters(ppAdapters);

            return adapters;
        }
    }
}
