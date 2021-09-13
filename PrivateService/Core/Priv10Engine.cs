using MiscHelpers;
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
using TweakEngine;
using PrivateService;
using PrivateAPI;
using WinFirewallAPI;

namespace PrivateWin10
{
    public class Priv10Engine //: IDisposable
    {
        public ProgramList ProgramList;

        public FirewallManager FirewallManager;
        public FirewallMonitor FirewallMonitor;
        public FirewallGuard FirewallGuard;
        public AppManager PkgMgr; // Windows 8 & 10 App Manager
        public ProcessMonitor ProcessMonitor;
        public NetworkMonitor NetworkMonitor;
        public DnsInspector DnsInspector;
        public DnsProxyServer DnsProxy;

        Dispatcher mDispatcher;
        ManualResetEvent mStarted = new ManualResetEvent(false);
        //ManualResetEvent mFinished = new ManualResetEvent(false);
        DispatcherTimer mTimer;
        volatile bool mDoQuit = false;
        DateTime LastSaveTime = DateTime.Now;
        bool SettingsChanged = false;

#if DEBUG
        //List<EtwAbstractLogger> EtwLoggers = new List<EtwAbstractLogger>();
#endif

        public delegate void FX();

        public enum UpdateTypes
        {
            ProgSet = 0,
            Rules
        }

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
            FirewallManager = new FirewallManager();

            if (!UwpFunc.IsWindows7OrLower)
            {
                Console.WriteLine("Initializing app manager...");
                PkgMgr = new AppManager();
            }

            AppLog.Debug("Loading program list...");
            ProgramList = new ProgramList();
            ProgramList.Load();

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

            if (App.GetConfigInt("Firewall", "RuleGuard", 0) != 0 && App.GetConfigInt("Firewall", "Enabled", 0) != 0)
            {
                if (!FirewallGuard.HasAuditPolicy())
                {
                    AppLog.Debug("Re-Enabling Firewall Rule auditing policy ...");
                    FirewallGuard.SetAuditPolicy(true);
                }
            }

            FirewallGuard.ChangeEvent += (object sender, WinFirewallAPI.RuleChangedEvent FwEvent) =>
            {
                RunInEngineThread(() => {
                    if(App.GetConfigInt("Firewall", "RuleGuard", 0) != 0)
                        OnRuleChangedEvent(FwEvent);
                });
            };

            if (App.GetConfigInt("Startup", "LoadLog", 0) != 0)
                LoadLogAsync();

            LoadFwRules();
            CleanupFwRules(true); // startup cleanup, remove temp rules imminetly

            AppLog.Debug("Starting Process Monitor...");
            ProcessMonitor = new ProcessMonitor();

            AppLog.Debug("Starting Network Monitor...");
            NetworkMonitor = new NetworkMonitor();
            NetworkMonitor.NetworksChanged += (object sender, EventArgs e) =>
            {
                DnsProxy?.ConfigureSystemDNS();
            };

            if (App.GetConfigInt("DnsInspector", "Enabled", 0) != 0)
            {
                AppLog.Debug("Starting Dns Inspector...");
                DnsInspector = new DnsInspector();
            }

            if (App.GetConfigInt("DnsProxy", "Enabled", 0) != 0)
            {
                AppLog.Debug("Starting Dns Proxy...");
                DnsProxy = new DnsProxyServer();
                if (!DnsProxy.Init())                   
                    DnsProxy = null;
            }
            //

            AppLog.Debug("Setting up IPC host...");
            App.host = new Priv10Host();
            App.host.Listen();

            mStarted.Set();

            ProgramList.Changed += (object sender, ProgramList.ListEvent e) =>
            {
                NotifyProgramUpdate(e.guid);
            };

            AppLog.Debug("Starting engine timer...");


            // test here
            //...
#if DEBUG
            //EtwLoggers.Add(new EtwUserLogger("dns_client", Guid.Parse("1c95126e-7eea-49a9-a3fe-a378b03ddb4d")));
            //EtwLoggers.Add(new EtwUserLogger("winsock_dns", Guid.Parse("55404e71-4db9-4deb-a5f5-8f86e46dde56")));
            //EtwLoggers.Add(new EtwUserLogger("winsock_afd", Guid.Parse("e53c6823-7bb8-44bb-90dc-3f86090d48a6")));
#endif

            mTimer = new DispatcherTimer();
            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 250); // 4x a second
            mTimer.Start();

            // queue a refresh push
            RunInEngineThread(() => {
                NotifyProgramUpdate(Guid.Empty);
            });

            Dispatcher.Run(); // run

            mTimer.Stop();

            // UnInit
            AppLog.Debug("Saving program list...");
            ProgramList.Store();

            FirewallMonitor.StopEventWatcher();
            ProcessMonitor.Dispose();
            NetworkMonitor.Dispose();
            if (DnsInspector != null)
                DnsInspector.Dispose();
            if(DnsProxy != null)
                DnsProxy.Dispose();
            //

            AppLog.Debug("Shuttin down IPC host...");
            App.host.Close();

            //mFinished.Set();

            mDispatcher = null;

