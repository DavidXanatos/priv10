//#define FW_COM_ITF

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
#if FW_COM_ITF
    public class FirewallManager: WinNetFwCom
#else
    public class FirewallManager : WinNetFwAPI
#endif
    {
        private struct RuleStat
        {
            internal int AllowAll;
            internal int BlockAll;
            internal int AllowLan;
            internal int BlockInet;
        }

        public FirewallManager()
        {
            if (!File.Exists(App.dataPath + @"\FirewallRules_bakup.reg"))
            {
                string args = "export HKEY_LOCAL_MACHINE\\" + FirewallGuard.FirewallRulesKey + " \"" + App.dataPath + "\\FirewallRules_bakup.reg\"";
                MiscFunc.Exec("reg.exe", args, true);
            }
        }

        public void EvaluateRules(ProgramSet progSet, bool StrictTest = false)
        {
            String InetRanges = NetFunc.GetNonLocalNet();

            // todo: remove test code
            /*if (progSet.config.Name == "Microsoft Edge (microsoftedge.exe)")
            {
                progSet.config.Name = "bam!";
            }*/

            progSet.config.CurAccess = ProgramSet.Config.AccessLevels.Unconfigured;

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

                    if (rule.Profile != (int)FirewallRule.Profiles.All && (rule.Profile != ((int)FirewallRule.Profiles.Public | (int)FirewallRule.Profiles.Private | (int)FirewallRule.Profiles.Domain)))
                        continue;
                    if (rule.Interface != (int)FirewallRule.Interfaces.All)
                        continue;

                    if (FirewallRule.IsEmptyOrStar(rule.RemoteAddresses))
                    {
                        if (rule.Action == FirewallRule.Actions.Allow && InetProts)
                            Stat.AllowAll |= ((int)rule.Direction);
                        else if (rule.Action == FirewallRule.Actions.Block && AllProts)
                            Stat.BlockAll |= ((int)rule.Direction);
                    }
                    else if (rule.RemoteAddresses == InetRanges)
                    {
                        if (rule.Action == FirewallRule.Actions.Block && AllProts)
                            Stat.BlockInet |= ((int)rule.Direction);
                    }
                    else if (rule.RemoteAddresses == "LocalSubnet")
                    {
                        if (rule.Action == FirewallRule.Actions.Allow && InetProts)
                            Stat.AllowLan |= ((int)rule.Direction);
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

                MergedStat.AllowAll &= Stat.AllowAll;
                MergedStat.BlockAll &= Stat.BlockAll;
                MergedStat.AllowLan &= Stat.AllowLan;
                MergedStat.BlockInet &= Stat.BlockInet;
            }

            if ((MergedStat.BlockAll & (int)FirewallRule.Directions.Outboun) != 0 && (!StrictTest || (MergedStat.BlockAll & (int)FirewallRule.Directions.Inbound) != 0))
                progSet.config.CurAccess = ProgramSet.Config.AccessLevels.BlockAccess;
            else if ((MergedStat.AllowAll & (int)FirewallRule.Directions.Outboun) != 0 && (!StrictTest || (MergedStat.AllowAll & (int)FirewallRule.Directions.Inbound) != 0))
                progSet.config.CurAccess = ProgramSet.Config.AccessLevels.FullAccess;
            else if ((MergedStat.AllowLan & (int)FirewallRule.Directions.Outboun) != 0 && (!StrictTest || ((MergedStat.AllowLan & (int)FirewallRule.Directions.Inbound) != 0 && (MergedStat.AllowLan & (int)FirewallRule.Directions.Inbound) != 0)))
                progSet.config.CurAccess = ProgramSet.Config.AccessLevels.LocalOnly;
            else if (enabledCound > 0)
                progSet.config.CurAccess = ProgramSet.Config.AccessLevels.CustomConfig;
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
            EvaluateRules(progSet);

            if (progSet.config.NetAccess == ProgramSet.Config.AccessLevels.Unconfigured || progSet.config.NetAccess == ProgramSet.Config.AccessLevels.CustomConfig)
                return;

            if (progSet.config.NetAccess == progSet.config.CurAccess)
                return;

            foreach (Program prog in progSet.Programs.Values)
            {
                ClearRules(prog, progSet.config.NetAccess != ProgramSet.Config.AccessLevels.CustomConfig);

                for (int i = 1; i <= 2; i++)
                {
                    FirewallRule.Directions direction = (FirewallRule.Directions)i;

                    switch (progSet.config.NetAccess)
                    {
                    case ProgramSet.Config.AccessLevels.FullAccess:
                        {
                            // add and enable allow all rule
                            FirewallRule rule = new FirewallRule(prog.ID);
                            rule.Name = MakeRuleName(AllowAllName, expiration != 0, prog.Description);
                            rule.Grouping = RuleGroup;
                            rule.Action = FirewallRule.Actions.Allow;
                            rule.Direction = direction;
                            rule.Enabled = true;
                            ApplyRule(prog, rule, expiration);
                            break;
                        }
                    case ProgramSet.Config.AccessLevels.LocalOnly:
                        {
                            // create block rule only of we operate in blacklist mode
                            //if (GetFilteringMode() == FilteringModes.BlackList)
                            //{
                            //add and enable block rules for the internet
                            FirewallRule rule1 = new FirewallRule(prog.ID);
                            rule1.Name = MakeRuleName(BlockInet, expiration != 0, prog.Description);
                            rule1.Grouping = RuleGroup;
                            rule1.Action = FirewallRule.Actions.Block;
                            rule1.Direction = direction;
                            rule1.Enabled = true;
                            rule1.RemoteAddresses = NetFunc.GetNonLocalNet();
                            ApplyRule(prog, rule1, expiration);
                            //}

                            //add and enable allow rules for the lan
                            FirewallRule rule2 = new FirewallRule(prog.ID);
                            rule2.Name = MakeRuleName(AllowLan, expiration != 0, prog.Description);
                            rule2.Grouping = RuleGroup;
                            rule2.Action = FirewallRule.Actions.Allow;
                            rule2.Direction = direction;
                            rule2.Enabled = true;
                            //rule.RemoteAddresses = NetFunc.GetSpecialNet("LocalSubnet");
                            rule2.RemoteAddresses = "LocalSubnet";
                            ApplyRule(prog, rule2, expiration);
                            break;
                        }
                    case ProgramSet.Config.AccessLevels.BlockAccess:
                        {
                            // add and enable broad block rules
                            FirewallRule rule = new FirewallRule(prog.ID);
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

            App.engine.OnRulesChanged(progSet);
        }

        public bool ApplyRule(Program prog, FirewallRule rule, UInt64 expiration = 0)
        {
            if (!UpdateRule(rule)) // if the rule is new i.e. guid == null this call will set a new unique guid and add the rule to the global list
                return false;

            FirewallRuleEx ruleEx;
            if (!prog.Rules.TryGetValue(rule.guid, out ruleEx))
            {
                ruleEx = new FirewallRuleEx();
                ruleEx.guid = rule.guid;
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

        public FirewallRule.Actions LookupRuleAction(FirewallEvent FwEvent, int FwProfile)
        {
            // Note: FwProfile should have only one bit set, but just in case we can haldnel more than one, but not accurately
            int BlockRules = 0;
            int AllowRules = 0;

            for (int i = 0; i < FwProfiles.Length; i++)
            {
                if ((FwProfile & (int)FwProfiles[i]) == 0)
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
                    case FirewallRule.Directions.Outboun:
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
    }
}
