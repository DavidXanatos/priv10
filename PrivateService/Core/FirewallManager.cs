using Microsoft.Win32;
using MiscHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using PrivateService;
using PrivateAPI;
using WinFirewallAPI;

namespace PrivateWin10
{
    public class FirewallManager : FirewallInterface
    {
        bool IgnoreDomain;

        public FirewallManager()
        {
            IgnoreDomain = App.GetConfigInt("Firewall", "IgnoreDomainProfiles", 1) != 0;

            if (!File.Exists(App.dataPath + @"\FirewallRules_bakup.reg"))
            {
                string args = "export HKEY_LOCAL_MACHINE\\" + FirewallGuard.FirewallRulesKey + " \"" + App.dataPath + "\\FirewallRules_bakup.reg\"";
                MiscFunc.Exec("reg.exe", args, true);
            }
        }

        private struct RuleStat
        {
            internal struct Dir
            {
                internal bool Inbound;
                internal bool Outbound;

                internal void Add(FirewallRule.Directions dir)
                {
                    if ((dir & FirewallRule.Directions.Inbound) != 0)
                        Inbound = true;
                    if ((dir & FirewallRule.Directions.Outbound) != 0)
                        Outbound = true;
                }

                internal void Merge(Dir dir)
                {
                    Inbound &= dir.Inbound;
                    Outbound &= dir.Outbound;
                }
            }

            internal struct Net
            {
                internal Dir Domain; // 1
                internal Dir Private; // 2
                internal Dir Public; // 4
                
                internal void Add(int pro, FirewallRule.Directions dir)
                {
                    if ((pro & (int)FirewallRule.Profiles.Domain) != 0)
                        Domain.Add(dir);
                    if ((pro & (int)FirewallRule.Profiles.Private) != 0)
                        Private.Add(dir);
                    if ((pro & (int)FirewallRule.Profiles.Public) != 0)
                        Public.Add(dir);
                }

                internal void Merge(Net net)
                {
                    Domain.Merge(net.Domain);
                    Private.Merge(net.Private);
                    Public.Merge(net.Public);
                }

                internal bool IsOutbound(bool ignoreDomain)
                {
                    if (!ignoreDomain && !Domain.Outbound)
                        return false;
                    if (!Private.Outbound)
                        return false;
                    if (!Public.Outbound)
                        return false;
                    return true;
                }

                internal bool IsInbound(bool ignoreDomain)
                {
                    if (!ignoreDomain && !Domain.Inbound)
                        return false;
                    if (!Private.Inbound)
                        return false;
                    if (!Public.Inbound)
                        return false;
                    return true;
                }
            }

            internal Net AllowAll;
            internal Net BlockAll;
            internal Net AllowLan;
            internal Net BlockInet;
        }