            AppLog.Debug("Engine Thread Terminating");
        }

        int mTickCount = 0;

        protected void OnTimerTick(object sender, EventArgs e)
        {
            if ((mTickCount++ % 4) != 0)
                return;

            NetworkMonitor.UpdateNetworks();

            NetworkMonitor.UpdateSockets();

            ProcessFirewallEvents();

            ProcessRuleChanges();

            ProgramList.UpdatePrograms(); // data rates and so on

            if ((mTickCount % (4 * 60)) == 0) // every minute
            {
                CleanupFwRules(); // remove temporary rules

                ProcessMonitor.CleanUpProcesses();
            }

            DnsProxy?.blockList?.CheckForUpdates();

            DnsInspector?.Process();

            if (LastSaveTime.AddMinutes(15) < DateTime.Now) // every 15 minutes
            {
                LastSaveTime = DateTime.Now;

                ProgramList.Store();

                if (DnsProxy != null)
                    DnsProxy.Store();
            }

            if (SettingsChanged)
            {
                SettingsChanged = false;
                App.host.NotifySettingsChanged();
            }

            if (mDoQuit)
                mDispatcher.InvokeShutdown();
        }

        public void Stop()
        {
            if (mDispatcher == null)
                return;

            mDispatcher.InvokeShutdown();
            mDispatcher.Thread.Join(); // Note: this waits for thread finish

            //mFinished.WaitOne();
        }

        public ProgramID GetProgIDbyPID(int PID, string serviceTag, string fileName = null)
        {
            if (PID == ProcFunc.SystemPID)
                return ProgramID.NewID(ProgramID.Types.System);

            if (fileName == null || fileName.Length == 0)
            {
                //fileName = ProcFunc.GetProcessFileNameByPID(PID);
                fileName = ProcessMonitor.GetProcessFileNameByPID(PID);
                if (fileName == null)
                    return null;
            }

            if (serviceTag != null)
                return FirewallRuleEx.AdjustProgID(ProgramID.NewSvcID(serviceTag, fileName));

            string SID = ProcFunc.GetAppPackageSidByPID(PID);
            if (SID != null)
                return ProgramID.NewAppID(SID, fileName);

            return ProgramID.NewProgID(fileName);
        }

        struct QueuedFwEvent
        {
            public FirewallEvent FwEvent;
            public NetworkMonitor.AdapterInfo NicInfo;
            public List<ProcessMonitor.ServiceInfo> Services;
        }

        List<QueuedFwEvent> QueuedFwEvents = new List<QueuedFwEvent>();

        protected void OnFirewallEvent(FirewallEvent FwEvent)
        {
            NetworkMonitor.AdapterInfo NicInfo = NetworkMonitor.GetAdapterInfoByIP(FwEvent.LocalAddress);

            OnFirewallEvent(FwEvent, NicInfo);
        }

        protected void OnFirewallEvent(FirewallEvent FwEvent, NetworkMonitor.AdapterInfo NicInfo)
        {
            ProgramID ProgID;
            if (FwEvent.ProcessFileName.Equals("System", StringComparison.OrdinalIgnoreCase))
                ProgID = ProgramID.NewID(ProgramID.Types.System);
            else
            {
                List<ProcessMonitor.ServiceInfo> Services = ProcessMonitor.GetServicesByPID(FwEvent.ProcessId);
                if (Services == null || Services.Count == 1)
                {
                    ProgID = GetProgIDbyPID(FwEvent.ProcessId, Services == null ? null : Services[0].ServiceName, FwEvent.ProcessFileName);
                    if (ProgID == null)
                        return; // the process already terminated and we did not have it's file name, just ignore this event
                }
                else //if(Services.Count > 1)
                {
                    // we don't have a unique service match, the process is hosting multiple services :/

                    QueuedFwEvents.Add(new QueuedFwEvent() { FwEvent = FwEvent, NicInfo = NicInfo, Services = Services });
                    return;
                }
            }

            OnFirewallEvent(FwEvent, NicInfo, ProgID);
        }

        protected void OnFirewallEvent(FirewallEvent FwEvent, NetworkMonitor.AdapterInfo NicInfo, ProgramID progID)
        {
            Program prog = ProgramList.FindProgram(progID, true, ProgramID.FuzzyModes.Any);

            Program.LogEntry entry = new Program.LogEntry(FwEvent, progID);
            if (NicInfo.Profile == FirewallRule.Profiles.All)
                entry.State = Program.LogEntry.States.FromLog;
            else
            {
                FirewallRule.Actions RuleAction = prog.LookupRuleAction(FwEvent, NicInfo);
                entry.CheckAction(RuleAction);
            }
            prog.AddLogEntry(entry);

            PushLogEntry(entry, prog);
        }

        protected void PushLogEntry(Program.LogEntry entry, Program prog, List<String> services = null)
        {
            bool Delayed = false;
            //DnsInspector.ResolveHost(entry.FwEvent.ProcessId, entry.FwEvent.RemoteAddress, entry, Program.LogEntry.HostSetter);
            if (DnsInspector != null)
            {
                DnsInspector.GetHostName(entry.FwEvent.ProcessId, entry.FwEvent.RemoteAddress, entry, (object obj, string name, NameSources source) =>
                {

                    var old_source = (obj as Program.LogEntry).RemoteHostNameSource;
                    Program.LogEntry.HostSetter(obj, name, source);

                    // if the resolution was delayed, re emit this event, its unique gui wil prevent it form being logged twice
                    if (Delayed && source > old_source) // only update if we got a better host name
                        App.host.NotifyActivity(prog.ProgSet.guid, entry, prog.ID, services, true);
                });
            }
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
                        OnFirewallEvent(cur.FwEvent, cur.NicInfo, Socket.ProgID);
                        return;
                    }
                }

                // try to find a proramm with a matching rule
                List<ProgramID> machingIDs = new List<ProgramID>();
                List<ProgramID> unruledIDs = new List<ProgramID>();
                List<String> services = new List<String>();
                foreach (ProcessMonitor.ServiceInfo info in cur.Services)
                {
                    services.Add(info.ServiceName);

                    ProgramID progID = GetProgIDbyPID(cur.FwEvent.ProcessId, info.ServiceName, cur.FwEvent.ProcessFileName);
                    Program prog = ProgramList.FindProgram(progID, false, ProgramID.FuzzyModes.Tag); // fuzzy match i.e. service tag match is enough

                    FirewallRule.Actions RuleAction = prog == null ? FirewallRule.Actions.Undefined : prog.LookupRuleAction(cur.FwEvent, cur.NicInfo);

                    // check if the program has any matchign rules
                    if (RuleAction == cur.FwEvent.Action)
                        machingIDs.Add(progID);
                    // if no program was found or it does not have matchign rules
                    else if (RuleAction == FirewallRule.Actions.Undefined)
                        unruledIDs.Add(progID);
                }

                // did we found one matching service?
                if (machingIDs.Count == 1)
                    OnFirewallEvent(cur.FwEvent, cur.NicInfo, machingIDs[0]);

                // if we have found no services with matching rules, but one service without any roules
                else if (machingIDs.Count == 0 && unruledIDs.Count == 1)
                    OnFirewallEvent(cur.FwEvent, cur.NicInfo, unruledIDs[0]);

                // well damn it we dont couldn't find out which service this event belongs to
                else
                {
                    // if there is at least one matchign rule, don't show a connection notification
                    bool bHasMatches = machingIDs.Count > 0;

                    // get the default action for if there is no rule
                    FirewallRule.Actions DefaultAction = FirewallManager.LookupRuleAction(cur.FwEvent, cur.NicInfo);

                    // if the action taken matches the default action, than unruled results are equivalent to the matches once
                    if (DefaultAction == cur.FwEvent.Action)
                        machingIDs.AddRange(unruledIDs);
                    // if the action taken does match the default action, unruled entries must be wrong
                    unruledIDs.Clear();

                    if (bHasMatches)
                    {
                        // emit an event for every possible match
                        foreach (var progID in machingIDs)
                            OnFirewallEvent(cur.FwEvent, cur.NicInfo, progID);
                    }
                    else
                    {
                        // log entry for firewall notification

                        ProgramID progID = GetProgIDbyPID(cur.FwEvent.ProcessId, null, cur.FwEvent.ProcessFileName);

                        Program prog = ProgramList.FindProgram(progID, true, ProgramID.FuzzyModes.No);

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

                    var FileName = entry.ProcessId == ProcFunc.SystemPID ? "System" : ProcFunc.GetProcessFileNameByPID(entry.ProcessId);
                    if (FileName == null || !entry.ProcessFileName.Equals(FileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    OnFirewallEvent(entry, new NetworkMonitor.AdapterInfo() { Profile = FirewallRule.Profiles.All });
                }

                AppLog.Debug("Finished loading log asynchroniusly");
            }).Start();
        }

        public void NotifyProgramUpdate(Guid guid)
        {
            if (App.host != null)
                App.host.NotifyProgUpdate(guid, UpdateTypes.ProgSet);
        }

        public void NotifyRulesUpdte(Guid guid)
        {
            if (App.host != null)
                App.host.NotifyProgUpdate(guid, UpdateTypes.Rules);
        }

        public void OnRulesUpdated(ProgramSet progSet)
        {
            foreach (Program prog in progSet.Programs.Values)
            {
                foreach (NetworkSocket Socket in prog.Sockets.Values)
                    Socket.Access = prog.LookupRuleAccess(Socket);
            }

            NotifyRulesUpdte(progSet.guid);
        }

        public void OnRulesUpdated(Program prog)
        {
            foreach (NetworkSocket Socket in prog.Sockets.Values)
                Socket.Access = prog.LookupRuleAccess(Socket);

            NotifyRulesUpdte(prog.ProgSet.guid);
        }

        protected class RuleChangedEvent
        {
            public string RuleId;
            public string RuleName;
        }

        Dictionary<string, RuleChangedEvent> RuleChangedEvents = new Dictionary<string, RuleChangedEvent>();

        protected void OnRuleChangedEvent(WinFirewallAPI.RuleChangedEvent FwEvent)
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
                    prog = ProgramList.FindProgram(FirewallRuleEx.GetIdFromRule(oldRule));
                    if (prog != null)
                        prog.Rules.TryGetValue(changeEvent.RuleId, out knownRule);

                    if(knownRule == null)
                        Priv10Logger.LogCriticalError("rule lists are inconsistent");
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

        public bool LoadFwRules()
        {
            AppLog.Debug("Loading Windows Firewall rules...");
            List<FirewallRule> rules = FirewallManager.LoadRules();
            if (rules == null)
                return false; // failed to load rules

            if ((App.GetConfigInt("Firewall", "RuleGuard", 0) != 0) && FirewallGuard.HasAuditPolicy())
            {
                AppLog.Debug("Loading Known Firewall rules...");

                Dictionary<string, Tuple<FirewallRuleEx, Program>> OldRules = new Dictionary<string, Tuple<FirewallRuleEx, Program>>();

                foreach (Program prog in ProgramList.Programs.Values)
                {
                    foreach (FirewallRuleEx rule in prog.Rules.Values)
                        OldRules.Add(rule.guid, Tuple.Create(rule, prog));
                }

                bool bApproveAll = OldRules.Count == 0;
                if (bApproveAll)
                    Priv10Logger.LogInfo("All valid Firewall Rules have been approved.");
                    //Priv10Logger.LogInfo(Translate.fmt("msg_rules_approved"));

                foreach (FirewallRule rule in rules)
                {
                    Tuple<FirewallRuleEx, Program> value;
                    if (!OldRules.TryGetValue(rule.guid, out value))
                        OnRuleAdded(rule, bApproveAll);
                    else
                    {
                        OldRules.Remove(rule.guid);

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
            {
                AppLog.Debug("Assigning Firewall rules...");

                // clear all old rules
                foreach (Program prog in ProgramList.Programs.Values)
                    prog.Rules.Clear();

                // assign new rules
                foreach (FirewallRule rule in rules)
                {
                    Program prog = ProgramList.FindProgram(FirewallRuleEx.GetIdFromRule(rule), true);
                    FirewallRuleEx ruleEx = new FirewallRuleEx();
                    ruleEx.Assign(rule);
                    ruleEx.SetApplied();

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
            Program prog = ProgramList.FindProgram(FirewallRuleEx.GetIdFromRule(rule), true);
            if (prog.Rules.ContainsKey(rule.guid))
            {
                Priv10Logger.LogCriticalError("rule lists are inconsistent 2");
                return;
            }

            FirewallRuleEx knownRule = new FirewallRuleEx();
            knownRule.Assign(rule);

            if (rule.Name.IndexOf(FirewallManager.TempRulePrefix) == 0) // Note: all temporary rules start with priv10temp - 
                knownRule.Expiration = MiscFunc.GetUTCTime(); // expire now

            if (bApproved)
            {
                prog.Rules.Add(knownRule.guid, knownRule);
                knownRule.SetApplied();
                return;
            }

            knownRule.SetChanged();

            RuleFixAction actionTaken = RuleFixAction.None;

            FirewallGuard.Mode Mode = (FirewallGuard.Mode)App.GetConfigInt("Firewall", "GuardMode", 0);

            if (Mode == FirewallGuard.Mode.Fix)
            {
                FirewallManager.RemoveRule(knownRule.guid);
                actionTaken = RuleFixAction.Deleted;
            }
            else
            {
                prog.Rules.Add(knownRule.guid, knownRule);

                if (knownRule.Enabled && Mode == FirewallGuard.Mode.Disable)
                {
                    knownRule.Backup = knownRule.Duplicate();
                    knownRule.Enabled = false;
                    FirewallManager.UpdateRule(knownRule);
                    actionTaken = RuleFixAction.Disabled;
                }
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
                    //LogRuleEvent(prog, knownRule, RuleEventType.UnChanged, RuleFixAction.None);
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
                if(knownRule.Backup == null)
                    knownRule.Backup = rule.Duplicate();
                knownRule.State = FirewallRuleEx.States.Changed;

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
                if (match == FirewallRule.MatchResult.DataChanged || match == FirewallRule.MatchResult.StateChanged) // data changed disable rule
                {
                    if (rule.Enabled) // if the rule changed, disable it
                    {
                        if (knownRule.Backup == null)
                            knownRule.Backup = rule.Duplicate();
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
                /*else if (match == FirewallRule.MatchResult.StateChanged) // state changed restore original state
                {
                    rule.Enabled = knownRule.Enabled;
                    rule.Action = knownRule.Action;

                    // if only the state changed, in this mode we restore it
                    FirewallManager.UpdateRule(rule);
                    actionTaken = RuleFixAction.Restored;
                }*/
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
                knownRule.State = FirewallRuleEx.States.Deleted;

                if (FirewallManager.UpdateRule(knownRule))
                    knownRule.State = FirewallRuleEx.States.Approved;

                actionTaken = RuleFixAction.Restored;
            }
            else
                knownRule.State = FirewallRuleEx.States.Deleted;

            LogRuleEvent(prog, knownRule, RuleEventType.Removed, actionTaken);
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

        private void LogRuleEvent(Program prog, FirewallRuleEx rule, RuleEventType type, RuleFixAction action)
        {
            OnRulesUpdated(prog);

            if (App.host != null)
                App.host.NotifyRuleChange(prog, rule, type, action);

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

            Priv10Logger.EventIDs EventID;
            string strEvent;
            switch (type)
            {
                case RuleEventType.Added:   strEvent = "Added"; //Translate.fmt("msg_rule_added");
                                            EventID = Priv10Logger.EventIDs.RuleAdded; break;
                case RuleEventType.Removed: strEvent = "Removed"; //Translate.fmt("msg_rule_removed");
                                            EventID = Priv10Logger.EventIDs.RuleDeleted; break;
                default:                    strEvent = "Changed"; //Translate.fmt("msg_rule_changed");
                                            EventID = Priv10Logger.EventIDs.RuleChanged; break;
            }

            string RuleName = App.GetResourceStr(rule.Name);            

            string Message; // "Firewall rule \"{0}\" for \"{1}\" was {2}."
            /*switch (action)
            {
                case RuleFixAction.Disabled:    Message = Translate.fmt("msg_rule_event", RuleName, prog.Description, strEvent + Translate.fmt("msg_rule_disabled")); break;
                case RuleFixAction.Restored:    Message = Translate.fmt("msg_rule_event", RuleName, prog.Description, strEvent + Translate.fmt("msg_rule_restored")); break;
                default:                        Message = Translate.fmt("msg_rule_event", RuleName, prog.Description, strEvent); break;
            }*/
            switch (action)
            {
                case RuleFixAction.Disabled:    Message = string.Format("Firewall rule \"{0}\" for \"{1}\" was {2}, the rule has been disabled.", RuleName, prog.Description, strEvent); break;
                case RuleFixAction.Deleted:     Message = string.Format("Firewall rule \"{0}\" for \"{1}\" was {2}, it was removed.", RuleName, prog.Description, strEvent); break;
                case RuleFixAction.Restored:    Message = string.Format("Firewall rule \"{0}\" for \"{1}\" was {2}, the original rule was restored.", RuleName, prog.Description, strEvent); break;
                default:                        Message = string.Format("Firewall rule \"{0}\" for \"{1}\" was {2}.", RuleName, prog.Description, strEvent); break;
            }

            if (type == RuleEventType.UnChanged || action == RuleFixAction.Restored)
                Priv10Logger.LogInfo(EventID, Params, Priv10Logger.EventFlags.AppLogEntries, Message);
            else
                Priv10Logger.LogWarning(EventID, Params, Priv10Logger.EventFlags.AppLogEntries, Message);
        }

        public void ApproveRules()
        {
            //Priv10Logger.LogInfo(Translate.fmt("msg_rules_approved"));
            Priv10Logger.LogInfo("All valid Firewall Rules have been approved.");

            foreach (Program prog in ProgramList.Programs.Values)
            {
                foreach (FirewallRuleEx rule in prog.Rules.Values)
                    rule.SetApplied();
            }

            ProgramList.Store(); 
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
                        if (App.engine.FirewallManager.RemoveRule(rule.guid))
                        {
                            Priv10Logger.LogInfo("Removed expired/temporary rule: {0}", App.GetResourceStr(rule.Name));
                            bRemoved = true;
                            prog.Rules.Remove(rule.guid);
                            Count++;
                        }
                    }
                }

                if(bRemoved)
                    OnRulesUpdated(prog);
            }
            return Count;
        }

        public int CleanupFwDuplicates(CleanupMode Mode)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            
            int Count = 0;
            foreach (Program prog in ProgramList.Programs.Values)
            {
                bool bRemoved = false;

                List<FirewallRuleEx> Rules = prog.Rules.Values.ToList();

                Action<FirewallRuleEx> RemoveRule = (rule) =>
                {
                    Rules.Remove(rule);
/*#if DEBUG
                    if(!bRemoved)
                        Debug.WriteLine("\r\nChecking for Duplicat Rules: " +  prog.Description); // thats sooo slow !!!!
                    Debug.WriteLine("Removed Duplicat Rule: " + App.GetResourceStr(rule.Name)); // thats sooo slow !!!!
#endif*/
                    if (App.engine.FirewallManager.RemoveRule(rule.guid))
                    {
                        Priv10Logger.LogInfo("Removed duplicate rule: {0}", App.GetResourceStr(rule.Name));
                        bRemoved = true;
                        prog.Rules.Remove(rule.guid);
                        Count++;
                    }
                };


                Func<FirewallRuleEx, FirewallRuleEx, int> TestRule = (CurRule, NextRule) =>
                {
                    int dropCur = CurRule.HasRealGuid() ? -1 : 0;
                    int dropNext = NextRule.HasRealGuid() ? 1 : 0;
                    if (dropCur == 0 && dropNext == 0)
                        return 0; // if booth rules have custom string guids we cant drop any

                    FirewallRule.MatchResult match = CurRule.Match(NextRule);
                    if (match == FirewallRule.MatchResult.Identical || match == FirewallRule.MatchResult.NameChanged)
                        return dropNext != 0 ? dropNext : dropCur; // identical, may be except the name, remove duplicate
                    if (match == FirewallRule.MatchResult.StateChanged)
                    {
                        if (CurRule.Enabled != NextRule.Enabled)
                            return CurRule.Enabled ? dropNext : dropCur; // keep the enaled rule

                        if (CurRule.Action == NextRule.Action)
                            return 0; // this can't happen but in case

                        if (Mode == CleanupMode.RemoveDuplicatesAllow) // keep the allow rule
                            return CurRule.Action == FirewallRule.Actions.Allow ? dropNext : dropCur;
                        else if (Mode == CleanupMode.RemoveDuplicatesBlock) // keep the block rule
                            return CurRule.Action == FirewallRule.Actions.Block ? dropNext : dropCur;
                    }
                    //if (match == FirewallRule.MatchResult.TargetChanged || match == FirewallRule.MatchResult.DataChanged)
                    return 0;
                };

                for (int i = 0; i < Rules.Count; i++)
                {
                    FirewallRuleEx CurRule = Rules[i];
                    for (int j = i + 1; j < Rules.Count;)
                    {
                        FirewallRuleEx NextRule = Rules[j];
                        int remove = TestRule(CurRule, NextRule);
                        if (remove == 1)
                            RemoveRule(NextRule);
                        else if (remove == -1)
                        {
                            RemoveRule(CurRule);
                            CurRule = NextRule;
                        }
                        else
                            j++;
                    }
                }

                if(bRemoved)
                    OnRulesUpdated(prog);
            }

            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;

            //AppLog.Debug("CleanupFwDuplicates took: " + elapsedMs + "ms");

            return Count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////

        /////////////////////////////////////////
        // Windows Firewall

        public FirewallManager.FilteringModes GetFilteringMode()
        {
            return mDispatcher.Invoke(new Func<FirewallManager.FilteringModes>(() => {
                return FirewallManager.GetFilteringMode();
            }));
        }

        public bool SetFilteringMode(FirewallManager.FilteringModes Mode)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                
                SettingsChanged = true;

                return FirewallManager.SetFilteringMode(Mode);
            }));
        }

        public bool IsFirewallGuard()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return (App.GetConfigInt("Firewall", "RuleGuard", 0) != 0) && FirewallGuard.HasAuditPolicy();
            }));
        }

        public bool SetFirewallGuard(bool guard, FirewallGuard.Mode mode)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                
                SettingsChanged = true;

                App.SetConfig("Firewall", "RuleGuard", guard == true ? 1 : 0);
                App.SetConfig("Firewall", "GuardMode", ((int)mode).ToString());
                if (guard == FirewallGuard.HasAuditPolicy())
                    return true; // don't do much if only the mode changed
                if (guard)
                    ApproveRules();
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
                
                SettingsChanged = true;

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
                return ProgramList.AddProgram(FirewallRuleEx.AdjustProgID(id), guid);
            }));
        }

        public bool UpdateProgram(Guid guid, ProgramConfig config, UInt64 expiration = 0)
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
                                Rules[i] = new FirewallRuleEx(Rules[i], Rule);// { Backup = Rules[i] };
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
                Program prog = ProgramList.FindProgram(FirewallRuleEx.GetIdFromRule(rule), true);
                if (rule.guid == null)
                {
                    if (rule.Direction == FirewallRule.Directions.Bidirectiona)
                    {
                        FirewallRule copy = rule.Duplicate();
                        copy.Direction = FirewallRule.Directions.Inbound;

                        if (!FirewallManager.ApplyRule(prog, copy, expiration))
                            return false;

                        rule.Direction = FirewallRule.Directions.Outbound;
                    }
                }
                else // remove old roule from program
                {
                    FirewallRule old_rule = FirewallManager.GetRule(rule.guid);
                    ProgramID old_id = FirewallRuleEx.GetIdFromRule(old_rule);
                    ProgramID new_id = FirewallRuleEx.GetIdFromRule(rule);
                    Program old_prog = old_rule == null ? null : (old_id == new_id ? prog : ProgramList.FindProgram(old_id));

                    // if rhe rule now belongs to a different program we have to update booth
                    if (old_prog != null && old_id == new_id)
                    {
                        old_prog?.Rules.Remove(old_rule.guid);

                        OnRulesUpdated(prog);
                    }
                }

                // update/add rule and (re) add the new rule to program
                if (!FirewallManager.ApplyRule(prog, rule, expiration)) // if the rule is new this will set the guid
                    return false;

                OnRulesUpdatedEx(prog);

                return true;
            }));
        }

        private void OnRulesUpdatedEx(Program prog)
        {
            OnRulesUpdated(prog);

            App.engine.FirewallManager.EvaluateRules(prog.ProgSet);
            NotifyProgramUpdate(prog.ProgSet.guid);
        }

        public bool RemoveRule(string guid, ProgramID ProgID)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                if (!FirewallManager.RemoveRule(guid))
                    return false;
                
                var prog = ProgramList.FindProgram(ProgID);
                if (prog != null)
                {
                    prog.Rules.Remove(guid);

                    OnRulesUpdated(prog);
                }
                return true;
            }));
        }

        private bool ApproveRule(bool bApply, Program prog, FirewallRuleEx ruleEx)
        {
            if (ruleEx.State == FirewallRuleEx.States.Deleted)
            {
                if (bApply)
                {
                    if (!FirewallManager.RemoveRule(ruleEx.guid))
                        return false;
                }
                prog.Rules.Remove(ruleEx.guid);
            }
            else if (ruleEx.State == FirewallRuleEx.States.Changed || ruleEx.State == FirewallRuleEx.States.Unknown)
            {
                if (bApply && ruleEx.Backup != null) // set the rule as it was mae by the 3rd party
                {
                    ruleEx.Assign(ruleEx.Backup);
                    FirewallManager.ApplyRule(prog, ruleEx);
                }
                else // just approve the curent state (rule is probably disabled)
                {
                    FirewallRule rule = FirewallManager.GetRule(ruleEx.guid);
                    ruleEx.Assign(rule);
                    ruleEx.SetApplied();
                }
            }

            OnRulesUpdatedEx(prog);
            return true;
        }

        private bool RestoreRule(Program prog, FirewallRuleEx ruleEx)
        {
            if (ruleEx.State == FirewallRuleEx.States.Changed || ruleEx.State == FirewallRuleEx.States.Deleted)
            {
                if (!FirewallManager.ApplyRule(prog, ruleEx))
                    return false;
            }
            else if (ruleEx.State == FirewallRuleEx.States.Unknown)
            {
                if(!FirewallManager.RemoveRule(ruleEx.guid))
                    return false;
            }
            ruleEx.Backup = null;

            OnRulesUpdatedEx(prog);
            return true;
        }

        public enum ApprovalMode
        {
            ApproveCurrent = 0,
            RestoreRules,
            ApproveChanges
        }

        public int SetRuleApproval(ApprovalMode Mode, string guid, ProgramID ProgID)
        {
            return mDispatcher.Invoke(new Func<int>(() => {
                int count = 0;
                if (guid != null)
                {
                    var prog = ProgramList.FindProgram(ProgID);
                    if (prog != null)
                    {
                        FirewallRuleEx ruleEx;
                        if (prog.Rules.TryGetValue(guid, out ruleEx))
                        {
                            if (Mode != ApprovalMode.RestoreRules ? ApproveRule(Mode == ApprovalMode.ApproveChanges, prog, ruleEx) : RestoreRule(prog, ruleEx))
                                count++;
                        }
                    }
                }
                else // all rules
                {
                    List<ProgramSet> progs = ProgramList.GetPrograms();
                    foreach (ProgramSet progSet in progs)
                    {
                        foreach (Program prog in progSet.Programs.Values)
                        {
                            foreach (FirewallRuleEx ruleEx in prog.Rules.Values.ToList())
                            {
                                if (Mode != ApprovalMode.RestoreRules ? ApproveRule(Mode == ApprovalMode.ApproveChanges, prog, ruleEx) : RestoreRule(prog, ruleEx))
                                    count++;
                            }
                        }
                    }
                }
                return count;
            }));
        }

        public bool BlockInternet(bool bBlock)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                bool ret = true;
                ProgramID id = ProgramID.NewID(ProgramID.Types.Global);
                Program prog = ProgramList.FindProgram(id, true);
                if (bBlock)
                {
                    /*FirewallRule ruleOut = new FirewallRule(prog.ID);
                    ruleOut.Name = FirewallManager.MakeRuleName(FirewallManager.BlockAllName, false, prog.Description);
                    ruleOut.Grouping = FirewallManager.RuleGroup;
                    ruleOut.Action = FirewallRule.Actions.Block;
                    ruleOut.Direction = FirewallRule.Directions.Outboun;
                    ruleOut.Enabled = true;

                    FirewallRule ruleIn = ruleOut.Duplicate();
                    ruleIn.Direction = FirewallRule.Directions.Inbound;

                    ret &= FirewallManager.ApplyRule(prog, ruleOut);
                    ret &= FirewallManager.ApplyRule(prog, ruleIn);*/

                    prog.ProgSet.config.NetAccess = ProgramConfig.AccessLevels.BlockAccess;
                    FirewallManager.ApplyRules(prog.ProgSet);
                }
                else
                {
                    prog.ProgSet.config.NetAccess = ProgramConfig.AccessLevels.CustomConfig;
                    FirewallManager.ClearRules(prog, false);
                    FirewallManager.EvaluateRules(prog.ProgSet, true);
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

        public bool ClearDnsLog()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                ProgramList.ClearDnsLog();
                return true;
            }));
        }

        public int CleanUpPrograms(bool ExtendedCleanup = false)
        {
            return mDispatcher.Invoke(new Func<int>(() => {
                return ProgramList.CleanUp(ExtendedCleanup);
            }));
        }

        public enum CleanupMode
        {
            RemoveExpired = 0,
            RemoveTemporary,
            RemoveDuplicates,
            RemoveDuplicatesAllow,
            RemoveDuplicatesBlock,
        }

        public int CleanUpRules(CleanupMode Mode)
        {
            return mDispatcher.Invoke(new Func<int>(() => {
                if(Mode == CleanupMode.RemoveExpired || Mode == CleanupMode.RemoveTemporary)
                    return CleanupFwRules(Mode == CleanupMode.RemoveTemporary);
                else
                    return CleanupFwDuplicates(Mode);
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

        public bool SetupDnsInspector(bool Enable)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {

                SettingsChanged = true;
                
                App.SetConfig("DnsInspector", "Enabled", Enable ? 1 : 0);

                if (DnsInspector != null && !Enable)
                {
                    DnsInspector.Dispose();
                    DnsInspector = null;
                }
                else if (DnsInspector == null && Enable)
                {
                    DnsInspector = new DnsInspector();
                }

                return true;
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

        public Dictionary<string, UwpFunc.AppInfo> GetAllAppPkgs(bool bWithPackageDetails = true)
        {
            return mDispatcher.Invoke(new Func<Dictionary<string, UwpFunc.AppInfo>>(() => {
                return FirewallManager.GetAllAppPkgs(bWithPackageDetails, true);
            }));
        }

        public string GetAppPkgRes(string str )
        {
            return mDispatcher.Invoke(new Func<string>(() => {
                return PkgMgr?.GetAppResourceStr(str);
            }));
        }

        /////////////////////////////////////////
        // Dns Proxy

        public bool ConfigureDNSProxy(bool Enable, bool? setLocal = null, string UpstreamDNS = null)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {

                SettingsChanged = true;

                App.SetConfig("DnsProxy", "Enabled", Enable == true ? 1 : 0);
                if (setLocal != null)
                    App.SetConfig("DnsProxy", "SetLocal", setLocal == true ? 1 : 0);
                if (UpstreamDNS != null)
                    App.SetConfig("DnsProxy", "UpstreamDNS", UpstreamDNS);

                if (Enable)
                {
                    if (DnsProxy == null)
                    {
                        DnsProxy = new DnsProxyServer();
                        if (!DnsProxy.Init())
                        {
                            DnsProxy = null;
                            return false;
                        }
                    }
                    else
                    {
                        DnsProxy.SetupUpstreamDNS();
                        DnsProxy.ConfigureSystemDNS();
                    }
                }
                else
                {
                    if (DnsProxy != null)
                    {
                        DnsProxy.Dispose();
                        DnsProxy = null;
                    }
                    DnsConfigurator.RestoreDNS();
                }
                return true;
            }));
        }


        // Querylog
        public List<DnsCacheMonitor.DnsCacheEntry> GetLoggedDnsQueries()
        {
            return mDispatcher.Invoke(new Func<List<DnsCacheMonitor.DnsCacheEntry>>(() => {
                return DnsProxy?.GetLoggedDnsQueries();
            }));
        }

        public bool ClearLoggedDnsQueries()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return DnsProxy?.ClearLoggedDnsQueries() == true;
            }));
        }

        // Whitelist/Blacklist
        public List<DomainFilter> GetDomainFilter(DnsBlockList.Lists List)
        {
            return mDispatcher.Invoke(new Func<List<DomainFilter>>(() => {
                return DnsProxy?.blockList?.GetDomainFilter(List);
            }));
        }

        public bool UpdateDomainFilter(DnsBlockList.Lists List, DomainFilter Filter)
        {
            return mDispatcher.Invoke(new Func<bool?>(() => {
                return DnsProxy?.blockList?.UpdateDomainFilter(List, Filter);
            })) == true;
        }

        public bool RemoveDomainFilter(DnsBlockList.Lists List, string Domain)
        {
            return mDispatcher.Invoke(new Func<bool?>(() => {
                return DnsProxy?.blockList?.RemoveDomainFilter(List, Domain);
            })) == true;
        }

        // Blocklist
        public List<DomainBlocklist> GetDomainBlocklists()
        {
            return mDispatcher.Invoke(new Func<List<DomainBlocklist>>(() => {
                return DnsProxy?.blockList?.GetDomainBlocklists();
            }));
        }

        public bool UpdateDomainBlocklist(DomainBlocklist Blocklist)
        {
            return mDispatcher.Invoke(new Func<bool?>(() => {
                return DnsProxy?.blockList?.UpdateDomainBlocklist(Blocklist);
            })) == true;
        }

        public bool RemoveDomainBlocklist(string Url)
        {
            return mDispatcher.Invoke(new Func<bool?>(() => {
                return DnsProxy?.blockList?.RemoveDomainBlocklist(Url);
            })) == true;
        }

        public bool RefreshDomainBlocklist(string Url = "") // empty means all
        {
            return mDispatcher.Invoke(new Func<bool?>(() => {
                return DnsProxy?.blockList?.RefreshDomainBlocklist(Url);
            })) == true;
        }

        /////////////////////////////////////////
        // Privacy tweaks

        public bool ApplyTweak(TweakList.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.TweakTools.ApplyTweak(tweak);
            }));
        }

        public bool TestTweak(TweakList.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.TweakTools.TestTweak(tweak);
            }));
        }

        public bool UndoTweak(TweakList.Tweak tweak)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return TweakEngine.TweakTools.UndoTweak(tweak);
            }));
        }

        /////////////////////////////////////////
        // Misc

        public bool Quit()
        {
            mDoQuit = true;
            return true;
        }
    }
}
