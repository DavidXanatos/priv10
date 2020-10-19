using MiscHelpers;
using PrivateAPI;
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
using WinFirewallAPI;

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

        ControlList<ProgramControl, ProgramSet> ProgramList;

        int DoSort(ProgramControl l, ProgramControl r)
        {
            switch (SortBy)
            {
                case Sorts.Name: return l.progSet.config.Name.CompareTo(r.progSet.config.Name);
                case Sorts.NameRev: return r.progSet.config.Name.CompareTo(l.progSet.config.Name);
                case Sorts.LastActivity: return r.progSet.GetLastActivity().CompareTo(l.progSet.GetLastActivity());
                case Sorts.DataRate: return r.progSet.GetDataRate().CompareTo(l.progSet.GetDataRate());
                case Sorts.SocketCount: return r.progSet.GetSocketCount().CompareTo(l.progSet.GetSocketCount());
                case Sorts.ModuleCount: return r.progSet.Programs.Count.CompareTo(l.progSet.Programs.Count);
            }
            return 0;
        }

        public ProgramListControl()
        {
            InitializeComponent();

            ProgramList = new ControlList<ProgramControl, ProgramSet>(this.processScroll, (prog) => { return new ProgramControl(prog, CatModel); }, (prog)=>prog.guid.ToString(),
                (list)=> { list.Sort(DoSort); }, (item)=> { return (CurFilter != null && FirewallPage.DoFilter(CurFilter, item.progSet)); });

            ProgramList.SelectionChanged += (s, e) => { SelectionChanged?.Invoke(this, e); };

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
        }

        public void UpdateProgramList(List<ProgramSet> progs)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            ProgramList.UpdateItems(progs);

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            AppLog.Debug("UpdateProgramList took: " + elapsedMs + "ms");

            ProgramList.SortAndFitlerList();
        }

        public void UpdateProgramItems(List<ProgramSet> progs)
        {
            foreach (ProgramSet prog in progs)
                ProgramList.UpdateItem(prog);
        }

        public void SortAndFitlerProgList(FirewallPage.FilterPreset Filter)
        {
            CurFilter = Filter;

            ProgramList.SortAndFitlerList();
        }


        //private void cmbSort_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //mSortBy = (Sorts)((sender as RibbonGallery).SelectedItem as RibbonGalleryItem).Tag;
            SortBy = (Sorts)((sender as ComboBox).SelectedItem as ContentControl).Tag;
            if (SuspendChange != 0)
                return;
            App.SetConfig("GUI", "SortList", (int)SortBy);

            ProgramList.SortAndFitlerList();
        }

        private void ChkNoLocal_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
        }

        private void ChkNoLan_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("GUI", "ActNoLan", this.chkNoLan.IsChecked == true ? 1 : 0);
        }
        

        public List<object> GetSelectedItems()
        {
            List<object> progSets = new List<object>();
            foreach (var item in ProgramList.SelectedItems)
                progSets.Add(item.progSet);
            return progSets;
        }

        public ProgramSet GetProgSet(Guid guid)
        {
            ProgramControl item;
            if (ProgramList.Items.TryGetValue(guid.ToString(), out item))
                return item.progSet;
            return null;
        }

        public bool OnActivity(ProgramSet prog, Program program, Priv10Engine.FwEventArgs args)
        {
            ProgramControl item = null; 
            if (!ProgramList.Items.TryGetValue(args.guid.ToString(), out item))
            {
                if (FirewallPage.DoFilter(CurFilter, prog))
                    return false;

                item = ProgramList.AddItem(prog);

                args.update = false;
            }

            //Note: windows firewall doesn't block localhost acces so we ignore it
            //if (args.entry.State == Program.LogEntry.States.RuleError
            //  && args.entry.FwEvent.Action == FirewallRule.Actions.Allow
            //  && !NetFunc.IsLocalHost(args.entry.FwEvent.RemoteAddress))
            //    item.SetError(true);

            if ((chkNoLocal.IsChecked != true || (args.entry.Realm != Program.LogEntry.Realms.LocalHost && args.entry.Realm != Program.LogEntry.Realms.MultiCast))
             && (chkNoLan.IsChecked != true || args.entry.Realm != Program.LogEntry.Realms.LocalArea)
             && args.entry.FwEvent.ProcessId != ProcFunc.CurID) // Note: When DNS proxy is nabled we are always very active, so ignore it
            {
                switch (args.entry.FwEvent.Action)
                {
                    case FirewallRule.Actions.Allow: item.Flash(Colors.LightGreen); break;
                    case FirewallRule.Actions.Block: item.Flash(Colors.LightPink); break;
                }
            }

            item.DoUpdate(prog);

            return SortBy == Sorts.LastActivity;
        }
    }
}
