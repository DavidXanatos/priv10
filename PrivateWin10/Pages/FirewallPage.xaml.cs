using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;



namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for FirewallManager.xaml
    /// </summary>
    public partial class FirewallPage : UserControl, IUserPage
    {
        DispatcherTimer mTimer = new DispatcherTimer();

        enum Sorts
        {
            Unsorted = 0,
            Name,
            NameRev,
            LastActivity,
            DataRate,
            SocketCount,
            ModuleCount
        }
        Sorts SortBy = Sorts.Name;
        bool SortProgs = false;

        public class FilterPreset
        {
            public enum Recent: int
            {
                Not = 0,
                Blocked = 1,
                Allowed = 2,
                Active = 3
            }
            public Recent Recently = Recent.Not;

            public enum Socket
            {
                Not = 0,
                Any,
                Web,
                TCP,
                Client,
                Server,
                UDP,
                //Raw,
                None
            }
            public Socket Sockets = Socket.Not;

            public enum Rule
            {
                Not = 0,
                Any,
                Active,
                Disabled,
                None
            }
            public Rule Rules = Rule.Not;

            public ProgramSet.Config.AccessLevels Access = ProgramSet.Config.AccessLevels.AnyValue;

            public List<string> Types = new List<string>();

            public List<string> Categories = new List<string>();

            public string Filter = "";
        }

        Dictionary<string, FilterPreset> AllFilters = new Dictionary<string, FilterPreset>();

        FilterPreset CurFilter = new FilterPreset();

        private CategoryModel CatModel;

        int SuspendChange = 0;

        public FirewallPage()
        {
            InitializeComponent();

            ruleList.SetPage(this);
            consList.firewallPage = this;
            sockList.firewallPage = this;
            dnsList.firewallPage = this;


            this.rbbFilter.Header = Translate.fmt("lbl_view_filter");

            this.rbbPresets.Header = Translate.fmt("filter_presets");

            this.rbbActivity.Header = Translate.fmt("filter_activity");

            this.lblRecent.Label = Translate.fmt("filter_recent");
            WpfFunc.CmbAdd(cmbRecent, Translate.fmt("filter_recent_not"), FilterPreset.Recent.Not);
            WpfFunc.CmbAdd(cmbRecent, Translate.fmt("filter_recent_active"), FilterPreset.Recent.Active);
            WpfFunc.CmbAdd(cmbRecent, Translate.fmt("filter_recent_blocked"), FilterPreset.Recent.Blocked);
            WpfFunc.CmbAdd(cmbRecent, Translate.fmt("filter_recent_allowed"), FilterPreset.Recent.Allowed);

            this.lblSockets.Label = Translate.fmt("filter_sockets");
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_not"), FilterPreset.Socket.Not);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_any"), FilterPreset.Socket.Any);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_web"), FilterPreset.Socket.Web);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_tcp"), FilterPreset.Socket.TCP);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_client"), FilterPreset.Socket.Client);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_server"), FilterPreset.Socket.Server);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_udp"), FilterPreset.Socket.UDP);
            //WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_raw"), FilterPreset.Socket.Raw);
            WpfFunc.CmbAdd(cmbSockets, Translate.fmt("filter_sockets_none"), FilterPreset.Socket.None);

            this.lblRules.Label = Translate.fmt("filter_rules");
            WpfFunc.CmbAdd(cmbRules, Translate.fmt("filter_rules_not"), FilterPreset.Rule.Not);
            WpfFunc.CmbAdd(cmbRules, Translate.fmt("filter_rules_any"), FilterPreset.Rule.Any);
            WpfFunc.CmbAdd(cmbRules, Translate.fmt("filter_rules_enabled"), FilterPreset.Rule.Active);
            WpfFunc.CmbAdd(cmbRules, Translate.fmt("filter_rules_disabled"), FilterPreset.Rule.Disabled);
            WpfFunc.CmbAdd(cmbRules, Translate.fmt("filter_rules_none"), FilterPreset.Rule.None);

            this.lblAccess.Label = Translate.fmt("filter_access");
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_any"), ProgramSet.Config.AccessLevels.AnyValue);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_none"), ProgramSet.Config.AccessLevels.Unconfigured);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_allow"), ProgramSet.Config.AccessLevels.FullAccess);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_edit"), ProgramSet.Config.AccessLevels.CustomConfig);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_lan"), ProgramSet.Config.AccessLevels.LocalOnly);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_block"), ProgramSet.Config.AccessLevels.BlockAccess);
            WpfFunc.CmbAdd(cmbAccess, Translate.fmt("acl_warn"), ProgramSet.Config.AccessLevels.WarningState);
            foreach (RibbonGalleryItem item in (cmbAccess.Items[0] as RibbonGalleryCategory).Items)
                item.Background = ProgramControl.GetAccessColor((ProgramSet.Config.AccessLevels)item.Tag);

            this.rbbFilters.Header = Translate.fmt("filter_program");
            this.lblTypes.Text = Translate.fmt("filter_types");
            this.chkProgs.Label = Translate.fmt("filter_programs");
            this.chkApps.Label = Translate.fmt("filter_apps");
            this.chkSys.Label = Translate.fmt("filter_system");

            this.rbbCaegories.Header = Translate.fmt("filter_category");



            //this.rbbView.Header = Translate.fmt("lbl_view_options");
            //this.rbbOptions.Header = Translate.fmt("lbl_view_options");

            //this.rbbRules.Header = Translate.fmt("lbl_rules_and");
            //this.btnReload.Label = Translate.fmt("btn_reload");
            this.chkAll.Label = Translate.fmt("chk_all");

            this.rbbProgs.Header = Translate.fmt("lbl_programs");
            this.btnAdd.Label = Translate.fmt("btn_add_prog");
            this.btnMerge.Label = Translate.fmt("btn_merge_progs");
            this.btnRemove.Label = Translate.fmt("btn_del_progs");
            this.btnCleanup.Label = Translate.fmt("btn_cleanup_list");

            //this.rbbSort.Header = Translate.fmt("lbl_sort_and");
            this.lblSort.Content = Translate.fmt("lbl_sort");
            //this.chkNoLocal.Content = Translate.fmt("chk_ignore_local");
            //this.chkNoLan.Content = Translate.fmt("chk_ignore_lan");

            //this.rbbRules // todo: xxx

            double progColHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallProgsWidth", "0.0"));
            if (progColHeight > 0.0)
                progsCol.Width = new GridLength(progColHeight, GridUnitType.Pixel);

            double rulesRowHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallRulesHeight", "0.0"));
            if (rulesRowHeight > 0.0)
                rulesRow.Height = new GridLength(rulesRowHeight, GridUnitType.Pixel);

            SuspendChange++;


            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_no"), Sorts.Unsorted);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_name"), Sorts.Name);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_rname"), Sorts.NameRev);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_act"), Sorts.LastActivity);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_rate"), Sorts.DataRate);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_socks"), Sorts.SocketCount);
            WpfFunc.CmbAdd(cmbSort, Translate.fmt("sort_count"), Sorts.ModuleCount);
            WpfFunc.CmbSelect(cmbSort, ((Sorts)App.GetConfigInt("GUI", "SortList", 0)).ToString());

            this.chkNoLocal.IsChecked = App.GetConfigInt("GUI", "ActNoLocal", 0) == 1;
            this.chkNoLan.IsChecked = App.GetConfigInt("GUI", "ActNoLan", 0) == 1;


            CatModel = new CategoryModel();

            LoadCategorys();

            CatModel.Categorys.CollectionChanged += Categorys_CollectionChanged;

            if(UwpFunc.IsWindows7OrLower)
                chkApps.IsEnabled = false;

            var Filter = LoadPreset();
            ApplyPreset(Filter);

            foreach (var section in App.IniEnumSections())
            {
                if (section.IndexOf("Preset_") != 0)
                    continue;
                string Name = section.Substring(7);
                WpfFunc.CmbAdd(cmdPreset, Name, Name);
            }

            SuspendChange--;

            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 250); // 4 times a second
            mTimer.Start();
            OnTimerTick(null, null);

            App.client.ActivityNotification += OnActivity;
            App.client.ChangeNotification += OnChange;

            this.processScroll.PreviewKeyDown += process_KeyEventHandler;

            CheckPrograms();
        }

        bool IsHidden = true;

        public void OnShow()
        {
            IsHidden = false;

            FullUpdate = false;
            UpdatesProgs.Clear();

            var pol = App.client.GetAuditPolicy();
            logTab.IsEnabled = pol != FirewallMonitor.Auditing.Off;
            consList.IsEnabled = logTab.IsEnabled;
            //consList.chkAllowed.IsEnabled = (pol & FirewallMonitor.Auditing.Allowed) != 0;
            //consList.chkBlocked.IsEnabled = (pol & FirewallMonitor.Auditing.Blocked) != 0;

            UpdateProgramList();
        }

        public void OnHide()
        {
            IsHidden = true;
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "FirewallProgsWidth", ((int)progsCol.ActualWidth).ToString());
            App.SetConfig("GUI", "FirewallRulesHeight", ((int)rulesRow.ActualHeight).ToString());

            ruleList.OnClose();
            consList.OnClose();
            sockList.OnClose();
            dnsList.OnClose();

            if (notificationWnd != null)
                notificationWnd.Close();
        }

        private void Categorys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LoadCategorys();
        }

        private void AddCatItem(string cat, object tag)
        {
            var chk = new CheckBox()
            {
                IsChecked = false,
                IsThreeState = true,
                //MaxWidth = 110,
                Content = cat,
                Tag = tag
            };
            chk.Unchecked += new RoutedEventHandler(OnFilter_Changed);
            chk.Checked += new RoutedEventHandler(OnFilter_Changed);
            chk.Indeterminate += new RoutedEventHandler(OnFilter_Changed);

            this.catFilter.Items.Add(chk);
        }

        private void LoadCategorys()
        {
            SuspendChange++;

            this.catFilter.Items.Clear();
            foreach (CategoryModel.Category cat in CatModel.Categorys)
            {
                if (cat.SpecialCat == CategoryModel.Category.Special.No)
                    AddCatItem(cat.Content.ToString(), cat.Content.ToString());
            }
            AddCatItem(Translate.fmt("cat_uncat"), "");
            
            SuspendChange--;
        }

        public ProgramSet GetProgSet(Guid guid, ProgramID progID, out ProgramControl item)
        {
            ProgramSet prog = null;
            if (mPrograms.TryGetValue(guid, out item))
                prog = item.Program;
            else if (progID != null)
                prog = App.client.GetProgram(progID);
            else
            {
                List<ProgramSet> progs = App.client.GetPrograms(new List<Guid>() { guid });
                if (progs.Count() != 0)
                    prog = progs[0];
            }
            return prog;
        }

        void OnActivity(object sender, Engine.FwEventArgs args)
        {
            ProgramControl item = null;
            ProgramSet prog = GetProgSet(args.guid, args.progID, out item);
            if (prog == null)
                return;

            Program program = null;
            prog.Programs.TryGetValue(args.progID, out program);

            if (args.entry.State == Program.LogEntry.States.UnRuled && args.entry.FwEvent.Action == FirewallRule.Actions.Block)
            {
                bool? Notify = prog.config.GetNotify();
                if (Notify == true || (App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0 && Notify != false))
                    ShowNotification(prog, args);
            }

            if (IsHidden)
                return;

            if (item == null)
            {
                if (DoFilter(CurFilter, prog))
                    return;

                item = AddProgramItem(prog);

                args.update = false;
            }

            if (!args.update) // ignore update events
            {
                //Note: windows firewall doesn't block localhost acces so we ignore it
                /*if (args.entry.State == Program.LogEntry.States.RuleError
                  && args.entry.FwEvent.Action == FirewallRule.Actions.Allow
                  && !NetFunc.IsLocalHost(args.entry.FwEvent.RemoteAddress))
                    item.SetError(true);*/

                if (program != null)
                {
                    switch (args.entry.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow: program.countAllowed++; break;
                        case FirewallRule.Actions.Block: program.countBlocked++; break; 
                    }
                }

                if ((chkNoLocal.IsChecked != true || (!NetFunc.IsLocalHost(args.entry.FwEvent.RemoteAddress) && !NetFunc.IsMultiCast(args.entry.FwEvent.RemoteAddress)))
                 && (chkNoLan.IsChecked != true || !FirewallRule.MatchAddress(args.entry.FwEvent.RemoteAddress, FirewallRule.AddrKeywordLocalSubnet)))
                {
                    switch (args.entry.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow: item.Flash(Colors.LightGreen); break;
                        case FirewallRule.Actions.Block: item.Flash(Colors.LightPink); break;
                    }

                    if (program != null)
                    {
                        switch (args.entry.FwEvent.Action)
                        {
                            case FirewallRule.Actions.Allow:
                                program.lastAllowed = DateTime.Now; 
                                break;
                            case FirewallRule.Actions.Block:
                                program.lastBlocked = DateTime.Now;
                                break;
                        }
                        if (SortBy == Sorts.LastActivity)
                            SortProgs = true;
                    }
                }

                item.DoUpdate();
            }

            // from here on only col log update:

            if (!mCurPrograms.Contains(item) && chkAll.IsChecked != true)
                return;

            consList.AddEntry(prog, program, args);

            //if (mSortBy == Sorts.LastActivity)
            //    OnChange(sender, new ProgramList.ChangeArgs() { guid = args.guid });
        }

        
        private NotificationWnd notificationWnd = null;

        private void ShowNotification(ProgramSet prog, Engine.FwEventArgs args)
        {
            if (notificationWnd == null && !args.update) // dont show on update events
            {
                notificationWnd = new NotificationWnd();
                notificationWnd.Closing += NotificationClosing;
                notificationWnd.Show();
            }
            notificationWnd.Add(prog, args);
        }

        void NotificationClosing(object sender, CancelEventArgs e)
        {
            notificationWnd = null;
        }

        HashSet<Guid> UpdatesProgs = new HashSet<Guid>();
        bool FullUpdate = false;
        bool RuleUpdate = false;

        void OnChange(object sender, Engine.ChangeArgs args)
        {
            if (args.guid == Guid.Empty)
                FullUpdate = true;
            else if (args.type == Engine.ChangeArgs.Types.ProgSet)
                UpdatesProgs.Add(args.guid);
            else
                RuleUpdate = true;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (IsHidden)
                return;
            if (App.mMainWnd.WindowState == WindowState.Minimized)
                return;
            if (App.mMainWnd.Visibility != Visibility.Visible)
                return;

            if (FullUpdate)
            {
                FullUpdate = false;
                UpdateProgramList();
            }
            else if (UpdatesProgs.Count > 0)
            {
                List<ProgramSet> progs = App.client.GetPrograms(UpdatesProgs.ToList());

                foreach (ProgramSet prog in progs)
                {
                    ProgramControl item;
                    if (!mPrograms.TryGetValue(prog.guid, out item))
                        item = AddProgramItem(prog);

                    item.Program = prog;
                    item.DoUpdate();
                }
            }

            if (RuleUpdate)
            {
                RuleUpdate = false;
                ruleList.UpdateRules();
            }

            if (UpdatesProgs.Count > 0 || SortProgs)
            {
                SortProgs = false;
                SortAndFitlerProgList();
            }

            UpdatesProgs.Clear();

            if (mCurPrograms.Count > 0 || chkAll.IsChecked == true)
            {
                sockList.UpdateSockets();
                dnsList.UpdateDnsLog(); // todo: update this liek the connection log i.e. incrementally
            }
        }

        SortedDictionary<Guid, ProgramControl> mPrograms = new SortedDictionary<Guid, ProgramControl>();

        private void UpdateProgramList()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Dictionary<Guid, ProgramControl> OldProcesses = new Dictionary<Guid, ProgramControl>(mPrograms);

            List<ProgramSet> progs = App.client.GetPrograms();

            foreach (ProgramSet prog in progs)
            {
                ProgramControl item;
                if (mPrograms.TryGetValue(prog.guid, out item))
                {
                    item.Program = prog;
                    item.DoUpdate();
                    OldProcesses.Remove(prog.guid);
                }
                else
                    item = AddProgramItem(prog);
            }

            foreach (Guid guid in OldProcesses.Keys)
            {
                mPrograms.Remove(guid);
                ProgramControl item;
                if (OldProcesses.TryGetValue(guid, out item))
                    this.processGrid.Children.Remove(item);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("UpdateProgramList took: " + elapsedMs + "ms");

            SortAndFitlerProgList();
        }

        ProgramControl AddProgramItem(ProgramSet prog)
        {
            ProgramControl item = new ProgramControl(prog, CatModel);
            mPrograms.Add(prog.guid, item);
            //item.Tag = process;
            item.VerticalAlignment = VerticalAlignment.Top;
            item.HorizontalAlignment = HorizontalAlignment.Stretch;
            item.Margin = new Thickness(1, 1, 1, 1);
            //item.MouseDown += new MouseButtonEventHandler(process_Click);
            item.Click += new RoutedEventHandler(process_Click);

            this.processGrid.Children.Add(item);

            return item;
        }

        static public bool DoTest(string Filter, string Name, List<ProgramID> IDs)
        {
            bool bNot = false;
            if (Filter.Length > 1 && Filter.Substring(0, 1) == "-")
            {
                bNot = true;
                Filter = Filter.Substring(1);
            }

            bool bPath = false;
            if (Filter.Length > 0 && Filter.Substring(0, 1) == "~")
            {
                bPath = true;
                Filter = Filter.Substring(1);
            }

            if (TextHelpers.CompareWildcard(Name, Filter) != bNot)
                return false;
            if (bPath)
            {
                foreach (ProgramID _id in IDs)
                {
                    if (_id.Path.Length > 0 && TextHelpers.CompareWildcard(_id.Path, Filter) != bNot)
                        return false;

                    if (_id.Type == ProgramID.Types.Service)
                    {
                        if (TextHelpers.CompareWildcard(_id.GetServiceId(), Filter) != bNot)
                            return false;
                        if (TextHelpers.CompareWildcard(_id.GetServiceName(), Filter) != bNot)
                            return false;
                    }
                    else if (_id.Type == ProgramID.Types.App)
                    {
                        if (TextHelpers.CompareWildcard(_id.GetPackageName(), Filter) != bNot)
                            return false;
                    }
                }
            }
            return true;
        }

        static public bool DoFilter(string Filter, string Name, List<ProgramID> IDs)
        {
            if (Filter.Length == 0)
                return false;

            foreach (string condition in TextHelpers.TokenizeStr(Filter))
            {
                if (DoTest(condition, Name, IDs))
                    return true;
            }
            return false;
        }

        private void SortAndFitlerProgList()
        {
            List<ProgramControl> OrderList = mPrograms.Values.ToList();

            int DoSort(ProgramControl l, ProgramControl r)
            {
                switch (SortBy)
                {
                    case Sorts.Name: return l.Program.config.Name.CompareTo(r.Program.config.Name);
                    case Sorts.NameRev: return r.Program.config.Name.CompareTo(l.Program.config.Name);
                    case Sorts.LastActivity: return r.Program.GetLastActivity().CompareTo(l.Program.GetLastActivity());
                    case Sorts.DataRate: return r.Program.GetDataRate().CompareTo(l.Program.GetDataRate());
                    case Sorts.SocketCount: return r.Program.GetSocketCount().CompareTo(l.Program.GetSocketCount());
                    case Sorts.ModuleCount: return r.Program.Programs.Count.CompareTo(l.Program.Programs.Count);
                }
                return 0;
            }

            if (SortBy != Sorts.Unsorted)
                OrderList.Sort(DoSort);

            for (int i = 0; i < OrderList.Count; i++)
            {
                if (i >= this.processGrid.RowDefinitions.Count)
                    this.processGrid.RowDefinitions.Add(new RowDefinition());
                RowDefinition row = this.processGrid.RowDefinitions[i];
                //ProcessControl item = tmp.Item2;
                ProgramControl item = OrderList[i];
                if (row.Height.Value != item.Height)
                    row.Height = GridLength.Auto; //new GridLength(item.Height + 2);
                Grid.SetRow(item, i);

                item.Visibility = DoFilter(CurFilter, item.Program) ? Visibility.Collapsed : Visibility.Visible;
            }

            while (OrderList.Count < this.processGrid.RowDefinitions.Count)
                this.processGrid.RowDefinitions.RemoveAt(OrderList.Count);
        }

        public static bool MultiFilter(List<string> filters, Func<string, bool> filterFx)
        {
            if (filters.Count == 0)
                return false; // empty list jut  pass

            int Count = 0;
            foreach (string _filter in filters)
            {
                string filter = _filter;

                bool bNot = false;
                if (filter.Length > 0 && filter.Substring(0, 1) == "-")
                {
                    bNot = true;
                    filter = filter.Substring(1);
                }
                else
                    Count++;

                if (filterFx(filter))
                    return !bNot ? false : true;
            }

            return Count > 0; // no match fail
        }

        private bool DoFilter(FilterPreset Filter, ProgramSet prog)
        {
            //Activity
            if (Filter.Recently != FilterPreset.Recent.Not)
            {
                DateTime lastActivity = prog.GetLastActivity((Filter.Recently & FilterPreset.Recent.Allowed) != 0, (Filter.Recently & FilterPreset.Recent.Blocked) != 0);
                if (lastActivity < DateTime.Now.AddMinutes(-App.GetConfigInt("GUI", "RecentTreshold", 15)))
                    return true;
            }

            //Sockets
            switch (Filter.Sockets)
            {
                case FilterPreset.Socket.Any:
                    if (prog.SocketsTcp == 0 && prog.SocketsUdp == 0)
                        return true;
                    break;
                case FilterPreset.Socket.TCP:
                    if (prog.SocketsTcp == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Client:
                    if (prog.SocketsTcp - prog.SocketsSrv == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Server:
                    if (prog.SocketsSrv == 0)
                        return true;
                    break;
                case FilterPreset.Socket.UDP:
                    if (prog.SocketsUdp == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Web:
                    if (prog.SocketsWeb == 0)
                        return true;
                    break;
                case FilterPreset.Socket.None:
                    if (prog.SocketsTcp != 0 || prog.SocketsUdp != 0)
                        return true;
                    break;
            }


            //Rules
            switch (Filter.Rules)
            {
                case FilterPreset.Rule.Any:
                    if (prog.EnabledRules == 0 && prog.DisabledRules == 0)
                        return true;
                    break;
                case FilterPreset.Rule.Active:
                    if (prog.EnabledRules == 0)
                        return true;
                    break;
                case FilterPreset.Rule.Disabled:
                    if (prog.DisabledRules == 0)
                        return true;
                    break;
                case FilterPreset.Rule.None:
                    if (prog.EnabledRules != 0 || prog.DisabledRules != 0)
                        return true;
                    break;
            }

            //Access
            if (Filter.Access != ProgramSet.Config.AccessLevels.AnyValue)
            {
                if (Filter.Access == ProgramSet.Config.AccessLevels.WarningState)
                {
                    if ((prog.config.NetAccess == ProgramSet.Config.AccessLevels.Unconfigured || prog.config.CurAccess == prog.config.NetAccess) && prog.ChgedRules == 0)
                        return true;
                }
                else if (Filter.Access != prog.config.CurAccess)
                    return true;
            }

            //Types
            if (MultiFilter(Filter.Types, (string Type) => {
                if (Type == "Programs")
                    return prog.Programs.Keys.FirstOrDefault(id => id.Type == ProgramID.Types.Program) != null;
                else if (Type == "System")
                    return prog.Programs.Keys.FirstOrDefault(id => (id.Type == ProgramID.Types.System || id.Type == ProgramID.Types.Global || id.Type == ProgramID.Types.Service)) != null;
                else if (Type == "Apps")
                    return prog.Programs.Keys.FirstOrDefault(id => id.Type == ProgramID.Types.App) != null;
                return false;
            }))
                return true;

            //Categories
            if (MultiFilter(Filter.Categories, (string Category) => {
                return Category.Length == 0 // Uncategorized
                ? (prog.config.Category == null || prog.config.Category.Length == 0)
                : (prog.config.Category != null && Category.Equals(prog.config.Category, StringComparison.OrdinalIgnoreCase));
            }))
                return true;

            //Filter
            if (DoFilter(Filter.Filter, prog.config.Name, prog.Programs.Keys.ToList()))
                return true;

            return false;
        }


        List<ProgramControl> mCurPrograms = new List<ProgramControl>();

        void process_Click(object sender, RoutedEventArgs e)
        {
            ProgramControl curProcess = sender as ProgramControl;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (mCurPrograms.Contains(curProcess))
                {
                    curProcess.SetFocus(false);
                    mCurPrograms.Remove(curProcess);
                }
                else
                {
                    curProcess.SetFocus(true);
                    mCurPrograms.Add(curProcess);
                }
            }
            else
            {
                foreach (ProgramControl curProg in mCurPrograms)
                    curProg.SetFocus(false);
                mCurPrograms.Clear();
                mCurPrograms.Add(curProcess);
                curProcess.SetFocus(true);
            }

            ruleList.UpdateRules(true);
            consList.UpdateConnections(true);
            sockList.UpdateSockets(true);
            dnsList.UpdateDnsLog(true);
            CheckPrograms();
        }

        public List<Guid> GetCurGuids(string filter = null)
        {
            List<Guid> guids = new List<Guid>();
            if (chkAll.IsChecked != true)
            {
                foreach (ProgramControl ctrl in mCurPrograms)
                {
                    if (filter != null && DoFilter(filter, ctrl.Program.config.Name, ctrl.Program.Programs.Keys.ToList()))
                        continue;

                    guids.Add(ctrl.Program.guid);
                }
            }
            return guids;
        }

        private List<Program> GetProgs(bool ignoreAll = false)
        {
            List<Program> progs = new List<Program>();

            void AddIDs(SortedDictionary<ProgramID, Program> programs)
            {
                foreach (Program prog in programs.Values)
                    progs.Add(prog);
            }
            if (chkAll.IsChecked == true && ignoreAll == false)
            {
                HashSet<ProgramSet> temp = new HashSet<ProgramSet>();
                foreach (ProgramSet entry in App.client.GetPrograms())
                {
                    if (temp.Add(entry))
                        AddIDs(entry.Programs);
                }
            }
            else
            {
                foreach (ProgramControl ctrl in mCurPrograms)
                {
                    AddIDs(ctrl.Program.Programs);
                }
            }

            return progs;
        }
        
        void process_KeyEventHandler(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Up || e.Key == Key.Down) /*&& !((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)*/)
            {
                ProgramControl curProcess = null;
                if (mCurPrograms.Count > 0)
                {
                    foreach (ProgramControl curProg in mCurPrograms)
                        curProg.SetFocus(false);
                    curProcess = mCurPrograms[mCurPrograms.Count - 1];
                    mCurPrograms.Clear();
                }

                e.Handled = true;
                int curRow = Grid.GetRow(curProcess);
                if (e.Key == Key.Up)
                {
                    while (curRow > 0)
                    {
                        curRow--;
                        ProgramControl curProg = this.processGrid.Children.Cast<ProgramControl>().First((c) => Grid.GetRow(c) == curRow);
                        if (curProg.Visibility == Visibility.Visible)
                        {
                            curProcess = curProg;
                            this.processScroll.ScrollToVerticalOffset(this.processScroll.VerticalOffset - (curProcess.ActualHeight + 2));
                            break;
                        }
                    }
                }
                else if (e.Key == Key.Down)
                {
                    while (curRow < this.processGrid.Children.Count - 1)
                    {
                        curRow++;
                        ProgramControl curProg = this.processGrid.Children.Cast<ProgramControl>().First((c) => Grid.GetRow(c) == curRow);
                        if (curProg.Visibility == Visibility.Visible)
                        {
                            curProcess = curProg;
                            this.processScroll.ScrollToVerticalOffset(this.processScroll.VerticalOffset + (curProcess.ActualHeight + 2));
                            break;
                        }
                    }
                }

                if (curProcess == null)
                    return;

                curProcess.SetFocus(true);
                mCurPrograms.Add(curProcess);

                ruleList.UpdateRules(true);
                consList.UpdateConnections(true);
                sockList.UpdateSockets(true);
                dnsList.UpdateDnsLog(true);
                CheckPrograms();
            }
        }

        
        //private void cmbSort_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //mSortBy = (Sorts)((sender as RibbonGallery).SelectedItem as RibbonGalleryItem).Tag;
            SortBy = (Sorts)((sender as ComboBox).SelectedItem as ContentControl).Tag;
            if (SuspendChange != 0)
                return;
            App.SetConfig("GUI", "SortList", (int)SortBy);
            SortProgs = true;
        }

        private void chkAll_Click(object sender, RoutedEventArgs e)
        {
            ruleList.UpdateRules(true);
            consList.UpdateConnections(true);
            sockList.UpdateSockets(true);
            dnsList.UpdateDnsLog(true);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProgramWnd progWnd = new ProgramWnd(null);

            if (progWnd.ShowDialog() != true)
                return;

            if (App.client.AddProgram(progWnd.ID, Guid.Empty))
                return;

            MessageBox.Show(Translate.fmt("msg_already_exist"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            if (mCurPrograms.Count < 2)
                return;

            foreach (ProgramControl curProgram in mCurPrograms)
            {
                if (curProgram.Program.Programs.First().Key.Type == ProgramID.Types.System || curProgram.Program.Programs.First().Key.Type == ProgramID.Types.Global)
                {
                    MessageBox.Show(Translate.fmt("msg_no_sys_merge"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            for (ProgramControl firstProg = mCurPrograms[0]; mCurPrograms.Count > 1; mCurPrograms.RemoveAt(1))
            {
                ProgramSet curProgram = mCurPrograms[1].Program;
                App.client.MergePrograms(firstProg.Program.guid, curProgram.guid);
            }
            
            //firstProg.Process.Log.Sort();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_progs"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (ProgramControl curProgram in mCurPrograms)
                App.client.RemoveProgram(curProgram.Program.guid);
        }

        private void btnCleanup_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clean_progs"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            //foreach (ProgramControl item in mPrograms.Values)
            //    item.SetError(false);

            int Count = App.client.CleanUpPrograms();

            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.mName, MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void btnCleanupEx_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clean_progs_ex"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            //foreach (ProgramControl item in mPrograms.Values)
            //    item.SetError(false);

            int Count = App.client.CleanUpPrograms(true);

            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.mName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            this.consList.ClearLog();
        }


        public void ShowRuleWindow(FirewallRule rule)
        {
            // if no rule was given it means create a new rule for one of the current programs
            if (rule == null)
            {
                rule = new FirewallRule() { guid = null, Profile = (int)FirewallRule.Profiles.All, Interface = (int)FirewallRule.Interfaces.All, Enabled = true };
                rule.Grouping = FirewallManager.RuleGroup;
                rule.Direction = FirewallRule.Directions.Bidirectiona;

                rule.Name = Translate.fmt("custom_rule", mCurPrograms.Count != 0 ? mCurPrograms[0].Program.config.Name : "");
                if (mCurPrograms.Count == 1)
                    rule.ProgID = mCurPrograms[0].Program.Programs.First().Key;
            }

            List<Program> progs = GetProgs();

            for (; ; )
            {
                RuleWindow ruleWnd = new RuleWindow(progs, rule);
                if (ruleWnd.ShowDialog() != true)
                    return;

                if (App.client.UpdateRule(rule)) // Note: this also adds
                    break;

                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void CheckPrograms()
        {
            bool GlobalSelected = false;
            int SelectedCount = 0;
            foreach (ProgramControl ctrl in mCurPrograms)
            {
                if (ctrl.Program.Programs.First().Key.Type == ProgramID.Types.Global || ctrl.Program.Programs.First().Key.Type == ProgramID.Types.System)
                    GlobalSelected = true;
                else
                    SelectedCount++;
            }

            btnMerge.IsEnabled = (SelectedCount >= 2 && !GlobalSelected);
            btnRemove.IsEnabled = (SelectedCount >= 1 && !GlobalSelected);
        }

        private void Ribbon_Loaded(object sender, RoutedEventArgs e)
        {
            try // when this page does not have focus this gets called but the required child is missing
            {
                // hide quick access bar: https://stackoverflow.com/questions/6265392/wpf-ribbon-hide-quick-access-toolbar
                Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
                if (child != null)
                    child.RowDefinitions[0].Height = new GridLength(0);

                txtPreset.Text = Translate.fmt("lbl_last_preset");
            }
            catch { }
        }

        private void ChkNoLocal_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
        }

        private void ChkNoLan_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLan", this.chkNoLan.IsChecked == true ? 1 : 0);
        }

        private void BtnNoFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset(new FilterPreset());
            App.IniDeleteSection("DefaultPreset");
            btnNoFilter.IsEnabled = false;
        }

        private void ApplyPreset(FilterPreset Filter)
        {
            CurFilter = Filter;

            SuspendChange++;

            //Recently
            WpfFunc.CmbSelect(this.cmbRecent, CurFilter.Recently.ToString());

            //Sockets
            WpfFunc.CmbSelect(this.cmbSockets, CurFilter.Sockets.ToString());

            //Rules
            WpfFunc.CmbSelect(this.cmbRules, CurFilter.Rules.ToString());

            //Access
            WpfFunc.CmbSelect(this.cmbAccess, CurFilter.Access.ToString());

            //Types
            chkProgs.IsChecked = CurFilter.Types.FirstOrDefault(cur => cur.Equals("Programs")) != null ? true : false;
            chkApps.IsChecked = CurFilter.Types.FirstOrDefault(cur => cur.Equals("Apps")) != null ? true : false;
            chkSys.IsChecked = CurFilter.Types.FirstOrDefault(cur => cur.Equals("System")) != null ? true : false;

            //Categories
            foreach (var item in this.catFilter.Items)
            {
                CheckBox box = item as CheckBox;
                string cat = box.Tag as string;

                if (CurFilter.Categories.FirstOrDefault(cur => cur.Equals(cat)) != null)
                    box.IsChecked = true;
                else if (CurFilter.Categories.FirstOrDefault(cur => cur.Equals("-" + cat)) != null)
                    box.IsChecked = null;
                else
                    box.IsChecked = false;
            }

            // Filter
            this.txtFilter.Text = CurFilter.Filter;

            SuspendChange--;

            SortProgs = true;
            //FullUpdate = true;
        }

        private void OnFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (SuspendChange != 0)
                return;

            //Recently
            if(this.cmbRecent.SelectedItem != null)
                CurFilter.Recently = (FilterPreset.Recent)(this.cmbRecent.SelectedItem as RibbonGalleryItem).Tag;

            //Sockets
            if (this.cmbSockets.SelectedItem != null)
                CurFilter.Sockets = (FilterPreset.Socket)(this.cmbSockets.SelectedItem as RibbonGalleryItem).Tag;

            //Rules
            if (this.cmbRules.SelectedItem != null)
                CurFilter.Rules = (FilterPreset.Rule)(this.cmbRules.SelectedItem as RibbonGalleryItem).Tag;

            //Access
            if (this.cmbAccess.SelectedItem != null)
            {
                this.lblAccess.Background = (this.cmbAccess.SelectedItem as RibbonGalleryItem).Background;
                CurFilter.Access = (ProgramSet.Config.AccessLevels)(this.cmbAccess.SelectedItem as RibbonGalleryItem).Tag;
            }

            //Types
            CurFilter.Types.Clear();
            if (chkProgs.IsChecked != false)
                CurFilter.Types.Add(/*chkProgs.IsChecked != true ? "-Programs" :*/ "Programs");
            if (chkApps.IsChecked != false)
                CurFilter.Types.Add(/*chkProgs.IsChecked != true ? "-Apps" :*/ "Apps");
            if (chkSys.IsChecked != false)
                CurFilter.Types.Add(/*chkProgs.IsChecked != true ? "-System" :*/ "System");

            //Categories
            CurFilter.Categories.Clear();
            foreach (var item in this.catFilter.Items)
            {
                CheckBox box = item as CheckBox;
                if (box.IsChecked == false)
                    continue;
                string cat = box.Tag as string;
                if (box.IsChecked != true)
                    cat = "-" + cat;
                CurFilter.Categories.Add(cat);
            }

            // Filter
            CurFilter.Filter = this.txtFilter.Text;

            SavePreset(CurFilter);

            SortProgs = true;
            //FullUpdate = true;

            btnNoFilter.IsEnabled = true;
        }

        private string MakePresetSection(string Name)
        {
            return Name != null ? "Preset_" + Name.Replace(" ", "_") : "DefaultPreset";
        }

        private void SavePreset(FilterPreset Filter, string Name = null)
        {
            string Section = MakePresetSection(Name);

            App.SetConfig(Section, "Recent", CurFilter.Recently.ToString());
            App.SetConfig(Section, "Sockets", CurFilter.Sockets.ToString());
            App.SetConfig(Section, "Rules", CurFilter.Rules.ToString());
            App.SetConfig(Section, "Access", CurFilter.Access.ToString());
            App.SetConfig(Section, "Types", string.Join(",", CurFilter.Types));
            App.SetConfig(Section, "Categories", string.Join(",", CurFilter.Categories));
            App.SetConfig(Section, "Filter", CurFilter.Filter);
        }

        private FilterPreset LoadPreset(string Name = null)
        {
            string Section = MakePresetSection(Name);

            FilterPreset Filter = new FilterPreset();
            Enum.TryParse(App.GetConfig(Section, "Recent"), out Filter.Recently);
            Enum.TryParse(App.GetConfig(Section, "Sockets"), out Filter.Sockets);
            Enum.TryParse(App.GetConfig(Section, "Rules"), out Filter.Rules);
            Enum.TryParse(App.GetConfig(Section, "Access"), out Filter.Access);
            Filter.Types = TextHelpers.SplitStr(App.GetConfig(Section, "Types"), ",");
            Filter.Categories = TextHelpers.SplitStr(App.GetConfig(Section, "Categories"), ",");
            Filter.Filter = App.GetConfig(Section, "Filter");
            return Filter;
        }


        private void CmdPreset_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SuspendChange != 0)
                return;

            var item = this.cmdPreset.SelectedItem;
            if (item == null)
                return;
            var Filter = LoadPreset((item as RibbonGalleryItem).Tag.ToString());
            ApplyPreset(Filter);
        }

        private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            if (txtPreset.Text.Length == 0)
                txtPreset.Text = "Preset_" + (cmdPresets.Items.Count + 1).ToString();

            string Name = txtPreset.Text;

            bool bFound = false;
            foreach (var item in cmdPresets.Items)
            {
                if ((item as RibbonGalleryItem).Content.Equals(Name))
                {
                    bFound = true;
                    break;
                }
            }

            if (!bFound)
            {
                var item = new RibbonGalleryItem { Content = Name, Tag = Name };

                cmdPresets.Items.Add(item);

                SuspendChange++;
                cmdPreset.SelectedItem = item;
                SuspendChange--;
            }

            SavePreset(CurFilter, Name);
        }

        private void BtnDelPreset_Click(object sender, RoutedEventArgs e)
        {
            var item = this.cmdPreset.SelectedItem;
            if (item == null)
                return;
            string Section = MakePresetSection((item as RibbonGalleryItem).Tag.ToString());
            App.IniDeleteSection(Section);
            cmdPresets.Items.Remove(item);
        }

        private void TxtPreset_LostFocus(object sender, RoutedEventArgs e)
        {
            var text = txtPreset.Text;
            Task.Delay(10).ContinueWith(t => txtPreset.Dispatcher.Invoke(() => txtPreset.Text = text));
        }

        private void TxtPreset_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TxtPreset_LostFocus(null, null);
        }
    }
}
