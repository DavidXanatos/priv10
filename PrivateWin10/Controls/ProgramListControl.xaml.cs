using PrivateWin10.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for ProgramListControl.xaml
    /// </summary>
    public partial class ProgramListControl : UserControl
    {
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

        public CategoryModel CatModel;

        int SuspendChange = 0;

        public event EventHandler<EventArgs> SelectionChanged;

        FirewallPage.FilterPreset CurFilter = null;

        SortedDictionary<Guid, ProgramControl> Programs = new SortedDictionary<Guid, ProgramControl>();

        List<ProgramControl> SelectedPrograms;

        /*protected ObservableCollection<ProgramSet> ProgramSets = null;
        public ObservableCollection<ProgramSet> ItemsSource { get { return ProgramSets; } set {
                if(ProgramSets != null)
                    ProgramSets.CollectionChanged -= ProgramSets_CollectionChanged;
                ProgramSets = value;
                ProgramSets.CollectionChanged += ProgramSets_CollectionChanged;
            } }

        private void ProgramSets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }
        */

        public ProgramListControl()
        {
            InitializeComponent();

            SelectedPrograms = new List<ProgramControl>();

            SuspendChange++;

            //this.rbbSort.Header = Translate.fmt("lbl_sort_and");
            this.lblSort.Content = Translate.fmt("lbl_sort");
            //this.chkNoLocal.Content = Translate.fmt("chk_ignore_local");
            //this.chkNoLan.Content = Translate.fmt("chk_ignore_lan");

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

            SuspendChange--;

            this.processScroll.PreviewKeyDown += process_KeyEventHandler;
        }

        public void UpdateProgramList(List<ProgramSet> progs)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Dictionary<Guid, ProgramControl> OldProcesses = new Dictionary<Guid, ProgramControl>(Programs);

            foreach (ProgramSet prog in progs)
            {
                ProgramControl item;
                if (Programs.TryGetValue(prog.guid, out item))
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
                Programs.Remove(guid);
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
            Programs.Add(prog.guid, item);
            //item.Tag = process;
            item.VerticalAlignment = VerticalAlignment.Top;
            item.HorizontalAlignment = HorizontalAlignment.Stretch;
            item.Margin = new Thickness(1, 1, 1, 1);
            //item.MouseDown += new MouseButtonEventHandler(process_Click);
            item.Click += new RoutedEventHandler(process_Click);

            this.processGrid.Children.Add(item);

            return item;
        }

        public void UpdateProgramItems(List<ProgramSet> progs)
        {
            foreach (ProgramSet prog in progs)
            {
                ProgramControl item;
                if (Programs.TryGetValue(prog.guid, out item))
                {
                    item.Program = prog;
                    item.DoUpdate();
                }
                else
                    item = AddProgramItem(prog);
            }
        }

        public void SortAndFitlerProgList(FirewallPage.FilterPreset Filter)
        {
            CurFilter = Filter;

            SortAndFitlerProgList();
        }

        private void SortAndFitlerProgList()
        {
            List<ProgramControl> OrderList = Programs.Values.ToList();

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

                item.Visibility = (CurFilter != null && FirewallPage.DoFilter(CurFilter, item.Program)) ? Visibility.Collapsed : Visibility.Visible;
            }

            while (OrderList.Count < this.processGrid.RowDefinitions.Count)
                this.processGrid.RowDefinitions.RemoveAt(OrderList.Count);
        }

        void process_KeyEventHandler(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Up || e.Key == Key.Down) /*&& !((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)*/)
            {
                ProgramControl curProcess = null;
                if (SelectedPrograms.Count > 0)
                {
                    foreach (ProgramControl curProg in SelectedPrograms)
                        curProg.SetFocus(false);
                    curProcess = SelectedPrograms[SelectedPrograms.Count - 1];
                    SelectedPrograms.Clear();
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
                SelectedPrograms.Add(curProcess);

                SelectionChanged?.Invoke(this, new EventArgs());
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

            SortAndFitlerProgList();
        }

        private void ChkNoLocal_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
        }

        private void ChkNoLan_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLan", this.chkNoLan.IsChecked == true ? 1 : 0);
        }

        void process_Click(object sender, RoutedEventArgs e)
        {
            ProgramControl curProcess = sender as ProgramControl;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (SelectedPrograms.Contains(curProcess))
                {
                    curProcess.SetFocus(false);
                    SelectedPrograms.Remove(curProcess);
                }
                else
                {
                    curProcess.SetFocus(true);
                    SelectedPrograms.Add(curProcess);
                }
            }
            else
            {
                foreach (ProgramControl curProg in SelectedPrograms)
                    curProg.SetFocus(false);
                SelectedPrograms.Clear();
                SelectedPrograms.Add(curProcess);
                curProcess.SetFocus(true);
            }

            SelectionChanged?.Invoke(this, new EventArgs());
        }

        public List<object> GetSelectedItems()
        {
            List<object> progSets = new List<object>();
            foreach (var item in SelectedPrograms)
                progSets.Add(item.Program);
            return progSets;
        }

        public ProgramSet GetProgSet(Guid guid)
        {
            ProgramControl item;
            if (Programs.TryGetValue(guid, out item))
                return item.Program;
            return null;
        }

        public bool OnActivity(ProgramSet prog, Program program, Engine.FwEventArgs args)
        {
            ProgramControl item = null; 
            if (!Programs.TryGetValue(args.guid, out item))
            {
                if (FirewallPage.DoFilter(CurFilter, prog))
                    return false;

                item = AddProgramItem(prog);

                args.update = false;
            }

            //Note: windows firewall doesn't block localhost acces so we ignore it
            //if (args.entry.State == Program.LogEntry.States.RuleError
            //  && args.entry.FwEvent.Action == FirewallRule.Actions.Allow
            //  && !NetFunc.IsLocalHost(args.entry.FwEvent.RemoteAddress))
            //    item.SetError(true);

            if ((chkNoLocal.IsChecked != true || (!NetFunc.IsLocalHost(args.entry.FwEvent.RemoteAddress) && !NetFunc.IsMultiCast(args.entry.FwEvent.RemoteAddress)))
             && (chkNoLan.IsChecked != true || !FirewallRule.MatchAddress(args.entry.FwEvent.RemoteAddress, FirewallRule.AddrKeywordLocalSubnet))
             && args.entry.FwEvent.ProcessId != ProcFunc.CurID) // Note: When DNS proxy is nabled we are always very active, so ignore it
            {
                switch (args.entry.FwEvent.Action)
                {
                    case FirewallRule.Actions.Allow: item.Flash(Colors.LightGreen); break;
                    case FirewallRule.Actions.Block: item.Flash(Colors.LightPink); break;
                }
            }

            item.DoUpdate();

            return SortBy == Sorts.LastActivity;
        }
    }
}
