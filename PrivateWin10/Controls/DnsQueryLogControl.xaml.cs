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

        public DnsQueryLogControl()
        {
            InitializeComponent();

            logGridExt = new DataGridExt(logGrid);

            QueryLogList = new ObservableCollection<DnsQueryItem>();
            logGrid.ItemsSource = QueryLogList;
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
