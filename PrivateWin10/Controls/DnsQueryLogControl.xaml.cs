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

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for DnsQueryLogControl.xaml
    /// </summary>
    public partial class DnsQueryLogControl : UserControl
    {
        ObservableCollection<DnsQueryItem> QueryLogList;

        DataGridExt logGridExt;

        string textFilter = "";

        ContextMenu contextMenu;

        public DnsQueryLogControl()
        {
            InitializeComponent();

            this.caption.Text = Translate.fmt("btn_query_log");

            this.logGrid.Columns[0].Header = Translate.fmt("lbl_time_stamp");
            this.logGrid.Columns[1].Header = Translate.fmt("lbl_query");
            this.logGrid.Columns[2].Header = Translate.fmt("lbl_type");
            this.logGrid.Columns[3].Header = Translate.fmt("lbl_state");
            this.logGrid.Columns[4].Header = Translate.fmt("lbl_reply");
            this.logGrid.Columns[5].Header = Translate.fmt("lbl_ttl");

            this.btnClear.Content = Translate.fmt("btn_clear_log");
            this.btnRefresh.Content = Translate.fmt("btn_refresh_log");

            logGridExt = new DataGridExt(logGrid);

            QueryLogList = new ObservableCollection<DnsQueryItem>();
            logGrid.ItemsSource = QueryLogList;

            contextMenu = new ContextMenu();

            WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_blacklist"), btnBlacklist_Click);
            WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_whitelist"), btnWhitelist_Click);
            contextMenu.Items.Add(new Separator());
            WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_copy_query"), btnCopyDomain_Click);
            WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_copy_answer"), btnCopyReply_Click);

            contextMenu.IsEnabled = false;

            logGrid.ContextMenu = contextMenu;
        }


        public void UpdateList()
        {
            List<DnsCacheMonitor.DnsCacheEntry> QueryLog = App.client.GetLoggedDnsQueries();
            if (QueryLog == null)
                return;

            QueryLogList.Clear();
            foreach (var QueryEntry in QueryLog)
                AddItem(QueryEntry);
        }

        private void AddItem(DnsCacheMonitor.DnsCacheEntry QueryEntry)
        {
            var Item = new DnsQueryItem(QueryEntry);
            QueryLogList.Insert(0, Item);
        }

        private void LogGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            contextMenu.IsEnabled = logGrid.SelectedItems.Count > 0;
        }

        private void btnBlacklist_Click(object sender, RoutedEventArgs e)
        {
            foreach (DnsQueryItem item in logGrid.SelectedItems)
                App.client.UpdateDomainFilter(DnsBlockList.Lists.Blacklist, new DomainFilter() { Domain = item.Question, Enabled = true, Format = DomainFilter.Formats.Plain });
        }

        private void btnWhitelist_Click(object sender, RoutedEventArgs e)
        {
            foreach (DnsQueryItem item in logGrid.SelectedItems)
                App.client.UpdateDomainFilter(DnsBlockList.Lists.Whitelist, new DomainFilter() { Domain = item.Question, Enabled = true, Format = DomainFilter.Formats.Plain });
        }

        private void btnCopyDomain_Click(object sender, RoutedEventArgs e)
        {
            List<string> Lines = new List<string>();
            foreach (DnsQueryItem item in logGrid.SelectedItems)
                Lines.Add(item.Question);
            MiscFunc.ClipboardNative.CopyTextToClipboard(String.Join("\r\n", Lines));
        }

        private void btnCopyReply_Click(object sender, RoutedEventArgs e)
        {
            List<string> Lines = new List<string>();
            foreach (DnsQueryItem item in logGrid.SelectedItems)
                Lines.Add(item.Reply);
            MiscFunc.ClipboardNative.CopyTextToClipboard(String.Join("\r\n", Lines));
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            QueryLogList.Clear();
            App.client.ClearLoggedDnsQueries();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFilter = txtSearch.Text;
            logGrid.Items.Filter = new Predicate<object>(item => LogFilter(item));
        }

        private bool LogFilter(object obj)
        {
            var item = obj as DnsQueryItem;

            if (item.Question.Contains(textFilter))
                return true;
            if (item.Reply != null && item.Reply.Contains(textFilter))
                return true;
            return false;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        /////////////////////////////////
        /// FilterListItem

        public class DnsQueryItem : INotifyPropertyChanged
        {
            public DnsCacheMonitor.DnsCacheEntry QueryEntry;

            public DnsQueryItem(DnsCacheMonitor.DnsCacheEntry QueryEntry)
            {
                this.QueryEntry = QueryEntry;
            }

            public DateTime? TimeStamp { get { return QueryEntry.TimeStamp; } }

            public string Question { get { return QueryEntry.HostName; } }

            public string Type { get { return QueryEntry.RecordType.ToString(); } }

            public string State { get { return QueryEntry.State.ToString(); } } // todo: localize
            
            public string Reply { get { return QueryEntry.ResolvedString != null ? QueryEntry.ResolvedString : QueryEntry.Address?.ToString(); } }

            public int TTL { get { return QueryEntry.GetTTL(); } }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
