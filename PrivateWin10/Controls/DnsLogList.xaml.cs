using PrivateWin10.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for DnsLogList.xaml
    /// </summary>
    public partial class DnsLogList : UserControl
    {
        DataGridExt dnsGridExt;

        public FirewallPage firewallPage = null;

        string textFilter = "";

        ObservableCollection<DnsItem> LogList;

        public DnsLogList()
        {
            InitializeComponent();

            this.txtDnsFilter.LabelText = Translate.fmt("lbl_text_filter");

            this.dnsGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.dnsGrid.Columns[2].Header = Translate.fmt("lbl_host_name");
            this.dnsGrid.Columns[3].Header = Translate.fmt("lbl_last_seen");
            this.dnsGrid.Columns[4].Header = Translate.fmt("lbl_seen_count");
            this.dnsGrid.Columns[5].Header = Translate.fmt("lbl_con_count");
            this.dnsGrid.Columns[6].Header = Translate.fmt("lbl_uploaded");
            this.dnsGrid.Columns[7].Header = Translate.fmt("lbl_downloaded");
            this.dnsGrid.Columns[8].Header = Translate.fmt("lbl_program");

            dnsGridExt = new DataGridExt(dnsGrid);
            dnsGridExt.Restore(App.GetConfig("GUI", "dnsGrid_Columns", ""));

            LogList = new ObservableCollection<DnsItem>();
            dnsGrid.ItemsSource = LogList;

            textFilter = App.GetConfig("GUI", "DnsFilter", "");
            txtDnsFilter.Text = textFilter;

            UwpFunc.AddBinding(dnsGrid, new KeyGesture(Key.F, ModifierKeys.Control), (s, e) => { this.txtDnsFilter.Focus(); });

            CheckLogEntries();
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "dnsGrid_Columns", dnsGridExt.Save());
        }

        public void UpdateDnsLog(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                LogList.Clear();

            Dictionary<Guid, DnsItem> oldLog = new Dictionary<Guid, DnsItem>();
            foreach (DnsItem oldItem in LogList)
                oldLog.Add(oldItem.entry.guid, oldItem);

            Dictionary<Guid, List<Program.DnsEntry>> entries = App.client.GetDomains(firewallPage.GetCurGuids());
            foreach (var entrySet in entries)
            {
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key);
                if (prog == null)
                    continue;

                foreach (Program.DnsEntry logEntry in entrySet.Value)
                {
                    DnsItem entry = null;
                    if (!oldLog.TryGetValue(logEntry.guid, out entry))
                    {
                        oldLog.Remove(logEntry.guid);

                        Program program = ProgramList.GetProgramFuzzy(prog.Programs, logEntry.ProgID, ProgramList.FuzzyModes.Any);

                        LogList.Insert(0, new DnsItem(logEntry, program != null ? program.Description : prog.config.Name));
                    }
                    else // update entry
                    {
                        oldLog.Remove(logEntry.guid);
                        entry.Update(logEntry);
                    }
                }
            }

            foreach (DnsItem item in oldLog.Values)
                LogList.Remove(item);
                
            // force sort
            // todo: improve that
            /*if (sockGrid.Items.SortDescriptions.Count > 0)
            {
                sockGrid.Items.SortDescriptions.Insert(0, sockGrid.Items.SortDescriptions.First());
                sockGrid.Items.SortDescriptions.RemoveAt(0);
            }*/
        }

        private void CheckLogEntries()
        {
            // todo:
        }

        private void DnsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckLogEntries();
        }

        private void TxtRuleFilter_Search(object sender, RoutedEventArgs e)
        {
            textFilter = txtDnsFilter.Text;
            App.SetConfig("GUI", "DnsFilter", textFilter);
            //UpdateRules(true);

            dnsGrid.Items.Filter = new Predicate<object>(item => LogFilter(item));
        }

        private bool LogFilter(object obj)
        {
            var item = obj as DnsItem;

            if (item.TestFilter(textFilter))
                return false;
            return true;
        }

        public void ClearLog()
        {
            if (MessageBox.Show(Translate.fmt("msg_clear_dns"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            if (App.client.ClearDnsLog())
                LogList.Clear();
        }

        /////////////////////////////////
        /// DnsItem

        public class DnsItem : INotifyPropertyChanged
        {
            public Program.DnsEntry entry;
            public string name;

            public DnsItem(Program.DnsEntry entry, string name)
            {
                this.entry = entry;
                this.name = name != null ? name : "[unknown progream]";
            }

            public bool TestFilter(string textFilter)
            {
                string strings = this.Name;
                strings += " " + this.HostName;
                strings += " " + this.LastSeen;
                strings += " " + this.SeenCount;
                strings += " " + this.ConnectionCount;
                strings += " " + this.Uploaded;
                strings += " " + this.Downloaded;
                return FirewallPage.DoFilter(textFilter, strings, new List<ProgramID>() { this.entry.ProgID });
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(entry.ProgID.Path, 16); } }

            public string Name { get { return name; } }
            public string HostName { get { return entry.HostName; } }
            public DateTime LastSeen { get { return entry.LastSeen; } }
            public int SeenCount { get { return entry.SeenCounter; } }
            public int ConnectionCount { get { return entry.ConCounter; } }
            public UInt64 Uploaded{ get { return entry.TotalUpload; } }
            public UInt64 Downloaded { get { return entry.TotalDownload; } }
            public string Program { get { return entry.ProgID.FormatString(); } }
            
            void UpdateValue<T>(ref T value, T new_value, string Name)
            {
                if (value == null ? new_value == null : value.Equals(new_value))
                    return;
                value = new_value;
                NotifyPropertyChanged(Name);
            }

            internal void Update(Program.DnsEntry new_entry)
            {
                UpdateValue(ref entry.SeenCounter, new_entry.SeenCounter, "SeenCount");
                UpdateValue(ref entry.ConCounter, new_entry.ConCounter, "ConnectionCount");
                UpdateValue(ref entry.LastSeen, new_entry.LastSeen, "LastSeen");
                UpdateValue(ref entry.TotalDownload, new_entry.TotalDownload, "Downloaded");
                UpdateValue(ref entry.TotalUpload, new_entry.TotalUpload, "Uploaded");
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private Helpers

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }
    }
}
