using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;



namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for FirewallManager.xaml
    /// </summary>
    public partial class FirewallPage : UserControl, IUserPage
    {
        DispatcherTimer mTimer = new DispatcherTimer();

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

        bool SortAndFilterProgs = false;

        CategoryModel CatModel;

        enum ListModes
        {
            None = 0,
            List = 1,
            Tree = 2
        }
        ListModes ListMode = ListModes.None;

        int SuspendChange = 0;

        //ObservableCollection<ProgramSet> ProgramSets;

        bool AllEntrySelected = false;
        //private bool showAll { get { return cmbViewMode.SelectedItem != modeNormal; } }
        private bool showAll(bool forRules = false)
        {
            if (chkAll.IsChecked == true || btnFullScreen.IsChecked == true)
                return true;
            if (!forRules && AllEntrySelected)
                return true;
            return false;
        }

        public FirewallPage()
        {
            InitializeComponent();

            ruleList.SetPage(this);
            consList.firewallPage = this;
            sockList.firewallPage = this;
            dnsList.firewallPage = this;

            #region Localization

            this.btnNormalView.ToolTip = Translate.fmt("lbl_normal_view");
            this.btnFullHeight.ToolTip = Translate.fmt("lbl_full_height");
            this.btnFullWidth.ToolTip = Translate.fmt("lbl_full_width");
            this.btnFullScreen.ToolTip = Translate.fmt("lbl_full_screen");
            this.chkAll.ToolTip = Translate.fmt("lbl_show_all");

            this.rbbFilter.Header = Translate.fmt("lbl_view_filter");

            this.btnNoFilter.Label = Translate.fmt("btn_no_filters");
            this.btnDelPreset.Label = Translate.fmt("btn_del_filter");
            this.btnAddPreset.Label = Translate.fmt("btn_save_filter");

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
            //this.chkAll.Label = Translate.fmt("chk_all");
            //cmbViewMode.SelectedItem = modeNormal;

            this.lblProgSet.Header = Translate.fmt("lbl_prog_set");
            this.lblProgOpts.Header = Translate.fmt("lbl_opts");
            this.lblProgInfos.Header = Translate.fmt("lbl_infos");
            this.lblCleanUp.Header = Translate.fmt("lbl_cleanup");



            this.rbbProgs.Header = Translate.fmt("lbl_programs");
            this.btnAdd.Label = Translate.fmt("btn_add_prog");
            this.btnAddSub.Header = Translate.fmt("btn_add_to_set");
            this.btnMerge.Label = Translate.fmt("btn_merge_progs");
            this.btnSplit.Label = Translate.fmt("btn_split_progs");


            this.btnAllowAll.Label = Translate.fmt("acl_allow");
            this.btnCustomCfg.Label = Translate.fmt("acl_edit");
            this.btnLanOnly.Header = Translate.fmt("acl_lan");
            this.btnNoConf.Header = Translate.fmt("acl_none");
            this.chkNotify.Label = Translate.fmt("acl_silence");
            this.btnBlockAll.Label = Translate.fmt("acl_block");

            this.btnRename.Label = Translate.fmt("btn_rename_prog");
            this.btnIcon.Label = Translate.fmt("btn_icon_prog");
            this.btnCategory.Label = Translate.fmt("btn_cat_prog");


            this.btnRemove.Label = Translate.fmt("btn_del_progs");
            this.btnCleanup.Label = Translate.fmt("btn_cleanup_list");
            this.btnCleanupEx.Header = Translate.fmt("btn_ext_cleanup");
            this.btnClearLog.Label = Translate.fmt("btn_clear_fw_log");
            this.btnClearDns.Label = Translate.fmt("btn_clear_dns_log");

            this.rbbRules.Header = Translate.fmt("lbl_rules");
            this.rbbRule.Header = Translate.fmt("lbl_rules");
            this.rbbRuleEdit.Header = Translate.fmt("lbl_sel_rules");
            this.rbbRuleGuard.Header = Translate.fmt("lbl_rule_guard");

            this.progTab.Header = Translate.fmt("lbl_programs");
            this.ruleTab.Header = Translate.fmt("lbl_fw_rules");
            this.sockTab.Header = Translate.fmt("lbl_sockets");
            this.logTab.Header = Translate.fmt("gtp_con_log");
            this.inspectorTab.Header = Translate.fmt("lbl_dns_inspector");

            #endregion

            rbbBar.SelectedIndex = 0;

            SuspendChange++;

            CatModel = new CategoryModel();
            LoadCategorys();
            CatModel.Categorys.CollectionChanged += Categorys_CollectionChanged;
            progList.CatModel = CatModel;

            double progColHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallProgsWidth", "0.0"));
            if (progColHeight > 0.0)
                progsCol.Width = new GridLength(progColHeight, GridUnitType.Pixel);

            double rulesRowHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallRulesHeight", "0.0"));
            if (rulesRowHeight > 0.0)
                rulesRow.Height = new GridLength(rulesRowHeight, GridUnitType.Pixel);

            ViewModes viewMode;
            if(!Enum.TryParse(App.GetConfig("GUI", "FirewallViewMode", ""), out viewMode))
                viewMode = ViewModes.NormalView;
            SetViewMode(viewMode);
            SetTreeMode();


            if (UwpFunc.IsWindows7OrLower)
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

            //this.cmbViewMode.SelectionChanged += CmdViewMode_SelectionChanged;

            btnAllowAll.Tag = ProgramSet.Config.AccessLevels.FullAccess;
            btnCustomCfg.Tag = ProgramSet.Config.AccessLevels.CustomConfig;
            btnLanOnly.Tag = ProgramSet.Config.AccessLevels.LocalOnly;
            btnNoConf.Tag = ProgramSet.Config.AccessLevels.Unconfigured;
            btnBlockAll.Tag = ProgramSet.Config.AccessLevels.BlockAccess;

            progTree.SetPage(this);

            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 250); // 4 times a second
            mTimer.Start();
            OnTimerTick(null, null);

            App.client.ActivityNotification += OnActivity;
            App.client.ChangeNotification += OnChange;
            App.client.UpdateNotification += OnUpdate;


            //ProgramSets = new ObservableCollection<ProgramSet>();
            //progList.ItemsSource = ProgramSets;
            progList.SelectionChanged += ProgList_SelectionChanged;

            progTree.SelectionChanged += ProgList_SelectionChanged;

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

            inspectorTab.IsEnabled = App.GetConfigInt("DnsInspector", "Enabled", 0) != 0;

            UpdateProgramList();
        }

        public void OnHide()
        {
            IsHidden = true;
        }

        public void OnClose()
        {

            App.SetConfig("GUI", "FirewallViewMode", viewMode.ToString());
            App.SetConfig("GUI", "FirewallProgsWidth", (centerSplitPos != null ? centerSplitPos.Value.Value : (int)progsCol.ActualWidth).ToString());
            App.SetConfig("GUI", "FirewallRulesHeight", (rightSplitPos != null ? rightSplitPos.Value.Value : (int)rulesRow.ActualHeight).ToString());

            progTree.OnClose();
            ruleList.OnClose();
            consList.OnClose();
            sockList.OnClose();
            dnsList.OnClose();
        }

        private void ProgList_SelectionChanged(object sender, EventArgs e)
        {
            var items = GetSelectedProgramSets();
            AllEntrySelected = items.Count == 1 && items[0].Programs.FirstOrDefault().Key.Type == ProgramID.Types.Global;

            ruleList.UpdateRules(true);
            consList.UpdateConnections(true);
            sockList.UpdateSockets(true);
            dnsList.UpdateDnsLog(true);
            CheckPrograms();
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

        void OnActivity(object sender, Priv10Engine.FwEventArgs args)
        {
            ProgramSet prog = GetProgSet(args.guid, args.progID);
            if (prog == null)
                return;

            Program program = null;
            prog.Programs.TryGetValue(args.progID, out program);

            if (args.entry.State == Program.LogEntry.States.UnRuled && args.entry.FwEvent.Action == FirewallRule.Actions.Block)
            {
                bool? Notify = prog.config.GetNotify();
                if (Notify == true || (App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0 && App.GetConfigInt("Firewall", "Enabled", 0) != 0 && Notify != false))
                    ShowNotification(prog, args);
            }

            if (IsHidden)
                return;

            if (!args.update) // ignore update events
            {
                /*if (program != null)
                {
                    switch (args.entry.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow: 
                            program.LastAllowed = DateTime.Now;
                            program.AllowedCount++; 
                            break;
                        case FirewallRule.Actions.Block: 
                            program.LastBlocked = DateTime.Now;
                            program.BlockedCount++; 
                            break; 
                    }
                }*/

                if (ListMode == ListModes.List)
                {
                    if (progList.OnActivity(prog, program, args))
                        SortAndFilterProgs = true;
                }
            }

            // from here on only col log update:

            if (GetSelectedItems().Contains(prog) || showAll())
                consList.AddEntry(prog, program, args);
        }

        void OnChange(object sender, Priv10Engine.ChangeArgs args)
        {
            App.MainWnd.notificationWnd.NotifyRule(args);
        }

        private void ShowNotification(ProgramSet prog, Priv10Engine.FwEventArgs args)
        {
            if (args.update && !App.MainWnd.notificationWnd.IsVisible) // dont show on update events
                return;

            App.MainWnd.notificationWnd.AddCon(prog, args);
        }

        HashSet<Guid> UpdatesProgs = new HashSet<Guid>();
        bool FullUpdate = false;
        bool RuleUpdate = false;

        void OnUpdate(object sender, Priv10Engine.UpdateArgs args)
        {
            if (args.guid == Guid.Empty)
                FullUpdate = true;
            else if (args.type == Priv10Engine.UpdateArgs.Types.ProgSet)
                UpdatesProgs.Add(args.guid);
            else
                RuleUpdate = true;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (IsHidden)
                return;
            if (App.MainWnd.WindowState == WindowState.Minimized)
                return;
            if (App.MainWnd.Visibility != Visibility.Visible)
                return;

            if (FullUpdate)
            {
                FullUpdate = false;
                UpdateProgramList();
            }
            else if (UpdatesProgs.Count > 0)
            {
                List<ProgramSet> progs = App.client.GetPrograms(UpdatesProgs.ToList());
                UpdateProgramItems(progs);
            }

            if (RuleUpdate)
            {
                RuleUpdate = false;
                ruleList.UpdateRules();
            }

            if (UpdatesProgs.Count > 0 || SortAndFilterProgs)
            {
                SortAndFilterProgs = false;
                SortAndFitlerProgList();
            }

            UpdatesProgs.Clear();

            if (GetSelectedItems().Count > 0 || showAll())
            {
                sockList.UpdateSockets();
                dnsList.UpdateDnsLog();
            }
        }

        private void UpdateProgramList()
        {
            List<ProgramSet> progs = App.client.GetPrograms();
            if(ListMode == ListModes.List)
                progList.UpdateProgramList(progs);
            else if (ListMode == ListModes.Tree)
                progTree.UpdateProgramList(progs);
            CheckPrograms();
        }

        private void UpdateProgramItems(List<ProgramSet> progs)
        {
            if (ListMode == ListModes.List)
                progList.UpdateProgramItems(progs);
            else  if (ListMode == ListModes.Tree)
                progTree.UpdateProgramItems(progs);
            CheckPrograms();
        }

        private void SortAndFitlerProgList()
        {
            if (ListMode == ListModes.List)
                progList.SortAndFitlerProgList(CurFilter);
            else if (ListMode == ListModes.Tree)
                progTree.SortAndFitlerProgList(CurFilter);
        }

        public List<object> GetSelectedItems()
        {
            if(ListMode == ListModes.List)
                return progList.GetSelectedItems();
            else if (ListMode == ListModes.Tree)
                return progTree.GetSelectedItems();
            return new List<object>();
        }

        public List<ProgramSet> GetSelectedProgramSets()
        {
            List<ProgramSet> SelectedItems = new List<ProgramSet>();
            foreach (var item in GetSelectedItems())
            {
                var progSet = item as ProgramSet;
                if (progSet == null)
                {
                    var prog = item as Program;
                    progSet = prog.ProgSet;
                }
                if (!SelectedItems.Contains(progSet))
                    SelectedItems.Add(progSet);
            }
            return SelectedItems;
        }

        public ProgramSet GetProgSet(Guid guid, ProgramID progID = null)
        {
            ProgramSet prog = null;
            if (ListMode == ListModes.List)
                prog = progList.GetProgSet(guid);
            else if (ListMode == ListModes.Tree)
                prog = progTree.GetProgSet(guid);
            if (prog != null)
                return prog;

            if (progID != null)
                return App.client.GetProgram(progID);

            List<ProgramSet> progs = App.client.GetPrograms(new List<Guid>() { guid });
            if (progs.Count() != 0)
                return progs[0];

            return null;
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

        static public bool DoFilter(FilterPreset Filter, ProgramSet prog)
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
                    if (prog.Programs.Sum(t => t.Value.SocketsTcp) == 0 && prog.Programs.Sum(t => t.Value.SocketsUdp) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.TCP:
                    if (prog.Programs.Sum(t => t.Value.SocketsTcp) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Client:
                    if (prog.Programs.Sum(t => t.Value.SocketsTcp) - prog.Programs.Sum(t => t.Value.SocketsSrv) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Server:
                    if (prog.Programs.Sum(t => t.Value.SocketsSrv) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.UDP:
                    if (prog.Programs.Sum(t => t.Value.SocketsUdp) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.Web:
                    if (prog.Programs.Sum(t => t.Value.SocketsWeb) == 0)
                        return true;
                    break;
                case FilterPreset.Socket.None:
                    if (prog.Programs.Sum(t => t.Value.SocketsTcp) != 0 || prog.Programs.Sum(t => t.Value.SocketsUdp) != 0)
                        return true;
                    break;
            }


            //Rules
            switch (Filter.Rules)
            {
                case FilterPreset.Rule.Any:
                    if (prog.Programs.Sum(t => t.Value.EnabledRules) == 0 && prog.Programs.Sum(t => t.Value.DisabledRules) == 0)
                        return true;
                    break;
                case FilterPreset.Rule.Active:
                    if (prog.Programs.Sum(t => t.Value.EnabledRules) == 0)
                        return true;
                    break;
                case FilterPreset.Rule.Disabled:
                    if (prog.Programs.Sum(t => t.Value.DisabledRules) == 0)
                        return true;
                    break;
                case FilterPreset.Rule.None:
                    if (prog.Programs.Sum(t => t.Value.EnabledRules) != 0 || prog.Programs.Sum(t => t.Value.DisabledRules) != 0)
                        return true;
                    break;
            }

            //Access
            if (Filter.Access != ProgramSet.Config.AccessLevels.AnyValue)
            {
                if (Filter.Access == ProgramSet.Config.AccessLevels.WarningState)
                {
                    if ((prog.config.NetAccess == ProgramSet.Config.AccessLevels.Unconfigured || prog.config.CurAccess == prog.config.NetAccess) && prog.Programs.Sum(t => t.Value.ChgedRules) == 0)
                        return true;
                }
                else if (Filter.Access != prog.config.GetAccess())
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

        public List<Guid> GetCurGuids(bool forRules = false)
        {
            List<Guid> guids = new List<Guid>();
            if (!showAll(forRules))
            {
                foreach (ProgramSet progSet in GetSelectedProgramSets())
                {
                    /*if (filter != null && DoFilter(filter, progSet.config.Name, progSet.Programs.Keys.ToList()))
                        continue;*/

                    guids.Add(progSet.guid);
                }
            }
            return guids;
        }
        
        private void CheckPrograms()
        {
            bool GlobalSelected = false;
            int SetCount = 0;
            int ProgCount = 0;
            bool? Notify = null;

            foreach (var item in GetSelectedItems())
            {
                var progSet = item as ProgramSet;
                if (progSet != null)
                {
                    if (progSet.Programs.First().Key.Type == ProgramID.Types.Global || progSet.Programs.First().Key.Type == ProgramID.Types.System)
                        GlobalSelected = true;
                    else
                    {
                        SetCount++;
                        if (Notify == null)
                            Notify = progSet.config.GetNotify();
                    }
                }
                else
                {
                    var prog = item as Program;
                    if (prog != null)
                    {
                        ProgCount++;
                    }
                }
            }

            //btnAdd.IsEnabled = progTree.menuAdd = true;
            btnAddSub.IsEnabled = progTree.menuAddSub.IsEnabled = (SetCount == 1 && ProgCount == 0 && !GlobalSelected);
            btnRemove.IsEnabled = progTree.menuRemove.IsEnabled = ((SetCount >= 1 || ProgCount >= 1) && !GlobalSelected);
            btnMerge.IsEnabled = progTree.menuMerge.IsEnabled = (SetCount >= 2 && ProgCount == 0 && !GlobalSelected);
            btnSplit.IsEnabled = progTree.menuSplit.IsEnabled = (SetCount == 0 && ProgCount >= 1 && !GlobalSelected && ListMode != ListModes.List);

            progTree.menuAccess.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            btnAllowAll.IsEnabled = progTree.menuAccessAllow.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            btnCustomCfg.IsEnabled = progTree.menuAccessCustom.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            btnLanOnly.IsEnabled = progTree.menuAccessLan.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            btnNoConf.IsEnabled = progTree.menuAccessNone.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            btnBlockAll.IsEnabled = progTree.menuAccessBlock.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);

            chkNotify.IsEnabled = progTree.menuNotify.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
            chkNotify.IsChecked = progTree.menuNotify.IsChecked = (Notify == true || (App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0 && Notify != false));

            btnRename.IsEnabled = progTree.menuRename.IsEnabled = (SetCount == 1 && ProgCount == 0 && !GlobalSelected);
            btnIcon.IsEnabled = progTree.menuSetIcon.IsEnabled = (SetCount == 1 && ProgCount == 0 && !GlobalSelected);
            btnCategory.IsEnabled = progTree.menuCategory.IsEnabled = (SetCount >= 1 && ProgCount == 0 && !GlobalSelected);
        }

        public void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProgramWnd progWnd = new ProgramWnd(null);

            if (progWnd.ShowDialog() != true)
                return;

            if (App.client.AddProgram(progWnd.ID, Guid.Empty))
                return;

            MessageBox.Show(Translate.fmt("msg_already_exist"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public void btnAddSub_Click(object sender, RoutedEventArgs e)
        {
            var SelectedProgramSets = GetSelectedProgramSets();
            if (SelectedProgramSets.Count != 1)
                return;

            ProgramWnd progWnd = new ProgramWnd(null);
            if (progWnd.ShowDialog() != true)
                return;

            if (!App.client.AddProgram(progWnd.ID, SelectedProgramSets[0].guid))
                MessageBox.Show(Translate.fmt("msg_already_exist"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            var SelectedProgramSets = GetSelectedProgramSets();
            if (SelectedProgramSets.Count < 2)
                return;

            foreach (var progSet in SelectedProgramSets)
            {
                if (progSet.Programs.First().Key.Type == ProgramID.Types.System || progSet.Programs.First().Key.Type == ProgramID.Types.Global)
                {
                    MessageBox.Show(Translate.fmt("msg_no_sys_merge"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            for (var firstProg = SelectedProgramSets[0]; SelectedProgramSets.Count > 1; SelectedProgramSets.RemoveAt(1))
            {
                ProgramSet curProgram = SelectedProgramSets[1];
                App.client.MergePrograms(firstProg.guid, curProgram.guid);
            }
        }

        public void btnSplit_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<Guid, List<Program>> Temp = new Dictionary<Guid, List<Program>>();

            foreach (var item in GetSelectedItems())
            {
                var prog = item as Program;
                Temp.GetOrCreate(prog.ProgSet.guid).Add(prog);
            }

            foreach (var item in Temp)
            {
                var progSet = item.Value[0].ProgSet;

                if (item.Value.Count == progSet.Programs.Count)
                {
                    MessageBox.Show(Translate.fmt("msg_no_split_all"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                foreach (var prog in item.Value)
                    App.client.SplitPrograms(progSet.guid, prog.ID);
            }
        }

        public void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_progs"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            var items = GetSelectedItems();

            HashSet<Guid> progSets = new HashSet<Guid>();

            foreach (var item in items)
            {
                var progSet = item as ProgramSet;
                if (progSet == null)
                    continue;

                if (!progSets.Contains(progSet.guid))
                    progSets.Add(progSet.guid);

                App.client.RemoveProgram(progSet.guid);
            }

            foreach (var item in items)
            {
                var prog = item as Program;
                if (prog == null || progSets.Contains(prog.ProgSet.guid))
                    continue;

                App.client.RemoveProgram(prog.ProgSet.guid, prog.ID);
            }
        }

        public void btnSetAccess_Click(object sender, RoutedEventArgs e)
        {
            foreach (var progSet in GetSelectedProgramSets())
            {
                var config = progSet.config.Clone();

                config.NetAccess = (ProgramSet.Config.AccessLevels)(sender as Control).Tag;
                App.client.UpdateProgram(progSet.guid, config);
            }
        }

        public void ChkNotify_Click(object sender, RoutedEventArgs e)
        {
            foreach (var progSet in GetSelectedProgramSets())
            {
                var config = progSet.config.Clone();

                config.Notify = config.Notify == true ? false : true;
                App.client.UpdateProgram(progSet.guid, config);
            }
        }

        public void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            var SelectedProgramSets = GetSelectedProgramSets();
            if (SelectedProgramSets.Count != 1)
                return;

            var progSet = SelectedProgramSets[0];

            InputWnd wnd = new InputWnd(Translate.fmt("msg_rename"), progSet.config.Name, App.Title);
            if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                return;

            var config = progSet.config.Clone();

            config.Name = wnd.Value;
            App.client.UpdateProgram(progSet.guid, config);
        }

        public void BtnIcon_Click(object sender, RoutedEventArgs e)
        {
            var SelectedProgramSets = GetSelectedProgramSets();
            if (SelectedProgramSets.Count != 1)
                return;

            var progSet = SelectedProgramSets[0];

            string iconFile = ProgramControl.OpenIconPicker(progSet.GetIcon());
            if (iconFile == null)
                return;

            var config = progSet.config.Clone();

            config.Icon = iconFile;
            App.client.UpdateProgram(progSet.guid, config);
        }

        public void BtnCategory_Click(object sender, RoutedEventArgs e)
        {
            var SelectedProgramSets = GetSelectedProgramSets();
            if (SelectedProgramSets.Count == 0)
                return;

            List<string> Categories = new List<string>();
            foreach (CategoryModel.Category cat in CatModel.Categorys)
            {
                if (cat.SpecialCat == CategoryModel.Category.Special.No)
                    Categories.Add(cat.Content.ToString());
            }

            InputWnd wnd = new InputWnd(Translate.fmt("btn_cat_prog"), Categories, SelectedProgramSets[0].config.Category, true, App.Title);
            if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                return;

            foreach (var progSet in SelectedProgramSets)
            {
                var config = progSet.config.Clone();

                config.Category = wnd.Value;
                App.client.UpdateProgram(progSet.guid, config);
            }
        }

        private void btnCleanup_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clean_progs"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            int Count = App.client.CleanUpPrograms();
            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCleanupEx_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // or else btnCleanup_Click will be triggered to
            if (MessageBox.Show(Translate.fmt("msg_clean_progs_ex"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            int Count = App.client.CleanUpPrograms(true);
            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            this.consList.ClearLog();
        }

        private void BtnClearDns_Click(object sender, RoutedEventArgs e)
        {
            this.dnsList.ClearLog();
        }

        public void ShowRuleWindow(FirewallRule rule)
        {
            List<Program> progs = new List<Program>();

            void AddIDs(SortedDictionary<ProgramID, Program> programs)
            {
                foreach (Program prog in programs.Values)
                {
                    if(!progs.Contains(prog))
                        progs.Add(prog);
                }
            }
            if (showAll(true))
            {
                foreach (ProgramSet entry in App.client.GetPrograms())
                    AddIDs(entry.Programs);
            }
            else
            {
                foreach (ProgramSet progSet in GetSelectedProgramSets())
                    AddIDs(progSet.Programs);
            }

            // if no rule was given it means create a new rule for one of the current programs
            if (rule == null)
            {
                rule = new FirewallRule() { guid = null, Profile = (int)FirewallRule.Profiles.All, Interface = (int)FirewallRule.Interfaces.All, Enabled = true };
                rule.Grouping = FirewallManager.RuleGroup;
                rule.Direction = FirewallRule.Directions.Bidirectiona;

                rule.Name = Translate.fmt("custom_rule", progs.Count != 0 ? progs[0].Description : "");
                if (progs.Count == 1)
                    rule.SetProgID(progs[0].ID);
            }

            for (; ; )
            {
                RuleWindow ruleWnd = new RuleWindow(progs, rule);
                if (ruleWnd.ShowDialog() != true)
                    return;

                if (App.client.UpdateRule(rule)) // Note: this also adds
                    break;

                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
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

            SortAndFilterProgs = true;
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
                CurFilter.Types.Add(/*chkProgs.IsChecked != true ? "-Programs" :*/
            "Programs");
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

            SortAndFilterProgs = true;
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
            Enum.TryParse(App.GetConfig(Section, "Access", ProgramSet.Config.AccessLevels.AnyValue.ToString()), out Filter.Access);
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

        private void CenterSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            CenterSpliter_switch(progsCol.Width.Value == 0);

            SetTreeMode();
        }

        private void SetTreeMode()
        {
            ListModes Mode = ListModes.Tree;
            if ((viewMode == ViewModes.NormalView || viewMode == ViewModes.FullHeight) && progsCol.Width.Value < 500.0)
                Mode = ListModes.List;

            if (ListMode != Mode)
            {
                ListMode = Mode;
                progList.Visibility = ListMode == ListModes.List ? Visibility.Visible : Visibility.Collapsed;
                progTree.Visibility = ListMode == ListModes.Tree ? Visibility.Visible : Visibility.Collapsed;
                UpdateProgramList();
            }
        }

        private void CenterSpliter_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CenterSpliter_switch(progsCol.Width.Value != 0);
        }

        private void CenterSpliter_switch(bool val)
        {
            if (viewMode == ViewModes.FullHeight)
            {
                if (val)
                    SetViewMode(ViewModes.FullScreen);
            }
            else if (viewMode == ViewModes.FullScreen)
            {
                if (!val)
                    SetViewMode(ViewModes.FullHeight);
            }
            else if (val)
                SetViewMode(ViewModes.FullWidth);
            else
                SetViewMode(ViewModes.NormalView);
        }

        private void RightSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            RightSplitter_switch(rulesRow.Height.Value == 0);
        }

        private void RightSplitter_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RightSplitter_switch(rulesRow.Height.Value != 0);
        }

        private void RightSplitter_switch(bool val)
        {
            if (viewMode == ViewModes.FullWidth)
            {
                if (val)
                    SetViewMode(ViewModes.FullScreen);
            }
            else if (viewMode == ViewModes.FullScreen)
            {
                if (!val)
                    SetViewMode(ViewModes.FullWidth);
            }
            else if (val)
                SetViewMode(ViewModes.FullHeight);
            else
                SetViewMode(ViewModes.NormalView);
        }

        enum ViewModes
        {
            Undefined = 0,
            NormalView,
            FullHeight,
            FullWidth,
            FullScreen
        }

        private ViewModes viewMode = ViewModes.Undefined;

        private GridLength? centerSplitPos = null;
        private GridLength? rightSplitPos = null;

        private void SetViewMode(ViewModes viewMode)
        {
            btnNormalView.IsChecked = viewMode == ViewModes.NormalView;
            btnFullWidth.IsChecked = viewMode == ViewModes.FullWidth;
            btnFullHeight.IsChecked = viewMode == ViewModes.FullHeight;
            btnFullScreen.IsChecked = viewMode == ViewModes.FullScreen;
            chkAll.IsEnabled = viewMode != ViewModes.FullScreen;

            if (this.viewMode == viewMode)
                return;
            this.viewMode = viewMode;

            AppLog.Debug("Settign View Mode: {0}", viewMode.ToString());

            if (viewMode == ViewModes.NormalView || viewMode == ViewModes.FullWidth)
            {
                if (rulesRow.Height.Value == 0)
                {
                    rulesRow.Height = rightSplitPos != null ? rightSplitPos.Value : new GridLength(this.ActualHeight / 2, GridUnitType.Pixel);
                    rightSplitPos = null;
                }
            }
            else if (viewMode == ViewModes.FullScreen || viewMode == ViewModes.FullHeight)
            {
                if (rulesRow.Height.Value != 0)
                {
                    rightSplitPos = rulesRow.Height;
                    rulesRow.Height = new GridLength(0, GridUnitType.Pixel);
                }
            }


            if (viewMode == ViewModes.NormalView || viewMode == ViewModes.FullHeight)
            {
                if (progsCol.Width.Value == 0)
                {
                    progsCol.Width = centerSplitPos != null ? centerSplitPos.Value : new GridLength(this.ActualWidth / 2, GridUnitType.Pixel);
                    centerSplitPos = null;
                }
            }
            else if (viewMode == ViewModes.FullScreen || viewMode == ViewModes.FullWidth)
            {
                if (progsCol.Width.Value != 0)
                {
                    centerSplitPos = progsCol.Width;
                    progsCol.Width = new GridLength(0, GridUnitType.Pixel);
                }
            }


            if (viewMode == ViewModes.NormalView)
            {
                progTab.Visibility = Visibility.Collapsed;
                ruleTab.Visibility = Visibility.Collapsed;
                if (tabs.SelectedIndex < 2)
                    tabs.SelectedIndex = 2;

                progTab.Content = null;
                ruleTab.Content = null;
                grpRules.Content = ruleList;
                grpTree.Content = progTree;
            }
            else if (viewMode == ViewModes.FullHeight)
            {
                progTab.Visibility = Visibility.Collapsed;
                ruleTab.Visibility = Visibility.Visible;
                if (tabs.SelectedIndex < 1)
                    tabs.SelectedIndex = 1;

                progTab.Content = null;
                grpRules.Content = null;
                ruleTab.Content = ruleList;
                grpTree.Content = progTree;
            }
            else if (viewMode == ViewModes.FullWidth)
            {
                progTab.Visibility = Visibility.Collapsed;
                ruleTab.Visibility = Visibility.Visible;
                if (tabs.SelectedIndex < 1)
                    tabs.SelectedIndex = 1;

                progTab.Content = null;
                grpTree.Content = null;
                ruleTab.Content = ruleList;
                grpRules.Content = progTree;
            }
            else if (viewMode == ViewModes.FullScreen)
            {
                progTab.Visibility = Visibility.Visible;
                ruleTab.Visibility = Visibility.Visible;

                grpRules.Content = null;
                grpTree.Content = null;
                progTab.Content = progTree;
                ruleTab.Content = ruleList;
            }

            SetTreeMode();
        }

        //private ViewModes prevMode = ViewModes.NormalView;
        /*private void CmdViewMode_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (cmbViewMode.SelectedItem == modeFull)
            {
                prevMode = viewMode;
                SetViewMode(ViewModes.FullScrean);
            }
            else
                SetViewMode(prevMode);

            ProgList_SelectionChanged(null, null);
        }*/

        private void chkAll_Click(object sender, RoutedEventArgs e)
        {
            ProgList_SelectionChanged(null, null);
        }

        private void BtnViewMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnNormalView) SetViewMode(ViewModes.NormalView);
            if (sender == btnFullWidth) SetViewMode(ViewModes.FullWidth);
            if (sender == btnFullHeight) SetViewMode(ViewModes.FullHeight);
            if (sender == btnFullScreen) SetViewMode(ViewModes.FullScreen);
        }

        /*private void BtnFull_Click(object sender, RoutedEventArgs e)
        {
            if (btnFull.IsChecked == true)
            {
                prevMode = viewMode;
                SetViewMode(ViewModes.FullScrean);
            }
            else
                SetViewMode(prevMode);

            ProgList_SelectionChanged(null, null);
        }*/

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (progsCol.Width.Value > this.ActualWidth - (48 + 5))
            {
                progsCol.Width = new GridLength(this.ActualWidth - (48 + 50 + 5), GridUnitType.Pixel);
            }

            if (rulesRow.Height.Value > this.ActualHeight - (rbbBar.ActualHeight + 5))
            {
                rulesRow.Height = new GridLength(this.ActualHeight - (rbbBar.ActualHeight + 50 + 5), GridUnitType.Pixel);
            }
        }


    }
}