        public void EvaluateRules(ProgramSet progSet, bool StrictTest = false)
        {
            String InetRanges = FirewallRule.AddrKeywordIntErnet;
            if (UwpFunc.IsWindows7OrLower)
                InetRanges = GetSpecialNet(InetRanges);

            progSet.config.CurAccess = ProgramConfig.AccessLevels.Unconfigured;

            SortedDictionary<ProgramID, RuleStat> RuleStats = new SortedDictionary<ProgramID, RuleStat>();
            int enabledCound = 0;

            foreach (Program prog in progSet.Programs.Values)
            {
                RuleStat Stat = new RuleStat();

                foreach (FirewallRule rule in prog.Rules.Values)
                {
                    if (!rule.Enabled)
                        continue;

                    enabledCound++;

                    if (!FirewallRule.IsEmptyOrStar(rule.LocalAddresses))
                        continue;
                    if (!FirewallRule.IsEmptyOrStar(rule.LocalPorts) || !FirewallRule.IsEmptyOrStar(rule.RemotePorts))
                        continue;
                    if (rule.IcmpTypesAndCodes != null && rule.IcmpTypesAndCodes.Length > 0)
                        continue;

                    bool AllProts = (rule.Protocol == (int)NetFunc.KnownProtocols.Any);
                    bool InetProts = AllProts || (rule.Protocol == (int)FirewallRule.KnownProtocols.TCP) || (rule.Protocol == (int)FirewallRule.KnownProtocols.UDP);

                    if (!InetProts)
                        continue;

                    //if (rule.Profile != (int)FirewallRule.Profiles.All && (rule.Profile != ((int)FirewallRule.Profiles.Public | (int)FirewallRule.Profiles.Private | (int)FirewallRule.Profiles.Domain)))
                    //    continue;
                    if (rule.Interface != (int)FirewallRule.Interfaces.All)
                        continue;

                    if (FirewallRule.IsEmptyOrStar(rule.RemoteAddresses))
                    {
                        if (rule.Action == FirewallRule.Actions.Allow && InetProts)
                            Stat.AllowAll.Add(rule.Profile, rule.Direction);
                        else if (rule.Action == FirewallRule.Actions.Block && AllProts)
                            Stat.BlockAll.Add(rule.Profile, rule.Direction);
                    }
                    else if (rule.RemoteAddresses == InetRanges)
                    {
                        if (rule.Action == FirewallRule.Actions.Block && AllProts)
                            Stat.BlockInet.Add(rule.Profile, rule.Direction);
                    }
                    else if (rule.RemoteAddresses == FirewallRule.AddrKeywordLocalSubnet)
                    {
                        if (rule.Action == FirewallRule.Actions.Allow && InetProts)
                            Stat.AllowLan.Add(rule.Profile, rule.Direction);
                    }
                }

                RuleStats.Add(prog.ID, Stat);
            }

            if (RuleStats.Count == 0 || enabledCound == 0)
                return;

            RuleStat MergedStat = RuleStats.Values.First();

            for (int i = 1; i < RuleStats.Count; i++)
            {
                RuleStat Stat = RuleStats.Values.ElementAt(i);

                MergedStat.AllowAll.Merge(Stat.AllowAll);
                MergedStat.BlockAll.Merge(Stat.BlockAll);
                MergedStat.AllowLan.Merge(Stat.AllowLan);
                MergedStat.BlockInet.Merge(Stat.BlockInet);
            }

            if (MergedStat.BlockAll.IsOutbound(IgnoreDomain) && (!StrictTest || MergedStat.BlockAll.IsInbound(IgnoreDomain)))
                progSet.config.CurAccess = ProgramConfig.AccessLevels.BlockAccess;
            //else if (MergedStat.AllowAll.IsOutbound(SkipDomain) && (!StrictTest || MergedStat.AllowAll.IsInbound(SkipDomain)))
            else if (MergedStat.AllowAll.IsOutbound(IgnoreDomain) && MergedStat.AllowAll.IsInbound(IgnoreDomain))
                progSet.config.CurAccess = ProgramConfig.AccessLevels.FullAccess;
            else if (MergedStat.AllowLan.IsOutbound(IgnoreDomain) && (!StrictTest || (MergedStat.AllowLan.IsInbound(IgnoreDomain) && MergedStat.AllowLan.IsInbound(IgnoreDomain))))
                progSet.config.CurAccess = ProgramConfig.AccessLevels.LocalOnly;
            else if (MergedStat.AllowAll.IsOutbound(IgnoreDomain))
                progSet.config.CurAccess = ProgramConfig.AccessLevels.OutBoundAccess;
            else if (MergedStat.AllowAll.IsInbound(IgnoreDomain))
                progSet.config.CurAccess = ProgramConfig.AccessLevels.InBoundAccess;
            else if (enabledCound > 0)
                progSet.config.CurAccess = ProgramConfig.AccessLevels.CustomConfig;
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

        public void ApplyRules(ProgramSet progSet, UInt64 expiration = 0)
        {
            EvaluateRules(progSet, true);

            if (progSet.config.NetAccess == ProgramConfig.AccessLevels.Unconfigured)
                return;

            if (progSet.config.NetAccess == progSet.config.CurAccess)
                return;

            foreach (Program prog in progSet.Programs.Values)
            {
                ClearRules(prog, progSet.config.NetAccess != ProgramConfig.AccessLevels.CustomConfig);

                if (progSet.config.NetAccess == ProgramConfig.AccessLevels.CustomConfig)
                    continue; // dont create any rules

                for (int i = 1; i <= 2; i++)
                {
                    FirewallRule.Directions direction = (FirewallRule.Directions)i;

                    if( (progSet.config.NetAccess == ProgramConfig.AccessLevels.InBoundAccess && direction != FirewallRule.Directions.Inbound)
                     || (progSet.config.NetAccess == ProgramConfig.AccessLevels.OutBoundAccess && direction != FirewallRule.Directions.Outbound) )
                        continue;

                    switch (progSet.config.NetAccess)
                    {
                    case ProgramConfig.AccessLevels.FullAccess:
                    case ProgramConfig.AccessLevels.InBoundAccess:
                    case ProgramConfig.AccessLevels.OutBoundAccess:
                        {
                            // add and enable allow all rule
                            FirewallRule rule = new FirewallRule();
                            FirewallRuleEx.SetProgID(rule, prog.ID);
                            rule.Name = MakeRuleName(AllowAllName, expiration != 0, prog.Description);
                            rule.Grouping = RuleGroup;
                            rule.Action = FirewallRule.Actions.Allow;
                            rule.Direction = direction;
                            rule.Enabled = true;
                            ApplyRule(prog, rule, expiration);
                            break;
                        }
                    case ProgramConfig.AccessLevels.LocalOnly:
                        {
                            // create block rule only of we operate in blacklist mode
                            //if (GetFilteringMode() == FilteringModes.BlackList)
                            //{
                            //add and enable block rules for the internet
                            FirewallRule rule1 = new FirewallRule();
                            FirewallRuleEx.SetProgID(rule1, prog.ID);
                            rule1.Name = MakeRuleName(BlockInet, expiration != 0, prog.Description);
                            rule1.Grouping = RuleGroup;
                            rule1.Action = FirewallRule.Actions.Block;
                            rule1.Direction = direction;
                            rule1.Enabled = true;
                            if (UwpFunc.IsWindows7OrLower)
                                rule1.RemoteAddresses = GetSpecialNet(FirewallRule.AddrKeywordIntErnet); 
                            else
                                rule1.RemoteAddresses = FirewallRule.AddrKeywordIntErnet;
                            ApplyRule(prog, rule1, expiration);
                            //}

                            //add and enable allow rules for the lan
                            FirewallRule rule2 = new FirewallRule();
                            FirewallRuleEx.SetProgID(rule2, prog.ID);
                            rule2.Name = MakeRuleName(AllowLan, expiration != 0, prog.Description);
                            rule2.Grouping = RuleGroup;
                            rule2.Action = FirewallRule.Actions.Allow;
                            rule2.Direction = direction;
                            rule2.Enabled = true;
                            //rule.RemoteAddresses = FirewallRule.GetSpecialNet(FirewallRule.AddrKeywordLocalSubnet);
                            rule2.RemoteAddresses = FirewallRule.AddrKeywordLocalSubnet;
                            ApplyRule(prog, rule2, expiration);
                            break;
                        }
                    case ProgramConfig.AccessLevels.BlockAccess:
                        {
                            // add and enable broad block rules
                            FirewallRule rule = new FirewallRule();
                            FirewallRuleEx.SetProgID(rule, prog.ID);
                            rule.Name = MakeRuleName(BlockAllName, expiration != 0, prog.Description);
                            rule.Grouping = RuleGroup;
                            rule.Action = FirewallRule.Actions.Block;
                            rule.Direction = direction;
                            rule.Enabled = true;
                            ApplyRule(prog, rule, expiration);
                            break;
                        }
                    }
                }
            }

            progSet.config.CurAccess = progSet.config.NetAccess;

            App.engine.OnRulesUpdated(progSet);
        }

        public bool ApplyRule(Program prog, FirewallRule rule, UInt64 expiration = 0)
        {
            if (!UpdateRule(rule)) // if the rule is new i.e. guid == null this call will set a new unique guid and add the rule to the global list
                return false;

            FirewallRuleEx ruleEx;
            if (!prog.Rules.TryGetValue(rule.guid, out ruleEx))
            {
                ruleEx = new FirewallRuleEx();
                ruleEx.ProgID = FirewallRuleEx.GetIdFromRule(rule);
                prog.Rules.Add(rule.guid, ruleEx);
            }
            ruleEx.Expiration = expiration;
            ruleEx.SetApplied();
            ruleEx.Assign(rule);
            return true;
        }

        public void ClearRules(Program prog, bool bDisable)
        {
            foreach (FirewallRuleEx rule in prog.Rules.Values.ToList())
            {
                if (rule.Name.IndexOf(RulePrefix) == 0) // Note: all internal rules start with priv10 - 
                {
                    if(RemoveRule(rule.guid))
                        prog.Rules.Remove(rule.guid);
                }
                // do not remove forign rules, onyl disable them if required
                else if (bDisable && rule.Enabled)
                {
                    rule.Enabled = false;
                    UpdateRule(rule);
                }
            }
        }


        public FirewallRule.Actions LookupRuleAction(FirewallEvent FwEvent, NetworkMonitor.AdapterInfo NicInfo)
        {
            // Note: FwProfile should have only one bit set, but just in case we can haldnel more than one, but not accurately
            int BlockRules = 0;
            int AllowRules = 0;

            for (int i = 0; i < FwProfiles.Length; i++)
            {
                if (((int)NicInfo.Profile & (int)FwProfiles[i]) == 0)
                    continue;

                switch (FwEvent.Direction)
                {
                    case FirewallRule.Directions.Inbound:
                        if (GetBlockAllInboundTraffic(FwProfiles[i]))
                            BlockRules++;
                        else
                            switch (GetDefaultInboundAction(FwProfiles[i]))
                            {
                                case FirewallRule.Actions.Allow: AllowRules++; break;
                                case FirewallRule.Actions.Block: BlockRules++; break;
                            }
                        break;
                    case FirewallRule.Directions.Outbound:
                        switch (GetDefaultOutboundAction(FwProfiles[i]))
                        {
                            case FirewallRule.Actions.Allow: AllowRules++; break;
                            case FirewallRule.Actions.Block: BlockRules++; break;
                        }
                        break;
                }
            }

            // Note: block rules take precedence
            if (BlockRules > 0)
                return FirewallRule.Actions.Block;
            if (AllowRules > 0)
                return FirewallRule.Actions.Allow;
            return FirewallRule.Actions.Undefined;
        }


        ////////////////////////////////////////////////////////
        // Rule Param Matching

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
                    if (UInt16.TryParse(strTemp[0], out beginPort) && UInt16.TryParse(strTemp[1], out endPort))
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
            {
                ; // todo:
            }
            else
                return null;
            return string.Join(",", IpRanges.ToArray());
        }

