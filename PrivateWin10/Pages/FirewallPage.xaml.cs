using PrivateWin10.Controls;
using PrivateWin10.ViewModels;
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
    /// Interaction logic for Firewall.xaml
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
            ModuleCount
        }
        Sorts mSortBy = Sorts.Name;

        string mCatFilter = "!Global";

        string mProgFilter = "";
        bool mSortProgs = false;

        bool mHideDisabled = false;
        string mRuleFilter = "";
        //string mIDFilter = "";
        string mConFilter = "";
        int mConLimit = 1000;

        enum ConTypes
        {
            All = 0,
            Blocked,
            Allowed
        }
        ConTypes mConTypes = ConTypes.All;

        private CategoryModel CatModel;

        int SuspendChange = 0;

        public FirewallPage()
        {
            InitializeComponent();

            this.btnAdd.Content = Translate.fmt("btn_add_prog");
            this.btnMerge.Content = Translate.fmt("btn_merge_progs");
            this.btnRemove.Content = Translate.fmt("btn_del_progs");
            this.btnCleanup.Content = Translate.fmt("btn_cleanup_list");
            this.chkNoLocal.Content = Translate.fmt("chk_ignore_local");

            this.lblSort.Content = Translate.fmt("lbl_sort");
            this.lblType.Content = Translate.fmt("lbl_type");
            this.lblFilter.Content = Translate.fmt("lbl_filter");

            this.btnReload.Content = Translate.fmt("btn_reload");
            this.chkAll.Content = Translate.fmt("chk_all");

            this.grpRules.Header = Translate.fmt("grp_firewall");
            this.grpLog.Header = Translate.fmt("gtp_con_log");
            this.grpRuleTools.Header = Translate.fmt("grp_tools");
            this.grpLogTools.Header = Translate.fmt("grp_tools");
            this.grpRuleView.Header = Translate.fmt("grp_view");
            this.grpLogView.Header = Translate.fmt("grp_view");

            this.btnCreateRule.Content = Translate.fmt("btn_mk_rule");
            this.btnEnableRule.Content = Translate.fmt("btn_enable_rule");
            this.btnDisableRule.Content = Translate.fmt("btn_disable_rule");
            this.btnRemoveRule.Content = Translate.fmt("btn_enable_rule");
            this.btnBlockRule.Content = Translate.fmt("btn_block_rule");
            this.btnAllowRule.Content = Translate.fmt("btn_allow_rule");
            this.btnEditRule.Content = Translate.fmt("btn_edit_rule");
            this.btnCloneRule.Content = Translate.fmt("btn_clone_rule");

            this.chkNoDisabled.Content = Translate.fmt("chk_hide_disabled");
            this.lblFilterRules.Content = Translate.fmt("lbl_filter_rules");

            this.btnMkRule.Content = Translate.fmt("btn_mk_rule");
            this.btnClearLog.Content = Translate.fmt("btn_clear_log");
            this.lblShowCons.Content = Translate.fmt("lbl_show_cons");
            this.lblFilterCons.Content = Translate.fmt("lbl_filter_cons");

            this.ruleGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.ruleGrid.Columns[2].Header = Translate.fmt("lbl_group");
            this.ruleGrid.Columns[3].Header = Translate.fmt("lbl_enabled");
            this.ruleGrid.Columns[4].Header = Translate.fmt("lbl_profiles");
            this.ruleGrid.Columns[5].Header = Translate.fmt("lbl_action");
            this.ruleGrid.Columns[6].Header = Translate.fmt("lbl_direction");
            this.ruleGrid.Columns[7].Header = Translate.fmt("lbl_protocol");
            this.ruleGrid.Columns[8].Header = Translate.fmt("lbl_remote_ip");
            this.ruleGrid.Columns[9].Header = Translate.fmt("lbl_local_ip");
            this.ruleGrid.Columns[10].Header = Translate.fmt("lbl_remote_port");
            this.ruleGrid.Columns[11].Header = Translate.fmt("lbl_local_port");
            this.ruleGrid.Columns[11].Header = Translate.fmt("lbl_icmp");
            this.ruleGrid.Columns[12].Header = Translate.fmt("lbl_interfaces");
            this.ruleGrid.Columns[13].Header = Translate.fmt("lbl_edge");
            this.ruleGrid.Columns[14].Header = Translate.fmt("lbl_program");

            this.consGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.consGrid.Columns[2].Header = Translate.fmt("lbl_time_stamp");
            this.consGrid.Columns[3].Header = Translate.fmt("lbl_action");
            this.consGrid.Columns[4].Header = Translate.fmt("lbl_direction");
            this.consGrid.Columns[5].Header = Translate.fmt("lbl_protocol");
            this.consGrid.Columns[6].Header = Translate.fmt("lbl_remote_ip");
            this.consGrid.Columns[7].Header = Translate.fmt("lbl_remote_port");
            this.consGrid.Columns[8].Header = Translate.fmt("lbl_local_ip");
            this.consGrid.Columns[9].Header = Translate.fmt("lbl_local_port");
            this.consGrid.Columns[10].Header = Translate.fmt("lbl_program");



            double progColHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallProgsWidth", "0.0"));
            if (progColHeight > 0.0)
                progsCol.Width = new GridLength(progColHeight, GridUnitType.Pixel);

            double rulesRowHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "FirewallRulesHeight", "0.0"));
            if (progColHeight > 0.0)
                rulesRow.Height = new GridLength(rulesRowHeight, GridUnitType.Pixel);

            ruleCols.DisplayIndexes = App.GetConfig("GUI", "ruleGrid_DisplayIndexes", "0;1;2;3;4;5;6;7;8;9;10;11;12;13;14;15");
            ruleCols.VisibleColumns = App.GetConfig("GUI", "ruleGrid_VisibleColumns", "Program;Local Ports;Remote Ports;Remote Address;Protocol;Direction;Action;Location;Enabled;Name;Profiles;Interfaces;-0");
            consCols.DisplayIndexes = App.GetConfig("GUI", "consGrid_DisplayIndexes", "0;1;2;3;4;5;6;7;8;9;10");
            consCols.VisibleColumns = App.GetConfig("GUI", "consGrid_VisibleColumns", "Program;Local Ports;Local Address;Remote Ports;Remote Address;Protocol;Direction;Action;Time Stamp;Name;;-0");

            SuspendChange++;

            cmbSoft.Items.Add(new ContentControl { Content = Translate.fmt("sort_no"), Tag = Sorts.Unsorted });
            cmbSoft.Items.Add(new ContentControl { Content = Translate.fmt("sort_name"), Tag = Sorts.Name });
            cmbSoft.Items.Add(new ContentControl { Content = Translate.fmt("sort_rname"), Tag = Sorts.NameRev });
            cmbSoft.Items.Add(new ContentControl { Content = Translate.fmt("sort_act"), Tag = Sorts.LastActivity });
            cmbSoft.Items.Add(new ContentControl { Content = Translate.fmt("sort_count"), Tag = Sorts.ModuleCount });
            WpfFunc.CmbSelect(cmbSoft, ((Sorts)App.GetConfigInt("GUI", "SortList", 0)).ToString());

            mProgFilter = App.GetConfig("GUI", "ProgFilter", "");
            txtFilter.Text = mProgFilter;

            mRuleFilter = App.GetConfig("GUI", "RuleFilter", "");
            txtRuleFilter.Text = mRuleFilter;

            //mIDFilter = App.GetConfig("GUI", "IDFilter", "");
            //txtIDFilter.Text = mIDFilter;

            mConFilter = App.GetConfig("GUI", "ConFilter", "");
            txtConFilter.Text = mConFilter;

            try
            {
                mConTypes = (ConTypes)App.GetConfigInt("GUI", "ConTypes", 0);
                cmbConTypes.SelectedIndex = (int)mConTypes;
            }
            catch { }

            //mConLimit = App.engine.programs.MaxLogLength * 10; // todo
            mConLimit = App.GetConfigInt("GUI", "LogLimit", 1000);

            CatModel = new CategoryModel();

            Filters = new ObservableCollection<CatEntry>();

            ListCollectionView lcv = new ListCollectionView(Filters);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            cmbFilter.ItemsSource = lcv;

            mCatFilter = App.GetConfig("GUI", "CategoryFilter", mCatFilter);
            LoadCategorys();

            CatModel.Categorys.CollectionChanged += Categorys_CollectionChanged;

            SuspendChange--;

            mTimer.Tick += new EventHandler(OnTimer_Tick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 250); // 4 times a second
            mTimer.Start();
            OnTimer_Tick(null, null);

            App.cb.ActivityNotification += OnActivity;
            App.cb.ChangeNotification += OnChange;

            this.processScroll.PreviewKeyDown += process_KeyEventHandler;

            CheckPrograms();
            CheckRules();
            CheckLogLines();
        }

        bool IsHidden = true;

        public void OnShow()
        {
            IsHidden = false;

            FullUpdate = false;
            UpdatesProgs.Clear();

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

            App.SetConfig("GUI", "ruleGrid_DisplayIndexes", ruleCols.DisplayIndexes);
            App.SetConfig("GUI", "ruleGrid_VisibleColumns", ruleCols.VisibleColumns);
            App.SetConfig("GUI", "consGrid_DisplayIndexes", consCols.DisplayIndexes);
            App.SetConfig("GUI", "consGrid_VisibleColumns", consCols.VisibleColumns);

            if (notificationWnd != null)
                notificationWnd.Close();
        }

        private void Categorys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LoadCategorys();
        }

        public class Category : ContentControl
        {
            public string Group { get; set; }
        }

        public class CatEntry : ContentControl
        {
            public bool? IsSelected { get; set; }
            public Visibility IsCheckVisible { get { return IsCheckable ? Visibility.Visible : Visibility.Collapsed; } }
            public bool IsCheckable { get; set; }
            public string Group { get; set; }
        }

        public ObservableCollection<CatEntry> Filters;

        private void AddCatItem(string text, string group, object tag)
        {
            Filters.Add(new CatEntry
            {
                IsCheckable = true,
                IsSelected = false,
                Content = text,
                Tag = tag,
                Group = group
            });
        }

        private void LoadCategorys()
        {
            SuspendChange++;

            Filters.Clear();

            AddCatItem(Translate.fmt("filter_all"), Translate.fmt("cat_gen"), "!All");
            AddCatItem(Translate.fmt("filter_programs"), Translate.fmt("cat_gen"), "!Programs");
            AddCatItem(Translate.fmt("filter_system"), Translate.fmt("cat_gen"), "!System");
            if (UwpFunc.IsWindows7OrLower == false)
                AddCatItem(Translate.fmt("filter_apps"), Translate.fmt("cat_gen"), "!Apps");

            foreach (CategoryModel.Category cat in CatModel.Categorys)
            {
                if (cat.SpecialCat == CategoryModel.Category.Special.No)
                    AddCatItem(cat.Content.ToString(), Translate.fmt("cat_cats"), cat.Content.ToString());
            }

            AddCatItem(Translate.fmt("filter_uncat"), Translate.fmt("cat_other"), "!Uncategorized");

            if (mCatFilter.Length > 0)
            {
                if (mCatFilter.Substring(0, 1) == "!")
                    WpfFunc.CmbSelect(cmbFilter, mCatFilter);
                else if (mCatFilter.Substring(0, 1) == ".")
                    cmbFilter.Text = mCatFilter.Substring(1);
                else
                {
                    if (mCatFilter.Contains(","))
                    {
                        HashSet<string> cats = new HashSet<string>(mCatFilter.Split(','));
                        foreach (CatEntry ctrl in cmbFilter.Items)
                        {
                            if (cats.Contains(ctrl.Tag.ToString()))
                                ctrl.IsSelected = true;
                            else if (cats.Contains("-" + ctrl.Tag.ToString()))
                                ctrl.IsSelected = null;
                            else
                                ctrl.IsSelected = false;
                        }
                    }
                    else
                        cmbFilter.Text = mCatFilter;
                }
            }

            SuspendChange--;
        }

        void OnActivity(object sender, Firewall.NotifyArgs args)
        {
            ProgramControl item = null;
            Program prog;
            if (mPrograms.TryGetValue(args.guid, out item))
                prog = item.Program;
            else
                prog = App.itf.GetProgram(args.entry.mID);

            if (args.entry.Type == Program.LogEntry.Types.UnRuled && args.entry.Action == Firewall.Actions.Block)
            {
                bool? Notify = prog.config.GetNotify();
                if(Notify == true || (App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0 && Notify != false))
                    ShowNotification(prog, args.entry);
            }

            if (IsHidden)
                return;

            if (item == null)
            {
                if (DoFilter(mProgFilter, prog.config.Name, prog.IDs))
                    return;

                item = AddProgramItem(prog);
            }

            if (args.entry.Type == Program.LogEntry.Types.RuleError)
                item.SetError(true);

            if (chkNoLocal.IsChecked != true || (!NetFunc.IsLocalHost(args.entry.RemoteAddress) && !NetFunc.IsMultiCast(args.entry.RemoteAddress)))
            {
                item.Program.lastActivity = DateTime.Now;
                switch (args.entry.Action)
                {
                    case Firewall.Actions.Allow: item.Program.allowedConnections++; break;
                    case Firewall.Actions.Block: item.Program.blockedConnections++; break;
                }
                if (mSortBy == Sorts.LastActivity)
                    mSortProgs = true;

                item.DoUpdate();

                switch (args.entry.Action)
                {
                    case Firewall.Actions.Allow: item.Flash(Colors.LightGreen); break;
                    case Firewall.Actions.Block: item.Flash(Colors.LightPink); break;
                }
            }

            // from here on only col log update:

            if (!mCurPrograms.Contains(item) && chkAll.IsChecked != true)
                return;

            if (DoFilter(mConFilter, args.entry.mID.GetDisplayName(), new HashSet<ProgramList.ID>() { args.entry.mID }))
                return;

            switch (mConTypes)
            {
                case ConTypes.Allowed: if (args.entry.Action != Firewall.Actions.Allow) return; break;
                case ConTypes.Blocked: if (args.entry.Action != Firewall.Actions.Block) return; break;
            }

            consGrid.Items.Insert(0, new LogItem(args.entry));

            while (consGrid.Items.Count > mConLimit)
                consGrid.Items.RemoveAt(mConLimit);

            //if (mSortBy == Sorts.LastActivity)
            //    OnChange(sender, new ProgramList.ChangeArgs() { guid = args.guid });
        }

        private NotificationWnd notificationWnd = null;


        private void ShowNotification(Program prog, Program.LogEntry entry)
        {
            if (notificationWnd == null)
            {
                notificationWnd = new NotificationWnd();
                notificationWnd.Closing += NotificationClosing;
                notificationWnd.Show();
            }
            notificationWnd.Add(prog, entry);
        }

        void NotificationClosing(object sender, CancelEventArgs e)
        {
            notificationWnd = null;
        }

        HashSet<Guid> UpdatesProgs = new HashSet<Guid>();
        bool FullUpdate = false;

        void OnChange(object sender, ProgramList.ChangeArgs args)
        {
            if (args.guid == Guid.Empty)
                FullUpdate = true;
            else
                UpdatesProgs.Add(args.guid);
        }

        private void OnTimer_Tick(object sender, EventArgs e)
        {
            if (IsHidden)
                return;

            if (FullUpdate)
            {
                FullUpdate = false;
                UpdateProgramList();
            }
            else if (UpdatesProgs.Count > 0)
            {
                List<Program> progs = App.itf.GetPrograms(UpdatesProgs.ToList());

                foreach (Program prog in progs)
                {
                    ProgramControl item;
                    if (!mPrograms.TryGetValue(prog.guid, out item))
                        item = AddProgramItem(prog);

                    item.Program = prog;
                    item.DoUpdate();
                    item.SetError(false);
                    if (mCurPrograms.Contains(item))
                    {
                        UpdateIDs();
                        UpdateRules();
                    }
                }
            }

            if (UpdatesProgs.Count > 0 || mSortProgs)
            {
                mSortProgs = false;
                SortAndFitlerProgList();
            }

            UpdatesProgs.Clear();
        }

        SortedDictionary<Guid, ProgramControl> mPrograms = new SortedDictionary<Guid, ProgramControl>();

        private void UpdateProgramList()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Dictionary<Guid, ProgramControl> OldProcesses = new Dictionary<Guid, ProgramControl>(mPrograms);

            foreach (Program prog in App.itf.GetPrograms())
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

            Console.WriteLine("UpdateProgramList took: " + elapsedMs + "ms");

            SortAndFitlerProgList();
        }

        ProgramControl AddProgramItem(Program prog)
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

        bool DoTest(string Filter, string Name, HashSet<ProgramList.ID> IDs)
        {
            bool bNot = false;
            if (Filter.Substring(0, 1) == "-")
            {
                bNot = true;
                Filter = Filter.Substring(1);
            }

            bool bPath = false;
            if (Filter.Substring(0, 1) == "~")
            {
                bPath = true;
                Filter = Filter.Substring(1);
            }

            if (TextHelpers.CompareWildcard(Name, Filter) != bNot)
                return false;
            if (bPath)
            {
                foreach (ProgramList.ID _id in IDs)
                {
                    if (TextHelpers.CompareWildcard(_id.Path,Filter) != bNot)
                        return false;
                    if (TextHelpers.CompareWildcard(_id.Name,Filter) != bNot)
                        return false;
                }
            }
            return true;
        }

        bool DoFilter(string Filter, string Name, HashSet<ProgramList.ID> IDs)
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
                switch (mSortBy)
                {
                    case Sorts.Name: return l.Program.config.Name.CompareTo(r.Program.config.Name);
                    case Sorts.NameRev: return r.Program.config.Name.CompareTo(l.Program.config.Name);
                    case Sorts.LastActivity: return r.Program.lastActivity.CompareTo(l.Program.lastActivity);
                    case Sorts.ModuleCount: return r.Program.IDs.Count.CompareTo(l.Program.IDs.Count);
                }
                return 0;
            }

            if (mSortBy != Sorts.Unsorted)
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


                Func<Program, bool> filterFx = (Program prog) =>
                {
                    if (mCatFilter.Length > 0)
                    {
                        int Accepted = 0;
                        foreach (string Filter in mCatFilter.Split(','))
                        {
                            switch (ExecTest(Filter, prog))
                            {
                                case 0: break;
                                case 1: Accepted++; break;
                                case -1: return false;
                            }
                        }
                        if (Accepted == 0)
                            return false;
                    }

                    if (DoFilter(mProgFilter, prog.config.Name, prog.IDs))
                        return false;

                    return true;
                };

                item.Visibility = filterFx(item.Program) ? Visibility.Visible : Visibility.Collapsed;

            }

            while (OrderList.Count < this.processGrid.RowDefinitions.Count)
                this.processGrid.RowDefinitions.RemoveAt(OrderList.Count);
        }

        private int ExecTest(string Filter, Program prog)
        {
            ProgramList.ID id = prog.GetMainID();

            bool bNot = false;
            if (Filter.Substring(0, 1) == "-")
            {
                bNot = true;
                Filter = Filter.Substring(1);
            }

            if (Filter.Substring(0, 1) == "!")
            {
                if (Filter == "!Uncategorized")
                {
                    if (prog.config.Category == null || prog.config.Category.Length == 0)
                        return bNot ? -1 : 1;
                }
                else if (Filter == "!Programs")
                {
                    if(id.Type == ProgramList.Types.Program)
                        return bNot ? -1 : 1;
                }
                else if (Filter == "!System")
                {
                    if(id.Type == ProgramList.Types.System || id.Type == ProgramList.Types.Global || id.Type == ProgramList.Types.Service)
                        return bNot ? -1 : 1;
                }
                else if (Filter == "!Apps")
                {
                    if (id.Type == ProgramList.Types.App)
                        return bNot ? -1 : 1;
                }
                else //if (Filter == "!All")
                    return 1;

            }
            else if (mCatFilter.Substring(0, 1) == ".")
            {
                Filter = Filter.Substring(1);
                if (TextHelpers.CompareWildcard(prog.config.Category,Filter))
                    return bNot ? -1 : 1;
            }
            else
            {
                if (prog.config.Category != null && Filter.Equals(prog.config.Category, StringComparison.OrdinalIgnoreCase))
                    return bNot ? -1 : 1;
            }
            return 0;
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

            UpdateIDs(true);
            UpdateRules(true);
            UpdateConnections(true);
            CheckPrograms();
        }

        private List<ProgramList.ID> GetIDs()
        {
            List<ProgramList.ID> IDs = new List<ProgramList.ID>();

            void AddIDs(HashSet<ProgramList.ID> ids)
            {
                foreach (ProgramList.ID ID in ids)
                    IDs.Add(ID);
            }
            if (chkAll.IsChecked == true)
            {
                HashSet<Program> temp = new HashSet<Program>();
                foreach (Program entry in App.itf.GetPrograms())
                {
                    if (temp.Add(entry))
                        AddIDs(entry.IDs);
                }
            }
            else
            {
                foreach (ProgramControl ctrl in mCurPrograms)
                {
                    AddIDs(ctrl.Program.IDs);
                }
            }

            return IDs;
        }

        private void UpdateIDs(bool clear = false)
        {
            /*if (clear)
                progGrid.Items.Clear();

            Dictionary<ProgramList.ID, ProgramControl.IDEntry> oldRules = new Dictionary<ProgramList.ID, ProgramControl.IDEntry>();
            foreach (ProgramControl.IDEntry oldItem in progGrid.Items)
                oldRules.Add(oldItem.mID, oldItem);

            foreach (ProgramList.ID id in GetIDs())
            {
                if (DoFilter(mIDFilter, id.GetDisplayName(), new HashSet<ProgramList.ID>() { id }))
                    continue;

                if (!oldRules.Remove(id))
                    progGrid.Items.Insert(0, new ProgramControl.IDEntry(id));
            }

            foreach (ProgramControl.IDEntry item in oldRules.Values)
                progGrid.Items.Remove(item);*/
        }

        private void UpdateRules(bool clear = false)
        {
            if (clear)
                ruleGrid.Items.Clear();

            Dictionary<FirewallRule, RuleItem> oldRules = new Dictionary<FirewallRule, RuleItem>();
            foreach (RuleItem oldItem in ruleGrid.Items)
                oldRules.Add(oldItem.Rule, oldItem);

            List<Guid> guids = new List<Guid>();
            if (chkAll.IsChecked != true)
            {
                foreach (ProgramControl ctrl in mCurPrograms)
                    guids.Add(ctrl.Program.guid);
            }

            foreach (FirewallRule rule in App.itf.GetRules(guids))
            {
                if (mHideDisabled && rule.Enabled == false)
                    continue;

                if (DoFilter(mRuleFilter, rule.Name, new HashSet<ProgramList.ID>() { rule.mID }))
                    continue;

                if (!oldRules.Remove(rule))
                    ruleGrid.Items.Add(new RuleItem(rule));
            }

            foreach (RuleItem item in oldRules.Values)
                ruleGrid.Items.Remove(item);

            // update existing cels
            ruleGrid.Items.Refresh();
        }

        private void UpdateConnections(bool clear = false)
        {
            if (clear)
                consGrid.Items.Clear();

            Dictionary<Guid, LogItem> oldLog = new Dictionary<Guid, LogItem>();
            foreach (LogItem oldItem in consGrid.Items)
                oldLog.Add(oldItem.Entry.guid, oldItem);

            List<Guid> guids = new List<Guid>();
            if (chkAll.IsChecked != true)
            {
                foreach (ProgramControl ctrl in mCurPrograms)
                    guids.Add(ctrl.Program.guid);
            }

            foreach (Program.LogEntry entry in App.itf.GetConnections(guids))
            {
                if (DoFilter(mConFilter, entry.mID.GetDisplayName(), new HashSet<ProgramList.ID>() { entry.mID }))
                    continue;

                switch (mConTypes)
                {
                    case ConTypes.Allowed: if (entry.Action != Firewall.Actions.Allow) continue; break;
                    case ConTypes.Blocked: if (entry.Action != Firewall.Actions.Block) continue; break;
                }

                if (!oldLog.Remove(entry.guid))
                    consGrid.Items.Insert(0, new LogItem(entry));
            }

            foreach (LogItem item in oldLog.Values)
                consGrid.Items.Remove(item);
        }

        void process_KeyEventHandler(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Up || e.Key == Key.Down) && !((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                ProgramControl curProcess = null;
                if (mCurPrograms.Count > 0)
                {
                    foreach (ProgramControl curProg in mCurPrograms)
                        curProg.SetFocus(false);
                    curProcess = mCurPrograms[mCurPrograms.Count - 1];
                    mCurPrograms.Clear();
                }

                if (e.Key == Key.Up)
                {
                    e.Handled = true;
                    int curRow = Grid.GetRow(curProcess);
                    if (curRow > 0)
                    {
                        if (curProcess != null)
                            curProcess.SetFocus(false);
                        curProcess = this.processGrid.Children.Cast<ProgramControl>().First((c) => Grid.GetRow(c) == curRow - 1);
                        if (curProcess != null)
                        {
                            curProcess.SetFocus(true);

                            this.processScroll.ScrollToVerticalOffset(this.processScroll.VerticalOffset - (curProcess.ActualHeight + 2));
                        }
                    }
                }
                else if (e.Key == Key.Down)
                {
                    e.Handled = true;
                    int curRow = Grid.GetRow(curProcess);
                    if (curRow < this.processGrid.Children.Count - 1)
                    {
                        if (curProcess != null)
                            curProcess.SetFocus(false);
                        curProcess = this.processGrid.Children.Cast<ProgramControl>().First((c) => Grid.GetRow(c) == curRow + 1);
                        if (curProcess != null)
                        {
                            curProcess.SetFocus(true);

                            this.processScroll.ScrollToVerticalOffset(this.processScroll.VerticalOffset + (curProcess.ActualHeight + 2));
                        }
                    }
                }

                mCurPrograms.Add(curProcess);

                UpdateIDs(true);
                UpdateRules(true);
                UpdateConnections(true);
                CheckPrograms();
            }
        }

        private void cmbSoft_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mSortBy = (Sorts)((sender as ComboBox).SelectedItem as ContentControl).Tag;
            if (SuspendChange != 0)
                return;
            App.SetConfig("GUI", "SortList", (int)mSortBy);
            mSortProgs = true;
        }

        bool cmbFilterOpen = false;

        private void cmbFilter_DropDownOpened(object sender, EventArgs e)
        {
            cmbFilterOpen = true;
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange != 0)
                return;
            ContentControl ctrl = (cmbFilter.SelectedItem as CatEntry);
            if (ctrl == null)
                return;
            mCatFilter = ctrl.Tag.ToString();
            mSortProgs = true;
            App.SetConfig("GUI", "CategoryFilter", mCatFilter);
        }

        private void cmbFilter_CheckClicked(object sender, RoutedEventArgs e)
        {
            if (SuspendChange != 0)
                return;

            mCatFilter = "";
            foreach (CatEntry ctrl in cmbFilter.Items)
            {
                if (ctrl.IsSelected == false)
                    continue;
                if (mCatFilter.Length > 0)
                    mCatFilter += ",";
                if (ctrl.IsSelected == null)
                    mCatFilter += "-" + ctrl.Tag.ToString();
                else
                    mCatFilter += ctrl.Tag.ToString();
            }
            mSortProgs = true;
            App.SetConfig("GUI", "CategoryFilter", mCatFilter);
        }

        private void cmbFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SuspendChange != 0 || cmbFilterOpen || cmbFilter.SelectedItem != null)
                return;
            if (mCatFilter.Length > 0 && mCatFilter.Substring(0, 1) != ".")
                cmbFilter.Text = "";
            mCatFilter = "." + cmbFilter.Text;
            mSortProgs = true;
            App.SetConfig("GUI", "CategoryFilter", mCatFilter);
        }

        private void cmbFilter_DropDownClosed(object sender, EventArgs e)
        {
            cmbFilterOpen = false;

            if (mCatFilter.Length > 0 && mCatFilter.Substring(0, 1) != "!" && mCatFilter.Substring(0, 1) != ".")
            {
                SuspendChange++;
                if (mCatFilter.Contains(","))
                    cmbFilter.Text = Translate.fmt("filer_multi");
                else
                    cmbFilter.Text = mCatFilter;
                SuspendChange--;
            }
        }

        private void chkAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateIDs(true);
            UpdateRules(true);
            UpdateConnections(true);
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mProgFilter = txtFilter.Text;
            mSortProgs = true;
            FullUpdate = true;
            App.SetConfig("GUI", "ProgFilter", mProgFilter);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProgramWnd progWnd = new ProgramWnd(null);

            for (; ; )
            {
                if (progWnd.ShowDialog() != true)
                    return;

                if (App.itf.AddProgram(progWnd.ID, Guid.Empty))
                    break;

                MessageBox.Show(Translate.fmt("msg_already_exist"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            if (mCurPrograms.Count < 2)
                return;

            foreach (ProgramControl curProgram in mCurPrograms)
            {
                if (curProgram.Program.GetMainID().Type == ProgramList.Types.System || curProgram.Program.GetMainID().Type == ProgramList.Types.Global)
                {
                    MessageBox.Show(Translate.fmt("msg_no_sys_merge"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            for (ProgramControl firstProg = mCurPrograms[0]; mCurPrograms.Count > 1; mCurPrograms.RemoveAt(1))
            {
                Program curProgram = mCurPrograms[1].Program;
                App.itf.MergePrograms(firstProg.Program.guid, curProgram.guid);
            }

            //firstProg.Process.Log.Sort();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_progs"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (ProgramControl curProgram in mCurPrograms)
                App.itf.RemoveProgram(curProgram.Program.guid);
        }

        private void btnEnableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                item.Rule.Enabled = true;
                App.itf.UpdateRule(item.Rule);
            }
        }

        private void btnDisableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                item.Rule.Enabled = false;
                App.itf.UpdateRule(item.Rule);
            }
        }

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_rules"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in ruleGrid.SelectedItems)
                App.itf.RemoveRule(item.Rule);
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            App.itf.LoadRules();
            UpdateIDs(true);
            UpdateRules(true);
        }

        private void btnBlockRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                item.Rule.Action = Firewall.Actions.Block;
                App.itf.UpdateRule(item.Rule);
            }
        }

        private void btnAllowRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                item.Rule.Action = Firewall.Actions.Allow;
                App.itf.UpdateRule(item.Rule);
            }
        }

        private void btnCleanup_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clean_progs"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            int Count = App.itf.CleanUpPrograms();

            MessageBox.Show(Translate.fmt("msg_clean_res", Count), App.mName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCreateRule_Click(object sender, RoutedEventArgs e)
        {
            if (mCurPrograms.Count == 0)
                return;
            FirewallRule rule = new FirewallRule() { guid = Guid.Empty, Profile = (int)Firewall.Profiles.All, Interface = (int)Firewall.Interfaces.All, Enabled = true };
            rule.Name = Translate.fmt("custom_rule", mCurPrograms[0].Program.GetMainID().GetDisplayName());
            rule.Grouping = FirewallRule.RuleGroup;
            rule.Direction = Firewall.Directions.Bidirectiona;
            if (mCurPrograms.Count == 1)
                rule.mID = mCurPrograms[0].Program.GetMainID();
            ShowRuleWindow(rule);
        }

        private void btnEditRule_Click(object sender, RoutedEventArgs e)
        {
            RuleItem item = (ruleGrid.SelectedItem as RuleItem);
            if (item == null)
                return;

            ShowRuleWindow(item.Rule);
        }

        private void ruleGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnEditRule_Click(null, null);
        }

        private void ShowRuleWindow(FirewallRule rule)
        {
            List<ProgramList.ID> IDs = GetIDs();

            for (; ; )
            {
                RuleWindow ruleWnd = new RuleWindow(IDs, rule);
                if (ruleWnd.ShowDialog() != true)
                    return;

                if (App.itf.UpdateRule(rule)) // Note: this also adds
                    break;

                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void cmbConTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mConTypes = (ConTypes)cmbConTypes.SelectedIndex;
            App.SetConfig("GUI", "ConTypes", (int)mConTypes);
            UpdateConnections(true);
        }

        private void txtConFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mConFilter = txtConFilter.Text;
            App.SetConfig("GUI", "ConFilter", mConFilter);
            UpdateConnections(true);
        }

        private void txtRuleFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mRuleFilter = txtRuleFilter.Text;
            App.SetConfig("GUI", "RuleFilter", mRuleFilter);
            UpdateRules(true);
        }

        /*private void txtIDFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mIDFilter = txtIDFilter.Text;
            App.SetConfig("GUI", "IDFilter", mRuleFilter);
            UpdateIDs(true);
        }*/

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_clear_log"), App.mName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (res == MessageBoxResult.Cancel)
                return;

            if(App.itf.ClearLog(res == MessageBoxResult.Yes))
                consGrid.Items.Clear();
        }

        private void btnCloneRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clone_rules"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                FirewallRule rule = item.Rule.Clone();
                rule.Name += " - Clone";
                App.itf.UpdateRule(rule);
            }
        }

        private void btnMkRule_Click(object sender, RoutedEventArgs e)
        {
            LogItem item = (consGrid.SelectedItem as LogItem);
            if (item == null)
                return;

            FirewallRule rule = new FirewallRule() { guid = Guid.Empty, mID = item.Entry.mID, Profile = (int)Firewall.Profiles.All, Interface = (int)Firewall.Interfaces.All, Enabled = true };

            rule.mID = item.Entry.mID;
            rule.Name = Translate.fmt("custom_rule", item.Entry.mID.GetDisplayName());
            rule.Grouping = FirewallRule.RuleGroup;

            rule.Direction = item.Entry.Direction;
            rule.Protocol = item.Entry.Protocol;
            switch (item.Entry.Protocol)
            {
                /*case (int)FirewallRule.KnownProtocols.ICMP:
                case (int)FirewallRule.KnownProtocols.ICMPv6:

                    break;*/
                case (int)FirewallRule.KnownProtocols.TCP:
                case (int)FirewallRule.KnownProtocols.UDP:
                    rule.RemotePorts = item.Entry.RemotePort.ToString();
                    break;
            }
            rule.RemoteAddresses = item.Entry.RemoteAddress.ToString();

            ShowRuleWindow(rule);
        }

        private void chkNoDisabled_Click(object sender, RoutedEventArgs e)
        {
            mHideDisabled = chkNoDisabled.IsChecked == true;
            UpdateRules(true);
        }


        /*private void progGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProgramControl.IDEntry entry = (progGrid.SelectedItem as ProgramControl.IDEntry);
            if (entry == null)
                return;

            ProgramWnd progWnd = new ProgramWnd(entry.mID);
            if (progWnd.ShowDialog() != true)
                return;

            // no editing
        }*/

        private void CheckPrograms()
        {
            bool GlobalSelected = false;
            int SelectedCount = 0;
            foreach (ProgramControl ctrl in mCurPrograms)
            {
                if (ctrl.Program.GetMainID().Type == ProgramList.Types.Global || ctrl.Program.GetMainID().Type == ProgramList.Types.System)
                    GlobalSelected = true;
                else
                    SelectedCount++;
            }

            btnCreateRule.IsEnabled = SelectedCount >= 1;
            btnMerge.IsEnabled = (SelectedCount >= 2 && !GlobalSelected);
            btnRemove.IsEnabled = (SelectedCount >= 1 && !GlobalSelected);
        }

        private void CheckRules()
        {
            int SelectedCount = 0;
            int EnabledCount = 0;
            int DisabledCount = 0;
            int AllowingCount = 0;
            int BlockingCount = 0;

            foreach (RuleItem item in ruleGrid.SelectedItems)
            {
                SelectedCount++;
                if (item.Rule.Enabled)
                    EnabledCount++;
                else
                    DisabledCount++;
                if (item.Rule.Action == Firewall.Actions.Allow)
                    AllowingCount++;
                if (item.Rule.Action == Firewall.Actions.Block)
                    BlockingCount++;
            }

            btnEnableRule.IsEnabled = DisabledCount >= 1;
            btnDisableRule.IsEnabled = EnabledCount >= 1;
            btnRemoveRule.IsEnabled = SelectedCount >= 1;
            btnBlockRule.IsEnabled = AllowingCount >= 1;
            btnAllowRule.IsEnabled = BlockingCount >= 1;
            btnEditRule.IsEnabled = SelectedCount == 1;
            btnCloneRule.IsEnabled = SelectedCount >= 1;
        }

        private void CheckLogLines()
        {
            btnMkRule.IsEnabled = consGrid.SelectedItems.Count == 1;
        }

        private void RuleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckRules();
        }

        private void ConsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckLogLines();
        }
    }

    public class LogItem : INotifyPropertyChanged
    {
        public Program.LogEntry Entry;

        public LogItem(Program.LogEntry entry)
        {
            Entry = entry;
        }

        void DoUpdate()
        {
            NotifyPropertyChanged(null);
        }

        public ImageSource Icon { get { return ImgFunc.GetIcon(Entry.mID.Path, 16); } }

        public string Name { get { return Entry.mID.GetDisplayName(); } }
        public string Program { get { return Entry.mID.AsString(); } }
        public string TimeStamp { get { return Entry.TimeStamp.ToString("HH:mm:ss dd.MM.yyyy"); } }
        public string Action
        {
            get
            {
                switch (Entry.Action)
                {
                    case Firewall.Actions.Allow: return Translate.fmt("str_allow");
                    case Firewall.Actions.Block: return Translate.fmt("str_block");
                    default: return Translate.fmt("str_undefined");
                }
            }
        }

        public string Direction
        {
            get
            {
                switch (Entry.Direction)
                {
                    case Firewall.Directions.Inbound: return Translate.fmt("str_inbound");
                    case Firewall.Directions.Outboun: return Translate.fmt("str_outbound");
                    default: return Translate.fmt("str_undefined");
                }
            }
        }
        public string Protocol { get { return NetFunc.Protocol2Str(Entry.Protocol); } }
        public string DestAddress { get { return Entry.RemoteAddress; } }
        public string DestPorts { get { return Entry.RemotePort.ToString(); } }
        public string SrcAddress { get { return Entry.LocalAddress; } }
        public string SrcPorts { get { return Entry.LocalPort.ToString(); } }

        public string ActionColor
        {
            get
            {
                if(Entry.Type == PrivateWin10.Program.LogEntry.Types.RuleError) return "yellow";
                switch (Entry.Action)
                {
                    case Firewall.Actions.Allow: return "green";
                    case Firewall.Actions.Block: return "red";
                    default: return "";
                }
            }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Helpers

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    public class RuleItem : INotifyPropertyChanged
    {
        public FirewallRule Rule;

        public RuleItem(FirewallRule rule)
        {
            Rule = rule;
        }

        void DoUpdate()
        {
            NotifyPropertyChanged(null);
        }

        public ImageSource Icon { get { return ImgFunc.GetIcon(Rule.mID.Path, 16); } }

        public string Name { get { return Rule.Name; } }
        public string Program { get { return Rule.mID.AsString(); } }
        public string Grouping
        {
            get
            {
                if (Rule.Grouping != null && Rule.Grouping.Substring(0, 1) == "@")
                    return MiscFunc.GetResourceStr(Rule.Grouping);
                return Rule.Grouping;
            }
        }
        public string Enabled { get { return Translate.fmt(Rule.Enabled ? "str_enabled" : "str_disabled"); } }

        public string DisabledColor { get { return Rule.Enabled ? "" : "gray"; } }

        public string Profiles
        {
            get
            {
                if (Rule.Profile == (int)Firewall.Profiles.All)
                    return Translate.fmt("str_all");
                else
                {
                    List<string> profiles = new List<string>();
                    if ((Rule.Profile & (int)Firewall.Profiles.Private) != 0)
                        profiles.Add(Translate.fmt("str_private"));
                    if ((Rule.Profile & (int)Firewall.Profiles.Domain) != 0)
                        profiles.Add(Translate.fmt("str_domain"));
                    if ((Rule.Profile & (int)Firewall.Profiles.Public) != 0)
                        profiles.Add(Translate.fmt("str_public"));
                    return string.Join(",", profiles.ToArray().Reverse());
                }
            }
        }
        public string Action
        {
            get
            {
                switch (Rule.Action)
                {
                    case Firewall.Actions.Allow: return Translate.fmt("str_allow");
                    case Firewall.Actions.Block: return Translate.fmt("str_block");
                    default: return Translate.fmt("str_undefined");
                }
            }
        }

        public string ActionColor
        {
            get
            {
                switch (Rule.Action)
                {
                    case Firewall.Actions.Allow: return "green";
                    case Firewall.Actions.Block: return "red";
                    default: return "";
                }
            }
        }

        public string Direction
        {
            get
            {
                switch (Rule.Direction)
                {
                    case Firewall.Directions.Inbound: return Translate.fmt("str_inbound");
                    case Firewall.Directions.Outboun: return Translate.fmt("str_outbound");
                    default: return Translate.fmt("str_undefined");
                }
            }
        }
        public string Protocol { get { return NetFunc.Protocol2Str(Rule.Protocol); } }
        public string DestAddress { get { return Rule.RemoteAddresses; } }
        public string DestPorts { get { return Rule.RemotePorts; } }
        public string SrcAddress { get { return Rule.LocalAddresses; } }
        public string SrcPorts { get { return Rule.LocalPorts; } }

        public string ICMPOptions { get { return Rule.IcmpTypesAndCodes; } }

        public string Interfaces
        {
            get
            {
                if (Rule.Interface == (int)Firewall.Interfaces.All)
                    return Translate.fmt("str_all");
                else
                {
                    List<string> interfaces = new List<string>();
                    if ((Rule.Profile & (int)Firewall.Interfaces.Lan) != 0)
                        interfaces.Add(Translate.fmt("str_lan"));
                    if ((Rule.Profile & (int)Firewall.Interfaces.RemoteAccess) != 0)
                        interfaces.Add(Translate.fmt("str_ras"));
                    if ((Rule.Profile & (int)Firewall.Interfaces.Wireless) != 0)
                        interfaces.Add(Translate.fmt("str_wifi"));
                    return string.Join(",", interfaces.ToArray().Reverse());
                }
            }
        }

        public string EdgeTraversal { get { return Rule.EdgeTraversal.ToString(); } }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Helpers

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
