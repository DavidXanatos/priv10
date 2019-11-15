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
    /// Interaction logic for FirewallLogList.xaml
    /// </summary>
    public partial class FirewallLogList : UserControl
    {
        DataGridExt logGridExt;

        ObservableCollection<LogItem> LogList;

        public FirewallPage firewallPage = null;

        FirewallRule.Actions evetnFilter = FirewallRule.Actions.Undefined;
        string textFilter = "";
        int logLimit = 1000;

        public FirewallLogList()
        {
            InitializeComponent();

            //this.grpLog.Header = Translate.fmt("gtp_con_log");
            /*this.grpLogTools.Header = Translate.fmt("grp_tools");
            this.grpLogView.Header = Translate.fmt("grp_view");

            this.btnMkRule.Content = Translate.fmt("btn_mk_rule");
            this.btnClearLog.Content = Translate.fmt("btn_clear_log");
            this.lblShowCons.Content = Translate.fmt("lbl_show_cons");
            this.chkNoLocal.Content = Translate.fmt("chk_hide_local");
            this.chkNoLAN.Content = Translate.fmt("chk_hide_lan");            
            this.lblFilterCons.Content = Translate.fmt("lbl_filter_cons");*/

            this.lblFilter.Content = Translate.fmt("lbl_filter");
            this.cmbAll.Content = Translate.fmt("str_all_events");
            this.cmbAllow.Content = Translate.fmt("str_allowed");
            this.cmbBlock.Content = Translate.fmt("str_blocked");
            this.chkNoINet.ToolTip = Translate.fmt("str_no_inet");
            this.chkNoLAN.ToolTip = Translate.fmt("str_no_lan");
            this.chkNoMulti.ToolTip = Translate.fmt("str_no_multi");
            this.chkNoLocal.ToolTip = Translate.fmt("str_no_local");


            this.logGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.logGrid.Columns[2].Header = Translate.fmt("lbl_time_stamp");
            this.logGrid.Columns[3].Header = Translate.fmt("lbl_action");
            this.logGrid.Columns[4].Header = Translate.fmt("lbl_direction");
            this.logGrid.Columns[5].Header = Translate.fmt("lbl_protocol");
            this.logGrid.Columns[6].Header = Translate.fmt("lbl_remote_ip");
            this.logGrid.Columns[7].Header = Translate.fmt("lbl_remote_port");
            this.logGrid.Columns[8].Header = Translate.fmt("lbl_local_ip");
            this.logGrid.Columns[9].Header = Translate.fmt("lbl_local_port");
            this.logGrid.Columns[10].Header = Translate.fmt("lbl_program");

            LogList = new ObservableCollection<LogItem>();
            logGrid.ItemsSource = LogList;

            logGridExt = new DataGridExt(logGrid);
            logGridExt.Restore(App.GetConfig("FwLog", "Columns", ""));

            try
            {
                textFilter = App.GetConfig("FwLog", "Filter", "");
                txtConFilter.Text = textFilter;
                cmbConTypes.SelectedIndex = App.GetConfigInt("FwLog", "Events", 0);
                //this.chkAllowed.IsChecked = App.GetConfigInt("FwLog", "ShowAllowed", 1) == 1;
                //this.chkBlocked.IsChecked = App.GetConfigInt("FwLog", "ShowBlocked", 1) == 1;
                this.chkNoLocal.IsChecked = App.GetConfigInt("FwLog", "NoLocal", 0) == 1;
                this.chkNoMulti.IsChecked = App.GetConfigInt("FwLog", "NoMulti", 0) == 1;
                this.chkNoLAN.IsChecked = App.GetConfigInt("FwLog", "NoLan", 0) == 1;
                this.chkNoINet.IsChecked = App.GetConfigInt("FwLog", "NoINet", 0) == 1;
            }
            catch { }

            //mConLimit = App.engine.programs.MaxLogLength * 10; // todo
            logLimit = App.GetConfigInt("GUI", "LogLimit", 1000);

            CheckLogLines();
        }

        public void OnClose()
        {
            App.SetConfig("FwLog", "Columns", logGridExt.Save());
        }

        public void UpdateConnections(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                LogList.Clear();

            Dictionary<Guid, LogItem> oldLog = new Dictionary<Guid, LogItem>();
            foreach (LogItem oldItem in LogList)
                oldLog.Add(oldItem.entry.guid, oldItem);

            Dictionary<Guid, List<Program.LogEntry>> entries = App.client.GetConnections(firewallPage.GetCurGuids());
            foreach (var entrySet in entries)
            {
                ProgramControl item = null;
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key, null, out item);
                if (prog == null)
                    continue;

                foreach (Program.LogEntry entry in entrySet.Value)
                {
                    //if (!TestEntry(prog, entry))
                    //    continue;

                    //LogItem Item;
                    //if (!oldLog.TryGetValue(entry.guid, out Item))
                    if (!oldLog.Remove(entry.guid))
                    {
                        Program program = ProgramList.GetProgramFuzzy(prog.Programs, entry.ProgID, ProgramList.FuzzyModes.Any);

                        LogList.Insert(0, new LogItem(entry, program != null ? program.Description : prog.config.Name));
                    }
                    /*else
                    {
                        oldLog.Remove(entry.guid);
                        Item.Update(entry);
                    }*/
                }
            }

            foreach (LogItem item in oldLog.Values)
                LogList.Remove(item);
        }

        /*private bool TestEntry(ProgramSet prog, Program.LogEntry entry)
        {
            switch (mConTypes)
            {
                case ConTypes.Allowed: if (entry.FwEvent.Action != FirewallRule.Actions.Allow) return false; break;
                case ConTypes.Blocked: if (entry.FwEvent.Action != FirewallRule.Actions.Block) return false; break;
            }

            if (chkNoLocal.IsChecked == true && (NetFunc.IsLocalHost(entry.FwEvent.RemoteAddress) || NetFunc.IsMultiCast(entry.FwEvent.RemoteAddress)))
                return false;

            if (chkNoLAN.IsChecked == true && FirewallRule.MatchAddress(entry.FwEvent.RemoteAddress, "LocalSubnet"))
                return false; 

            if (FirewallPage.DoFilter(mConFilter, prog.config.Name, new List<ProgramID>() { entry.ProgID }))
                return false;

            return true;
        }*/

        public void AddEntry(ProgramSet prog, Program program, Engine.FwEventArgs args)
        {
            //if (!TestEntry(prog, args.entry))
            //    return;

            if (args.update)
            {
                foreach (LogItem Item in LogList)
                {
                    if (Item.entry.guid.Equals(args.entry.guid))
                    {
                        Item.Update(args.entry);
                        return;
                    }
                }
            }

            LogList.Insert(0, new LogItem(args.entry, program != null ? program.Description : null));

            while (LogList.Count > logLimit)
                LogList.RemoveAt(logLimit);
        }

        private void cmbConTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            evetnFilter = (FirewallRule.Actions)cmbConTypes.SelectedIndex;
            cmbConTypes.Background = (cmbConTypes.SelectedItem as ComboBoxItem).Background;
            App.SetConfig("FwLog", "Events", (int)evetnFilter);
            //UpdateConnections(true);
            logGrid.Items.Filter = new Predicate<object>(item => LogFilter(item));
        }
		
        private void LogTypeFilters_Click(object sender, RoutedEventArgs e)
        {
            //App.SetConfig("FwLog", "ShowAllowed", this.chkAllowed.IsChecked == true ? 1 : 0);
            //App.SetConfig("FwLog", "ShowBlocked", this.chkBlocked.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "NoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "NoMulti", this.chkNoMulti.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "NoLan", this.chkNoLAN.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "NoINet", this.chkNoINet.IsChecked == true ? 1 : 0);
            //UpdateConnections(true);
            logGrid.Items.Filter = new Predicate<object>(item => LogFilter(item));
        }


        private void txtConFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFilter = txtConFilter.Text;
            App.SetConfig("FwLog", "Filter", textFilter);
            //UpdateConnections(true);
            logGrid.Items.Filter = new Predicate<object>(item => LogFilter(item));
        }

        private bool LogFilter(object obj)
        {
            var item = obj as LogItem;

            switch (evetnFilter)
            {
                case FirewallRule.Actions.Allow: if (item.entry.FwEvent.Action != FirewallRule.Actions.Allow) return false; break;
                case FirewallRule.Actions.Block: if (item.entry.FwEvent.Action != FirewallRule.Actions.Block) return false; break;
            }

            if (item.IsLocal)
            {
                if (chkNoLocal.IsChecked == true)
                    return false;
            }
            else if (item.IsMulti)
            {
                if (chkNoMulti.IsChecked == true)
                    return false;
            }
            else if (item.IsLan)
            {
                if (chkNoLAN.IsChecked == true)
                    return false;
            }
            else if (chkNoINet.IsChecked == true)
                return false;

            if (FirewallPage.DoFilter(textFilter, item.name, new List<ProgramID>() { item.entry.ProgID }))
                return false;
            return true;
        }

        /*
        private void btnMkRule_Click(object sender, RoutedEventArgs e)
        {
            LogItem entry = (logGrid.SelectedItem as LogItem);
            if (entry == null)
                return;

            ProgramControl item = null;
            ProgramSet prog = firewallPage.GetProgSet(entry.entry.guid, entry.entry.ProgID, out item);
            if (prog == null)
                return;
            Program program = null;
            prog.Programs.TryGetValue(entry.entry.ProgID, out program);
            if (program == null)
                return;

            FirewallRule rule = new FirewallRule() { guid = null, ProgID = entry.entry.ProgID, Profile = (int)FirewallRule.Profiles.All, Interface = (int)FirewallRule.Interfaces.All, Enabled = true };

            rule.ProgID = entry.entry.ProgID;
            rule.Name = Translate.fmt("custom_rule", program.Description);
            rule.Grouping = FirewallManager.RuleGroup;

            rule.Direction = entry.entry.FwEvent.Direction;
            rule.Protocol = (int)entry.entry.FwEvent.Protocol;
            switch (entry.entry.FwEvent.Protocol)
            {
                /case (int)FirewallRule.KnownProtocols.ICMP:
                case (int)FirewallRule.KnownProtocols.ICMPv6:

                    break;/
                case (int)FirewallRule.KnownProtocols.TCP:
                case (int)FirewallRule.KnownProtocols.UDP:
                    rule.RemotePorts = entry.entry.FwEvent.RemotePort.ToString();
                    break;
            }
            rule.RemoteAddresses = entry.entry.FwEvent.RemoteAddress.ToString();

            firewallPage.ShowRuleWindow(rule);
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_clear_log"), App.mName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (res == MessageBoxResult.Cancel)
                return;

            if (App.client.ClearLog(res == MessageBoxResult.Yes))
                LogList.Clear();
        }
        */

        private void CheckLogLines()
        {
            //btnMkRule.IsEnabled = consGrid.SelectedItems.Count == 1; // todo xxx
        }


        private void ConsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckLogLines();
        }


        /////////////////////////////////
        /// LogItem

        public class LogItem : INotifyPropertyChanged
        {
            public Program.LogEntry entry;
            public string name;
            public bool IsLocal;
            public bool IsMulti;
            public bool IsLan;

            public LogItem(Program.LogEntry entry, string name)
            {
                this.entry = entry;
                this.name = name != null ? name : "[unknown progream]";

                this.IsLocal = NetFunc.IsLocalHost(entry.FwEvent.RemoteAddress);
                this.IsMulti = NetFunc.IsMultiCast(entry.FwEvent.RemoteAddress);
                this.IsLan = FirewallRule.MatchAddress(entry.FwEvent.RemoteAddress, "LocalSubnet");
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(entry.ProgID.Path, 16); } }

            public string Name { get { return name; } }
            public string Program { get { return entry.ProgID.FormatString(); } }
            public DateTime TimeStamp { get { return entry.FwEvent.TimeStamp; } }
            public string Action
            {
                get
                {
                    switch (entry.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow: return Translate.fmt("str_allow");
                        case FirewallRule.Actions.Block: return Translate.fmt("str_block");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }

            public string Direction
            {
                get
                {
                    switch (entry.FwEvent.Direction)
                    {
                        case FirewallRule.Directions.Inbound: return Translate.fmt("str_inbound");
                        case FirewallRule.Directions.Outboun: return Translate.fmt("str_outbound");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }
            public string Protocol { get { return (entry.FwEvent.Protocol == (int)NetFunc.KnownProtocols.Any) ? Translate.fmt("pro_any") : NetFunc.Protocol2Str(entry.FwEvent.Protocol); } }
            public string DestAddress
            {
                get
                {
                    if (entry.HasHostName())
                        return entry.FwEvent.RemoteAddress.ToString() + " (" + entry.GetHostName() + ")";
                    return entry.FwEvent.RemoteAddress.ToString();
                }
            }
            public string DestPorts { get { return entry.FwEvent.RemotePort.ToString(); } }
            public string SrcAddress { get { return entry.FwEvent.LocalAddress.ToString(); } }
            public string SrcPorts { get { return entry.FwEvent.LocalPort.ToString(); } }

            public string ActionColor
            {
                get
                {
                    if (entry.State == PrivateWin10.Program.LogEntry.States.RuleError) return "warn";
                    switch (entry.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow:
                            if (IsMulti)
                                return "blue2";
                            else if (IsLan)
                                return "blue";
                            else
                                return "green";
                        case FirewallRule.Actions.Block:
                            if (IsMulti)
                                return "yellow2";
                            else if (IsLan)
                                return "yellow";
                            else
                                return "red";
                        default: return "";
                    }
                }
            }

            void UpdateValue<T>(ref T value, T new_value, string Name)
            {
                if (value == null ? new_value == null : value.Equals(new_value))
                    return;
                value = new_value;
                NotifyPropertyChanged(Name);
            }

            internal void Update(Program.LogEntry new_args)
            {
                if(entry.Update(new_args))
                    NotifyPropertyChanged("DestAddress");
                // The rest can't change
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
