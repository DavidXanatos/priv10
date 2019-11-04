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

        string mDnsFilter = "";

        ObservableCollection<DnsItem> LogList;

        public DnsLogList()
        {
            InitializeComponent();

            this.dnsGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.dnsGrid.Columns[2].Header = Translate.fmt("lbl_host_name");
            this.dnsGrid.Columns[3].Header = Translate.fmt("lbl_last_seen");
            this.dnsGrid.Columns[4].Header = Translate.fmt("lbl_seen_count");
            this.dnsGrid.Columns[5].Header = Translate.fmt("lbl_program");

            dnsGridExt = new DataGridExt(dnsGrid);
            dnsGridExt.Restore(App.GetConfig("GUI", "dnsGrid_Columns", ""));

            LogList = new ObservableCollection<DnsItem>();
            dnsGrid.ItemsSource = LogList;

            mDnsFilter = App.GetConfig("GUI", "DnsFilter", "");
            txtDnsFilter.Text = mDnsFilter;

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

            Dictionary<Guid, List<Program.DnsEntry>> entries = App.client.GetDomains(firewallPage.GetCurGuids(mDnsFilter));
            foreach (var entrySet in entries)
            {
                ProgramControl item = null;
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key, null, out item);
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

        private void txtDnsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mDnsFilter = txtDnsFilter.Text;
            App.SetConfig("GUI", "DnsFilter", mDnsFilter);
            UpdateDnsLog(true);
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

            void DoUpdate()
            {
                NotifyPropertyChanged(null);
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(entry.ProgID.Path, 16); } }

            public string Name { get { return name; } }
            public string HostName { get { return entry.HostName; } }
            public string LastSeen { get { return entry.LastSeen.ToString("HH:mm:ss dd.MM.yyyy"); } }
            public string SeenCount { get { return entry.SeenCounter.ToString(); } }
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
                UpdateValue(ref entry.LastSeen, new_entry.LastSeen, "LastSeen");
                UpdateValue(ref entry.SeenCounter, new_entry.SeenCounter, "SeenCount");
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
    }
}