        ////////////////////////////////////////////////////////
        // Firewall Config

        public enum FilteringModes
        {
            Unknown = 0,
            NoFiltering = 1,
            BlackList = 2,
            WhiteList = 3,
            //BlockAll = 4,
        }

        public bool SetFilteringMode(FilteringModes Mode)
        {
            try
            {
                RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", false);
                bool DoNotAllowExceptions = subKey == null ? false : ((int)subKey.GetValue("DoNotAllowExceptions", 0) != 0);

                SetFirewallEnabled(Mode != FilteringModes.NoFiltering);
                switch (Mode)
                {
                    case FilteringModes.NoFiltering:
                        break;
                    case FilteringModes.BlackList:
                        SetBlockAllInboundTraffic(DoNotAllowExceptions);
                        SetDefaultOutboundAction(FirewallRule.Actions.Allow);
                        break;
                    case FilteringModes.WhiteList:
                        SetBlockAllInboundTraffic(DoNotAllowExceptions);
                        SetDefaultOutboundAction(FirewallRule.Actions.Block);
                        break;
                        //case FilteringModes.BlockAll:
                        //    BlockAllTrafic();
                        //    break;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            return true;
        }

        public FilteringModes GetFilteringMode()
        {
            try
            {
                if (TestFirewallEnabled(true))
                {
                    if (TestDefaultOutboundAction(FirewallRule.Actions.Allow))
                        return FilteringModes.BlackList;

                    if (TestDefaultOutboundAction(FirewallRule.Actions.Block))
                        return FilteringModes.WhiteList;
                }
                return FilteringModes.NoFiltering;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return FilteringModes.Unknown;
        }

        static public FirewallRule.Profiles[] FwProfiles = { FirewallRule.Profiles.Private, FirewallRule.Profiles.Public, FirewallRule.Profiles.Domain };

        protected void SetDefaultOutboundAction(FirewallRule.Actions Action)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                SetDefaultOutboundAction(FwProfiles[i], Action);
            }
        }

        protected bool TestDefaultOutboundAction(FirewallRule.Actions Action)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                if (GetDefaultOutboundAction(FwProfiles[i]) != Action)
                    return false;
            }
            return true;
        }

