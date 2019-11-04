using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PrivateWin10
{
    public class Engine//: IDisposable
    {
        public ProgramList ProgramList;

        public FirewallManager FirewallManager;
        public FirewallMonitor FirewallMonitor;
        public FirewallGuard FirewallGuard;
        public NetworkMonitor NetworkMonitor;
        public DnsInspector DnsInspector;

        Dispatcher mDispatcher;
        ManualResetEvent mStarted = new ManualResetEvent(false);
        ManualResetEvent mFinished = new ManualResetEvent(false);
        DispatcherTimer mTimer;

        public delegate void FX();


        public void RunInEngineThread(FX fx)
        {
            if (mDispatcher == null)
                return;

            mDispatcher.BeginInvoke(new Action(() =>
            {
                fx();
            }));
        }

        /*public Engine()
        {
        }

        public void Dispose()
        {
        }*/

        public void Start()
        {
            if (mDispatcher != null)
                return;

            Thread thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;
            thread.Name = "Engine";
            thread.SetApartmentState(ApartmentState.STA); // needed for tweaks
            thread.Start();

            mStarted.WaitOne();

            AppLog.Debug("Private Win10 Engine started.");
        }

        public void Run()
        {
            AppLog.Debug("Engine Thread Running");

            mDispatcher = Dispatcher.CurrentDispatcher;

            // Init
            AppLog.Debug("Loading program list...");
            ProgramList = new ProgramList();
            ProgramList.LoadList();

            AppLog.Debug("Starting firewall monitor...");
            FirewallMonitor = new FirewallMonitor();
            FirewallMonitor.StartEventWatcher();

            string AuditPolicyStr = App.GetConfig("Firewall", "AuditPolicy", "");
            FirewallMonitor.Auditing AuditPolicy;
            if (Enum.TryParse(AuditPolicyStr, out AuditPolicy))
            {
                if ((FirewallMonitor.GetAuditPolicy() & AuditPolicy) != AuditPolicy)
                {
                    AppLog.Debug("Re-Enabling Firewall Event auditing policy ...");
                    FirewallMonitor.SetAuditPolicy(AuditPolicy);
                }
            }

            FirewallMonitor.FirewallEvent += (object sender, FirewallEvent FwEvent) =>
            {
                RunInEngineThread(() => {
                    OnFirewallEvent(FwEvent);
                });
            };

            AppLog.Debug("Starting firewall guard...");
            FirewallGuard = new FirewallGuard();
            FirewallGuard.StartEventWatcher();

            if (App.GetConfigInt("Firewall", "RuleGuard", 0) != 0)
            {
                if (!FirewallGuard.HasAuditPolicy())
                {
                    AppLog.Debug("Re-Enabling Firewall Rule auditing policy ...");
                    FirewallGuard.SetAuditPolicy(true);
                }
            }

            FirewallGuard.ChangeEvent += (object sender, PrivateWin10.RuleChangedEvent FwEvent) =>
            {
                RunInEngineThread(() => {
                    OnRuleChangedEvent(FwEvent);
                });
            };

            if (App.GetConfigInt("Startup", "LoadLog", 0) != 0)
                LoadLogAsync();

            FirewallManager = new FirewallManager();
            LoadFwRules();
            CleanupFwRules();

            AppLog.Debug("Starting network manger...");
            NetworkMonitor = new NetworkMonitor();
            DnsInspector = new DnsInspector();

            DnsInspector.DnsQueryEvent += (object sender, DnsInspector.DnsEvent DnsEvent) =>
            {
                OnDnsEvent(DnsEvent);
            };
            //

            AppLog.Debug("Setting up IPC host...");
            App.host = new Priv10Host(App.mSvcName);
            App.host.Listen();

            mStarted.Set();

            ProgramList.Changed += OnChanged;

            AppLog.Debug("Starting engine timer...");

            // todo
            // test
            /*try
            {
                throw new Exception("blabla");
            }
            catch (Exception ex)
            {
                AppLog.Exception(ex);
            }*/
            //

            mTimer = new DispatcherTimer();
            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 250); // 4x a second
            mTimer.Start();

            // queue a refresh push
            RunInEngineThread(() => {
                App.host.NotifyChange(new ProgramList.ListEvent());
            });

            Dispatcher.Run(); // run

            mTimer.Stop();

            // UnInit
            AppLog.Debug("Saving program list...");
            ProgramList.StoreList();

            FirewallMonitor.StopEventWatcher();

            NetworkMonitor.Dispose();
            DnsInspector.Dispose();
            //

            AppLog.Debug("Shuttin down IPC host...");
            App.host.Close();

            mFinished.Set();

            AppLog.Debug("Engine Thread Terminating");
        }

        int mTickCount = 0;

        protected void OnTimerTick(object sender, EventArgs e)
        {
            if ((mTickCount++ % 4) == 0)
                return;

            if ((mTickCount % (4 * 60)) == 0)
                NetworkMonitor.UpdateNetworks();

            NetworkMonitor.UpdateSockets();

            ProcessFirewallEvents();

            ProcessRuleChanges();

            ProgramList.UpdatePrograms(); // data rates and so on

            if ((mTickCount % (4 * 60)) == 0)
                CleanupFwRules();

            if ((mTickCount % (4 * 60 * 30)) == 0)
                DnsInspector.CleanupCache();
        }

        public void Stop()
        {
            if (mDispatcher == null)
                return;

            mDispatcher.InvokeShutdown();
            mDispatcher.Thread.Join(); // Note: this waits for thread finish
            mDispatcher = null;

            mFinished.WaitOne();
        }

        public ProgramID GetProgIDbyPID(int PID, string serviceTag, string fileName = null)
        {
            if (PID == ProcFunc.SystemPID)
                return ProgramID.NewID(ProgramID.Types.System);

            if (fileName == null || fileName.Length == 0)
            {
                fileName = ProcFunc.GetProcessName(PID);
                if (fileName == null)
                    return null;
            }

            if (serviceTag != null)
                return ProgramID.NewSvcID(serviceTag, fileName);

            if (App.PkgMgr != null)
            {
                string SID = App.PkgMgr.GetAppPackageSidByPID(PID);
                if (SID != null)
                    return ProgramID.NewAppID(SID, fileName);
            }

            return ProgramID.NewProgID(fileName);
        }

        struct QueuedFwEvent
        {
            public FirewallEvent FwEvent;
            public int FwProfile;
            public List<ServiceHelper.ServiceInfo> Services;
        }

        List<QueuedFwEvent> QueuedFwEvents = new List<QueuedFwEvent>();

        protected void OnFirewallEvent(FirewallEvent FwEvent)
        {
            int FwProfile = NetworkMonitor.GetFirewallProfileByIP(FwEvent.LocalAddress);

            OnFirewallEvent(FwEvent, FwProfile);
        }

        protected void OnFirewallEvent(FirewallEvent FwEvent, int FwProfile)
        {
            ProgramID ProgID;
            if (FwEvent.ProcessFileName.Equals("System", StringComparison.OrdinalIgnoreCase))
                ProgID = ProgramID.NewID(ProgramID.Types.System);
            else
            {
                List<ServiceHelper.ServiceInfo> Services = ServiceHelper.GetServicesByPID(FwEvent.ProcessId);
                if (Services == null || Services.Count == 1)
                {
                    ProgID = GetProgIDbyPID(FwEvent.ProcessId, Services == null ? null : Services[0].ServiceName, FwEvent.ProcessFileName);
                    if (ProgID == null)
                        return; // the process already terminated and we did not have it's file name, just ignore this event
                }
                else //if(Services.Count > 1)
                {
                    // we don't have a unique service match, the process is hosting multiple services :/

                    QueuedFwEvents.Add(new QueuedFwEvent() { FwEvent = FwEvent, FwProfile = FwProfile, Services = Services });
                    return;
                }
            }

            OnFirewallEvent(FwEvent, FwProfile, ProgID);
        }

        protected void OnFirewallEvent(FirewallEvent FwEvent, int FwProfile, ProgramID progID)
        {
            Program prog = ProgramList.GetProgram(progID, true, ProgramList.FuzzyModes.Any);

            Program.LogEntry entry = new Program.LogEntry(FwEvent, progID);
            if (FwProfile == -1)
                entry.State = Program.LogEntry.States.FromLog;
            else
            {
                FirewallRule.Actions RuleAction = prog.LookupRuleAction(FwEvent, FwProfile);
                entry.CheckAction(RuleAction);
            }
            prog.AddLogEntry(entry);

            PushLogEntry(entry, prog);
        }

        protected void PushLogEntry(Program.LogEntry entry, Program prog, List<String> services = null)
        {
            bool Delayed = false;
            //DnsInspector.ResolveHost(entry.FwEvent.ProcessId, entry.FwEvent.RemoteAddress, entry, Program.LogEntry.HostSetter);
            DnsInspector.GetHostName(entry.FwEvent.ProcessId, entry.FwEvent.RemoteAddress, entry, (object obj, string name, DnsInspector.NameSources source) => {

                var old_source = (obj as Program.LogEntry).RemoteHostNameSource;
                Program.LogEntry.HostSetter(obj, name, source);

                // if the resolution was delayed, re emit this event, its unique gui wil prevent it form being logged twice
                if (Delayed && source > old_source) // only update if we got a better host name
                    App.host.NotifyActivity(prog.ProgSet.guid, entry, prog.ID, services, true);
            });
            Delayed = true;

            // Note: services is to be specifyed only if multiple services are hosted by the process and a unique resolution was not possible 
            App.host.NotifyActivity(prog.ProgSet.guid, entry, prog.ID, services);
        }

        protected void ProcessFirewallEvents()
        {
            foreach (QueuedFwEvent cur in QueuedFwEvents)
            {
                // this function is called just after updating the socket list, so for allowed connections we can just check the sockets to identify the service
                if (cur.FwEvent.Action == FirewallRule.Actions.Allow)
                {
                    UInt32 ProtocolType = cur.FwEvent.Protocol;
                    if (cur.FwEvent.LocalAddress.GetAddressBytes().Length == 4)
                        ProtocolType |= (UInt32)IPHelper.AF_INET.IP4 << 8;
                    else
                        ProtocolType |= (UInt32)IPHelper.AF_INET.IP6 << 8;

                    NetworkSocket Socket = NetworkMonitor.FindSocket(cur.FwEvent.ProcessId, ProtocolType, cur.FwEvent.LocalAddress, cur.FwEvent.LocalPort, cur.FwEvent.RemoteAddress, cur.FwEvent.RemotePort, NetworkSocket.MatchMode.Strict);
                    if (Socket != null && Socket.ProgID != null)
                    {
                        OnFirewallEvent(cur.FwEvent, cur.FwProfile, Socket.ProgID);
                        return;
                    }
                }

                // try to find a proramm with a matching rule
                List<ProgramID> machingIDs = new List<ProgramID>();
                List<ProgramID> unruledIDs = new List<ProgramID>();
                List<String> services = new List<String>();
                foreach (ServiceHelper.ServiceInfo info in cur.Services)
                {
                    services.Add(info.ServiceName);

                    ProgramID progID = GetProgIDbyPID(cur.FwEvent.ProcessId, info.ServiceName, cur.FwEvent.ProcessFileName);
                    Program prog = ProgramList.GetProgram(progID, false, ProgramList.FuzzyModes.Tag); // fuzzy match i.e. service tag match is enough

                    FirewallRule.Actions RuleAction = prog == null ? FirewallRule.Actions.Undefined : prog.LookupRuleAction(cur.FwEvent, cur.FwProfile);

                    // check if the program has any matchign rules
                    if (RuleAction == cur.FwEvent.Action)
                        machingIDs.Add(progID);
                    // if no program was found or it does not have matchign rules
                    else if (RuleAction == FirewallRule.Actions.Undefined)
                        unruledIDs.Add(progID);
                }

                // did we found one matching service?
                if (machingIDs.Count == 1)
                    OnFirewallEvent(cur.FwEvent, cur.FwProfile, machingIDs[0]);

                // if we have found no services with matching rules, but one service without any roules
                else if (machingIDs.Count == 0 && unruledIDs.Count == 1)
                    OnFirewallEvent(cur.FwEvent, cur.FwProfile, unruledIDs[0]);

                // well damn it we dont couldn't find out which service this event belongs to
                else
                {
                    // if there is at least one matchign rule, don't show a connection notification
                    bool bHasMatches = machingIDs.Count > 0;

                    // get the default action for if there is no rule
                    FirewallRule.Actions DefaultAction = FirewallManager.LookupRuleAction(cur.FwEvent, cur.FwProfile);

                    // if the action taken matches the default action, than unruled results are equivalent to the matches once
                    if (DefaultAction == cur.FwEvent.Action)
                        machingIDs.AddRange(unruledIDs);
                    // if the action taken does match the default action, unruled entries must be wrong
                    unruledIDs.Clear();

                    if (bHasMatches)
                    {
                        // emit an event for every possible match
                        foreach (var progID in machingIDs)
                            OnFirewallEvent(cur.FwEvent, cur.FwProfile, progID);
                    }
                    else
                    {
                        // log entry for firewall notification

                        ProgramID progID = GetProgIDbyPID(cur.FwEvent.ProcessId, null, cur.FwEvent.ProcessFileName);

                        Program prog = ProgramList.GetProgram(progID, true, ProgramList.FuzzyModes.No);

                        Program.LogEntry entry = new Program.LogEntry(cur.FwEvent, progID);
                        entry.State = Program.LogEntry.States.UnRuled;
                        prog.AddLogEntry(entry);

                        PushLogEntry(entry, prog, services);
                    }
                }
            }
            QueuedFwEvents.Clear();
        }

        public void LoadLogAsync()
        {
            AppLog.Debug("Started loading firewall log...");
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                List<FirewallEvent> log = FirewallMonitor.LoadLog();
                foreach (var entry in log)
                {
                    var StartTime = ProcFunc.GetProcessCreationTime(entry.ProcessId);
                    if (StartTime == 0)
                        continue;

                    var FileName = entry.ProcessId == ProcFunc.SystemPID ? "System" : ProcFunc.GetProcessName(entry.ProcessId);
                    if (FileName == null || !entry.ProcessFileName.Equals(FileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    OnFirewallEvent(entry, -1);
                }

                AppLog.Debug("Finished loading log asynchroniusly");
            }).Start();
        }

        public void OnChanged(object sender, ProgramList.ListEvent args) // todo: should be private
        {
            if (App.host == null) // early start stage
                return;

            App.host.NotifyChange(args);
        }

#if FW_COM_ITF
        protected void OnRuleChangedEvent(PrivateWin10.RuleChangedEvent FwEvent)
        {
            // todo
        }

        protected void ProcessRuleChanges()
        {
            // todo
        }
#else
        protected class RuleChangedEvent
        {
            public string RuleId;
            public string RuleName;
        }

        Dictionary<string, RuleChangedEvent> RuleChangedEvents = new Dictionary<string, RuleChangedEvent>();

        protected void OnRuleChangedEvent(PrivateWin10.RuleChangedEvent FwEvent)
        {
            //string[] ruleIds = { FwEvent.RuleId };
            //var rule = FirewallManager.LoadRules(ruleIds);

            RuleChangedEvent changeEvent;
            if (!RuleChangedEvents.TryGetValue(FwEvent.RuleId, out changeEvent))
            {
                changeEvent = new RuleChangedEvent();
                changeEvent.RuleId = FwEvent.RuleId;
                changeEvent.RuleName = FwEvent.RuleName; // for debug only
                RuleChangedEvents.Add(FwEvent.RuleId, changeEvent);
            }

            //AppLog.Debug("Rule {2}: {0} ({1})", FwEvent.RuleId, FwEvent.RuleName, FwEvent.EventID == FirewallGuard.EventIDs.Added ? "Added" 
            //                                                   : (FwEvent.EventID == FirewallGuard.EventIDs.Removed ? "Removed" : "Changed"));
        }

        protected void ProcessRuleChanges()
        {
            if (RuleChangedEvents.Count == 0)
                return;

            // cache old rules
            Dictionary<string, FirewallRule> oldRules = FirewallManager.GetRules(RuleChangedEvents.Keys.ToArray());

            // update all rules that may have been changed
            Dictionary<string, FirewallRule> updatedRules = FirewallManager.LoadRules(RuleChangedEvents.Keys.ToArray()); 

            foreach (RuleChangedEvent changeEvent in RuleChangedEvents.Values)
            {
                FirewallRule oldRule = null;
                Program prog = null;
                FirewallRuleEx knownRule = null; // known rule from the program
                if (oldRules.TryGetValue(changeEvent.RuleId, out oldRule))
                {
                    prog = ProgramList.GetProgram(oldRule.ProgID);
                    if (prog != null)
                        prog.Rules.TryGetValue(changeEvent.RuleId, out knownRule);

                    if(knownRule == null)
                        App.LogCriticalError("rule lists are inconsistent");
                }

                FirewallRule rule = null;
                updatedRules.TryGetValue(changeEvent.RuleId, out rule);


                if (knownRule == null && rule == null)
                {
                    // we have just removed this rule
                    // or the rule was added and han right away removed
                }
                else if (knownRule == null && rule != null)
                    OnRuleAdded(rule);
                else if (knownRule != null && rule == null)
                    OnRuleRemoved(knownRule, prog);
                else if(oldRule.Match(rule) != FirewallRule.MatchResult.Identical)
                    OnRuleUpdated(rule, knownRule, prog);
                // else // rules match i.e. we ended up here as a result of an update we issued
            }

            RuleChangedEvents.Clear();
        }
#endif

        public bool LoadFwRules()
        {
            AppLog.Debug("Loading firewall rules...");
            List<FirewallRule> rules = FirewallManager.LoadRules();
            if (rules == null)
                return false; // failed to load rules

#if FW_COM_ITF
            // todo
#else
            if (FirewallGuard.HasAuditPolicy())
            {
                Dictionary<string, Tuple<FirewallRuleEx, Program>> OldRules = new Dictionary<string, Tuple<FirewallRuleEx, Program>>();

                foreach (Program prog in ProgramList.Programs.Values)
                {
                    foreach (FirewallRuleEx rule in prog.Rules.Values)
                        OldRules.Add(rule.guid, Tuple.Create(rule, prog));
                }

                bool bApproveAll = OldRules.Count == 0;
                if (bApproveAll)
                    App.LogInfo(Translate.fmt("msg_rules_approved"));

                foreach (FirewallRule rule in rules)
                {
                    Tuple<FirewallRuleEx, Program> value;
                    if (!OldRules.TryGetValue(rule.guid, out value))
                        OnRuleAdded(rule, bApproveAll);
                    else
                    {
                        OldRules.Remove(rule.guid);

                        // Note: We don't save the ProgID to disk for each rule, instead we take the ProgID from the program just loaded.
                        // The prgram ProgID never has RawPath set as depanding how the program was seen first it would or wouldn't have a RawPath.
                        // Hence we try here to recover the RawPath information from the just loaded rule lost.
                        if (rule.ProgID.RawPath != null && value.Item1.ProgID.RawPath == null)
                            value.Item1.ProgID = rule.ProgID;

                        // update the rule index which is used only for ui sorting
                        value.Item1.Index = rule.Index; 

                        if (value.Item1.State == FirewallRuleEx.States.Changed && rule.Enabled == false)
                            continue; // to not re issue events on rule enumeration

                        // This tests if the rule actually change and it it did not it does not do anything
                        OnRuleUpdated(rule, value.Item1, value.Item2);
                    }
                }

                foreach (Tuple<FirewallRuleEx, Program> value in OldRules.Values)
                {
                    if (value.Item1.State == FirewallRuleEx.States.Deleted)
                        continue; // to not re issue events on rule enumeration

                    OnRuleRemoved(value.Item1, value.Item2);
                }
            }
            else
#endif
            {
                // clear all old rules
                foreach (Program prog in ProgramList.Programs.Values)
                    prog.Rules.Clear();

                // assign new rules
                foreach (FirewallRule rule in rules)
                {
                    Program prog = ProgramList.GetProgram(rule.ProgID, true);
                    FirewallRuleEx ruleEx = new FirewallRuleEx();
                    ruleEx.guid = rule.guid;
                    ruleEx.Assign(rule);

                    if (rule.Name.IndexOf(FirewallManager.TempRulePrefix) == 0) // Note: all temporary rules start with priv10temp - 
                        ruleEx.Expiration = MiscFunc.GetUTCTime(); // expire now

                    prog.Rules.Add(rule.guid, ruleEx);
                }
            }

            foreach (ProgramSet progSet in ProgramList.ProgramSets.Values)
                App.engine.FirewallManager.EvaluateRules(progSet);

            return true;
        }

        protected void OnRuleAdded(FirewallRule rule, bool bApproved = false)
        {
            Program prog = ProgramList.GetProgram(rule.ProgID, true);
            if (prog.Rules.ContainsKey(rule.guid))
            {
                App.LogCriticalError("rule lists are inconsistent 2");
                return;
            }

            FirewallRuleEx knownRule = new FirewallRuleEx();
            knownRule.guid = rule.guid;
            knownRule.Assign(rule);

            if (rule.Name.IndexOf(FirewallManager.TempRulePrefix) == 0) // Note: all temporary rules start with priv10temp - 
                knownRule.Expiration = MiscFunc.GetUTCTime(); // expire now

            prog.Rules.Add(knownRule.guid, knownRule);
            if (bApproved)
            {
                knownRule.SetApplied();
                return;
            }

            knownRule.SetChanged();

            RuleFixAction actionTaken = RuleFixAction.None;

            FirewallGuard.Mode Mode = (FirewallGuard.Mode)App.GetConfigInt("Firewall", "GuardMode", 0);
            if (knownRule.Enabled && (Mode == FirewallGuard.Mode.Fix || Mode == FirewallGuard.Mode.Disable))
            {
                knownRule.Enabled = false;
                FirewallManager.UpdateRule(knownRule);
                actionTaken = RuleFixAction.Disabled;
            }

            LogRuleEvent(prog, knownRule, RuleEventType.Added, actionTaken);
        }

        protected void OnRuleUpdated(FirewallRule rule, FirewallRuleEx knownRule, Program prog)
        {
            var match = knownRule.Match(rule);
            if (match == FirewallRule.MatchResult.Identical)
            {
                if (knownRule.State == FirewallRuleEx.States.Changed || knownRule.State == FirewallRuleEx.States.Deleted)
                {
                    knownRule.State = FirewallRuleEx.States.Approved; // it seams the rule recivered
                    LogRuleEvent(prog, knownRule, RuleEventType.UnChanged, RuleFixAction.None);
                }
                return;
            }

            knownRule.SetChanged();

#if DEBUG
            knownRule.Match(rule);
#endif

            FirewallGuard.Mode Mode = (FirewallGuard.Mode)App.GetConfigInt("Firewall", "GuardMode", 0);
            if (match == FirewallRule.MatchResult.TargetChanged && Mode != FirewallGuard.Mode.Fix)
            {
                // this measn the rule does not longer apply to the program it was associated with
                // handle it as if the old rule has got removed and a new rule was added

                if (prog.Rules.ContainsKey(knownRule.guid)) // should not happen but in case....
                {
                    prog.Rules.Remove(knownRule.guid);

                    if (knownRule.State == FirewallRuleEx.States.Unknown) 
                        LogRuleEvent(prog, knownRule, RuleEventType.Removed, RuleFixAction.Deleted);
                    else // if the rule was a approved rule, keep it listed
                    {
                        knownRule.guid = Guid.NewGuid().ToString("B").ToUpperInvariant(); // create a new guid to not conflict with the original one
                        prog.Rules.Add(knownRule.guid, knownRule);

                        LogRuleEvent(prog, knownRule, RuleEventType.Removed, RuleFixAction.None);
                    }
                }

                OnRuleAdded(rule);
                return;
            }

            bool bUnChanged = false;
            RuleFixAction actionTaken = RuleFixAction.None;

            if (Mode == FirewallGuard.Mode.Fix)
            {
                FirewallManager.UpdateRule(knownRule);
                actionTaken = RuleFixAction.Restored;
            }
            else if (match == FirewallRule.MatchResult.NameChanged) // no relevant changed just name, description or groupe
            {
                knownRule.Name = rule.Name;
                knownRule.Grouping = rule.Grouping;
                knownRule.Description = rule.Description;
                actionTaken = RuleFixAction.Updated;

                // did the rule recover
                if (knownRule.State == FirewallRuleEx.States.Changed || knownRule.State == FirewallRuleEx.States.Deleted)
                {
                    knownRule.State = FirewallRuleEx.States.Approved; // it seams the rule recivered
                    bUnChanged = true;
                }
            }
            else if (Mode == FirewallGuard.Mode.Disable)
            {
                if (match == FirewallRule.MatchResult.DataChanged)
                {
                    if (rule.Enabled) // if the rule changed, disable it
                    {
                        knownRule.Enabled = false;

                        rule.Enabled = false;
                        FirewallManager.UpdateRule(rule);
                        actionTaken = RuleFixAction.Disabled;
                    }

                    if (knownRule.State == FirewallRuleEx.States.Unknown)
                        knownRule.Assign(rule);
                    else if (knownRule.State == FirewallRuleEx.States.Approved || knownRule.State == FirewallRuleEx.States.Deleted)
                        knownRule.State = FirewallRuleEx.States.Changed;
                }
                else if (match == FirewallRule.MatchResult.StateChanged)
                {
                    rule.Enabled = knownRule.Enabled;
                    rule.Action = knownRule.Action;

                    // if only the state changed, in this mode we restore it
                    FirewallManager.UpdateRule(rule);
                    actionTaken = RuleFixAction.Restored;
                }
            }
            else //if (Mode == FirewallGuard.Mode.Alert)
            {
                if (knownRule.State == FirewallRuleEx.States.Unknown)
                {
                    knownRule.Assign(rule);
                    actionTaken = RuleFixAction.Updated;
                }
                else
                    knownRule.State = FirewallRuleEx.States.Changed;
            }

            LogRuleEvent(prog, knownRule, bUnChanged ? RuleEventType.UnChanged : RuleEventType.Changed, actionTaken);
        }

        protected void OnRuleRemoved(FirewallRuleEx knownRule, Program prog)
        {
            knownRule.SetChanged();

            RuleFixAction actionTaken = RuleFixAction.None;

            FirewallGuard.Mode Mode = (FirewallGuard.Mode)App.GetConfigInt("Firewall", "GuardMode", 0);
            if (knownRule.State == FirewallRuleEx.States.Unknown)
            {
                if (prog.Rules.ContainsKey(knownRule.guid)) // should not happen but in case....
                    prog.Rules.Remove(knownRule.guid);
                actionTaken = RuleFixAction.Deleted;
            }
            else if (Mode == FirewallGuard.Mode.Fix)
            {
                FirewallManager.UpdateRule(knownRule);
                actionTaken = RuleFixAction.Restored;
            }
            else
                knownRule.State = FirewallRuleEx.States.Deleted;

            LogRuleEvent(prog, knownRule, RuleEventType.Removed, actionTaken);
        }

        enum RuleEventType
        {
            Changed = 0,
            Added,
            Removed,
            UnChanged, // role was changed to again match the aproved configuration
        }

        enum RuleFixAction
        {
            None = 0,
            Restored,
            Disabled,
            Updated,
            Deleted
        }

        private void LogRuleEvent(Program prog, FirewallRuleEx rule, RuleEventType type, RuleFixAction action)
        {
            // Issue an update for the UI
            OnChanged(null, new ProgramList.ListEvent() { guid = prog.ProgSet.guid });

            // Logg the event
            Dictionary<string, string> Params = new Dictionary<string, string>();
            Params.Add("Name", rule.Name);
            Params.Add("Program", prog.Description);
            //Params.Add("ProgramSet", prog.ProgSet.config.Name);
            Params.Add("Event", type.ToString());
            Params.Add("Action", action.ToString());

            Params.Add("RuleGuid", rule.guid);
            Params.Add("ProgID", prog.ID.AsString());
            Params.Add("SetGuid", prog.ProgSet.guid.ToString());

            App.EventIDs EventID;
            string strEvent;
            switch (type)
            {
                case RuleEventType.Added:   strEvent = Translate.fmt("msg_rule_added");
                                            EventID = App.EventIDs.RuleAdded; break;
                case RuleEventType.Removed: strEvent = Translate.fmt("msg_rule_removed");
                                            EventID = App.EventIDs.RuleDeleted; break;
                default:                    strEvent = Translate.fmt("msg_rule_changed");
                                            EventID = App.EventIDs.RuleChanged; break;
            }

            string Message; // "Firewall rule \"{0}\" for \"{1}\" was {2}."
            switch (action)
            {
                case RuleFixAction.Disabled:    Message = Translate.fmt("msg_rule_event", rule.Name, prog.Description, strEvent + Translate.fmt("msg_rule_disabled")); break;
                case RuleFixAction.Restored:    Message = Translate.fmt("msg_rule_event", rule.Name, prog.Description, strEvent + Translate.fmt("msg_rule_restored")); break;
                default:                        Message = Translate.fmt("msg_rule_event", rule.Name, prog.Description, strEvent); break;
            }

            if (type == RuleEventType.UnChanged || action == RuleFixAction.Restored)
                App.LogInfo(EventID, Params, App.EventFlags.Notifications, Message);
            else
                App.LogWarning(EventID, Params, App.EventFlags.Notifications, Message);
        }

        public void ApproveRules()
        {
            App.LogInfo(Translate.fmt("msg_rules_approved"));

            foreach (Program prog in ProgramList.Programs.Values)
            {
                foreach (FirewallRuleEx rule in prog.Rules.Values)
                    rule.SetApplied();
            }

            ProgramList.StoreList(); 
        }

        public int CleanupFwRules(bool bAll = false)
        {
            UInt64 curTime = MiscFunc.GetUTCTime();

            int Count = 0;
            foreach (Program prog in ProgramList.Programs.Values)
            {
                bool bRemoved = false;

                foreach (FirewallRuleEx rule in prog.Rules.Values.ToList())
                {
                    if (rule.Expiration != 0 && (bAll || curTime >= rule.Expiration))
                    {
                        if (App.engine.FirewallManager.RemoveRule(rule))
                        {
                            bRemoved = true;
                            prog.Rules.Remove(rule.guid);
                            Count++;
                        }
                    }
                }

                if(bRemoved)
                    OnChanged(null, new ProgramList.ListEvent() { guid = prog.ProgSet.guid });
            }
            return Count;
        }

        protected void OnDnsEvent(DnsInspector.DnsEvent DnsEvent)
        {
            List<ServiceHelper.ServiceInfo> Services = ServiceHelper.GetServicesByPID(DnsEvent.ProcessId);

            ProgramID ProgID = GetProgIDbyPID(DnsEvent.ProcessId, (Services == null || Services.Count > 1) ? null : Services[0].ServiceName);
            if (ProgID == null)
                return; // proces was already terminated

            Program prog = ProgramList.GetProgram(ProgID, true, ProgramList.FuzzyModes.Any);

            prog?.LogDomain(DnsEvent.HostName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////
        // Interface
        //

        public FirewallManager.FilteringModes GetFilteringMode()
        {
            return mDispatcher.Invoke(new Func<FirewallManager.FilteringModes>(() => {
                return FirewallManager.GetFilteringMode();
            }));
        }

        public bool SetFilteringMode(FirewallManager.FilteringModes Mode)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return FirewallManager.SetFilteringMode(Mode);
            }));
        }

        public bool IsFirewallGuard()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return FirewallGuard.HasAuditPolicy();
            }));
        }

        public bool SetFirewallGuard(bool guard, FirewallGuard.Mode mode)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                if(guard)
                    ApproveRules();
                App.SetConfig("Firewall", "RuleGuard", guard == true ? 1 : 0);
                App.SetConfig("Firewall", "GuardMode", ((int)mode).ToString());
                return FirewallGuard.SetAuditPolicy(guard);
            }));
        }

        public FirewallMonitor.Auditing GetAuditPolicy()
        {
            return mDispatcher.Invoke(new Func<FirewallMonitor.Auditing>(() => {
                return FirewallMonitor.GetAuditPolicy();
            }));
        }

        public bool SetAuditPolicy(FirewallMonitor.Auditing audit)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                App.SetConfig("Firewall", "AuditPolicy", audit.ToString());
                return FirewallMonitor.SetAuditPolicy(audit);
            }));
        }

        public List<ProgramSet> GetPrograms(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<List<ProgramSet>>(() => {
                return ProgramList.GetPrograms(guids);
            }));
        }

        public ProgramSet GetProgram(ProgramID id, bool canAdd = false)
        {
            return mDispatcher.Invoke(new Func<ProgramSet>(() => {
                Program prog = ProgramList.GetProgram(id, canAdd);
                return prog?.ProgSet;
            }));
        }

        public bool AddProgram(ProgramID id, Guid guid)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return ProgramList.AddProgram(id, guid);
            }));
        }

        public bool UpdateProgram(Guid guid, ProgramSet.Config config, UInt64 expiration = 0)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return ProgramList.UpdateProgram(guid, config, expiration);
            }));
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return ProgramList.MergePrograms(to, from);
            }));
        }

        public bool SplitPrograms(Guid from, ProgramID id)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return ProgramList.SplitPrograms(from, id);
            }));
        }
        public bool RemoveProgram(Guid guid, ProgramID id = null)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return ProgramList.RemoveProgram(guid, id);
            }));
        }

        public bool LoadRules()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return LoadFwRules();
            }));
        }

        //

        public Dictionary<Guid, List<FirewallRuleEx>> GetRules(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<Dictionary<Guid, List<FirewallRuleEx>>>(() => {
                List<ProgramSet> progs = ProgramList.GetPrograms(guids);
                Dictionary<Guid, List<FirewallRuleEx>> rules = new Dictionary<Guid, List<FirewallRuleEx>>();
                foreach (ProgramSet progSet in progs)
                {
                    List<FirewallRuleEx> Rules = progSet.GetRules();

                    // Note: if a rule has status changed we want to return the actual rule, not the cached approved value
                    for (int i = 0; i < Rules.Count; i++)
                    {
                        if ((Rules[i] as FirewallRuleEx).State == FirewallRuleEx.States.Changed)
                        {
                            FirewallRule Rule = FirewallManager.GetRule(Rules[i].guid);
                            if (Rule != null)
                                Rules[i] = new FirewallRuleEx(Rules[i], Rule);
                        }
                    }

                    rules.Add(progSet.guid, Rules);
                }
                return rules;
            }));
        }

        public bool UpdateRule(FirewallRule rule, UInt64 expiration = 0)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                Program prog = ProgramList.GetProgram(rule.ProgID, true);
                if (rule.guid == null)
                {
                    if (rule.Direction == FirewallRule.Directions.Bidirectiona)
                    {
                        FirewallRule copy = rule.Duplicate();
                        copy.Direction = FirewallRule.Directions.Inbound;

                        if (!FirewallManager.ApplyRule(prog, copy, expiration))
                            return false;

                        rule.Direction = FirewallRule.Directions.Outboun;
                    }
                }
                else // remove old roule from program
                {
                    FirewallRule old_rule = FirewallManager.GetRule(rule.guid);
                    Program old_prog = old_rule == null ? null : (old_rule.ProgID == rule.ProgID ? prog : ProgramList.GetProgram(old_rule.ProgID));

                    // if rhe rule now belongs to a different program we have to update booth
                    if (old_prog != null && old_rule.ProgID == rule.ProgID)
                    {
                        old_prog?.Rules.Remove(old_rule.guid);

                        OnChanged(null, new ProgramList.ListEvent() { guid = prog.ProgSet.guid });
                    }
                }

                // update/add rule and (re) add the new rule to program
                if (!FirewallManager.ApplyRule(prog, rule, expiration)) // if the rule is new this will set the guid
                    return false;

                App.engine.FirewallManager.EvaluateRules(prog.ProgSet);

                OnChanged(null, new ProgramList.ListEvent() { guid = prog.ProgSet.guid });

                return true;
            }));
        }

        public bool RemoveRule(FirewallRule rule)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                if (!FirewallManager.RemoveRule(rule))
                    return false;
                
                var prog = ProgramList.GetProgram(rule.ProgID);
                if (prog != null)
                {
                    prog.Rules.Remove(rule.guid);

                    OnChanged(null, new ProgramList.ListEvent() { guid = prog.ProgSet.guid });
                }
                return true;
            }));
        }

        public bool BlockInternet(bool bBlock)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                bool ret = true;
                ProgramID id = ProgramID.NewID(ProgramID.Types.Global);
                Program prog = ProgramList.GetProgram(id, true);
                if (bBlock)
                {
                    FirewallRule ruleOut = new FirewallRule(prog.ID);
                    ruleOut.Name = FirewallManager.MakeRuleName(FirewallManager.BlockAllName, false, prog.Description);
                    ruleOut.Grouping = FirewallManager.RuleGroup;
                    ruleOut.Action = FirewallRule.Actions.Block;
                    ruleOut.Direction = FirewallRule.Directions.Outboun;
                    ruleOut.Enabled = true;

                    FirewallRule ruleIn = ruleOut.Duplicate();
                    ruleIn.Direction = FirewallRule.Directions.Inbound;

                    ret &= FirewallManager.ApplyRule(prog, ruleOut);
                    ret &= FirewallManager.ApplyRule(prog, ruleIn);
                }
                else
                {
                    FirewallManager.ClearRules(prog, false);
                }
                return ret;
            }));
        }

        public bool ClearLog(bool ClearSecLog)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                if (ClearSecLog)
                {
                    EventLog eventLog = new EventLog("Security");
                    eventLog.Clear();
                    eventLog.Dispose();
                }
                ProgramList.ClearLog();
                return true;
            }));
        }
        public int CleanUpPrograms()
        {
            return mDispatcher.Invoke(new Func<int>(() => {
                return ProgramList.CleanUp();
            }));
        }

        public Dictionary<Guid, List<Program.LogEntry>> GetConnections(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<Dictionary<Guid, List<Program.LogEntry>>>(() => {
                List<ProgramSet> progs = ProgramList.GetPrograms(guids);
                Dictionary<Guid, List<Program.LogEntry>> entries = new Dictionary<Guid, List<Program.LogEntry>>();
                foreach (ProgramSet progSet in progs)
                    entries.Add(progSet.guid, progSet.GetConnections());
                return entries;
            }));
        }

        public Dictionary<Guid, List<NetworkSocket>> GetSockets(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<Dictionary<Guid, List<NetworkSocket>>>(() => {
                List<ProgramSet> progs = ProgramList.GetPrograms(guids);
                Dictionary<Guid, List<NetworkSocket>> entries = new Dictionary<Guid, List<NetworkSocket>>();
                foreach (ProgramSet progSet in progs)
                    entries.Add(progSet.guid, progSet.GetSockets());
                return entries;
            }));
        }

        public Dictionary<Guid, List<Program.DnsEntry>> GetDomains(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<Dictionary<Guid, List<Program.DnsEntry>>>(() => {
                List<ProgramSet> progs = ProgramList.GetPrograms(guids);
                Dictionary<Guid, List<Program.DnsEntry>> entries = new Dictionary<Guid, List<Program.DnsEntry>>();
                foreach (ProgramSet progSet in progs)
                    entries.Add(progSet.guid, progSet.GetDomains());
                return entries;
            }));
        }


        public bool ApplyTweak(TweakManager.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.ApplyTweak(tweak);
            }));
        }

        public bool TestTweak(TweakManager.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.TestTweak(tweak);
            }));
        }

        public bool UndoTweak(TweakManager.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.UndoTweak(tweak);
            }));
        }
    }
}
