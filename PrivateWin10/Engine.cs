using PrivateWin10.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PrivateWin10
{
    public class Engine : IPCInterface
    {
        public ProgramList programs;
        public Firewall firewall;
        public AppManager appMgr;

        Dispatcher mDispatcher;
        ManualResetEvent mStarted = new ManualResetEvent(false);
        //ManualResetEvent mFinished = new ManualResetEvent(false);
        DispatcherTimer mTimer = new DispatcherTimer();

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
        }

        public void Stop()
        {
            if (mDispatcher == null)
                return;

            mDispatcher.InvokeShutdown();
            mDispatcher.Thread.Join(); // Note: this waits for thread finish
            mDispatcher = null;

            //mFinished.WaitOne();
        }


        public void Run()
        {
            Console.WriteLine("Entered Engine::Run");

            mDispatcher = Dispatcher.CurrentDispatcher;

            Console.WriteLine("Initializing program list...");
            programs = new ProgramList();
            if (!UwpFunc.IsWindows7OrLower)
            {
                Console.WriteLine("Initializing app manager...");
                appMgr = new AppManager();
            }
            Console.WriteLine("Initializing firewall...");
            firewall = new Firewall();

            Console.WriteLine("Loading program list...");
            programs.LoadList();

            Console.WriteLine("Loading firewall rules...");
            firewall.LoadRules(true);
            Console.WriteLine("Loading connection log...");
            if (App.GetConfigInt("Startup", "LoadLog", 1) != 0)
                firewall.LoadLogAsync();
            firewall.WatchConnections();

            Console.WriteLine("Setting up IPC host...");
            App.host = new PipeHost();
            App.host.Listen();

            mStarted.Set();

            Console.WriteLine("Starting engine timer...");

            mTimer.Tick += new EventHandler(OnTimer_Tick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 10*1000); // every 10 seconds
            mTimer.Start();

            Dispatcher.Run();

            Console.WriteLine("Saving program list...");
            programs.StoreList();


            Console.WriteLine("Shuttin down IPC host...");
            App.host.Close();

            //mFinished.Set();
        }

        private void OnTimer_Tick(object sender, EventArgs e)
        {
            firewall.CleanUpRules();
        }

        public Firewall.FilteringModes GetFilteringMode()
        {
            return mDispatcher.Invoke(new Func<Firewall.FilteringModes>(() => {
                return firewall.GetFilteringMode();
            }));
        }

        public bool SetFilteringMode(Firewall.FilteringModes Mode)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return firewall.SetFilteringMode(Mode);
            }));
        }

        public Firewall.Auditing GetAuditPol()
        {
            return mDispatcher.Invoke(new Func<Firewall.Auditing>(() => {
                return firewall.GetAuditPol();
            }));
        }

        public bool SetAuditPol(Firewall.Auditing audit)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return firewall.SetAuditPol(audit);
            }));
        }

        public List<Program> GetPrograms(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<List<Program>>(() => {
                return programs.GetPrograms(guids);
            }));
        }

        public Program GetProgram(ProgramList.ID id, bool canAdd = false)
        {
            return mDispatcher.Invoke(new Func<Program>(() => {
                return programs.GetProgram(id, canAdd);
            }));
        }

        public bool AddProgram(ProgramList.ID id, Guid guid)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return programs.AddProgram(id, guid);
            }));
        }

        public bool UpdateProgram(Guid guid, Program.Config config)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return programs.UpdateProgram(guid, config);
            }));
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return programs.MergePrograms(to, from);
            }));
        }

        public bool SplitPrograms(Guid from, ProgramList.ID id)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return programs.SplitPrograms(from, id);
            }));
        }

        public bool RemoveProgram(Guid guid, ProgramList.ID id = null)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return programs.RemoveProgram(guid, id);
            }));
        }

        public List<FirewallRule> GetRules(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<List<FirewallRule>>(() => {
                List<Program> progs = programs.GetPrograms(guids);
                List<FirewallRule> rules = new List<FirewallRule>();
                foreach (Program prog in progs)
                {
                    foreach (FirewallRule rule in prog.Rules.Values)
                        rules.Add(rule);
                }
                return rules;
            }));
        }

        public bool LoadRules()
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return firewall.LoadRules();
            }));
        }

        public bool UpdateRule(FirewallRule rule)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                if (rule.guid == Guid.Empty)
                {
                    if (rule.Direction == Firewall.Directions.Bidirectiona)
                    {
                        FirewallRule copy = rule.Clone();
                        copy.Direction = Firewall.Directions.Inbound;
                        if (!firewall.UpdateRule(copy))
                            return false;

                        rule.Direction = Firewall.Directions.Outboun;
                    }
                }
                return firewall.UpdateRule(rule);
            }));
        }

        public bool RemoveRule(FirewallRule rule)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return firewall.RemoveRule(rule);
            }));
        }

        public bool ClearLog(bool ClearSecLog)
        {
            return mDispatcher.Invoke(new Func<bool>(() => {
                return firewall.ClearLog(ClearSecLog);
            }));
        }

        public int CleanUpPrograms()
        {
            return mDispatcher.Invoke(new Func<int>(() => {
                return programs.CleanUp();
            }));
        }

        public List<Program.LogEntry> GetConnections(List<Guid> guids = null)
        {
            return mDispatcher.Invoke(new Func<List<Program.LogEntry>>(() => {
                List<Program> progs = programs.GetPrograms(guids);
                List<Program.LogEntry> entries = new List<Program.LogEntry>();
                foreach (Program prog in progs)
                {
                    foreach (Program.LogEntry entry in prog.Log)
                        entries.Add(entry);
                }
                return entries;
            }));
        }

        public List<AppManager.AppInfo> GetAllApps()
        {
            return mDispatcher.Invoke(new Func<List<AppManager.AppInfo>>(() => {
                return appMgr.GetAllApps();
            }));
        }

        /*public bool ApplyTweak(Tweak tweak)
        {
            return Tweaks.ApplyTweak(tweak);
        }

        public bool TestTweak(Tweak tweak)
        {
            return Tweaks.TestTweak(tweak);
        }

        public bool UndoTweak(Tweak tweak)
        {
            return Tweaks.UndoTweak(tweak);
        }*/

        public void LogActivity(Program.LogEntry entry, bool fromLog = false)
        {
            // Threading note: this function is called from other a service thread watching the security log

            mDispatcher.BeginInvoke(new Action(() => {

                // Threading note: here we are in the engine thread

                Program prog = App.engine.programs.GetProgram(entry.mID, true);

                prog.LogActivity(entry, fromLog);

                if (App.host != null && !fromLog) // dont norify activitis form the log
                    App.host.NotifyActivity(prog.guid, entry);
            }));
        }

        public void NotifyChange(Program prog)
        {
            if(prog != null)
                firewall.EvaluateRules(prog, false);
            if (App.host != null)
                App.host.NotifyChange(prog == null ? Guid.Empty : prog.guid);
        }
    }
}
