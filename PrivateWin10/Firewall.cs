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
    public class Firewall
    {
        INetFwPolicy2 mFirewallPolicy;

        Dictionary<Guid, INetFwRule2> mAllRules = new Dictionary<Guid, INetFwRule2>();

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
            None = 0,
            Lan = 0x0001,
            Wireless = 0x0002,
            RemoteAccess = 0x0004,
            All = 0x7FFFFFFF
        }

        public class NotifyArgs : EventArgs
        {
            public Guid guid;
            public Program.LogEntry entry;
        }

        public Firewall()
        {
            // this exists since windows vista
            mFirewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        }

        public enum Auditing
        {
            Off = 0,
            Blocked = 1,
            Allowed = 2,
            All = 3
        }

        public Auditing GetAuditPol()
        {
            try
            {
                AuditPol.AUDIT_POLICY_INFORMATION pol = AuditPol.GetSystemPolicy("0CCE9226-69AE-11D9-BED3-505054503030");
                if ((pol.AuditingInformation & AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Success) != 0 && (pol.AuditingInformation & AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Failure) != 0)
                    return Auditing.All;
                if ((pol.AuditingInformation & AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Success) != 0)
                    return Auditing.Allowed;
                if ((pol.AuditingInformation & AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Failure) != 0)
                    return Auditing.Blocked;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return Auditing.Off;
        }

        public bool SetAuditPol(Auditing audit)
        {
            //MiscFunc.Exec("auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /failure:enable /success:enable");
            try
            {
                AuditPol.AUDIT_POLICY_INFORMATION pol = AuditPol.GetSystemPolicy("0CCE9226-69AE-11D9-BED3-505054503030");
                switch (audit)
                {
                    case Auditing.All: pol.AuditingInformation = AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Success | AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Failure; break;
                    case Auditing.Blocked: pol.AuditingInformation = AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Failure; break;
                    case Auditing.Allowed: pol.AuditingInformation = AuditPol.AUDIT_POLICY_INFORMATION_TYPE.Success; break;
                    case Auditing.Off: pol.AuditingInformation = AuditPol.AUDIT_POLICY_INFORMATION_TYPE.None; break;
                }
                TokenManipulator.AddPrivilege(TokenManipulator.SE_SECURITY_NAME);
                // Note: without SeSecurityPrivilege this fails silently
                AuditPol.SetSystemPolicy(pol);
                TokenManipulator.RemovePrivilege(TokenManipulator.SE_SECURITY_NAME);
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                return false;
            }
            return true;
        }

        EventLogWatcher mEventWatcher = null;

        private enum EventIDs
        {
            Blocked = 5157,
            Allowed = 5156
        }

        public bool WatchConnections(bool enable = true)
        {
            try
            {
                if (enable)
                {
                    mEventWatcher = new EventLogWatcher(new EventLogQuery("Security", PathType.LogName, "*[System[(Level=4 or Level=0) and (EventID=" + (int)EventIDs.Blocked + " or EventID=" + (int)EventIDs.Allowed + ")]] and *[EventData[Data[@Name='LayerRTID']>='48']]"));
                    mEventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnConnection);
                    mEventWatcher.Enabled = true;
                }
                else
                {
                    mEventWatcher.EventRecordWritten -= new EventHandler<EventRecordWrittenEventArgs>(OnConnection);
                    mEventWatcher.Dispose();
                    mEventWatcher = null;
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                return false;
            }
            return true;
        }

        private void OnConnection(object obj, EventRecordWrittenEventArgs arg)
        {
            if (arg.EventRecord == null)
                return;
            try
            {
                int processId = MiscFunc.parseInt(arg.EventRecord.Properties[0].Value.ToString());
                string path = arg.EventRecord.Properties[1].Value.ToString();

                Actions action = Actions.Undefined;
                if (arg.EventRecord.Id == (int)EventIDs.Blocked)
                    action = Actions.Block;
                else if (arg.EventRecord.Id == (int)EventIDs.Allowed)
                    action = Actions.Allow;

                string direction_str = arg.EventRecord.Properties[2].Value.ToString();
                Directions direction = Directions.Unknown;
                if (direction_str == "%%14592")
                    direction = Directions.Inbound;
                else if (direction_str == "%%14593")
                    direction = Directions.Outboun;
                string src_ip = arg.EventRecord.Properties[3].Value.ToString();
                int src_port = MiscFunc.parseInt(arg.EventRecord.Properties[4].Value.ToString());
                string dest_ip = arg.EventRecord.Properties[5].Value.ToString();
                int dest_port = MiscFunc.parseInt(arg.EventRecord.Properties[6].Value.ToString());
                int protocol = MiscFunc.parseInt(arg.EventRecord.Properties[7].Value.ToString());

                ProgramList.ID id = GetIDforEntry(path, processId);
                if (id == null)
                    return;

                Program.LogEntry entry = new Program.LogEntry(id, action, direction, src_ip, src_port, dest_ip, dest_port, protocol, processId, DateTime.Now);

                entry.Profile = GetCurrentProfiles();

                App.engine.LogActivity(entry);
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
        }

        private ProgramList.ID GetIDforEntry(string path, int processId)
        {
            ProgramList.Types type = ProgramList.Types.Global;
            string name = null;
            if (path.Equals("System", StringComparison.OrdinalIgnoreCase))
                type = ProgramList.Types.System;
            else
            {
                path = MiscFunc.parsePath(path);
                if (path.Length == 0) // fallback
                {
                    path = ProgramList.GetProcessPathById(processId);
                    if (path == null)
                        return null;
                }

                //if (Path.GetFileName(path).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase))
                List<ServiceHelper.ServiceInfo> Services = ServiceHelper.GetServicesByPID(processId);
                if (Services != null)
                {
                    type = ProgramList.Types.Service;
                    if (Services.Count > 1)
                    {
                        // ToDo: handle teh case Services.length > 1 !!!!
                        Console.WriteLine("Non unique service " + Services.Count);
                    }
                    name = Services[0].ServiceName;
                }
                else
                {
                    name = App.engine.appMgr != null ? App.engine.appMgr.GetAppPackage(path) : null;
                    if (name != null)
                        type = ProgramList.Types.App;
                    else
                        type = ProgramList.Types.Program;
                }
            }
            ProgramList.ID id = new ProgramList.ID(type, path, name);
            return id;
        }

        public void ClearRules(Program prog, bool bDisable)
        {
            foreach (FirewallRule rule in prog.Rules.Values.ToList())
            {
                if (rule.Name.IndexOf(FirewallRule.RulePrefix) == 0) // Note: all imternal rules start with priv10 - 
                {
                    RemoveRule(rule, true);
                }
                else if (bDisable)
                {
                    rule.Enabled = false;
                    UpdateRule(rule, true);
                }
            }
        }

        private struct RuleStat
        {
            internal int AllowAll;
            internal int BlockAll;
            internal int AllowLan;
            internal int BlockInet;
        }

        public static bool IsEmptyOrStar(string str)
        {
            return str == null || str == "" || str == "*";
        }

        public static bool MatchPort(int numPort, string strPorts)
        {
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

        public static bool MatchAddress(string strIP, string strRanges)
        {
            int type;
            BigInteger numIP = NetFunc.IpStrToInt(strIP, out type);

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
                        return MatchAddress(strIP, NetFunc.GetSpecialNet(strTemp[0].Trim()));
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
        public static bool MatchProfiles(int profile, int profiles)
        {
            return (profile & profiles) != 0;
        }

        public void EvaluateRules(Program prog, bool apply)
        {
            String InetRanges = NetFunc.GetNonLocalNet();

            prog.config.CurAccess = Program.Config.AccessLevels.Unconfigured;

            bool StrictTest = false;

            if (prog.Rules.Count > 0)
            {
                SortedDictionary<ProgramList.ID, RuleStat> RuleStats = new SortedDictionary<ProgramList.ID, RuleStat>();
                int enabledCound = 0;

                foreach (FirewallRule rule in prog.Rules.Values.ToList())
                {
                    RuleStat Stat;
                    if (!RuleStats.TryGetValue(rule.mID, out Stat))
                    {
                        Stat = new RuleStat();
                        RuleStats.Add(rule.mID, Stat);
                    }

                    if (!rule.Enabled)
                        continue;

                    enabledCound++;

                    if (!IsEmptyOrStar(rule.LocalAddresses))
                        continue;
                    if (!IsEmptyOrStar(rule.LocalPorts) || !IsEmptyOrStar(rule.RemotePorts))
                        continue;
                    if (!IsEmptyOrStar(rule.IcmpTypesAndCodes))
                        continue;

                    bool AllProts = (rule.Protocol == (int)NetFunc.KnownProtocols.Any);
                    bool InetProts = AllProts || (rule.Protocol == (int)FirewallRule.KnownProtocols.TCP) || (rule.Protocol == (int)FirewallRule.KnownProtocols.UDP);

                    if (!InetProts)
                        continue;

                    if (rule.Profile != (int)Profiles.All && (rule.Profile != ((int)Profiles.Public | (int)Profiles.Private | (int)Profiles.Domain)))
                        continue;
                    if (rule.Interface != (int)Interfaces.All)
                        continue;

                    if (IsEmptyOrStar(rule.RemoteAddresses))
                    {
                        if (rule.Action == Actions.Allow && InetProts)
                            Stat.AllowAll |= ((int)rule.Direction);
                        else if (rule.Action == Actions.Block && AllProts)
                            Stat.BlockAll |= ((int)rule.Direction);
                    }
                    else if (rule.RemoteAddresses == InetRanges)
                    {
                        if (rule.Action == Actions.Block && AllProts)
                            Stat.BlockInet |= ((int)rule.Direction);
                    }
                    else if (rule.RemoteAddresses == "LocalSubnet")
                    {
                        if (rule.Action == Actions.Allow && InetProts)
                            Stat.AllowLan |= ((int)rule.Direction);
                    }
                    RuleStats[rule.mID] = Stat;
                }

                RuleStat MergedStat = RuleStats.Values.ElementAt(0);

                for (int i = 1; i < RuleStats.Count; i++)
                {
                    RuleStat Stat = RuleStats.Values.ElementAt(i);

                    MergedStat.AllowAll &= Stat.AllowAll;
                    MergedStat.BlockAll &= Stat.BlockAll;
                    MergedStat.AllowLan &= Stat.AllowLan;
                    MergedStat.BlockInet &= Stat.BlockInet;
                }

                if ((MergedStat.BlockAll & (int)Directions.Outboun) != 0 && (!StrictTest || (MergedStat.BlockAll & (int)Directions.Inbound) != 0))
                    prog.config.CurAccess = Program.Config.AccessLevels.BlockAccess;
                else if ((MergedStat.AllowAll & (int)Directions.Outboun) != 0 && (!StrictTest || (MergedStat.AllowAll & (int)Directions.Inbound) != 0))
                    prog.config.CurAccess = Program.Config.AccessLevels.FullAccess;
                else if ((MergedStat.AllowLan & (int)Directions.Outboun) != 0 && (!StrictTest || ((MergedStat.AllowLan & (int)Directions.Inbound) != 0 && (MergedStat.AllowLan & (int)Directions.Inbound) != 0)))
                    prog.config.CurAccess = Program.Config.AccessLevels.LocalOnly;
                else if (enabledCound > 0)
                    prog.config.CurAccess = Program.Config.AccessLevels.CustomConfig;
            }


            if (!apply || prog.config.NetAccess == Program.Config.AccessLevels.Unconfigured || prog.config.NetAccess == Program.Config.AccessLevels.CustomConfig)
                return;

            if (prog.config.NetAccess == prog.config.CurAccess)
                return;

            ClearRules(prog, prog.config.NetAccess != Program.Config.AccessLevels.CustomConfig);

            foreach (ProgramList.ID id in prog.IDs)
            {
                for (int i = 1; i <= 2; i++)
                {
                    Directions direction = (Directions)i;

                    switch (prog.config.NetAccess)
                    {
                        case Program.Config.AccessLevels.FullAccess:

                            // add and enable allow all rule
                            UpdateRule(FirewallRule.MakeAllowRule(id, direction), true);
                            break;
                        case Program.Config.AccessLevels.LocalOnly:

                            // create block rule only of we operate in blacklist mode
                            //if (GetFilteringMode() == FilteringModes.BlackList)
                            //{
                            //add and enable block rules for the internet
                            UpdateRule(FirewallRule.MakeBlockInetRule(id, direction), true);
                            //}

                            //add and enable allow rules for the lan
                            UpdateRule(FirewallRule.MakeAllowLanRule(id, direction), true);
                            break;
                        case Program.Config.AccessLevels.BlockAccess:

                            // add and enable broad block rules
                            UpdateRule(FirewallRule.MakeBlockRule(id, direction), true);
                            break;
                    }
                }
            }

            prog.config.CurAccess = prog.config.NetAccess;

            App.engine.NotifyChange(prog);
        }

        public void LoadLogAsync()
        {
            AppLog.Line("Started loading log...");
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                LoadLog();
                AppLog.Line("Finished loading log asynchroniusly");
            }).Start();
        }

        public void LoadLog()
        {
            EventLog eventLog = new EventLog("Security");
            try
            {
                //for (int i = eventLog.Entries.Count-1; i > 0; i--)
                foreach (EventLogEntry logEntry in eventLog.Entries)
                {
                    //EventLogEntry entry = eventLog.Entries[i];
                    if (logEntry.InstanceId != (long)EventIDs.Allowed && logEntry.InstanceId != (long)EventIDs.Blocked)
                        continue;
                    string[] ReplacementStrings = logEntry.ReplacementStrings;

                    string direction_str = ReplacementStrings[2];
                    Directions direction = Directions.Unknown;
                    if (direction_str == "%%14592")
                        direction = Directions.Inbound;
                    else if (direction_str == "%%14593")
                        direction = Directions.Outboun;

                    int processId = MiscFunc.parseInt(ReplacementStrings[0]);
                    string path = ReplacementStrings[1];

                    ProgramList.ID id = GetIDforEntry(path, processId);
                    if (id == null)
                        return;

                    Actions action = Actions.Undefined;
                    if (logEntry.InstanceId == (int)EventIDs.Blocked)
                        action = Actions.Block;
                    else if (logEntry.InstanceId == (int)EventIDs.Allowed)
                        action = Actions.Allow;

                    string src_ip = ReplacementStrings[3];
                    int src_port = MiscFunc.parseInt(ReplacementStrings[4]);
                    string dest_ip = ReplacementStrings[5];
                    int dest_port = MiscFunc.parseInt(ReplacementStrings[6]);
                    int protocol = MiscFunc.parseInt(ReplacementStrings[7]);

                    Program.LogEntry entry = new Program.LogEntry(id, action, direction, src_ip, src_port, dest_ip, dest_port, protocol, processId, logEntry.TimeGenerated);

                    App.engine.LogActivity(entry, true);
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            eventLog.Dispose();
        }

        public bool ClearLog(bool ClearSecLog)
        {
            foreach (Program process in App.engine.programs.Progs.Values)
                process.Log.Clear();

            if (ClearSecLog)
            {
                EventLog eventLog = new EventLog("Security");
                eventLog.Clear();
                eventLog.Dispose();
            }

            return true;
        }

        public bool LoadRules(bool CleanUp = false)
        {
            if (App.engine.appMgr != null)
                App.engine.appMgr.LoadApps();

            foreach (Program prog in App.engine.programs.Progs.Values)
                prog.Rules.Clear();
            mAllRules.Clear();

            List<INetFwRule2> entries = new List<INetFwRule2>();
            try
            {
                foreach (INetFwRule2 entry in mFirewallPolicy.Rules)
                    entries.Add(entry);
            }
            catch // firewall service is deisabled :/
            {
                return false;
            }

            foreach (INetFwRule2 entry in entries)
            {
                if (CleanUp && entry.Name.Contains(FirewallRule.TempRulePrefix))
                {
                    AppLog.Line("Cleaning Up temporary rule: {0}", entry.Name);
                    RemoveRule(entry);
                    continue;
                }

                FirewallRule rule = new FirewallRule();
                if (FirewallRule.LoadRule(rule, entry))
                {
                    mAllRules.Add(rule.guid, entry);

                    Program process = App.engine.programs.GetProgram(rule.mID, true);
                    process.Rules.Add(rule.guid, rule);
                }
            }

            foreach (Program prog in App.engine.programs.Progs.Values)
                EvaluateRules(prog, false);

            App.engine.NotifyChange(null);

            return true;
        }

        public bool UpdateRule(FirewallRule rule, bool silent = false)
        {
            bool bAdd = (rule.guid == Guid.Empty);
            try
            {
                INetFwRule2 entry;
                if (bAdd)
                    entry = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                else if (!mAllRules.TryGetValue(rule.guid, out entry))
                {
                    AppLog.Line("Failed Updating rule: ruls is not longer present");
                    return false;
                }

                if (!FirewallRule.SaveRule(rule, entry))
                    return false;

                Program prog = App.engine.programs.GetProgram(rule.mID, true);
                if (bAdd)
                {
                    mFirewallPolicy.Rules.Add(entry);

                    rule.guid = Guid.NewGuid();
                    mAllRules.Add(rule.guid, entry);
                }
                else
                    prog.Rules.Remove(rule.guid);
                prog.Rules.Add(rule.guid, rule);

                if (!silent)
                    App.engine.NotifyChange(prog);
            }
            catch (Exception err)
            {
                AppLog.Line("Failed to Write rule: " + err.Message);
                return false;
            }
            return true;
        }

        public bool RemoveRule(INetFwRule2 entry)
        {
            try
            {
                // Note: if this is not set to null renam may fail as well as other sets :/
                entry.EdgeTraversalOptions = (int)NET_FW_EDGE_TRAVERSAL_TYPE_.NET_FW_EDGE_TRAVERSAL_TYPE_DENY;

                // Note: the removal is done byname, howeever multiple rules may have the same name, WTF, so we set a temporary unique name
                entry.Name = "***_to_be_deleted_***";

                mFirewallPolicy.Rules.Remove(entry.Name);
            }
            catch (Exception err)
            {
                AppLog.Line("Failed to Remove rule: " + err.Message);
                return false;
            }
            return true;
        }

        public bool RemoveRule(FirewallRule rule, bool silent = false)
        {
            INetFwRule2 entry;
            if (!mAllRules.TryGetValue(rule.guid, out entry))
                return true; // tne rule is already gone

            if (!RemoveRule(entry))
                return false;

            var prog = App.engine.programs.GetProgram(rule.mID);
            if (prog != null)
            {
                prog.Rules.Remove(rule.guid);

                if (!silent)
                    App.engine.NotifyChange(prog);
            }

            return true;
        }

        public bool BlockInternet(bool bBlock)
        {
            bool ret = true;
            Program prog = App.engine.programs.GetProgram(new ProgramList.ID(ProgramList.Types.Global), true);
            if (bBlock)
            {
                ret &= UpdateRule(FirewallRule.MakeBlockRule(prog.GetMainID(), Directions.Inbound), true);
                ret &= UpdateRule(FirewallRule.MakeBlockRule(prog.GetMainID(), Directions.Outboun), true);
            }
            else
            {
                ClearRules(prog, false);
            }
            return ret;
        }

        public int CleanUpRules(bool bAll = false)
        {
            long curTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            int Count = 0;
            foreach (Program prog in App.engine.programs.Progs.Values)
            {
                foreach (FirewallRule rule in prog.Rules.Values.ToList())
                {
                    if (rule.Expiration != 0 && (bAll || curTime > rule.Expiration))
                    {
                        if (RemoveRule(rule))
                            Count++;
                    }
                }
            }
            return Count;
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
                        SetDefaultOutboundAction(NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                        break;
                    case FilteringModes.WhiteList:
                        SetBlockAllInboundTraffic(DoNotAllowExceptions);
                        SetDefaultOutboundAction(NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
                        break;
                    //case FilteringModes.BlockAll:
                    //    BlockAllTrafic();
                    //    break;
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Settign FilteringMode failed: " + err.Message);
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
                    if (TestDefaultOutboundAction(NET_FW_ACTION_.NET_FW_ACTION_ALLOW))
                        return FilteringModes.BlackList;

                    if (TestDefaultOutboundAction(NET_FW_ACTION_.NET_FW_ACTION_BLOCK))
                        return FilteringModes.WhiteList;
                }
                return FilteringModes.NoFiltering;
            }
            catch (Exception err)
            {
                AppLog.Line("Getting FilteringMode failed: " + err.Message);
            }
            return FilteringModes.Unknown;
        }

        private void SetDefaultOutboundAction(NET_FW_ACTION_ Action)
        {
            mFirewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, Action);
            mFirewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, Action);
            mFirewallPolicy.set_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, Action);
        }

        private bool TestDefaultOutboundAction(NET_FW_ACTION_ Action)
        {
            if (mFirewallPolicy.get_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != Action)
                return false;
            if (mFirewallPolicy.get_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != Action)
                return false;
            if (mFirewallPolicy.get_DefaultOutboundAction(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != Action)
                return false;
            return true;
        }

        private void SetBlockAllInboundTraffic(bool Block)
        {
            mFirewallPolicy.set_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, Block);
            mFirewallPolicy.set_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, Block);
            mFirewallPolicy.set_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, Block);
        }

        private bool TestBlockAllInboundTraffic(bool Block)
        {
            if (mFirewallPolicy.get_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != Block)
                return false;
            if (mFirewallPolicy.get_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != Block)
                return false;
            if (mFirewallPolicy.get_BlockAllInboundTraffic(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != Block)
                return false;
            return true;
        }

        private void SetFirewallEnabled(bool Enable)
        {
            mFirewallPolicy.set_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, Enable);
            mFirewallPolicy.set_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, Enable);
            mFirewallPolicy.set_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, Enable);
        }

        private bool TestFirewallEnabled(bool Enable)
        {
            if (mFirewallPolicy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != Enable)
                return false;
            if (mFirewallPolicy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != Enable)
                return false;
            if (mFirewallPolicy.get_FirewallEnabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != Enable)
                return false;
            return true;
        }
        
        public int GetCurrentProfiles()
        {
            return mFirewallPolicy.CurrentProfileTypes;
        }
    }
}