        protected void SetBlockAllInboundTraffic(bool Block)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                SetBlockAllInboundTraffic(FwProfiles[i], Block);
            }
        }

        protected bool TestBlockAllInboundTraffic(bool Block)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                if (GetBlockAllInboundTraffic(FwProfiles[i]) != Block)
                    return false;
            }
            return true;
        }

        protected void SetFirewallEnabled(bool Enable)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                SetFirewallEnabled(FwProfiles[i], Enable);
            }
        }

        protected bool TestFirewallEnabled(bool Enable)
        {
            for (int i = 0; i < FwProfiles.Length; i++)
            {
                if (GetFirewallEnabled(FwProfiles[i]) != Enable)
                    return false;
            }
            return true;
        }

        ////////////////////////////////////////////////
        // App Package List

        Dictionary<string, UwpFunc.AppInfo> AppPackages = new Dictionary<string, UwpFunc.AppInfo>();
        DateTime LastAppReload = DateTime.Now;
        bool HasPackageDetails = false;

        public bool LoadAppPkgs(bool bWithPackageDetails = false)
        {
            AppPackages.Clear();
            LastAppReload = DateTime.Now;

            if (UwpFunc.IsWindows7OrLower)
                return false;

            var watch = System.Diagnostics.Stopwatch.StartNew();

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

                if (bWithPackageDetails)
                {
                    var AppInfo2 = App.engine?.PkgMgr?.GetAppInfoByID(AppInfo.ID);
                    AppInfo.Logo = AppInfo2?.Logo;
                }

                AppPackages.Add(SID, AppInfo);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("LoadAppPkgs took: " + elapsedMs + "ms");

            return true;
        }

        public Dictionary<string, UwpFunc.AppInfo> GetAllAppPkgs(bool bWithPackageDetails, bool bUpdate = false)
        {
            if (bUpdate || AppPackages.Count == 0 || (DateTime.Now - LastAppReload).TotalMilliseconds > 30*1000 || (bWithPackageDetails && !HasPackageDetails))
                LoadAppPkgs(bWithPackageDetails);
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
    }
}
